using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TIK2
{
    class DecoderNode
    {
        public DecoderNode Left { get; set; } = null;
        public DecoderNode Right { get; set; } = null;
        public byte? Value { get; set; }

        public DecoderNode GetNextNode(byte direction)
        {
            if (direction == 0)
                return Left;
            else if (direction == 1)
                return Right;
            else
                throw new ArgumentException();
        }

        public DecoderNode(byte? value = null)
        {
            Value = value;
        }
    }

    class DecoderTree
    {
        private DecoderNode Root { get; set; }
        private DecoderNode CurNode { get; set; }
        private FileStream CurrentFileStream { get; set; }

        private void AddCode((byte, string) entry)
        {
            var curNode = Root;
            foreach (var bit in entry.Item2)
            {
                if (curNode.Value != null)
                    throw new Exception($"Encountered an already filled code node when adding a code {entry.Item2}");

                if (bit == '0')
                {
                    if (curNode.Left == null)
                        curNode.Left = new DecoderNode();
                    curNode = curNode.Left;
                }
                else if (bit == '1')
                {
                    if (curNode.Right == null)
                        curNode.Right = new DecoderNode();
                    curNode = curNode.Right;
                }
                else
                    throw new ArgumentException();
            }
            curNode.Value = entry.Item1;
        }

        public void FeedBit(byte treat)
        {
            CurNode = CurNode.GetNextNode(treat);
            if (CurNode.Value.HasValue)
            {
                var valToWrite = CurNode.Value.Value;
                WriteByte(valToWrite);
                CurNode = Root;
            }
        }

        public void FeedBytes(params byte[] food)
        {
            foreach (var num in food)
            {
                for (int i = 7; i >= 0; i--)
                    FeedBit((byte)((num >> i) & 1));
            }
        }

        public DecoderTree(IEnumerable<(byte, string)> dict)
        {
            Root = new DecoderNode();
            CurNode = Root;

            //File.WriteAllText(HistoryDumpPath, "code,value\n");

            foreach (var entry in dict)
                AddCode(entry);
        }

        public DecoderTree(IEnumerable<(byte, string)> dict, string outputFilepath) : this(dict)
        {
            SetFileStream(outputFilepath);
        }

        public void SetFileStream(string outputFilepath)
        {
            if (CurrentFileStream != null)
                CurrentFileStream.Close();
            CurrentFileStream = new FileStream(outputFilepath, FileMode.Create);
        }

        public void WriteByte(byte b)
        {
            //Console.WriteLine($"About to write {b}");

            if (CurrentFileStream != null && CurrentFileStream.CanWrite)
                CurrentFileStream.Write(new byte[] { b }, 0, 1);
        }


        public void FinishWriting()
        {
            CurrentFileStream.Close();
            //DumpCurrentHistory();
        }
    }
}
