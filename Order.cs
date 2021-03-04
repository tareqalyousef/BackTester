using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTester
{
    public class Order
    {
        /// <summary>
        /// Are we buying or selling?
        /// </summary>
        public ActionType ActionType { get; }

        /// <summary>
        /// Are we using a limit?
        /// </summary>
        public OrderType OrderType { get; }

        /// <summary>
        /// The symbol of the position.
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// The price we will use with limits.
        /// </summary>
        public double LimitPrice { get; }

        /// <summary>
        /// The date of order being entered.
        /// </summary>
        public DateTime Date { get; }

        /// <summary>
        /// The number of shares of this position we are trying to buy or sell.
        /// </summary>
        public int Shares { get; }

        /// <summary>
        /// Create a market order.
        /// </summary>
        public Order(ActionType actionType, string symbol, DateTime date, int shares)
        {
            OrderType = OrderType.Market;

            ActionType = actionType;
            Symbol = symbol;
            Date = date;
            Shares = shares;
        }

        /// <summary>
        /// Create a limit order.
        /// </summary>
        public Order(ActionType actionType, double limitPrice, string symbol, DateTime date, int shares)
        {
            OrderType = OrderType.Limit;

            ActionType = actionType;
            Symbol = symbol;
            LimitPrice = limitPrice;
            Date = date;
            Shares = shares;
        }
    }

    public enum OrderType
    {
        Market, Limit
    }

    public enum ActionType
    {
        Buy, Sell
    }
}
