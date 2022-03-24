using System;
using System.Runtime.InteropServices;

namespace SonicHeroes.Utils.OneRedirector.One;

public unsafe class VirtualOne : IDisposable
{
    /// <summary>
    /// Contains the entire ONE file.
    /// </summary>
    public byte[] File { get; private set; }

    /// <summary>
    /// A pointer to the ONE file.
    /// </summary>
    public byte* FilePtr { get; private set; }

    private readonly GCHandle? _virtualOneHandle;

    /// <summary>
    /// Creates a Virtual ONE given the name of the file and the header of an ONE file.
    /// </summary>
    /// <param name="oneFile">The bytes corresponding to the new ONE header.</param>
    public VirtualOne(byte[] oneFile)
    {
        File = oneFile;
        _virtualOneHandle = GCHandle.Alloc(File, GCHandleType.Pinned);
        FilePtr = (byte*) _virtualOneHandle.Value.AddrOfPinnedObject();
    }

    ~VirtualOne()
    {
        Dispose();
    }

    public void Dispose()
    {
        _virtualOneHandle?.Free();
        GC.SuppressFinalize(this);
    }
}