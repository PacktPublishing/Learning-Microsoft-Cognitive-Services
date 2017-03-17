using Microsoft.ProjectOxford.SpeakerRecognition;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Identification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace End_to_End.Model
{
    public class SpeakerIdentification
    {
        public event EventHandler<SpeakerIdentificationStatusUpdateEventArgs> OnSpeakerIdentificationStatusUpdated;
        public event EventHandler<SpeakerIdentificationErrorEventArgs> OnSpeakerIdentificationError;

        private ISpeakerIdentificationServiceClient _speakerIdentificationClient;

        public SpeakerIdentification(ISpeakerIdentificationServiceClient speakerIdentificationClient)
        {
            _speakerIdentificationClient = speakerIdentificationClient;
        }

        public async void IdentifySpeaker(Stream audioStream, Guid[] speakerIds)
        {
            try
            {
                OperationLocation location = await _speakerIdentificationClient.IdentifyAsync(audioStream, speakerIds);

                if (location == null)
                {
                    RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs("Failed to identify speaker."));
                    return;
                }

                GetIdentificationOperationStatus(location);                
            }
            catch(IdentificationException ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to identify speaker: {ex.Message}"));
            }
            catch (Exception ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to identify speaker: {ex.Message}"));
            }
        }

        public async Task<Guid> CreateSpeakerProfile()
        {
            try
            {
                CreateProfileResponse response = await _speakerIdentificationClient.CreateProfileAsync("en-US");

                if (response == null)
                {
                    RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs("Failed to create speaker profile."));
                    return Guid.Empty;
                }

                return response.ProfileId;
            }
            catch (IdentificationException ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to create speaker profile: {ex.Message}"));

                return Guid.Empty;
            }
            catch (Exception ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to create speaker profile: {ex.Message}"));

                return Guid.Empty;
            }
        }
        
        public async void CreateSpeakerEnrollment(Stream audioStream, Guid profileId)
        {
            try
            {
                OperationLocation location = await _speakerIdentificationClient.EnrollAsync(audioStream, profileId);

                if (location == null)
                {
                    RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs("Failed to start enrollment process."));
                    return;
                }

                GetEnrollmentOperationStatus(location);
            }
            catch (IdentificationException ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to add speaker enrollment: {ex.Message}"));
            }
            catch (Exception ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to add speaker enrollment: {ex.Message}"));
            }
        }

        public async Task<List<Guid>> ListSpeakerProfiles()
        {
            try
            {
                List<Guid> speakerProfiles = new List<Guid>();

                Profile[] profiles = await _speakerIdentificationClient.GetProfilesAsync();

                if (profiles == null || profiles.Length == 0)
                {
                    RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs("No profiles exist"));
                    return null;
                }

                foreach (Profile profile in profiles)
                {
                    speakerProfiles.Add(profile.ProfileId);
                }

                return speakerProfiles;
            }
            catch (IdentificationException ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to retrieve speaker profile list: {ex.Message}"));
                return null;
            }
            catch (Exception ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to retrieve speaker profile list: {ex.Message}"));
                return null;
            }

        }

        public async void GetSpeakerProfile(Guid profileId)
        {
            try
            {
                Profile profile = await _speakerIdentificationClient.GetProfileAsync(profileId);

                if (profile == null)
                {
                    RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"No speaker profile found for ID {profileId}"));
                    return;
                }
            }
            catch (IdentificationException ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to retrieve speaker profile: {ex.Message}"));
            }
            catch (Exception ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to retrieve speaker profile: {ex.Message}"));
            }
        }

        public async void DeleteSpeakerProfile(Guid profileId)
        {
            try
            {
                await _speakerIdentificationClient.DeleteProfileAsync(profileId);
            }
            catch (IdentificationException ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to delete speaker profile: {ex.Message}"));
            }
            catch (Exception ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to delete speaker profile: {ex.Message}"));
            }
        }

        public async void ResetEnrollments(Guid profileId)
        {
            try
            {
                await _speakerIdentificationClient.ResetEnrollmentsAsync(profileId);
            }
            catch (IdentificationException ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to reset enrollments: {ex.Message}"));
            }
            catch (Exception ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to reset enrollments: {ex.Message}"));
            }
        }

        private async void GetIdentificationOperationStatus(OperationLocation location)
        {
            try
            {
                while (true)
                {
                    IdentificationOperation result = await _speakerIdentificationClient.CheckIdentificationStatusAsync(location);

                    if (result.Status != Status.Running)
                    {
                        RaiseOnIdentificationStatusUpdated(new SpeakerIdentificationStatusUpdateEventArgs(result.Status.ToString(),
                            $"Enrollment finished with message: {result.Message}.")
                        { IdentifiedProfile = result.ProcessingResult });

                        break;
                    }

                    RaiseOnIdentificationStatusUpdated(new SpeakerIdentificationStatusUpdateEventArgs(result.Status.ToString(), "Identifying..."));

                    await Task.Delay(1000);
                }
            }
            catch (IdentificationException ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to get operation status: {ex.Message}"));
            }
            catch (Exception ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to get operation status: {ex.Message}"));
            }
        }

        private async void GetEnrollmentOperationStatus(OperationLocation location)
        {
            try
            {
                while(true)
                { 
                    EnrollmentOperation result = await _speakerIdentificationClient.CheckEnrollmentStatusAsync(location);

                    if(result.Status != Status.Running)
                    {
                        RaiseOnIdentificationStatusUpdated(new SpeakerIdentificationStatusUpdateEventArgs(result.Status.ToString(),
                            $"Enrollment finished. Enrollment status: {result.ProcessingResult.EnrollmentStatus.ToString()}"));

                        break;
                    }

                    RaiseOnIdentificationStatusUpdated(new SpeakerIdentificationStatusUpdateEventArgs(result.Status.ToString(), "Enrolling..."));

                    await Task.Delay(1000);
                }
            }
            catch (IdentificationException ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to get operation status: {ex.Message}"));
            }
            catch (Exception ex)
            {
                RaiseOnIdentificationError(new SpeakerIdentificationErrorEventArgs($"Failed to get operation status: {ex.Message}"));
            }
        }

        private void RaiseOnIdentificationStatusUpdated(SpeakerIdentificationStatusUpdateEventArgs args)
        {
            OnSpeakerIdentificationStatusUpdated?.Invoke(this, args);
        }

        private void RaiseOnIdentificationError(SpeakerIdentificationErrorEventArgs args)
        {
            OnSpeakerIdentificationError?.Invoke(this, args);
        }
    }
    
    public class SpeakerIdentificationStatusUpdateEventArgs : EventArgs
    {
        public string Status { get; private set; }
        public string Message { get; private set; }
        public Identification IdentifiedProfile { get; set; }

        public SpeakerIdentificationStatusUpdateEventArgs(string status, string message)
        {
            Status = status;
            Message = message;
        }
    }

    public class SpeakerIdentificationErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; private set; }

        public SpeakerIdentificationErrorEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}
