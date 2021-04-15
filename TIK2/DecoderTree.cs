using Helper;
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
        
        public long BytesWritten { get; set; }

        private void AddCode(SymbolEncoding entry)
        {
            var code = entry.Code;

            var curNode = Root;
            for (int i = 0; i < code.BitLength; i++)
            {
                var bit = code[i];

                if (curNode.Value != null)
                    throw new Exception($"Encountered an already filled code node when adding a code {code}");

                if (bit == 0)
                {
                    if (curNode.Left == null)
                        curNode.Left = new DecoderNode();
                    curNode = curNode.Left;
                }
                else if (bit == 1)
                {
                    if (curNode.Right == null)
                        curNode.Right = new DecoderNode();
                    curNode = curNode.Right;
                }
                else
                    throw new ArgumentException();
            }
            curNode.Value = entry.Symbol;
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

        public BitBuffer GetEncodedTree()
        {
            var symbols = new List<byte>();
            var buffer = new BitBuffer();

            void dfs(DecoderNode node)
            {
                if (node.Left != null)
                {
                    buffer.AppendBit(0); // move down
                    dfs(node.Left);
                }
                if (node.Right != null)
                {
                    buffer.AppendBit(0); // move down
                    dfs(node.Right);
                }

                if (node.Value != null)
                    symbols.Add((byte)node.Value);

                buffer.AppendBit(1); // move up
            }

            dfs(Root);
            symbols.ForEach(b => buffer.AppendByte(b));
            return buffer;
        }

        public Dictionary<byte, SymbolEncoding> GetEncodingDictionary()
        {
            var buffer = new BitBuffer();
            var dict = new Dictionary<byte, SymbolEncoding>();

            void dfs(DecoderNode node)
            {
                if (node.Left != null)
                {
                    buffer.AppendBit(0); // move left
                    dfs(node.Left);
                }
                if (node.Right != null)
                {
                    buffer.AppendBit(1); // move right
                    dfs(node.Right);
                }

                if (node.Value != null)
                {
                    var symbol = (byte)node.Value;
                    var encoding = new SymbolEncoding(symbol, new BitBuffer(buffer));
                    dict.Add(symbol, encoding);
                }

                if (buffer.BitLength > 0)
                    buffer.PopBit(); // move up
            }

            dfs(Root);
            return dict;
        }

        public DecoderTree(DecoderNode root)
        {
            Root = root;
            CurNode = Root;
        }

        public DecoderTree(IEnumerable<SymbolEncoding> dict)
        {
            Root = new DecoderNode();
            CurNode = Root;

            foreach (var entry in dict)
                AddCode(entry);
        }

        public DecoderTree(IEnumerable<SymbolEncoding> dict, string outputFilepath) : this(dict)
        {
            SetOutputFileStream(outputFilepath);
        }

        /// <summary>
        /// Decode the tree using some of that sweet-sweet magic
        /// </summary>
        /// <param name="br"></param>
        public DecoderTree(BitReader br)
        {
            Root = new DecoderNode();
            CurNode = Root;
            var valueNodes = new List<DecoderNode>();

            var nodeStack = new Stack<DecoderNode>();
            nodeStack.Push(Root);

            byte nextMove;
            byte prevMove = 1; // up. Equals to 1 to work on empty dicts

            while (nodeStack.Count > 0)
            {
                br.ReadBit(out nextMove);
                var curNode = nodeStack.Peek();

                if (nextMove == 0) // down
                {
                    DecoderNode nextNode;
                    if (curNode.Left == null)
                    {
                        curNode.Left = new DecoderNode();
                        nextNode = curNode.Left;
                    }
                    else if (curNode.Right == null)
                    {
                        curNode.Right = new DecoderNode();
                        nextNode = curNode.Right;
                    }
                    else
                        throw new Exception("Tried going down on the node that has both children");

                    nodeStack.Push(nextNode);
                }
                else // up
                {
                    nodeStack.Pop();
                    if (prevMove == 0) // down
                        valueNodes.Add(curNode);
                }

                prevMove = nextMove;
            }

            byte value;
            for (int i = 0; i < valueNodes.Count; i++)
            {
                br.ReadByte(out value);
                valueNodes[i].Value = value;
            }
        }

        public void SetOutputFileStream(string outputFilepath)
        {
            if (CurrentFileStream != null)
                CurrentFileStream.Close();
            CurrentFileStream = new FileStream(outputFilepath, FileMode.Create);
        }

        public void WriteByte(byte b)
        {
            if (CurrentFileStream != null && CurrentFileStream.CanWrite)
            {
                CurrentFileStream.Write(new byte[] { b }, 0, 1);
                BytesWritten++;
            }
        }


        public void StopWriting() =>
            CurrentFileStream.Close();
    }
}
