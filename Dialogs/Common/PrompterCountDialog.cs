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
    public static class PrompterCountDialog
    {
        public static  async Task<DialogTurnResult> PromptForHowMany(WaterfallStepContext stepContext, CancellationToken cancellationToken, string dialogId, string promptText)
        {
            var prompt = new PromptOptions
            {
                Style = ListStyle.SuggestedAction,
                Prompt = MessageFactory.Text(promptText),
                Choices = ChoiceFactory.ToChoices(new List<string>
                   {
                        "1","2","3","4","5","6","7","8","9","10"
                   }),
            };

            return await stepContext.PromptAsync(dialogId,
              prompt, cancellationToken);
        }
    }
}
