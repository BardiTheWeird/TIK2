using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    public static class HelperMath
    {
        public static bool IsPowerOfTwo(this int x) =>
            (x > 0) && ((x & (x - 1)) == 0);
    }
}
