using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SecurityDriven.Core.Tests
{
	// Microsoft .NET Random tests: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Runtime.Extensions/tests/System/Random.cs
	// Microsoft .NET RandomNumberGenerator tests: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Security.Cryptography.Algorithms/tests/RandomNumberGeneratorTests.cs

	[TestClass]
	public class CryptoRandomTests
	{
		#region System.Random tests
		[DataTestMethod]
		[DataRow(false, false)]
		[DataRow(false, true)]
		[DataRow(true, false)]
		[DataRow(true, true)]
		public void InvalidArguments_Throws(bool derived, bool seeded)
		{
			Random r = Create(derived, seeded);
			Assert.ThrowsException<ArgumentNullException>(() => r.NextBytes(null));
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => r.Next(-1));
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => r.Next(2, 1));
		}//InvalidArguments_Throws()

		[DataTestMethod]
		[DataRow(false, false)]
		[DataRow(false, true)]
		[DataRow(true, false)]
		[DataRow(true, true)]
		public void SmallRanges_ReturnsExpectedValue(bool derived, bool seeded)
		{
			CryptoRandom r = Create(derived, seeded);

			Assert.IsTrue(0 == r.Next(0));
			Assert.IsTrue(0 == r.Next(0, 0));
			Assert.IsTrue(1 == r.Next(1, 1));

			Assert.IsTrue(0 == r.Next(1));
			Assert.IsTrue(1 == r.Next(1, 2));

			Assert.IsTrue(0 == r.NextInt64(0));
			Assert.IsTrue(0 == r.NextInt64(0, 0));
			Assert.IsTrue(1 == r.NextInt64(1, 1));

			Assert.IsTrue(0 == r.NextInt64(1));
			Assert.IsTrue(1 == r.NextInt64(1, 2));
		}//SmallRanges_ReturnsExpectedValue()

		[DataTestMethod]
		[DataRow(false, false)]
		[DataRow(false, true)]
		[DataRow(true, false)]
		[DataRow(true, true)]
		public void NextInt_AllValuesAreWithinSpecifiedRange(bool derived, bool seeded)
		{
			CryptoRandom r = Create(derived, seeded);

			for (int i = 0; i < 1000; i++)
			{

				Assert_InRange(r.Next(20), 0, 19);
				Assert_InRange(r.Next(20, 30), 20, 29);

				Assert_InRange(r.NextInt64(20), 0, 19);
				Assert_InRange(r.NextInt64(20, 30), 20, 29);
			}

			for (int i = 0; i < 1000; i++)
			{
				double x = r.NextDouble();
				Assert.IsTrue(x >= 0.0 && x < 1.0);
			}

			for (int i = 0; i < 1000; i++)
			{
				float x = r.NextSingle();
				Assert.IsTrue(x >= 0.0 && x < 1.0);
			}
		}//NextInt_AllValuesAreWithinSpecifiedRange()

		[DataTestMethod]
		[DataRow(false, false)]
		[DataRow(false, true)]
		[DataRow(true, false)]
		[DataRow(true, true)]
		public void Next_Int_AllValuesWithinSmallRangeHit(bool derived, bool seeded)
		{
			Random r = Create(derived, seeded);

			var hs = new HashSet<int>();
			for (int i = 0; i < 10_000; i++)
			{
				hs.Add(r.Next(4));
			}

			for (int i = 0; i < 4; i++)
			{
				Assert.IsTrue(hs.Contains(i));
			}

			Assert.IsTrue(!hs.Contains(-1));
			Assert.IsTrue(!hs.Contains(4));
		}//Next_Int_AllValuesWithinSmallRangeHit()

		[DataTestMethod]
		[DataRow(false, false)]
		[DataRow(false, true)]
		[DataRow(true, false)]
		[DataRow(true, true)]
		public void Next_IntInt_AllValuesWithinSmallRangeHit(bool derived, bool seeded)
		{
			Random r = Create(derived, seeded);

			var hs = new HashSet<int>();
			for (int i = 0; i < 10_000; i++)
			{
				hs.Add(r.Next(42, 44));
			}

			for (int i = 42; i < 44; i++)
			{
				Assert.IsTrue(hs.Contains(i));
			}

			Assert.IsTrue(!hs.Contains(41));
			Assert.IsTrue(!hs.Contains(44));
		}//Next_IntInt_AllValuesWithinSmallRangeHit()

		public static IEnumerable<(bool derived, bool seeded, int min, int max)> Next_IntInt_Next_IntInt_AllValuesAreWithinRange_MemberData() =>
			from derived in new[] { false, true }
			from seeded in new[] { false, true }
			from (int min, int max) pair in new[]
			{
						(1, 2),
						(-10, -3),
						(0, int.MaxValue),
						(-1, int.MaxValue),
						(int.MinValue, 0),
						(int.MinValue, int.MaxValue),
			}
			select (derived, seeded, pair.min, pair.max);

		[TestMethod]
		public void Next_IntInt_Next_IntInt_AllValuesAreWithinRange()
		{
			foreach (var testcase in Next_IntInt_Next_IntInt_AllValuesAreWithinRange_MemberData())
			{
				Random r = Create(testcase.derived, testcase.seeded);
				int min = testcase.min;
				int max = testcase.max;
				for (int i = 0; i < 100; i++)
				{
					Assert_InRange(r.Next(min, max), min, max - 1);
				}
			}//foreach
		}//Next_IntInt_Next_IntInt_AllValuesAreWithinRange()

		[DataTestMethod]
		[DataRow(false, false)]
		[DataRow(false, true)]
		[DataRow(true, false)]
		[DataRow(true, true)]
		public void Next_Long_AllValuesWithinSmallRangeHit(bool derived, bool seeded)
		{
			CryptoRandom r = Create(derived, seeded);

			var hs = new HashSet<long>();
			for (int i = 0; i < 10_000; i++)
			{
				hs.Add(r.NextInt64(4));
			}

			for (long i = 0; i < 4; i++)
			{
				Assert.IsTrue(hs.Contains(i));
			}

			Assert.IsTrue(!hs.Contains(-1L));
			Assert.IsTrue(!hs.Contains(4L));
		}//Next_Long_AllValuesWithinSmallRangeHit()

		[DataTestMethod]
		[DataRow(false, false)]
		[DataRow(false, true)]
		[DataRow(true, false)]
		[DataRow(true, true)]
		public void Next_LongLong_AllValuesWithinSmallRangeHit(bool derived, bool seeded)
		{
			CryptoRandom r = Create(derived, seeded);

			var hs = new HashSet<long>();
			for (int i = 0; i < 10_000; i++)
			{
				hs.Add(r.NextInt64(42, 44));
			}

			for (long i = 42; i < 44; i++)
			{
				Assert.IsTrue(hs.Contains(i));
			}

			Assert.IsTrue(!hs.Contains(41L));
			Assert.IsTrue(!hs.Contains(44L));
		}//Next_LongLong_AllValuesWithinSmallRangeHit()

		public static IEnumerable<(bool derived, bool seeded, long min, long max)> Next_LongLong_Next_IntInt_AllValuesAreWithinRange_MemberData() =>
			from derived in new[] { false, true }
			from seeded in new[] { false, true }
			from (long min, long max) pair in new[]
			{
						(1L, 2L),
						(0L, long.MaxValue),
						(2147483648, 2147483658),
						(-1L, long.MaxValue),
						(long.MinValue, 0L),
						(long.MinValue, long.MaxValue),
			}
			select (derived, seeded, pair.min, pair.max);

		[TestMethod]
		public void Next_LongLong_Next_IntInt_AllValuesAreWithinRange()
		{
			foreach (var testcase in Next_LongLong_Next_IntInt_AllValuesAreWithinRange_MemberData())
			{
				CryptoRandom r = Create(testcase.derived, testcase.seeded);
				long min = testcase.min;
				long max = testcase.max;
				for (int i = 0; i < 100; i++)
				{
					Assert_InRange(r.NextInt64(min, max), min, max - 1);
				}
			}//foreach
		}//Next_LongLong_Next_IntInt_AllValuesAreWithinRange()

		[DataTestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void CtorWithSeed_SequenceIsRepeatable(bool derived)
		{
			Random r1 = Create(derived, seeded: true);
			Random r2 = Create(derived, seeded: true);

			for (int i = 0; i < 2; i++)
			{
				byte[] b1 = new byte[1000];
				byte[] b2 = new byte[1000];
				if (i == 0)
				{
					r1.NextBytes(b1);
					r2.NextBytes(b2);
				}
				else
				{
					r1.NextBytes((Span<byte>)b1);
					r2.NextBytes((Span<byte>)b2);
				}
				Assert.IsTrue(Enumerable.SequenceEqual(b1, b2));
			}

			for (int i = 0; i < 1000; i++)
			{
				Assert.IsTrue(r1.Next() == r2.Next());
			}
		}//CtorWithSeed_SequenceIsRepeatable()

		[DataTestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void ExpectedValues(bool derived)
		{
			// CryptoRandom has a predictable sequence of values it generates based on its seed.
			// To ensure that we would be made aware if a change to the implementation causes these
			// sequences to change, this test verifies the first few numbers for a few seeds.
			int[][] expectedValues = new int[][]
			{
				new int[] { 1027647438, 1852530765, 1405439495, 413004730, 1241735282, 1948952119, 250979025, 238356085, 682707677, 2078362189, },
				new int[] { 1136682261, 891282810, 339569888, 1679577548, 902277428, 917941590, 1117253954, 99423136, 2083370328, 1595249226, },
				new int[] { 195781101, 1940982636, 598644121, 1879316649, 1365741193, 1985560985, 120351702, 183756128, 203463642, 407523771, },
				new int[] { 2097021156, 1659474388, 1481513956, 1949070438, 1213307541, 620297144, 330354699, 2104853946, 629216054, 385463448, },
				new int[] { 725087327, 1219827580, 635169074, 325535162, 1974055998, 1507458866, 112403882, 1355244321, 1793038287, 1773857518, },
				new int[] { 1359711929, 1539838827, 1527968577, 1419722492, 566173104, 761948295, 512113060, 2145167905, 960218089, 1232639679, },
				new int[] { 1589907896, 232631374, 2059605279, 114856965, 1224532737, 209001029, 1737677890, 830321804, 934999427, 705504517, },
				new int[] { 166237700, 19033973, 1982373402, 1641512351, 942624309, 1494101747, 21891837, 1030588009, 143309231, 788115328, },
				new int[] { 1473979008, 16670556, 171015190, 1824259853, 1897927532, 1550734193, 1906212723, 44567062, 2137522750, 490523763, },
				new int[] { 1260503815, 280085394, 1743464228, 135753883, 624969495, 1073065004, 1685006766, 752873683, 356807245, 2023099415, },
				new int[] { 1422751086, 1056721485, 916355461, 500211338, 827124636, 606353662, 409805816, 1774354262, 1586807745, 1217241799, },
				new int[] { 1729506193, 1363614911, 225716869, 630887721, 1072829911, 910009381, 1401887696, 1900645751, 316430831, 1750544604, },
				new int[] { 84638795, 138084919, 834666341, 1033600571, 361837898, 942282631, 404185607, 197360574, 1149826209, 1610639602, },
				new int[] { 2090663828, 300631120, 1208395287, 364891715, 452130656, 2110318451, 410150535, 2002803408, 1527207155, 549673775, },
				new int[] { 1524873186, 1551755788, 1634213696, 706552658, 365588876, 2060962779, 1011727843, 396089606, 1497734152, 1657778646, },
				new int[] { 774635153, 1043968297, 98936198, 2112840736, 770833630, 2086128308, 299348404, 1910304716, 213577125, 1634121351, },
				new int[] { 51464426, 1701566159, 1982177536, 1468975643, 316111667, 1198152202, 1586629143, 186690041, 294082959, 1704188567, },
				new int[] { 1304684169, 1818545359, 319735098, 1418385078, 263752120, 2100576437, 327020547, 360518901, 1070142289, 455532598, },
				new int[] { 775389430, 1580218831, 696175502, 1256614496, 225865955, 1701390138, 369029401, 1403096819, 1234959516, 415577915, },
				new int[] { 941952185, 1115008635, 848528758, 1696163500, 1393210995, 1521454688, 1908205729, 1600689996, 238872050, 891574570, },
			};

			for (int seed = 0; seed < expectedValues.Length; ++seed)
			{
				Random r = derived ? new SubCryptoRandom(seed) : new CryptoRandom(seed);
				for (int i = 0; i < expectedValues[seed].Length; ++i)
				{
					Assert.IsTrue(r.Next() == expectedValues[seed][i]);
				}
			}

			/* 
				// Generator
				int[][] expectedValues = new int[20][];
				for (int i = 0; i < expectedValues.Length; ++i) expectedValues[i] = new int[10];
				for (int seed = 0; seed < expectedValues.Length; ++seed)
				{
					Random r = new CryptoRandom(seed);
					Console.Write("new int[] { ");
					for (int i = 0; i < expectedValues[seed].Length; ++i) Console.Write($"{r.Next()}, ");
					Console.WriteLine("},");
				}
			*/
		}//ExpectedValues()

		[DataTestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void ExpectedValues_NextBytes(bool derived)
		{
			byte[][] expectedValues = new byte[][]
			{
				new byte[] { 0xCE, 0xA7, 0x40, 0x3D, 0x4D, 0x60, 0x6B, 0x6E, 0x07, 0x4E, },
				new byte[] { 0x15, 0x65, 0xC0, 0x43, 0x7A, 0xE5, 0x1F, 0xB5, 0xE0, 0x6C, },
				new byte[] { 0xED, 0x61, 0xAB, 0x0B, 0x6C, 0x0B, 0xB1, 0x73, 0x99, 0x95, },
				new byte[] { 0xE4, 0x00, 0xFE, 0xFC, 0xD4, 0x91, 0xE9, 0xE2, 0xE4, 0x1B, },
				new byte[] { 0x5F, 0xF4, 0x37, 0xAB, 0x7C, 0x17, 0xB5, 0xC8, 0x32, 0xE9, },
				new byte[] { 0xB9, 0x8E, 0x0B, 0xD1, 0x6B, 0x13, 0xC8, 0x5B, 0x41, 0xF3, },
				new byte[] { 0xB8, 0x11, 0xC4, 0xDE, 0x4E, 0xAC, 0xDD, 0x0D, 0x1F, 0x15, },
				new byte[] { 0x04, 0x96, 0xE8, 0x09, 0x75, 0x6F, 0x22, 0x81, 0x1A, 0x9E, },
				new byte[] { 0x80, 0x22, 0xDB, 0x57, 0x5C, 0x5F, 0xFE, 0x00, 0x16, 0x7C, },
				new byte[] { 0x07, 0xC3, 0x21, 0xCB, 0x92, 0xC3, 0xB1, 0x10, 0x24, 0x27, },
				new byte[] { 0x6E, 0x75, 0xCD, 0xD4, 0x4D, 0x4A, 0xFC, 0x3E, 0x85, 0x79, },
				new byte[] { 0x91, 0x2B, 0x16, 0xE7, 0xBF, 0x1C, 0x47, 0xD1, 0x85, 0x2A, },
				new byte[] { 0x4B, 0x7C, 0x0B, 0x85, 0x37, 0x02, 0x3B, 0x08, 0x65, 0xFF, },
				new byte[] { 0x94, 0xFF, 0x9C, 0xFC, 0x50, 0x44, 0xEB, 0x11, 0x17, 0xA6, },
				new byte[] { 0xE2, 0xB7, 0xE3, 0x5A, 0x0C, 0xEA, 0x7D, 0x5C, 0x40, 0x1F, },
				new byte[] { 0x91, 0xFE, 0x2B, 0xAE, 0x29, 0xB1, 0x39, 0xBE, 0x86, 0xA5, },
				new byte[] { 0xEA, 0x48, 0x11, 0x83, 0xCF, 0xD6, 0x6B, 0x65, 0x00, 0xA1, },
				new byte[] { 0x89, 0xE6, 0xC3, 0xCD, 0xCF, 0xCC, 0x64, 0xEC, 0x3A, 0xC5, },
				new byte[] { 0xF6, 0x80, 0x37, 0xAE, 0xCF, 0x39, 0x30, 0xDE, 0x8E, 0xCB, },
				new byte[] { 0xB9, 0x0C, 0x25, 0x38, 0x7B, 0xAE, 0x75, 0x42, 0x76, 0x85, },
			};

			for (int seed = 0; seed < expectedValues.Length; seed++)
			{
				byte[] actualValues = new byte[expectedValues[seed].Length];
				Random r = derived ? new SubCryptoRandom(seed) : new CryptoRandom(seed);

				r.NextBytes(actualValues);
				Assert.IsTrue(Enumerable.SequenceEqual(expectedValues[seed], actualValues));
			}

			for (int seed = 0; seed < expectedValues.Length; seed++)
			{
				byte[] actualValues = new byte[expectedValues[seed].Length];
				Random r = derived ? new SubCryptoRandom(seed) : new CryptoRandom(seed);

				r.NextBytes((Span<byte>)actualValues);
				Assert.IsTrue(Enumerable.SequenceEqual(expectedValues[seed], actualValues));
			}

			/*
				// Generator
				int[][] expectedValues = new int[20][];
				for (int i = 0; i < expectedValues.Length; ++i) expectedValues[i] = new int[10];
				for (int seed = 0; seed < expectedValues.Length; ++seed)
				{
					Random r = new CryptoRandom(seed);
					Console.Write("new byte[] { ");
					for (int i = 0; i < expectedValues[seed].Length; ++i) Console.Write($"0x{(r as CryptoRandom).Next<byte>().ToString("X2")}, ");
					Console.WriteLine("},");
				}
			*/
		}//ExpectedValues_NextBytes()

		[DataTestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void Sample(bool seeded)
		{
			SubCryptoRandom r = seeded ? new SubCryptoRandom(42) : new SubCryptoRandom();
			for (int i = 0; i < 1000; i++)
			{
				double d = r.ExposeSample();
				Assert.IsTrue(d >= 0.0 && d < 1.0);
			}
		}//Sample()

		[DataTestMethod]
		[DataRow(false, false)]
		[DataRow(false, true)]
		[DataRow(true, false)]
		[DataRow(true, true)]
		public void Empty_Success(bool derived, bool seeded)
		{
			Random r = Create(derived, seeded);
			r.NextBytes(new byte[0]);
			r.NextBytes(Span<byte>.Empty);
		}//Empty_Success()

		[TestMethod]
		public void Shared_IsSingleton()
		{
			Assert.IsNotNull(CryptoRandom.Shared);
			Assert.AreSame(CryptoRandom.Shared, CryptoRandom.Shared);
			Assert.AreSame(CryptoRandom.Shared, Task.Run(() => CryptoRandom.Shared).Result);
		}//Shared_IsSingleton()

		[DataTestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void RandomDistributionBug(bool seeded)
		{
			// test absence of bug in CryptoRandom
			{
				var random = Create(derived: false, seeded: seeded);
				const int mod = 2;
				int[] hist = new int[mod];
				for (int i = 0; i < 1_000_000; ++i)
				{
					int num = random.Next(0x55555555);
					int num2 = num % mod;
					++hist[num2];
				}
				decimal ratio = (decimal)hist[0] / (decimal)hist[1];
				Assert.IsTrue(ratio > 0.99M && ratio < 1.01M);
			}

			// test presence of bug in System.Random
			{
				var random = new Random(123);
				const int mod = 2;
				int[] hist = new int[mod];
				for (int i = 0; i < 1_000_000; ++i)
				{
					int num = random.Next(0x55555555);
					int num2 = num % mod;
					++hist[num2];
				}
				decimal ratio = (decimal)Math.Min(hist[0], hist[1]) / (decimal)Math.Max(hist[0], hist[1]);
				Assert.IsTrue(ratio > 0.45M && ratio < 0.55M);
			}
		}//RandomDistributionBug()

		static CryptoRandom Create(bool derived, bool seeded, int seed = 42)
		{
			return (derived, seeded) switch
			{
				(false, false) => new CryptoRandom(),
				(false, true) => new CryptoRandom(seed),
				(true, false) => new SubCryptoRandom(),
				(true, true) => new SubCryptoRandom(seed)
			};
		}//Create()

		[TestMethod]
		public void Shared_ParallelUsage()
		{
			using var barrier = new Barrier(2);
			Parallel.For(0, 2, _ =>
			{
				byte[] buffer = new byte[1000];

				barrier.SignalAndWait();
				for (int i = 0; i < 1_000; i++)
				{
					Assert_InRange(CryptoRandom.Shared.Next(), 0, int.MaxValue - 1);
					Assert_InRange(CryptoRandom.Shared.Next(5), 0, 4);
					Assert_InRange(CryptoRandom.Shared.Next(42, 50), 42, 49);

					Assert_InRange(CryptoRandom.Shared.NextInt64(), 0, long.MaxValue - 1);
					Assert_InRange(CryptoRandom.Shared.NextInt64(5), 0L, 5L);
					Assert_InRange(CryptoRandom.Shared.NextInt64(42L, 50L), 42L, 49L);

					Assert_InRange(CryptoRandom.Shared.NextSingle(), 0.0f, 1.0f);
					Assert_InRange(CryptoRandom.Shared.NextDouble(), 0.0, 1.0);

					Array.Clear(buffer, 0, buffer.Length);
					CryptoRandom.Shared.NextBytes(buffer);
					Assert.IsTrue(buffer.Any(b => b != 0));

					Array.Clear(buffer, 0, buffer.Length);
					CryptoRandom.Shared.NextBytes((Span<byte>)buffer);
					Assert.IsTrue(buffer.Any(b => b != 0));
				}//for
			});// Parallel.For
		}//Shared_ParallelUsage()

		class SubCryptoRandom : CryptoRandom
		{
			public bool SampleCalled, NextCalled;

			public SubCryptoRandom() { }
			public SubCryptoRandom(int Seed) : base(Seed) { }

			public double ExposeSample() => Sample();

			protected override double Sample()
			{
				SampleCalled = true;
				return base.Sample();
			}

			public override int Next()
			{
				NextCalled = true;
				return base.Next();
			}
		}//class SubCryptoRandom

		static void Assert_InRange(long value, long minInclusive, long maxInclusive)
		{
			if (value < minInclusive)
				throw new ArgumentOutOfRangeException(nameof(value), "Value is less than minimum.");

			if (value > maxInclusive)
				throw new ArgumentOutOfRangeException(nameof(value), "Value is greater than maximum.");
		}//InRange(long)

		static void Assert_InRange(double value, double minInclusive, double maxInclusive)
		{
			if (value < minInclusive)
				throw new ArgumentOutOfRangeException(nameof(value), "Value is less than minimum.");

			if (value > maxInclusive)
				throw new ArgumentOutOfRangeException(nameof(value), "Value is greater than maximum.");
		}//InRange(double)
		#endregion System.Random tests

		#region RandomNumberGenerator tests
		[DataTestMethod]
		[DataRow(2048)]
		[DataRow(65536)]
		[DataRow(1048576)]
		public void RandomDistribution(int arraySize)
		{
			byte[] random = new byte[arraySize];

			CryptoRandom rng = new CryptoRandom();
			rng.NextBytes(random);

			VerifyRandomDistribution(random);
		}//RandomDistribution()

		[TestMethod]
		public void ZeroLengthInput()
		{
			CryptoRandom rng = new CryptoRandom();
			rng.NextBytes(Array.Empty<byte>()); // While this will do nothing, it's not something that throws.

		}//ZeroLengthInput()

		[TestMethod]
		public void ConcurrentAccess()
		{
			const int ParallelTasks = 3;
			const int PerTaskIterationCount = 20;
			const int RandomSize = 1024;

			Task[] tasks = new Task[ParallelTasks];
			byte[][] taskArrays = new byte[ParallelTasks][];

			CryptoRandom rng = new CryptoRandom();
			using (ManualResetEvent sync = new ManualResetEvent(false))
			{
				for (int iTask = 0; iTask < ParallelTasks; iTask++)
				{
					taskArrays[iTask] = new byte[RandomSize];
					byte[] taskLocal = taskArrays[iTask];

					tasks[iTask] = Task.Run(
						() =>
						{
							sync.WaitOne();

							for (int i = 0; i < PerTaskIterationCount; i++)
							{
								rng.NextBytes(taskLocal);
							}
						});
				}

				// Ready? Set() Go!
				sync.Set();
				Task.WaitAll(tasks);
			}

			for (int i = 0; i < ParallelTasks; i++)
			{
				// The Real test would be to ensure independence of data, but that's difficult.
				// The other end of the spectrum is to test that they aren't all just new byte[RandomSize].
				// Middle ground is to assert that each of the chunks has random data.
				VerifyRandomDistribution(taskArrays[i]);
			}
		}//ConcurrentAccess()

		[DataTestMethod]
		[DataRow(400)]
		[DataRow(65536)]
		[DataRow(1048576)]
		public void GetBytes_Offset(int arraySize)
		{
			CryptoRandom rng = new CryptoRandom();

			byte[] rand = new byte[arraySize];

			// Set canary bytes
			rand[99] = 77;
			rand[399] = 77;

			rng.NextBytes(new Span<byte>(rand, 100, 200));

			// Array should not have been touched outside of 100-299
			Assert.AreEqual(99, Array.IndexOf<byte>(rand, 77, 0));
			Assert.AreEqual(399, Array.IndexOf<byte>(rand, 77, 300));

			// Ensure 100-300 has random bytes; not likely to ever fail here by chance (256^200)
			Assert.IsTrue(rand.Skip(100).Take(200).Sum(b => b) > 0);
		}//GetBytes_Offset()

		[TestMethod]
		public void GetBytes_Array_Offset_ZeroCount()
		{
			CryptoRandom rng = new CryptoRandom();

			byte[] rand = new byte[1] { 1 };

			// A count of 0 should not do anything
			rng.NextBytes(new Span<byte>(rand, 0, 0));
			Assert.AreEqual(1, rand[0]);

			// Having an offset of Length is allowed if count is 0
			rng.NextBytes(new Span<byte>(rand, rand.Length, 0));
			Assert.AreEqual(1, rand[0]);

			// Zero-length array should not throw
			rand = Array.Empty<byte>();
			rng.NextBytes(new Span<byte>(rand, 0, 0));
		}//GetBytes_Array_Offset_ZeroCount()

		[DataTestMethod]
		[DataRow(10)]
		[DataRow(256)]
		[DataRow(65536)]
		[DataRow(1048576)]
		public void DifferentSequential_Array(int arraySize)
		{
			// Ensure that the RNG doesn't produce a stable set of data.
			byte[] first = new byte[arraySize];
			byte[] second = new byte[arraySize];

			CryptoRandom rng = new CryptoRandom();

			rng.NextBytes(first);
			rng.NextBytes(second);

			// Random being random, there is a chance that it could produce the same sequence.
			// The smallest test case that we have is 10 bytes.
			// The probability that they are the same, given a Truly Random Number Generator is:
			// Pmatch(byte0) * Pmatch(byte1) * Pmatch(byte2) * ... * Pmatch(byte9)
			// = 1/256 * 1/256 * ... * 1/256
			// = 1/(256^10)
			// = 1/1,208,925,819,614,629,174,706,176
			Assert.IsFalse(Enumerable.SequenceEqual(first, second));
		}//DifferentSequential_Array()

		[DataTestMethod]
		[DataRow(10)]
		[DataRow(256)]
		[DataRow(65536)]
		[DataRow(1048576)]
		public void DifferentParallel(int arraySize)
		{
			// Ensure that two RNGs don't produce the same data series (such as being implemented via new Random(1)).
			byte[] first = new byte[arraySize];
			byte[] second = new byte[arraySize];

			CryptoRandom rng1 = new CryptoRandom();
			CryptoRandom rng2 = new CryptoRandom();

			rng1.NextBytes(first);
			rng2.NextBytes(second);

			// Random being random, there is a chance that it could produce the same sequence.
			// The smallest test case that we have is 10 bytes.
			// The probability that they are the same, given a Truly Random Number Generator is:
			// Pmatch(byte0) * Pmatch(byte1) * Pmatch(byte2) * ... * Pmatch(byte9)
			// = 1/256 * 1/256 * ... * 1/256
			// = 1/(256^10)
			// = 1/1,208,925,819,614,629,174,706,176
			Assert.IsFalse(Enumerable.SequenceEqual(first, second));
		}//DifferentParallel()

		[TestMethod]
		public void NextBytes_InvalidArgs()
		{
			CryptoRandom rng = new CryptoRandom();
			Assert.ThrowsException<ArgumentNullException>(() => rng.NextBytes(null));
			rng.NextBytes(new Span<byte>(null, 0, 0)); // should not throw, and just do nothing
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => rng.NextBytes(new Span<byte>(Array.Empty<byte>(), -1, 0)));
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => rng.NextBytes(new Span<byte>(Array.Empty<byte>(), 0, -1)));
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => rng.NextBytes(new Span<byte>(Array.Empty<byte>(), 0, 1)));
		}//NextBytes_InvalidArgs()

		[TestMethod]
		public void NextBytes_Int_Negative()
		{
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => CryptoRandom.Shared.NextBytes(-1));
		}//NextBytes_Int_Negative()

		[TestMethod]
		public void NextBytes_Int_Empty()
		{
			byte[] result = CryptoRandom.Shared.NextBytes(0);
			Assert.IsTrue(result.Length == 0);
		}//GetBytes_Int_Empty()

		[TestMethod]
		public void NextBytes_Span_ZeroCount()
		{
			CryptoRandom rng = new CryptoRandom();
			var rand = new byte[1] { 1 };
			rng.NextBytes(new Span<byte>(rand, 0, 0));
			Assert.AreEqual(1, rand[0]);
		}//NextBytes_Span_ZeroCount()

		[TestMethod]
		public void Fill_SpanLength1()
		{
			byte[] rand = { 1 };
			bool replacedValue = false;

			for (int i = 0; i < 10; i++)
			{
				CryptoRandom.Shared.NextBytes(rand);

				if (rand[0] != 1)
				{
					replacedValue = true;
					break;
				}
			}
			Assert.IsTrue(replacedValue, "Fill eventually wrote a different byte");
		}//Fill_SpanLength1()

		[DataTestMethod]
		[DataRow(1 << 1)]
		[DataRow(1 << 4)]
		[DataRow(1 << 16)]
		[DataRow(1 << 24)]
		public void Next_PowersOfTwo(int toExclusive)
		{
			for (int i = 0; i < 1000; i++)
			{
				int result = CryptoRandom.Shared.Next(toExclusive);
				Assert_InRange(result, 0, toExclusive - 1);
			}
		}//Next_PowersOfTwo()

		[DataTestMethod]
		[DataRow((1 << 1) + 1)]
		[DataRow((1 << 4) + 1)]
		[DataRow((1 << 16) + 1)]
		[DataRow((1 << 24) + 1)]
		public void Next_PowersOfTwoPlusOne(int toExclusive)
		{
			for (int i = 0; i < 1000; i++)
			{
				int result = CryptoRandom.Shared.Next(toExclusive);
				Assert_InRange(result, 0, toExclusive - 1);
			}
		}//Next_PowersOfTwoPlusOne()

		[TestMethod]
		public void Next_FullRange()
		{
			for (int i = 0; i < 1000; ++i)
			{
				int result = CryptoRandom.Shared.Next(int.MinValue, int.MaxValue);
				Assert.AreNotEqual(int.MaxValue, result);
			}
		}//Next_FullRange()

		[TestMethod]
		public void Next_DoesNotProduceSameNumbers()
		{
			int result1 = CryptoRandom.Shared.Next(int.MinValue, int.MaxValue);
			int result2 = CryptoRandom.Shared.Next(int.MinValue, int.MaxValue);
			int result3 = CryptoRandom.Shared.Next(int.MinValue, int.MaxValue);

			// The changes of this happening are (2^32 - 1) * 3.
			Assert.IsFalse(result1 == result2 && result2 == result3, "Generated the same number 3 times in a row.");
		}//Next_DoesNotProduceSameNumbers()

		[TestMethod]
		public void Next_FullRange_DistributesBitsEvenly()
		{
			// This test should work since we are selecting random numbers that are a [Power of two minus one] so no bit should favored.
			int numberToGenerate = 512;
			byte[] bytes = new byte[numberToGenerate * 4];
			Span<byte> bytesSpan = bytes.AsSpan();
			for (int i = 0, j = 0; i < numberToGenerate; i++, j += 4)
			{
				int result = CryptoRandom.Shared.Next(int.MinValue, int.MaxValue);
				Span<byte> slice = bytesSpan.Slice(j, 4);
				System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(slice, result);
			}
			VerifyRandomDistribution(bytes);
		}//Next_FullRange_DistributesBitsEvenly()

		[TestMethod]
		public void Next_CoinFlipLowByte()
		{
			int numberToGenerate = 2048;
			Span<int> generated = stackalloc int[numberToGenerate];

			for (int i = 0; i < numberToGenerate; i++)
			{
				generated[i] = CryptoRandom.Shared.Next(0, 2);
				Assert_InRange(generated[i], 0, 2);
			}
			VerifyDistribution(generated, 0.5);
		}//Next_CoinFlipLowByte()

		[TestMethod]
		public void Next_CoinFlipOverByteBoundary()
		{
			int numberToGenerate = 2048;
			Span<int> generated = stackalloc int[numberToGenerate];

			for (int i = 0; i < numberToGenerate; i++)
			{
				generated[i] = CryptoRandom.Shared.Next(255, 257);
				Assert_InRange(generated[i], 255, 257);
			}
			VerifyDistribution(generated, 0.5);
		}//Next_CoinFlipOverByteBoundary()

		[TestMethod]
		public void Next_NegativeBounds1000d20()
		{
			int numberToGenerate = 10_000;
			Span<int> generated = new int[numberToGenerate];

			for (int i = 0; i < numberToGenerate; i++)
			{
				generated[i] = CryptoRandom.Shared.Next(-4000, -3979);
				Assert_InRange(generated[i], -4000, -3979);
			}
			VerifyDistribution(generated, 0.05);
		}//Next_NegativeBounds1000d20()

		[TestMethod]
		public void GetInt32_1000d6()
		{
			int numberToGenerate = 10_000;
			Span<int> generated = new int[numberToGenerate];

			for (int i = 0; i < numberToGenerate; i++)
			{
				generated[i] = CryptoRandom.Shared.Next(1, 7);
				Assert_InRange(generated[i], 1, 7);
			}
			VerifyDistribution(generated, 0.16);
		}//GetInt32_1000d6()

		[DataTestMethod]
		[DataRow(int.MinValue, int.MinValue + 3)]
		[DataRow(-257, -129)]
		[DataRow(-100, 5)]
		[DataRow(254, 512)]
		[DataRow(-1_073_741_909, -1_073_741_825)]
		[DataRow(65_534, 65_539)]
		[DataRow(16_777_214, 16_777_217)]
		public void Next_MaskRangeCorrect(int fromInclusive, int toExclusive)
		{
			int numberToGenerate = 10_000;
			Span<int> generated = new int[numberToGenerate];

			for (int i = 0; i < numberToGenerate; i++)
			{
				generated[i] = CryptoRandom.Shared.Next(fromInclusive, toExclusive);
				Assert_InRange(generated[i], fromInclusive, toExclusive);
			}

			double expectedDistribution = 1d / (toExclusive - fromInclusive);
			VerifyDistribution(generated, expectedDistribution);
		}//Next_MaskRangeCorrect()

		static void VerifyRandomDistribution(byte[] random)
		{
			// Better tests for randomness are available.  For now just use a simple check that compares the number of 0s and 1s in the bits.
			VerifyNeutralParity(random);
		}

		static void VerifyNeutralParity(byte[] random)
		{
			int zeroCount = 0, oneCount = 0;

			for (int i = 0; i < random.Length; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					if (((random[i] >> j) & 1) == 1)
					{
						oneCount++;
					}
					else
					{
						zeroCount++;
					}
				}
			}

			// Over the long run there should be about as many 1s as 0s. This isn't a guarantee, just a statistical observation.
			// Allow a 7% tolerance band before considering it to have gotten out of hand.
			double bitDifference = Math.Abs(zeroCount - oneCount) / (double)(zeroCount + oneCount);
			const double AllowedTolerance = 0.07;
			if (bitDifference > AllowedTolerance)
			{
				throw new InvalidOperationException("Expected bitDifference < " + AllowedTolerance + ", got " + bitDifference + ".");
			}
		}//VerifyNeutralParity();

		static void VerifyDistribution(ReadOnlySpan<int> numbers, double expected)
		{
			var observedNumbers = new Dictionary<int, int>(numbers.Length);
			for (int i = 0; i < numbers.Length; i++)
			{
				int number = numbers[i];
				if (!observedNumbers.TryAdd(number, 1))
				{
					observedNumbers[number]++;
				}
			}
			const double tolerance = 0.07;
			foreach ((_, int occurrences) in observedNumbers)
			{
				double percentage = occurrences / (double)numbers.Length;
				double actual = Math.Abs(expected - percentage);
				Assert.IsTrue(actual < tolerance, $"Occurred number of times within threshold. Actual: {actual}");
			}
		}//VerifyDistribution()
		#endregion RandomNumberGenerator tests

		#region CryptoRandom tests from Inferno (https://github.com/sdrapkin/SecurityDriven.Inferno)
		static void AssertNeutralParity(byte[] random)
		{
			int oneCount = 0;
			int zeroCount = 0;

			for (int i = 0; i < random.Length; ++i)
			{
				for (int j = 0; j < 8; ++j)
				{
					if (((random[i] >> j) & 1) == 1)
					{
						++oneCount;
					}
					else
					{
						++zeroCount;
					}
				}
			}

			int totalCount = zeroCount + oneCount;
			float bitDifference = (float)Math.Abs(zeroCount - oneCount) / totalCount;

			// Over the long run there should be about as many 1s as 0s.
			// This isn't a guarantee, just a statistical observation.
			// Allow a 6% tolerance band before considering it to have gotten out of hand.
			Assert.IsTrue(bitDifference < 0.06, bitDifference.ToString());
		}//AssertNeutralParity()

		[TestMethod]
		public void CryptoRandom_NextDouble()
		{
			decimal bucketFn(decimal val, int _bucketCount)
			{
				if (val < 0M) return decimal.MinValue / 4;
				if (val >= 1M) return decimal.MaxValue / 2;

				decimal step = 1M / _bucketCount, m = step;
				for (int i = 0; i < _bucketCount - 1; ++i, m += step)
				{
					if (val < m) return m;
				}
				return m;
			}

			var rng = new CryptoRandom();
			decimal decimalFn() => (decimal)rng.NextDouble();

			const int bucketCount = 200;

			const int extra_count1 = 0;
			const int extra_count2 = 0;
			const int count = 40000 * 1;
			const int totalCount = count + extra_count1 + extra_count2;

			var q1 = Enumerable.Range(0, count).Select(i => decimalFn())
				.Concat(Enumerable.Repeat(0.1M, extra_count1))
				.Concat(Enumerable.Repeat(0.9M, extra_count2)).ToList();

			var q2 = q1.AsParallel().GroupBy(val => bucketFn(val, bucketCount));
			var q3 = q2.Select(d => new { Key = d.Key, Count = d.LongCount() });

			decimal expectedMaxAverageDelta = (1M / ((decimal)Math.Pow(totalCount, 1d / 2.5)));

			decimal actualAverage = q1.Average();
			decimal actualAverageDelta = Math.Abs(actualAverage - 0.5M);
			Assert.IsTrue(actualAverageDelta < expectedMaxAverageDelta, $"Unexpected average delta: {actualAverageDelta} {expectedMaxAverageDelta}");

			decimal keySum = decimal.Round(q3.Sum(i => i.Key), 2);
			Assert.IsTrue(keySum > 0);
			Assert.IsTrue(keySum < bucketCount);

			var expectedKeySum = decimal.Round((1M + bucketCount) / 2, 2);
			Assert.IsTrue(keySum == expectedKeySum, $"Unexpected bucket key sum: {keySum} expected: {expectedKeySum}");

			var avg = q3.Select(i => i.Count).Average();
			var sumOfSquares = (from i in q3 let delta = (i.Count - avg) select delta * delta).Sum();
			var stddev = Math.Sqrt(sumOfSquares / q3.Count());

			var q4 = q3.Select(i => Math.Abs(i.Count - avg));

			decimal stddevTest1 = 0M, stddevTest2 = 0M, stddevTest3 = 0M;
			foreach (var val in q4)
			{
				if (val < stddev * 1) ++stddevTest1;
				if (val < stddev * 2) ++stddevTest2;
				if (val < stddev * 3) ++stddevTest3;
			}

			stddevTest1 = decimal.Round(stddevTest1 / bucketCount, 2);
			stddevTest2 = decimal.Round(stddevTest2 / bucketCount, 2);
			stddevTest3 = decimal.Round(stddevTest3 / bucketCount, 2);

			Assert.IsTrue(Math.Abs(stddevTest1 - 0.68M) <= 0.06M, $"{nameof(stddevTest1)} failed: {stddevTest1}"); // target: 0.68
			Assert.IsTrue(Math.Abs(stddevTest2 - 0.95M) <= 0.04M, $"{nameof(stddevTest2)} failed: {stddevTest2}"); // target: 0.95
			Assert.IsTrue(Math.Abs(stddevTest3 - 0.99M) <= 0.04M, $"{nameof(stddevTest3)} failed: {stddevTest3}"); // target: 0.99
		}//CryptoRandom_NextDouble()

		static void DifferentSequential(int arraySize)
		{
			// Ensure that the RNG doesn't produce a stable set of data.
			byte[] first = new byte[arraySize];
			byte[] second = new byte[arraySize];

			var rng1 = new CryptoRandom();
			var rng2 = new CryptoRandom();

			rng1.NextBytes(first);
			rng1.NextBytes(second);

			// Random being random, there is a chance that it could produce the same sequence.
			// The smallest test case that we have is 10 bytes.
			// The probability that they are the same, given a Truly Random Number Generator is:
			// Pmatch(byte0) * Pmatch(byte1) * Pmatch(byte2) * ... * Pmatch(byte9)
			// = 1/256 * 1/256 * ... * 1/256
			// = 1/(256^10)
			// = 1/1,208,925,819,614,629,174,706,176

			Assert.IsTrue(!Enumerable.SequenceEqual(first, second));

			first.AsSpan().Clear();
			second.AsSpan().Clear();
			rng1 = new CryptoRandom();
			rng2 = new CryptoRandom();
			rng1.NextBytes(first.AsSpan());
			rng2.NextBytes(second.AsSpan());
			Assert.IsTrue(!Enumerable.SequenceEqual(first, second));
		}//DifferentSequential()

		[TestMethod]
		public void CryptoRandom_DifferentSequential_10() => DifferentSequential(10);

		[TestMethod]
		public void CryptoRandom_DifferentSequential_256() => DifferentSequential(256);

		[TestMethod]
		public void CryptoRandom_DifferentSequential_65536() => DifferentSequential(65536);

		[TestMethod]
		public void CryptoRandom_DifferentParallel_10() => DifferentParallel(10);

		[TestMethod]
		public void CryptoRandom_DifferentParallel_256() => DifferentParallel(256);

		[TestMethod]
		public void CryptoRandom_DifferentParallel_65536() => DifferentParallel(65536);

		[TestMethod]
		public void CryptoRandom_NeutralParity()
		{
			byte[] random = new byte[2048];

			var rng = new CryptoRandom();
			rng.NextBytes(random);

			AssertNeutralParity(random);


			random.AsSpan().Clear();
			rng.NextBytes(random.AsSpan());
			AssertNeutralParity(random);
		}//CryptoRandom_NeutralParity()

		[TestMethod]
		public void CryptoRandom_NullInput1()
		{
			var rng = new CryptoRandom();
			try
			{
				rng.NextBytes(null); // should throw
			}
			catch (ArgumentNullException)
			{
				Assert.IsTrue(true);
				return;
			}
			Assert.Fail($"Failed to throw {nameof(ArgumentNullException)}.");
		}//CryptoRandom_NullInput1()

		[TestMethod]
		public void CryptoRandom_ZeroLengthInput()
		{
			var rng = new CryptoRandom();

			// While this will do nothing, it's not something that throws.
			rng.NextBytes(Array.Empty<byte>());
			rng.NextBytes(new Span<byte>(Array.Empty<byte>(), 0, 0));

			bool isThrown = false;
			try
			{
				rng.NextBytes(new Span<byte>(Array.Empty<byte>(), 0, 123));
			}
			catch (ArgumentException) { isThrown = true; }
			Assert.IsTrue(isThrown);

			isThrown = false;
			try
			{
				rng.NextBytes(new Span<byte>(Array.Empty<byte>(), 123, 0));
			}
			catch (ArgumentException) { isThrown = true; }
			Assert.IsTrue(isThrown);
		}//CryptoRandom_ZeroLengthInput()

		[TestMethod]
		public void CryptoRandom_ConcurrentAccess()
		{
			const int ParallelTasks = 16;
			const int PerTaskIterationCount = 20;
			const int RandomSize = 1024;

			var tasks = new System.Threading.Tasks.Task[ParallelTasks];
			byte[][] taskArrays = new byte[ParallelTasks][];

			var rng = new CryptoRandom();
			using (var sync = new System.Threading.ManualResetEvent(false))
			{
				for (int iTask = 0; iTask < ParallelTasks; iTask++)
				{
					taskArrays[iTask] = new byte[RandomSize];
					byte[] taskLocal = taskArrays[iTask];

					tasks[iTask] = Task.Factory.StartNew(
						() =>
						{
							sync.WaitOne();

							for (int i = 0; i < PerTaskIterationCount; i++)
							{
								rng.NextBytes(taskLocal);
							}
						}, TaskCreationOptions.LongRunning);
				}

				// Ready? Set() Go!
				sync.Set();
				Task.WaitAll(tasks);
			}

			for (int i = 0; i < ParallelTasks; i++)
			{
				// The Real test would be to ensure independence of data, but that's difficult.
				// The other end of the spectrum is to test that they aren't all just new byte[RandomSize].
				// Middle ground is to assert that each of the chunks has neutral(ish) bit parity.
				AssertNeutralParity(taskArrays[i]);
			}
		}//CryptoRandom_ConcurrentAccess()

		#endregion
	}//class CryptoRandomTests

}//ns
