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
        private string _curByteBin;
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
            _curByteBin = Convert.ToString(_curByte, 2).PadLeft(8, '0');
            _bitIndex = 0;

            return true;
        }

        public bool ReadBit(out char output)
        {
            output = 'a';
            if (_bitIndex > 7)
                if (!ReadNextByte())
                    return false;

            output = _curByteBin[_bitIndex++];
            return true;
        }

        public bool ReadBits(int amount, out string output)
        {
            var sb = new StringBuilder();
            char curBit = 'a';
            for (int i = 0; i < amount && ReadBit(out curBit); i++)
                sb.Append(curBit);

            output = sb.ToString();
            if (output.Length < amount)
                return false;

            return true;
        }

        public bool ReadByte(out byte output)
        {
            output = 0;
            var res = ReadBits(8, out var stringRead);
            if (res)
                output = Convert.ToByte(stringRead, 2);
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
