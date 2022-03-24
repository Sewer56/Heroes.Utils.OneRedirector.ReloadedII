using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Reloaded.Mod.Interfaces;
using SonicHeroes.Utils.OneRedirector.Configuration;
using SonicHeroes.Utils.OneRedirector.One;
using SonicHeroes.Utils.OneRedirector.Structs;

namespace SonicHeroes.Utils.OneRedirector;

/// <summary>
/// FileSystem hook that redirects accesses to ONE file.
/// </summary>
public unsafe class OneHook
{
    private readonly OneFileTracker _oneFileTracker;

    private readonly ILogger _logger;
    private Config _configuration;
    private OneBuilderCollection _builderCollection;
    private readonly Dictionary<string, VirtualOne> _virtualOneFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _modFolders = new();

    public OneHook(ILogger logger, Config configuration, NativeFunctions functions)
    {
        _logger = logger;
        _configuration = configuration;
        _configuration.ConfigurationUpdated += OnConfigurationUpdated;
        _oneFileTracker = new OneFileTracker(functions);
        _oneFileTracker.OnOneHandleOpened += OnOneHandleOpened;
        _oneFileTracker.OnOneReadData += OnOneReadData;
        _oneFileTracker.OnGetFileSize += OnGetFileSize;
    }

    private void OnConfigurationUpdated(IConfigurable obj)
    {
        _configuration = (Config)obj;
        _logger.WriteLine($"[ONE Redirector] Config Updated");
    }

    private int OnGetFileSize(IntPtr handle)
    {
        if (!_oneFileTracker.TryGetInfoForHandle(handle, out var info))
            return -1;

        if (_virtualOneFiles.ContainsKey(info.FilePath))
            return _virtualOneFiles[info.FilePath].File.Length;

        return -1;
    }

    /// <summary>
    /// The evil one. Commits hard drive reading fraud!
    /// </summary>
    private bool OnOneReadData(IntPtr handle, byte* buffer, uint length, long offset, out int numReadBytes)
    {
        if (_oneFileTracker.TryGetInfoForHandle(handle, out var info))
        {
            if (!_virtualOneFiles.ContainsKey(info.FilePath))
            {
                numReadBytes = 0;
                return false;
            }

            var oneFile     = _virtualOneFiles[info.FilePath];
            var bufferSpan  = new Span<byte>(buffer, (int)length);
            var oneFileSpan = new Span<byte>(oneFile.FilePtr, oneFile.File.Length);

            var endOfReadOffset = offset + length;
            if (endOfReadOffset > oneFileSpan.Length)
                length -= (uint)(endOfReadOffset - oneFileSpan.Length);

            var slice = oneFileSpan.Slice((int)offset, (int)length);
            slice.CopyTo(bufferSpan);

            numReadBytes = slice.Length;
            return true;
        }

        numReadBytes = 0;
        return false;
    }

    /// <summary>
    /// When an ONE file is found, associate it with an existing virtual file.
    /// </summary>
    private void OnOneHandleOpened(IntPtr handle, string filepath)
    {
        if (!_configuration.AlwaysBuildArchive && _virtualOneFiles.ContainsKey(filepath))
            return;

        // Note: This is a bit inefficient, but necessary for `Always Build Archive`
        //       allowing modifications without restarting game.
        _builderCollection = new OneBuilderCollection();
        foreach (var modFolder in _modFolders)
            _builderCollection.AddFromFolders(GetRedirectPath(modFolder));

        string fileName = Path.GetFileName(filepath);
        if (_builderCollection.TryGetBuilder(fileName, out var builder))
        {
#if DEBUG
            Console.WriteLine("------------ BUILDING ONE ------------");
            Stopwatch watch = new Stopwatch();
            watch.Start();
#endif
            _virtualOneFiles[filepath] = builder.Build(filepath);
#if DEBUG
            Console.WriteLine($"------------ COMPLETE {watch.ElapsedMilliseconds}ms ------------");
#endif
        }
    }

    /// <param name="modDirectory">The full path to the mod.</param>
    public void OnModLoading(string modDirectory)
    {
        if (Directory.Exists(GetRedirectPath(modDirectory))) 
            _modFolders.Add(modDirectory);
    }

    /// <param name="modDirectory">The full path to the mod.</param>
    public void OnModUnloading(string modDirectory)
    {
        _modFolders.Remove(modDirectory);
    }

    private string GetRedirectPath(string modFolder) => $"{modFolder}/{Constants.RedirectorFolderName}";
}