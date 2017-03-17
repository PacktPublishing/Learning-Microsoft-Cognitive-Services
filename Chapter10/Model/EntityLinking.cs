using Microsoft.ProjectOxford.EntityLinking;
using Microsoft.ProjectOxford.EntityLinking.Contract;
using System;
using System.Threading.Tasks;

namespace End_to_End.Model
{
    public class EntityLinking
    {
        public event EventHandler<EntityLinkingErrorEventArgs> EntityLinkingError;

        private EntityLinkingServiceClient _entityLinkingServiceClient;

        public EntityLinking(string apiKey)
        {
            _entityLinkingServiceClient = new EntityLinkingServiceClient(apiKey);
        }

        public async Task<EntityLink[]> LinkEntities(string inputText, string selection = "", int offset = 0)
        {
            try
            {
                EntityLink[] linkingResponse = await _entityLinkingServiceClient.LinkAsync(inputText, selection, offset);

                return linkingResponse;
            }
            catch(Exception ex)
            {
                RaiseOnEntityLinkingError(new EntityLinkingErrorEventArgs(ex.Message));
                return null;
            }
        }

        private void RaiseOnEntityLinkingError(EntityLinkingErrorEventArgs args)
        {
            EntityLinkingError?.Invoke(this, args);
        }
    }

    public class EntityLinkingErrorEventArgs
    {
        public string ErrorMessage { get; private set; }

        public EntityLinkingErrorEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}
