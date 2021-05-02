using Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TIK2
{
    enum CRCDecodingResult
    {
        OK,
        Corrupted,
    }

    static class CRC
    {
        public static BigInteger Polynomial = 0xd175; // message length = 32751; HD = 4;

        public static int GetLargestBinPower(BigInteger num) // 001001 -> 3
        {
            var arr = num.ToByteArray();
            var power = 8 * (arr.Length - 1);
            var finByte = arr[^1];
            while (finByte > 0)
            {
                power++;
                finByte = (byte)(finByte >> 1);
            }

            return power - 1;
        }

        public static BigInteger CRCLongDivisionRemainder(BigInteger divided, BigInteger divisor)
        {
            var divisorPower = GetLargestBinPower(divisor);
            var dividedPower = GetLargestBinPower(divided);

            while (dividedPower > divisorPower)
            {
                divided = divided ^ (divisor << (dividedPower - divisorPower));
                dividedPower = GetLargestBinPower(divided);
            }

            return divisor;
        }

        public static BitBuffer EncodeCRC(BitBuffer message, BigInteger polynomial)
        {
            if (polynomial < 1)
                throw new ArgumentException($"you insane? your polynomial is {polynomial}");

            var hashSumSize = GetLargestBinPower(polynomial) + 1;

            var numberToEncode = message.ToBigInteger() << hashSumSize;
            var remainder = CRCLongDivisionRemainder(numberToEncode, polynomial);

            // subtracting the remainder
            numberToEncode = numberToEncode ^ remainder;

            var rBuffer = new BitBuffer(numberToEncode);
            var expectedLength = message.BitLength + hashSumSize;
            var lbuffer = new BitBuffer(0, expectedLength - rBuffer.BitLength);
            lbuffer.AppendBuffer(rBuffer);

            return lbuffer;
        }

        public static (BitBuffer, CRCDecodingResult) DecodeCRC(BitBuffer encodedBlock, BigInteger polynomial)
        {
            if (polynomial < 1)
                throw new ArgumentException($"you insane? your polynomial is {polynomial}");

            var hashSumSize = GetLargestBinPower(polynomial) + 1;

            if (encodedBlock.BitLength < hashSumSize)
                throw new ArgumentException($"block size is {encodedBlock.BitLength}, which is less than {hashSumSize}, " +
                    $"hash sum size for polynomial {polynomial}");

            CRCDecodingResult result = CRCDecodingResult.OK;
            if (CRCLongDivisionRemainder(encodedBlock.ToBigInteger(), polynomial) != 0)
                result = CRCDecodingResult.Corrupted;

            encodedBlock.PopBits(hashSumSize);
            return (encodedBlock, result);
        }
    }
}
