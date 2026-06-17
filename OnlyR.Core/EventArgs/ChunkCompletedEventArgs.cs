using OnlyR.Core.Models;
using System;

namespace OnlyR.Core.EventArgs;

/// <summary>
/// Event args for when a recording chunk has been completed.
/// </summary>
public sealed class ChunkCompletedEventArgs : System.EventArgs
{
    public ChunkCompletedEventArgs(ChunkInfo systemChunk, ChunkInfo micChunk)
    {
        SystemChunk = systemChunk;
        MicChunk = micChunk;
    }

    public ChunkInfo SystemChunk { get; }
    public ChunkInfo MicChunk { get; }
}
