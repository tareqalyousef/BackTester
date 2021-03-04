using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTester
{
    /// <summary>
    /// An example strategy--it trades the golden cross on sp500.
    /// </summary>
    public class ExampleStrategy : IStrategy
    {
        private int len1;
        private int len2;
        private double amt;

        /// <summary>
        /// Create a moving average strategy.
        /// </summary>
        /// <param name="maLongLength">The length of the longer moving average.</param>
        /// <param name="maShortLength">The length of the shorter moving average.</param>
        /// <param name="buyPrice">The amount of equity we spend on each buy signal.</param>
        public ExampleStrategy(int maLongLength, int maShortLength, double buyPrice)
        {
            len1 = maLongLength;
            len2 = maShortLength;  
            amt = buyPrice;
        }

        public void Update(Account account)
        {
            account.CancelOrders();

            // we can loop through all the bars we currently have
            // and decide which stocks to invest or sell
            for (int i = 0; i < account.MarketState.Count; i++)
            {
                string symbol = account.MarketState.ElementAt(i).Key;
                List<BarData> data = account.MarketState.ElementAt(i).Value;

                // is this stock in the sp500?
                if (!DataManager.GetSP500Raw().Contains(symbol)) { continue; }

                // calculate the moving averages
                double ma1Curr = Calculator.MovingAverage(data.Select(x => x.Close).ToList(), len1);
                double ma2Curr = Calculator.MovingAverage(data.Select(x => x.Close).ToList(), len2);

                // calculate the last moving average to determine cross
                double ma1Last = Calculator.MovingAverage(data.Take(data.Count - 1).Select(x => x.Close).ToList(), len1);
                double ma2Last = Calculator.MovingAverage(data.Take(data.Count - 1).Select(x => x.Close).ToList(), len2);

                // buy signal when short term moving averge crosses above long term
                if (ma2Curr > ma1Curr &&
                    ma2Last <= ma1Last)
                {
                    int shares = (int)(amt / data.Last().Close);
                    account.PlaceMarketBuy(symbol, shares);
                }
                else if (ma2Curr <= ma1Curr &&
                         ma2Last > ma1Last)
                {
                    // how many shares do we have?
                    int shares = account.GetShares(symbol);
                    account.PlaceMarketSell(symbol, shares);
                }
            }
        }

        public void ProcessOrder(Account account, Order order, double price) { }

        public void ProcessBoxOrder(Account account, BoxOrder order, double price) { }
    }
}