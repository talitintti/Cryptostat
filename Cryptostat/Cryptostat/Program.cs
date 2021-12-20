using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cryptostat
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //Console.WriteLine(await GetLongestDowntrend((2018, 1, 1), (2021, 9, 12)));
            await GetHighestTradingVolume((2005, 4,13),(2021,12,13));
        }

        public static async Task<(int year, int month, int day, int volume)> GetHighestTradingVolume((int year, int month, int day) fromDate, 
            (int year, int month, int day) toDate) //TODO: get rid of the async? you might not need it
        {
            var coinID = "bitcoin";
            var currencyID = "eur";
            string urlBase = "https://api.coingecko.com/api/v3/"; //TODO: these *might* be better off as attributes

            Int64 unixFromDate = DateToUnixTimestamp(fromDate.year, fromDate.month, fromDate.day);
            Int64 unixToDate = DateToUnixTimestamp(toDate.year, fromDate.month, fromDate.day);
            
            string requestRangeUrl = BuildRangeRequestUrl(urlBase, coinID, currencyID, unixFromDate, unixToDate);
            string serverMessage = await HttpRequestAsync(requestRangeUrl);
            if (serverMessage.Equals(""))
            {
                return (0,0,0,0);
            }

            double[][] coinVolumeAndDate = DeserializeDateAndPrice(serverMessage, 1);
            (double date, double volume) = HighestVolume(coinVolumeAndDate);
            
            return (1,1,1,1); //TODO:obv fix this
        }

        /// <summary>
        /// Gets the longest downtrend from a specified time range. If can't fulfill request -> return 0
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns>Longest downtrend in days</returns>
        public static async Task<int> GetLongestDowntrend((int year, int month, int day) fromDate,
            (int year, int month, int day) toDate)
        {
            var coinID = "bitcoin";
            var currencyID = "eur";
            string urlBase = "https://api.coingecko.com/api/v3/";

            Int64 unixFromDate = DateToUnixTimestamp(fromDate.year, fromDate.month, fromDate.day); //does this check for validity sas well??
            Int64 unixToDate = DateToUnixTimestamp(toDate.year, fromDate.month, fromDate.day);
            
            string requestRangeUrl = BuildRangeRequestUrl(urlBase, coinID, currencyID, unixFromDate, unixToDate);
            string serverMessage = await HttpRequestAsync(requestRangeUrl);
            if (serverMessage.Equals(""))
            {
                return (0);
            }

            double[][] coinPriceAndDate = DeserializeDateAndPrice(serverMessage,0);
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
                //Console.Error.WriteLine(httpEx.StackTrace);
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
        private static string BuildRangeRequestUrl(string baseUrl,string coinID, string currency, Int64 fromDate, Int64 toDate)
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
        public static Int64 DateToUnixTimestamp(int year,int month, int day)
        {
            Int64 timestamp = 0;
            int[] daysPerMonth = new[] {0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
            if (year >= 1 && month <= 12 && month >= 1 && day >= 1 && day <= daysPerMonth[month])
            {
                var tdo = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
                timestamp = tdo.ToUnixTimeSeconds();
            }
            return timestamp;
        }

        /// <summary>
        /// Deserialize the given json into 2-dim array.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="chooseData">This decides if data fetched is date+price (0) or date+volume (1)</param>
        /// <returns>Array with requested data</returns>
        public static double[][] DeserializeDateAndPrice(string json, int chooseData)
        {
           double[][] array;
           switch (chooseData)
           {
              default:  
                array = JsonConvert.DeserializeObject<CoinPrice>(json).prices; //TODO: does this need any sec checks;
                break;
              case 1:
                array = JsonConvert.DeserializeObject<CoinVolume>(json).total_volumes; //TODO: does this need any sec checks;
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
            if (numberOfDays < 1) return (0,0);
            int indexOfHighest = 0;
            
            for ( int i = 1; i < numberOfDays; i++)
            {
                if (volumeAndDate[i][1] > volumeAndDate[indexOfHighest][1])
                    indexOfHighest = i;
            }

            return (volumeAndDate[indexOfHighest][0], volumeAndDate[indexOfHighest][1]);
        }

            public static DateTime ConvertUnixTimeStampToDateTime(double unixtime) 
            {
                DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0).ToLocalTime();
                return sTime.AddSeconds(unixtime); //maybe change to int int int 
            }
            
        public static int LongestDownStreak(double[][] volumeAndDate) //TODO: TEST FOR EDGE CASES!!!!
        {
            int streak = 0;
            int currentStreak = 0;
            int numberOfDays = volumeAndDate.GetLength(0);
            
            if (numberOfDays < 2) return 1;
            
            for ( int i = 1; i < numberOfDays; i++)
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