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
    public class CRCEncoder : INotifyPropertyChanged
    {
        public string Log { get; set; }
        private Stopwatch _sw = new Stopwatch();
        private string _errorDumpFile = "crcEncoderErrorDump.txt";

        public event PropertyChangedEventHandler PropertyChanged;

        public string Encode(string filepathIn, string filepathOut, uint blockSize, CancellationToken token)
        {
            BitReader br = null;
            BitWriter bw = null;
            try
            {
                _sw.Start();

                var sb = new StringBuilder();

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
                        Log = $"CRC-encoding... {percentage * 1}%;\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s";
                    }

                    var readLen = (int)Math.Min(blockSize, lenBits - i);

                    if (readLen < blockSize)
                        Console.Write("");

                    br.ReadBits(readLen, out var message);
                    var encoded = CRCMath.EncodeCRC(message, CRCMath.Polynomial);
                    bw.WriteBuffer(encoded);
                    sb.AppendLine($"encoded length: {encoded.BitLength}");
                }
                bw.WriteTheRestOfTheBuffer();
                bw.StopWriting();
                br.StopReading();

                Log = "";
                var outString = $"Finished CRC-encoding. Encoded file: {Path.GetFileName(filepathOut)}.\n" +
                    $"\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s\n";
                _sw.Reset();

                File.WriteAllText("_CRCEncoderLog.txt", sb.ToString());

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
            finally
            {
                try
                {
                    br.StopReading();
                    bw.StopWriting();
                }
                catch { }
            }
        }
    }
}
