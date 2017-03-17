using Chapter6.Contracts;
using Chapter6.Interface;
using Chapter6.Model;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Windows.Input;

namespace Chapter6.ViewModel
{
    public class WebLmViewModel : ObservableObject
    {
        private WebRequest _webRequest;

        private string _inputQuery;
        public string InputQuery
        {
            get { return _inputQuery; }
            set
            {
                _inputQuery = value;
                RaisePropertyChangedEvent("InputQuery");
            }
        }

        private string _result;
        public string Result
        {
            get { return _result; }
            set
            {
                _result = value;
                RaisePropertyChangedEvent("Result");
            }
        }

        public ICommand BreakWordsCommand { get; private set; }
        public ICommand CondProbCommand { get; private set; }
        public ICommand JointProbCommand { get; private set; }
        public ICommand GenerateNextWordsCommand { get; private set; }

        /// <summary>
        /// WebLmViewModel constructor. Creates a <see cref="WebRequest"/> object and command objects
        /// </summary>
        public WebLmViewModel()
        {
            _webRequest = new WebRequest("https://api.projectoxford.ai/text/weblm/v1.0/", "API_KEY_HERE");
            BreakWordsCommand = new DelegateCommand(BreakWords, CanExecuteOperation);
            CondProbCommand = new DelegateCommand(CondProb, CanExecuteOperation);
            JointProbCommand = new DelegateCommand(JointProb, CanExecuteOperation);
            GenerateNextWordsCommand = new DelegateCommand(GenerateNextWords, CanExecuteOperation);
        }

        /// <summary>
        /// Determines if operations can be executed
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if input query has been specified, false otherwise</returns>
        private bool CanExecuteOperation(object obj)
        {
            return !string.IsNullOrEmpty(InputQuery);
        }

        /// <summary>
        /// Command function to get the conditional probability for a given word being the next in a sequence of words
        /// </summary>
        /// <param name="obj"></param>
        private async void CondProb(object obj)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            queryString["model"] = "body"; // Optional values include title/anchor/query/body
            //queryString["order"] = "5";

            var input = InputQuery.Split(';');

            WebLmConditionalProbRequest request = new WebLmConditionalProbRequest
            {
                queries = new WebLmCondProbQueries[] { new WebLmCondProbQueries { word = input[1], words = input[0] } }
            };

            WebLmCondProbResponse response = await _webRequest.MakeRequest<WebLmConditionalProbRequest, WebLmCondProbResponse>
                (HttpMethod.Post, $"calculateConditionalProbability?{queryString.ToString()}", request);

            if (response == null && response.results?.Length == 0)
            {
                Result = "Could not calculate the joint probability";
                return;
            }

            StringBuilder sb = new StringBuilder();

            foreach (WebLmCondProbResult candidate in response.results)
            {
                sb.AppendFormat("Probability of '{0}' being the next word in '{1}' is {2}\n", candidate.word, candidate.words, candidate.probability);
            }

            Result = sb.ToString();
        }

        /// <summary>
        /// Command function to get the joint probability that given words appears together
        /// </summary>
        /// <param name="obj"></param>
        private async void JointProb(object obj)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            queryString["model"] = "body"; // Optional values include title/anchor/query/body
            //queryString["order"] = "5";

            WebLmJointProbRequest request = new WebLmJointProbRequest
            {
                queries = InputQuery.Split(',')
            };

            WebLmJointProbResponse response = await _webRequest.MakeRequest<WebLmJointProbRequest, WebLmJointProbResponse>
                (HttpMethod.Post, $"calculateJointProbability?{queryString.ToString()}", request);

            if (response == null && response.results?.Length == 0)
            {
                Result = "Could not calculate the joint probability";
                return;
            }

            StringBuilder sb = new StringBuilder();

            foreach (WebLmJointProbResults candidate in response.results)
            {
                sb.AppendFormat("Probability of '{0}' to appear together is {1}\n", candidate.words, candidate.probability);
            }

            Result = sb.ToString();
        }

        /// <summary>
        /// Command function to break a long word into several words
        /// </summary>
        /// <param name="obj"></param>
        private async void BreakWords(object obj)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            queryString["model"] = "body"; // Optional values include title/anchor/query/body
            queryString["text"] = InputQuery;
            //queryString["order"] = "5";
            //queryString["maxNumOfCandidatesReturned"] = "5";

            WebLmWordBreakResponse response = await _webRequest.MakeRequest<object, WebLmWordBreakResponse>(HttpMethod.Post, $"breakIntoWords?{queryString.ToString()}");

            if(response == null && response.candidates?.Length != 0)
            {
                Result = "Could not break into words";
                return;
            }

            StringBuilder sb = new StringBuilder();

            foreach (WebLmCandidates candidate in response.candidates)
            {
                sb.AppendFormat("Candidate: {0}, with probability: {1}\n", candidate.words, candidate.probability);
            }

            Result = sb.ToString();
        }

        /// <summary>
        /// Command function to generate the next word in a sequence of words
        /// </summary>
        /// <param name="obj"></param>
        private async void GenerateNextWords(object obj)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            queryString["model"] = "body"; // Optional values include title/anchor/query/body
            queryString["words"] = InputQuery;
            //queryString["order"] = "5";
            //queryString["maxNumOfCandidatesReturned"] = "5";

            WebLmNextWordResults response = await _webRequest.MakeRequest<object, WebLmNextWordResults>(HttpMethod.Post, $"generateNextWords?{queryString.ToString()}");

            if (response == null && response.candidates?.Length == 0)
            {
                Result = "Could not generate next words";
                return;
            }

            StringBuilder sb = new StringBuilder();

            foreach (WebLmNextWordCandidates candidate in response.candidates)
            {
                sb.AppendFormat("Candidate: {0}, with probability: {1}\n", candidate.word, candidate.probability);
            }

            Result = sb.ToString();
        }
    }
}