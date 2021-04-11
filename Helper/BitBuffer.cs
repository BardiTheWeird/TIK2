using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections;
using System.Linq;

namespace Helper
{
    public class BitBuffer
    {
        public List<byte> ByteBuffer { get; set; }
        public Span<byte> ByteBufferSpan => CollectionsMarshal.AsSpan(ByteBuffer);
        public Span<byte> GetFullBytesSpan => ByteBufferSpan.Slice(0, BitLength / 8);
        public int BitLength { get; set; }
        public int FullBytes => BitLength / 8;

        #region appendStuff
        public void AppendBit(byte bit)
        {
            if (BitLength % 8 == 0)
                ByteBuffer.Add(bit);
            else
                ByteBuffer[^1] = ByteBuffer[^1].AddBit(bit);

            BitLength++;
        }

        public void AppendByte(byte num)
        {
            if (BitLength % 8 == 0)
            {
                ByteBuffer.Add(num);
            }
            else
            {
                ShiftLastByteToWritableState();
                var lenLeft = (8 - (BitLength % 8)) % 8;
                ByteBuffer[^1] += (byte)(num >> (8 - lenLeft));
                ByteBuffer.Add((byte)(((num << lenLeft) & 0xff) >> lenLeft));
            }
            BitLength += 8;
        }

        public void AppendPartialByte(byte num, byte len)
        {
            num = (byte)(num << (8 - len));
            for (byte i = 0; i < len; i++)
                AppendBit(num.GetBit(i));
        }

        public void AppendBuffer(BitBuffer buffer)
        {
            if (buffer.BitLength < 1)
                return;

            for (int i = 0; i < buffer.ByteBuffer.Count - 1; i++)
                AppendByte(buffer.ByteBuffer[i]);

            var lastByte = buffer.ByteBuffer[^1];
            byte lastByteLen = (byte)(buffer.BitLength % 8);
            if (lastByteLen == 0)
                AppendByte(lastByte);
            else
                AppendPartialByte(lastByte, lastByteLen);
        }
        #endregion

        public byte PopBit()
        {
            if (BitLength < 1)
                throw new ArgumentException("No more popping is available at the time");

            var output = (byte)(ByteBuffer[^1] & 1);
            ByteBuffer[^1] = (byte)(ByteBuffer[^1] >> 1);
            BitLength--;
            if (BitLength % 8 == 0)
                ByteBuffer.RemoveAt(ByteBuffer.Count - 1);

            return output;
        }

        public byte this[int index]
        {
            get 
            {
                var byteIndex = index / 8;
                byte bitOffset = (byte)(index % 8);

                if (byteIndex == ByteBuffer.Count - 1 && BitLength % 8 != 0)
                {
                    var byteInQuestion = (byte)(ByteBuffer[^1] << (8 - BitLength % 8));
                    return byteInQuestion.GetBit(bitOffset);
                }

                return ByteBuffer[byteIndex].GetBit(bitOffset);
            }
        }

        public void ShiftLastByteToWritableState() =>
            ByteBuffer[^1] = (byte)(ByteBuffer[^1] << ((8 - (BitLength % 8)) % 8));

        public void FillWithZeroes(int count)
        {
            ByteBuffer = Enumerable.Repeat<byte>(0, count).ToList();
            BitLength = count * 8;
        }
        
        public void ClearAllFullBytes()
        {
            if (BitLength % 8 == 0)
            {
                ByteBuffer.Clear();
                BitLength = 0;
            }
            else
            {
                var temp = ByteBuffer[^1];
                ByteBuffer.Clear();
                BitLength = BitLength % 8;
                ByteBuffer.Add(temp);
            }
        }

        public void FullClear()
        {
            ByteBuffer.Clear();
            BitLength = 0;
        }

        public override string ToString()
        {
            if (BitLength % 8 == 0)
                return string.Join("_", ByteBuffer.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

            return string.Join('_', Enumerable.Range(0, ByteBuffer.Count - 1)
                .Select(i => Convert.ToString(ByteBuffer[i], 2).PadLeft(8, '0'))
                .Append(Convert.ToString(ByteBuffer[^1], 2).PadLeft(BitLength % 8, '0')));
        }

        public BitBuffer()
        {
            ByteBuffer = new List<byte>(4);
        }
    }
}
