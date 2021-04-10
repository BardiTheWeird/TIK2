using Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TIK2
{
    public class EmptyFileException : Exception
    {
        public EmptyFileException() { }
    }

    public class Encoder : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Log { get; set; }
        private Stopwatch _sw = new Stopwatch();

        private string _errorDumpFile = "encoderErrorDump.txt";

        private (byte, long)[] GetSymbolCountOrdered(string filepathIn, CancellationToken token)
        {
            var countArr = Helper.ReadingWriting.GetFrequencyArray(filepathIn, _sw,
                x => Log = x, "Creating an encoding dictionary...", token);

            Log = "";
            if (token.IsCancellationRequested)
                return null;

            return Enumerable.Range(0, 256)
                .Where(i => countArr[i] > 0)
                .Select(i => ((byte)i, countArr[i]))
                .OrderByDescending(p => p.Item2)
                .ToArray();
        }

        static IEnumerable<(byte, long)> CumulativeCount(IEnumerable<(byte, long)> countArr)
        {
            long sum = 0;
            foreach (var entry in countArr)
            {
                yield return (entry.Item1, entry.Item2 + sum);
                sum += entry.Item2;
            }
        }

        private static Dictionary<byte, SymbolEncoding> GetEncodingDictionary((byte, long)[] count)
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

        private static BitBuffer GetEncodedDictionaryBuffer(Dictionary<byte, SymbolEncoding> dict)
        {
            BitBuffer buffer = new BitBuffer();

            buffer.AppendByte((byte)(dict.Count - 1));

            foreach (var encoding in dict.Values)
            {
                buffer.AppendByte(encoding.Symbol);
                buffer.AppendByte((byte)(encoding.Code.BitLength - 1));
                buffer.AppendBuffer(encoding.Code);
            }

            return buffer;
        }

        public string Encode(string filepathIn, string filepathOut, CancellationToken token)
        {
            try
            {
                _sw.Start();

                var count = GetSymbolCountOrdered(filepathIn, token);
                if (token.IsCancellationRequested)
                    return string.Empty;

                var dict = GetEncodingDictionary(count);
                File.WriteAllText("encoded_dict.txt", string.Join('\n',
    dict.Select(e => $"{e.Value.Symbol},{e.Value.Code}")));

                // the file format is as follows:
                // first 8 bits - the length of the length of the encoded file in BITS. Doesn't count itself
                // the length in bits
                // 8 bits - the amount of entries in a dictionary (offset by 1)
                // dictionary entries in the format "symbol|codeLen|code"
                // encoded file

                var encodedDict = GetEncodedDictionaryBuffer(dict);

                long fileLen = 0;
                foreach (var entry in count)
                    fileLen += entry.Item2 * dict[entry.Item1].Code.BitLength;

                fileLen += 64 + encodedDict.BitLength;

                //var fileLenBits = Convert.ToString(fileLen, 2);
                //var lengthLen = Convert.ToString(fileLenBits.Length, 2).PadLeft(8, '0');

                var buffer = new BitBuffer();
                buffer.AppendLong(fileLen);
                buffer.AppendBuffer(encodedDict);

                var bw = new BitWriter(filepathOut);
                bw.WriteBuffer(buffer);

                var previousPercentage = -1;

                var br = new BitReader(filepathIn);
                byte curByte;
                while (br.ReadByte(out curByte))
                {
                    if (token.IsCancellationRequested)
                    {
                        br.StopReading();
                        bw.StopWriting();
                        _sw.Reset();
                        return string.Empty;
                    }

                    bw.WriteBuffer(dict[curByte].Code);

                    var percentage = (int)(Math.Round(br.BytesRead / (float)br.FileLength, 2) * 100);
                    if (percentage > previousPercentage)
                    {
                        previousPercentage = percentage;
                        Log = $"Encoding... {percentage * 1}%;\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s";
                    }
                }
                bw.WriteTheRestOfTheBuffer();
                bw.StopWriting();

                Log = "";
                var outString = $"Finished encoding. Encoded file: {Path.GetFileName(filepathOut)}. " +
                    $"Time elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s";
                _sw.Reset();

                return outString;
            }
            catch (EmptyFileException e)
            {
                Log = "";
                _sw.Reset();

                return $"Encoding failed. Cannot encode an empty file";
            }
            catch (Exception e)
            {
                Log = "";
                _sw.Reset();
                File.WriteAllText(_errorDumpFile, e.Message);

                return $"Encoding failed. Details are in the encoder error dump file";
            }
        }
    }
}
