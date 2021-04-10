using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SecurityDriven.Core.Bench
{
	using Extensions;
	class Bench
	{
		static void Main(string[] args)
		{
			const bool SEEDED_TEST = true;
			if (SEEDED_TEST)
			{
				var seedkey = new byte[CryptoRandom.Params.Seeded.SEEDKEY_SIZE];
				var seeded = new CryptoRandom(seedkey);

				Span<byte> data = new byte[256];

				seeded.Reseed(seedkey);
				seeded.NextBytes(data);
				Convert.ToHexString(data).Dump();

				data.Clear();
				//seeded = new SeededCryptoRandomImpl(seedkey); 
				seeded.Reseed(seedkey);
				for (int i = 0; i < data.Length; ++i)
				{
					seeded.NextBytes(data.Slice(i, 1));
					Convert.ToHexString(data.Slice(0, i + 1)).Dump(); Console.WriteLine("====================");
				}

				return;
			}
			var sw = new Stopwatch();
			const long ITER = 5_000_000L * 2L;

			$"{nameof(Environment.ProcessorCount)}: {Environment.ProcessorCount}".Dump();
			$"{nameof(CryptoRandom.Params.RNG.BYTE_CACHE_SIZE)}: {CryptoRandom.Params.RNG.BYTE_CACHE_SIZE}".Dump();
			$"{nameof(CryptoRandom.Params.RNG.REQUEST_CACHE_LIMIT)}: {CryptoRandom.Params.RNG.REQUEST_CACHE_LIMIT}".Dump();
			$"{nameof(TestStruct)} Size: {Utils.StructSizer<TestStruct>.Size}\n".Dump();

			const bool IS_SEQUENTIAL = false;
			const bool IS_PARALLEL = true;
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


			IS_SEQUENTIAL.Dump(nameof(IS_SEQUENTIAL));
			IS_PARALLEL.Dump(nameof(IS_PARALLEL));

			for (int j = 0; j < REPS; ++j)
			{
				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, i =>
					{
						var data = default(TestStruct);
						var span = Utils.AsSpan(ref data);
						cr.NextBytes(span);
					});
					sw.Stop();
					$"{sw.Elapsed} {nameof(Utils.AsSpan) + " " + nameof(cr.NextBytes)} {cr.GetType().Name}".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, i =>
					{
						var data = default(TestStruct);
						cr.FillStruct(ref data);
					});
					sw.Stop();
					$"{sw.Elapsed} {nameof(CryptoRandomExtensions.FillStruct)} {cr.GetType().Name}".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, i =>
					{
						var data = default(TestStruct);
						CryptoRandom.Instance.FillStruct(ref data);
					});
					sw.Stop();
					$"{sw.Elapsed} {nameof(CryptoRandom)}.{nameof(CryptoRandom.Instance)}.{nameof(CryptoRandomExtensions.FillStruct)} {cr.GetType().Name}".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, i =>
					{
						cr.NewRandomGuid();
					});
					sw.Stop();
					$"{sw.Elapsed} {cr.GetType().Name}.{nameof(CryptoRandomExtensions.NewRandomGuid)}".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, i =>
					{
						Guid.NewGuid();
					});
					sw.Stop();
					$"{sw.Elapsed} {typeof(Guid).FullName}.{nameof(Guid.NewGuid)}".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, i =>
					{
						cr.NewSqlServerGuid();
					});
					sw.Stop();
					$"{sw.Elapsed} {cr.GetType().FullName}.{nameof(CryptoRandomExtensions.NewSqlServerGuid)}".Dump();
				}
				"".Dump();
			}// REPS
		}//Main()

		[StructLayout(LayoutKind.Sequential, Size = 16)]
		public struct TestStruct { }

		static void Runner(long iterations, bool isSequential, bool isParallel, Action<long> action)
		{
			if (isSequential) for (var i = 0L; i < iterations; ++i) action(i);

			if (isParallel) Parallel.For(0L, iterations, i => action(i));
		}//Runner()
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