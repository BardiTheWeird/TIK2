using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections;

namespace Helper
{
    public class BitBuffer
    {
        public List<byte> ByteBuffer { get; set; }
        public Span<byte> ByteBufferSpan => CollectionsMarshal.AsSpan(ByteBuffer);
        public int BitLength { get; set; }
        public int ByteLength => ByteBuffer.Count;

        public void AddBit(byte bit)
        {
            if (BitLength % 8 == 0)
                ByteBuffer.Add(bit);
            else
                ByteBuffer[^1] = ByteBuffer[^1].AddBit(bit);

            BitLength++;
        }

        public void AddByte(byte num)
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

        public void AppendBuffer(BitBuffer buffer)
        {
            for (int i = 0; i < buffer.ByteLength - 1; i++)
                AddByte(buffer.ByteBuffer[i]);

            var lastByte = buffer.ByteBuffer[^1];
            byte lastByteLen = (byte)(buffer.BitLength % 8);
            if (lastByteLen == 0)
                AddByte(lastByte);
            else
            {
                lastByte = (byte)(lastByte << (8 - lastByteLen));
                for (byte i = 0; i < lastByteLen; i++)
                    AddBit(lastByte.GetBit(i));
            }
        }

        public byte this[int index]
        {
            get 
            {
                var byteIndex = index / 8;
                byte bitOffset = (byte)(index % 8);
                return ByteBuffer[byteIndex].GetBit(bitOffset);
            }
        }

        public void ShiftLastByteToWritableState() =>
            ByteBuffer[^1] = (byte)(ByteBuffer[^1] << ((8 - (BitLength % 8)) % 8));

        public bool MoveNext()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public BitBuffer()
        {
            ByteBuffer = new List<byte>(4);
        }
    }
}
