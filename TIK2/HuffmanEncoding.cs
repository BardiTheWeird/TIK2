using Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIK2
{
    static class HuffmanEncoding
    {
        public static DecoderTree CreateHuffmanEncodingTree((byte, long)[] count)
        {
            var heap = new Heap<(DecoderNode, long)>(count.Length, (x, y) => x.Item2 < y.Item2);
            foreach (var entry in count)
                heap.Add((new DecoderNode(entry.Item1), entry.Item2));

            while (heap.Size > 1)
            {
                var nodeL = heap.Pop();
                var nodeR = heap.Pop();

                var newNode = new DecoderNode();
                newNode.Left = nodeL.Item1;
                newNode.Right = nodeR.Item1;

                heap.Add((newNode, nodeL.Item2 + nodeR.Item2));
            }

            if (heap.Size == 0)
                throw new Exception("Heap size is equal to 0");
            else if (heap.Size > 1)
                throw new Exception("Heap size is greater than 1");

            var root = heap.Peek();
            
            return new DecoderTree(root.Item1);
        }

        public static Dictionary<byte, SymbolEncoding> GetEncodingDictionary((byte, long)[] count) 
        { 
            return CreateHuffmanEncodingTree(count).GetEncodingDictionary(); 
        }
    }
}
