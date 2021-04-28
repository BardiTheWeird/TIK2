using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    public static class Masks
    {
        public static byte[] RemovalMasks = new byte[]
        {
            0b_0111_1111,
            0b_1011_1111,
            0b_1101_1111,
            0b_1110_1111,
            0b_1111_0111,
            0b_1111_1011,
            0b_1111_1101,
            0b_1111_1110,
        };
    }
}
