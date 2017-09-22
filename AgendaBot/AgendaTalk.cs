using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AgendaBot
{
    [Serializable]
    public class AgendaTalk
    {
        public string Title { get; set; }
        public string Venue { get; set; }
        public List<string> Speakers { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
    }
}