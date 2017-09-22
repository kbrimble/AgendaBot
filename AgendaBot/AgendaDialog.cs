using System;
using System.Collections.Generic;
using System.EnterpriseServices;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;

namespace AgendaBot
{
    [Serializable]
    [LuisModel("MODEL_ID", "SUB_KEY")]
    public class AgendaDialog : LuisDialog<object>
    {
        private AgendaSearchService _agendaSearchService;

        public AgendaDialog()
        {
            _agendaSearchService = new AgendaSearchService();
        }

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult luisResult)
        {
            await context.PostAsync($"Sorry, I didn't understand \"{luisResult.Query}\".").ConfigureAwait(false);
            await context.PostAsync("Ask my stuff like \"what's on now?\" or \"when is Kristian speaking?\"").ConfigureAwait(false);
            context.Wait(MessageReceived);
        }

        [LuisIntent("now")]
        public async Task TalksHappeningNow(IDialogContext context, LuisResult luisResult)
        {
            await context.PostAsync("Let's see what's on now...").ConfigureAwait(false);
            var talks = _agendaSearchService.Now().ToList();
            await SendReplyForTalks(context, talks).ConfigureAwait(false);
            context.Wait(MessageReceived);
        }

        [LuisIntent("next")]
        public async Task TalksHappeningNext(IDialogContext context, LuisResult luisResult)
        {
            await context.PostAsync("I'll see what's coming up next").ConfigureAwait(false);
            var talks = _agendaSearchService.AfterThis().ToList();
            await SendReplyForTalks(context, talks).ConfigureAwait(false);
            context.Wait(MessageReceived);
        }

        [LuisIntent("by_name")]
        public async Task TalksByName(IDialogContext context, LuisResult luisResult)
        {
            var bestNameEntity = BestEntityForType(luisResult, "talk_name");
            var talkName = bestNameEntity.Entity;
            await context.PostAsync($"Ok, I'll see if I can find \"{talkName}\".").ConfigureAwait(false);
            var talk = _agendaSearchService.WithTitle(talkName);
            await SendReplyForTalks(context, new [] { talk }).ConfigureAwait(false);
            context.Wait(MessageReceived);
        }

        [LuisIntent("by_speaker")]
        public async Task TalksBySpeaker(IDialogContext context, LuisResult luisResult)
        {
            var bestSpeakerEntity = BestEntityForType(luisResult, "speaker_name");
            var speakerName = bestSpeakerEntity.Entity;
            await context.PostAsync($"Talks by {speakerName} huh? Let me have a look...").ConfigureAwait(false);
            var talks = _agendaSearchService.WithSpeaker(speakerName).ToList();
            await SendReplyForTalks(context, talks).ConfigureAwait(false);
            context.Wait(MessageReceived);

        }

        [LuisIntent("at_time")]
        public async Task TalksAtTime(IDialogContext context, LuisResult luisResult)
        {
            var bestTimeEntity = BestEntityForType(luisResult, "builtin.datetime");
            var atTime = GetTimeFromEntity(bestTimeEntity);
            await context.PostAsync($"Checking what's on at {atTime:h:mm}").ConfigureAwait(false);
            var talks = _agendaSearchService.AtTime(atTime).ToList();
            await SendReplyForTalks(context, talks).ConfigureAwait(false);
            context.Wait(MessageReceived);
        }

        private TimeSpan GetTimeFromEntity(EntityRecommendation entity)
        {
            var timeElement = entity.Resolution["time"];
            Regex.Replace(timeElement, @"[^\d:]", "");
            return TimeSpan.Parse(timeElement);
        }

        private static EntityRecommendation BestEntityForType(LuisResult luisResult, string entityType)
            => luisResult.Entities.Where(ent => ent.Type == entityType).OrderByDescending(ord => ord.Score).FirstOrDefault();

        private async Task SendReplyForTalks(IDialogContext context, IList<AgendaTalk> talks)
        {
            if (talks.Any())
            {
                var multipleTalks = talks.Count > 1;
                var talkCards = talks.Select(CardForTalk);
                var reply = context.MakeMessage();
                reply.Text = multipleTalks ? "I've found these talks from the agenda." : "Got it!";
                reply.Attachments = talkCards.Select(card => card.ToAttachment()).ToList();
                await context.PostAsync(reply).ConfigureAwait(false);
            }
            else
            {
                await NoTalksFound(context).ConfigureAwait(false);
            }
        }

        private async Task NoTalksFound(IDialogContext context)
        {
            await context.PostAsync("Sorry, I couldn't find any talks for that.").ConfigureAwait(false);
            await context.PostAsync("Ask my stuff like \"what's on now?\" or \"when is Kristian speaking?\"").ConfigureAwait(false);
        }

        private ThumbnailCard CardForTalk(AgendaTalk talk)
        {
            return new ThumbnailCard
            {
                Title = talk.Title,
                Subtitle = string.Join(", ", talk.Speakers),
                Text = $"{talk.Venue}: {talk.Day} {talk.Start:hh':'mm}-{talk.End:hh':'mm}"
            };
        }
    }
}