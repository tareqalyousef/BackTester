using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTester
{
    public class Account
    {
        /// <summary>
        /// The strategy that will make calls on this account.
        /// </summary>
        public IStrategy Strategy { get; private set; }

        /// <summary>
        /// What is the current date in the simulation?
        /// </summary>
        public DateTime CurrentDate { get; private set; }

        /// <summary>
        /// When do we start simulating?
        /// </summary>
        public DateTime StartDate { get; private set; }

        /// <summary>
        /// When do we stop simulating?
        /// </summary>
        public DateTime EndDate { get; private set; }

        /// <summary>
        /// The days in the account window where the stock market was open (up to the current date).
        /// </summary>
        public List<DateTime> StockDays { get; private set; } = new List<DateTime>();

        /// <summary>
        /// Is the market open today?
        /// </summary>
        public bool StockDay { get; private set; }

        /// <summary>
        /// What does the market look like in the simulation date? This list
        /// contains a list of every stock, as well as its historical bar data.
        /// </summary>
        public Dictionary<string, List<BarData>> MarketState { get; private set; } = new Dictionary<string, List<BarData>>();

        /// <summary>
        /// The initial value of the account.
        /// </summary>
        public double InitialEquity { get; private set; }

        /// <summary>
        /// The current, total value of the account.
        /// </summary>
        public double Equity { get; private set; }

        /// <summary>
        /// What were our daily equity values from the start date to and including today?
        /// </summary>
        public List<Tuple<DateTime, double>> EquityHistory { get; private set; } = new List<Tuple<DateTime, double>>();

        /// <summary>
        /// What were our daily buying power values from the start date to and including today?
        /// </summary>
        public List<Tuple<DateTime, double>> BuyingPowerHistory { get; private set; } = new List<Tuple<DateTime, double>>();

        /// <summary>
        /// The amount of money that can be spent.
        /// </summary>
        public double BuyingPower { get; private set; }

        /// <summary>
        /// Every pending buy or sell order on the account.
        /// </summary>
        public List<Order> Orders { get; private set; } = new List<Order>();

        /// <summary>
        /// Every pending box order on the account.
        /// </summary>
        public List<BoxOrder> BoxOrders { get; private set; } = new List<BoxOrder>();

        /// <summary>
        /// Every position currently on the account.
        /// </summary>
        public List<Position> Positions { get; private set; } = new List<Position>();

        /// <summary>
        /// The number of sales that have resulted in a net gain in equity.
        /// </summary>
        public int Wins { get; private set; } = 0;

        /// <summary>
        /// The number of sales that have resulted in a net loss in equity.
        /// </summary>
        public int Losses { get; private set; } = 0;

        /// <summary>
        /// Are we in half-bake mode? With this mode, we look at a fewer
        /// set of stocks to simulate faster at the expense of accuracy.
        /// </summary>
        public bool HalfBake { get; private set; }

        /// <summary>
        /// The unique ID of the current backtest that will be used for logs.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Logs that will be written to a local directory.
        /// </summary>
        public List<Tuple<DateTime, string>> Logs { get; private set; } = new List<Tuple<DateTime, string>>();

        public Account(IStrategy strategy, DateTime startDate, DateTime endDate, double equity, bool halfBake)
        {
            Strategy = strategy;
            StartDate = startDate;
            CurrentDate = startDate;
            EndDate = endDate;
            InitialEquity = equity;
            BuyingPower = equity;
            Equity = equity;
            HalfBake = halfBake;
            EquityHistory.Add(new Tuple<DateTime, double>(startDate, equity));
            BuyingPowerHistory.Add(new Tuple<DateTime, double>(startDate, BuyingPower));
            ID = new Random().Next(1000000, 9999999);
        }

        /// <summary>
        /// Simulate a day in the market.
        /// </summary>
        public void Simulate()
        {
            if (CurrentDate != EndDate)
            {
                CurrentDate = CurrentDate.AddDays(1);
                StockDay = DataManager.IsStockDay(CurrentDate);

                // before we even bother with any calculations, we need to
                // make sure the stock market was open on this given day
                if (StockDay)
                {
                    UpdateMarketState();
                    StockDays.Add(CurrentDate);
                    Log("Stockday on " + CurrentDate + ", proceeding...");

                    // how has our equity changed over postmarket and premarket
                    for (int i = 0; i < Positions.Count; i++)
                    {
                        List<BarData> history = MarketState.FirstOrDefault(x => x.Key == Positions[i].Symbol).Value;

                        // how much has the stock changed in value overnight?
                        double change = history.Last().Open - history[history.Count - 2].Close;

                        // change our equity!
                        Equity += change * Positions[i].Shares;
                    }

                    // process orders
                    ProcessAllOrders();

                    // with our new positions, how will our equity change today?
                    for (int i = 0; i < Positions.Count; i++)
                    {
                        List<BarData> history = MarketState.FirstOrDefault(x => x.Key == Positions[i].Symbol).Value;

                        // how much has the stock changed in value overnight?
                        double change = history.Last().Close - history.Last().Open;

                        // change our equity!
                        Equity += change * Positions[i].Shares;

                        // update the position value
                        Positions[i].PriceHistory.Add(new Tuple<DateTime, double, double>(CurrentDate, history.Last().Close, change));
                    }

                    // allow the strategy to make calls for the next day
                    Strategy.Update(this);

                    // process all logs for the day
                    ProcessLogs();
                }

                EquityHistory.Add(new Tuple<DateTime, double>(CurrentDate, Equity));
                BuyingPowerHistory.Add(new Tuple<DateTime, double>(CurrentDate, BuyingPower));
            }
            else
            {
                throw new Exception("Simulation is already complete.");
            }
        }

        /// <summary>
        /// Processes orders and box orders.
        /// </summary>
        private void ProcessAllOrders()
        {
            // now, what orders were placed overnight?
            for (int i = 0; i < Orders.Count; i++)
            {
                List<BarData> history = MarketState.FirstOrDefault(x => x.Key == Orders[i].Symbol).Value;

                if (Orders[i].ActionType == ActionType.Buy)
                {
                    if (Orders[i].OrderType == OrderType.Limit)
                    {
                        double realBuyPrice = -1;

                        // has the stocks price gone over or below the limit price today?
                        if (history.Last().High >= Orders[i].LimitPrice &&
                            history.Last().Low <= Orders[i].LimitPrice)
                        {
                            realBuyPrice = Orders[i].LimitPrice;
                        }
                        else if (Orders[i].LimitPrice < history.Last().Open && Orders[i].LimitPrice > history[history.Count - 2].High)
                        {
                            realBuyPrice = history.Last().Open;
                        }
                        else if (Orders[i].LimitPrice > history.Last().Open && Orders[i].LimitPrice < history[history.Count - 2].Low)
                        {
                            realBuyPrice = history.Last().Open;
                        }

                        if (realBuyPrice != -1)
                        {
                            // if so, can we afford to buy this many shares?
                            if (Orders[i].LimitPrice * Orders[i].Shares <= BuyingPower)
                            {
                                // buy the shares!
                                BuyingPower -= realBuyPrice * Orders[i].Shares;

                                // place a position
                                Positions.Add(new Position(Orders[i].Symbol, Orders[i].Shares, realBuyPrice, CurrentDate));

                                // fire event
                                Strategy.ProcessOrder(this, Orders[i], realBuyPrice);

                                // log
                                Log("Bought " + Orders[i].Shares + " shares of " + Orders[i].Symbol + " for $" + realBuyPrice + " per share.");

                                // remove this order
                                Orders.Remove(Orders[i]);
                            }
                        }
                    }
                    else if (Orders[i].OrderType == OrderType.Market)
                    {
                        if (history.Last().Open * Orders[i].Shares <= BuyingPower)
                        {
                            // buy the shares!
                            BuyingPower -= history.Last().Open * Orders[i].Shares;

                            // place a position
                            Positions.Add(new Position(Orders[i].Symbol, Orders[i].Shares, history.Last().Open, CurrentDate));

                            // fire event
                            Strategy.ProcessOrder(this, Orders[i], history.Last().Open);

                            // log
                            Log("Bought " + Orders[i].Shares + " shares of " + Orders[i].Symbol + " for $" + history.Last().Open + " per share.");

                            // remove this order
                            Orders.Remove(Orders[i]);
                        }
                    }
                }
                else if (Orders[i].ActionType == ActionType.Sell)
                {
                    if (Orders[i].OrderType == OrderType.Limit)
                    {
                        double realSellPrice = -1;

                        // has the stocks price gone over or below the limit price today?
                        if (history.Last().High >= Orders[i].LimitPrice &&
                            history.Last().Low <= Orders[i].LimitPrice)
                        {
                            realSellPrice = Orders[i].LimitPrice;
                        }
                        else if (Orders[i].LimitPrice < history.Last().Open && Orders[i].LimitPrice > history[history.Count - 2].High)
                        {
                            realSellPrice = history.Last().Open;
                        }
                        else if (Orders[i].LimitPrice > history.Last().Open && Orders[i].LimitPrice < history[history.Count - 2].Low)
                        {
                            realSellPrice = history.Last().Open;
                        }

                        if (realSellPrice != -1)
                        {
                            // sell the shares!
                            BuyingPower += realSellPrice * Orders[i].Shares;
                            Equity += (realSellPrice - history.Last().Open) * Orders[i].Shares;

                            int toRemove = Orders[i].Shares;
                            List<Position> positionsToRemove = new List<Position>();

                            // remove the position
                            for (int j = 0; j < Positions.Count; j++)
                            {
                                if (Positions[j].Symbol == Orders[i].Symbol)
                                {
                                    if (Positions[j].BuyPrice >= realSellPrice)
                                    {
                                        Losses++;
                                    }
                                    else
                                    {
                                        Wins++;
                                    }

                                    if (Positions[j].Shares > toRemove)
                                    {
                                        Positions[j].Shares -= toRemove;
                                        break;
                                    }
                                    else if (Positions[j].Shares == toRemove)
                                    {
                                        positionsToRemove.Add(Positions[j]);
                                        break;
                                    }
                                    else if (Positions[j].Shares < toRemove)
                                    {
                                        positionsToRemove.Add(Positions[j]);
                                        toRemove -= Positions[j].Shares;
                                    }
                                }
                            }

                            foreach (Position pos in positionsToRemove)
                            {
                                Positions.Remove(pos);
                            }

                            // fire event
                            Strategy.ProcessOrder(this, Orders[i], realSellPrice);

                            // log
                            Log("Sold " + Orders[i].Shares + " shares of " + Orders[i].Symbol + " for $" + realSellPrice + " per share.");

                            // remove this order
                            Orders.Remove(Orders[i]);
                        }
                    }
                    else if (Orders[i].OrderType == OrderType.Market)
                    {
                        // sell the shares!
                        BuyingPower += history.Last().Open * Orders[i].Shares;

                        int toRemove = Orders[i].Shares;
                        List<Position> positionsToRemove = new List<Position>();

                        // remove the position
                        for (int j = 0; j < Positions.Count; j++)
                        {
                            if (Positions[j].Symbol == Orders[i].Symbol)
                            {
                                if (Positions[j].BuyPrice >= history.Last().Open)
                                {
                                    Losses++;
                                }
                                else
                                {
                                    Wins++;
                                }

                                if (Positions[j].Shares > toRemove)
                                {
                                    Positions[j].Shares -= toRemove;
                                    break;
                                }
                                else if (Positions[j].Shares == toRemove)
                                {
                                    positionsToRemove.Add(Positions[j]);
                                    break;
                                }
                                else if (Positions[j].Shares < toRemove)
                                {
                                    positionsToRemove.Add(Positions[j]);
                                    toRemove -= Positions[j].Shares;
                                }
                            }
                        }

                        foreach (Position pos in positionsToRemove)
                        {
                            Positions.Remove(pos);
                        }

                        // fire event
                        Strategy.ProcessOrder(this, Orders[i], history.Last().Open);

                        // log
                        Log("Sold " + Orders[i].Shares + " shares of " + Orders[i].Symbol + " for $" + history.Last().Open + " per share.");

                        // remove this order
                        Orders.Remove(Orders[i]);
                    }
                }
            }

            // now, what box orders were placed overnight?
            for (int i = 0; i < BoxOrders.Count; i++)
            {
                List<BarData> history = MarketState.FirstOrDefault(x => x.Key == BoxOrders[i].Symbol).Value;

                if (BoxOrders[i].ActionType == ActionType.Buy)
                {
                    // todo:
                }
                else if (BoxOrders[i].ActionType == ActionType.Sell)
                {
                    double realSellPrice = -1;

                    // has the stocks price gone over or below either of the limit prices today?
                    if (history.Last().High >= BoxOrders[i].UpperLimitPrice &&
                        history.Last().Low <= BoxOrders[i].UpperLimitPrice)
                    {
                        realSellPrice = BoxOrders[i].UpperLimitPrice;
                    }
                    else if (history.Last().High >= BoxOrders[i].LowerLimitPrice &&
                             history.Last().Low <= BoxOrders[i].LowerLimitPrice)
                    {
                        realSellPrice = BoxOrders[i].LowerLimitPrice;
                    }
                    else if (BoxOrders[i].UpperLimitPrice < history.Last().Open && BoxOrders[i].UpperLimitPrice > history[history.Count - 2].High)
                    {
                        realSellPrice = history.Last().Open;
                    }
                    else if (BoxOrders[i].LowerLimitPrice < history.Last().Open && BoxOrders[i].LowerLimitPrice > history[history.Count - 2].High)
                    {
                        realSellPrice = history.Last().Open;
                    }
                    else if (BoxOrders[i].UpperLimitPrice > history.Last().Open && BoxOrders[i].UpperLimitPrice < history[history.Count - 2].Low)
                    {
                        realSellPrice = history.Last().Open;
                    }
                    else if (BoxOrders[i].LowerLimitPrice > history.Last().Open && BoxOrders[i].LowerLimitPrice < history[history.Count - 2].Low)
                    {
                        realSellPrice = history.Last().Open;
                    }

                    if (realSellPrice != -1)
                    {
                        // sell the shares!
                        BuyingPower += realSellPrice * BoxOrders[i].Shares;
                        Equity += (realSellPrice - history.Last().Open) * BoxOrders[i].Shares;

                        int toRemove = BoxOrders[i].Shares;
                        List<Position> positionsToRemove = new List<Position>();

                        // remove the position
                        for (int j = 0; j < Positions.Count; j++)
                        {
                            if (Positions[j].Symbol == BoxOrders[i].Symbol)
                            {
                                if (Positions[j].BuyPrice >= realSellPrice)
                                {
                                    Losses++;
                                }
                                else
                                {
                                    Wins++;
                                }

                                if (Positions[j].Shares > toRemove)
                                {
                                    Positions[j].Shares -= toRemove;
                                    break;
                                }
                                else if (Positions[j].Shares == toRemove)
                                {
                                    positionsToRemove.Add(Positions[j]);
                                    break;
                                }
                                else if (Positions[j].Shares < toRemove)
                                {
                                    positionsToRemove.Add(Positions[j]);
                                    toRemove -= Positions[j].Shares;
                                }
                            }
                        }

                        foreach (Position pos in positionsToRemove)
                        {
                            Positions.Remove(pos);
                        }

                        // fire event
                        Strategy.ProcessBoxOrder(this, BoxOrders[i], realSellPrice);

                        // log
                        Log("Sold " + BoxOrders[i].Shares + " shares of " + BoxOrders[i].Symbol + " for $" + realSellPrice + " per share.");

                        // remove this order
                        BoxOrders.Remove(BoxOrders[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Using the current day, we update the market information.
        /// </summary>
        private void UpdateMarketState()
        {
            List<string> stocks = DataManager.GetStocks(HalfBake);
            MarketState.Clear();

            for (int i = 0; i < stocks.Count; i++)
            {
                MarketState.Add(stocks[i], DataManager.GetBars(stocks[i], StartDate, CurrentDate));
            }
        }

        /// <summary>
        /// Place a market buy.
        /// </summary>
        public void PlaceMarketBuy(string symbol, int shares)
        {
            Orders.Add(new Order(ActionType.Buy, symbol, CurrentDate, shares));
        }

        /// <summary>
        /// Place a limit buy.
        /// </summary>
        public void PlaceLimitBuy(string symbol, int shares, double price)
        {
            Orders.Add(new Order(ActionType.Buy, price, symbol, CurrentDate, shares));
        }

        /// <summary>
        /// Place a market sell.
        /// </summary>
        public void PlaceMarketSell(string symbol, int shares)
        {
            // we cannot place an order when we have box orders
            foreach (BoxOrder order in BoxOrders)
            {
                if (order.Symbol == symbol && order.ActionType == ActionType.Sell)
                {
                    throw new Exception("Cannot make order for stock " + symbol);
                }
            }

            // do we even have this many shares?
            int owned = 0;

            foreach (Position pos in Positions)
            {
                if (pos.Symbol == symbol)
                {
                    owned += pos.Shares;
                }
            }

            // how many sells do we already have?
            int ownedSells = 0;

            foreach (Order order in Orders)
            {
                if (order.Symbol == symbol)
                {
                    ownedSells += order.Shares;
                }
            }

            if (shares <= (owned - ownedSells))
            {
                Orders.Add(new Order(ActionType.Sell, symbol, CurrentDate, shares));
            }
            else
            {
                throw new Exception("Cannot place order for " + shares + " shares of " + symbol);
            }
        }

        /// <summary>
        /// Place a limit sell.
        /// </summary>
        public void PlaceLimitSell(string symbol, int shares, double price)
        {
            // we cannot place an order when we have box orders
            foreach (BoxOrder order in BoxOrders)
            {
                if (order.Symbol == symbol && order.ActionType == ActionType.Sell)
                {
                    throw new Exception("Cannot make order for stock " + symbol);
                }
            }

            // do we even have this many shares?
            int owned = 0;

            foreach (Position pos in Positions)
            {
                if (pos.Symbol == symbol)
                {
                    owned += pos.Shares;
                }
            }

            // how many sells do we already have?
            int ownedSells = 0;

            foreach (Order order in Orders)
            {
                if (order.Symbol == symbol)
                {
                    ownedSells += order.Shares;
                }
            }

            if (shares <= (owned - ownedSells))
            {
                Orders.Add(new Order(ActionType.Sell, price, symbol, CurrentDate, shares));
            }
            else
            {
                throw new Exception("Cannot place order for " + shares + " shares of " + symbol);
            }
        }

        /// <summary>
        /// Place a box order.
        /// </summary>
        public void PlaceBoxOrderSell(string symbol, int shares, double lowerLimit, double upperLimit)
        {
            // we cannot place box orders when we have regular orders
            foreach (Order order in Orders)
            {
                if (order.Symbol == symbol && order.ActionType == ActionType.Sell)
                {
                    throw new Exception("Cannot make box order for stock " + symbol);
                }
            }

            // do we even have this many shares?
            int owned = 0;

            foreach (Position pos in Positions)
            {
                if (pos.Symbol == symbol)
                {
                    owned += pos.Shares;
                }
            }

            // how many sells do we already have?
            int ownedSells = 0;

            foreach (BoxOrder order in BoxOrders)
            {
                if (order.Symbol == symbol)
                {
                    ownedSells += order.Shares;
                }
            }

            if (shares <= (owned - ownedSells))
            {
                BoxOrders.Add(new BoxOrder(ActionType.Sell, symbol, CurrentDate, shares, lowerLimit, upperLimit));
            }
            else
            {
                throw new Exception("Cannot place box order for " + shares + " shares of " + symbol);
            }
        }

        /// <summary>
        /// Add all logs to local directory.
        /// </summary>
        private void ProcessLogs()
        {
            string dir = Settings.LOGS_DIR + ID.ToString() + ".txt";
            string data = "";

            foreach (Tuple<DateTime, string> entry in Logs)
            {
                data += entry.Item1.ToString() + ": " + entry.Item2 + "\n";
            }

            File.WriteAllText(dir, data);
        }

        /// <summary>
        /// Cancel all orders.
        /// </summary>
        public void CancelOrders()
        {
            Orders.Clear();
        }

        /// <summary>
        /// Cancel all box orders.
        /// </summary>
        public void CancelBoxOrders()
        {
            BoxOrders.Clear();
        }

        /// <summary>
        /// Is the simulation complete?
        /// </summary>
        public bool IsComplete() { return CurrentDate == EndDate; }

        /// <summary>
        /// How many sales have we made thus far?
        /// </summary>
        public int GetNumberOfSales()
        {
            return Wins + Losses;
        }

        /// <summary>
        /// How many shares do we have of a given symbol?
        /// </summary>
        public int GetShares(string symbol)
        {
            int amt = 0;

            foreach (Position pos in Positions)
            {
                if (pos.Symbol == symbol)
                {
                    amt += pos.Shares;
                }
            }

            return amt;
        }

        /// <summary>
        /// Add data to the logs.
        /// </summary>
        private void Log(string data)
        {
            Logs.Add(new Tuple<DateTime, string>(DateTime.Now, data));
        }
    }
}