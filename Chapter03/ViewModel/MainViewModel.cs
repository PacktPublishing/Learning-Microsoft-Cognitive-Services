using Chapter3.Interface;
using Chapter3.Model;

namespace Chapter3.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        private VideoOperations _videoOperations;
        
        private VideoViewModel _videoVm;
        public VideoViewModel VideoVm
        {
            get { return _videoVm; }
            set
            {
                _videoVm = value;
                RaisePropertyChangedEvent("VideoVm");
            }
        }

        public MainViewModel()
        {
            _videoOperations = new VideoOperations();
            
            VideoVm = new VideoViewModel(_videoOperations);
        }
    }
}