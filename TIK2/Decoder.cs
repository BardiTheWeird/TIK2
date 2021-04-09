using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace TIK2
{
    static class Decoder
    {
        private static (byte, string)[] DecodeDictionary(BitReader br)
        {
            byte curByte;
            br.ReadByte(out curByte);
            var dictLen = curByte + 1;

            var dict = new (byte, string)[dictLen];

            for (int i = 0; i < dictLen; i++)
            {
                br.ReadByte(out curByte);
                var symbol = curByte;

                br.ReadByte(out curByte);
                var codeLen = curByte + 1;
                
                string code;
                br.ReadBits(codeLen, out code);

                dict[i] = (symbol, code);
            }
            return dict;
        }

        public static void Decode(string filepathIn, string filepathOut)
        {
            Console.WriteLine("Started decoding...");
            var sw = new Stopwatch();
            sw.Start();

            var br = new BitReader(filepathIn);
            
            // reading the length of a file in BITS
            byte byteOut;
            br.ReadByte(out byteOut); // length of a length
            string fileLengthString;
            br.ReadBits(byteOut, out fileLengthString);
            long fileLength = Convert.ToInt64(fileLengthString, 2) + fileLengthString.Length;

            var dict = DecodeDictionary(br);
            var decoder = new DecoderTree(dict, filepathOut);

            Console.WriteLine($"Finished preparations. Time elapsed: {sw.ElapsedMilliseconds / 1000f:.00}s");
            var previousPercentage = -1;

            char curBit;
            while(br.BitsRed < fileLength && br.ReadBit(out curBit))
            {
                decoder.FeedBit(curBit);

                var percentage = (int)(Math.Round(br.BytesRead / (float)br.FileLength, 2) * 100);
                if (percentage > previousPercentage)
                {
                    previousPercentage = percentage;
                    Console.WriteLine($"\tDone {percentage * 1}%;\tTime elapsed: {sw.ElapsedMilliseconds / 1000f:.00}s");
                }
            }
            decoder.FinishWriting();

            sw.Stop();
            Console.WriteLine($"Finished decoding. Time elapsed: {sw.ElapsedMilliseconds / 1000f:.00}s");
        }
    }
}
