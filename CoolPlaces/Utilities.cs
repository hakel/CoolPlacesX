using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using Amazon.Lambda.Core;

namespace CoolPlaces
{
    public class Utilities
    {
        public enum PlaceType { bar, night_club, restaurant };
        /// <summary>
        /// Set some basic variables and fetch our places data
        /// </summary>
        /// <param name="miles"></param>
        /// <param name="baseLocation"></param>
        /// <param name="placeType"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static List<CoolPlaceResource> InitCoolPlaces(int miles, string baseLocation, PlaceType placeType, string apiKey)
        {
            List<CoolPlaceResource> resources = new List<CoolPlaceResource>();
            // TODO - i feel like this should be in our main function instead of this utility class
            CoolPlaceResource enUSResource = new CoolPlaceResource("en-US");
            enUSResource.SkillName = "Cool Places";
            enUSResource.GetCoolPlaceOpenMessage = "Here's your Cool Place: ";
            enUSResource.HelpMessage = "You can say tell me a cool place, or, you can say exit... What can I help you with?";
            enUSResource.HelpReprompt = "You can say tell me a cool place to start";
            enUSResource.StopMessage = "Goodbye!";

            // Fetch the data
            enUSResource.CoolPlaces = FetchCoolPlaces(miles, baseLocation, placeType, apiKey);

            resources.Add(enUSResource);

            return resources;
        }

        /// <summary>
        /// Call the google api to get the places
        /// </summary>
        /// <param name="miles"></param>
        /// <param name="baseLocation"></param>
        /// <param name="placeType"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static List<CoolPlace> FetchCoolPlaces(int miles, string baseLocation, PlaceType placeType, string apiKey)
        {
            List<CoolPlace> coolPlaces = new List<CoolPlace>();
            //https://developers.google.com/places/web-service/search#find-place-examples

            string baseUrl = "https://maps.googleapis.com/maps/api/place/";
            //string apiKey = "&key=AIzaSyCM0J-Drb3xKzY96XecL7khAfs33zM4Uac";
            string outputType = "/json?";

            //int miles = 10;
            int metersPerMile = 1609;
            int radius = miles * metersPerMile;

            string searchFunction = "nearbysearch";
            // string baseLocation = "location=-33.8670522,151.1957362";
            string radiusParm = "&radius=" + radius.ToString();

            //string searchParms = "&types=food";
            //string searchParms = "&type=restaurant";
            string searchParms = "&type=" + placeType.ToString();

            string parms = baseLocation + radiusParm + searchParms;

            //The 'using' will help to prevent memory leaks.
            //Create a new instance of HttpClient
            string apiCall = baseUrl + searchFunction + outputType + parms + apiKey;

            while (apiCall != string.Empty)
            {

                using (HttpClient client = new HttpClient())
                {
                    var response = client.GetAsync(apiCall).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content;

                        // by calling .Result you are synchronously reading the result
                        string responseString = responseContent.ReadAsStringAsync().Result;

                        dynamic responseData = JsonConvert.DeserializeObject(responseString);

                        int docCount = (int)responseData.results.Count;

                        for (int i = 0; i < docCount; i++)
                        {
                            CoolPlace place = new CoolPlace();
                            place.Name = responseData.results[i].name;
                            place.ID = responseData.results[i].id;
                            place.PlaceID = responseData.results[i].place_id;
                            place.Icon = responseData.results[i].icon;
                            place.Rating = responseData.results[i].rating;
                            place.Location = responseData.results[i].vicinity;
                            if (responseData.results[i].photos.Count > 0)
                            {
                                CoolPlacePhoto photo = new CoolPlacePhoto();
                                photo.PhotoReference = responseData.results[i].photos[0].photo_reference;
                                photo.Height = responseData.results[i].photos[0].height;
                                photo.Width = responseData.results[i].photos[0].width;
                                place.Photo = photo;
                            }

                            coolPlaces.Add(place);
                        }

                        // see if we need to get more results
                        string pagetoken = responseData.next_page_token;
                        apiCall = string.Empty;
                        if (pagetoken != null)
                        {
                            string parmsNext = "pagetoken=" + pagetoken;
                            apiCall = baseUrl + searchFunction + outputType + parmsNext + apiKey;
                        }
                    }
                }
            }

            return coolPlaces;

        }

        /// <summary>
        /// This does the randomization of the places we have fetched
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static CoolPlace EmitCoolPlace(CoolPlaceResource resource)
        {
            Random r = new Random();
            return resource.CoolPlaces[r.Next(resource.CoolPlaces.Count)];
        }
        /// <summary>
        /// This utility convers a zip code into lat/long info, to be used by the google api
        /// </summary>
        /// <param name="zipCode"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static string GetLocationDataFromZip(string zipCode, string apiKey)
        {

            string baseUrl = "https://maps.googleapis.com/maps/api/geocode";
            //string apiKey = "&key=AIzaSyCM0J-Drb3xKzY96XecL7khAfs33zM4Uac";
            string outputType = "/json?";

            string searchParms = "address=" + zipCode;

            string location = "";

            //The 'using' will help to prevent memory leaks.
            //Create a new instance of HttpClient
            string apiCall = baseUrl + outputType + searchParms + apiKey;

            using (HttpClient client = new HttpClient())
            {
                var response = client.GetAsync(apiCall).Result;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content;

                    // by calling .Result you are synchronously reading the result
                    string responseString = responseContent.ReadAsStringAsync().Result;

                    dynamic responseData = JsonConvert.DeserializeObject(responseString);

                    int docCount = (int)responseData.results.Count;

                    for (int i = 0; i < docCount; i++)
                    {
                        string lat = responseData.results[i].geometry.location.lat;
                        string lng = responseData.results[i].geometry.location.lng;
                        location = lat + "," + lng;
                    }
                }
            }
            return location;
        }

        /*
        public static string GetDeviceLocation2(string apiAccessToken, string url)
        {
            //TODO - make this look like the other things
            string response = "";

            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiAccessToken);
                client.DefaultRequestHeaders.Add("User-Agent", "Request-Promise");
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                response = client.GetAsync(url).Result.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                response = "error: " + ex.Message;
            }


            return response;

        }
        */

        public static string GetDeviceZipCode(ILambdaLogger log,string json, string defaultZipCode)
        {
            // parse out the zip code and return it
            /*
                {
                "countryCode" : "US",
                "postalCode" : "98109"
                }
            */

            log.LogLine($"CoolPlaces Skill location json:");
            log.LogLine(json);

            string zipCode = defaultZipCode;

            dynamic responseData = JsonConvert.DeserializeObject(json);
            
            zipCode = responseData.postalCode.ToString();

            log.LogLine($"CoolPlaces Skill location zip:");
            log.LogLine(zipCode);

            return zipCode;
        }

        public static string GetDeviceLocation(string apiAccessToken, string url)
        {

            string responsex = "";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Clear();

                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiAccessToken);
                    //TODO - experiment, do we need all of these?
                    client.DefaultRequestHeaders.Add("User-Agent", "Request-Promise");
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = client.GetAsync(url).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        HttpContent responseContent = response.Content;

                        // by calling .Result you are synchronously reading the result
                        string responseString = responseContent.ReadAsStringAsync().Result;

                        responsex = responseString;
                    }
                    else
                    {
                        responsex = "error code: " + response.StatusCode.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                responsex = "error: " + ex.Message;
            }

            return responsex;

        }

    }
}
