using System;
using End_to_End.Interface;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Emotion;

namespace End_to_End.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        private FaceServiceClient _faceServiceClient;
        private EmotionServiceClient _emotionServiceClient;

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


        public MainViewModel()
        {
            Initialize();
        }

        private void Initialize()
        {
            _faceServiceClient = new FaceServiceClient("API_KEY_HERE");
            _emotionServiceClient = new EmotionServiceClient("API_KEY_HERE");

            AdministrationVm = new AdministrationViewModel(_faceServiceClient);
            HomeVm = new HomeViewModel(_faceServiceClient, _emotionServiceClient);
            LuisVm = new LuisViewModel();
        }
    }
}
