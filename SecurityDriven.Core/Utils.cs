using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SecurityDriven.Core
{
	public static class Utils
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<byte> AsSpan<T>(ref T @struct) where T : unmanaged
		{
			return MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref @struct), StructSizer<T>.Size);
		}//AsSpan<T>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AsStruct<T>(Span<byte> span) where T : unmanaged
		{
			if (StructSizer<T>.Size > span.Length)
				AsStruct_Throw_ArgumentOutOfRangeException<T>();

			return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
		}//AsStruct<T> for Span

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AsStruct<T>(ReadOnlySpan<byte> span) where T : unmanaged
		{
			if (StructSizer<T>.Size > span.Length)
				AsStruct_Throw_ArgumentOutOfRangeException<T>();

			return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
		}//AsStruct<T> for ReadOnlySpan

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void AsStruct_Throw_ArgumentOutOfRangeException<T>() =>
			  throw new ArgumentOutOfRangeException("span", typeof(T).FullName + " is larger than span.Length.");

		public static class StructSizer<T> where T : unmanaged
		{
			public static readonly int Size = Unsafe.SizeOf<T>();
		}//class StructSizer<T>
	}//class Utils
}//ns
