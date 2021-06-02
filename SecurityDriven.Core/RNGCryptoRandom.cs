using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

namespace SecurityDriven.Core
{
    internal sealed class RNGCryptoRandom : CryptoRandom.CryptoRandomBase
    {
        //references: https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System Random*.cs
        //references: https://source.dot.net/#System.Private.CoreLib Random*.cs

        private const int CACHE_LINE = 64; // cache-line is assumed to be 64 bytes

        /// <summary>Per-processor byte cache size.</summary>
        public const int BYTE_CACHE_SIZE = 4096; // 4k buffer seems to work best (empirical experimentation).

        /// <summary>Requests larger than this limit will bypass the cache.</summary>
        public const int REQUEST_CACHE_LIMIT = BYTE_CACHE_SIZE / 4; // Must be less than BYTE_CACHE_SIZE.

        private readonly ByteCache[] _byteCaches = new ByteCache[Environment.ProcessorCount];

        private sealed class ByteCache
        {
            public byte[] Bytes = GC.AllocateUninitializedArray<byte>(BYTE_CACHE_SIZE + CACHE_LINE);
            public int Position { get; private set; } = BYTE_CACHE_SIZE;

            /// <summary>
            /// Increases the position by the given amount and returns the new position.
            /// </summary>
            /// <param name="amount"></param>
            /// <returns>The new position.</returns>
            public int AdvancePosition(int amount)
            {
                int position = Position;
                Position = position + amount;
                return position;
            }

            public void ResetPosition()
            {
                Position = 0;
            }

            /// <summary>
            /// Determines if the cache contains the given number of items or more.
            /// </summary>
            /// <param name="numberOfItemsRequired">The number of items needed from the cache.</param>
            /// <returns>True if the cache does not contain the given number of items or more, false otherwise.</returns>
            public bool IsExhausted(int numberOfItemsRequired)
            {
                return NumberOfItemsRemaining < numberOfItemsRequired;
            }

            private int NumberOfItemsRemaining => BYTE_CACHE_SIZE - Position;

            [StructLayout(LayoutKind.Sequential, Size = CACHE_LINE)] private struct PaddingStruct { }
        }// internal class ByteCache

        /// <summary>Fills the elements of a specified span of bytes with random numbers.</summary>
        /// <param name="buffer">The array to be filled with random numbers.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void NextBytes(Span<byte> buffer)
        {
            if (buffer.Length > REQUEST_CACHE_LIMIT)
            {
                RandomNumberGenerator.Fill(buffer);
                return;
            }

            FillBytes(buffer);
        }//NextBytes(Span<byte>)

        /// <summary>
        /// Gets the appropriate cache and fills the buffer from it.
        /// </summary>
        /// <param name="buffer">The array to be filled with random numbers.</param>
        private void FillBytes(Span<byte> buffer)
        {
            ByteCache cache = GetCache();

            bool lockTaken = false;
            try
            {
                Monitor.Enter(cache, ref lockTaken);
                FillBytesFromCache(buffer, cache);
            }
            finally
            {
                if (lockTaken) Monitor.Exit(cache);
            }
        }

        /// <summary>
        /// Fills the cache with random numbers if needed, then copies numbers from the cache into the buffer and clears the used values from the cache.
        /// </summary>
        /// <param name="buffer">The array to be filled with random numbers.</param>
        /// <param name="cache">The cache for holding randomly-generated numbers.</param>
        private static void FillBytesFromCache(Span<byte> buffer, ByteCache cache)
        {
            int cachePosition = PrepareCache(buffer, cache);
            CopyBytesThenReplace(buffer, ref cache.Bytes[cachePosition]);
        }

        /// <summary>
        /// Prepares the cache for filling the given buffer.
        /// </summary>
        /// <param name="buffer">The array to be filled with random numbers.</param>
        /// <param name="cache">The cache for holding randomly-generated numbers.</param>
        /// <returns></returns>
        private static int PrepareCache(Span<byte> buffer, ByteCache cache)
        {
            if (cache.IsExhausted(buffer.Length))
            {
                FillCache(cache);
            }

            // ensure we advance the position before touching any data, in case anything throws
            return cache.AdvancePosition(buffer.Length);
        }

        /// <summary>
        /// Fills the given cache with randomly-generated numbers.
        /// </summary>
        /// <param name="cache">The cache to be filled.</param>
        private static void FillCache(ByteCache cache)
        {
            RandomNumberGenerator.Fill(new Span<byte>(cache.Bytes, 0, BYTE_CACHE_SIZE));
            cache.ResetPosition();
        }

        /// <summary>
        /// Copies bytes from the given start address into the given span and initializes the used bytes to 0.
        /// </summary>
        /// <param name="bytesToFill">The bytes to hold the copied bytes.</param>
        /// <param name="startAddress">The start address to copy bytes from.</param>
        private static void CopyBytesThenReplace(Span<byte> bytesToFill, ref byte startAddress)
        {
            uint numberOfBytesToFill = (uint)bytesToFill.Length;
            Unsafe.CopyBlockUnaligned(destination: ref MemoryMarshal.GetReference(bytesToFill), source: ref startAddress, byteCount: numberOfBytesToFill);
            Unsafe.InitBlockUnaligned(startAddress: ref startAddress, value: 0, byteCount: numberOfBytesToFill);
        }

        private ByteCache GetCache()
        {
            int processorId = GetProcessorId();
            return GetCache(processorId);
        }

        /// <summary>
        /// Gets the cache for the given processor id.
        /// </summary>
        /// <param name="processorId"></param>
        /// <returns></returns>
        private ByteCache GetCache(int processorId)
        {
            ByteCache byteCache = _byteCaches[processorId];
            if (byteCache == null)
            {
                Interlocked.CompareExchange(ref Unsafe.As<ByteCache, object>(ref _byteCaches[processorId]), new ByteCache(), null);
                byteCache = _byteCaches[processorId];
            }

            return byteCache;
        }

        private static int GetProcessorId()
        {
            return Environment.ProcessorCount > 1
                ? Thread.GetCurrentProcessorId()
                : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Reseed(ReadOnlySpan<byte> seedKey) =>
            throw new NotImplementedException(message: "Reseed is only implemented for seeded construction of CryptoRandom.");
    }//class RNGCryptoRandom
}//ns