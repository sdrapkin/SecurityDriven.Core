using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SecurityDriven.Core
{
	/// <summary>Implements a fast, *thread-safe*, cryptographically-strong random number generator. Inherits from <see cref="System.Random"/>.</summary>
	public partial class CryptoRandom : System.Random
	{
		/// <summary>Initializes a new instance of <see cref="CryptoRandom"/>.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CryptoRandom() : base(Seed: int.MinValue)
		{
			// Minimize the wasted time of calling default System.Random base ctor.
			// We can't avoid calling at least some base ctor, ie. some compute is wasted anyway.
			// That's the price of inheriting from System.Random (doesn't implement an interface).
			_impl = new RNGCryptoRandom();
		}//ctor

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CryptoRandom(ReadOnlySpan<byte> seedKey) : base(Seed: int.MinValue)
		{
			_impl = new SeededCryptoRandom(seedKey);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CryptoRandom(int Seed) : base(Seed: int.MinValue)
		{
			Span<byte> seedKey = stackalloc byte[SeededCryptoRandom.SEEDKEY_SIZE];
			Unsafe.WriteUnaligned<int>(ref seedKey[0],
				BitConverter.IsLittleEndian ? Seed : BinaryPrimitives.ReverseEndianness(Seed));

			_impl = new SeededCryptoRandom(seedKey);
		}

		/// <summary>Shared instance of <see cref="CryptoRandom"/>.</summary>
		public static CryptoRandom Instance { get; private set; }

		[ModuleInitializer]
		internal static void SecurityDrivenCore_ModuleInitializer() => Instance = new();

		readonly CryptoRandomBase _impl;

		// reference: https://github.com/dotnet/runtime/blob/7795971839be34099b07595fdcf47b95f048a730/src/libraries/System.Security.Cryptography.Algorithms/src/System/Security/Cryptography/RandomNumberGenerator.cs#L161
		/// <summary>Creates an array of bytes with a cryptographically strong random sequence of values.</summary>
		/// <param name="count">The number of bytes of random values to create.</param>
		/// <returns>An array populated with cryptographically strong random values.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte[] NextBytes(int count)
		{
			byte[] bytes = GC.AllocateUninitializedArray<byte>(count);
			_impl.NextBytes(new Span<byte>(bytes));
			return bytes;
		}//NextBytes(count)

		#region New System.Random APIs
		/// <summary>Returns a non-negative random integer.</summary>
		/// <returns>A 64-bit signed integer that is greater than or equal to 0 and less than <see cref="long.MaxValue"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public long NextInt64()
		{
			long result;
			Span<byte> span8 = stackalloc byte[sizeof(long)];
			do
			{
				_impl.NextBytes(span8);
				result = Unsafe.As<byte, long>(ref MemoryMarshal.GetReference(span8)) & 0x7FFF_FFFF_FFFF_FFFF; // Mask away the sign bit
			} while (result == long.MaxValue); // the range must be [0, int.MaxValue)
			return result;
		}//NextInt64()

		/// <summary>Returns a non-negative random integer that is less than the specified maximum.</summary>
		/// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue"/> must be greater than or equal to 0.</param>
		/// <returns>
		/// A 64-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue"/>; that is, the range of return values ordinarily
		/// includes 0 but not <paramref name="maxValue"/>. However, if <paramref name="maxValue"/> equals 0, <paramref name="maxValue"/> is returned.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public long NextInt64(long maxValue)
		{
			if (maxValue < 0) ThrowNewArgumentOutOfRangeException(nameof(maxValue));
			return NextInt64(0, maxValue);
		}//NextInt64(maxValue)

		/// <summary>Returns a random integer that is within a specified range.</summary>
		/// <param name="minValue">The inclusive lower bound of the random number returned.</param>
		/// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>.</param>
		/// <returns>
		/// A 64-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>; that is, the range of return values includes <paramref name="minValue"/>
		/// but not <paramref name="maxValue"/>. If minValue equals <paramref name="maxValue"/>, <paramref name="minValue"/> is returned.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public long NextInt64(long minValue, long maxValue)
		{
			if (minValue == maxValue) return minValue;
			if (minValue > maxValue) ThrowNewArgumentOutOfRangeException(nameof(minValue));

			// The total possible range is [0, 18,446,744,073,709,551,615). Subtract 1 to account for zero being an actual possibility.
			ulong range = (ulong)(maxValue - minValue) - 1;

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

			Span<byte> span8 = stackalloc byte[sizeof(ulong)];
			ref ulong result = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(span8));

			do
			{
				_impl.NextBytes(span8);
				result &= mask;
			} while (result > range);
			return minValue + (long)result;
		}//NextInt64(minValue, maxValue)
		#endregion

		public abstract class CryptoRandomBase
		{
			public abstract void NextBytes(Span<byte> buffer);
		}

	}//class CryptoRandom
}//ns
