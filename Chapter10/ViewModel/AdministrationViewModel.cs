using End_to_End.Interface;
using End_to_End.Model;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.SpeakerRecognition;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Identification;
using NAudio.Wave;
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

namespace End_to_End.ViewModel
{
    public class AdministrationViewModel : ObservableObject
    {
        private SpeakerIdentification _speakerIdentification;
        private FaceServiceClient _faceServiceClient;
        private Recording _recorder;

        #region Properties

        private string _statusText;
        public string StatusText
        {
            get { return _statusText; }
            set
            {
                _statusText = value;
                RaisePropertyChangedEvent("StatusText");
            }
        }
        
        private string _personGroupName;
        public string PersonGroupName
        {
            get { return _personGroupName; }
            set
            {
                _personGroupName = value;
                RaisePropertyChangedEvent("PersonGroupName");
            }
        }

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
                RaisePropertyChangedEvent("SelectedPersonGroup");
                GetPersons();
            }
        }

        private string _personName;
        public string PersonName
        {
            get { return _personName; }
            set
            {
                _personName = value;
                RaisePropertyChangedEvent("PersonName");
            }
        }

        private ObservableCollection<Person> _persons = new ObservableCollection<Person>();
        public ObservableCollection<Person> Persons
        {
            get { return _persons; }
            set
            {
                _persons = value;
                RaisePropertyChangedEvent("Persons");
            }
        }

        private Person _selectedPerson;
        public Person SelectedPerson
        {
            get { return _selectedPerson; }
            set
            {
                _selectedPerson = value;
                RaisePropertyChangedEvent("SelectedPerson");
            }
        }
        
        private ObservableCollection<Guid> _speakerProfiles = new ObservableCollection<Guid>();
        public ObservableCollection<Guid> SpeakerProfiles
        {
            get { return _speakerProfiles; }
            set
            {
                _speakerProfiles = value;
                RaisePropertyChangedEvent("SpeakerProfiles");
            }
        }

        private Guid _selectedSpeakerProfile;
        public Guid SelectedSpeakerProfile
        {
            get { return _selectedSpeakerProfile; }
            set
            {
                _selectedSpeakerProfile = value;
                RaisePropertyChangedEvent("SelectedSpeakerProfile");
            }
        }
                
        #endregion Properties

        #region ICommand Properties

        public ICommand AddPersonGroupCommand { get; private set; }
        public ICommand TrainPersonGroupCommand { get; private set; }
        public ICommand DeleteSelectedPersonGroup { get; private set; }
        public ICommand AddPersonCommand { get; private set; }
        public ICommand DeletePersonCommand { get; private set; }
        public ICommand AddPersonFaceCommand { get; private set; }
        public ICommand AddSpeakerNameCommand { get; private set; }
        public ICommand DeleteSpeakerProfileCommand { get; private set; }
        public ICommand EnrollSpeakerCommand { get; private set; }
        public ICommand ResetEnrollmentsCommand { get; private set; }
        public ICommand StopRecordingCommand { get; private set; }

        #endregion ICommand Properties

        public AdministrationViewModel(FaceServiceClient faceServiceClient, ISpeakerIdentificationServiceClient speakerIdentification)
        {
            _speakerIdentification = new SpeakerIdentification(speakerIdentification);
            _speakerIdentification.OnSpeakerIdentificationError += OnSpeakerIdentificationError;
            _speakerIdentification.OnSpeakerIdentificationStatusUpdated += OnSpeakerIdentificationStatusUpdated;

            _faceServiceClient = faceServiceClient;

            Initialize();
        }

        private void Initialize()
        {
            _recorder = new Recording();
            _recorder.OnAudioStreamAvailable += OnRecordingAudioStreamAvailable;
            _recorder.OnRecordingError += OnRecordingError;

            AddPersonGroupCommand = new DelegateCommand(AddPersonGroup, CanAddPersonGroup);
            TrainPersonGroupCommand = new DelegateCommand(TrainPersonGroup, CanTrainPersonGroup);
            DeleteSelectedPersonGroup = new DelegateCommand(DeletePersonGroup, CanDeletePersonGroup);
            AddPersonCommand = new DelegateCommand(AddPerson, CanAddPerson);
            DeletePersonCommand = new DelegateCommand(DeletePerson, CanDeletePerson);
            AddPersonFaceCommand = new DelegateCommand(AddPersonFace, CanAddPersonFace);

            GetPersonGroups();

            AddSpeakerNameCommand = new DelegateCommand(AddSpeaker);
            DeleteSpeakerProfileCommand = new DelegateCommand(DeleteSpeaker, CanExecuteSpeakerProfileOperations);
            EnrollSpeakerCommand = new DelegateCommand(EnrollSpeaker, CanExecuteSpeakerProfileOperations);
            ResetEnrollmentsCommand = new DelegateCommand(ResetEnrollment, CanExecuteSpeakerProfileOperations);
            StopRecordingCommand = new DelegateCommand(StopRecording);

            GetSpeakerProfiles();

            StatusText = "ViewModel initialized";
        }

        #region Speaker identification

        private bool CanExecuteSpeakerProfileOperations(object obj)
        {
            return SelectedSpeakerProfile != null && !SelectedSpeakerProfile.Equals(Guid.Empty);
        }
        
        private async void GetSpeakerProfiles()
        {
            List<Guid> profiles = await _speakerIdentification.ListSpeakerProfiles();

            if (profiles == null) return;

            foreach(Guid profile in profiles)
            {
                SpeakerProfiles.Add(profile);
            }
        }

        private async void AddSpeaker(object obj)
        {
            Guid speakerId = await _speakerIdentification.CreateSpeakerProfile();

            GetSpeakerProfiles();
        }

        private void DeleteSpeaker(object obj)
        {
            _speakerIdentification.DeleteSpeakerProfile(SelectedSpeakerProfile);
        }

        private void EnrollSpeaker(object obj)
        {
            _recorder.StartRecording();
        }

        private void ResetEnrollment(object obj)
        {
            _speakerIdentification.ResetEnrollments(SelectedSpeakerProfile);
        }

        private void StopRecording(object obj)
        {
            _recorder.StopRecording();
        }

        private void OnRecordingAudioStreamAvailable(object sender, RecordingAudioAvailableEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => 
            {
                _speakerIdentification.CreateSpeakerEnrollment(e.AudioStream, SelectedSpeakerProfile);
            });
        }

        private void OnRecordingError(object sender, RecordingErrorEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => 
            {
                StatusText = e.ErrorMessage;
            });
        }
        
        private void OnSpeakerIdentificationStatusUpdated(object sender, SpeakerIdentificationStatusUpdateEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => 
            {
                StatusText = e.Message;
            });
        }

        private void OnSpeakerIdentificationError(object sender, SpeakerIdentificationErrorEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => 
            {
                StatusText = e.ErrorMessage;
            });
        }

        #endregion Speaker identification

        #region Person handling 

        private bool CanAddPerson(object obj)
        {
            return SelectedPersonGroup != null && !string.IsNullOrEmpty(PersonName);
        }

        private async void AddPerson(object obj)
        {
            try
            {
                CreatePersonResult personId = await _faceServiceClient.CreatePersonAsync(SelectedPersonGroup.PersonGroupId, PersonName);

                StatusText = $"Added person {PersonName} got ID: {personId.PersonId.ToString()}";

                GetPersons();
            }
            catch(FaceAPIException ex)
            {
                StatusText = $"Failed to add person: {ex.ErrorMessage}";
            }
            catch(Exception ex)
            {
                StatusText = $"Failed to add person: {ex.Message}";
            }
        }

        private bool CanDeletePerson(object obj)
        {
            return SelectedPersonGroup != null && SelectedPerson != null;
        }

        private async void DeletePerson(object obj)
        {
            try
            {
                await _faceServiceClient.DeletePersonAsync(SelectedPersonGroup.PersonGroupId, SelectedPerson.PersonId);

                StatusText = $"Deleted {SelectedPerson.Name} from {SelectedPersonGroup.Name}";

                GetPersons();
            }
            catch(FaceAPIException ex)
            {
                StatusText = $"Could not delete {SelectedPerson.Name}: {ex.ErrorMessage}";
            }
            catch(Exception ex)
            {
                StatusText = $"Could not delete {SelectedPerson.Name}: {ex.Message}";
            }
        }

        private bool CanAddPersonFace(object obj)
        {
            return SelectedPersonGroup != null && SelectedPerson != null;
        }

        private async void AddPersonFace(object obj)
        {
            try
            {
                var openDialog = new Microsoft.Win32.OpenFileDialog();

                openDialog.Filter = "Image Files(*.jpg, *.gif, *.bmp, *.png)|*.jpg;*.jpeg;*.gif;*.bmp;*.png";
                bool? result = openDialog.ShowDialog();

                if (!(bool)result) return;

                string filePath = openDialog.FileName;

                using (Stream imageFile = File.OpenRead(filePath))
                {
                    AddPersistedFaceResult addFaceResult = await _faceServiceClient.AddPersonFaceAsync(SelectedPersonGroup.PersonGroupId, SelectedPerson.PersonId, imageFile);

                    if (addFaceResult != null)
                    {
                        StatusText = $"Face added for {SelectedPerson.Name}. Remeber to train the person group!";
                    }
                }
            }
            catch(FaceAPIException ex)
            {
                StatusText = $"Failed to add person face: {ex.ErrorMessage}";
            }
            catch(Exception ex)
            {
                StatusText = $"Failed to add person face: {ex.Message}";
            }
        }

        private async void GetPersons()
        {
            if (SelectedPersonGroup == null) return;

            Persons.Clear();

            try
            {
                Person[] persons = await _faceServiceClient.GetPersonsAsync(SelectedPersonGroup.PersonGroupId);

                if (persons == null || persons.Length == 0)
                {
                    StatusText = $"No persons found in {SelectedPersonGroup.Name}.";
                    return;
                }

                foreach (Person person in persons)
                {
                    Persons.Add(person);
                }
            }
            catch(FaceAPIException ex)
            {
                StatusText = $"Failed to get persons from {SelectedPersonGroup.Name}: {ex.ErrorMessage}";
            }
            catch(Exception ex)
            {
                StatusText = $"Failed to get persons from {SelectedPersonGroup.Name}: {ex.Message}";
            }
        }

        #endregion Person handling

        #region Person group handling

        private bool CanAddPersonGroup(object obj)
        {
            return !string.IsNullOrEmpty(PersonGroupName);
        }

        private async void AddPersonGroup(object obj)
        {
            try
            {
                if(await DoesPersonGroupExistAsync(PersonGroupName.ToLower()))
                {
                    StatusText = $"Person group {PersonGroupName} already exists";
                    return;
                }

                await _faceServiceClient.CreatePersonGroupAsync(PersonGroupName.ToLower(), PersonGroupName);
                StatusText = $"Person group {PersonGroupName} added";
                GetPersonGroups();
            }
            catch(FaceAPIException ex)
            {
                StatusText = $"Failed to add person group: {ex.ErrorMessage}";
            }
            catch(Exception ex)
            {
                StatusText = $"Failed to add person group: {ex.Message}";
            }
        }

        private async Task<bool> DoesPersonGroupExistAsync(string personGroupId)
        {
            bool result = false;

            try
            {
                PersonGroup personGroup = await _faceServiceClient.GetPersonGroupAsync(personGroupId);

                if (personGroup != null)
                    result = true;
            }
            catch(Exception)
            {
                result = false;
            }

            return result;
        }

        private async void GetPersonGroups()
        {
            try
            {
                PersonGroup[] personGroups = await _faceServiceClient.ListPersonGroupsAsync();

                if(personGroups == null || personGroups.Length == 0)
                {
                    StatusText = "No person groups found.";
                    return;
                }

                PersonGroups.Clear();

                foreach (PersonGroup personGroup in personGroups)
                {
                    PersonGroups.Add(personGroup); 
                }
            }
            catch(FaceAPIException ex)
            {
                StatusText = $"Failed to fetch person groups: {ex.ErrorMessage}";
            }
            catch(Exception ex)
            {
                StatusText = $"Failed to fetch person groups: {ex.Message}";
            }
        }

        private bool CanDeletePersonGroup(object obj)
        {
            return SelectedPersonGroup != null;
        }

        private async void DeletePersonGroup(object obj)
        {
            try
            {
                await _faceServiceClient.DeletePersonGroupAsync(SelectedPersonGroup.PersonGroupId);

                StatusText = $"Deleted person group {SelectedPersonGroup.Name}";

                GetPersonGroups();
            }
            catch(FaceAPIException ex)
            {
                StatusText = $"Could not delete person group {SelectedPersonGroup.Name}: {ex.ErrorMessage}";
            }
            catch(Exception ex)
            {
                StatusText = $"Could not delete person group {SelectedPersonGroup.Name}: {ex.Message}";
            }
        }

        private bool CanTrainPersonGroup(object obj)
        {
            return SelectedPersonGroup != null;
        }

        private async void TrainPersonGroup(object obj)
        {
            try
            {
                await _faceServiceClient.TrainPersonGroupAsync(SelectedPersonGroup.PersonGroupId);

                while(true)
                {
                    TrainingStatus trainingStatus = await _faceServiceClient.GetPersonGroupTrainingStatusAsync(SelectedPersonGroup.PersonGroupId);

                    if(trainingStatus.Status != Microsoft.ProjectOxford.Face.Contract.Status.Running)
                    {
                        StatusText = $"Person group finished with status: {trainingStatus.Status}";
                        break;
                    }

                    StatusText = "Training person group...";
                    await Task.Delay(1000);
                }
            }
            catch(FaceAPIException ex)
            {
                StatusText = $"Failed to train person group: {ex.ErrorMessage}";
            }
            catch(Exception ex)
            {
                StatusText = $"Failed to train person group: {ex.Message}";
            }
        }

        #endregion Person group handling
    }
}
