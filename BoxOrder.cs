using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTester
{
    public class BoxOrder
    {
        /// <summary>
        /// Are we buying or selling?
        /// </summary>
        public ActionType ActionType { get; }

        /// <summary>
        /// The symbol of the position.
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// The upper box limit price.
        /// </summary>
        public double UpperLimitPrice { get; }

        /// <summary>
        /// The lower box limit price.
        /// </summary>
        public double LowerLimitPrice { get; }

        /// <summary>
        /// The date of order being entered.
        /// </summary>
        public DateTime Date { get; }

        /// <summary>
        /// The number of shares of this position we are trying to buy or sell.
        /// </summary>
        public int Shares { get; }

        /// <summary>
        /// Create a box order.
        /// </summary>
        public BoxOrder(ActionType actionType, string symbol, DateTime date, int shares, double lowerLimit, double upperLimit)
        {
            ActionType = actionType;
            Symbol = symbol;
            Date = date;
            Shares = shares;

            LowerLimitPrice = lowerLimit;
            UpperLimitPrice = upperLimit;
        }
    }
}
