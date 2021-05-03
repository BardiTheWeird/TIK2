using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TIK2.CRC
{
    public class CRCDecoder : INotifyPropertyChanged
    {
        public string Log { get; set; }
        private Stopwatch _sw = new Stopwatch();
        private string _errorDumpPath = "crcDecoderErrorDump.txt";

        public event PropertyChangedEventHandler PropertyChanged;

        public string Decode(string filepathIn, string filepathOut, uint blockSize, CancellationToken token)
        {
            BitReader br = null;
            BitWriter bw = null;
            try
            {
                _sw.Start();

                blockSize += (uint)(CRCMath.GetLargestBinPower(CRCMath.Polynomial) + 1);

                var resultCount = new Dictionary<CRCDecodingResult, uint>();
                resultCount.Add(CRCDecodingResult.OK, 0);
                resultCount.Add(CRCDecodingResult.Corrupted, 0);

                br = new BitReader(filepathIn);
                bw = new BitWriter(filepathOut);

                var lenBits = br.FileLength * 8;

                var previousPercentage = -1;

                for (long i = 0; i < lenBits; i += blockSize)
                {
                    if (token.IsCancellationRequested)
                    {
                        br.StopReading();
                        bw.StopWriting();
                        _sw.Reset();
                        Log = string.Empty;
                        return string.Empty;
                    }

                    var percentage = (int)(Math.Round(i / (float)lenBits, 2) * 100);
                    if (percentage > previousPercentage)
                    {
                        previousPercentage = percentage;
                        Log = $"CRC-decoding... {percentage * 1}%;\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s";
                    }

                    var readLen = (int)Math.Min(blockSize, lenBits - i);

                    br.ReadBits(readLen, out var encodedMessage);
                    var decodingResult = CRCMath.DecodeCRC(encodedMessage, CRCMath.Polynomial);
                    bw.WriteBuffer(decodingResult.Item1);
                    resultCount[decodingResult.Item2]++;
                }
                bw.WriteTheRestOfTheBuffer();
                bw.StopWriting();
                br.StopReading();

                Log = "";
                var outString = $"Finished CRC-decoding. Encoded file: {Path.GetFileName(filepathOut)}.\n" +
                    $"\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s\n" +
                    $"\tOK/Corrupted: {resultCount[CRCDecodingResult.OK]}/{resultCount[CRCDecodingResult.Corrupted]}";
                _sw.Reset();

                return outString;
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
