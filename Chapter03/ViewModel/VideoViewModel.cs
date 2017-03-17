using Chapter3.Interface;
using Chapter3.Model;
using Microsoft.ProjectOxford.Video.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Chapter3.ViewModel
{
    public class VideoViewModel : ObservableObject
    {
        private string _videoExtension = ".mp4";
        private string _filePath;
        private VideoOperations _videoOperations;
        
        private Uri _videoSource;
        public Uri VideoSource
        {
            get { return _videoSource; }
            set
            {
                _videoSource = value;
                RaisePropertyChangedEvent("VideoSource");
            }
        }

        private Uri _resultVideoSource;
        public Uri ResultVideoSource
        {
            get { return _resultVideoSource; }
            set
            {
                _resultVideoSource = value;
                RaisePropertyChangedEvent("ResultVideoSource");
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
        
        public IEnumerable<AvailableOperations> VideoOperations
        {
            get { return Enum.GetValues(typeof(AvailableOperations)).Cast<AvailableOperations>(); }
        }

        private AvailableOperations _selectedVideoOperation;
        public AvailableOperations SelectedVideoOperation
        {
            get { return _selectedVideoOperation; }
            set
            {
                _selectedVideoOperation = value;
                RaisePropertyChangedEvent("SelectedVideoOperation");
            }
        }
        
        public ICommand BrowseVideoCommand { get; private set; }
        public ICommand ExecuteVideoOperationCommand { get; private set; }

        /// <summary>
        /// Constructor for the VideoViewModel
        /// </summary>
        /// <param name="videoOperations">Requires a <see cref="VideoOperations"/> object</param>
        public VideoViewModel(VideoOperations videoOperations)
        {
            _videoOperations = videoOperations;
            _videoOperations.OnVideoOperationCompleted += OnVideoOperationCompleted;
            _videoOperations.OnVideoOperationStatus += OnVideoOperationStatus;

            BrowseVideoCommand = new DelegateCommand(BrowseVideo);
            ExecuteVideoOperationCommand = new DelegateCommand(ExecuteVideoOperation, CanExecuteVideoCommand);
        }

        /// <summary>
        /// Command function to browse for videos
        /// </summary>
        /// <param name="obj"></param>
        private void BrowseVideo(object obj)
        {
            Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
            openDialog.Filter = "Video files (*.mp4, *.mov, *.wmv)|*.mp4;*.mov;*.wmv";

            bool? result = openDialog.ShowDialog();

            if (!(bool)result) return;

            _filePath = openDialog.FileName;
            _videoExtension = Path.GetExtension(_filePath);
        }

        /// <summary>
        /// Function to determine if we can execute the video operation or not
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Returns true if we have loaded a video, otherwise false</returns>
        private bool CanExecuteVideoCommand(object obj)
        {
            return !string.IsNullOrEmpty(_filePath);
        }

        /// <summary>
        /// Command function to execute a specified video operation
        /// </summary>
        /// <param name="obj"></param>
        private async void ExecuteVideoOperation(object obj)
        {
            VideoOperationSettings operationSettings = _videoOperations.CreateVideoOperationSettings(SelectedVideoOperation);

            using (FileStream originalVideo = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
            {
                if (operationSettings == null)
                {
                    await _videoOperations.ExecuteVideoEmotionAnalysis(originalVideo);
                }
                else
                {
                    await _videoOperations.ExecuteVideoOperation(originalVideo, operationSettings);
                }
            }

            VideoSource = new Uri(_filePath);
        }
        
        /// <summary>
        /// Eventhandler for video operation status. Will print the current status, and any messages to the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnVideoOperationStatus(object sender, VideoOperationStatusEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}\n", e.Status);
                sb.AppendFormat("{0}\n", e.Message);

                Result = sb.ToString();
            });
        }

        /// <summary>
        /// Eventhanlder for video operation completion. If the operation succeeded and produced a result video
        /// it is fetched and displayed in the UI. 
        /// Will either way display the operation results as a nicely formatted JSON string.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnVideoOperationCompleted(object sender, VideoOperationResultEventArgs e)
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}\n", e.Status);
                sb.AppendFormat("{0}\n", e.Message);

                if (!string.IsNullOrEmpty(e.ProcessingResult))
                {
                    sb.Append("Results: \n");
                    sb.AppendFormat("{0}\n", JsonFormatter.FormatJson(e.ProcessingResult));
                }

                if (!string.IsNullOrEmpty(e.ResourceLocation))
                {
                    sb.AppendFormat("Video can be fetched at: {0}\n", e.ResourceLocation);
                    Stream resultVideo = await _videoOperations.GetResultVideoAsync(e.ResourceLocation);
                     
                    if (resultVideo == null) return;

                    string tempFilePath = Path.GetTempFileName() + _videoExtension;

                    using (FileStream stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        byte[] b = new byte[2048];
                        int lenght = 0;

                        while((lenght = await resultVideo.ReadAsync(b, 0, b.Length)) > 0)
                        {
                            await stream.WriteAsync(b, 0, lenght);
                        }
                    }

                    Uri fileUri = new Uri(tempFilePath);
                    ResultVideoSource = fileUri;
                }

                if(e.EmotionResult != null)
                {
                    sb.Append("Emotion results:\n");

                    foreach(var fragment in e.EmotionResult.Fragments)
                    {
                        if (fragment.Events == null) continue;

                        foreach(var aggregate in fragment.Events)
                        {
                            if (aggregate == null) continue;

                            foreach(var emotion in aggregate)
                            {
                                if (emotion == null) continue;

                                var emotionScores = emotion.WindowMeanScores.ToRankedList();

                                foreach(var score in emotionScores)
                                {
                                    sb.AppendFormat("Emotion: {0} / Score: {1}\n", score.Key, score.Value);
                                }
                            }
                            sb.Append("\n");
                        }
                        sb.Append("\n");
                    }
                }

                Result = sb.ToString();

            });
        }
    }
}