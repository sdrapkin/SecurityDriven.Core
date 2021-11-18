using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SecurityDriven.Core
{
	public partial class CryptoRandom
	{
		#region System.Random overrides - old

		/// <summary>Returns a non-negative random integer.</summary>
		/// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than <see cref="int.MaxValue"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int Next() => (_unseeded is not null) ? _unseeded.Next() : _seeded.Next();

		/// <summary>Returns a non-negative random integer that is less than the specified maximum.</summary>
		/// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue"/> must be greater than or equal to 0.</param>
		/// <returns>
		/// A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue"/>; that is, the range of return values ordinarily
		/// includes 0 but not <paramref name="maxValue"/>. However, if <paramref name="maxValue"/> equals 0, <paramref name="maxValue"/> is returned.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int Next(int maxValue)
		{
			if (maxValue < 0) ThrowNewArgumentOutOfRangeException(nameof(maxValue));
			return (_unseeded is RNGCryptoRandom unseeded) ? unseeded.Next(maxValue) : _seeded.Next(maxValue);
		}//Next(maxValue)

		/// <summary>Returns a random integer that is within a specified range.</summary>
		/// <param name="minValue">The inclusive lower bound of the random number returned.</param>
		/// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>.</param>
		/// <returns>
		/// A 32-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>; that is, the range of return values includes <paramref name="minValue"/>
		/// but not <paramref name="maxValue"/>. If minValue equals <paramref name="maxValue"/>, <paramref name="minValue"/> is returned.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int Next(int minValue, int maxValue)
		{
			//if (minValue == maxValue) return minValue;
			if (minValue > maxValue) ThrowNewArgumentOutOfRangeException(nameof(minValue));
			return (_unseeded is RNGCryptoRandom unseeded) ? unseeded.Next(minValue, maxValue) : _seeded.Next(minValue, maxValue);
		}//Next(minValue, maxValue)

		/// <summary>Fills the elements of a specified array of bytes with random numbers.</summary>
		/// <param name="buffer">The array to be filled with random numbers.</param>
		/// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void NextBytes(byte[] buffer)
		{
			if (buffer is null) ThrowNewArgumentNullException(nameof(buffer));
			if (_unseeded is RNGCryptoRandom unseeded)
				unseeded.NextBytes((Span<byte>)buffer);
			else
				_seeded.NextBytes((Span<byte>)buffer);
		}//NextBytes(byte[])

		/// <summary>Fills the elements of a specified span of bytes with random numbers.</summary>
		/// <param name="buffer">The array to be filled with random numbers.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void NextBytes(Span<byte> buffer)
		{
			if (_unseeded is RNGCryptoRandom unseeded)
				unseeded.NextBytes(buffer);
			else
				_seeded.NextBytes(buffer);
		}//NextBytes(Span<byte>)

		/// <summary>Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.</summary>
		/// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override double NextDouble() => (_unseeded is RNGCryptoRandom unseeded) ? unseeded.NextDouble() : _seeded.NextDouble();

		/// <summary>Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.</summary>
		/// <returns>A single-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NET6_0_OR_GREATER
		public override
#else
		public virtual
#endif
		float NextSingle() => (_unseeded is RNGCryptoRandom unseeded) ? unseeded.NextSingle() : _seeded.NextSingle();

		/// <summary>Returns a random floating-point number between 0.0 and 1.0.</summary>
		/// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override double Sample()
		{
			return NextDouble();
		}//Sample()
		#endregion

		#region System.Random overrides - new
		/// <summary>Returns a non-negative random integer.</summary>
		/// <returns>A 64-bit signed integer that is greater than or equal to 0 and less than <see cref="long.MaxValue"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NET6_0_OR_GREATER
		public override
#else
		public
#endif
		long NextInt64() => (_unseeded is RNGCryptoRandom unseeded) ? unseeded.NextInt64() : _seeded.NextInt64();

		/// <summary>Returns a non-negative random integer that is less than the specified maximum.</summary>
		/// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue"/> must be greater than or equal to 0.</param>
		/// <returns>
		/// A 64-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue"/>; that is, the range of return values ordinarily
		/// includes 0 but not <paramref name="maxValue"/>. However, if <paramref name="maxValue"/> equals 0, <paramref name="maxValue"/> is returned.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NET6_0_OR_GREATER
		public override
#else
		public
#endif
		long NextInt64(long maxValue)
		{
			if (maxValue < 0) ThrowNewArgumentOutOfRangeException(nameof(maxValue));
			return (_unseeded is RNGCryptoRandom unseeded) ? unseeded.NextInt64(maxValue) : _seeded.NextInt64(maxValue);
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
#if NET6_0_OR_GREATER
		public override
#else
		public
#endif
		long NextInt64(long minValue, long maxValue)
		{
			if (minValue > maxValue) ThrowNewArgumentOutOfRangeException(nameof(minValue));
			return (_unseeded is RNGCryptoRandom unseeded) ? unseeded.NextInt64(minValue, maxValue) : _seeded.NextInt64(minValue, maxValue);
		}//NextInt64(minValue, maxValue)
		#endregion

		[DoesNotReturn]
		static void ThrowNewArgumentOutOfRangeException(string paramName) => throw new ArgumentOutOfRangeException(paramName: paramName);

		[DoesNotReturn]
		static void ThrowNewArgumentNullException(string paramName) => throw new ArgumentNullException(paramName: paramName);
	}//class CryptoRandom
}//ns
