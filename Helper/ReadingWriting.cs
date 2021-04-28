using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Helper
{
    public static class ReadingWriting
    {
        public static int MB = 1048576;

        public static long[] GetFrequencyArray(string filepath, Stopwatch sw,
            Action<string> updateLog, string progressString, CancellationToken token)
        {
            sw.Start();
            var count = new ConcurrentDictionary<byte, long>();
            for (int i = 0; i < 256; i++)
                count.GetOrAdd((byte)i, 0);

            var fs = new FileStream(filepath, FileMode.Open);
            long len = fs.Length;
            var chunkSize = 1 * MB;
            //var chunkSize = 1;
            var buffer = new byte[chunkSize];

            var previousPercentage = -1;
            for (long i = 0; i < len; i += chunkSize)
            {
                if (token.IsCancellationRequested)
                {
                    fs.Close();
                    updateLog("");
                    sw.Reset();
                    return null;
                }

                var percentage = (int)(HelperMath.Round(i / (float)len, 2) * 100);
                if (percentage > previousPercentage)
                {
                    previousPercentage = percentage;
                    updateLog($"{progressString} {percentage * 1}%;\tTime elapsed: {sw.ElapsedMilliseconds / 1000f:.00}s");
                }

                fs.Read(buffer, 0, chunkSize);

                var higherLimit = HelperMath.Min(chunkSize, len - i);
                Parallel.For(0, higherLimit, j =>
                {
                    count.AddOrUpdate(buffer[j], 1, (key, old) => old + 1);
                });
            }
            fs.Close();

            return count.Select(x => x.Value).ToArray();
        }
    }
}
