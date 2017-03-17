using End_to_End.Contracts;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace End_to_End.Model
{
    public class BingSearch
    {
        private BingWebRequest _webRequest;

        public BingSearch()
        {
            _webRequest = new BingWebRequest("API_KEY_HERE");
        }

        public async Task<BingNewsResponse> SearchNewsCategory(string query)
        {
            string endpoint = string.Format("{0}{1}&mkt=en-US", "https://api.cognitive.microsoft.com/bing/v5.0/news?category=", query);

            try
            {
                BingNewsResponse response = await _webRequest.MakeRequest<BingNewsResponse>(endpoint);

                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return null;
        }

        public async Task<BingNewsResponse> SearchNews(string query, SafeSearch safeSearch)
        {
            string endpoint = string.Format("{0}{1}&safeSearch={2}&count=5&mkt=en-US", "https://api.cognitive.microsoft.com/bing/v5.0/news/search?q=", query, safeSearch.ToString());

            try
            {
                BingNewsResponse response = await _webRequest.MakeRequest<BingNewsResponse>(endpoint);

                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return null;
        }

        public async Task<WebSearchResponse> SearchWeb(string query, SafeSearch safeSearch)
        {
            string endpoint = string.Format("{0}{1}&safeSearch={2}&count=5&mkt=en-US", "https://api.cognitive.microsoft.com/bing/v5.0/search?q=", query, safeSearch.ToString());

            try
            {
                WebSearchResponse response = await _webRequest.MakeRequest<WebSearchResponse>(endpoint);

                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return null;
        }
    }
}