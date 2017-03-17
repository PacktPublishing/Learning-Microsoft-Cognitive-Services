using Chapter3.Interface;
using System;
using System.Threading.Tasks;
using System.IO;
using Microsoft.ProjectOxford.Video;
using Microsoft.ProjectOxford.Video.Contract;
using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Common.Contract;

namespace Chapter3.Model
{
    public class VideoOperations
    {
        private EmotionServiceClient _emotionServiceClient;
        private VideoServiceClient _videoServiceClient;

        public event EventHandler<VideoOperationResultEventArgs> OnVideoOperationCompleted;
        public event EventHandler<VideoOperationStatusEventArgs> OnVideoOperationStatus;

        /// <summary>
        /// VideoOperations constructor. Creates new objects for Emotion and Video APIs
        /// </summary>
        public VideoOperations()
        {
            _emotionServiceClient = new EmotionServiceClient("API_KEY_HERE");
            _videoServiceClient = new VideoServiceClient("API_KEY_HERE");
        }
        
        /// <summary>
        /// Function to create video operation settings. Emotion operations should not have any settings
        /// </summary>
        /// <param name="operation">The currently selected API operation</param>
        /// <returns>Returns default <see cref="VideoOperationSettings"/> for the currently selected operation</returns>
        public VideoOperationSettings CreateVideoOperationSettings(AvailableOperations operation)
        {
            VideoOperationSettings videoOperationSettings = null;

            switch (operation)
            {
                case AvailableOperations.Emotion:
                    videoOperationSettings = null;
                    break;
                case AvailableOperations.FaceDetection:
                    videoOperationSettings = new FaceDetectionOperationSettings();
                    break;
                case AvailableOperations.Stabilization:
                    videoOperationSettings = new VideoStabilizationOperationSettings();
                    break;
                case AvailableOperations.MotionDetection:
                    videoOperationSettings = new MotionDetectionOperationSettings()
                    {
                        DetectLightChange = true,
                        FrameSamplingValue = 10,
                        MergeTimeThreshold = 10,
                        SensitivityLevel = MotionDetectionOperationSettings.SensitivityLevels.Medium
                    };
                    break;
                case AvailableOperations.Thumbnail:
                    videoOperationSettings = new VideoThumbnailOperationSettings()
                    {
                        FadeInFadeOut = true,
                        MaxMotionThumbnailDurationInSecs = 10,
                        OutputAudio = true,
                        OutputType = VideoThumbnailOperationSettings.OutputTypes.Video
                    };
                    break;
                default:
                    break;
            }

            return videoOperationSettings;
        }

        /// <summary>
        /// Execute emotion analysis on a video stream
        /// Starts a new task to get the operation results
        /// </summary>
        /// <param name="videoStream">Video Stream to analyze</param>
        /// <returns></returns>
        public async Task ExecuteVideoEmotionAnalysis(Stream videoStream)
        {
            try
            {
                VideoEmotionRecognitionOperation operation = await _emotionServiceClient.RecognizeInVideoAsync(videoStream);

                if (operation == null)
                {
                    RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", "Failed to analyze emotions in video"));
                    return;
                }

                await Task.Run(() => GetVideoEmotionResultAsync(operation));
            }
            catch (ClientException ex)
            {
                RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", $"Failed to execute video operation: {ex.Error.Message}"));
            }
            catch (Exception ex)
            {
                RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", $"Failed to execute video operation: {ex.Message}"));
            }
        }

        /// <summary>
        /// Execute the selected video operation, when the video is a Stream object. 
        /// Starts a new task to get the operation results
        /// </summary>
        /// <param name="stream">Video to do an operation on</param>
        /// <param name="videoOperationSettings"><see cref="VideoOperationSettings"/> for the currently selected operation</param>
        /// <returns>No return value</returns>
        public async Task ExecuteVideoOperation(Stream stream, VideoOperationSettings videoOperationSettings)
        {
            try
            {
                Operation operation = await _videoServiceClient.CreateOperationAsync(stream, videoOperationSettings);

                if (operation == null)
                {
                    RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", "Failed to create video operation"));
                    return;
                }

                await Task.Run(() => GetVideoOperationResultAsync(operation));
            }
            catch (Exception ex)
            {
                RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", $"Failed to execute video operation: {ex.Message}"));
            }
        }

        /// <summary>
        /// Execute the selected video operation, when the video is a byte array. 
        /// Starts a new task to get the operation results
        /// </summary>
        /// <param name="stream">Byte array with video</param>
        /// <param name="videoOperationSettings"><see cref="VideoOperationSettings"/> for the currently selected operation</param>
        /// <returns>No return value</returns>
        public async Task ExecuteVideoOperation(byte[] stream, VideoOperationSettings videoOperationSettings)
        {
            try
            {
                Operation operation = await _videoServiceClient.CreateOperationAsync(stream, videoOperationSettings);

                if (operation == null)
                {
                    RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", "Failed to create video operation"));
                    return;
                }

                await Task.Run(() => GetVideoOperationResultAsync(operation));
            }
            catch (Exception ex)
            {
                RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", $"Failed to execute video operation: {ex.Message}"));
            }
        }

        /// <summary>
        /// Execute the selected video operation, when a video is in a given URL 
        /// Starts a new task to get the operation results
        /// </summary>
        /// <param name="videoUrl">String containing the video URL</param>
        /// <param name="videoOperationSettings"><see cref="VideoOperationSettings"/> for the currently selected operation</param>
        /// <returns>No return value</returns>
        public async Task ExecuteVideoOperation(string videoUrl, VideoOperationSettings videoOperationSettings)
        {
            try
            {
                Operation operation = await _videoServiceClient.CreateOperationAsync(videoUrl, videoOperationSettings);

                if (operation == null)
                {
                    RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", "Failed to create video operation"));
                    return;
                }

                await Task.Run(() => GetVideoOperationResultAsync(operation));
            }
            catch(Exception ex)
            {
                RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", $"Failed to execute video operation: {ex.Message}"));
            }
        }

        /// <summary>
        /// Retrieves a Stream object, containing the result video (e.g generated video thumbnail or stabilized video).
        /// Accepts the URL for the video, based on the operation results
        /// </summary>
        /// <param name="url">URL for the result video</param>
        /// <returns>Stream of the video</returns>
        public async Task<Stream> GetResultVideoAsync(string url)
        {
            try
            {
                Stream resultStream = await _videoServiceClient.GetResultVideoAsync(url);
                return resultStream;
            }
            catch(Exception ex)
            {
                RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", $"Failed to get video result: {ex.Message}"));
                return null;
            }
        }

        /// <summary>
        /// Check the current status of the video operation. If the processing is not completed (which it can be from failing or succeeding)
        /// we will check again in 20 seconds. If the process has succeeded, the results is sent with an event
        /// </summary>
        /// <param name="videoOperation">The current video <see cref="Operation"/>, containing location for the operation status</param>
        private async void GetVideoOperationResultAsync(Operation videoOperation)
        {
            try
            {
                while(true)
                {
                    OperationResult operationResult = await _videoServiceClient.GetOperationResultAsync(videoOperation);

                    bool isCompleted = false;

                    switch(operationResult.Status)
                    {
                        case OperationStatus.Failed:
                            RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", $"Video operation failed: {operationResult.Message}"));
                            isCompleted = true;
                            break;
                        case OperationStatus.NotStarted:
                            RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Not started", "Video operation has not started yet"));
                            break;
                        case OperationStatus.Running:
                        default:
                            RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Running", "Video operation is running"));
                            break;
                        case OperationStatus.Uploading:
                            RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Uploading", "Video is uploading"));
                            break;
                        case OperationStatus.Succeeded:
                            RaiseVideoOperationCompleted(new VideoOperationResultEventArgs
                            {
                                Status = "Succeeded",
                                Message = "Video operation completed successfully",
                                ProcessingResult = operationResult.ProcessingResult,
                                ResourceLocation = operationResult.ResourceLocation,
                            });
                            isCompleted = true;
                            break;
                    }

                    if (isCompleted)
                        break;
                    else
                        await Task.Delay(TimeSpan.FromSeconds(20));
                }
            }
            catch (ClientException ex)
            {
                RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", $"Failed to execute video operation: {ex.Error.Message}"));
            }
            catch (Exception ex)
            {
                RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", $"Failed to get video operation result: {ex.Message}"));
            }
        }

        /// <summary>
        /// Check the current status of the video emotion operation. If the processing is not completed (which it can be from failing or succeeding)
        /// we will check again in 20 seconds. If the process has succeeded, the results is sent with an event
        /// </summary>
        /// <param name="videoOperation">The current video <see cref="VideoEmotionRecognitionOperation"/>, containing location for the operation status</param>
        private async void GetVideoEmotionResultAsync(VideoEmotionRecognitionOperation videoOperation)
        {
            try
            {
                while (true)
                {
                    var operationResult = await _emotionServiceClient.GetOperationResultAsync(videoOperation);

                    bool isCompleted = false;

                    switch (operationResult.Status)
                    {
                        case VideoOperationStatus.Failed:
                            RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", $"Video operation failed: {operationResult.Message}"));
                            isCompleted = true;
                            break;
                        case VideoOperationStatus.NotStarted:
                            RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Not started", "Video operation has not started yet"));
                            break;
                        case VideoOperationStatus.Running:
                        default:
                            RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Running", "Video operation is running"));
                            break;
                        case VideoOperationStatus.Uploading:
                            RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Uploading", "Video is uploading"));
                            break;
                        case VideoOperationStatus.Succeeded:
                            var result = operationResult as VideoOperationInfoResult<VideoAggregateRecognitionResult>;
                            RaiseVideoOperationCompleted(new VideoOperationResultEventArgs
                            {
                                Status = "Succeeded",
                                Message = "Video operation completed successfully",
                                EmotionResult = result.ProcessingResult,
                            });
                            isCompleted = true;
                            break;
                    }

                    if (isCompleted)
                        break;
                    else
                        await Task.Delay(TimeSpan.FromSeconds(20));
                }
            }
            catch (ClientException ex)
            {
                RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", $"Failed to execute video operation: {ex.Error.Message}"));
            }
            catch (Exception ex)
            {
                RaiseVideoOperationStatus(new VideoOperationStatusEventArgs("Failed", $"Failed to get video operation result: {ex.Message}"));
            }
        }

        /// <summary>
        /// Helper function to raise OnVideoOperationStatus event
        /// </summary>
        /// <param name="args"><see cref="VideoOperationStatusEventArgs"/> - Event arguments</param>
        private void RaiseVideoOperationStatus(VideoOperationStatusEventArgs args)
        {
            OnVideoOperationStatus?.Invoke(this, args);
        }

        /// <summary>
        /// Helper function to raise OnVideoOperationCompleted event
        /// </summary>
        /// <param name="args"><see cref="VideoOperationResultEventArgs"/> - Event arguments</param>
        private void RaiseVideoOperationCompleted(VideoOperationResultEventArgs args)
        {
            OnVideoOperationCompleted?.Invoke(this, args);
        }
    }

    /// <summary>
    /// EventArgs class containing information of the video operation status
    /// </summary>
    public class VideoOperationStatusEventArgs : EventArgs
    {
        public string Status { get; private set; }
        public string Message { get; private set; }

        public VideoOperationStatusEventArgs(string status, string message)
        {
            Status = status;
            Message = message;
        }
    }

    /// <summary>
    /// EventArgs class containing information of the video operation results
    /// </summary>
    public class VideoOperationResultEventArgs : EventArgs
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string ProcessingResult { get; set; }
        public string ResourceLocation { get; set; }
        public VideoAggregateRecognitionResult EmotionResult { get; set; }
    }
}