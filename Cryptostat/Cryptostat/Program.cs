using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cryptostat
{
    class Cryptostat
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine(await GetLongestDowntrend((2018, 1, 1), (2021, 9, 12)));
            Console.WriteLine(await GetLongestDowntrend((2021, 2, 1), (2018, 9, 12)));
            Console.WriteLine(await GetHighestTradingVolume((2005, 4,13),(2021,12,13)));
            Console.WriteLine(await GetHighestTradingVolume((2021,2,1),(2019,12,13)));
        }

        /// <summary>
        /// Returns the highest date in terms of volume in given range. Returns also the trading volume of that day.
        /// If could not fulfill request or data does not exist returns (0,0,0,0)
        /// </summary>
        /// <param name="fromDate">Start of the time range</param>
        /// <param name="toDate">End of the time range</param>
        /// <returns>(year,month,day,volume)</returns>
        public static async Task<(uint year, uint month, uint day, double volume)> GetHighestTradingVolume(
            (uint year, uint month, uint day) fromDate,
            (uint year, uint month, uint day) toDate)
        {
            const string coinID = "bitcoin";
            const string currencyID = "eur";
            const string
                urlBase = "https://api.coingecko.com/api/v3/"; //TODO: these *might* be better off as attributes

            long unixFromDate = DateToUnixTimestamp(fromDate.year, fromDate.month, fromDate.day);
            long unixToDate = DateToUnixTimestamp(toDate.year, fromDate.month, fromDate.day);

            string requestRangeUrl = BuildRangeRequestUrl(urlBase, coinID, currencyID, unixFromDate, unixToDate);
            string serverMessageJson = await HttpRequestAsync(requestRangeUrl);
            if (serverMessageJson.Equals(""))
            {
                return (0, 0, 0, 0);
            }

            double[][] coinVolumeAndDate = DeserializeHistoricData(serverMessageJson, 1);
            (double unixDate, double volume) = HighestVolume(coinVolumeAndDate);
            (uint highestVolYear, uint highestVolMonth, uint highestVolDay) = UnixTimestampToDate(unixDate);
            return (highestVolYear, highestVolMonth, highestVolDay, volume);
        }

        /// <summary>
        /// Gets the longest downtrend in days from a specified time range. If can't fulfill request -> return -1
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns>Longest downtrend in days</returns>
        public static async Task<int> GetLongestDowntrend((uint year, uint month, uint day) fromDate,
            (uint year, uint month, uint day) toDate)
        {
            const string coinID = "bitcoin";
            const string currencyID = "eur";
            const string urlBase = "https://api.coingecko.com/api/v3/";

            long unixFromDate = DateToUnixTimestamp(fromDate.year, fromDate.month, fromDate.day);
            long unixToDate = DateToUnixTimestamp(toDate.year, fromDate.month, fromDate.day);

            string requestRangeUrl = BuildRangeRequestUrl(urlBase, coinID, currencyID, unixFromDate, unixToDate);
            string serverMessage = await HttpRequestAsync(requestRangeUrl);
            if (serverMessage.Equals(""))
            {
                return -1;
            }

            double[][] coinPriceAndDate = DeserializeHistoricData(serverMessage, 0);
            int longestDowntrend = LongestDownStreak(coinPriceAndDate);
            return longestDowntrend;
        }


        /// <summary>
        /// Make a http request with provided url and return a Task<string>
        /// that contains the response. In case of bad url, timeout or no
        /// connection we return an empty string.
        /// </summary>
        /// <param name="url">Request for the server</param>
        /// <returns>Server's message or empty string</returns>
        private static async Task<string> HttpRequestAsync(string url)
        {
            string response = "";
            try
            {
                using var httpClient = new HttpClient();
                response = await httpClient.GetStringAsync(url); //TODO: test with invalid url
            }
            catch (HttpRequestException httpEx)
            {
                Console.Error.WriteLine(httpEx.Message);
                Console.Error.WriteLine(httpEx.StackTrace);
            }

            return response;
        }


        /// <summary>
        /// TODO: kuvaus
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="coinID"></param>
        /// <param name="currency"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns></returns>
        private static string BuildRangeRequestUrl(string baseUrl, string coinID, string currency, long fromDate,
            long toDate)
        {
            string rangeReqUrl = "coins/" + coinID + "/market_chart/range?vs_currency=" +
                                 currency + "&from=" + fromDate + "&to=" + toDate;
            return baseUrl + rangeReqUrl;
        }


        /// <summary>
        /// Returns unix timestamp equivalent of given year/month/day -date.
        /// If date is not valid -> returns timestamp of zero
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <returns>unix timestamp in seconds</returns>
        private static long DateToUnixTimestamp(uint year, uint month, uint day)
        {
            long timestamp = 0;
            uint[] daysPerMonth = {0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
            if (year >= 1 && month <= 12 && month >= 1 && day >= 1 && day <= daysPerMonth[month])
            {
                var tdo = new DateTimeOffset((int) year, (int) month, (int) day, 0, 0, 0, TimeSpan.Zero);
                timestamp = tdo.ToUnixTimeSeconds();
            }

            return timestamp;
        }

        /// <summary>
        /// Deserialize the given json into 2-dim array.
        /// The resulting array structure looks something like {{unixtimestamp,priceDuringThatTime},{unixtimestamp+1day,...},...}
        /// </summary>
        /// <param name="json"></param>
        /// <param name="chooseData">0->{date,price} 1->{date,volume}</param>
        /// <returns>Array with requested data</returns>
        private static double[][] DeserializeHistoricData(string json, int chooseData)
        {
            double[][] array;
            switch (chooseData)
            {
                default:
                    array = JsonConvert.DeserializeObject<CoinPrice>(json)
                        .prices; //TODO: does this need any sec checks;
                    break;
                case 1:
                    array = JsonConvert.DeserializeObject<CoinVolume>(json)
                        .total_volumes; //TODO: does this need any sec checks;
                    break;
            }

            return array;
        }

        /// <summary>
        /// Returns the values of i and x where x is the highest value of the array {{i,x},...}.
        /// Array has to be of dimensions [][]
        /// If there are multiple biggest values of x the first one in order from 0...arraySize is returned.
        /// (0,0) is returned if cant find the biggest.
        /// </summary>
        /// <param name="volumeAndDate"></param>
        /// <returns>Biggest x and corresponding i</returns>
        private static (double date, double volume) HighestVolume(double[][] volumeAndDate)
        {
            int numberOfDays = volumeAndDate.GetLength(0);
            if (numberOfDays < 1) return (0, 0);
            int indexOfHighest = 0;

            for (int i = 1; i < numberOfDays; i++)
            {
                if (volumeAndDate[i][1] > volumeAndDate[indexOfHighest][1])
                    indexOfHighest = i;
            }

            return (volumeAndDate[indexOfHighest][0], volumeAndDate[indexOfHighest][1]);
        }

        public static (uint year, uint month, uint day) UnixTimestampToDate(double unixTime)
        {
            DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0,0, DateTimeKind.Local);
            sTime = sTime.AddMilliseconds(unixTime);
            return ((uint) sTime.Year, (uint) sTime.Month, (uint) sTime.Day);
        }

        private static int LongestDownStreak(double[][] volumeAndDate) //TODO: TEST FOR EDGE CASES!!!!
        {
            int streak = 0;
            int currentStreak = 0;
            int numberOfDays = volumeAndDate.GetLength(0);

            if (numberOfDays < 2) return 1;

            for (int i = 1; i < numberOfDays; i++)
            {
                if (volumeAndDate[i][1] < volumeAndDate[i - 1][1]) currentStreak++;
                else if (currentStreak > streak)
                {
                    streak = currentStreak;
                    currentStreak = 0;
                }
            }

            return streak;
        }
    }

    public class CoinPrice
    {
        public double[][] prices { get; set; }
    }

    public class CoinVolume
    {
        public double[][] total_volumes { get; set; }
    }
}