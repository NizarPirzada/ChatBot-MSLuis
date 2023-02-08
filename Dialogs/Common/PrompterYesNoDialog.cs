using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Dialogs.Common
{
    public static class PrompterYesNoDialog
    {
        public static  async Task<DialogTurnResult> PromptForYesOrNo(WaterfallStepContext stepContext, CancellationToken cancellationToken, string dialogId, string promptText,bool addOptOut)
        {
            var prompt = new PromptOptions
            {
                Style = ListStyle.SuggestedAction,
                Prompt = MessageFactory.Text(promptText),
                Choices = ChoiceFactory.ToChoices(new List<string>
                   {
                        "👍",
                        "👎"
                   }),
            };

            if (addOptOut)
            {
                prompt.Choices.Add( new Choice() { Value = "Repeat" });
            }

            return await stepContext.PromptAsync(dialogId,
              prompt, cancellationToken);
        }
    }
}
