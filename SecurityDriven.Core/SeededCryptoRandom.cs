using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SecurityDriven.Core
{
	internal sealed class SeededCryptoRandom : CryptoRandom.CryptoRandomBase
	{
		public const int SEEDKEY_SIZE = 32;
		public const int BUFFER_SIZE = SEEDKEY_SIZE + (1024 * 8 - SEEDKEY_SIZE); // must be a multiple of AES_BLOCK_SIZE, and greater than SEEDKEY_SIZE
		const int AES_BLOCK_SIZE = 16;

		static readonly byte[] s_ptBuffer;
		static readonly Aes s_aes;

		readonly byte[] _aeskey;
		readonly byte[] _ctBuffer;
		int _ctIndex;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static SeededCryptoRandom()
		{
			var ptBuffer = new byte[BUFFER_SIZE];

			// Create nonces for s_ptBuffer. Ensure consistent endianness.
			if (BitConverter.IsLittleEndian)
			{
				for (uint i = 1; i < (BUFFER_SIZE / AES_BLOCK_SIZE); ++i)
				{
					Unsafe.WriteUnaligned<uint>(ref ptBuffer[i * AES_BLOCK_SIZE + (AES_BLOCK_SIZE - sizeof(uint))], BinaryPrimitives.ReverseEndianness(i));
				}
			}
			else
			{
				for (uint i = 1; i < (BUFFER_SIZE / AES_BLOCK_SIZE); ++i)
				{
					Unsafe.WriteUnaligned<uint>(ref ptBuffer[i * AES_BLOCK_SIZE + (AES_BLOCK_SIZE - sizeof(uint))], i);
				}
			}
			s_ptBuffer = ptBuffer;

			var aes = Aes.Create();
			aes.Dispose(); // just to make a point that Aes itself does not hold onto any resources, and still serves its purpose after disposal
			aes.Mode = CipherMode.ECB;
			aes.Padding = PaddingMode.None;
			s_aes = aes;
		}//static ctor

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SeededCryptoRandom(ReadOnlySpan<byte> seedKey)
		{
			if (seedKey.Length != SEEDKEY_SIZE)
				Action_Throw_SeedKeyOutOfRangeException();

			_aeskey = new byte[SEEDKEY_SIZE];
			_ctBuffer = GC.AllocateUninitializedArray<byte>(BUFFER_SIZE);

			Reseed(seedKey);
			NextBytes(Span<byte>.Empty); // trigger first _ctBuffer generation
		}//ctor

		[DoesNotReturn]
		static void Action_Throw_SeedKeyOutOfRangeException() =>
			throw new ArgumentOutOfRangeException(paramName: "seedKey", message: "Seed must be " + SEEDKEY_SIZE + " bytes long.");

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void Reseed(ReadOnlySpan<byte> seedKey)
		{
			if (seedKey.Length != SEEDKEY_SIZE)
				Action_Throw_SeedKeyOutOfRangeException();

			lock (_ctBuffer)
			{
				seedKey.CopyTo(_aeskey);
				_ctIndex = BUFFER_SIZE;
			}
		}//Reseed(seedKey)

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void NextBytes(byte[] buffer) => NextBytes((Span<byte>)buffer);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void NextBytes(Span<byte> buffer)
		{
			int bufferLength = buffer.Length;
			int requestedBytesRemaining = bufferLength;
			byte[] ctBuffer = _ctBuffer;
			byte[] aeskey = _aeskey;

			lock (ctBuffer)
			{
				int ctIndex = _ctIndex;
				while (true)
				{
					if (requestedBytesRemaining == 0) break;

					int bytesAvailable = BUFFER_SIZE - ctIndex;
					if (bytesAvailable == 0)
					{
						fnLocalReseed(aeskey, ctBuffer);
						ctIndex = SEEDKEY_SIZE;
						bytesAvailable = BUFFER_SIZE - SEEDKEY_SIZE;
					}

					if (requestedBytesRemaining < bytesAvailable)
						bytesAvailable = requestedBytesRemaining;

					Unsafe.CopyBlockUnaligned(destination: ref buffer[bufferLength - requestedBytesRemaining], source: ref ctBuffer[ctIndex], byteCount: (uint)bytesAvailable);
					requestedBytesRemaining -= bytesAvailable;
					ctIndex += bytesAvailable;
				}//while
				_ctIndex = ctIndex;
			}//lock

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static void fnLocalReseed(byte[] aeskey, byte[] ctBuffer)
			{
				var encryptor = s_aes.CreateEncryptor(rgbKey: aeskey, rgbIV: null);
				encryptor.TransformBlock(inputBuffer: s_ptBuffer, inputOffset: 0, inputCount: BUFFER_SIZE, outputBuffer: ctBuffer, outputOffset: 0);
				encryptor.Dispose();
				Unsafe.CopyBlockUnaligned(destination: ref MemoryMarshal.GetArrayDataReference(aeskey), source: ref MemoryMarshal.GetArrayDataReference(ctBuffer), byteCount: SEEDKEY_SIZE);
			}//fnLocalReseed()
		}//NextBytes(Span<byte>)

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Guid NextGuid()
		{
			Guid guid = default;
			Span<byte> guidSpan = MemoryMarshal.CreateSpan(ref Unsafe.As<Guid, byte>(ref guid), 16);
			this.NextBytes(guidSpan);
			return guid;
		}//NextGuid()

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int Next()
		{
			int temp = default, result;
			Span<byte> span4 = MemoryMarshal.CreateSpan(ref Unsafe.As<int, byte>(ref temp), sizeof(int));
			do
			{
				this.NextBytes(span4);
				result = temp & 0x7FFF_FFFF; // Mask away the sign bit
			} while (result == int.MaxValue); // the range must be [0, int.MaxValue)
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int Next(int maxValue)
		{
			return this.Next(0, maxValue);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int Next(int minValue, int maxValue)
		{
			if (minValue == maxValue)
			{
				this.NextBytes(stackalloc byte[sizeof(uint)]);
				return minValue;
			}
			// The total possible range is [0, 4,294,967,295). Subtract 1 to account for zero being an actual possibility.
			uint range = (uint)maxValue - (uint)minValue - 1;

			// If there is only one possible choice, nothing random will actually happen, so return the only possibility.
			if (range == 0) return minValue;

			// Create a mask for the bits that we care about for the range. The other bits will be masked away.
			uint mask = range;
			mask |= mask >> 01;
			mask |= mask >> 02;
			mask |= mask >> 04;
			mask |= mask >> 08;
			mask |= mask >> 16;

			uint temp = default, result;
			Span<byte> span4 = MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref temp), sizeof(uint));
			do
			{
				this.NextBytes(span4);
				result = temp & mask;
			} while (result > range);
			return minValue + (int)result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override long NextInt64()
		{
			long temp = default, result;
			Span<byte> span8 = MemoryMarshal.CreateSpan(ref Unsafe.As<long, byte>(ref temp), sizeof(long));
			do
			{
				this.NextBytes(span8);
				result = temp & 0x7FFF_FFFF_FFFF_FFFF; // Mask away the sign bit
			} while (result == long.MaxValue); // the range must be [0, int.MaxValue)
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override long NextInt64(long maxValue)
		{
			return this.NextInt64(0, maxValue);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override long NextInt64(long minValue, long maxValue)
		{
			if (minValue == maxValue) return minValue;

			// The total possible range is [0, 18,446,744,073,709,551,615). Subtract 1 to account for zero being an actual possibility.
			ulong range = (ulong)maxValue - (ulong)minValue - 1;

			// If there is only one possible choice, nothing random will actually happen, so return the only possibility.
			if (range == 0) return minValue;

			// Create a mask for the bits that we care about for the range. The other bits will be masked away.
			ulong mask = range;
			mask |= mask >> 01;
			mask |= mask >> 02;
			mask |= mask >> 04;
			mask |= mask >> 08;
			mask |= mask >> 16;
			mask |= mask >> 32;

			ulong temp = default, result;
			Span<byte> span8 = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref temp), sizeof(ulong));
			do
			{
				this.NextBytes(span8);
				result = temp & mask;
			} while (result > range);
			return minValue + (long)result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override double NextDouble()
		{
			const double ONE_OVER_MAX = 1.0D / (1UL << (64 - 11)); // https://en.wikipedia.org/wiki/Double-precision_floating-point_format

			long tempLong = default;
			this.NextBytes(MemoryMarshal.CreateSpan(ref Unsafe.As<long, byte>(ref tempLong), sizeof(long)));
			return ((ulong)tempLong >> 11) * ONE_OVER_MAX;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override float NextSingle()
		{
			const float ONE_OVER_MAX = 1.0F / (1U << (32 - 8)); // https://en.wikipedia.org/wiki/Single-precision_floating-point_format

			int tempInt = default;
			this.NextBytes(MemoryMarshal.CreateSpan(ref Unsafe.As<int, byte>(ref tempInt), sizeof(int)));
			return ((uint)tempInt >> 8) * ONE_OVER_MAX;
		}
	}//class SeededCryptoRandom
	 //#endif
}//ns
