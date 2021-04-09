using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace TIK2
{
    class Program
    {
        private readonly static int MB = 1048576;

        #region encoding
        static string[] SplitIntoEqualLengthSlices(string arr, int sliceLen, out string left)
        {
            var sliceCount = arr.Length / sliceLen;
            left = null;
            if (arr.Length % sliceLen == 0)
            {
                left = "";
            }
            else
                left = arr[^(arr.Length % sliceLen)..];

            return Enumerable.Range(0, Math.Max(sliceCount, 0))
                   .Select(i => arr[(i * sliceLen)..((i + 1) * sliceLen)])
                   .ToArray();
        }

        static IEnumerable<(byte, int)> ReadCountTable(string filepath)
        {
            var countingArr = new int[256];
            
            var fs = new FileStream(filepath, FileMode.Open);
            var len = (int)fs.Length;

            var sb = new StringBuilder();

            var chunkSize = MB;

            var bits = new byte[chunkSize];
            for (int i = 0; i < len; i += chunkSize)
            {
                fs.Read(bits, 0, chunkSize);
                if (len - i < chunkSize)
                    bits = bits[..(len - i)];

                foreach (var symbol in bits)
                    countingArr[symbol]++;

                sb.Append(string.Join("", bits.Select(b => $"{b.ToString()},")));
            }

            fs.Close();
            File.WriteAllText("binaryDump.txt", sb.ToString()[..^1]);

            // select non-empty ones
            return Enumerable.Range(0, countingArr.Length)
                .Where(i => countingArr[i] > 0)
                .Select(i => ((byte)i, countingArr[i]));
        }

        static IEnumerable<(byte, int)> CumulativeCount(IEnumerable<(byte, int)> countArr)
        {
            int sum = 0;
            foreach (var entry in countArr)
            {
                yield return (entry.Item1, entry.Item2 + sum);
                sum += entry.Item2;
            }
        }

        //static int CumulativeArrMidpointSearch((byte, int)[] arr, int lo, int hi, int value)
        //{
        //    while (true)
        //    {
        //        if (Math.Abs(hi - lo) <= 1)
        //            return lo;

        //        int mid = (hi + lo) / 2;
        //        int midVal = arr[mid].Item2;

        //        if (midVal == value)
        //            return mid;

        //        if (midVal < value)
        //        {
        //            if (arr[mid + 1].Item2 > value)
        //                return mid;

        //            lo = mid + 1;
        //        }
        //        else
        //        {
        //            if (arr[mid - 1].Item2 < value)
        //                return mid - 1;

        //            hi = mid - 1;
        //        }

        //        Console.WriteLine($"LOOP: lo: {lo}, hi: {hi}");
        //    }
        //}

        static int CumulativeArrMidpointSearch((byte, int)[] arr, int lo, int hi, int value)
        {
            int i;
            for (i = lo; i < hi - 1; i++)
            {
                if (arr[i + 1].Item2 > value)
                    return i;
            }
            return i;
        }

        static Dictionary<byte, string> CreateEncoderDictionary(IEnumerable<(byte, int)> countTable)
        {
            var countArr = CumulativeCount(countTable.OrderByDescending(p => p.Item2)).ToArray();
            var codeTable = countArr.Select(p => (p.Item1, "")).ToArray();

            void createEncoderDictionaryRec(int lo, int hi)
            {
                if (Math.Abs(hi - lo) <= 1)
                    return;

                var midValue = (countArr[hi - 1].Item2 + countArr[lo].Item2) / 2;
                int leftPartHigh = lo + 1;

                for (int i = lo; i < hi; i++)
                {
                    if (countArr[i].Item2 > midValue)
                    {
                        leftPartHigh = i;
                        break;
                    }
                    codeTable[i].Item2 += '0';
                }
                for (int i = leftPartHigh; i < hi; i++)
                    codeTable[i].Item2 += '1';

                createEncoderDictionaryRec(lo, leftPartHigh);
                createEncoderDictionaryRec(leftPartHigh, hi);
            }

            createEncoderDictionaryRec(0, countArr.Length);

            return codeTable.ToDictionary(p => p.Item1, p => p.Item2);
        }

        static IEnumerable<byte> EncodeDictionary(Dictionary<byte, string> dict)
        {
            yield return Convert.ToByte(dict.Count - 1);

            foreach (var (symbol, code) in dict.OrderBy(p => p.Key))
            {
                byte len = (byte)(code.Length - 1);
                int lenBytes = (int)Math.Ceiling((len + 1) / 8f);
                var binaryCodeString = code.PadLeft(8 * lenBytes, '0');

                yield return symbol;
                yield return len;

                for (int i = 0; i < lenBytes; i++)
                {
                    var byteVal = Convert.ToByte(binaryCodeString[(8 * i)..(8 * (i + 1))], 2);
                    yield return byteVal;
                }
            }
        }

        static bool DictionaryValid(Dictionary<byte, string> dict)
        {
            try
            {
                var tree = new DecoderTree(dict.Select(p => (p.Key, p.Value)).OrderBy(p => p.Key));
            }
            catch
            {
                return false;
            }
            return true;
        }

        static void EncodeFile(string inFilepath, string outFilepath)
        {
            var sw = new Stopwatch();
            sw.Start();

            var countTable = ReadCountTable(inFilepath);
            var dict = CreateEncoderDictionary(countTable);


            if (!DictionaryValid(dict))
            {
                Console.WriteLine("Dictionary isn't valid");
                throw new Exception();
            }

            var encodedDictionary = EncodeDictionary(dict).ToArray();

            var fs = new FileStream(outFilepath, FileMode.Create);
            fs.Write(encodedDictionary, 0, encodedDictionary.Count());

            var fsSource = new FileStream(inFilepath, FileMode.Open);
            var len = (int)fsSource.Length;

            string bitsLeft = "";

            Console.WriteLine($"Encoding... File size: {len / (float)MB:.00}MB");
            var previousPercentage = -1;

            var chunkSyze = 1;

            var bytes = new byte[chunkSyze];
            for (int i = 0; i < len; i += chunkSyze)
            {
                if (i > 1048570)
                    Console.Write("");
                    
                fsSource.Read(bytes, 0, chunkSyze);
                if (len - i < chunkSyze)
                    bytes = bytes[..(len - i)];

                var bitsToWrite = string.Join("", bytes.Select(b => dict[b]));
                bitsToWrite = bitsLeft + bitsToWrite;
                var bytesToWrite = SplitIntoEqualLengthSlices(bitsToWrite, 8, out bitsLeft)
                    .Select(s => Convert.ToByte(s, 2))
                    .ToArray();

                if (bytesToWrite.Length > 0)
                    fs.Write(bytesToWrite, 0, bytesToWrite.Length);

                var percentage = (int)(Math.Round(i / (float)len, 1) * 10);
                if (percentage > previousPercentage)
                {
                    previousPercentage = percentage;
                    Console.WriteLine($"Encoded {percentage * 10}%;\tTime elapsed: {sw.ElapsedMilliseconds / 1000f}s");
                }
            }
            var lenLeft = bitsLeft.Length;
            fs.Write(new byte[] { Convert.ToByte(bitsLeft.PadLeft(8, '0'), 2) }, 0, 1);
            fs.Write(new byte[] { (byte)lenLeft }, 0, 1);

            fs.Close();
            fsSource.Close();

            Console.WriteLine("Finished encoding");
            sw.Stop();
        }
        #endregion

        #region decoding
        static List<(byte, string)> ReadDictionary(FileStream fs, out int bytesRead)
        {
            var dict = new List<(byte, string)>();
            var buffer = new byte[32];

            // reading the amount of entries
            fs.Read(buffer, 0, 1);
            var entryCount = buffer[0] + 1;

            bytesRead = 1 + entryCount * 2;

            // for each entry
            for (int i = 0; i < entryCount; i++)
            {
                // symbol
                fs.Read(buffer, 0, 1);
                var symbol = buffer[0];
                // code length
                fs.Read(buffer, 0, 1);
                var codeLen = buffer[0] + 1;
                var byteLen = (int)Math.Ceiling(codeLen / 8f);
                // reading the code
                fs.Read(buffer, 0, byteLen);
                var codeBytes = buffer[..byteLen];
                var codePadded = string.Join("", codeBytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
                var code = codePadded[^codeLen..];

                bytesRead += byteLen;

                dict.Add((symbol, code));
            }
            return dict;
        }

        static void CompareDictionaries(IEnumerable<(byte, string)> or, IEnumerable<(byte, string)> read)
        {
            var sb = new StringBuilder();
            if (or.Count() != read.Count())
                sb.AppendLine($"different lengths: {or.Count()} vs {read.Count()}");

            foreach (var (ori, dec) in or.OrderBy(p => p.Item1).Zip(read.OrderBy(p => p.Item1)))
            {
                var isDiff = false;
                if (ori.Item1 != dec.Item1 || ori.Item2 != dec.Item2)
                    isDiff = true;

                sb.AppendLine($"({ori.Item1}, {ori.Item2})\t({dec.Item1}, {dec.Item2}){(isDiff ? "\tDIFF" : "")}");
            }

            File.WriteAllText($"_dict comparison.txt", sb.ToString());
            Console.WriteLine($"Dictionaries compared");
        }

        static void DecodeFile(string inFilepath, string outFilepath)
        {
            var sw = new Stopwatch();
            sw.Start();

            var fs = new FileStream(inFilepath, FileMode.Open);
            var len = fs.Length;
            var dict = ReadDictionary(fs, out var dictLen);

            var decoder = new DecoderTree(dict, outFilepath);

            var previousPercentage = -1;
            Console.WriteLine($"Decoding... File size: {len / (float)MB:.00}MB");

            var buffer = new byte[2];
            for (int i = dictLen; i < len - 2; i++)
            {
                fs.Read(buffer, 0, 1);

                if (i - dictLen > 1040000)
                {
                    if (buffer[0] == 107)
                    {
                        Console.Write("");
                        DecoderTree.isBreak = true;
                    }
                }

                decoder.FeedBytes(buffer[0]);

                var percentage = (int)(Math.Round(i / (float)len, 2) * 100);
                if (percentage > previousPercentage)
                {
                    previousPercentage = percentage;
                    Console.WriteLine($"Decoded {percentage * 1}%;\tTime elapsed: {sw.ElapsedMilliseconds / 1000f}s");
                }
            }
            fs.Read(buffer, 0, 2);
            decoder.FeedBits(Convert.ToString(buffer[0], 2)[buffer[1]..]);
            decoder.FinishWriting();

            Console.WriteLine("Finished decoding");

            sw.Stop();
        }
        #endregion

        static void PointOutTheDifferences(string file1, string file2)
        {
            var fs1 = new FileStream(file1, FileMode.Open);
            var fs2 = new FileStream(file2, FileMode.Open);

            var sb = new StringBuilder();
            var sw = new Stopwatch();
            sw.Start();

            if (fs1.Length != fs2.Length)
            {
                sb.AppendLine($"Different lengths. File1: {fs1.Length} bytes, File2: {fs2.Length}");
                //return;
            }

            var buffer1 = new byte[1];
            var buffer2 = new byte[1];

            var comparisons = new List<(byte, byte, int)>();

            var len = Math.Min(fs1.Length, fs2.Length);

            Console.WriteLine($"Comparing... File size: {len / (float)MB:.00}MB");
            var previousPercentage = -1;

            for (int i = 0; i < Math.Min(fs1.Length, fs2.Length); i++)
            {
                fs1.Read(buffer1, 0, 1);
                fs2.Read(buffer2, 0, 1);

                comparisons.Add((buffer1[0], buffer2[0], i));

                var percentage = (int)(Math.Round(i / (float)len, 1) * 10);
                if (percentage > previousPercentage)
                {
                    previousPercentage = percentage;
                    Console.WriteLine($"Compared {percentage * 10}%;\tTime elapsed: {sw.ElapsedMilliseconds / 1000f}s");
                }
            }
            fs1.Close();
            fs2.Close();
            sw.Stop();

            //var diffGroupList = comparisons
            //    .Where(p => p.Item1 != p.Item2)
            //    .GroupBy(p => p.Item1)
            //    .OrderBy(group => group.Key)
            //    .ToList();

            //foreach (var group in diffGroupList)
            //{
            //    sb.AppendLine($"{group.Key} was decoded incorrectly");
            //    var actualValues = group.Select(p => p.Item2).Distinct().OrderBy(x => x);
            //    sb.AppendLine(string.Join("\n",
            //        actualValues.Select(x => $"\tas {x}")));
            //}

            var firstMismatch = comparisons.Where(p => (p.Item1 != p.Item2)).OrderBy(p => p.Item3).FirstOrDefault();
            var mismatchedSymbol = comparisons.Where(p => p.Item1 == firstMismatch.Item1).OrderBy(p => p.Item3).FirstOrDefault();

            sb.AppendLine($"First mismatched occured at byte num {firstMismatch.Item3}. It's {firstMismatch.Item1} -> {firstMismatch.Item2}");
            sb.AppendLine($"mismathcedSymbol first occured at byte num {mismatchedSymbol.Item3}. " +
                $"It's {mismatchedSymbol.Item1} -> {mismatchedSymbol.Item2}");

            File.WriteAllText($"_DIFFERENCES. {file1} vs {file2}.txt", sb.ToString());

            Console.WriteLine(sb);
        }

        static void Main(string[] args)
        {
            //try
            //{
            //    EncodeFile("pd.exe", "pd.exe.encoded");
            //    DecodeFile("pd.exe.encoded", "pd_decoded.exe");
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("Operation didn't finish successfully");
            //    Console.WriteLine(e.Message);
            //}

            //EncodeFile("pd.exe", "pd.exe.encoded", out var dict);
            //DecodeFile("pd.exe.encoded", "pd_decoded.exe", dict);

            //EncodeFile("16e1.mp4", "16e1.mp4.encoded", out var dict);
            //DecodeFile("16e1.mp4.encoded", "16e1_decoded.mp4", dict);

            //EncodeFile("pd.exe", "pd.exe.encoded2");
            //DecodeFile("pd.exe.encoded", "pd_decoded2.exe");

            //PointOutTheDifferences("pd.exe", "pd_decoded.exe");
            //PointOutTheDifferences("pd.exe", "pd_decoded2.exe");

            //PointOutTheDifferences("pd.exe.encoded", "pd.exe.encoded2");

            //Encoder.Encode("pd.exe", "pd.exe.encoded3");
            //Decoder.Decode("pd.exe.encoded3", "pd_decoded3.exe");  

            //var sw = new Stopwatch();
            //sw.Start();
            //Encoder.Encode("16e1.mp4", "16e1.mp4.encoded");
            //Decoder.Decode("16e1.mp4.encoded", "16e1_decoded.mp4");

            //Console.WriteLine($"Total time elapsed: {sw.ElapsedMilliseconds / 1000f:.00}s");

            Console.WriteLine(Path.GetDirectoryName(@"E:\imma_coder\dotnet\TIK2\TIK2\CompressionExample.cs"));
        }
    }
}
