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
        private static readonly int MB = 1024 * 1024;

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
            PointOutTheDifferences("pd.exe", "pd_decoded.exe");
        }
    }
}
