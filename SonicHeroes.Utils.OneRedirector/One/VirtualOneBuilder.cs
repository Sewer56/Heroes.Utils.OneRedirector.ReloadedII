using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Heroes.SDK.Definitions.Structures.Archive.OneFile.Custom;
using Heroes.SDK.Parsers;
using Reloaded.Memory;
using SonicHeroes.Utils.OneRedirector.Structs;
using static SonicHeroes.Utils.OneRedirector.Program;

namespace SonicHeroes.Utils.OneRedirector.One
{
    /// <summary>
    /// Stores the information required to build a "Virtual ONE" file.
    /// </summary>
    public unsafe class VirtualOneBuilder
    {
        private static bool _warningGiven = false;

        private Dictionary<string, VirtualFile> _customFiles = new Dictionary<string, VirtualFile>();
        private HashSet<string> _deletedFiles = new HashSet<string>();

        /// <summary>
        /// Adds a file to the Virtual ONE builder.
        /// </summary>
        public void AddOrReplaceFile(string fileName, string filePath)
        {
            bool isCompressed = fileName.EndsWith(Constants.PrsExtension);
            if (isCompressed)
                fileName = fileName.Substring(0, fileName.Length - Constants.PrsExtension.Length);

            #if RELEASE
            if (!isCompressed && !_warningGiven)
            {
                Logger.WriteLine($"[ONE Redirector] Uncompressed file(s) found. This is only meant to be used for testing/creating mods. " +
                                 $"If you are an end user, who downloaded this mod from the internet, please ask the mod author to compress their files. " +
                                 $"Here's a file path to the offending file: {filePath}'", Logger.ColorYellowLight);
                _warningGiven = true;
            }
            #endif

            _customFiles[fileName] = new VirtualFile(filePath, isCompressed);
        }

        /// <summary>
        /// Removes a file from the resulting ONE builder.
        /// </summary>
        public void RemoveFile(string fileName)
        {
            if (fileName.EndsWith(Constants.DeleteExtension))
                fileName = fileName.Substring(0, fileName.Length - Constants.DeleteExtension.Length);

            _deletedFiles.Add(fileName);
        }

        /// <summary>
        /// Builds a virtual ONE based upon a supplied base ONE file.
        /// </summary>
        public VirtualOne Build(string filePath)
        {
            var originalFile = new OneArchive(File.ReadAllBytes(filePath));
            var filesDictionary = new Dictionary<string, ManagedOneFile>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in originalFile.GetFiles()) 
                filesDictionary[file.Name] = file;

            foreach (var deleteFile in _deletedFiles) 
                filesDictionary.Remove(deleteFile);

            foreach (var customFile in _customFiles)
                filesDictionary[customFile.Key] = new ManagedOneFile(customFile.Key, customFile.Value.GetDataCompressed(), true, originalFile.Header->RenderWareVersion);

            var files = filesDictionary.Values.ToArray();
            return new VirtualOne(OneArchive.FromFiles(files));
        }
    }
}
