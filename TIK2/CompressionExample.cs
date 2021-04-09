using System;
using System.Collections.Generic;
using System.Text;

namespace TIK2
{
    class CompressionExample
    {
		public class BitStream
		{
			public byte[] BytePointer;
			public uint BitPosition;
			public uint Index;
		}

		public struct Symbol
		{
			public uint Sym;
			public uint Count;
			public uint Code;
			public uint Bits;
		}

		private static void initBitStream(ref BitStream stream, byte[] buffer)
		{
			stream.BytePointer = buffer;
			stream.BitPosition = 0;
		}

		private static void writeBits(ref BitStream stream, uint x, uint bits)
		{
			byte[] buffer = stream.BytePointer;
			uint bit = stream.BitPosition;
			uint mask = (uint)(1 << (int)(bits - 1));

			for (uint count = 0; count < bits; ++count)
			{
				buffer[stream.Index] = 
					(byte)((buffer[stream.Index] & (0xff ^ (1 << (int)(7 - bit)))) + ((Convert.ToBoolean(x & mask) ? 1 : 0) << (int)(7 - bit)));
				x <<= 1;
				bit = (bit + 1) & 7;

				if (!Convert.ToBoolean(bit))
				{
					++stream.Index;
				}
			}

			stream.BytePointer = buffer;
			stream.BitPosition = bit;
		}

		private static void histogram(byte[] input, Symbol[] sym, uint size)
		{
			Symbol temp;
			int i, swaps;
			int index = 0;

			for (i = 0; i < 256; ++i)
			{
				sym[i].Sym = (uint)i;
				sym[i].Count = 0;
				sym[i].Code = 0;
				sym[i].Bits = 0;
			}

			for (i = (int)size; Convert.ToBoolean(i); --i, ++index)
			{
				sym[input[index]].Count++;
			}

			do
			{
				swaps = 0;

				for (i = 0; i < 255; ++i)
				{
					if (sym[i].Count < sym[i + 1].Count)
					{
						temp = sym[i];
						sym[i] = sym[i + 1];
						sym[i + 1] = temp;
						swaps = 1;
					}
				}
			} while (Convert.ToBoolean(swaps));
		}

		private static void makeTree(Symbol[] sym, ref BitStream stream, uint code, uint bits, uint first, uint last)
		{
			uint i, size, sizeA, sizeB, lastA, firstB;

			if (first == last)
			{
				writeBits(ref stream, 1, 1);
				writeBits(ref stream, sym[first].Sym, 8);
				sym[first].Code = code;
				sym[first].Bits = bits;
				return;
			}
			else
			{
				writeBits(ref stream, 0, 1);
			}

			size = 0;

			for (i = first; i <= last; ++i)
			{
				size += sym[i].Count;
			}

			sizeA = 0;

			for (i = first; sizeA < ((size + 1) >> 1) && i < last; ++i)
			{
				sizeA += sym[i].Count;
			}

			if (sizeA > 0)
			{
				writeBits(ref stream, 1, 1);

				lastA = i - 1;

				makeTree(sym, ref stream, (code << 1) + 0, bits + 1, first, lastA);
			}
			else
			{
				writeBits(ref stream, 0, 1);
			}

			sizeB = size - sizeA;

			if (sizeB > 0)
			{
				writeBits(ref stream, 1, 1);

				firstB = i;

				makeTree(sym, ref stream, (code << 1) + 1, bits + 1, firstB, last);
			}
			else
			{
				writeBits(ref stream, 0, 1);
			}
		}

		public static int Compress(byte[] input, byte[] output, uint inputSize)
		{
			Symbol[] sym = new Symbol[256];
			Symbol temp;
			BitStream stream = new BitStream();
			uint i, totalBytes, swaps, symbol, lastSymbol;

			if (inputSize < 1)
				return 0;

			initBitStream(ref stream, output);
			histogram(input, sym, inputSize);

			for (lastSymbol = 255; sym[lastSymbol].Count == 0; --lastSymbol) ;

			if (lastSymbol == 0)
				++lastSymbol;

			makeTree(sym, ref stream, 0, 0, 0, lastSymbol);

			do
			{
				swaps = 0;

				for (i = 0; i < 255; ++i)
				{
					if (sym[i].Sym > sym[i + 1].Sym)
					{
						temp = sym[i];
						sym[i] = sym[i + 1];
						sym[i + 1] = temp;
						swaps = 1;
					}
				}
			} while (Convert.ToBoolean(swaps));

			for (i = 0; i < inputSize; ++i)
			{
				symbol = input[i];
				writeBits(ref stream, sym[symbol].Code, sym[symbol].Bits);
			}

			totalBytes = stream.Index;

			if (stream.BitPosition > 0)
			{
				++totalBytes;
			}

			return (int)totalBytes;
		}
	}
}
