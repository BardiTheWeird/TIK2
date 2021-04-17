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
        public event PropertyChangedEventHandler PropertyChanged;

        public string Log { get; set; }

        private Stopwatch _sw = new Stopwatch();

        static double CountEntropy(IEnumerable<long> count)
        {
            var sum = count.Sum();

            return count.Where(x => x > 0)
                .Select(x => { 
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

        public double? FromFilePath(string filepath, CancellationToken token, out long msElapsed)
        {
            var count = Helper.ReadingWriting.GetFrequencyArray(filepath, _sw, x => Log = x, "Calculating entropy...", token);

            Log = "";

            File.WriteAllText("count.txt", 
                string.Join("\n", Enumerable.Range(0, 256)
                .Select(i => $"{i} -> {count[i]}")));

            msElapsed = _sw.ElapsedMilliseconds;
            _sw.Reset();

            if (token.IsCancellationRequested)
                return null;

            return CountEntropy(count);
        }
    }
}
