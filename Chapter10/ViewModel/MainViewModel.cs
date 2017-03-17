using System;
using End_to_End.Interface;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.SpeakerRecognition;

namespace End_to_End.ViewModel
{
    public class MainViewModel : ObservableObject, IDisposable
    {
        private FaceServiceClient _faceServiceClient;
        private EmotionServiceClient _emotionServiceClient;
        private ISpeakerIdentificationServiceClient _speakerIdentificationClient;

        private AdministrationViewModel _administrationVm;
        public AdministrationViewModel AdministrationVm
        {
            get { return _administrationVm; }
            set
            {
                _administrationVm = value;
                RaisePropertyChangedEvent("AdministrationVm");
            }
        }

        private HomeViewModel _homeVm;
        public HomeViewModel HomeVm
        {
            get { return _homeVm; }
            set
            {
                _homeVm = value;
                RaisePropertyChangedEvent("HomeVm");
            }
        }

        private LuisViewModel _luisVm;
        public LuisViewModel LuisVm
        {
            get { return _luisVm; }
            set
            {
                _luisVm = value;
                RaisePropertyChangedEvent("LuisVm");
            }
        }

        private EntityLinkingViewModel _entityLinkingVm;
        public EntityLinkingViewModel EntityLinkingVm
        {
            get { return _entityLinkingVm; }
            set
            {
                _entityLinkingVm = value;
                RaisePropertyChangedEvent("EntityLinkingVm");
            }
        }

        private BingSearchViewModel _bingSearchVm;  
        public BingSearchViewModel BingSearchVm
        {
            get { return _bingSearchVm; }
            set
            {
                _bingSearchVm = value;
                RaisePropertyChangedEvent("BingSearchVm");
            }
        }

        public MainViewModel()
        {
            Initialize();
        }

        private void Initialize()
        {
            _faceServiceClient = new FaceServiceClient("API_KEY_HERE");
            _emotionServiceClient = new EmotionServiceClient("API_KEY_HERE");
            _speakerIdentificationClient = new SpeakerIdentificationServiceClient("API_KEY_HERE");

            AdministrationVm = new AdministrationViewModel(_faceServiceClient, _speakerIdentificationClient);
            HomeVm = new HomeViewModel(_faceServiceClient, _emotionServiceClient, _speakerIdentificationClient);
            LuisVm = new LuisViewModel();
            BingSearchVm = new BingSearchViewModel();

            EntityLinkingVm = new EntityLinkingViewModel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                LuisVm.Dispose();
            }
        }
    }
}
