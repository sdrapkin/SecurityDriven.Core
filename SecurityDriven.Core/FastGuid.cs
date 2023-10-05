using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SecurityDriven.Core
{
	/// <summary>Represents a globally unique identifier (GUID).</summary>
	public static class FastGuid
	{
		// Copyright (c) 2023 Stan Drapkin
		// LICENSE: https://github.com/sdrapkin/SecurityDriven.FastGuid

		const int GUIDS_PER_THREAD = 1 << 8; // 256 (keep it power-of-2)
		const int GUID_SIZE_IN_BYTES = 16;

		[StructLayout(LayoutKind.Sequential, Size = GUIDS_PER_THREAD * GUID_SIZE_IN_BYTES, Pack = 1)]
		struct Guids
		{
			Guid guid0;

			public Span<Guid> AsSpanGuid() => MemoryMarshal.CreateSpan(ref guid0, GUIDS_PER_THREAD);
		}//Guids

		struct Container
		{
			public Guids _guids; // do not move, should be 1st
			public byte _idx; // wraps around on 256 (GUIDS_PER_THREAD)
		}//Container

		[ThreadStatic] static Container ts_container; //ts stands for "ThreadStatic"

		/// <summary>Initializes a new instance of the <see cref="Guid"/> structure.</summary>
		/// <returns>A new <see cref="Guid"/> struct.</returns>
		/// <remarks>Faster alternative to <see cref="Guid.NewGuid"/>.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Guid NewGuid()
		{
			ref Container container = ref ts_container;
			byte idx = container._idx++;
			if (idx == 0)
			{
				RandomNumberGenerator.Fill(
					MemoryMarshal.CreateSpan<byte>(ref Unsafe.As<Container, byte>(ref container), GUIDS_PER_THREAD * GUID_SIZE_IN_BYTES));
			}
			Span<Guid> span = container._guids.AsSpanGuid();
			Guid guid = span[idx];
			span[idx] = default;
			return guid;
		}//NewGuid()
	}//class FastGuid
}//ns