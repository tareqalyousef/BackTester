using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTester
{
    public class Position
    {
        /// <summary>
        /// The symbol associated with this position.
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// The number of shares we have in this stock.
        /// </summary>
        public int Shares { get; set; }

        /// <summary>
        /// The price per share we paid for this position.
        /// </summary>
        public double BuyPrice { get; }

        /// <summary>
        /// The history of prices and changes for this position up till current day.
        /// </summary>
        public List<Tuple<DateTime, double, double>> PriceHistory { get; } = new List<Tuple<DateTime, double, double>>();

        /// <summary>
        /// The day we acquired this position.
        /// </summary>
        public DateTime Date { get; }

        public Position(string symbol, int shares, double buyPrice, DateTime date)
        {
            Symbol = symbol;
            Shares = shares;
            BuyPrice = buyPrice;
            Date = date;
        }
    }
}
