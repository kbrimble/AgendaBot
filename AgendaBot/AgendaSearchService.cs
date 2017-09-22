using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;

namespace AgendaBot
{
    public interface IAgendaSearchService
    {
        IEnumerable<AgendaTalk> Now();
        IEnumerable<AgendaTalk> AfterThis();
        IEnumerable<AgendaTalk> AtTime(TimeSpan atTime);
        IEnumerable<AgendaTalk> WithSpeaker(string speakerName);
        AgendaTalk WithTitle(string talkTitle);
    }

    public class AgendaSearchService : IAgendaSearchService
    {
        private readonly List<AgendaTalk> _talks;

        public AgendaSearchService()
        {
            var filePath = HttpContext.Current.Server.MapPath("~\\agenda.json");
            _talks = JsonConvert.DeserializeObject<List<AgendaTalk>>(File.ReadAllText(filePath));
        }

        public IEnumerable<AgendaTalk> Now()
            => _talks.Where(talk => talk.Day == DateTimeOffset.Now.DayOfWeek && talk.Start < DateTimeOffset.Now.TimeOfDay && talk.End > DateTimeOffset.Now.TimeOfDay);

        public IEnumerable<AgendaTalk> AfterThis()
        {
            var now = DateTimeOffset.Now;
            var laterTalks = _talks.Where(talk => talk.Day == now.DayOfWeek && talk.Start > now.TimeOfDay).OrderBy(talk => talk.Start);
            var nextTalk = laterTalks.First();
            return laterTalks.Where(talk => talk.Day == nextTalk.Day && talk.Start == nextTalk.Start);
        }

        public IEnumerable<AgendaTalk> AtTime(TimeSpan atTime)
            => _talks.Where(talk => talk.Day == DateTimeOffset.Now.DayOfWeek && talk.Start <= atTime && talk.End >= atTime);

        public IEnumerable<AgendaTalk> WithSpeaker(string speakerName)
            => _talks.Where(talk => talk.Speakers.Any(spk => Regex.IsMatch(spk, $"*{speakerName}*", RegexOptions.IgnoreCase)));

        public AgendaTalk WithTitle(string talkTitle)
            => _talks.FirstOrDefault(talk => string.Equals(talk.Title, talkTitle, StringComparison.InvariantCultureIgnoreCase));
    }
}