using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Helper
{
    public static class BitOperations
    {
        #region getting stuff
        public static byte GetBit(this byte number, byte position) =>
            (byte)((number >> 8 - 1 - position) & 1);

        public static byte GetBit(this uint number, byte position) =>
            (byte)((number >> 32 - 1 - position) & 1);

        public static byte GetBit(this ulong number, byte position) =>
            (byte)((number >> 64 - 1 - position) & 1);

        public static byte GetBit(this BigInteger number, byte position) =>
            (byte)((number >> number.GetByteCount() * 8 - 1 - position) & 1);

        static byte GetByteLen(byte num)
        {
            byte i = 0;
            for (; num != 0; i++)
                num = (byte)(num >> 1);
            return i;
        }
        #endregion

        #region adding stuff
        public static byte AddBit(this byte number, byte bit) =>
            (byte)((number << 1) + bit);

        public static BigInteger AddBit(this BigInteger number, byte bit) =>
            (number << 1) + bit;

        public static BigInteger AddBits(this BigInteger number, BigInteger bitsToAdd, int len) =>
            (number << len) + bitsToAdd;
        #endregion

        // It's a bit tricky to store codes in BitIntegers as it basically disregards all leading 0s
        // That's why, when you start writing into them, you should first put a 1 into it
        // After that, you can keep on adding bits as usual.
        
        // When reading, first get the length of the last byte of BigInteger's byte array.
        // Read it off. You can Read the rest as usual

    }
}
