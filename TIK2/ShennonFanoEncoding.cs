using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIK2
{
    static class ShennonFanoEncoding
    {
        static IEnumerable<(byte, long)> CumulativeCount(IEnumerable<(byte, long)> countArr)
        {
            long sum = 0;
            foreach (var entry in countArr)
            {
                yield return (entry.Item1, entry.Item2 + sum);
                sum += entry.Item2;
            }
        }

        public static Dictionary<byte, SymbolEncoding> GetEncodingDictionary((byte, long)[] count)
        {
            count = CumulativeCount(count).ToArray();
            var codeTable = count.Select(p => new SymbolEncoding { Symbol = p.Item1 }).ToArray();

            void createEncoderDictionaryRec(int lo, int hi)
            {
                if (Math.Abs(hi - lo) <= 1)
                    return;

                var midValue = (count[hi - 1].Item2 + count[lo].Item2) / 2;
                int leftPartHigh = lo + 1;

                for (int i = lo; i < hi; i++)
                {
                    if (count[i].Item2 > midValue)
                    {
                        leftPartHigh = i;
                        break;
                    }
                    codeTable[i].Code.AppendBit(0);
                }
                for (int i = leftPartHigh; i < hi; i++)
                {
                    codeTable[i].Code.AppendBit(1);
                }

                createEncoderDictionaryRec(lo, leftPartHigh);
                createEncoderDictionaryRec(leftPartHigh, hi);
            }

            createEncoderDictionaryRec(0, count.Length);
            return codeTable.ToDictionary(p => p.Symbol, p => p);
        }
    }
}
