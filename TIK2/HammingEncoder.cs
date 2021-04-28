﻿using Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TIK2
{
    public class HammingEncoder
    {
        public string Log { get; set; }
        private Stopwatch _sw;
        private string _errorDumpFile = "hammingEncoderErrorDump.txt";

        public string Encode(string filepathIn, string filepathOut, int blockSize, CancellationToken token)
        {
            BitReader br = null;
            BitWriter bw = null;
            try
            {
                _sw.Start();

                if (!HelperMath.IsPowerOfTwo(blockSize))
                    throw new ArgumentException("block size is not a power of two");

                var informationBlockSize = HammingCodes.GetInformationBlockSize(blockSize);

                br = new BitReader(filepathIn);
                bw = new BitWriter(filepathOut);

                // The file format is simple.
                // The first byte is the amount of overflown bits (that don't fit the informationBlockSize)
                // The second blockSized block encodes overflown bits + some padded zeroes to the right
                // The rest is all just encoded blocks

                var len = br.FileLength;
                var lenBits = len * 8;
                var lenOverflow = (byte)(lenBits % informationBlockSize);
                bw.WriteByte(lenOverflow);
                if (lenOverflow > 0)
                {
                    br.ReadBits(lenOverflow, out var overflownBuffer);
                    var padBuffer = new BitBuffer();
                    padBuffer.FillWithZeroesBits(informationBlockSize - lenOverflow);
                    overflownBuffer.AppendBuffer(padBuffer);
                    bw.WriteBuffer(HammingCodes.EncodeHamming(overflownBuffer));
                }

                var previousPercentage = -1;

                for (long i = lenOverflow; i < len - lenOverflow; i += blockSize)
                {
                    if (token.IsCancellationRequested)
                    {
                        br.StopReading();
                        bw.StopWriting();
                        _sw.Reset();
                        Log = string.Empty;
                        return string.Empty;
                    }

                    var percentage = (int)(Math.Round(i / (float)len, 2) * 100);
                    if (percentage > previousPercentage)
                    {
                        previousPercentage = percentage;
                        Log = $"Hamming-coding... {percentage * 1}%;\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s";
                    }

                    br.ReadBits(informationBlockSize, out var infoBuffer);
                    bw.WriteBuffer(HammingCodes.EncodeHamming(infoBuffer));
                }
                bw.WriteTheRestOfTheBuffer();
                bw.StopWriting();
                br.StopReading();

                Log = "";
                var outString = $"Finished hamming-encoding. Encoded file: {Path.GetFileName(filepathOut)}.\n" +
                    $"\tTime elapsed: {_sw.ElapsedMilliseconds / 1000f:.00}s\n";
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
