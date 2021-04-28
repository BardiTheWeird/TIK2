using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Helper;

namespace TIK2
{
    public class HammingDecoder
    {
        public string Log { get; set; }
        private Stopwatch _sw;
        private string _errorDumpFile = "hammingDecoderErrorDump.txt";

        public string Decode(string filepathIn, string filepathOut, int blockSize, CancellationToken token)
        {
            BitReader br = null;
            BitWriter bw = null;
            try
            {
                _sw.Start();
                br = new BitReader(filepathIn);
                bw = new BitWriter(filepathOut);

                br.ReadByte(out var overflownLen);
                if (overflownLen > 0)
                {
                    br.ReadBits(blockSize, out var overflownPadded);
                    var paddedInfo = HammingCodes.DecodeHamming(overflownPadded);
                    
                    var overflownInfo = new BitBuffer();
                    for (int i = 0; i < overflownLen; i++)
                        overflownInfo.AppendBit(paddedInfo.Item1[i]);

                    bw.WriteBuffer(overflownInfo);
                }

                var previousPercentage = -1;

                while (br.BitsRead < br.FileLength * 8)
                {
                    if (token.IsCancellationRequested)
                    {
                        br.StopReading();
                        bw.StopWriting();
                        _sw.Reset();
                        Log = "";
                        return string.Empty;
                    }

                    br.ReadBits(blockSize, out var encodedInfo);
                    bw.WriteBuffer(HammingCodes.DecodeHamming(encodedInfo).Item1);

                    var percentage = (int)(Math.Round(br.BytesRead / (float)br.FileLength, 2) * 100);
                    if (percentage > previousPercentage)
                    {
                        previousPercentage = percentage;
                        Log = $"Huffman-decoding... {percentage * 1}%;\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s";
                    }
                }
                bw.WriteTheRestOfTheBuffer();
                bw.StopWriting();
                br.StopReading();

                _sw.Stop();
                Log = "";
                var res = $"Finished decoding. Decoded file:{Path.GetFileName(filepathOut)}.\n" +
                    $"\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s";
                _sw.Reset();
                return res;
            }
            catch (Exception e)
            {
                Log = "";
                _sw.Reset();
                File.WriteAllText(_errorDumpFile, e.Message);

                return $"Decoding failed. Details are in the encoder error dump file";
            }
            finally
            {
                try
                {
                    bw.StopWriting();
                    br.StopReading();
                }
                catch { }
            }
        }
    }
}
