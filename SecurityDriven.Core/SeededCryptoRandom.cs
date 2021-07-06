using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;

namespace SecurityDriven.Core
{
	internal sealed class SeededCryptoRandom : CryptoRandom.CryptoRandomBase
	{
		public const int SEEDKEY_SIZE = 32;
		public const int BUFFER_SIZE = SEEDKEY_SIZE + (1024 * 8 - SEEDKEY_SIZE); // must be a multiple of AES_BLOCK_SIZE, and greater than SEEDKEY_SIZE
		const int AES_BLOCK_SIZE = 16;

		static readonly byte[] s_ptBuffer;
		static readonly Aes s_aes;

		readonly byte[] _aeskey;
		readonly byte[] _ctBuffer;
		int _ctIndex;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static SeededCryptoRandom()
		{
			var ptBuffer = new byte[BUFFER_SIZE];

			// Create nonces for s_ptBuffer. Ensure consistent endianness.
			if (BitConverter.IsLittleEndian)
			{
				for (uint i = 1; i < (BUFFER_SIZE / AES_BLOCK_SIZE); ++i)
				{
					Unsafe.WriteUnaligned<uint>(ref ptBuffer[i * AES_BLOCK_SIZE + (AES_BLOCK_SIZE - sizeof(uint))], BinaryPrimitives.ReverseEndianness(i));
				}
			}
			else
			{
				for (uint i = 1; i < (BUFFER_SIZE / AES_BLOCK_SIZE); ++i)
				{
					Unsafe.WriteUnaligned<uint>(ref ptBuffer[i * AES_BLOCK_SIZE + (AES_BLOCK_SIZE - sizeof(uint))], i);
				}
			}
			s_ptBuffer = ptBuffer;

			var aes = Aes.Create();
			aes.Dispose(); // just to make a point that Aes itself does not hold onto any resources, and still serves its purpose after disposal
			aes.Mode = CipherMode.ECB;
			aes.Padding = PaddingMode.None;
			s_aes = aes;
		}//static ctor

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SeededCryptoRandom(ReadOnlySpan<byte> seedKey)
		{
			if (seedKey.Length != SEEDKEY_SIZE)
				Action_Throw_SeedKeyOutOfRangeException();

			_aeskey = new byte[SEEDKEY_SIZE];
			_ctBuffer = GC.AllocateUninitializedArray<byte>(BUFFER_SIZE);

			Reseed(seedKey);
			NextBytes(Span<byte>.Empty); // trigger first _ctBuffer generation
		}//ctor

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void Action_Throw_SeedKeyOutOfRangeException() =>
			throw new ArgumentOutOfRangeException(paramName: "seedKey", message: "Seed must be " + SEEDKEY_SIZE + " bytes long.");

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void Reseed(ReadOnlySpan<byte> seedKey)
		{
			if (seedKey.Length != SEEDKEY_SIZE)
				Action_Throw_SeedKeyOutOfRangeException();

			lock (_ctBuffer)
			{
				seedKey.CopyTo(_aeskey);
				_ctIndex = BUFFER_SIZE;
			}
		}//Reseed(seedKey)

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void NextBytes(Span<byte> buffer)
		{
			int requestedBytesRemaining = buffer.Length;
			byte[] ctBuffer = _ctBuffer;

			bool lockTaken = false;
			try
			{
				Monitor.Enter(ctBuffer, ref lockTaken);
				do
				{
					int bytesAvailable = BUFFER_SIZE - _ctIndex;
					if (bytesAvailable == 0)
					{
						fnLocalReseed(_aeskey, ctBuffer);
						_ctIndex = SEEDKEY_SIZE;
						bytesAvailable = BUFFER_SIZE - SEEDKEY_SIZE;
					}
					if (requestedBytesRemaining == 0) return;

					if (requestedBytesRemaining < bytesAvailable)
						bytesAvailable = requestedBytesRemaining;

					int ctIndexOld = _ctIndex;
					_ctIndex = ctIndexOld + bytesAvailable;

					Unsafe.CopyBlockUnaligned(destination: ref buffer[^requestedBytesRemaining], source: ref ctBuffer[ctIndexOld], byteCount: (uint)bytesAvailable);
					requestedBytesRemaining -= bytesAvailable;
				} while (true);
			}
			finally
			{
				if (lockTaken) Monitor.Exit(ctBuffer);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static void fnLocalReseed(byte[] aeskey, byte[] ctBuffer)
			{
				var encryptor = s_aes.CreateEncryptor(rgbKey: aeskey, rgbIV: null);
				encryptor.TransformBlock(inputBuffer: s_ptBuffer, inputOffset: 0, inputCount: BUFFER_SIZE, outputBuffer: ctBuffer, outputOffset: 0);
				encryptor.Dispose();

				ref var ctBufferRef0 = ref ctBuffer[0];
				Unsafe.CopyBlockUnaligned(destination: ref aeskey[0], source: ref ctBufferRef0, byteCount: SEEDKEY_SIZE);
				Unsafe.InitBlockUnaligned(startAddress: ref ctBufferRef0, value: 0, byteCount: SEEDKEY_SIZE);
			}//fnLocalReseed()
		}//NextBytes(Span<byte>)
	}//class SeededCryptoRandom
}//ns
