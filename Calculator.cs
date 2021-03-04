using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTester
{
    public static class Calculator
    {
        /// <summary>
        /// Gets the indices of all green candles in a set of bars.
        /// </summary>
        public static List<int> FindGreenCandles(List<BarData> bars)
        {
            List<int> indices = new List<int>();

            for (int i = 0; i < bars.Count; i++)
            {
                if (IsGreenCandle(bars[i]))
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        /// <summary>
        /// Gets the indices of all red candles in a set of bars.
        /// </summary>
        public static List<int> FindRedCandles(List<BarData> bars)
        {
            List<int> indices = new List<int>();

            for (int i = 0; i < bars.Count; i++)
            {
                if (IsRedCandle(bars[i]))
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        /// <summary>
        /// Checks if a given bar is a red candle.
        /// </summary>
        public static bool IsRedCandle(BarData bar) { return bar.Close <= bar.Open; }

        /// <summary>
        /// Checks if a given bar is a red candle.
        /// </summary>
        public static bool IsGreenCandle(BarData bar) { return bar.Close > bar.Open; }

        /// <summary>
        /// Get the average nose length for a set of bars.
        /// </summary>
        public static double GetAverageNoseLength(List<BarData> bars)
        {
            double pool = 0;
            int amount = 0;

            foreach (BarData bar in bars)
            {
                double length = GetNoseLength(bar);

                if (length >= 0)
                {
                    amount++;
                    pool += length;
                }
            }

            return amount > 0 ? pool / amount : 0;
        }

        /// <summary>
        /// Get the average tail length for a set of bars.
        /// </summary>
        public static double GetAverageTailLength(List<BarData> bars)
        {
            double pool = 0;
            int amount = 0;

            foreach (BarData bar in bars)
            {
                double length = GetTailLength(bar);

                if (length >= 0)
                {
                    amount++;
                    pool += length;
                }
            }

            return amount > 0 ? pool / amount : 0;
        }

        /// <summary>
        /// Get the tail length for a bar.
        /// </summary>
        public static double GetTailLength(BarData bar) { return Math.Min(bar.Close, bar.Open) - bar.Low; }

        /// <summary>
        /// Get the nose length for a bar.
        /// </summary>
        public static double GetNoseLength(BarData bar) { return bar.High - Math.Max(bar.Close, bar.Open); }

        /// <summary>
        /// Get the weighted percent change over a given set of bars.
        /// </summary>
        public static double GetWeightedPercentChange(List<BarData> bars)
        {
            double pool = 0;

            for (int i = 0; i < bars.Count - 1; i++)
            {
                pool += (bars[i + 1].Open + bars[i + 1].Close) / (bars[i].Open + bars[i].Close);
            }

            return pool / (bars.Count - 1);
        }

        /// <summary>
        /// Check for a hammer pattern.
        /// </summary>
        public static bool CheckHammer(List<BarData> bars)
        {
            // what percent decrease would be considered as a downtrend?
            double downTrendPercent = 0.987;

            // get previous bars
            List<BarData> prevBars = new List<BarData>();

            for (int i = 0; i < Settings.TREND_BAR_COUNT; i++)
            {
                prevBars.Add(bars[bars.Count - Settings.TREND_BAR_COUNT + i - 2]);
            }

            // get last bar
            BarData lastBar = bars[bars.Count - 1];

            // the bar can be either color, but the most profitable form
            // of the hammer trend would include a green candle
            if (IsRedCandle(lastBar)) { return false; }

            // what is the trend?
            double factor = GetWeightedPercentChange(prevBars);

            // are we in a downtrend?
            if (factor <= downTrendPercent)
            {
                // what percent less than the average open close difference is considered short?
                double shortPercent = 0.5;

                // what is the average open close different for the previous bars?
                double averageDiff = prevBars.Average(x => Math.Abs(x.Open - x.Close));

                // if the average difference is so slim that it is equal to zero,
                // then there is no possible chance of us being smaller than it
                if (averageDiff == 0) { return false; }

                // are we shorter than this?
                if (Math.Abs(lastBar.Open - lastBar.Close) / averageDiff <= shortPercent)
                {
                    // what percent longer than the average tail is considered long?
                    double tailLongPercent = 2.2;

                    // what is the average tail length?
                    double averageTail = GetAverageTailLength(prevBars);

                    // what is our tail length?
                    double tailLength = GetTailLength(lastBar);

                    // in the case that the average tail is so slim that it is equal to zero,
                    // then we assume we are long enough
                    bool canContinue = false;
                    if (averageTail == 0)
                    {
                        canContinue = true;
                    }
                    else
                    {
                        if (tailLength / averageTail >= tailLongPercent) { canContinue = true; }
                    }

                    if (canContinue)
                    {
                        // what percent shorter than our tail is considered short?
                        double noseShortPercent = 0.4;

                        // what is our nose length?
                        double noseLength = GetNoseLength(lastBar);

                        // are we sufficiently shorter than the average?
                        if (noseLength / tailLength <= noseShortPercent)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check for an inverse hammer pattern.
        /// </summary>
        public static bool CheckInverseHammer(List<BarData> bars)
        {
            // what percent decrease would be considered as a downtrend?
            double downTrendPercent = 0.987;

            // get previous bars
            List<BarData> prevBars = new List<BarData>();
            for (int i = 0; i < Settings.TREND_BAR_COUNT; i++)
            {
                prevBars.Add(bars[bars.Count - Settings.TREND_BAR_COUNT + i - 2]);
            }

            // get last bar
            BarData lastBar = bars[bars.Count - 1];

            // the bar can be either color, but the most profitable form
            // of the hammer trend would include a green candle
            if (IsRedCandle(lastBar)) { return false; }

            // what is the trend?
            double factor = GetWeightedPercentChange(prevBars);

            // are we in a downtrend?
            if (factor <= downTrendPercent)
            {
                // what percent less than the average open close difference is considered short?
                double shortPercent = 0.5;

                // what is the average open close different for the previous bars?
                double averageDiff = prevBars.Average(x => Math.Abs(x.Open - x.Close));

                // if the average difference is so slim that it is equal to zero,
                // then there is no possible chance of us being smaller than it
                if (averageDiff == 0) { return false; }

                // are we shorter than this?
                if (Math.Abs(lastBar.Open - lastBar.Close) / averageDiff <= shortPercent)
                {
                    // what percent longer than the average nose is considered long?
                    double noseLongPercent = 2.2;

                    // what is the average tail length?
                    double averageNose = GetAverageNoseLength(prevBars);

                    // what is our tail length?
                    double noseLength = GetNoseLength(lastBar);

                    // in the case that the average tail is so slim that it is equal to zero,
                    // then we assume we are long enough
                    bool canContinue = false;
                    if (averageNose == 0)
                    {
                        canContinue = true;
                    }
                    else
                    {
                        if (noseLength / averageNose >= noseLongPercent) { canContinue = true; }
                    }

                    if (canContinue)
                    {
                        // what percent shorter than our tail is considered short?
                        double tailShortPercent = 0.4;

                        // what is our nose length?
                        double tailLength = GetTailLength(lastBar);

                        // are we sufficiently shorter than the average?
                        if (tailLength / noseLength <= tailShortPercent)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check for a bullish engulfing pattern.
        /// </summary>
        public static bool CheckBullishEngulfing(List<BarData> bars)
        {
            // what percent decrease would be considered as a downtrend?
            double downTrendPercent = 0.987;

            // get previous bars
            List<BarData> prevBars = new List<BarData>();
            for (int i = 0; i < Settings.TREND_BAR_COUNT; i++)
            {
                prevBars.Add(bars[bars.Count - Settings.TREND_BAR_COUNT + i - 2]);
            }

            // get last bars
            BarData secondLastBar = bars[bars.Count - 2];
            BarData lastBar = bars[bars.Count - 1];

            // the pattern is marked by a red candle followed by a green candle
            if (IsGreenCandle(secondLastBar)) { return false; }
            if (IsRedCandle(lastBar)) { return false; }

            // what is the trend?
            double factor = GetWeightedPercentChange(prevBars);

            // are we in a downtrend?
            if (factor <= downTrendPercent)
            {
                // what percent less than the average open close difference is considered short?
                double shortPercent = 0.6;

                // what is the average open close different for the previous bars?
                double averageDiff = prevBars.Average(x => Math.Abs(x.Open - x.Close));

                // if the average difference is so slim that it is equal to zero,
                // then there is no possible chance of us being smaller than it
                if (averageDiff == 0) { return false; }

                // is our second to last bar shorter than this?
                if (Math.Abs(secondLastBar.Open - secondLastBar.Close) / averageDiff <= shortPercent)
                {
                    // what percent longer than the short open close difference is considered long?
                    double longPercent = 1.4;

                    // what is the open close difference of the second to last bar?
                    double secondLastDiff = Math.Abs(secondLastBar.Open - secondLastBar.Close);

                    // are we sufficiently longer than this?
                    if (Math.Abs(lastBar.Open - lastBar.Close) / secondLastDiff >= longPercent)
                    {
                        // is the last open below the second to last close?
                        if (secondLastBar.Close - lastBar.Open < 0) { return false; }

                        // is the last bar close above second last bar open?
                        if (lastBar.Close - secondLastBar.Open <= 0) { return false; }

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check for a piercing line pattern.
        /// </summary>
        public static bool CheckPiercingLine(List<BarData> bars)
        {
            // what percent decrease would be considered as a downtrend?
            double downTrendPercent = 0.987;

            // get previous bars
            List<BarData> prevBars = new List<BarData>();
            for (int i = 0; i < Settings.TREND_BAR_COUNT; i++)
            {
                prevBars.Add(bars[bars.Count - Settings.TREND_BAR_COUNT + i - 2]);
            }

            // get last bars
            BarData secondLastBar = bars[bars.Count - 2];
            BarData lastBar = bars[bars.Count - 1];

            // the pattern is marked by a red candle followed by a green candle
            if (IsGreenCandle(secondLastBar)) { return false; }
            if (IsRedCandle(lastBar)) { return false; }

            // what is the trend?
            double factor = GetWeightedPercentChange(prevBars);

            // are we in a downtrend?
            if (factor <= downTrendPercent)
            {
                // is the last close greater than second to last midpoint?
                if (lastBar.Close <= ((secondLastBar.Open + secondLastBar.Close) / 2)) { return false; }

                // is the last open less than second last close?
                if (lastBar.Open >= secondLastBar.Close) { return false; }

                // is the last close less than the previous open?
                if (lastBar.Close >= secondLastBar.Open) { return false; }

                // what is the max size difference between the bars?
                double maxPercent = 1.5;

                // what is the size difference?
                double diff = Math.Abs(secondLastBar.Open - secondLastBar.Close) / Math.Abs(secondLastBar.Open - secondLastBar.Close);

                if (diff < maxPercent && diff > Math.Pow(maxPercent, -1))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check for a morning star pattern.
        /// </summary>
        public static bool CheckMorningStar(List<BarData> bars)
        {
            // what percent decrease would be considered as a downtrend?
            double downTrendPercent = 0.987;

            // get previous bars
            List<BarData> prevBars = new List<BarData>();
            for (int i = 0; i < Settings.TREND_BAR_COUNT; i++)
            {
                prevBars.Add(bars[bars.Count - Settings.TREND_BAR_COUNT + i - 3]);
            }

            // get last bars
            BarData thirdLastBar = bars[bars.Count - 3];
            BarData secondLastBar = bars[bars.Count - 2];
            BarData lastBar = bars[bars.Count - 1];

            // the pattern is marked by a red candle following either color and a green candle
            if (IsGreenCandle(thirdLastBar)) { return false; }
            if (IsRedCandle(lastBar)) { return false; }

            // what is the trend?
            double factor = GetWeightedPercentChange(prevBars);

            // are we in a downtrend?
            if (factor <= downTrendPercent)
            {
                // how short must the middle bar be?
                double middleShortPercent = 0.4;

                // what is the average bar size of the left and right candles?
                double averageTwinBody = (Math.Abs(thirdLastBar.Open - thirdLastBar.Close) + Math.Abs(lastBar.Open - lastBar.Close)) / 2;

                // do we fit the criteria?
                if (Math.Abs(secondLastBar.Open - secondLastBar.Close) / averageTwinBody <= middleShortPercent)
                {
                    // what size relative to the previous bars would be considered long?
                    double longPercent = 1.5;

                    // what is the average bar length?
                    double averageLength = prevBars.Average(x => Math.Abs(x.Open - x.Close));

                    // what are the bar lengths for the twins?
                    double leftLength = Math.Abs(thirdLastBar.Open - thirdLastBar.Close);
                    double rightLength = Math.Abs(lastBar.Open - lastBar.Close);

                    // do we fit the criteria?
                    if (leftLength / averageLength >= longPercent &&
                        rightLength / averageLength >= longPercent)
                    {
                        // what is the average bottom of the twins?
                        double leftBot = thirdLastBar.Close;
                        double rightBot = lastBar.Open;
                        double averageBot = (rightBot + leftBot) / 2;

                        // is the middle bar somewhat below them?
                        if (Math.Min(secondLastBar.Close, secondLastBar.Open) <= averageBot)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check for the three white soldiers pattern.
        /// </summary>
        public static bool CheckThreeWhiteSoldiers(List<BarData> bars)
        {
            // what percent decrease would be considered as a downtrend?
            double downTrendPercent = 0.987;

            // get previous bars
            List<BarData> prevBars = new List<BarData>();
            for (int i = 0; i < Settings.TREND_BAR_COUNT; i++)
            {
                prevBars.Add(bars[bars.Count - Settings.TREND_BAR_COUNT + i - 4]);
            }

            // get last bars
            BarData thirdLastBar = bars[bars.Count - 3];
            BarData secondLastBar = bars[bars.Count - 2];
            BarData lastBar = bars[bars.Count - 1];

            // are all three green?
            if (IsRedCandle(thirdLastBar) ||
                IsRedCandle(secondLastBar) ||
                IsRedCandle(lastBar))
            {
                return false;
            }

            // what is the trend?
            double factor = GetWeightedPercentChange(prevBars);

            // are we in a downtrend?
            if (factor <= downTrendPercent)
            {
                double thirdLastDiff = thirdLastBar.Close - thirdLastBar.Open;
                double secondLastDiff = thirdLastBar.Close - thirdLastBar.Open;
                double lastDiff = lastBar.Close - thirdLastBar.Open;

                // are we concave up?
                if (lastDiff > secondLastDiff && secondLastDiff > lastDiff)
                {
                    // what percent of the bar length would be considered a small nose?
                    double smallPercent = 0.2;

                    // what is the average bar length of our soldiers?
                    double averageLength = (thirdLastDiff + secondLastDiff + lastDiff) / 3;

                    // what is our average nose length?
                    double averageNose = (GetNoseLength(thirdLastBar) + GetNoseLength(secondLastBar) + GetNoseLength(lastBar)) / 3;

                    // do we fit the criteria?
                    if (averageNose / averageLength <= smallPercent)
                    {
                        return true;
                    }
                }

            }

            return false;
        }

        /// <summary>
        /// Calculate the moving average.
        /// </summary>
        public static double MovingAverage(List<double> input, int samples)
        {
            double sum = 0;

            for (int i = 0; i < samples; i++)
            {
                sum += input[input.Count - i - 1];
            }

            return sum / samples;
        }

        /// <summary>
        /// Calculate the volume weighted moving average.
        /// </summary>
        public static double VolumeWeightedMovingAverage(List<BarData> input, int samples)
        {
            double sumTop = 0;
            double sumBot = 0;

            for (int i = 0; i < samples; i++)
            {
                sumTop += input[input.Count - i - 1].Close * input[input.Count - i - 1].Volume;
                sumBot += input[input.Count - i - 1].Volume;
            }

            return sumTop / sumBot;
        }

        /// <summary>
        /// Calculate the standard deviation.
        /// </summary>
        public static double StandardDeviation(List<double> input, int samples)
        {
            List<double> values = input.Skip(Math.Max(0, input.Count - samples)).ToList();

            double standardDeviation = 0;

            double avg = values.Average();
            double sum = values.Sum(x => Math.Pow(x - avg, 2));
            standardDeviation = Math.Sqrt((sum) / (values.Count() - 1));
            
            return standardDeviation;
        }

        /// <summary>
        /// Calculate the exponential moving average.
        /// </summary>
        public static double RelativeStrengthIndex(List<double> input, int samples)
        {
            List<double> gains = new List<double>();
            List<double> losses = new List<double>();

            for (int i = input.Count - samples; i < input.Count; i++)
            {
                double change = input[i] - input[i - 1];

                gains.Add(change >= 0 ? change : 0);
                losses.Add(change < 0 ? -1 * change : 0);
            }

            double rs = RollingMovingAverage(gains, samples) / RollingMovingAverage(losses, samples);
            return 100 - 100 / (1 + rs);
        }

        /// <summary>
        /// Calculate the exponential moving average.
        /// </summary>
        public static double ExponentialMovingAverage(List<double> input, int samples)
        {
            Console.WriteLine(input.Count);
            Console.WriteLine(input.Count - samples);
            double sum = MovingAverage(input, samples);
            double alpha = 2 / (samples + 1);

            for (int i = input.Count - samples; i < input.Count; i++)
            {
                sum = alpha * input[i] + (1 - alpha) * sum; 
            }

            return sum;
        }

        /// <summary>
        /// Calculate the double exponential moving average.
        /// </summary>
        public static double DoubleExponentialMovingAverage(List<double> input, int samples)
        {
            List<double> emas = new List<double>();

            for (int i = 0; i < input.Count; i++)
            {
                if (i >= samples * 2)
                {
                    emas.Add(ExponentialMovingAverage(input.GetRange(0, i + 1), samples));
                }
            }

            return 2 * ExponentialMovingAverage(input, samples) - ExponentialMovingAverage(emas, samples);
        }

        /// <summary>
        /// Calculate the triple exponential moving average.
        /// </summary>
        public static double TripleExponentialMovingAverage(List<double> input, int samples)
        {
            List<double> emas = new List<double>();
            List<double> emas2 = new List<double>();

            for (int i = 0; i < input.Count; i++)
            {
                if (i >= samples * 2)
                {
                    emas.Add(ExponentialMovingAverage(input.GetRange(0, i + 1), samples));
                }
            }

            for (int i = 0; i < emas.Count; i++)
            {
                if (i >= samples * 2)
                {
                    emas2.Add(ExponentialMovingAverage(emas.GetRange(0, i + 1), samples));
                }
            }

            return 3 * ExponentialMovingAverage(input, samples) - 3 * ExponentialMovingAverage(emas, samples) + ExponentialMovingAverage(emas2, samples);
        }

        /// <summary>
        /// Calculate the rolling moving average.
        /// </summary>
        public static double RollingMovingAverage(List<double> input, int samples)
        {
            double sum = MovingAverage(input, samples);
            double alpha = 1 / samples;

            for (int i = input.Count - samples; i < input.Count; i++)
            {
                sum = alpha * input[i] + (1 - alpha) * sum;
            }

            return sum;
        }

        /// <summary>
        /// Calculate the bollinger bands.
        /// </summary>
        public static Tuple<double, double> BollingerBands(List<BarData> input, int samples, double stDevs)
        {
            double stDev = StandardDeviation(input.Select(x => x.Close).ToList(), samples);
            double sma = MovingAverage(input.Select(x => x.Close).ToList(), samples);

            return new Tuple<double, double>(sma + stDevs * stDev, sma - stDevs * stDev);
        }
    }
}
