using Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TIK2
{
    class BitReader
    {
        #region properties
        public string Filepath { get; set; }
        public long FileLength { get; set; }
        public long BytesRead { get; set; } = -1;
        public long BitsRead => BytesRead * 8 + _bitIndex;
        #endregion

        #region fields
        private FileStream _fs;
        private byte[] _buffer = new byte[1];
        private byte _curByte => _buffer[0];
        private int _bitIndex = 8;
        #endregion

        #region readingStuff
        private bool ReadNextByte()
        {
            if (++BytesRead >= FileLength)
            {
                _fs.Close();
                return false;
            }

            _fs.Read(_buffer, 0, 1);
            _bitIndex = 0;

            return true;
        }

        public bool ReadBit(out byte output)
        {
            output = 0;
            if (_bitIndex > 7)
                if (!ReadNextByte())
                    return false;

            //output = _curByteBin[_bitIndex++];
            output = (byte)((_curByte >> (8 - _bitIndex - 1)) & 1);
            _bitIndex++;
            return true;
        }

        public bool ReadBits(int amount, out BitBuffer output)
        {
            output = new BitBuffer();
            byte curBit = 0;
            
            for (int i = 0; i < amount; i++)
            {
                if (!ReadBit(out curBit))
                    return false;

                output.AppendBit(curBit);
            }
            return true;
        }

        public bool ReadByte(out byte output)
        {
            output = 0;
            var res = ReadBits(8, out var bitsRead);
            if (res)
                output = bitsRead.ByteBuffer[0];
            return res;
        }

        public void StopReading() =>
            _fs.Close();
        #endregion

        #region ctor
        public BitReader(string filepath)
        {
            Filepath = filepath;
            _fs = new FileStream(filepath, FileMode.Open);
            FileLength = _fs.Length;

            if (FileLength < 1)
                throw new Exception($"The file at {filepath} is empty");
        }
        #endregion
    }
}
