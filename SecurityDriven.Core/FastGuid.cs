using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SecurityDriven.Core
{
	/// <summary>Represents a globally unique identifier (GUID).</summary>
	internal static class FastGuid
	{
		// Copyright (c) 2022 Stan Drapkin
		// LICENSE: https://github.com/sdrapkin/SecurityDriven.FastGuid

		const int GUIDS_PER_THREAD = 256; //keep it power-of-2
		const int GUID_SIZE_IN_BYTES = 16;

		struct Container
		{
			public Guid[] _guids;
			public int _idx;
		}

		[ThreadStatic] static Container ts_data;

		/// <summary>
		/// Initializes a new instance of the <see cref="Guid"/> structure.
		/// </summary>
		/// <returns>A new <see cref="Guid"/> struct.</returns>
		/// <remarks>Faster alternative to <see cref="Guid.NewGuid"/>.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Guid NewGuid()
		{
			ref var guid0 = ref MemoryMarshal.GetArrayDataReference(ts_data._guids ??= GC.AllocateUninitializedArray<Guid>(GUIDS_PER_THREAD));
			int idx = ts_data._idx++ & (GUIDS_PER_THREAD - 1);
			if (idx == 0)
			{
				RandomNumberGenerator.Fill(
					MemoryMarshal.CreateSpan<byte>(ref Unsafe.As<Guid, byte>(ref guid0), GUIDS_PER_THREAD * GUID_SIZE_IN_BYTES));
			}

			var guid = Unsafe.Add(ref guid0, idx);
			Unsafe.Add(ref guid0, idx) = default; // prevents Guid leakage
			return guid;
		}//NewGuid()
	}//class FastGuid
}//ns