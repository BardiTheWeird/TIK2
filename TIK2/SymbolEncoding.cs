using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Helper;

namespace TIK2
{
    class SymbolEncoding
    {
        public byte Symbol { get; set; }
        public BitBuffer Code { get; set; } = new BitBuffer();

        public SymbolEncoding() { }

        public SymbolEncoding(byte symbol)
        {
            Symbol = symbol;
        }

        public SymbolEncoding(byte symbol, BitBuffer code)
        {
            Symbol = symbol;
            Code = code;
        }
    }
}
