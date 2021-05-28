using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SecurityDriven.Core.Tests
{
	// Microsoft .NET Random tests: https://github.com/stephentoub/runtime/blob/main/src/libraries/System.Runtime.Extensions/tests/System/Random.cs

	[TestClass]
	public class CryptoRandomTests
	{
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
		}//InRange()
	}//class CryptoRandomTests

}//ns
