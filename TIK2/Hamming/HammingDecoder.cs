using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Helper;

namespace TIK2
{
    public class HammingDecoder : INotifyPropertyChanged
    {
        public string Log { get; set; }
        private Stopwatch _sw = new Stopwatch();
        private string _errorDumpPath = "hammingDecoderErrorDump.txt";
        private string _decoderLogPath = "hammingDecoderLog.txt";

        public event PropertyChangedEventHandler PropertyChanged;

        public string Decode(string filepathIn, string filepathOut, int blockSize, CancellationToken token)
        {
            BitReader br = null;
            BitWriter bw = null;
            try
            {
                var logSb = new StringBuilder();
                _sw.Start();
                br = new BitReader(filepathIn);
                bw = new BitWriter(filepathOut);

                br.ReadBits(16, out var overflownLenBuffer);
                var overflownLen = HammingCodes.DecodeHamming(overflownLenBuffer).Item1.ByteBuffer[0];
                if (overflownLen > 0)
                {
                    br.ReadBits(blockSize, out var overflownPadded);
                    var paddedInfo = HammingCodes.DecodeHamming(overflownPadded);
                    logSb.AppendLine(paddedInfo.Item3);
                    
                    var overflownInfo = new BitBuffer();
                    for (int i = 0; i < overflownLen; i++)
                        overflownInfo.AppendBit(paddedInfo.Item1[i]);

                    bw.WriteBuffer(overflownInfo);
                }

                var previousPercentage = -1;

                while (br.BitsRead < br.FileLength * 8 - blockSize)
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
                    var decoding = HammingCodes.DecodeHamming(encodedInfo);
                    bw.WriteBuffer(decoding.Item1);
                    logSb.AppendLine(decoding.Item3);

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

                File.WriteAllText(_decoderLogPath, logSb.ToString());

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
                File.WriteAllText(_errorDumpPath, e.Message);

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
