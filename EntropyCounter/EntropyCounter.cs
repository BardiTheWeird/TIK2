using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EntropyCounter
{
    public class EntropyCounter : INotifyPropertyChanged
    {
        static int MB = 1048576;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Log { get; set; }

        private Stopwatch _sw = new Stopwatch();

        static double CountEntropy(IEnumerable<long> count)
        {
            var sum = count.Sum();

            return count.Select(x => { 
                var p = x / (double)sum; 
                return -1 * p * Math.Log2(p); 
            }).Sum();
        }

        public static double FromString(string input)
        {
            var count = input.GroupBy(c => c)
                .Select(c => (long)c.Count());

            return CountEntropy(count);
        }

        public static long[] GetFrequencyArray(string filepath, Stopwatch sw, 
            Action<string> updateLog, string progressString, CancellationToken token)
        {
            sw.Start();
            var count = new ConcurrentDictionary<byte, long>();

            var fs = new FileStream(filepath, FileMode.Open);
            var len = fs.Length;
            var chunkSize = 1 * MB;
            var buffer = new byte[chunkSize];

            var previousPercentage = -1;
            for (int i = 0; i < len; i += chunkSize)
            {
                if (token.IsCancellationRequested)
                {
                    fs.Close();
                    updateLog("");
                    sw.Reset();
                    return null;
                }

                var percentage = (int)(Math.Round(i / (float)len, 2) * 100);
                if (percentage > previousPercentage)
                {
                    previousPercentage = percentage;
                    updateLog($"{progressString} {percentage * 1}%;\tTime elapsed: {sw.ElapsedMilliseconds / 1000f:.00}s");
                }

                fs.Read(buffer, 0, chunkSize);

                var higherLimit = Math.Min(chunkSize, len - i);
                Parallel.For(0, higherLimit, j =>
                {
                    count.AddOrUpdate(buffer[j], 1, (key, old) => old + 1);
                });
            }
            fs.Close();

            return count.Select(x => x.Value).ToArray();
        }

        public double? FromFilePath(string filepath, CancellationToken token, out long msElapsed)
        {
            var count = GetFrequencyArray(filepath, _sw, x => Log = x, "Calculating entropy...", token);

            Log = "";
            msElapsed = _sw.ElapsedMilliseconds;
            _sw.Reset();

            if (token.IsCancellationRequested)
                return null;

            return CountEntropy(count);
        }
    }
}
