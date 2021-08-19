using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SecurityDriven.Core
{
	internal static class FastGuid
	{
		const int GUIDS_PER_THREAD = 512; //keep it power-of-2
		[ThreadStatic] static Container ts_data;

		static Container CreateContainer() => ts_data = new();
		static Container LocalContainer => ts_data ?? CreateContainer();

		sealed class Container
		{
			public Guid[] _guids = GC.AllocateUninitializedArray<Guid>(GUIDS_PER_THREAD);
			public int _idx = GUIDS_PER_THREAD;

			public Guid NextGuid()
			{
				int idx = _idx++ & (GUIDS_PER_THREAD - 1);
				if (idx == 0) RandomNumberGenerator.Fill(MemoryMarshal.Cast<Guid, byte>(_guids));

				var guid = _guids[idx];
				_guids[idx] = default; // prevents Guid leakage
				return guid;
			}//NextGuid()
		}//class Container

		public static Guid NewGuid() => LocalContainer.NextGuid();
	}//class FastGuid
}//ns