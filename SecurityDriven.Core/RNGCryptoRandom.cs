using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SecurityDriven.Core
{
	internal sealed class RNGCryptoRandom : CryptoRandom.CryptoRandomBase
	{
		//references: https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System Random*.cs
		//references: https://source.dot.net/#System.Private.CoreLib Random*.cs 

		const int ULONGS_PER_THREAD = 512; //keep it power-of-2

		/// <summary>Per-processor byte cache size.</summary>
		public const int BYTE_CACHE_SIZE = ULONGS_PER_THREAD * sizeof(ulong); // 4k buffer seems to work best (empirical experimentation).
		/// <summary>Requests larger than this limit will bypass the cache.</summary>
		public const int REQUEST_CACHE_LIMIT = BYTE_CACHE_SIZE / 4; // Must be less than BYTE_CACHE_SIZE.

		[ThreadStatic] static Container ts_data;

		internal static Container LocalContainer
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ts_data ??= new();
		}

		internal sealed class Container
		{
			public ulong[] _ulongs = GC.AllocateUninitializedArray<ulong>(ULONGS_PER_THREAD);
			public int _idx;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ulong NextULong()
			{
				ref var ulong0 = ref MemoryMarshal.GetArrayDataReference(_ulongs);
				int idx = _idx++ & (ULONGS_PER_THREAD - 1);
				if (idx == 0)
				{
					RandomNumberGenerator.Fill(
						MemoryMarshal.CreateSpan<byte>(ref Unsafe.As<ulong, byte>(ref ulong0), ULONGS_PER_THREAD * sizeof(ulong)));
				}
				ulong result = Unsafe.Add(ref ulong0, idx);
				Unsafe.Add(ref ulong0, idx) = 0UL; // prevents leakage
				return result;
			}//NextULong()
		}//class Container

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int Next()
		{
			var container = LocalContainer;
			while (true)
			{
				// Get top 31 bits to get a value in the range [0, int.MaxValue], but try again
				// if the value is actually int.MaxValue, as the method is defined to return a value
				// in the range [0, int.MaxValue).
				ulong result = container.NextULong() >> 33;
				if (result != uint.MaxValue) return (int)result;
			}
		}//Next()

		/// <summary>Returns the integer (ceiling) log of the specified value, base 2.</summary>
		/// <param name="value">The value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int Log2Ceiling(uint value)
		{
			int result = BitOperations.Log2(value);
			if (BitOperations.PopCount(value) != 1)
			{
				result++;
			}
			return result;
		}//Log2Ceiling(uint value)

		/// <summary>Returns the integer (ceiling) log of the specified value, base 2.</summary>
		/// <param name="value">The value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int Log2Ceiling(ulong value)
		{
			int result = BitOperations.Log2(value);
			if (BitOperations.PopCount(value) != 1)
			{
				result++;
			}
			return result;
		}//Log2Ceiling(ulong value)


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int Next(int maxValue)
		{
			if (maxValue > 1)
			{
				// Narrow down to the smallest range [0, 2^bits] that contains maxValue.
				// Then repeatedly generate a value in that outer range until we get one within the inner range.
				int bits = Log2Ceiling((uint)maxValue);
				var container = LocalContainer;
				while (true)
				{
					ulong result = container.NextULong() >> (sizeof(ulong) * 8 - bits);
					if (result < (uint)maxValue)
					{
						return (int)result;
					}
				}
			}

			Debug.Assert(maxValue == 0 || maxValue == 1);
			return 0;
		}//Next(maxValue)

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int Next(int minValue, int maxValue)
		{
			ulong exclusiveRange = (ulong)((long)maxValue - minValue);

			if (exclusiveRange > 1)
			{
				// Narrow down to the smallest range [0, 2^bits] that contains maxValue.
				// Then repeatedly generate a value in that outer range until we get one within the inner range.
				int bits = Log2Ceiling(exclusiveRange);
				var container = LocalContainer;
				while (true)
				{
					ulong result = container.NextULong() >> (sizeof(ulong) * 8 - bits);
					if (result < exclusiveRange)
					{
						return (int)result + minValue;
					}
				}
			}

			Debug.Assert(minValue == maxValue || minValue + 1 == maxValue);
			return minValue;
		}//Next(minValue, maxValue)

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override long NextInt64()
		{
			var container = LocalContainer;
			while (true)
			{
				// Get top 63 bits to get a value in the range [0, long.MaxValue], but try again
				// if the value is actually long.MaxValue, as the method is defined to return a value
				// in the range [0, long.MaxValue).
				ulong result = container.NextULong() >> 1;
				if (result != long.MaxValue) return (long)result;
			}
		}//NextInt64()

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override long NextInt64(long maxValue)
		{
			if (maxValue > 1)
			{
				// Narrow down to the smallest range [0, 2^bits] that contains maxValue.
				// Then repeatedly generate a value in that outer range until we get one within the inner range.
				int bits = Log2Ceiling((ulong)maxValue);
				var container = LocalContainer;
				while (true)
				{
					ulong result = container.NextULong() >> (sizeof(ulong) * 8 - bits);
					if (result < (ulong)maxValue)
					{
						return (long)result;
					}
				}
			}

			Debug.Assert(maxValue == 0 || maxValue == 1);
			return 0;
		}//NextInt64(maxValue)

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override long NextInt64(long minValue, long maxValue)
		{
			ulong exclusiveRange = (ulong)(maxValue - minValue);

			if (exclusiveRange > 1)
			{
				// Narrow down to the smallest range [0, 2^bits] that contains maxValue.
				// Then repeatedly generate a value in that outer range until we get one within the inner range.
				int bits = Log2Ceiling(exclusiveRange);
				var container = LocalContainer;
				while (true)
				{
					ulong result = container.NextULong() >> (sizeof(ulong) * 8 - bits);
					if (result < exclusiveRange)
					{
						return (long)result + minValue;
					}
				}
			}

			Debug.Assert(minValue == maxValue || minValue + 1 == maxValue);
			return minValue;
		}//NextInt64(minValue, maxValue)

		/*
			As described in http://prng.di.unimi.it/:
			"A standard double (64-bit) floating-point number in IEEE floating point format has 52 bits of significand,
			plus an implicit bit at the left of the significand. Thus, the representation can actually store numbers with
			53 significant binary digits.Because of this fact, in C99 a 64-bit unsigned integer x should be converted to
			a 64-bit double using the expression
			(x >> 11) * 0x1.0p-53"
		*/
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override double NextDouble() => (LocalContainer.NextULong() >> 11) * (1.0 / (1ul << 53));

		// Same as above, but with 24 bits instead of 53.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override float NextSingle() => (LocalContainer.NextULong() >> 40) * (1.0f / (1u << 24));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void NextBytes(byte[] buffer) => NextBytes((Span<byte>)buffer);

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
			if (count == 0) return;

			int ulongsNeeded = ((count - 1) >> 3) + 1;
			Container container = LocalContainer;
			ref ulong ulong0 = ref MemoryMarshal.GetArrayDataReference(container._ulongs);

			int idx = container._idx & (ULONGS_PER_THREAD - 1);
			if (idx == 0 || (idx > (ULONGS_PER_THREAD - ulongsNeeded)))
			{
				RandomNumberGenerator.Fill(
					MemoryMarshal.CreateSpan<byte>(ref Unsafe.As<ulong, byte>(ref ulong0), ULONGS_PER_THREAD * sizeof(ulong)));
				idx = 0;
			}

			container._idx = idx + ulongsNeeded;

			ref byte byteCacheLocalStart = ref Unsafe.As<ulong, byte>(ref Unsafe.Add(ref ulong0, idx));
			Unsafe.CopyBlockUnaligned(destination: ref MemoryMarshal.GetReference(buffer), source: ref byteCacheLocalStart, byteCount: (uint)count);
			Unsafe.InitBlockUnaligned(startAddress: ref byteCacheLocalStart, value: 0, byteCount: (uint)(ulongsNeeded * sizeof(ulong)));
		}

		[DoesNotReturn]
		public override void Reseed(ReadOnlySpan<byte> seedKey) =>
			throw new NotImplementedException(message: "Reseed is only implemented for seeded construction of " + nameof(CryptoRandom) + ".");

	}//class RNGCryptoRandom
}//ns
