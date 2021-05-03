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

    static class CRCMath
    {
        public static BitBuffer Polynomial = new BitBuffer(0xd175); // message length = 32751; HD = 4;

        public static int GetLargestBinPower(BitBuffer num) // 001001 -> 3
        {
            var search = num.Enumerate().FirstOrDefault(pair => pair.Item2 == 1);

            return search == default((int, byte)) ? -1 : num.BitLength - search.Item1 - 1;
        }

        public static BitBuffer CRCLongDivisionRemainder(BitBuffer divided, BitBuffer divisor)
        {
            var divisorPower = GetLargestBinPower(divisor);
            var dividedPower = GetLargestBinPower(divided);

            while (dividedPower > divisorPower)
            {
                var newDivisor = new BitBuffer(divisor).PadRight(0, dividedPower - divisorPower);
                divided.Subtract(newDivisor);
                dividedPower = GetLargestBinPower(divided);
            }

            return divided;
        }

        public static BitBuffer EncodeCRC(BitBuffer message, BitBuffer polynomial)
        {
            var hashSumSize = GetLargestBinPower(polynomial) + 1;

            var numberToEncode = new BitBuffer(message).PadRight(0, hashSumSize);
            var remainder = CRCLongDivisionRemainder(numberToEncode, polynomial)[^hashSumSize..];

            // subtracting the remainder
            numberToEncode.Subtract(remainder);
            //numberToEncode -= remainder;

            return numberToEncode;
        }

        public static (BitBuffer, CRCDecodingResult) DecodeCRC(BitBuffer encodedBlock, BitBuffer polynomial)
        {
            var hashSumSize = GetLargestBinPower(polynomial) + 1;

            if (encodedBlock.BitLength < hashSumSize)
                throw new ArgumentException($"block size is {encodedBlock.BitLength}, which is less than {hashSumSize}, " +
                    $"hash sum size for polynomial {polynomial}");

            CRCDecodingResult result = CRCDecodingResult.OK;
            var remainder = CRCLongDivisionRemainder(encodedBlock, polynomial);
            if (GetLargestBinPower(remainder) != -1)
                result = CRCDecodingResult.Corrupted;

            encodedBlock.PopBits(hashSumSize);
            return (encodedBlock, result);
        }
    }
}
