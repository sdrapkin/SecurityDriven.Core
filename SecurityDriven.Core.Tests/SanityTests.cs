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
		[DataRow(1, 0, 8 /*, "alpha" */)]
		public void VersionCheck(int major, int minor, int build, string releaseType = "")
		{
			Assembly assembly = typeof(CryptoRandom).Assembly;
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			var expectedFileVersion = new Version(major, minor, build, 0);

			Assert.IsTrue(new Version(fvi.FileVersion) == expectedFileVersion);
			var expectedProductVersion = $"{major}.{minor}.{build}";

			if (!string.IsNullOrEmpty(releaseType))
				expectedProductVersion += $"-{releaseType}";
			Assert.IsTrue(fvi.ProductVersion == expectedProductVersion);

			assembly.GetModules()[0].GetPEKind(out var kind, out var machine);
			Assert.IsTrue(kind == PortableExecutableKinds.ILOnly);

			string environment =
#if NET
				"[.NET] "
#elif NETCOREAPP
				"[NETCOREAPP] " 
#endif
				+ $"[{Environment.Version}] [{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}] [{System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()}]";
			Console.WriteLine($"[{assembly.FullName}] file-version: [{expectedFileVersion}] product-version: [{expectedProductVersion}]");
			Console.WriteLine(environment);
		}//VersionCheck()
	}//class SanityTests
}//ns
