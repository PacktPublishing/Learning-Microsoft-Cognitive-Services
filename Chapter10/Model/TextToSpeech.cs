using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace End_to_End.Model
{
    public class TextToSpeech
    {
        public event EventHandler<AudioEventArgs> OnAudioAvailable;
        public event EventHandler<AudioErrorEventArgs> OnError;
        
        private string _gender;
        private string _voiceName;
        private string _outputFormat;
        private string _authorizationToken;
        private AccessTokenInfo _token; 

        private List<KeyValuePair<string, string>> _headers = new List<KeyValuePair<string, string>>();

        private const string RequestUri = "https://speech.platform.bing.com/synthesize";
        private const string SsmlTemplate = "<speak version='1.0' xml:lang='en-US'><voice xml:lang='en-US' xml:gender='{0}' name='{1}'>{2}</voice></speak>";

        public TextToSpeech()
        {
            _gender = "Female";
            _outputFormat = "riff-16khz-16bit-mono-pcm";
            _voiceName = "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)";
        }

        public bool GenerateAuthenticationToken(string clientId, string clientSecret)
        {
            Authentication auth = new Authentication(clientId, clientSecret);

            try
            {
                _token = auth.Token;

                if (_token != null)
                {
                    _authorizationToken = $"Bearer {_token.access_token}";

                    return true;
                }
                else
                {
                    RaiseOnError(new AudioErrorEventArgs("Failed to generate authentication token."));
                    return false;
                }
            }
            catch(Exception ex)
            {
                RaiseOnError(new AudioErrorEventArgs($"Failed to generate authentication token - {ex.Message}"));

                return false;
            }
        }

        public void GenerateHeaders()
        {
            _headers.Add(new KeyValuePair<string, string>("Content-Type", "application/ssml+xml"));
            _headers.Add(new KeyValuePair<string, string>("X-Microsoft-OutputFormat", _outputFormat));
            _headers.Add(new KeyValuePair<string, string>("Authorization", _authorizationToken));
            _headers.Add(new KeyValuePair<string, string>("X-Search-AppId", Guid.NewGuid().ToString("N")));
            _headers.Add(new KeyValuePair<string, string>("X-Search-ClientID", Guid.NewGuid().ToString("N")));
            _headers.Add(new KeyValuePair<string, string>("User-Agent", "Chapter1"));
        }

        public Task SpeakAsync(string textToSpeak, CancellationToken cancellationToken)
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            var client = new HttpClient(handler);

            foreach(var header in _headers)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, RequestUri)
            {
                Content = new StringContent(string.Format(SsmlTemplate, _gender, _voiceName, textToSpeak))
            };

            var httpTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var saveTask = httpTask.ContinueWith(
                async (responseMessage, token) =>
                {
                    try
                    {
                        if(responseMessage.IsCompleted && responseMessage.Result != null && responseMessage.Result.IsSuccessStatusCode)
                        {
                            var httpStream = await responseMessage.Result.Content.ReadAsStreamAsync().ConfigureAwait(false);
                            RaiseOnAudioAvailable(new AudioEventArgs(httpStream));
                        }
                        else
                        {
                            RaiseOnError(new AudioErrorEventArgs($"Service returned {responseMessage.Result.StatusCode}"));
                        }
                    }
                    catch(Exception e)
                    {
                        RaiseOnError(new AudioErrorEventArgs(e.GetBaseException().Message));
                    }
                    finally
                    {
                        responseMessage.Dispose();
                        request.Dispose();
                        client.Dispose();
                        handler.Dispose();
                    }
                }, TaskContinuationOptions.AttachedToParent, cancellationToken);
            
            return saveTask;
        }

        private void RaiseOnAudioAvailable(AudioEventArgs args)
        {
            OnAudioAvailable?.Invoke(this, args);
        }

        private void RaiseOnError(AudioErrorEventArgs args)
        {
            OnError?.Invoke(this, args);
        }
    }

    public class AudioEventArgs : EventArgs
    {
        public AudioEventArgs(Stream eventData)
        {
            EventData = eventData;
        }

        public Stream EventData { get; private set; } 
    }

    public class AudioErrorEventArgs : EventArgs
    {
        public AudioErrorEventArgs(string message)
        {
            ErrorMessage = message;
        }

        public string ErrorMessage { get; private set; }
    }
}
