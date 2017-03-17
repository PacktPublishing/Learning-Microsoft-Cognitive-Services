using End_to_End.Interface;
using End_to_End.Model;
using Microsoft.Cognitive.LUIS;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System;
using System.Linq;
using System.Media;
using System.Diagnostics;
using System.Threading;
using End_to_End.Contracts;
using Microsoft.ProjectOxford.Vision;
using System.Collections.Generic;
using Microsoft.ProjectOxford.Vision.Contract;

namespace End_to_End.ViewModel
{
    public class LuisViewModel : ObservableObject, IDisposable
    {
        private Luis _luis;
        private SpeechToText _sttClient;
        private TextToSpeech _ttsClient;
        private BingSearch _bingSearch;
        private IVisionServiceClient _visionClient;
        
        private bool _requiresResponse = false;
        private LuisResult _lastResult = null;

        private string _bingApiKey = "BING_SPEECH_API_KEY";

        private string _inputText;
        public string InputText
        {
            get { return _inputText; }
            set
            {
                _inputText = value;
                RaisePropertyChangedEvent("InputText");
            }
        }

        private string _resultText;
        public string ResultText
        {
            get { return _resultText; }
            set
            {
                _resultText = value;
                RaisePropertyChangedEvent("ResultText");
            }
        }

        public ICommand RecordUtteranceCommand { get; private set; }
        public ICommand ExecuteUtteranceCommand { get; private set; }

        public LuisViewModel()
        {
            _bingSearch = new BingSearch();

            _visionClient = new VisionServiceClient("FACE_API_KEY");

            _luis = new Luis(new LuisClient("LUIS_APP_ID", "LUIS_API_KEY", true));
            _luis.OnLuisUtteranceResultUpdated += OnLuisUtteranceResultUpdated;

            _sttClient = new SpeechToText(_bingApiKey);
            _sttClient.OnSttStatusUpdated += OnSttStatusUpdated;

            _ttsClient = new TextToSpeech();
            _ttsClient.OnAudioAvailable += OnTtsAudioAvailable;
            _ttsClient.OnError += OnTtsError;

            if (_ttsClient.GenerateAuthenticationToken("SmartHouseApp", _bingApiKey))
                _ttsClient.GenerateHeaders();

            RecordUtteranceCommand = new DelegateCommand(RecordUtterance);
            ExecuteUtteranceCommand = new DelegateCommand(ExecuteUtterance, CanExecuteUtterance);
        }

        private void RecordUtterance(object obj)
        {
            _sttClient.StartMicToText();
        }

        private bool CanExecuteUtterance(object obj)
        {
            return !string.IsNullOrEmpty(InputText);
        }

        private void ExecuteUtterance(object obj)
        {
            CallLuis(InputText);
        }

        private async void CallLuis(string input)
        {
            if (!_requiresResponse)
            {
                await _luis.RequestAsync(input);
            }
            else
            {
                await _luis.ReplyAsync(_lastResult, input);
                _requiresResponse = false;
            }
        }

        private void OnTtsAudioAvailable(object sender, AudioEventArgs e)
        {
            SoundPlayer player = new SoundPlayer(e.EventData);
            player.Play();
            e.EventData.Dispose();
        }

        private void OnTtsError(object sender, AudioErrorEventArgs e)
        {
            Debug.WriteLine($"Status: Audio service failed -  {e.ErrorMessage}");
        }

        private void OnSttStatusUpdated(object sender, SpeechToTextEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => 
            {
                StringBuilder sb = new StringBuilder();

                if(e.Status == SttStatus.Success)
                {
                    if(!string.IsNullOrEmpty(e.Message))
                    {
                        sb.AppendFormat("Result message: {0}\n\n", e.Message);
                    }

                    if(e.Results != null && e.Results.Count != 0)
                    {
                        sb.Append("Retrieved the following results:\n");
                        foreach(string sentence in e.Results)
                        {
                            sb.AppendFormat("{0}\n\n", sentence);
                        }

                        sb.Append("Calling LUIS with the top result\n");

                        CallLuis(e.Results.FirstOrDefault());
                    }
                }
                else
                {
                    sb.AppendFormat("Could not convert speech to text: {0}\n", e.Message);
                }

                sb.Append("\n");

                ResultText = sb.ToString();
            });
        }

        private void OnLuisUtteranceResultUpdated(object sender, LuisUtteranceResultEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                StringBuilder sb = new StringBuilder(ResultText);
                
                _requiresResponse = e.RequiresReply;

                sb.AppendFormat("Status: {0}\n", e.Status);
                sb.AppendFormat("Summary: {0}\n\n", e.Message);
                
                sb.AppendFormat("Action: {0}\n", e.ActionName);
                sb.AppendFormat("Action triggered: {0}\n\n", e.ActionExecuted);

                if (e.ActionExecuted)
                    await TriggerActionExectution(e.ActionName, e.ActionValue);

                if (e.RequiresReply && !string.IsNullOrEmpty(e.DialogResponse))
                {
                    await _ttsClient.SpeakAsync(e.DialogResponse, CancellationToken.None);
                    sb.AppendFormat("Response: {0}\n", e.DialogResponse);
                    sb.Append("Reply in the left textfield");

                    RecordUtterance(sender);
                }

                ResultText = sb.ToString();
            });
        }

        private async Task TriggerActionExectution(string actionName, string actionValue)
        {
            LuisActions action;

            if (!Enum.TryParse(actionName, true, out action)) return;

            switch(action)
            {
                case LuisActions.GetRoomTemperature:
                case LuisActions.SetRoomTemperature:
                case LuisActions.None:
                default:
                    break;
                case LuisActions.GetNews:
                    await GetLatestNews(actionValue);
                    break;
            }
        }

        private async Task GetLatestNews(string queryString)
        {
            BingNewsResponse news = await _bingSearch.SearchNews(queryString, SafeSearch.Moderate);

            if (news.value == null || news.value.Length == 0) return;
            
            await ParseNews(news.value[0]);
        }

        private async Task ParseNews(Value newsArticle)
        {
            string imageDescription = await GetImageDescription(newsArticle.image.thumbnail.contentUrl);

            string articleDescription = $"{newsArticle.name}, published {newsArticle.datePublished}. Description: {newsArticle.description}. Corresponding image is {imageDescription}";

            await _ttsClient.SpeakAsync(articleDescription, CancellationToken.None);
        }

        private async Task<string> GetImageDescription(string contentUrl)
        {
            try
            {
                AnalysisResult imageAnalysisResult = await _visionClient.AnalyzeImageAsync(contentUrl, new List<VisualFeature>() { VisualFeature.Description });

                if (imageAnalysisResult == null || imageAnalysisResult.Description?.Captions?.Length == 0) return "none";

                return imageAnalysisResult.Description.Captions.First().Text;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return "none";
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
                _sttClient.Dispose();
        }
    }
}