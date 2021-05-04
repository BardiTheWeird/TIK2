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
        Corrected,
    }

    enum ErrorSearchResult
    {
        SingleError,
        MultipleErrors,
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
                divided -= newDivisor;
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
            numberToEncode -= remainder;

            return numberToEncode;
        }

        static (int?, ErrorSearchResult) FindErroredBitPosition(BitBuffer buffer, BitBuffer polynomial)
        {
            int? res = null;

            for (int i = 0; i < buffer.BitLength; i++)
            {
                buffer.FlipBit(i);
                if (GetLargestBinPower(CRCLongDivisionRemainder(buffer, polynomial)) == -1)
                {
                    if (res != null)
                        return (null, ErrorSearchResult.MultipleErrors);

                    res = i;
                }
                buffer.FlipBit(i);
            }

            var searchResult = res == null ? ErrorSearchResult.MultipleErrors : ErrorSearchResult.SingleError;
            return (res, searchResult);
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
            {
                var errorSearchResult = FindErroredBitPosition(encodedBlock, polynomial);
                if (errorSearchResult.Item2 == ErrorSearchResult.SingleError)
                {
                    encodedBlock.FlipBit((int)errorSearchResult.Item1);
                    result = CRCDecodingResult.Corrected;
                }
                else
                    result = CRCDecodingResult.Corrupted;
            }

            encodedBlock.PopBits(hashSumSize);
            return (encodedBlock, result);
        }
    }
}
