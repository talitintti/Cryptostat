using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cryptostat
{
    class Program
    {
        static async Task Main(string[] args)
        {
        }
        
        
        
        //public static string GetLongestDowntrend() 
        
        private static async Task<(long, long, long)> GetDataGivenRange(string coin, string currency, string fromDate, string toDate) {
            string urlBase = "https://api.coingecko.com/api/v3/";
            //Check for inputted coin and currency validity here
            Int64 unixFromDate = DateToUnixTimestamp(fromDate); //does this check for validity sas well??
            Int64 unixToDate = DateToUnixTimestamp(toDate);
            string requestRangeUrl = BuildRangeRequestUrl(urlBase, coin, currency, unixFromDate, unixToDate);
            string a = await HttpRequestAsync(requestRangeUrl);
            if (a.Equals(""))
            {
                return (0,0,0);
                //Console.WriteLine("invalid url")
            }
            Console.WriteLine(a);
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
            string builtUrl = baseUrl;
            string rangeReqUrl = "coins/" + coinID + "market_chart range?vs_currency=" +
                                 currency + "&from=" + fromDate + "&to=" + toDate; 
            return builtUrl;
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
    }
    
}