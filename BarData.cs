using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTester
{
    public class BarData
    {
        public DateTime TimeStamp { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Close { get; set; }
        public double Low { get; set; }
        public double Volume { get; set; }

        public override string ToString()
        {
            return string.Format("Timestamp: {0}, Open: {1}, High: {2}, Low: {3}, Close: {4}", TimeStamp, Open, High, Low, Close);
        }
    }
}
