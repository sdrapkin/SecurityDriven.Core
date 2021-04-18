using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SecurityDriven.Core.Extensions
{
	/// <summary>CryptoRandom extension methods.</summary>
	public static class CryptoRandomExtensions
	{
		/// <summary>Fills an unmanaged <paramref name="struct"/> with cryptographically strong random bytes.</summary>
		/// <typeparam name="T"></typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Random<T>(this CryptoRandom cryptoRandom, ref T @struct) where T : unmanaged
		{
			cryptoRandom.NextBytes(MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref @struct), Utils.StructSizer<T>.Size));
		}//Random<T>(ref T)

		/// <summary>
		/// Returns random struct T.</summary>
		/// <returns>Random struct T.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Random<T>(this CryptoRandom cryptoRandom) where T : unmanaged
		{
			Span<byte> span = stackalloc byte[Utils.StructSizer<T>.Size];
			cryptoRandom.NextBytes(span);
			return Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
		}//T Random<T>()
		
		/// <summary>
		/// Returns new 128-bit random Guid.</summary>
		/// <returns>Guid.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Guid RandomGuid(this CryptoRandom cryptoRandom)
		{
			Span<byte> guidSpan = stackalloc byte[16];
			cryptoRandom.NextBytes(guidSpan);
			return Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(guidSpan));
		}//RandomGuid()

		/// <summary>
		/// Returns new Guid well-suited to be used as a SQL-Server clustered key.
		/// Guid structure is [8 random bytes][8 bytes of SQL-Server-ordered DateTime.UtcNow].
		/// Each Guid should be sequential within 100-nanoseconds UtcNow precision limits.
		/// 64-bit cryptographic strength provides reasonable unguessability and protection against online brute-force attacks.
		/// </summary>
		/// <returns>Guid for SQL-Server clustered key.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Guid SqlServerGuid(this CryptoRandom cryptoRandom)
		{
			Span<byte> guidSpan = stackalloc byte[16];
			cryptoRandom.NextBytes(guidSpan.Slice(0, 8));

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
	}//class CryptoRandomExtensions
}//ns
