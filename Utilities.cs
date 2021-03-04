using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTester
{
    public static class Utilities
    {
        public static double RoundToIncrement(double value, double increment)
        {
            return Math.Round(value / increment) * increment;
        }
    }
}
