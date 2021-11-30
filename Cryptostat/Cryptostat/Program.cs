using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cryptostat
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string a = await HttpRequestAsync("https://www.google.com");
            if (a.Equals("")) Console.WriteLine("not working");
            Console.WriteLine(a);
        }
        
        /// <summary>
        /// Make a http request with provided url and return a Task<string>
        /// that contains the response. In case of bad url, timeout or no
        /// connection we return an empty string.
        /// </summary>
        /// <param name="url">Request for the server</param>
        /// <returns>Server's message or empty string</returns>
        public static async Task<string> HttpRequestAsync(string url)
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
    }
    
}