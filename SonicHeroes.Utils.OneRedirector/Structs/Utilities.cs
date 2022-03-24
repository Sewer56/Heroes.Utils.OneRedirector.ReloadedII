using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SonicHeroes.Utils.OneRedirector.Structs;

internal static class Utilities
{
    /// <summary>
    /// Rounds up a specified number to the next multiple of X.
    /// </summary>
    /// <param name="number">The number to round up.</param>
    /// <param name="multiple">The multiple the number should be rounded to.</param>
    /// <returns></returns>
    public static int RoundUp(int number, int multiple)
    {
        if (multiple == 0)
            return number;

        int remainder = number % multiple;
        if (remainder == 0)
            return number;

        return number + multiple - remainder;
    }

    /// <summary>
    /// Reads an unmanaged, generic type from the stream.
    /// </summary>
    /// <param name="stream">The stream to read the value from.</param>
    /// <param name="value">The value to return.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool TryReadSafe<T>(this Stream stream, out T value) where T : unmanaged
    {
        value = default;
        var valueSpan = new Span<byte>(Unsafe.AsPointer(ref value), sizeof(T));
        return TryReadSafe(stream, valueSpan);
    }

    /// <summary>
    /// Reads a given number of bytes from a stream.
    /// </summary>
    /// <param name="stream">The stream to read the value from.</param>
    /// <param name="result">The buffer to receive the bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool TryReadSafe(this Stream stream, Span<byte> result)
    {
        int numBytesRead = 0;
        int numBytesToRead = result.Length;

        do
        {
            int bytesRead = stream.Read(result.Slice(numBytesRead));
            if (bytesRead <= 0)
                return false;

            numBytesRead += bytesRead;
            numBytesToRead -= bytesRead;
        }
        while (numBytesRead < numBytesToRead);

        return true;
    }
}