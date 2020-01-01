using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Heroes.SDK.Definitions.Structures.Archive.OneFile;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory;
using SonicHeroes.Utils.OneRedirector.Structs;
using FileInfo = SonicHeroes.Utils.OneRedirector.Structs.FileInfo;

namespace SonicHeroes.Utils.OneRedirector
{
    public unsafe class OneFileTracker
    {
        /// <summary>
        /// Executed when a handle to a file is opened.
        /// </summary>
        public event HandleOpened OnOneHandleOpened = (path, handle) => { };

        /// <summary>
        /// Executed when application queries for the file size
        /// </summary>
        public event GetFileSize OnGetFileSize = (handle) => -1;

        /// <summary>
        /// Executed after data is read from a file.
        /// </summary>
        public event DataRead OnOneReadData = (IntPtr handle, byte* buffer, uint length, long offset, out int numReadBytes) =>
        {
            numReadBytes = 0;
            return false;
        };

        /// <summary>
        /// Maps file handles to file paths.
        /// </summary>
        private ConcurrentDictionary<IntPtr, FileInfo> _handleToInfoMap = new ConcurrentDictionary<IntPtr, FileInfo>();
        private Dictionary<string, bool> _isOneFileCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private IHook<Native.Native.NtCreateFile> _createFileHook;
        private IHook<Native.Native.NtReadFile> _readFileHook;
        private IHook<Native.Native.NtSetInformationFile> _setFilePointerHook;
        private IHook<Native.Native.NtQueryInformationFile> _getFileSizeHook;

        private object _createLock = new object();
        private object _readLock = new object();
        private object _setInfoLock = new object();
        private object _getInfoLock = new object();

        public OneFileTracker(NativeFunctions functions)
        {
            _createFileHook = functions.NtCreateFile.Hook(NtCreateFileImpl).Activate();
            _readFileHook = functions.NtReadFile.Hook(NtReadFileImpl).Activate();
            _setFilePointerHook = functions.NtSetInformationFile.Hook(SetInformationFileImpl).Activate();
            _getFileSizeHook = functions.NtQueryInformationFile.Hook(QueryInformationFileImpl).Activate();

            // TODO: Hook NtClose
            // Problem: Native->Managed Transition hits NtClose in .NET Core, so our hook code is never hit.
            // Problem: NtClose needs synchronization.
            // Solution: Write custom ASM to solve the problem, see NtClose branch.
        }

        
        /// <summary>
        /// Tries to get the information for a file behind a handle.
        /// </summary>
        public bool TryGetInfoForHandle(IntPtr handle, out FileInfo info)
        {
            info = null;

            if (!_handleToInfoMap.ContainsKey(handle))
                return false;

            info = _handleToInfoMap[handle];
            return true;
        }

        private int QueryInformationFileImpl(IntPtr hfile, out Native.Native.IO_STATUS_BLOCK ioStatusBlock, void* fileInformation, uint length, Native.Native.FileInformationClass fileInformationClass)
        {
            lock (_getInfoLock)
            {
                if (_handleToInfoMap.ContainsKey(hfile) && fileInformationClass == Native.Native.FileInformationClass.FileStandardInformation)
                {
                    var result      = _getFileSizeHook.OriginalFunction(hfile, out ioStatusBlock, fileInformation, length, fileInformationClass);
                    var information = (Native.Native.FILE_STANDARD_INFORMATION*)fileInformation;
                    var newFileSize = OnGetFileSize(hfile);
                    if (newFileSize != -1)
                    {
                        information->EndOfFile = newFileSize;
                        information->AllocationSize = Utilities.RoundUp(newFileSize, 4096);
                    }

#if DEBUG
                    Console.WriteLine($"[ONEHook] QueryInformationFile: Alloc Size: {information->AllocationSize}, EndOfFile: {information->EndOfFile}");
#endif
                    
                    return result;
                }

                return _getFileSizeHook.OriginalFunction(hfile, out ioStatusBlock, fileInformation, length, fileInformationClass);
            }
        }

        private int SetInformationFileImpl(IntPtr hfile, out Native.Native.IO_STATUS_BLOCK ioStatusBlock, void* fileInformation, uint length, Native.Native.FileInformationClass fileInformationClass)
        {
            lock (_setInfoLock)
            {
                if (_handleToInfoMap.ContainsKey(hfile) && fileInformationClass == Native.Native.FileInformationClass.FilePositionInformation)
                {
                    var pointer = *(long*)fileInformation;
                    _handleToInfoMap[hfile].FilePointer = pointer;
                }

                return _setFilePointerHook.OriginalFunction(hfile, out ioStatusBlock, fileInformation, length, fileInformationClass);

            }
        }

        private unsafe int NtReadFileImpl(IntPtr handle, IntPtr hEvent, IntPtr* apcRoutine, IntPtr* apcContext, ref Native.Native.IO_STATUS_BLOCK ioStatus, byte* buffer, uint length, long* byteOffset, IntPtr key)
        {
            lock (_readLock)
            {
                if (_handleToInfoMap.ContainsKey(handle))
                {
                    long offset          = _handleToInfoMap[handle].FilePointer;
                    long requestedOffset = byteOffset != (void*) 0 ? *byteOffset : -1;
#if DEBUG
                    Console.WriteLine($"[ONEHook] Read Request, Buffer: {(long)buffer:X}, Length: {length}, Offset (via SetInformationFile): {offset}, Requested Offset (Optional): {requestedOffset}");
#endif
                    
                    DisableRedirectionHooks();
                    bool result;
                    int numReadBytes;
                    result = requestedOffset != -1 ? OnOneReadData(handle, buffer, length, requestedOffset, out numReadBytes) 
                                                   : OnOneReadData(handle, buffer, length, offset, out numReadBytes);
                    EnableRedirectionHooks();

                    if (result)
                    {
                        offset += length;
                        SetInformationFileImpl(handle, out _, &offset, sizeof(long), Native.Native.FileInformationClass.FilePositionInformation);

                        // Set number of read bytes.
                        ioStatus.Status      = 0;
                        ioStatus.Information = new IntPtr(numReadBytes);
                        return 0;
                    }
                }

                return _readFileHook.OriginalFunction(handle, hEvent, apcRoutine, apcContext, ref ioStatus, buffer, length, byteOffset, key);
            }
        }

        private int NtCreateFileImpl(out IntPtr handle, FileAccess access, ref Native.Native.OBJECT_ATTRIBUTES objectAttributes, ref Native.Native.IO_STATUS_BLOCK ioStatus, ref long allocSize, uint fileAttributes, FileShare share, uint createDisposition, uint createOptions, IntPtr eaBuffer, uint eaLength)
        {
            lock (_createLock)
            {
                string oldFileName = objectAttributes.ObjectName.ToString();
                if (!TryGetFullPath(oldFileName, out var newFilePath))
                    return _createFileHook.OriginalFunction(out handle, access, ref objectAttributes, ref ioStatus, ref allocSize, fileAttributes, share, createDisposition, createOptions, eaBuffer, eaLength);

                // Check if ONE file and register if it is.
                if (newFilePath.Contains(Constants.OneExtension, StringComparison.OrdinalIgnoreCase))
                {
                    var result = _createFileHook.OriginalFunction(out handle, access, ref objectAttributes, ref ioStatus, ref allocSize, fileAttributes, share, createDisposition, createOptions, eaBuffer, eaLength);
                    DisableRedirectionHooks();
                    if (IsOneFile(newFilePath))
                    {
#if DEBUG
                        Console.WriteLine($"[ONEHook] ONE Handle Opened: {handle}, File: {newFilePath}");
#endif
                        _handleToInfoMap[handle] = new FileInfo(newFilePath, 0);
                        OnOneHandleOpened(handle, newFilePath);
                    }
                    EnableRedirectionHooks();
                    return result;
                }

                var ntStatus = _createFileHook.OriginalFunction(out handle, access, ref objectAttributes, ref ioStatus, ref allocSize, fileAttributes, share, createDisposition, createOptions, eaBuffer, eaLength);

                // Invalidate Duplicate Handles (until we implement NtClose hook).
                if (_handleToInfoMap.ContainsKey(handle))
                {
                    _handleToInfoMap.TryRemove(handle, out var value);
#if DEBUG
                    Console.WriteLine($"[ONEHook] Removed old disposed handle: {handle}, File: {value.FilePath}");
#endif
                }

                return ntStatus;
            }
        }

        private void DisableRedirectionHooks()
        {
            _createFileHook.Disable();
            _readFileHook.Disable();
        }

        private void EnableRedirectionHooks()
        {
            _readFileHook.Enable();
            _createFileHook.Enable();
        }

        /// <summary>
        /// Checks if a file at a specified path is an ONE archive.
        /// </summary>
        private bool IsOneFile(string filePath)
        {
            if (_isOneFileCache.ContainsKey(filePath))
                return _isOneFileCache[filePath];

            if (!File.Exists(filePath))
                return false;

            using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, sizeof(OneArchiveHeader));

            var data     = new byte[sizeof(OneArchiveHeader)];
            var dataSpan = data.AsSpan();
            stream.Read(dataSpan);
            Struct.FromArray(dataSpan, out OneArchiveHeader header);

            var sizeOfFile = stream.Seek(0, SeekOrigin.End);
            bool isOneFile = header.FileSize + sizeof(OneArchiveHeader) == sizeOfFile;
            _isOneFileCache[filePath] = isOneFile;
            return isOneFile;
        }

        /// <summary>
        /// Tries to resolve a given file path from NtCreateFile to a full file path.
        /// </summary>
        private bool TryGetFullPath(string oldFilePath, out string newFilePath)
        {
            if (oldFilePath.StartsWith("\\??\\", StringComparison.InvariantCultureIgnoreCase))
                oldFilePath = oldFilePath.Replace("\\??\\", "");

            if (!String.IsNullOrEmpty(oldFilePath))
            {
                newFilePath = Path.GetFullPath(oldFilePath);
                return true;
            }

            newFilePath = oldFilePath;
            return false;
        }

        public delegate int GetFileSize(IntPtr handle);
        public delegate void HandleOpened(IntPtr handle, string filePath);
        public delegate bool DataRead(IntPtr handle, byte* buffer, uint length, long offset, out int numReadBytes);
    }
}
