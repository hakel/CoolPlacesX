using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Alexa.NET.Response;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

/*
 * http://matthiasshapiro.com/2017/02/10/tutorial-alexa-skills-in-c-the-code/
 * http://matthiasshapiro.com/2017/02/10/tutorial-alexa-skills-deployment/
 netdev
Netdev1$ 
 * 
 */
namespace CoolPlaces
{
    public class Function
    {

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            SkillResponse response = new SkillResponse();
            response.Response = new ResponseBody();
            response.Response.ShouldEndSession = false;

            IOutputSpeech innerResponse = null;
            ICard responseCard = null;
            CoolPlace myCoolPlace = null;
            StandardCard myStandardCard = null;
            CardImage myCardImage = null;

            var log = context.Logger;

            log.LogLine($"CoolPlaces Skill Request Object:");
            log.LogLine(JsonConvert.SerializeObject(input));

            // Get what we need to get the zip code from the device
            string deviceID = input.Context.System.Device.DeviceID;
            string token = input.Context.System.ApiAccessToken;
            string apiEndpoint = input.Context.System.ApiEndpoint;
            log.LogLine($"CoolPlaces Skill DeviceID:");
            log.LogLine(deviceID);
            log.LogLine($"CoolPlaces Skill APIAccessToken:");
            log.LogLine(token);
            log.LogLine($"CoolPlaces Skill APIEndpoint:");
            log.LogLine(apiEndpoint);

            string ep1 = "/v1/devices/";
            string ep2 = "/settings/address/countryAndPostalCode";

            string ep3 = ep1 + deviceID + ep2;
            string lookupURL = apiEndpoint + ep3;

            log.LogLine($"CoolPlaces Skill DeviceLocationEndpoint:");
            log.LogLine(lookupURL);

            // this is where we get the location info from alexa.  
            //if it errors out, we know we dont have the permissions yet
            string deviceLocationJSON = Utilities.GetDeviceLocation(token, lookupURL);

            log.LogLine($"CoolPlaces Skill devicelocation result:");
            log.LogLine(deviceLocationJSON);
            //https://developer.amazon.com/docs/custom-skills/device-address-api.html
            //https://developer.amazon.com/blogs/alexa/post/0c975fc7-17dd-4f5c-8343-a37024b66c99/alexa-skill-recipe-using-the-device-address-api-to-request-information
            string defaultZipCode = "45150";

            bool zipFound = false;

            string zipCode = "";

            //  look to see if the location request errored out, then we know we need to ask for permissions
            if(!deviceLocationJSON.Contains("error code:") && !deviceLocationJSON.Contains("error:"))
            {
                zipFound = true;

                // get the zipcode from the json
                log.LogLine($"CoolPlaces Skill devicelocation found!!!!:");
                log.LogLine(deviceLocationJSON);


                zipCode = Utilities.GetDeviceZipCode(log, deviceLocationJSON, defaultZipCode); ;
                log.LogLine($"CoolPlaces Skill zipcode:");
                log.LogLine(zipCode);
            }
            else
            {
                // default the zip code
                zipCode = defaultZipCode;
                log.LogLine($"CoolPlaces Skill devicelocation not found :( ");
                log.LogLine(deviceLocationJSON);
            }


            // variables                                                                  
            string apiKey = "&key=AIzaSyCM0J-Drb3xKzY96XecL7khAfs33zM4Uac";
            Utilities.PlaceType placeType = Utilities.PlaceType.bar;
            int miles = 10;

            // fetch the location to use for the search using the zipcode
            string loct = Utilities.GetLocationDataFromZip(zipCode, apiKey);
            string baseLocation = "location=" + loct;

            var allResources = Utilities.InitCoolPlaces(miles, baseLocation, placeType, apiKey);
            var resource = allResources.FirstOrDefault();

            string responseText = "";

            if (input.GetRequestType() == typeof(LaunchRequest))
            {
                log.LogLine($"CoolPlaces Default LaunchRequest made: 'Alexa, open Cool Places'");

                if (!zipFound)
                {
                    log.LogLine($"CoolPlaces : Need Permissions");

                    innerResponse = new PlainTextOutputSpeech();
                    responseText = "I need your permission for your location to find cool places near you.  Please see your alexa app to grant me this permission.";
                    (innerResponse as PlainTextOutputSpeech).Text = responseText;

                    AskForPermissionsConsentCard permissionCard = new AskForPermissionsConsentCard();
                    permissionCard.Permissions.Add("read::alexa:device:all:address:country_and_postal_code");

                    responseCard = permissionCard;

                }
                else
                {
                    log.LogLine($"CoolPlaces : Dont Need Permissions");
                    log.LogLine($"CoolPlaces LaunchRequest");
                    // ****START
                    myCoolPlace = Utilities.EmitCoolPlace(resource);

                    innerResponse = new PlainTextOutputSpeech();
                    responseText = myCoolPlace.Name;
                    (innerResponse as PlainTextOutputSpeech).Text = resource.GetCoolPlaceOpenMessage + responseText;

                    //SimpleCard mySimpleCard = new SimpleCard();
                    //responseCard = new AskForPermissionsConsentCard();
                    //responseCard = new LinkAccountCard();
                    myStandardCard = new StandardCard();
                    myCardImage = new CardImage();
                    myCardImage.SmallImageUrl = myCoolPlace.Photo.PhotoAPIURL(720, apiKey);
                    myCardImage.LargeImageUrl = myCoolPlace.Photo.PhotoAPIURL(1200, apiKey);
                    myStandardCard.Image = myCardImage;

                    myStandardCard.Content = myCoolPlace.Name + "\r\n" + myCoolPlace.Location + "\r\n Rating " + myCoolPlace.Rating + " out of 5" + "\r\n";
                    myStandardCard.Title = myCoolPlace.Name;
                    responseCard = myStandardCard;
                    // ****END
                }
            }
            else if (input.GetRequestType() == typeof(IntentRequest))
            {
                var intentRequest = (IntentRequest)input.Request;

                switch (intentRequest.Intent.Name)
                {
                    case "AMAZON.CancelIntent":
                        log.LogLine($"CoolPlaces AMAZON.CancelIntent: send StopMessage");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = resource.StopMessage;
                        response.Response.ShouldEndSession = true;
                        break;
                    case "AMAZON.StopIntent":
                        log.LogLine($"CoolPlaces AMAZON.StopIntent: send StopMessage");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = resource.StopMessage;
                        response.Response.ShouldEndSession = true;
                        break;
                    case "AMAZON.HelpIntent":
                        log.LogLine($"CoolPlaces AMAZON.HelpIntent: send HelpMessage");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = resource.HelpMessage;
                        break;
                    case "AMAZON.FallbackIntent":
                        log.LogLine($"CoolPlaces AMAZON.FallbackIntent");
                        // Same as the help
                        //https://developer.amazon.com/docs/custom-skills/standard-built-in-intents.html#amazonfallbackintent-and-dialogdelegate
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = resource.HelpMessage;
                        break;
                    case "AMAZON.NavigateHomeIntent":
                        log.LogLine($"CoolPlaces AMAZON.NavigateHomeIntent");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = resource.StopMessage;
                        response.Response.ShouldEndSession = true;
                        break;
                    case "GetCoolPlaceIntent":
                        log.LogLine($"CoolPlaces GetCoolPlaceIntent sent: send cool place");
                        // ****START
                        myCoolPlace = Utilities.EmitCoolPlace(resource);

                        innerResponse = new PlainTextOutputSpeech();
                        responseText = myCoolPlace.Name;
                        (innerResponse as PlainTextOutputSpeech).Text = resource.GetCoolPlaceOpenMessage + responseText;

                        //SimpleCard mySimpleCard = new SimpleCard();
                        //responseCard = new AskForPermissionsConsentCard();
                        //responseCard = new LinkAccountCard();
                        myStandardCard = new StandardCard();
                        myCardImage = new CardImage();
                        myCardImage.SmallImageUrl = myCoolPlace.Photo.PhotoAPIURL(720, apiKey);
                        myCardImage.LargeImageUrl = myCoolPlace.Photo.PhotoAPIURL(1200, apiKey);
                        myStandardCard.Image = myCardImage;

                        myStandardCard.Content = myCoolPlace.Name + "\r\n" + myCoolPlace.Location + "\r\n Rating " + myCoolPlace.Rating + " out of 5" + "\r\n";
                        myStandardCard.Title = myCoolPlace.Name;
                        responseCard = myStandardCard;
                        // ****END

                        //innerResponse = new PlainTextOutputSpeech();
                        //responseText = Utilities.EmitCoolPlace(resource).Name;
                        //(innerResponse as PlainTextOutputSpeech).Text = responseText;

                        break;
                    case "GetNewCoolPlaceIntent":
                        log.LogLine($"CoolPlaces GetNewCoolPlaceIntent sent: send new cool place");
                        // ****START
                        myCoolPlace = Utilities.EmitCoolPlace(resource);

                        innerResponse = new PlainTextOutputSpeech();
                        responseText = myCoolPlace.Name;
                        (innerResponse as PlainTextOutputSpeech).Text = resource.GetCoolPlaceOpenMessage + responseText;

                        //SimpleCard mySimpleCard = new SimpleCard();
                        //responseCard = new LinkAccountCard();
                        myStandardCard = new StandardCard();
                        myCardImage = new CardImage();
                        myCardImage.SmallImageUrl = myCoolPlace.Photo.PhotoAPIURL(720, apiKey);
                        myCardImage.LargeImageUrl = myCoolPlace.Photo.PhotoAPIURL(1200, apiKey);
                        myStandardCard.Image = myCardImage;

                        myStandardCard.Content = myCoolPlace.Name + "\r\n" + myCoolPlace.Location + "\r\n Rating " + myCoolPlace.Rating + " out of 5" + "\r\n";
                        myStandardCard.Title = myCoolPlace.Name;
                        responseCard = myStandardCard;
                        // ****END


                        //innerResponse = new PlainTextOutputSpeech();
                        //responseText = Utilities.EmitCoolPlace(resource).Name;
                        //(innerResponse as PlainTextOutputSpeech).Text = responseText;


                        break;
                    default:
                        log.LogLine($"CoolPlaces Unknown intent: " + intentRequest.Intent.Name);
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = resource.HelpReprompt;
                        break;
                }
            }

            response.Response.OutputSpeech = innerResponse;
            response.Response.Card = responseCard;
            response.Version = "1.0";
            log.LogLine($"CoolPlaces Skill Response Object...");
            log.LogLine(JsonConvert.SerializeObject(response));
            return response;

        }
    }

    
}