using Microsoft.Cognitive.LUIS;
using System;
using System.Threading.Tasks;

namespace End_to_End.Model
{
    public class Luis
    {
        public event EventHandler<LuisUtteranceResultEventArgs> OnLuisUtteranceResultUpdated;
        
        private LuisClient _luisClient;

        public Luis(LuisClient luisClient)
        {
            _luisClient = luisClient;
        }

        public async Task RequestAsync(string input)
        {
            try
            {
                LuisResult result = await _luisClient.Predict(input);
                ProcessResult(result);
            }
            catch(Exception ex)
            {
                RaiseOnLuisUtteranceResultUpdated(new LuisUtteranceResultEventArgs { Status = "Failed", Message = ex.Message });
            }
        }

        public async Task ReplyAsync(LuisResult previousResult, string input)
        {
            try
            {
                LuisResult result = await _luisClient.Reply(previousResult, input);
                ProcessResult(result);
            }
            catch(Exception ex)
            {
                RaiseOnLuisUtteranceResultUpdated(new LuisUtteranceResultEventArgs { Status = "Failed", Message = ex.Message });
            }
        }

        private void ProcessResult(LuisResult result)
        {
            LuisUtteranceResultEventArgs args = new LuisUtteranceResultEventArgs();

            args.RequiresReply = !string.IsNullOrEmpty(result.DialogResponse.Prompt);
            args.DialogResponse = !string.IsNullOrEmpty(result.DialogResponse.Prompt) ? result.DialogResponse.Prompt : string.Empty;

            if (result.TopScoringIntent.Actions != null && result.TopScoringIntent.Actions.Length != 0)
            {
                var action = result.TopScoringIntent.Actions[0];
                args.ActionExecuted = action.Triggered;
                args.ActionName = action.Name;

                string actionValue = (action.Parameters[0].ParameterValues != null && action.Parameters[0].ParameterValues.Length != 0) ? action.Parameters[0].ParameterValues[0].Entity : string.Empty;
                args.ActionValue = actionValue;
            }
            else
            {
                args.ActionExecuted = false;
                args.ActionName = "None";

            }

            args.Status = "Succeeded";
            args.Message = $"Top intent is {result.TopScoringIntent.Name} with score {result.TopScoringIntent.Score}. Found {result.Entities.Count} entities.";

            RaiseOnLuisUtteranceResultUpdated(args);
        }

        private void RaiseOnLuisUtteranceResultUpdated(LuisUtteranceResultEventArgs args)
        {
            OnLuisUtteranceResultUpdated?.Invoke(this, args);
        }
    }
    
    public class LuisUtteranceResultEventArgs : EventArgs
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public bool RequiresReply { get; set; }
        public string DialogResponse { get; set; }
        public string ActionName { get; set; }
        public bool ActionExecuted { get; set; }
        public string ActionValue { get; set; }
    }
}