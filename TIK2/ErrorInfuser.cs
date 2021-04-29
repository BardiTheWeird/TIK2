using Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TIK2
{
    public class ErrorInfuser : INotifyPropertyChanged
    {
        public string Log { get; set; }
        private Stopwatch _sw = new Stopwatch();
        private string _errorDumpFile = "errorInducerErrorDump.txt";

        public event PropertyChangedEventHandler PropertyChanged;

        BitBuffer InfuseErrorIntoBuffer(BitBuffer source, int errorCount)
        {
            if (errorCount < 0)
                throw new ArgumentException("errorCount can't be negative");
            if (source.BitLength < errorCount)
                throw new ArgumentException("source length can't be lesser than errorCount");

            var indicesIn = Enumerable.Range(0, source.BitLength).ToList();
            var indicesOut = new List<int>(errorCount);

            var rand = new Random();
            for (int _ = 0; _ < errorCount; _++)
            {
                var i = rand.Next(0, indicesIn.Count);
                indicesOut.Add(indicesIn[i]);
                indicesIn.RemoveAt(i);
            }

            var newBuffer = new BitBuffer(source);
            foreach (var i in indicesOut)
                newBuffer[i] = (byte)(newBuffer[i] ^ 1);

            return newBuffer;
        }

        public string InfuseErrorIntoFile(string filepathIn, string filepathOut, int blockSize, int errorCount, CancellationToken token)
        {
            BitReader br = null;
            BitWriter bw = null;
            //try
            //{
            _sw.Start();

            br = new BitReader(filepathIn);
            var bitLength = br.FileLength * 8;
            bw = new BitWriter(filepathOut);

            var previousPercentage = -1;

            br.ReadBits(16, out var lenOverflowBuffer);
            bw.WriteBuffer(lenOverflowBuffer);

            for (long i = 0; i < bitLength - blockSize; i += blockSize)
            {
                if (token.IsCancellationRequested)
                {
                    br.StopReading();
                    bw.StopWriting();
                    _sw.Reset();
                    Log = string.Empty;
                    return string.Empty;
                }

                var percentage = (int)(Math.Round(i / (float)bitLength, 2) * 100);
                if (percentage > previousPercentage)
                {
                    previousPercentage = percentage;
                    Log = $"Infusing error... {percentage * 1}%;\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s";
                }


                var readSize = (int)Math.Min(blockSize, bitLength - i);
                br.ReadBits(readSize, out var sourceBuffer);
                var curErrorCount = Math.Min(errorCount, sourceBuffer.BitLength);
                bw.WriteBuffer(InfuseErrorIntoBuffer(sourceBuffer, curErrorCount));
            }
            bw.WriteTheRestOfTheBuffer();
            bw.StopWriting();
            br.StopReading();

            Log = "";
            var outString = $"Finished infusing error. Encoded file: {Path.GetFileName(filepathOut)}.\n" +
                $"\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s\n";
            _sw.Reset();

            return outString;
            //}
            //catch (EmptyFileException e)
            //{
            //    Log = "";
            //    _sw.Reset();

            //    return $"Encoding failed. Cannot encode an empty file";
            //}
            //catch (Exception e)
            //{
            //    Log = "";
            //    _sw.Reset();
            //    File.WriteAllText(_errorDumpFile, e.Message);

            //    return $"Encoding failed. Details are in the encoder error dump file";
            //}
            //finally
            //{
            //    try
            //    {
            //        br.StopReading();
            //        bw.StopWriting();
            //    }
            //    catch { }
            //}
        }
    }
}
