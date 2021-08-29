using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SecurityDriven.Core
{
	internal static class FastGuid
	{
		const int GUIDS_PER_THREAD = 512; //keep it power-of-2
		[ThreadStatic] static Container ts_data;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static Container CreateContainer() => ts_data = new();

		static Container LocalContainer
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ts_data ?? CreateContainer();
		}

		sealed class Container
		{
			Guid[] _guids = GC.AllocateUninitializedArray<Guid>(GUIDS_PER_THREAD);
			int _idx;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Guid NextGuid()
			{
				var guids = _guids;
				int idx = _idx++ & (GUIDS_PER_THREAD - 1);
				if (idx == 0) RandomNumberGenerator.Fill(MemoryMarshal.Cast<Guid, byte>(guids));

				var guid = guids[idx];
				guids[idx] = default; // prevents Guid leakage
				return guid;
			}//NextGuid()
		}//class Container

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Guid NewGuid() => LocalContainer.NextGuid();
	}//class FastGuid
}//ns