using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace BackTester
{
    public static class DataManager
    {
        private static Dictionary<string, List<BarData>> fullData = new Dictionary<string, List<BarData>>();

        static DataManager()
        {
            List<string> stocks = GetStocks();

            for (int i = 0; i < stocks.Count; i++)
            {
                fullData.Add(stocks[i], GetBars(stocks[i]));
            }
        }

        /// <summary>
        /// Get all historical, daily bars for a given symbol.
        /// </summary>
        public static List<BarData> GetBars(string symbol)
        {
            List<BarDataRaw> list = new List<BarDataRaw>();
            string sourceFile = Settings.DATA_DIR + $"/{symbol}.csv";

            using (TextReader fileReader = File.OpenText(sourceFile))
            {
                var csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                list = csv.GetRecords<BarDataRaw>().ToList();
            }

            list.Reverse();

            return GetAdjusted(list);
        }

        /// <summary>
        /// Get all historical daily bars that start whenever but must include the given timeframe.
        /// </summary>
        public static List<BarData> GetBars(string symbol, DateTime startDate, DateTime endDate)
        {
            List<BarData> list = fullData.First(x => x.Key == symbol).Value;

            int startIndex = -1;
            DateTime realStartDate = new DateTime();

            int endIndex = -1;
            DateTime realEndDate = new DateTime();

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].TimeStamp >= startDate &&
                    realStartDate == new DateTime())
                {
                    startIndex = i;
                    realStartDate = list[i].TimeStamp;
                }

                if (list[i].TimeStamp > endDate &&
                    realEndDate == new DateTime())
                {
                    endIndex = i - 1;
                    realEndDate = list[i - 1].TimeStamp;

                    if (realStartDate <= realEndDate)
                    {
                        return list.GetRange(0, endIndex + 1);
                    }
                }
            }


            // something must be wrong with the data, should never trigger
            throw new Exception("Invalid stock data for " + symbol);
        }

        /// <summary>
        /// Retuns a list of sp500 raw stock data.
        /// </summary>
        public static List<string> GetSP500Raw()
        {
            string[] lines = File.ReadAllLines(Settings.RAW_SP500_DIR);
            List<string> stocks = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                stocks.Add(lines[i]);
            }

            return stocks;
        }

        /// <summary>
        /// Retuns a list of raw stock data.
        /// </summary>
        public static List<string> GetStocksRaw()
        {
            string[] lines = File.ReadAllLines(Settings.RAW_STOCKS_DIR);
            List<string> stocks = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                stocks.Add(lines[i]);
            }

            return stocks;
        }


        /// <summary>
        /// Retuns a list of stocks that we have data for.
        /// </summary>
        public static List<string> GetStocks(bool halfBake = false)
        {
            DirectoryInfo d = new DirectoryInfo(Settings.DATA_DIR);
            FileInfo[] files = d.GetFiles("*.csv");

            List<string> stocks = new List<string>();

            int amount = halfBake == true ? Settings.HALF_BAKE_COUNT : files.Length;

            for (int i = 0; i < amount; i++)
            {
                stocks.Add(files[i].ToString().Substring(0, files[i].ToString().Length - 4));
            }

            return stocks;
        }

        /// <summary>
        /// Was the stock market open on this date?
        /// </summary>
        public static bool IsStockDay(DateTime day)
        {
            List<BarData> check = GetBars("AAPL");

            foreach (BarData bar in check)
            {
                if (bar.TimeStamp == day)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Uses adjusted close prices to fix other bar values.
        /// </summary>
        private static List<BarData> GetAdjusted(List<BarDataRaw> unadjusted)
        {
            List<BarData> fix = new List<BarData>();

            for (int i = 0; i < unadjusted.Count; i++)
            {
                BarDataRaw newBar = unadjusted[i];
                double ratio = newBar.AdjustedClose / newBar.Close;

                fix.Add(new BarData() { Open = newBar.Open * ratio,
                                        Close = newBar.Close * ratio,
                                        High = newBar.High * ratio,
                                        Low = newBar.Low * ratio,
                                        Volume = newBar.Volume,
                                        TimeStamp = newBar.TimeStamp });
            }

            return fix;
        }
    }
}
