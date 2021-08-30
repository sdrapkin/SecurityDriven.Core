using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SecurityDriven.Core
{
	internal static class FastGuid
	{
		const int GUIDS_PER_THREAD = 512; //keep it power-of-2
		const int GUID_SIZE_IN_BYTES = 16;
		[ThreadStatic] static Container ts_data;

		static Container LocalContainer
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ts_data ??= new();
		}
		sealed class Container
		{
			Guid[] _guids = GC.AllocateUninitializedArray<Guid>(GUIDS_PER_THREAD);
			int _idx;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Guid NextGuid()
			{
				ref var guid0 = ref MemoryMarshal.GetArrayDataReference(_guids);
				int idx = _idx++ & (GUIDS_PER_THREAD - 1);
				if (idx == 0)
				{
					RandomNumberGenerator.Fill(
						MemoryMarshal.CreateSpan<byte>(ref Unsafe.As<Guid, byte>(ref guid0), GUIDS_PER_THREAD * GUID_SIZE_IN_BYTES));
				}

				var guid = Unsafe.Add(ref guid0, idx);
				Unsafe.Add(ref guid0, idx) = default; // prevents Guid leakage
				return guid;
			}//NextGuid()
		}//class Container

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Guid NewGuid() => LocalContainer.NextGuid();
	}//class FastGuid
}//ns