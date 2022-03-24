using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonicHeroes.Utils.OneRedirector.One;

namespace SonicHeroes.Utils.OneRedirector;

/// <summary>
/// Encapsulates a group of Builders, keyed by file names.
/// </summary>
public class OneBuilderCollection
{
    private readonly Dictionary<string, VirtualOneBuilder> _builders = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns all of the builders held by this collection, mapped string to builder.
    /// </summary>
    /// <returns></returns>
    public KeyValuePair<string, VirtualOneBuilder>[] GetAllBuilders() => _builders.ToArray();

    /// <summary>
    /// Adds files to the collection of builders given a directory containing directories
    /// corresponding to ONE archives.
    /// </summary>
    /// <param name="folder">Folder containing folders named after ONE archives.</param>
    public void AddFromFolders(string folder)
    {
        foreach (var directory in Directory.GetDirectories(folder))
        {
            if (!directory.EndsWith(Constants.OneExtension, StringComparison.OrdinalIgnoreCase))
                continue;

            var builder = GetBuilder(Path.GetFileName(directory));
            foreach (var filePath in Directory.GetFiles(directory))
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName.EndsWith(Constants.DeleteExtension))
                    builder.RemoveFile(fileName);
                else
                    builder.AddOrReplaceFile(fileName, filePath);
            }
        }
    }

    /// <summary>
    /// Tries to get a <see cref="VirtualOneBuilder"/> for the given file name.
    /// Returns true on success, false on failure.
    /// </summary>
    /// <param name="fileName">The file name of the ONE, including extension. Case insensitive.</param>
    public bool TryGetBuilder(string fileName, out VirtualOneBuilder builder)
    {
        builder = _builders.ContainsKey(fileName) ? _builders[fileName] : null;
        return builder != null;
    }

    /// <summary>
    /// Gets a <see cref="VirtualOneBuilder"/> for the given name or creates one if one does not exist.
    /// </summary>
    /// <param name="fileName">The file name of the ONE, including extension. Case insensitive.</param>
    public VirtualOneBuilder GetBuilder(string fileName)
    {
        if (!_builders.ContainsKey(fileName)) 
            _builders[fileName] = new VirtualOneBuilder();

        return _builders[fileName];
    }
}