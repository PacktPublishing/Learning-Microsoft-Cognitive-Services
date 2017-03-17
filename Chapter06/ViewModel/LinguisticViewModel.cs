using Chapter6.Contracts;
using Chapter6.Interface;
using Chapter6.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Windows.Input;

namespace Chapter6.ViewModel
{
    public class LinguisticViewModel : ObservableObject
    {
        private WebRequest _webRequest;

        private ObservableCollection<Analyzer> _analyzers = new ObservableCollection<Analyzer>();
        public ObservableCollection<Analyzer> Analyzers
        {
            get { return _analyzers; }
            set
            {
                _analyzers = value;
                RaisePropertyChangedEvent("Analyzers");
            }
        }

        private List<Analyzer> _selectedAnalyzers = new List<Analyzer>();
        public List<Analyzer> SelectedAnalyzers
        {
            get { return _selectedAnalyzers; }
            set { _selectedAnalyzers = value; }
        }

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

        public ICommand ExecuteOperationCommand { get; private set; }

        /// <summary>
        /// LinguisticViewModel constructor. Creates the <see cref="WebRequest"/> and command objects
        /// Will retrieve all available analyzers
        /// </summary>
        public LinguisticViewModel()
        {
            _webRequest = new WebRequest("https://api.projectoxford.ai/linguistics/v1.0/", "API_KEY_HERE");
            ExecuteOperationCommand = new DelegateCommand(ExecuteOperation, CanExecuteOperation);

            GetAnalyzers();
        }

        /// <summary>
        /// Determines if a command can be executed
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if the input query is entered, false otherwise</returns>
        private bool CanExecuteOperation(object obj)
        {
            return !string.IsNullOrEmpty(InputQuery);
        }

        /// <summary>
        /// Command function to execute the linguistic analysis, based on the given analyzers and input query
        /// Results is displayed in the UI
        /// </summary>
        /// <param name="obj"></param>
        private async void ExecuteOperation(object obj)
        {
            var queryString = HttpUtility.ParseQueryString("analyze");

            LinguisticRequest request = new LinguisticRequest
            {
                language = "en",
                analyzerIds = SelectedAnalyzers.Select(x => x.id).ToArray(),
                text = InputQuery
            };

            AnalyzerResults[] results = await _webRequest.MakeRequest<LinguisticRequest, AnalyzerResults[]>(HttpMethod.Post, queryString.ToString(), request);

            if(results == null || results.Length == 0)
            {
                Result = "Could not analyze text.";
                return;
            }

            StringBuilder sb = new StringBuilder();

            foreach (AnalyzerResults analyzedResult in results)
            {
                sb.AppendFormat("{0}\n", analyzedResult.result.ToString());
            }

            Result = sb.ToString();
        }

        /// <summary>
        /// Function to retrieve a list of available lingustic analyzers
        /// The list is displayed in the UI
        /// </summary>
        private async void GetAnalyzers()
        {
            var queryString = HttpUtility.ParseQueryString("analyzers");

            Analyzer[] response = await _webRequest.MakeRequest<object, Analyzer[]>(HttpMethod.Get, queryString.ToString());

            if (response == null || response.Length == 0) return;

            foreach (Analyzer analyzer in response)
            {
                Analyzers.Add(analyzer);
            }
        }
    }
}