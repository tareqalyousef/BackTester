using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Extreme.Mathematics.Curves;
using ScottPlot;
using ScottPlot.WPF;
using ServiceStack;

namespace BackTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker dataCollectionWorker;
        private BackgroundWorker simulationWorker;
        private BackgroundWorker experimentalWorker;

        public MainWindow()
        {
            InitializeComponent();

            plotUpper.plt.Style(ScottPlot.Style.Gray2);
            plotUpper.plt.Ticks(dateTimeX: true);
            plotUpper.plt.Legend(location: legendLocation.lowerLeft);

            plotLower.plt.Style(ScottPlot.Style.Gray2);
            plotLower.plt.Ticks(dateTimeX: true);
            plotLower.plt.Legend(location: legendLocation.lowerLeft);

            dataCollectionWorker = new BackgroundWorker();
            simulationWorker = new BackgroundWorker();
            experimentalWorker = new BackgroundWorker();

            dataCollectionWorker.WorkerSupportsCancellation = true;
            simulationWorker.WorkerSupportsCancellation = true;
            experimentalWorker.WorkerSupportsCancellation = true;

            dataCollectionWorker.DoWork += DataCollectionWorker_DoWork;
            simulationWorker.DoWork += SimulationWorker_DoWork;
            experimentalWorker.DoWork += ExperimentalWorker_DoWork;
        }

        /// <summary>
        /// Wait a given time.
        /// </summary>
        public async Task Wait(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }

        /// <summary>
        /// Add a text to the debug window.
        /// </summary>
        public void Debug(string text)
        {
            debug.Dispatcher.Invoke(new Action(() =>
            {
                if (debug.Text == "")
                {
                    debug.Text = DateTime.Now + ": " + text;
                }
                else
                {
                    debug.Text = DateTime.Now + ": " + text + "\n" + debug.Text;
                }
            }));
        }

        /// <summary>
        /// Update all data in the UI.
        /// </summary>
        private void UpdateUI(Account account)
        {
            double totalProfit = account.Equity - account.InitialEquity;

            // winrate
            if (account.Losses != 0)
            {
                winrateValue.Dispatcher.Invoke(new Action(() =>
                {
                    double winrate = ((double)account.Wins / (account.Wins + account.Losses)) * 100;
                    winrateValue.Text = Utilities.RoundToIncrement(winrate, 0.01) + "%";
                }));
            }

            // average change per day
            if (account.StockDays.Count != 0)
            {
                avgChangeDayValue.Dispatcher.Invoke(new Action(() =>
                {
                    double perDay = Math.Pow(Math.E, Math.Log((account.Equity / account.InitialEquity)) / account.StockDays.Count);
                    double difference = perDay * 100 - 100;

                    avgChangeDayValue.Text = (perDay >= 1 ? "+" : "") + Utilities.RoundToIncrement(difference, 0.01) + "%";
                }));
            }

            // average change per stock
            if (account.GetNumberOfSales() != 0)
            {
                avgChangeSellValue.Dispatcher.Invoke(new Action(() =>
                {
                    double perTrade = Math.Pow(Math.E, Math.Log((account.Equity / account.InitialEquity)) / account.GetNumberOfSales());
                    double difference = perTrade * 100 - 100;

                    avgChangeSellValue.Text = (perTrade >= 1 ? "+" : "") + Utilities.RoundToIncrement(difference, 0.01) + "%";
                }));
            }

            // initial equity
            initialEquityValue.Dispatcher.Invoke(new Action(() => { initialEquityValue.Text = "$" + Utilities.RoundToIncrement(account.InitialEquity, 0.05); }));

            // current equity
            currentEquityValue.Dispatcher.Invoke(new Action(() => { currentEquityValue.Text = "$" + Utilities.RoundToIncrement(account.Equity, 0.05); }));

            // buying power
            buyingPowerValue.Dispatcher.Invoke(new Action(() => { buyingPowerValue.Text = "$" + Utilities.RoundToIncrement(account.BuyingPower, 0.05); }));

            // positions
            positionsValue.Dispatcher.Invoke(new Action(() => { positionsValue.Text = account.Positions.Count.ToString(); }));

            // wins
            winsValue.Dispatcher.Invoke(new Action(() => { winsValue.Text = account.Wins.ToString(); }));

            // losses
            lossesValue.Dispatcher.Invoke(new Action(() => { lossesValue.Text = account.Losses.ToString(); }));

            // plotting
            List<double> dates = new List<double>();
            List<double> equities = new List<double>();
            List<double> powers = new List<double>();

            for (int i = 0; i < account.EquityHistory.Count; i++)
            {
                dates.Add(account.EquityHistory[i].Item1.ToOADate());
                equities.Add(account.EquityHistory[i].Item2);
                powers.Add(account.BuyingPowerHistory[i].Item2);
            }

            plotUpper.Dispatcher.Invoke(new Action(() =>
            {
                // clear our previous graph
                plotUpper.plt.Clear();

                // plot
                plotUpper.plt.PlotScatter(dates.ToArray(), equities.ToArray(), label: "Equity");
                plotUpper.plt.PlotHLine(account.InitialEquity, System.Drawing.Color.White, lineStyle: LineStyle.Dot);
                plotUpper.plt.Grid(lineStyle: LineStyle.Dot);

                // render
                plotUpper.Render();
            }));

            plotLower.Dispatcher.Invoke(new Action(() =>
            {
                // clear our previous graph
                plotLower.plt.Clear();

                // plot
                plotLower.plt.PlotScatter(dates.ToArray(), powers.ToArray(), label: "Buying Power");
                plotLower.plt.PlotHLine(account.InitialEquity, System.Drawing.Color.White, lineStyle: LineStyle.Dot);
                plotLower.plt.Grid(lineStyle: LineStyle.Dot);

                // render
                plotLower.Render();
            }));

        }

        /// <summary>
        /// Start the backtest.
        /// </summary>
        private void SimulationWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Debug("Starting backtest.");

            Account acc = new Account(new ExampleStrategy(200, 50, 1000), Settings.START_DATE, Settings.END_DATE, Settings.INITIAL_EQUITY, false);

            // while the simulation is not done, continue
            while (acc.CurrentDate != acc.EndDate)
            {
                if (simulationWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                // simulate!
                acc.Simulate();

                // now that the day is completely finished, we can update the UI
                UpdateUI(acc);
            }

            Debug("Backtest complete.");
        }

        /// <summary>
        /// Start the data collection.
        /// </summary>
        private async void DataCollectionWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Debug("Starting data collection!");

            List<string> stocks = DataManager.GetStocksRaw();
            
            for (int i = 0; i < stocks.Count; i++)
            {
                if (dataCollectionWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                // debug progress
                float percent = ((float)(i + 1) / stocks.Count) * 100;
                Debug("Collecting data for stock " + stocks[i] + "... (" + percent.ToString().Substring(0, Math.Min(percent.ToString().Length, 4)) + "%) (1/3)");

                // we try three times
                for (int j = 0; j < 3; j++)
                {
                    try
                    {
                        // download data
                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(new Uri($"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY_ADJUSTED&symbol={stocks[i]}&outputsize=full&apikey={Settings.DATA_KEY}&datatype=csv"), Settings.DATA_DIR + $"/{stocks[i]}.csv");
                        break;
                    }
                    catch
                    {
                        Debug("Error with stock " + stocks[i] + "...");
                        await Wait(Settings.ERROR_DELAY);
                    }
                }
            }

            // before we filter, are the parameters a problem?
            if (!DataManager.IsStockDay(Settings.MAX_START_DATE) || !DataManager.IsStockDay(Settings.MIN_END_DATE))
            {
                throw new Exception("The given frame is invalid");
            }

            Debug("Cleaning stocks...");

            // now that we have collected data for all the stocks, we
            // filter stocks that are too small or don't have data
            for (int i = 0; i < stocks.Count; i++)
            {
                float percent = ((float)(i + 1) / stocks.Count) * 100;
                Debug("Cleaning data for stock " + stocks[i] + "... (" + percent.ToString().Substring(0, Math.Min(percent.ToString().Length, 4)) + "%) (2/3)");

                bool delete = false;

                try
                {
                    List<BarData> bars = DataManager.GetBars(stocks[i]);

                    bool first = false;
                    bool second = false;

                    // in this case, we do have the data, but there is a chance
                    // the company is too small and does not exist in the given frame
                    for (int j = 0; j < bars.Count; j++)
                    {
                        if (bars[j].TimeStamp == Settings.MAX_START_DATE)
                        {
                            first = true;
                        }

                        if (bars[j].TimeStamp == Settings.MIN_END_DATE)
                        {
                            second = true;
                        }
                    }

                    if (!first || !second) { delete = true; }
                }
                catch
                {
                    delete = true;
                }

                if (delete)
                {
                    Debug("Deleting stock " + stocks[i] + "...");
                    File.Delete(Settings.DATA_DIR + $"/{stocks[i]}.csv");
                }
            }

            Debug("Data collection complete!");
        }

        /// <summary>
        /// Start any tests that are independent of a strategy.
        /// </summary>
        private void ExperimentalWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void StartStrategyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!simulationWorker.IsBusy && !dataCollectionWorker.IsBusy && !experimentalWorker.IsBusy)
            {
                simulationWorker.RunWorkerAsync();
            }
            else
            {
                Debug("Cannot start simulation.");
            }
        }

        private void CollectDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (!simulationWorker.IsBusy && !dataCollectionWorker.IsBusy && !experimentalWorker.IsBusy)
            {
                dataCollectionWorker.RunWorkerAsync();
            }
            else
            {
                Debug("Cannot start data collection.");
            }
        }

        private void ExperimentalButton_Click(object sender, RoutedEventArgs e)
        {
            if (!simulationWorker.IsBusy && !dataCollectionWorker.IsBusy && !experimentalWorker.IsBusy)
            {
                experimentalWorker.RunWorkerAsync();
            }
            else
            {
                Debug("Cannot start experiment.");
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Debug("Stopping all processes.");

            dataCollectionWorker.CancelAsync();
            simulationWorker.CancelAsync();
            experimentalWorker.CancelAsync();
        }
    }
}
