#nullable enable

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace MerinoClient.Features.Protection.BundleVerifier;

internal class BundleHashCache : IDisposable
{
    private const int HashSizeBytes = 256 / 8;

    [ThreadStatic] private static byte[]? _ourHashBuffer;

    private readonly ConcurrentDictionary<HashStruct, bool> _myHashes = new();
    private readonly FileStream? _myHashWriterStream;

    public BundleHashCache(string? cacheFile)
    {
        if (cacheFile == null) return;

        _myHashWriterStream = new FileStream(cacheFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        var transferArray = new byte[HashSizeBytes];
        var numHashesNoRead = _myHashWriterStream.Length / HashSizeBytes;
        for (var i = 0; i < numHashesNoRead; i++)
        {
            var readBytes = _myHashWriterStream.Read(transferArray, 0, transferArray.Length);
            if (readBytes != transferArray.Length)
            {
                MerinoLogger.Error($"Failure to read {transferArray.Length} bytes from cached bad URL file");
                _myHashWriterStream.Position = 0;
                break;
            }

            _myHashes[HashStruct.From(transferArray)] = true;
        }
    }

    public void Dispose()
    {
        _myHashWriterStream?.Dispose();
    }

    public bool Contains(string input)
    {
        return _myHashes.ContainsKey(HashStruct.Hash(input));
    }

    public void Add(string input)
    {
        var hash = HashStruct.Hash(input);
        if (!_myHashes.TryAdd(hash, true)) return;

        if (_myHashWriterStream == null) return;

        _ourHashBuffer ??= new byte[HashSizeBytes];
        hash.WriteTo(_ourHashBuffer);

        lock (this)
        {
            _myHashWriterStream.Write(_ourHashBuffer, 0, _ourHashBuffer.Length);
            _myHashWriterStream.Flush(true);
        }
    }

    public void Clear()
    {
        _myHashes.Clear();

        if (_myHashWriterStream == null) return;

        lock (this)
        {
            _myHashWriterStream.Position = 0;
            _myHashWriterStream.SetLength(0);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct HashStruct : IEquatable<HashStruct>
    {
        public readonly long Long1;
        public readonly long Long2;
        public readonly long Long3;
        public readonly long Long4;

        public HashStruct(long long1, long long2, long long3, long long4)
        {
            Long1 = long1;
            Long2 = long2;
            Long3 = long3;
            Long4 = long4;
        }

        public unsafe void WriteTo(byte[] bytes)
        {
            if (bytes.Length < HashSizeBytes)
                throw new ArgumentException($"Buffer too small ({bytes.Length}, need {HashSizeBytes})");

            fixed (byte* ptr = bytes)
            {
                *(HashStruct*)ptr = this;
            }
        }

        public static HashStruct Hash(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Hash(bytes, 0, bytes.Length);
        }

        private static HashStruct Hash(byte[] input, int offset, int length)
        {
            var hash = SHA256.Create();
            var result = hash.ComputeHash(input, offset, length);
            return From(result);
        }

        public static HashStruct From(byte[] bytes, int offset = 0)
        {
            if (offset + HashSizeBytes > bytes.Length)
                throw new IndexOutOfRangeException($"Out of range: {bytes.Length} available, offset={offset}");

            return new HashStruct(BitConverter.ToInt64(bytes, offset), BitConverter.ToInt64(bytes, offset + 8),
                BitConverter.ToInt64(bytes, offset + 16), BitConverter.ToInt64(bytes, offset + 24));
        }

        public bool Equals(HashStruct other)
        {
            return Long1 == other.Long1 && Long2 == other.Long2 && Long3 == other.Long3 && Long4 == other.Long4;
        }

        public override bool Equals(object obj)
        {
            return obj is HashStruct other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Long1.GetHashCode();
                hashCode = (hashCode * 397) ^ Long2.GetHashCode();
                hashCode = (hashCode * 397) ^ Long3.GetHashCode();
                hashCode = (hashCode * 397) ^ Long4.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(HashStruct left, HashStruct right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HashStruct left, HashStruct right)
        {
            return !left.Equals(right);
        }
    }
}