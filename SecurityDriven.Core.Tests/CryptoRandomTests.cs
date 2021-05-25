using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
