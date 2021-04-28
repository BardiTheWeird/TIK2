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
        public long BytesRead => BitsRead / 8;
        public long BitsRead { get; set; } = -1;
        #endregion

        #region fields
        private FileStream _fs;

        private int _byteChunkSize;
        private int _bitChunkSize => _byteChunkSize * 8;
        private BitBuffer _bitBuffer;
        private int _bitIndex;
        #endregion

        #region readingStuff

        public bool ReadBit(out byte output)
        {
            BitsRead++;
            if (_bitIndex >= _bitChunkSize)
            {
                _fs.Read(_bitBuffer.ByteBufferSpan.Slice(0));
                _bitIndex = 0;
            }

            output = _bitBuffer[_bitIndex++];
            return true;
        }

        public bool ReadBits(int amount, out BitBuffer output)
        {
            output = new BitBuffer();
            byte curBit;
            
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

            _byteChunkSize = ReadingWriting.MB;
            _bitIndex = _bitChunkSize;
            _bitBuffer = new BitBuffer();
            _bitBuffer.FillWithZeroesBytes(_byteChunkSize);
        }
        #endregion
    }
}
