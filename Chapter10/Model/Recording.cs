using NAudio.Utils;
using NAudio.Wave;
using System;
using System.IO;

namespace End_to_End.Model
{
    public class Recording
    {
        public event EventHandler<RecordingAudioAvailableEventArgs> OnAudioStreamAvailable;
        public event EventHandler<RecordingErrorEventArgs> OnRecordingError;

        private const int SAMPLERATE = 16000;
        private const int CHANNELS = 1;

        private WaveIn _waveIn;
        private WaveFileWriter _fileWriter;
        private Stream _stream;

        public Recording()
        {
            InitializeRecorder();
        }

        public void StartRecording()
        {
            if (_waveIn == null) return;

            try
            {
                if(WaveIn.DeviceCount == 0)
                {
                    RaiseRecordingError(new RecordingErrorEventArgs("No microphones detected"));
                    return;
                }

                _waveIn.StartRecording();
            }
            catch(Exception ex)
            {
                RaiseRecordingError(new RecordingErrorEventArgs($"Error when starting microphone recording: {ex.Message}"));
            }
        }

        public void StopRecording()
        {
            _waveIn.StopRecording();
        }

        private void InitializeRecorder()
        {
            _waveIn = new WaveIn();
            _waveIn.DeviceNumber = 0;
            _waveIn.WaveFormat = new WaveFormat(SAMPLERATE, CHANNELS);
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;            
        }
        
        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if(_fileWriter == null)
            {
                _stream = new IgnoreDisposeStream(new MemoryStream());
                _fileWriter = new WaveFileWriter(_stream, _waveIn.WaveFormat);
            }

            _fileWriter.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            _fileWriter.Dispose();
            _fileWriter = null;
            _stream.Seek(0, SeekOrigin.Begin);

            _waveIn.Dispose();
            InitializeRecorder();

            RaiseRecordingAudioAvailable(new RecordingAudioAvailableEventArgs(_stream));
        }

        private void RaiseRecordingAudioAvailable(RecordingAudioAvailableEventArgs args)
        {
            OnAudioStreamAvailable?.Invoke(this, args);
        }

        private void RaiseRecordingError(RecordingErrorEventArgs args)
        {
            OnRecordingError?.Invoke(this, args);
        }
    }

    public class RecordingAudioAvailableEventArgs : EventArgs
    {
        public Stream AudioStream { get; private set; }

        public RecordingAudioAvailableEventArgs(Stream audioStream)
        {
            AudioStream = audioStream;
        }
    }

    public class RecordingErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; private set; }

        public RecordingErrorEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}
