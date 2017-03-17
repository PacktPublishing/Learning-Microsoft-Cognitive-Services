using End_to_End.Interface;
using End_to_End.Model;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.SpeakerRecognition;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VideoFrameAnalyzer;

namespace End_to_End.ViewModel
{
    public class HomeViewModel : ObservableObject
    {
        private FaceServiceClient _faceServiceClient;
        private EmotionServiceClient _emotionServiceClient;
        private SpeakerIdentification _speakerIdentification;

        private Recording _recording;

        private FrameGrabber<CameraResult> _frameGrabber;
        private static readonly ImageEncodingParam[] s_jpegParams = {
            new ImageEncodingParam(ImwriteFlags.JpegQuality, 60)
        };

        #region Properties

        private ObservableCollection<PersonGroup> _personGroups = new ObservableCollection<PersonGroup>();
        public ObservableCollection<PersonGroup> PersonGroups
        {
            get { return _personGroups; }
            set
            {
                _personGroups = value;
                RaisePropertyChangedEvent("PersonGroups");
            }
        }

        private PersonGroup _selectedPersonGroup;        
        public PersonGroup SelectedPersonGroup
        {
            get { return _selectedPersonGroup; }
            set
            {
                _selectedPersonGroup = value;
                RaisePropertyChangedEvent("PersonGroups");
            }
        }
        
        private BitmapImage _imageSource;
        public BitmapImage ImageSource
        {
            get { return _imageSource; }
            set
            {
                _imageSource = value;
                RaisePropertyChangedEvent("ImageSource");
            }
        }
        
        private string _systemResponse;
        public string SystemResponse
        {
            get { return _systemResponse; }
            set
            {
                _systemResponse = value;
                RaisePropertyChangedEvent("SystemResponse");
            }
        }

        #endregion Properties

        #region ICommand properties

        public ICommand StopCameraCommand { get; private set; }
        public ICommand StartCameraCommand { get; private set; }
        public ICommand UploadOwnerImageCommand { get; private set; }
        public ICommand StartSpeakerRecording { get; private set; }
        public ICommand StopSpeakerRecording { get; private set; }

        #endregion ICommand properties

        public HomeViewModel(FaceServiceClient faceServiceClient, EmotionServiceClient emotionServiceClient, ISpeakerIdentificationServiceClient speakerIdentification)
        {
            _faceServiceClient = faceServiceClient;
            _emotionServiceClient = emotionServiceClient;
            _speakerIdentification = new SpeakerIdentification(speakerIdentification);

            _frameGrabber = new FrameGrabber<CameraResult>();
            _recording = new Recording();

            Initialize();
        }

        private void Initialize()
        {
            GetPersonGroups();
            _frameGrabber.NewFrameProvided += OnNewFrameProvided;
            _frameGrabber.NewResultAvailable += OnResultAvailable;
            _frameGrabber.AnalysisFunction = EmotionAnalysisAsync;

            StopCameraCommand = new DelegateCommand(StopCamera);
            StartCameraCommand = new DelegateCommand(StartCamera, CanStartCamera);
            UploadOwnerImageCommand = new DelegateCommand(UploadOwnerImage, CanUploadOwnerImage);

            _speakerIdentification.OnSpeakerIdentificationError += OnSpeakerIdentificationError;
            _speakerIdentification.OnSpeakerIdentificationStatusUpdated += OnSpeakerIdentificationStatusReceived;

            _recording.OnAudioStreamAvailable += OnSpeakerRecordingAvailable;
            _recording.OnRecordingError += OnSpeakerRecordingError;

            StartSpeakerRecording = new DelegateCommand(StartSpeaker);
            StopSpeakerRecording = new DelegateCommand(StopSpeaker);
        }

        private void StopSpeaker(object obj)
        {
            _recording.StopRecording();
        }

        private void StartSpeaker(object obj)
        {
            _recording.StartRecording();
        }

        private async void OnSpeakerRecordingAvailable(object sender, RecordingAudioAvailableEventArgs e)
        {
            try
            {
                List<Guid> profiles = await _speakerIdentification.ListSpeakerProfiles();
                _speakerIdentification.IdentifySpeaker(e.AudioStream, profiles.ToArray());
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnSpeakerRecordingError(object sender, RecordingErrorEventArgs e)
        {
            Debug.WriteLine(e.ErrorMessage);
        }

        private void OnSpeakerIdentificationStatusReceived(object sender, SpeakerIdentificationStatusUpdateEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => 
            {
                if (e.IdentifiedProfile == null) return;

                SystemResponse = $"Hi there, {e.IdentifiedProfile.IdentifiedProfileId}";
            });
        }

        private void OnSpeakerIdentificationError(object sender, SpeakerIdentificationErrorEventArgs e)
        {
            Debug.WriteLine(e.ErrorMessage);
        }

        private bool CanUploadOwnerImage(object obj)
        {
            return SelectedPersonGroup != null;
        }

        private async void UploadOwnerImage(object obj)
        {
            try
            {
                SystemResponse = "Identifying...";

                var openDialog = new Microsoft.Win32.OpenFileDialog();

                openDialog.Filter = "Image Files(*.jpg, *.gif, *.bmp, *.png)|*.jpg;*.jpeg;*.gif;*.bmp;*.png";
                bool? result = openDialog.ShowDialog();

                if (!(bool)result) return;

                string filePath = openDialog.FileName;

                Uri fileUri = new Uri(filePath);
                BitmapImage image = new BitmapImage(fileUri);

                image.CacheOption = BitmapCacheOption.None;
                image.UriSource = fileUri;

                ImageSource = image;

                using (Stream imageFile = File.OpenRead(filePath))
                {
                    Face[] faces = await _faceServiceClient.DetectAsync(imageFile);
                    Guid[] faceIds = faces.Select(face => face.FaceId).ToArray();

                    IdentifyResult[] personsIdentified = await _faceServiceClient.IdentifyAsync(SelectedPersonGroup.PersonGroupId, faceIds, 1);

                    foreach(IdentifyResult personIdentified in personsIdentified)
                    { 
                        if(personIdentified.Candidates.Length == 0)     
                        {
                            SystemResponse = "Failed to identify you.";
                            break;
                        }

                        Guid personId = personIdentified.Candidates[0].PersonId;
                        Person person = await _faceServiceClient.GetPersonAsync(SelectedPersonGroup.PersonGroupId, personId);

                        if(person != null)
                        {
                            SystemResponse = $"Welcome home, {person.Name}";
                            break;
                        }
                    }
                }
            }
            catch(FaceAPIException ex)
            {
                SystemResponse = $"Failed to identify you: {ex.ErrorMessage}";
            }
            catch(Exception ex)
            {
                SystemResponse = $"Failed to identify you: {ex.Message}";
            }            
        }

        private async void GetPersonGroups()
        {
            try
            {
                PersonGroup[] personGroups = await _faceServiceClient.ListPersonGroupsAsync();

                if (personGroups == null || personGroups.Length == 0) return;

                PersonGroups.Clear();

                foreach(PersonGroup group in personGroups)
                {
                    PersonGroups.Add(group);
                }
            }
            catch(FaceAPIException ex)
            {
                SystemResponse = $"Failed to get person groups: {ex.ErrorMessage}";
            }
            catch(Exception ex)
            {
                SystemResponse = $"Failed to get person groups: {ex.Message}";
            }
        }

        private bool CanStartCamera(object obj)
        {
            return _frameGrabber.GetNumCameras() > 0 && SelectedPersonGroup != null;
        }

        private async void StartCamera(object obj)
        {
            _frameGrabber.TriggerAnalysisOnInterval(TimeSpan.FromSeconds(5));
            await _frameGrabber.StartProcessingCameraAsync();
        }

        private async void StopCamera(object obj)
        {
            await _frameGrabber.StopProcessingAsync();
        }

        private void OnNewFrameProvided(object sender, FrameGrabber<CameraResult>.NewFrameEventArgs e)
        {          
            Application.Current.Dispatcher.Invoke(() =>
            {
                BitmapSource bitmapSource = e.Frame.Image.ToBitmapSource();

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                MemoryStream memoryStream = new MemoryStream();
                BitmapImage image = new BitmapImage();

                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(memoryStream);

                memoryStream.Position = 0;
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = memoryStream;
                image.EndInit();

                memoryStream.Close();

                ImageSource = image;
            });
        }

        private void OnResultAvailable(object sender, FrameGrabber<CameraResult>.NewResultEventArgs e)
        {
            var analysisResult = e.Analysis.EmotionScores;

            if (analysisResult == null || analysisResult.Length == 0) return;

            string emotion = AnalyseEmotions(analysisResult[0]);

            Application.Current.Dispatcher.Invoke(() =>
            {
                SystemResponse = $"You seem to be {emotion} today.";
            });
        }

        private string AnalyseEmotions(Scores analysisResult)
        {
            string emotion = string.Empty;

            var sortedEmotions = analysisResult.ToRankedList();

            string currentEmotion = sortedEmotions.First().Key;

            switch(currentEmotion)
            {
                case "Anger":
                    emotion = "angry";
                    break;
                case "Contempt":
                    emotion = "contempt";
                    break;
                case "Disgust":
                    emotion = "disgusted";
                    break;
                case "Fear":
                    emotion = "scared";
                    break;
                case "Happiness":
                    emotion = "happy";
                    break;
                case "Neutral":
                default:
                    emotion = "neutral";
                    break;
                case "Sadness":
                    emotion = "sad";
                    break;
                case "Suprise":
                    emotion = "suprised";
                    break;
            }

            return emotion;
        }

        private async Task<CameraResult> EmotionAnalysisAsync(VideoFrame frame)
        {
            MemoryStream jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);

            try
            {
                Emotion[] emotions = await _emotionServiceClient.RecognizeAsync(jpg);

                return new CameraResult
                {
                    EmotionScores = emotions.Select(e => e.Scores).ToArray()
                };
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Failed to analyze emotions: {ex.Message}");

                return null;
            }            
        }
    }

    internal class CameraResult
    {
        public Scores[] EmotionScores { get; set; } = null;
    }
}
