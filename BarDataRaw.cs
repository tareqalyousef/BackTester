using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTester
{
    class BarDataRaw
    {
        [Name("timestamp")]
        public DateTime TimeStamp { get; set; }

        [Name("open")]
        public double Open { get; set; }

        [Name("high")]
        public double High { get; set; }

        [Name("close")]
        public double Close { get; set; }

        [Name("low")]
        public double Low { get; set; }

        [Name("volume")]
        public double Volume { get; set; }

        [Name("adjusted_close")]
        public double AdjustedClose { get; set; }

        [Name("dividend_amount")]
        public double DividendAmount { get; set; }

        [Name("split_coefficient")]
        public double SplitCoefficient { get; set; }
    }
}
