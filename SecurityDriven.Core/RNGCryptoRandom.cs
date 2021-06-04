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

		const int CACHE_LINE = 64; // cache-line is assumed to be 64 bytes

		/// <summary>Per-processor byte cache size.</summary>
		public const int BYTE_CACHE_SIZE = 4096; // 4k buffer seems to work best (empirical experimentation).
		/// <summary>Requests larger than this limit will bypass the cache.</summary>
		public const int REQUEST_CACHE_LIMIT = BYTE_CACHE_SIZE / 4; // Must be less than BYTE_CACHE_SIZE.

		readonly ByteCache[] _byteCaches = new ByteCache[Environment.ProcessorCount];

		sealed class ByteCache
		{
			public byte[] Bytes = GC.AllocateUninitializedArray<byte>(BYTE_CACHE_SIZE + CACHE_LINE);
			public int Position = BYTE_CACHE_SIZE;

			[StructLayout(LayoutKind.Sequential, Size = CACHE_LINE)] struct PaddingStruct { }
#pragma warning disable 0169 // field is never used
			readonly PaddingStruct paddingToAvoidFalseSharing;
#pragma warning restore 0169
		}// internal class ByteCache

		/// <summary>Fills the elements of a specified span of bytes with random numbers.</summary>
		/// <param name="buffer">The array to be filled with random numbers.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void NextBytes(Span<byte> buffer)
		{
			int count = buffer.Length;
			if (count > REQUEST_CACHE_LIMIT)
			{
				RandomNumberGenerator.Fill(buffer);
				return;
			}

			ByteCache[] byteCaches = _byteCaches;
			int procId = Thread.GetCurrentProcessorId() % Environment.ProcessorCount;

			ByteCache byteCacheLocal = byteCaches[procId];
			if (byteCacheLocal == null)
			{
				Interlocked.CompareExchange(ref Unsafe.As<ByteCache, object>(ref byteCaches[procId]), new ByteCache(), null);
				byteCacheLocal = byteCaches[procId];
			}

			bool lockTaken = false;
			try
			{
				Monitor.Enter(byteCacheLocal, ref lockTaken);

				byte[] byteCacheBytesLocal = byteCacheLocal.Bytes;
				int byteCachePositionLocal = byteCacheLocal.Position;

				if (byteCachePositionLocal + count > BYTE_CACHE_SIZE)
				{
					RandomNumberGenerator.Fill(new Span<byte>(byteCacheBytesLocal, 0, BYTE_CACHE_SIZE));
					byteCachePositionLocal = 0;
				}

				byteCacheLocal.Position = byteCachePositionLocal + count; // ensure we advance the position before touching any data, in case anything throws

				ref byte byteCacheLocalStart = ref byteCacheBytesLocal[byteCachePositionLocal];
				Unsafe.CopyBlockUnaligned(destination: ref MemoryMarshal.GetReference(buffer), source: ref byteCacheLocalStart, byteCount: (uint)count);
				Unsafe.InitBlockUnaligned(startAddress: ref byteCacheLocalStart, value: 0, byteCount: (uint)count);
			}
			finally
			{
				if (lockTaken) Monitor.Exit(byteCacheLocal);
			}
		}//NextBytes(Span<byte>)

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void Reseed(ReadOnlySpan<byte> seedKey) =>
			throw new NotImplementedException(message: "Reseed is only implemented for seeded construction of CryptoRandom.");

	}//class RNGCryptoRandom
}//ns
