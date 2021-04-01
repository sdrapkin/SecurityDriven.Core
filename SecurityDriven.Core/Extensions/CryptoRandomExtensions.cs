using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SecurityDriven.Core.Extensions
{
	/// <summary>CryptoRandom extension methods.</summary>
	public static class CryptoRandomExtensions
	{
		/// <summary>Fills an unmanaged struct with cryptographically strong random bytes.</summary>
		/// <typeparam name="T"></typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void FillStruct<T>(this CryptoRandom cryptoRandom, ref T structure) where T : unmanaged
		{
			cryptoRandom.NextBytes(MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref structure), Utils.StructSizer<T>.Size));
		}//FillStruct<T>

		/// <summary>
		/// Returns new 128-bit random Guid.</summary>
		/// <returns>Guid.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Guid NewRandomGuid(this CryptoRandom cryptoRandom)
		{
			Span<byte> guidSpan = stackalloc byte[16];
			cryptoRandom.NextBytes(guidSpan);
			return Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(guidSpan));
		}//NewRandomGuid()

		/// <summary>
		/// Returns new Guid well-suited to be used as a SQL-Server clustered key.
		/// Guid structure is [8 random bytes][8 bytes of SQL-Server-ordered DateTime.UtcNow].
		/// Each Guid should be sequential within 100-nanoseconds UtcNow precision limits.
		/// 64-bit cryptographic strength provides reasonable unguessability and protection against online brute-force attacks.
		/// </summary>
		/// <returns>Guid for SQL-Server clustered key.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Guid NewSqlServerGuid(this CryptoRandom cryptoRandom)
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
		}//NewSqlServerGuid()
	}//class CryptoRandomExtensions
}//ns
