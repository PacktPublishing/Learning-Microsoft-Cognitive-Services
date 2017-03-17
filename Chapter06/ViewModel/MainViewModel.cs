using Chapter6.Interface;

namespace Chapter6.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        private SpellCheckViewModel _spellCheckVm;
        public SpellCheckViewModel SpellCheckVm
        {
            get { return _spellCheckVm; }
            set
            {
                _spellCheckVm = value;
                RaisePropertyChangedEvent("SpellCheckVm");
            }
        }

        private TextAnalysisViewModel _textAnalysisVm;
        public TextAnalysisViewModel TextAnalysisVm
        {
            get { return _textAnalysisVm; }
            set
            {
                _textAnalysisVm = value;
                RaisePropertyChangedEvent("TextAnalysisVm");
            }
        }

        private LinguisticViewModel _linguisticVm;
        public LinguisticViewModel LinguisticVm
        {
            get { return _linguisticVm; }
            set
            {
                _linguisticVm = value;
                RaisePropertyChangedEvent("LinguisticVm");
            }
        }

        private WebLmViewModel _webLmVm;
        public WebLmViewModel WebLmVm
        {
            get { return _webLmVm; }
            set
            {
                _webLmVm = value;
                RaisePropertyChangedEvent("WebLmVm");
            }
        }

        /// <summary>
        /// MainViewModel constructor creates all other view model objects
        /// </summary>
        public MainViewModel()
        {
            SpellCheckVm = new SpellCheckViewModel();
            TextAnalysisVm = new TextAnalysisViewModel();
            LinguisticVm = new LinguisticViewModel();
            WebLmVm = new WebLmViewModel();
        }
    }
}