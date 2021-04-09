using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TIK2
{
    class BitWriter
    {
        #region properties
        public string Filepath { get; set; }
        public long BitsWritten { get; set; } = 0;
        #endregion

        #region fields
        private FileStream _fs;
        private byte[] _byteBuffer = new byte[1];

        private char[] _bitBuffer = new char[8];
        private int _bitIndex = 0;
        #endregion

        #region writing
        private void WriteByte()
        {
            _byteBuffer[0] = Convert.ToByte(new string(_bitBuffer), 2);
            _fs.Write(_byteBuffer, 0, 1);

            _bitIndex = 0;
        }

        public void WriteBit(char c)
        {
            _bitBuffer[_bitIndex] = c;
            if (++_bitIndex == 8)
                WriteByte();

            BitsWritten++;
        }

        public void WriteBits(string bits)
        {
            foreach (var bit in bits)
                WriteBit(bit);
        }

        public void DumpCharBuffer()
        {
            if (_bitIndex > 0)
            {
                for (int i = _bitIndex; i < 8; i++)
                    _bitBuffer[i] = '0';
                WriteByte();
            }
        }

        public void StopWriting() =>
            _fs.Close();
        #endregion

        #region ctor
        public BitWriter(string filepath)
        {
            Filepath = filepath;
            _fs = new FileStream(filepath, FileMode.Create);
        }
        #endregion
    }
}
