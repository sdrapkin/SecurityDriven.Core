# SecurityDriven.Core

## **CryptoRandom** : Random

* **.NET Random done right**
	* Fast, Thread-safe, Cryptographically strong: choose only 3
* Subclasses and replaces `System.Random`
* Also replaces `System.Security.Cryptography.RandomNumberGenerator`
* `CryptoRandom` is (unlike `System.Random`):
	* **Fast** (faster than `Random` or `RandomNumberGenerator`)
	* **Thread-safe** (all APIs)
	* **Cryptographically strong** (seeded or unseeded)
* Implements [Fast-Key-Erasure](https://blog.cr.yp.to/20170723-random.html) RNG for seeded `CryptoRandom`.
* Achieves ~1.3cpb (cycles-per-byte) performance, similar to [AES-NI](https://en.wikipedia.org/wiki/AES_instruction_set)
* Scales per-CPU/Core
* Example: `CryptoRandom.NextGuid()` vs. `Guid.NewGuid()` [BenchmarkDotNet]:
	* 2~4x faster on Windows-x64
	* 5~30x faster on Linux-x64
	* 3~4x faster on Linux-ARM64 (AWS Graviton-2)
* Supports .NET 5.0+

## **CryptoRandom API**:
* All APIs of `System.Random`
* Static shared instance `CryptoRandom.Shared` for convenience (thread-safe of course)
* Seeded constructors:
	* `CryptoRandom(ReadOnlySpan<byte> seedKey)`
	* `CryptoRandom(int Seed)` (just like seeded `Random` ctor)
* `byte[] NextBytes(int count)`
* `Guid NextGuid()`
* `Guid SqlServerGuid()`
* `long NextInt64()`
* `long NextInt64(long maxValue)`
* `long NextInt64(long minValue, long maxValue)`
* Random `struct` 's (make sure you know what you're doing):
	* `void Next<T>(ref T @struct) where T : unmanaged`
	* `T Next<T>() where T : unmanaged`

## **Utils API** (`SecurityDriven.Core.Utils`):
* `static Span<byte> AsSpan<T>(ref T @struct) where T : unmanaged`
	* Casts unmanaged struct T as equivalent `Span<byte>`
* `static ref T AsStruct<T>(Span<byte> span) where T : unmanaged`
	* Casts `Span<byte>` as equivalent unmanaged struct T
* `int StructSizer<T>.Size`
	* Returns byte-size of struct T
