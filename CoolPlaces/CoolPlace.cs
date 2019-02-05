using System;
using System.Collections.Generic;
using System.Text;

namespace CoolPlaces
{
    public class CoolPlace
    {
        public string ID { get; set; }//"id"
        public string PlaceID { get; set; }//"place_id" 
        public string Name { get; set; }//"name"
        public string Icon { get; set; }//"icon"
        public string Rating { get; set; }//"rating"
        public string Location { get; set; }//"vicinity"
        public CoolPlacePhoto Photo { get; set; }

    }
}
