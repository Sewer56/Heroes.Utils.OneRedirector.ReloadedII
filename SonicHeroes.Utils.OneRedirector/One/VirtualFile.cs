using System;
using System.IO;
using System.Runtime.InteropServices;
using Heroes.SDK;

namespace SonicHeroes.Utils.OneRedirector.One
{
    public class VirtualFile
    {
        /// <summary>
        /// Path to the file on the hard disk.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// True if the file is compressed, else false.
        /// </summary>
        public bool IsCompressed { get; private set; }

        public VirtualFile(string filePath, bool isCompressed)
        {
            FilePath = filePath;
            IsCompressed = isCompressed;
        }

        /// <summary>
        /// Reads the file from the hard disk.
        /// </summary>
        public byte[] GetData() => File.ReadAllBytes(FilePath);

        /// <summary>
        /// Reads the file from the hard disk and compresses it if necessary.
        /// </summary>
        /// <param name="searchBufferSize">Size of the search buffer used for compression, between 1 - 8191</param>
        public byte[] GetDataCompressed(int searchBufferSize = 256)
        {
            var data = GetData();
            return !IsCompressed ? SDK.Prs.Compress(data, searchBufferSize) : data;
        }
    }
}
