using System;
using System.Collections.Generic;
using System.Text;

namespace CoolPlaces
{
    public class CoolPlaceResource
    {
        public CoolPlaceResource(string language)
        {
            this.Language = language;
            this.CoolPlaces = new List<CoolPlace>();
        }

        public string Language { get; set; }
        public string SkillName { get; set; }
        public List<CoolPlace> CoolPlaces { get; set; }
        public string GetCoolPlaceOpenMessage { get; set; }
        public string HelpMessage { get; set; }
        public string HelpReprompt { get; set; }
        public string StopMessage { get; set; }
    }


}
