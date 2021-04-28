using Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIK2
{
    static class HammingCodes
    {
        public static int GetInformationBlockSize(int encodedBlockSize) =>
            (int)(encodedBlockSize - Math.Log2(encodedBlockSize) - 1);

        public static BitBuffer EncodeHamming(BitBuffer message)
        {
            if (message.BitLength < 4)
                throw new ArgumentException("bitlength is less than 4");

            var finalLength = (int)(Math.Ceiling(Math.Log2(message.BitLength)) + 1 + message.BitLength);
            if (finalLength == 7)
                finalLength = 8;
            else if (!HelperMath.IsPowerOfTwo(finalLength))
                throw new ArgumentException("bitlength is invalid for information bits in hamming coding");

            var parityBitsNum = (int)Math.Log2(finalLength);

            // setting up the parity bits (as 0)
            var newBufferArr = new byte[finalLength];
            var infoCoordinates = Enumerable.Range(0, finalLength)
                .Where(i => i != 0 && !HelperMath.IsPowerOfTwo(i));

            var threaded = infoCoordinates.Zip(message.Enumerate(), (i, enumerated) => (i, enumerated.Item2));
            foreach (var (i, bit) in threaded)
                newBufferArr[i] = bit;

            message = new BitBuffer();
            foreach (var bit in newBufferArr)
                message.AppendBit(bit);

            var xorRes = message.Enumerate()
                .Where(x => x.Item2 == 1)
                .Select(x => x.Item1)
                .Aggregate(0, (x, y) => x ^ y);

            for (int i = 0; i < parityBitsNum; i++)
            {
                var parity = xorRes & 1;
                xorRes = xorRes >> 1;

                message[(int)Math.Pow(2, i)] = (byte)parity;
            }

            message[0] = (byte)(message.Enumerate()
                .Select(x => x.Item2)
                .Sum(x => x) % 2);

            return message;
        }


        public enum DecodingResult
        {
            Fail,
            Sucess,
            PossibleMistake,
        }

        public static (BitBuffer, DecodingResult, string) DecodeHamming(BitBuffer message)
        {
            var parityBitsNumDouble = Math.Log2(message.BitLength);
            var parityBitsNum = (int)parityBitsNumDouble;

            if (parityBitsNumDouble > parityBitsNum)
                return (null, DecodingResult.Fail, $"message length is {message.BitLength}, not a power of 2");

            var onesPositions = message.Enumerate()
                .Where(x => x.Item2 == 1)
                .Select(x => x.Item1);

            var mistakePosition = onesPositions.Aggregate(0, (x, y) => x ^ y);
            if (mistakePosition != 0)
            {
                var wrongBit = message[mistakePosition];
                message[mistakePosition] = (byte)(wrongBit ^ 1);
            }

            var decodedMessage = new BitBuffer();

            var parityBitsPositions = Enumerable.Repeat(0, 1)
                .Concat(Enumerable.Range(0, parityBitsNum)
                    .Select(i => (int)Math.Pow(2, i)));
            var messageBits = message.Enumerate()
                .Where(x => !parityBitsPositions.Contains(x.Item1))
                .Select(x => x.Item2);

            foreach (var bit in messageBits)
                decodedMessage.AppendBit(bit);

            var totalParity = onesPositions.Count() % 2;
            if (totalParity != 0)
            {
                string log;
                if (mistakePosition == 0)
                    log = "2n mistakes";
                else
                    log = "3 + 2n mistakes";
                return (decodedMessage, DecodingResult.PossibleMistake, log);
            }

            return (decodedMessage, DecodingResult.Sucess, string.Empty);
        }
    }
}
