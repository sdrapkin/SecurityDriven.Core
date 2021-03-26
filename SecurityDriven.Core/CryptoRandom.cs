using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

namespace SecurityDriven.Core
{
	/// <summary>Implements a fast, *thread-safe*, cryptographically-strong pseudo-random number generator.</summary>
	public partial class CryptoRandom
	{
		public const int BYTE_CACHE_SIZE = 4096; // 4k buffer seems to work best (empirical experimentation).
		public const int REQUEST_CACHE_LIMIT = BYTE_CACHE_SIZE / 4; //  Must be less than BYTE_CACHE_SIZE.
		const int PADDING_FACTOR_POWER_OF2 = 4; // ([64-byte cache line] / [4-byte int]) is 16, which is 2^4.

		readonly int[] _byteCachePositions = new int[Environment.ProcessorCount << PADDING_FACTOR_POWER_OF2];
		readonly byte[][] _byteCaches = new byte[Environment.ProcessorCount][];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CryptoRandom()
		{
			for (int i = 0, procCount = Environment.ProcessorCount; i < procCount; ++i)
			{
				_byteCachePositions[i << PADDING_FACTOR_POWER_OF2] = BYTE_CACHE_SIZE;
			}
		}//ctor

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void FillSpan(Span<byte> span)
		{
			int count = span.Length;
			if (count > REQUEST_CACHE_LIMIT)
			{
				RandomNumberGenerator.Fill(span);
				return;
			}

			int procId = Environment.ProcessorCount == 1 ? 0 : Thread.GetCurrentProcessorId();
			byte[] byteCacheLocal = _byteCaches[procId];

			if (byteCacheLocal == null)
			{
				Interlocked.CompareExchange(ref Unsafe.As<byte[], object>(ref _byteCaches[procId]), new byte[BYTE_CACHE_SIZE], null);
				byteCacheLocal = _byteCaches[procId];
			}

			ref int byteCachePositionLocalRef = ref _byteCachePositions[procId << PADDING_FACTOR_POWER_OF2];
			bool lockTaken = false;

			try
			{
				Monitor.Enter(byteCacheLocal, ref lockTaken);
				if (byteCachePositionLocalRef + count > BYTE_CACHE_SIZE)
				{
					RandomNumberGenerator.Fill(new Span<byte>(byteCacheLocal));
					byteCachePositionLocalRef = 0;
				}

				ref byte byteCacheLocalStartRef = ref byteCacheLocal[byteCachePositionLocalRef];
				Unsafe.CopyBlockUnaligned(destination: ref MemoryMarshal.GetReference(span), source: ref byteCacheLocalStartRef, byteCount: (uint)count);
				Unsafe.InitBlockUnaligned(startAddress: ref byteCacheLocalStartRef, value: 0, byteCount: (uint)count);

				byteCachePositionLocalRef += count;
			}
			finally
			{
				if (lockTaken) Monitor.Exit(byteCacheLocal);
			}
		}//FillSpan()

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void FillStruct<T>(ref T structure) where T : unmanaged
		{
			this.FillSpan(MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref structure), Utils.StructSizer<T>.Size));
		}//FillStruct<T>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Guid NewSqlServerGuid()
		{
			Span<byte> guidSpan = stackalloc byte[16];
			this.FillSpan(guidSpan.Slice(0, 8));

			DateTime utcNow = DateTime.UtcNow;
			Span<byte> ticksSpan = MemoryMarshal.CreateSpan(ref Unsafe.As<DateTime, byte>(ref utcNow), 8);

			// based on Microsoft SqlGuid.cs
			// https://github.com/microsoft/referencesource/blob/5697c29004a34d80acdaf5742d7e699022c64ecd/System.Data/System/Data/SQLTypes/SQLGuid.cs

			guidSpan[10] = ticksSpan[7];
			guidSpan[11] = ticksSpan[6];
			guidSpan[12] = ticksSpan[5];
			guidSpan[13] = ticksSpan[4];
			guidSpan[14] = ticksSpan[3];
			guidSpan[15] = ticksSpan[2];

			guidSpan[08] = ticksSpan[1];
			guidSpan[09] = ticksSpan[0];

			return Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(guidSpan));
		}//NewSqlServerGuid()
	}//class CryptoRandom
}//ns
