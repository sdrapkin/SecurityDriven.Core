using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SecurityDriven.Core
{
	/// <summary>Helpful utility methods.</summary>
	public static class Utils
	{
		#region public
		/// <summary>Returns byte-size of struct T.</summary>
		/// <typeparam name="T">struct T</typeparam>
		public static class StructSizer<T> where T : struct
		{
			/// <summary>Returns byte-size of struct T.</summary>
			public static readonly int Size = Unsafe.SizeOf<T>();
		}//class StructSizer<T>

		/// <summary>Casts unmanaged struct T as equivalent Span&lt;byte&gt;.</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="struct"></param>
		/// <returns>Casts unmanaged struct T as equivalent Span&lt;byte&gt;.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<byte> AsSpan<T>(ref T @struct) where T : unmanaged
		{
			return MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref @struct), StructSizer<T>.Size);
		}//AsSpan<T>

		/// <summary>Casts Span&lt;byte&gt; as equivalent unmanaged struct T.</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="span"></param>
		/// <returns>Casts Span&lt;byte&gt; as equivalent unmanaged struct T.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AsStruct<T>(Span<byte> span) where T : unmanaged
		{
			if (StructSizer<T>.Size > span.Length)
				AsStruct_Throw_ArgumentOutOfRangeException<T>();

			return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
		}//AsStruct<T> for Span

		/// <summary>Casts ReadOnlySpan&lt;byte&gt; as equivalent unmanaged struct T.</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="span"></param>
		/// <returns>Casts ReadOnlySpan&lt;byte&gt; as equivalent unmanaged struct T.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AsStruct<T>(ReadOnlySpan<byte> span) where T : unmanaged
		{
			if (StructSizer<T>.Size > span.Length)
				AsStruct_Throw_ArgumentOutOfRangeException<T>();

			return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
		}//AsStruct<T> for ReadOnlySpan
		#endregion

		#region non-public
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void AsStruct_Throw_ArgumentOutOfRangeException<T>() =>
			  throw new ArgumentOutOfRangeException(paramName: "span", message: typeof(T).FullName + " is larger than span.Length.");
		#endregion

		#region LongStruct
		[StructLayout(LayoutKind.Explicit, Pack = 1)]
		internal struct LongStruct
		{
			[FieldOffset(0)]
			public long LongValue;
			[FieldOffset(0)]
			public ulong UlongValue;

			[FieldOffset(0)]
			public byte B1;
			[FieldOffset(1)]
			public byte B2;
			[FieldOffset(2)]
			public byte B3;
			[FieldOffset(3)]
			public byte B4;
			[FieldOffset(4)]
			public byte B5;
			[FieldOffset(5)]
			public byte B6;
			[FieldOffset(6)]
			public byte B7;
			[FieldOffset(7)]
			public byte B8;
		}// LongStruct
		#endregion LongStruct

		#region IntStruct
		[StructLayout(LayoutKind.Explicit, Pack = 1)]
		internal struct IntStruct
		{
			[FieldOffset(0)]
			public int IntValue;
			[FieldOffset(0)]
			public uint UintValue;

			[FieldOffset(0)]
			public byte B1;
			[FieldOffset(1)]
			public byte B2;
			[FieldOffset(2)]
			public byte B3;
			[FieldOffset(3)]
			public byte B4;
		}// IntStruct
		#endregion IntStruct
	}//class Utils
}//ns
