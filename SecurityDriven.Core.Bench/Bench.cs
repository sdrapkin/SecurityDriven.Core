using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SecurityDriven.Core.Bench
{
	class Bench
	{
		static void Main(string[] args)
		{
			var sw = new Stopwatch();
			const long ITER = 5_000_000L * 1L;

			$"{nameof(Environment.ProcessorCount)}: {Environment.ProcessorCount}".Dump();
			$"{nameof(CryptoRandom.BYTE_CACHE_SIZE)}: {CryptoRandom.BYTE_CACHE_SIZE}".Dump();
			$"{nameof(CryptoRandom.REQUEST_CACHE_LIMIT)}: {CryptoRandom.REQUEST_CACHE_LIMIT}".Dump();
			$"{nameof(TestStruct)} Size: {Utils.StructSizer<TestStruct>.Size}\n".Dump();

			var cr = new CryptoRandom();

			for (int _ = 0; _ < 4; ++_)
			{
				Guid g = default;
				cr.FillStruct(ref g);
				g.Dump();
			}
			"".Dump();
			//return;
			const int REPS = 6;

			for (int j = 0; j < REPS; ++j)
			{
				{
					sw.Restart();
					for (var k = 0L; k < ITER; ++k)
					//Parallel.For(0L, ITER, i =>
					{
						var data = default(TestStruct);
						var span = Utils.AsSpan(ref data);
						cr.NextBytes(span);
					}
					sw.Stop();
					$"{sw.Elapsed} {nameof(Utils.AsSpan) + " " + nameof(cr.NextBytes)} {cr.GetType().Name}".Dump();
				}

				{
					sw.Restart();
					for (var k = 0L; k < ITER; ++k)
					//Parallel.For(0L, ITER, i =>
					{
						var data = default(TestStruct);
						cr.FillStruct(ref data);
					}
					sw.Stop();
					$"{sw.Elapsed} {nameof(cr.FillStruct)} {cr.GetType().Name}".Dump();
				}

				{
					sw.Restart();
					for (var k = 0L; k < ITER; ++k)
					//Parallel.For(0L, ITER, i =>
					{
						var data = default(TestStruct);
						CryptoRandom.Instance.FillStruct(ref data);
					}
					sw.Stop();
					$"{sw.Elapsed} {nameof(CryptoRandom)}.{nameof(CryptoRandom.Instance)}.{nameof(CryptoRandom.FillStruct)} {cr.GetType().Name}".Dump();
				}

				{
					sw.Restart();
					for (var k = 0L; k < ITER; ++k)
					//Parallel.For(0L, ITER, i =>
					{
						Guid.NewGuid();
					}
					sw.Stop();
					$"{sw.Elapsed} {nameof(Guid.NewGuid)}".Dump();
				}
				"".Dump();
			}
		}

		[StructLayout(LayoutKind.Sequential, Size = 16)]
		public struct TestStruct { }

	}//class Bench

	public static class ObjExtensions
	{
		public static T Dump<T>(this T entity, string msg = null)
		{
			var str = entity.ToString();
			if (msg != null) str += $" [{msg}]";
			Console.WriteLine(str);
			return entity;
		}
	}// class ObjExtensions
}//ns