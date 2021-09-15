using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SecurityDriven.Core.Bench
{
	class Bench
	{
		static readonly CryptoRandom cr = CryptoRandom.Shared;
		static void Main(string[] args)
		{
			const bool SEEDED_TEST = false;
			const long ITER = 10_000_000L * 2L;
			const bool IS_SEQUENTIAL = false;
			const bool IS_PARALLEL = true;

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

            $".NET: [{Environment.Version}]".Dump();
			$"{nameof(Environment.ProcessorCount)}: {Environment.ProcessorCount}".Dump();
			$"{nameof(CryptoRandom.Params.RNG.BYTE_CACHE_SIZE)}: {CryptoRandom.Params.RNG.BYTE_CACHE_SIZE}".Dump();
			$"{nameof(CryptoRandom.Params.RNG.REQUEST_CACHE_LIMIT)}: {CryptoRandom.Params.RNG.REQUEST_CACHE_LIMIT}".Dump();
			$"{nameof(TestStruct)} Size: {Utils.StructSizer<TestStruct>.Size}\n".Dump();


			for (int _ = 0; _ < 4; ++_)
			{
				Guid g = default;
				cr.Next(ref g);
				g.Dump();
			}
			"".Dump();
			//return;
			const int REPS = 5;


			IS_SEQUENTIAL.Dump(nameof(IS_SEQUENTIAL));
			IS_PARALLEL.Dump(nameof(IS_PARALLEL));

			for (int j = 0; j < REPS; ++j)
			{
				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, static i =>
					{
						var data = default(TestStruct);
						var span = Utils.AsSpan(ref data);
						cr.NextBytes(span);
					});
					sw.Stop();
					$"{sw.Elapsed} Utils.AsSpan(ref data); cr.NextBytes(span);".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, static i =>
					{
						var data = default(TestStruct);
						cr.Next(ref data);
					});
					sw.Stop();
					$"{sw.Elapsed} cr.Next(ref data);".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, static i =>
					{
						var data = default(TestStruct);
						CryptoRandom.Shared.Next(ref data);
					});
					sw.Stop();
					$"{sw.Elapsed} CryptoRandom.Shared.Next(ref data);".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, static i =>
					{
						cr.NextGuid();
					});
					sw.Stop();
					$"{sw.Elapsed} cr.NextGuid();".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, static i =>
					{
						cr.Next<Guid>();
					});
					sw.Stop();
					$"{sw.Elapsed} cr.Next<Guid>();".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, static i =>
					{
						Guid.NewGuid();
					});
					sw.Stop();
					$"{sw.Elapsed} Guid.NewGuid();".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, static i =>
					{
						cr.SqlServerGuid();
					});
					sw.Stop();
					$"{sw.Elapsed} cr.SqlServerGuid();".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, static i =>
					{
						cr.Next();
					});
					sw.Stop();
					$"{sw.Elapsed} cr.Next();".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, static i =>
					{
						cr.NextDouble();
					});
					sw.Stop();
					$"{sw.Elapsed} cr.NextDouble();".Dump();
				}

				{
					sw.Restart();
					Runner(ITER, IS_SEQUENTIAL, IS_PARALLEL, static i =>
					{
						cr.NextSingle();
					});
					sw.Stop();
					$"{sw.Elapsed} cr.NextSingle();".Dump();
				}
				"".Dump();
			}// REPS
		}//Main()

		[StructLayout(LayoutKind.Sequential, Size = 16)]
		public struct TestStruct { }

		static void Runner(long iterations, bool isSequential, bool isParallel, Action<long> action)
		{
			if (isSequential) for (var i = 0L; i < iterations; ++i) action(i);

			if (isParallel) Parallel.For(0L, iterations, action);
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