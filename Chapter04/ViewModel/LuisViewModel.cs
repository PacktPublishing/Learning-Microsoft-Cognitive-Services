using End_to_End.Interface;
using End_to_End.Model;
using Microsoft.Cognitive.LUIS;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace End_to_End.ViewModel
{
    public class LuisViewModel : ObservableObject
    {
        private Luis _luis;
        private bool _requiresResponse = false;
        private LuisResult _lastResult = null;

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

        public ICommand ExecuteUtteranceCommand { get; private set; }

        public LuisViewModel()
        {
            _luis = new Luis(new LuisClient("APP_ID_HERE", "API_KEY_HERE", true));
            _luis.OnLuisUtteranceResultUpdated += OnLuisUtteranceResultUpdated;
            ExecuteUtteranceCommand = new DelegateCommand(ExecuteUtterance, CanExecuteUtterance);
        }

        private bool CanExecuteUtterance(object obj)
        {
            return !string.IsNullOrEmpty(InputText);
        }

        private async void ExecuteUtterance(object obj)
        {
            if (!_requiresResponse)
            {
                await _luis.RequestAsync(InputText);
            }
            else
            {
                await _luis.ReplyAsync(_lastResult, InputText);
                _requiresResponse = false;
            }
        }

        private void OnLuisUtteranceResultUpdated(object sender, LuisUtteranceResultEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => 
            {
                StringBuilder sb = new StringBuilder();

                _lastResult = e.Result;
                _requiresResponse = e.RequiresReply;

                sb.AppendFormat("Status: {0}\n", e.Status);
                sb.AppendFormat("Summary: {0}\n\n", e.Message);

                if(e.Result.Entities != null && e.Result.Entities.Count != 0)
                {
                    sb.AppendFormat("Entities found: {0}\n", e.Result.Entities.Count);
                    sb.Append("Entities:\n");

                    foreach(var entities in e.Result.Entities)
                    {
                        foreach(var entity in entities.Value)
                        {
                            sb.AppendFormat("Name: {0}\tValue: {1}\n", entity.Name, entity.Value);
                        }
                    }

                    sb.Append("\n");
                }

                sb.AppendFormat("Action: {0}\n", e.ActionName);
                sb.AppendFormat("Action triggered: {0}\n\n", e.ActionExecuted);

                if (e.RequiresReply && !string.IsNullOrEmpty(e.DialogResponse))
                {
                    sb.AppendFormat("Response: {0}\n", e.DialogResponse);
                    sb.Append("Reply in the left textfield");
                }

                ResultText = sb.ToString();
            });
        }
    }
}
