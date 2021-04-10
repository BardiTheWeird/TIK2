using Helper;
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
        private BitBuffer _buffer;
        private int _writeThreshold = 20;
        #endregion

        #region writing
        private void MaybeWriteToFile()
        {
            if (_buffer.FullBytes < _writeThreshold)
                return;

            _fs.Write(_buffer.GetFullBytesSpan);
            _buffer.ClearAllFullBytes();
        }

        public void WriteByte(byte num)
        {
            _buffer.AppendByte(num);
            MaybeWriteToFile();
        }

        public void WriteBuffer(BitBuffer buffer)
        {
            _buffer.AppendBuffer(buffer);
            MaybeWriteToFile();
        }

        public void WriteTheRestOfTheBuffer()
        {
            _buffer.ShiftLastByteToWritableState();
            _fs.Write(_buffer.GetByteBufferSpan);
            _buffer.FullClear();
        }

        public void StopWriting() =>
            _fs.Close();
        #endregion

        #region ctor
        public BitWriter(string filepath)
        {
            Filepath = filepath;
            _fs = new FileStream(filepath, FileMode.Create);
            _buffer = new BitBuffer();
        }
        #endregion
    }
}
