using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace MerinoClient.Features.Protection.BundleVerifier.RestrictedProcessRunner;

internal class MemoryMapWriterStream : Stream
{
    private readonly MemoryMappedViewAccessor _myView;
    private int _myPosition;

    public MemoryMapWriterStream(MemoryMappedFile mapFile)
    {
        _myView = mapFile.CreateViewAccessor();
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => Position;

    public override long Position
    {
        get => _myPosition;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    internal unsafe byte* GetPointer()
    {
        byte* ptr = null;
        _myView.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        return ptr;
    }

    internal void ReleasePointer()
    {
        _myView.SafeMemoryMappedViewHandle.ReleasePointer();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        _myView.Write(0, (int)value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (count + _myPosition > _myView.Capacity - 8)
            throw new IOException($"Stream is full (capacity {_myView.Capacity}, requires {count + _myPosition}");

        _myView.WriteArray(_myPosition + 8, buffer, offset, count);
        _myPosition += count;
        Interlocked.MemoryBarrier();
        _myView.Write(4, _myPosition);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _myView.Dispose();
    }
}