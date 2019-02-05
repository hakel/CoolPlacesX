using System;
using System.Collections.Generic;
using System.Text;

namespace CoolPlaces
{
    public class CoolPlacePhoto
    {
        //public string HTMLAttributes { get; set; }//"html_attributions"
        public string Height { get; set; }//"height"
        public string Width { get; set; }//"width"
        public string PhotoReference { get; set; }//"photo_reference"
        public string PhotoAPIURL(int picWidth, string apiKey)
        {
            string APIURL = "";
            string baseURL = "https://maps.googleapis.com/maps/api/place/photo";
            APIURL = baseURL + "?maxwidth=" + picWidth.ToString() + "&photoreference=" + PhotoReference + apiKey;
            return APIURL;
        }
    }
}
