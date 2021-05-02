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
            var countArr = ReadingWriting.GetFrequencyArray(filepathIn, _sw,
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

        public string Encode(string filepathIn, string filepathOut, EncodingType encodingType, CancellationToken token)
        {
            try
            {
                _sw.Start();

                var count = GetSymbolCountOrdered(filepathIn, token);
                if (token.IsCancellationRequested)
                    return string.Empty;

                Dictionary<byte, SymbolEncoding> dict = null;

                if (encodingType == EncodingType.ShennonFano)
                    dict = ShennonFanoEncoding.GetEncodingDictionary(count);
                else if (encodingType == EncodingType.Huffman)
                    dict = HuffmanEncoding.GetEncodingDictionary(count);

                // the file format is as follows:
                // 3 bits - the amount of bits to read in the final byte
                // 8 bits - the amount of entries in a dictionary (offset by 1)
                // dictionary entries in the format "symbol|codeLen|code"
                // encoded file

                var tree = new DecoderTree(dict.Values);
                var encodedDict = tree.GetEncodedTree();

                long fileLen = 0;
                foreach (var entry in count)
                    fileLen += entry.Item2 * dict[entry.Item1].Code.BitLength;

                fileLen += encodedDict.BitLength + 3;

                var finalByteLen = (byte)(fileLen % 8);


                var buffer = new BitBuffer();
                buffer.AppendPartialByte(finalByteLen, 3);
                buffer.AppendBuffer(encodedDict);

                var bw = new BitWriter(filepathOut);
                bw.WriteBuffer(buffer);

                var previousPercentage = -1;

                var fs = new FileStream(filepathIn, FileMode.Open);
                var chunkSize = ReadingWriting.MB;
                var readBuffer = new byte[chunkSize];
                var len = fs.Length;

                for (long i = 0; i < len; i += chunkSize)
                {
                    if (token.IsCancellationRequested)
                    {
                        fs.Close();
                        bw.StopWriting();
                        _sw.Reset();
                        return string.Empty;
                    }

                    var percentage = (int)(Math.Round(i / (float)len, 2) * 100);
                    if (percentage > previousPercentage)
                    {
                        previousPercentage = percentage;
                        Log = $"Encoding... {percentage * 1}%;\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s";
                    }

                    fs.Read(readBuffer, 0, chunkSize);
                    var higherLimit = Math.Min(chunkSize, len - i);
                    for (int j = 0; j < higherLimit; j++)
                        bw.WriteBuffer(dict[readBuffer[j]].Code);
                }
                bw.WriteTheRestOfTheBuffer();
                bw.StopWriting();
                fs.Close();

                Log = "";
                var outString = $"Finished encoding. Encoded file: {Path.GetFileName(filepathOut)}.\n" +
                    $"\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s\n" +
                    $"\tCompression ratio: {bw.BytesWritten / ((double)len)}";
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
