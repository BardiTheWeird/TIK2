using Helper;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace TIK2
{
    public class Decoder : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Log { get; set; }

        private Stopwatch _sw = new Stopwatch();
        private string _errorDumpFile = "decoderErrorDump.txt";

        private static SymbolEncoding[] DecodeDictionary(BitReader br)
        {
            byte curByte;
            br.ReadByte(out curByte);
            var dictLen = curByte + 1;

            var dict = new SymbolEncoding[dictLen];

            for (int i = 0; i < dictLen; i++)
            {
                br.ReadByte(out curByte);
                var symbol = curByte;

                br.ReadByte(out curByte);
                var codeLen = curByte + 1;
                
                br.ReadBits(codeLen, out var code);

                dict[i] = new SymbolEncoding(symbol, code);
            }
            return dict;
        }

        public string Decode(string filepathIn, string filepathOut, CancellationToken token)
        {
            //try
            //{
            _sw.Start();

            Log = "Started decoding...";
            var br = new BitReader(filepathIn);

            // reading the length of a file in BITS
            // br.ReadByte(out var lenLength); // length of a length

            //br.ReadBits(64, out var fileLengthBuffer);
            //var fileLength = fileLengthBuffer.ToLong(0, 8);

            br.ReadBits(3, out var finalByteLengthBuffer);
            var finalByteLength = finalByteLengthBuffer.ByteBuffer[0];
            if (finalByteLength == 0)
                finalByteLength = 8;

            var totalLength = br.FileLength * 8 - (8 - finalByteLength);

            var dict = DecodeDictionary(br);
            var decoder = new DecoderTree(dict, filepathOut);

            var readingChunk = ReadingWriting.MB;
            var previousPercentage = -1;

            while (br.BitsRead < totalLength)
            {
                if (token.IsCancellationRequested)
                {
                    br.StopReading();
                    decoder.StopWriting();
                    _sw.Reset();
                    Log = "";
                    return string.Empty;
                }

                var percentage = (int)(Math.Round(br.BytesRead / (float)br.FileLength, 2) * 100);
                if (percentage > previousPercentage)
                {
                    previousPercentage = percentage;
                    Log = $"Decoding... {percentage * 1}%;\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s";
                }

                var higherLimit = Math.Min(readingChunk, totalLength - br.BitsRead);
                for (int i = 0; i < higherLimit; i++)
                {
                    br.ReadBit(out var curBit);
                    decoder.FeedBit(curBit);
                }
            }
            decoder.StopWriting();
            br.StopReading();

            _sw.Stop();
            Log = "";
            var res = $"Finished decoding. Decoded file:{Path.GetFileName(filepathOut)}. " +
                $"Time elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s";
            _sw.Reset();
            return res;
            //}
            //catch (Exception e)
            //{
            //    Log = "";
            //    _sw.Reset();
            //    File.WriteAllText(_errorDumpFile, e.Message);

            //    return $"Decoding failed. Details are in the encoder error dump file";
            //}
        }
    }
}
