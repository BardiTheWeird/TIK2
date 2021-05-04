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

        public event PropertyChangedEventHandler PropertyChanged;

        public string Decode(string filepathIn, string filepathOut, int blockSize, CancellationToken token)
        {
            BitReader br = null;
            BitWriter bw = null;
            try
            {
                _sw.Start();
                br = new BitReader(filepathIn);
                bw = new BitWriter(filepathOut);

                var resultCount = new Dictionary<HammingCodes.DecodingResult, uint>();
                resultCount.Add(HammingCodes.DecodingResult.Fail, 0);
                resultCount.Add(HammingCodes.DecodingResult.OK, 0);
                resultCount.Add(HammingCodes.DecodingResult.OneBitErrorCorrected, 0);
                resultCount.Add(HammingCodes.DecodingResult.TwoBitError, 0);

                br.ReadBits(32, out var overflownLenBuffer);
                var overflownLen = HammingCodes.DecodeHamming(overflownLenBuffer).Item1.ToBigInteger();
                if (overflownLen > 0)
                {
                    br.ReadBits(blockSize, out var overflownPadded);
                    var paddedInfo = HammingCodes.DecodeHamming(overflownPadded);
                    resultCount[paddedInfo.Item2]++;
                    
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
                    resultCount[decoding.Item2]++;

                    var percentage = (int)(Math.Round(br.BytesRead / (float)br.FileLength, 2) * 100);
                    if (percentage > previousPercentage)
                    {
                        previousPercentage = percentage;
                        Log = $"Hamming-decoding... {percentage * 1}%;\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s";
                    }
                }
                bw.WriteTheRestOfTheBuffer();
                bw.StopWriting();
                br.StopReading();

                _sw.Stop();
                Log = "";
                var res = $"Finished decoding. Decoded file:{Path.GetFileName(filepathOut)}.\n" +
                    $"\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s\n" +
                    $"\tFailed: {resultCount[HammingCodes.DecodingResult.Fail]}\n" +
                    $"\tOK: {resultCount[HammingCodes.DecodingResult.OK]}\n" +
                    $"\t1-bit error corrected: {resultCount[HammingCodes.DecodingResult.OneBitErrorCorrected]}\n" +
                    $"\t2-bit error: {resultCount[HammingCodes.DecodingResult.TwoBitError]}";
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
