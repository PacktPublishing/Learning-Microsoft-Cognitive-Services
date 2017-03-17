using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Diagnostics;

namespace Chapter1.Model
{
    /// <summary>
    /// Class to generate authentication tokens to use for Bing Speech API
    /// </summary>
    public class Authentication : IDisposable
    {
        private string _requestDetails;
        private AccessTokenInfo _token;
        private Timer _tokenRenewer;

        private const int TokenRefreshInterval = 9;

        public AccessTokenInfo Token { get { return _token; } }

        /// <summary>
        /// Constructor - Sets the request details, gets a token and starts a timer for token renewal
        /// </summary>
        /// <param name="clientId">Id of the current client, should be unique for your application</param>
        /// <param name="clientSecret">Bing Speech API key</param>
        public Authentication(string clientId, string clientSecret)
        {
            _requestDetails = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope={2}",
                                          HttpUtility.UrlEncode(clientId),
                                          HttpUtility.UrlEncode(clientSecret),
                                          HttpUtility.UrlEncode("https://speech.platform.bing.com"));

            _token = GetToken();
            
            _tokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback),
                                           this,
                                           TimeSpan.FromMinutes(TokenRefreshInterval),
                                           TimeSpan.FromMilliseconds(-1));
        }
        
        /// <summary>
        /// Function that executes when the timers refresh interval is reached. Will generate a new token
        /// </summary>
        /// <param name="stateInfo"></param>
        private void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                AccessTokenInfo newAccessToken = GetToken();
                _token = newAccessToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
            }
            finally
            {
                _tokenRenewer.Change(TimeSpan.FromMinutes(TokenRefreshInterval), TimeSpan.FromMilliseconds(-1));
            }
        }

        /// <summary>
        /// Function to generate an application token
        /// </summary>
        /// <returns>Returns the <see cref="AccessTokenInfo"/> token if call is successful, null otherwise </returns>
        private AccessTokenInfo GetToken()
        {
            WebRequest webRequest = WebRequest.Create("https://oxford-speech.cloudapp.net/token/issueToken");
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";

            byte[] bytes = Encoding.ASCII.GetBytes(_requestDetails);
            webRequest.ContentLength = bytes.Length;
                       
            try
            {
                using (Stream outputStream = webRequest.GetRequestStream())
                {
                    outputStream.Write(bytes, 0, bytes.Length);
                }

                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AccessTokenInfo));
                    AccessTokenInfo token = (AccessTokenInfo)serializer.ReadObject(webResponse.GetResponseStream());
                    return token;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }

        }

        public void Dispose()
        {            
            _tokenRenewer.Dispose();
        }
    }

    /// <summary>
    /// Class containing deserialized token information
    /// </summary>
    [DataContract]
    public class AccessTokenInfo
    {
        [DataMember]
        public string access_token { get; set; }
        [DataMember]
        public string token_type { get; set; }
        [DataMember]
        public string expires_in { get; set; }
        [DataMember]
        public string scope { get; set; }
    }
}
