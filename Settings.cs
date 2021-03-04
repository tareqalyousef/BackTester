using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTester
{
    public static class Settings
    {
        #region Directories

        /// <summary>
        /// The location of the text file containing the list of stocks.
        /// </summary>
        public static string RAW_STOCKS_DIR = @"C:\Users\tareq\Desktop\VSProjects\BackTester\data\stocks.txt";

        /// <summary>
        /// The location of the text file containing the list of sp500 stocks.
        /// </summary>
        public static string RAW_SP500_DIR = @"C:\Users\tareq\Desktop\VSProjects\BackTester\data\sp500.txt";

        /// <summary>
        /// The location of the daily bars.
        /// </summary>
        public static string DATA_DIR = @"C:\Users\tareq\Desktop\VSProjects\BackTester\data\";

        /// <summary>
        /// The location of the text file containing logs.
        /// </summary>
        public static string LOGS_DIR = @"C:\Users\tareq\Desktop\VSProjects\BackTester\logs\";

        #endregion

        #region Delays

        /// <summary>
        /// The amount of time to wait when an error is raised.
        /// </summary>
        public static int ERROR_DELAY = 5 * 1000;

        #endregion

        #region Keys

        /// <summary>
        /// The data key for Alpha Vantage.
        /// </summary>
        public static string DATA_KEY = "V3YQ3XZNS7E3ROCX";

        #endregion

        #region Simulation

        /// <summary>
        /// At what date should the simulation start?
        /// </summary>
        public static DateTime START_DATE = new DateTime(2016, 1, 2);

        /// <summary>
        /// At what date should the simulation end?
        /// </summary>
        public static DateTime END_DATE = new DateTime(2020, 1, 2);

        /// <summary>
        /// How much money do we start with?
        /// </summary>
        public static double INITIAL_EQUITY = 30000;

        /// <summary>
        /// How many stocks should we look at in half-bake mode?
        /// </summary>
        public static int HALF_BAKE_COUNT = 200;

        #endregion

        #region Data Collection

        /// <summary>
        /// Every stock should have data at or directly before this point.
        /// </summary>
        public static DateTime MAX_START_DATE = new DateTime(2010, 1, 4);

        /// <summary>
        /// Every stock should have data up to this point.
        /// </summary>
        public static DateTime MIN_END_DATE = new DateTime(2020, 11, 27);

        #endregion

        #region Calculator

        /// <summary>
        /// The number of bars to check when looking for a trend.
        /// </summary>
        public static int TREND_BAR_COUNT = 5;

        #endregion
    }
}
