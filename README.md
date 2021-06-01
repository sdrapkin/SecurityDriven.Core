# SecurityDriven.Core [![NuGet](https://img.shields.io/nuget/v/CryptoRandom.svg)](https://www.nuget.org/packages/CryptoRandom/)

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
* Achieves ~1.3cpb (cycles-per-byte) performance, similar to [AES-NI](https://en.wikipedia.org/wiki/AES_instruction_set)
* Scales per-CPU/Core
* Example: `CryptoRandom.NextGuid()` vs. `Guid.NewGuid()` [BenchmarkDotNet]:
	* 2~4x faster on Windows-x64
	* 5~30x faster on Linux-x64
	* 3~4x faster on Linux-ARM64 (AWS Graviton-2)
* Built for .NET 5.0+ and 6.0+
* Full test coverage & correctness validation

---
## **CryptoRandom API**:
* All APIs of `System.Random` ([link](https://docs.microsoft.com/en-us/dotnet/api/system.random))
* `CryptoRandom.Shared` static shared instance for convenience (thread-safe of course)
* Seeded constructors:
	* `CryptoRandom(ReadOnlySpan<byte> seedKey)`
	* `CryptoRandom(int Seed)` (just like seeded `Random` ctor)
* `byte[] NextBytes(int count)`
* `Guid NextGuid()`
* `Guid SqlServerGuid()`
	* Returns new Guid well-suited to be used as a SQL-Server clustered key
	* Guid structure is `[8 random bytes][8 bytes of SQL-Server-ordered DateTime.UtcNow]`
	* Each Guid should be sequential within 100-nanoseconds `UtcNow` precision limits
	* 64-bit entropy for reasonable unguessability and protection against online brute-force attacks
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
