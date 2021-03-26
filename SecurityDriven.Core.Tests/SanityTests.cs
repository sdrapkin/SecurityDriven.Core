using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SecurityDriven.Core.Tests
{
	[TestClass]
	public class SanityTests
	{
		[DataTestMethod]
		[DataRow(1, 0, 0, 0)]
		public void VersionCheck(int major, int minor, int build, int revision)
		{
			Assembly assembly = typeof(CryptoRandom).Assembly;
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			var expectedVersion = new Version(major, minor, build, revision);

			Assert.IsTrue(new Version(fvi.ProductVersion) == expectedVersion);
			Assert.IsTrue(new Version(fvi.FileVersion) == expectedVersion);

			assembly.GetModules()[0].GetPEKind(out var kind, out var machine);
			Assert.IsTrue(kind == PortableExecutableKinds.ILOnly);

			string environment =
#if NET
				"[.NET] "
#elif NETCOREAPP
				"[NETCOREAPP] " 
#endif
				+ $"[{Environment.Version}] [{System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()}]";
			Console.WriteLine($"[{assembly.FullName}] version: [{expectedVersion}]");
			Console.WriteLine(environment);
		}//VersionCheck()
	}//class SanityTests
}//ns
