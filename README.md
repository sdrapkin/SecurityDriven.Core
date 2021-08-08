# **CryptoRandom** [![GitHub Actions](https://github.com/sdrapkin/SecurityDriven.Core/workflows/.NET%205%20CI/badge.svg)](https://github.com/sdrapkin/SecurityDriven.Core/actions) [![NuGet](https://img.shields.io/nuget/v/CryptoRandom.svg)](https://www.nuget.org/packages/CryptoRandom/)

### by [Stan Drapkin](https://github.com/sdrapkin/)

## **CryptoRandom** : Random

* **.NET Random done right**
* [Fast], [Thread-safe], [Cryptographically strong]: choose 3
* Subclasses and replaces `System.Random`
* Also replaces `System.Security.Cryptography.RandomNumberGenerator` ([link](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator))
* `CryptoRandom` is (unlike `System.Random`):
	* **Fast** (much faster than `Random` or `RandomNumberGenerator`)
	* **Thread-safe** (all APIs)
	* **Cryptographically strong** (seeded or unseeded)
* Implements [Fast-Key-Erasure](https://blog.cr.yp.to/20170723-random.html) RNG for seeded `CryptoRandom`
* Produces *the same* sequence of seeded `CryptoRandom` values on *all* .NET versions (unlike `Random`)
* Wraps `RandomNumberGenerator` for unseeded (with additional smarts)
* Provides *backtracking resistance*: internal state cannot be used to recover the output
* Achieves ~1.3cpb (cycles-per-byte) performance, similar to [AES-NI](https://en.wikipedia.org/wiki/AES_instruction_set)
* Scales per-CPU/Core
* Example: `CryptoRandom.NextGuid()` vs. `Guid.NewGuid()` [BenchmarkDotNet]:
	* 2~4x faster on Windows-x64
	* 5~30x faster on Linux-x64
	* 3~4x faster on Linux-ARM64 (AWS Graviton-2)
* Built for .NET 5.0+ and 6.0+
* Extensive test coverage & correctness validation

---
## **CryptoRandom API**:
* All APIs of `System.Random` ([link](https://docs.microsoft.com/en-us/dotnet/api/system.random))
* `CryptoRandom.Shared` static shared instance for convenience (thread-safe of course)
* Seeded constructors:
	* `CryptoRandom(ReadOnlySpan<byte> seedKey)`
	* `CryptoRandom(int Seed)` (just like seeded `Random` ctor)
* `byte[] NextBytes(int count)`
* `Guid NextGuid()` - 2x faster than `Guid.NewGuid()`
* `Guid SqlServerGuid()`
	* Returns new Guid well-suited to be used as a SQL-Server clustered key
	* Guid structure is `[8 random bytes][8 bytes of SQL-Server-ordered DateTime.UtcNow]`
	* Each Guid should be sequential within 100-nanoseconds `UtcNow` precision limits
	* 64-bit entropy for reasonable unguessability and protection against online brute-force attacks
	* ~15% faster than `Guid.NewGuid()`
* `long NextInt64()`
* `long NextInt64(long maxValue)`
* `long NextInt64(long minValue, long maxValue)`
* Random `struct` 's (make sure you know what you're doing):
	* `void Next<T>(ref T @struct) where T : unmanaged`
	* `T Next<T>() where T : unmanaged`
---
## **Utils API** (`SecurityDriven.Core.Utils`):
* `static Span<byte> AsSpan<T>(ref T @struct) where T : unmanaged`
	* Casts unmanaged struct T as equivalent `Span<byte>`
* `static ref T AsStruct<T>(Span<byte> span) where T : unmanaged`
	* Casts `Span<byte>` as equivalent unmanaged struct T
* `int StructSizer<T>.Size`
	* Returns byte-size of struct T

---
## **Quick benchmark**:
```csharp
// using System.Threading.Tasks;
// using SecurityDriven.Core;

var sw = new Stopwatch();
const long ITER = 100_000_000, REPS = 5;

for (int i = 0; i < REPS; ++i)
{
	sw.Restart();
	Parallel.For(0, ITER, static i => CryptoRandom.Shared.NextGuid());
	Console.WriteLine($"{sw.Elapsed} cryptoRandom.NextGuid()");
}
Console.WriteLine(new string('=', 40));
for (int i = 0; i < REPS; ++i)
{
	sw.Restart();
	Parallel.For(0, ITER, i => Guid.NewGuid());
	Console.WriteLine($"{sw.Elapsed} Guid.NewGuid()");
}
```

---
## **What's wrong with `Random` and `RandomNumberGenerator`**?
* `Random` is slow and not thread-safe (fails miserably and silently on concurrent access)
* `Random` is incorrectly implemented:
```csharp
Random r = new Random(); // new CryptoRandom();
const int mod = 2;
int[] hist = new int[mod];
for (int i = 0; i < 10000000; i++)
{
	int num = r.Next(0x55555555);
	int num2 = num % 2;
	++hist[num2];
}
for (int i = 0; i < mod; i++)
	Console.WriteLine($"{i}: {hist[i]}");
// Run this on .NET 5 or below. Surprised? Now change to CryptoRandom
// Fails on .NET 6 if you use seeded "new Random(seed)"
```
* `Random`/.NET 6 unseeded is fast (new algorithm), with a safe `.Shared` property, but instances are not thread-safe
* `Random`/.NET 6 seeded falls back to legacy slow non-thread-safe .NET algorithm
* Neither `Random` implementation aims for cryptographically-strong results
* `RandomNumberGenerator` can be much faster with intelligent wrapping and more useful `Random` API

---
## **Throughput (single-threaded):**
```
BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19042.985 (20H2/October2020Update)
Intel Core i7-10510U CPU 1.80GHz, 1 CPU, 8 logical and 4 physical cores

[Host] : .NET 6.0.0 (6.0.21.30105), X64 RyuJIT
|             Method |       Mean |     Error |    StdDev |    Throughput |
|------------------- |-----------:|----------:|----------:|--------------:|
|       SystemRandom |   139.6 μs |   2.47 μs |   2.31 μs | 7,164.57 MB/s |
| SeededSystemRandom | 9,359.2 μs | 184.64 μs | 219.80 μs |   106.85 MB/s |
|       CryptoRandom |   431.0 μs |   5.41 μs |   4.52 μs | 2,320.18 MB/s |
| SeededCryptoRandom |   384.1 μs |   7.66 μs |  12.15 μs | 2,603.38 MB/s |

[Host] : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT
|             Method |       Mean |     Error |    StdDev |    Throughput |
|------------------- |-----------:|----------:|----------:|--------------:|
|       SystemRandom | 8,221.7 μs | 121.76 μs | 101.67 μs |   121.63 MB/s |
| SeededSystemRandom | 8,202.1 μs |  42.20 μs |  39.47 μs |   121.92 MB/s |
|       CryptoRandom |   416.1 μs |   8.03 μs |   8.25 μs | 2,403.24 MB/s |
| SeededCryptoRandom |   360.6 μs |   7.03 μs |   6.57 μs | 2,772.92 MB/s |
```
