using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Helper
{
    public static class BitOperations
    {
        public static byte GetBit(this byte number, byte position) =>
            (byte)((number >> 8 - 1 - position) & 1);

        public static byte AddBit(this byte number, byte bit) =>
            (byte)((number << 1) + bit);
    }
}
