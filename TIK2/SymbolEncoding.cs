using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace TIK2
{
    class SymbolEncoding
    {
        public byte Symbol { get; set; }
        public byte CodeLength { get; set; }
        public BigInteger Code { get; set; }
    }
}
