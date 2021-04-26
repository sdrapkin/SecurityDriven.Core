using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SecurityDriven.Core
{
	/// <summary>Implements a fast, *thread-safe*, cryptographically-strong random number generator. Inherits from <see cref="System.Random"/>.</summary>
	public partial class CryptoRandom : System.Random
	{
		#region static Params
		/// <summary>Internal constants for advanced users.</summary>
		public static class Params
		{
			/// <summary><see cref="RNGCryptoRandom"/> constants.</summary>
			public static class RNG
			{
				/// <summary>Per-processor byte cache size.</summary>
				public const int BYTE_CACHE_SIZE = RNGCryptoRandom.BYTE_CACHE_SIZE;
				/// <summary>Requests larger than this limit will bypass the cache.</summary>
				public const int REQUEST_CACHE_LIMIT = RNGCryptoRandom.REQUEST_CACHE_LIMIT;
			}//class RNG

			/// <summary><see cref="SeededCryptoRandom"/> constants.</summary>
			public static class Seeded
			{
				/// <summary>AES key size.</summary>
				public const int SEEDKEY_SIZE = SeededCryptoRandom.SEEDKEY_SIZE;
				/// <summary>Ciphertext buffer size.</summary>
				public const int BUFFER_SIZE = SeededCryptoRandom.BUFFER_SIZE;
			}// class Seeded
		}// static class Params
		#endregion

		/// <summary>Initializes a new instance of <see cref="CryptoRandom"/>.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CryptoRandom() : base(Seed: int.MinValue)
		{
			// Minimize the wasted time of calling default System.Random base ctor.
			// We can't avoid calling at least some base ctor, ie. some compute is wasted anyway.
			// That's the price of inheriting from System.Random (doesn't implement an interface).
			_impl = new RNGCryptoRandom();
		}//ctor

		/// <summary>Creates a seeded instance of <see cref="CryptoRandom"/> using 32-byte seedKey.</summary>
		/// <param name="seedKey"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CryptoRandom(ReadOnlySpan<byte> seedKey) : base(Seed: int.MinValue)
		{
			_impl = new SeededCryptoRandom(seedKey);
		}//ctor seedKey

		/// <summary>
		/// Creates a seeded instance of <see cref="CryptoRandom"/> using an int seed.
		/// <para>OBSOLETE - use only for backwards compatibility with <see cref="System.Random"/>.</para>
		/// <para>Proper seeded <see cref="CryptoRandom"/> constructor takes a 32-byte seedKey.</para>
		/// </summary>
		/// <param name="Seed"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CryptoRandom(int Seed) : base(Seed: int.MinValue)
		{
			Span<byte> seedKey = stackalloc byte[SeededCryptoRandom.SEEDKEY_SIZE];
			Unsafe.WriteUnaligned<int>(destination: ref seedKey[0],
				value: BitConverter.IsLittleEndian ? Seed : BinaryPrimitives.ReverseEndianness(Seed));

			_impl = new SeededCryptoRandom(seedKey);
		}//ctor int Seed

		/// <summary>Shared instance of <see cref="CryptoRandom"/>.</summary>
		public static CryptoRandom Shared { get; } = new();

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

		/// <summary>Reseeds a seeded instance of <see cref="CryptoRandom"/>.</summary>
		/// <param name="seedKey"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reseed(ReadOnlySpan<byte> seedKey) => _impl.Reseed(seedKey);

		/// <summary>Fills an unmanaged <paramref name="struct"/> with cryptographically strong random bytes.</summary>
		/// <typeparam name="T"></typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Next<T>(ref T @struct) where T : unmanaged
		{
			_impl.NextBytes(MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref @struct), Utils.StructSizer<T>.Size));
		}//Next<T>(ref T)

		/// <summary>
		/// Returns random struct T.</summary>
		/// <returns>Random struct T.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Next<T>() where T : unmanaged
		{
			Span<byte> span = stackalloc byte[Utils.StructSizer<T>.Size];
			_impl.NextBytes(span);
			return Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
		}//T Next<T>()

		/// <summary>
		/// Returns new 128-bit random Guid. Replacement for <see cref="Guid.NewGuid"/>.</summary>
		/// <returns>Guid.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Guid NextGuid()
		{
			Span<byte> guidSpan = stackalloc byte[16];
			_impl.NextBytes(guidSpan);
			return Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(guidSpan));
		}//NextGuid()

		/// <summary>
		/// Returns new Guid well-suited to be used as a SQL-Server clustered key.
		/// Guid structure is [8 random bytes][8 bytes of SQL-Server-ordered DateTime.UtcNow].
		/// Each Guid should be sequential within 100-nanoseconds UtcNow precision limits.
		/// 64-bit cryptographic strength provides reasonable unguessability and protection against online brute-force attacks.
		/// </summary>
		/// <returns>Guid for SQL-Server clustered key.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Guid SqlServerGuid()
		{
			Span<byte> guidSpan = stackalloc byte[16];
			_impl.NextBytes(guidSpan.Slice(0, 8));

			DateTime utcNow = DateTime.UtcNow;
			Span<byte> ticksSpan = MemoryMarshal.CreateSpan(ref Unsafe.As<DateTime, byte>(ref utcNow), 8);

			// based on Microsoft SqlGuid.cs
			// https://github.com/microsoft/referencesource/blob/5697c29004a34d80acdaf5742d7e699022c64ecd/System.Data/System/Data/SQLTypes/SQLGuid.cs

			guidSpan[10] = ticksSpan[7];
			guidSpan[11] = ticksSpan[6];
			guidSpan[12] = ticksSpan[5];
			guidSpan[13] = ticksSpan[4];
			guidSpan[14] = ticksSpan[3];
			guidSpan[15] = ticksSpan[2];

			guidSpan[08] = ticksSpan[1];
			guidSpan[09] = ticksSpan[0];

			return Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(guidSpan));
		}//SqlServerGuid()

		#endregion

		internal abstract class CryptoRandomBase
		{
			public abstract void NextBytes(Span<byte> buffer);
			public abstract void Reseed(ReadOnlySpan<byte> seedKey);
		}//class CryptoRandomBase

	}//class CryptoRandom
}//ns
