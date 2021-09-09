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
			_unseeded = new RNGCryptoRandom();
		}//ctor

		/// <summary>Creates a seeded instance of <see cref="CryptoRandom"/> using 32-byte seedKey.</summary>
		/// <param name="seedKey"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CryptoRandom(ReadOnlySpan<byte> seedKey) : base(Seed: int.MinValue)
		{
			_seeded = new SeededCryptoRandom(seedKey);
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
			ref var seedKeyRef = ref MemoryMarshal.GetReference(seedKey);
			Unsafe.InitBlockUnaligned(ref seedKeyRef, 0, SeededCryptoRandom.SEEDKEY_SIZE);

			Unsafe.WriteUnaligned<int>(destination: ref seedKeyRef,
				value: BitConverter.IsLittleEndian ? Seed : BinaryPrimitives.ReverseEndianness(Seed));

			_seeded = new SeededCryptoRandom(seedKey);
		}//ctor int Seed

		/// <summary>Shared instance of <see cref="CryptoRandom"/>.</summary>
#if NET6_0_OR_GREATER
		public static new
#else
		public static
#endif
		CryptoRandom Shared
		{ get; } = new();

		RNGCryptoRandom _unseeded;
		SeededCryptoRandom _seeded;

		// reference: https://github.com/dotnet/runtime/blob/7795971839be34099b07595fdcf47b95f048a730/src/libraries/System.Security.Cryptography.Algorithms/src/System/Security/Cryptography/RandomNumberGenerator.cs#L161
		/// <summary>Creates an array of bytes with a cryptographically strong random sequence of values.</summary>
		/// <param name="count">The number of bytes of random values to create.</param>
		/// <returns>An array populated with cryptographically strong random values.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte[] NextBytes(int count)
		{
			if (count < 0) ThrowNewArgumentOutOfRangeException(nameof(count));
			byte[] bytes = GC.AllocateUninitializedArray<byte>(count);
			if (_unseeded is not null)
				_unseeded.NextBytes((Span<byte>)bytes);
			else _seeded.NextBytes((Span<byte>)bytes);
			return bytes;
		}//NextBytes(count)

		/// <summary>Reseeds a seeded instance of <see cref="CryptoRandom"/>.</summary>
		/// <param name="seedKey"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reseed(ReadOnlySpan<byte> seedKey)
		{
			if (_seeded is not null)
				_seeded.Reseed(seedKey);
			else _unseeded.Reseed(seedKey);
		}//Reseed(seedKey)

		/// <summary>Fills an unmanaged <paramref name="struct"/> with cryptographically strong random bytes.</summary>
		/// <typeparam name="T"></typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Next<T>(ref T @struct) where T : unmanaged
		{
			if (_unseeded is not null)
				_unseeded.NextBytes(MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref @struct), Utils.StructSizer<T>.Size));
			else _seeded.NextBytes(MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref @struct), Utils.StructSizer<T>.Size));
		}//Next<T>(ref T)

		/// <summary>
		/// Returns random struct T.</summary>
		/// <returns>Random struct T.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Next<T>() where T : unmanaged
		{
			T @struct = default;
			Span<byte> span = MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref @struct), Utils.StructSizer<T>.Size);
			if (_unseeded is not null)
				_unseeded.NextBytes(span);
			else _seeded.NextBytes(span);
			return @struct;
		}//T Next<T>()

		/// <summary>
		/// Returns new 128-bit random Guid. Replacement for <see cref="Guid.NewGuid"/>.</summary>
		/// <returns>Guid.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Guid NextGuid() => (_seeded is not null) ? _seeded.NextGuid() : FastGuid.NewGuid();

		/// <summary>
		/// Returns new Guid well-suited to be used as a SQL-Server clustered key.
		/// Guid structure is [8 random bytes][8 bytes of SQL-Server-ordered DateTime.UtcNow].
		/// Each Guid should be sequential within 100-nanosecond UtcNow precision limits.
		/// 64-bit cryptographic strength provides reasonable unguessability and protection against online brute-force attacks.
		/// </summary>
		/// <returns>Guid for SQL-Server clustered key.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Guid SqlServerGuid()
		{
			Guid guid = default;
			ref byte guid0 = ref Unsafe.As<Guid, byte>(ref guid);
			Span<byte> guidSpan = MemoryMarshal.CreateSpan(ref guid0, 16);
			if (_unseeded is not null)
			{
				Unsafe.WriteUnaligned<ulong>(ref guid0, RNGCryptoRandom.LocalContainer.NextULong());
			}
			else _seeded.NextBytes(guidSpan.Slice(0, 8));

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

			return guid;
		}//SqlServerGuid()

		internal abstract class CryptoRandomBase
		{
			public abstract void Reseed(ReadOnlySpan<byte> seedKey);
			public abstract int Next();
			public abstract int Next(int maxValue);
			public abstract int Next(int minValue, int maxValue);
			public abstract long NextInt64();
			public abstract long NextInt64(long maxValue);
			public abstract long NextInt64(long minValue, long maxValue);
			public abstract float NextSingle();
			public abstract double NextDouble();
			public abstract void NextBytes(byte[] buffer);
			public abstract void NextBytes(Span<byte> buffer);
		}//class CryptoRandomBase

	}//class CryptoRandom
}//ns
