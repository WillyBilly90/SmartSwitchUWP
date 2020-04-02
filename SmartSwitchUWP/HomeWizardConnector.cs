using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Windows.Security.Cryptography;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using System.Net;

namespace SmartSwitchUWP
{
    class HomeWizardConnector
    {
        public List<SmartSwitch> PowerSwitch (string UserName, String Password)
        //UserName = UserName of HomeWizard account
        //Password = Password of HomeWizard account
        //returns a list of switches
        {
            List<SmartSwitch> switchList = new List<SmartSwitch>();
            string sessionId = CreateHomeWizardSession(UserName, Password).GetAwaiter().GetResult();
            JsonArray switches = GetDevicesJson(sessionId).GetAwaiter().GetResult();
            //Find all controllers in account
            int countControllers = switches.Count;
            for (int controllerCounter = 0; controllerCounter <= countControllers - 1; controllerCounter++)
            {
                string controllerString = switches[controllerCounter].ToString();
                //Get controller from string
                JsonObject controller=JsonObject.Parse(controllerString);
                //Find devices on this controller
                JsonArray devicesArray = controller.GetNamedArray("devices");
                int countDevices = devicesArray.Count;
                for (int deviceCounter = 0; deviceCounter <= countDevices - 1; deviceCounter++)
                {
                    string deviceString = devicesArray[deviceCounter].ToString();
                    //Get device from string
                    JsonObject device=JsonObject.Parse(deviceString);
                    //check if device is plugoutlet, other devices are not tested, but can be defined if you like
                    string typeName=device.GetNamedValue("typeName").ToString();
                    typeName=typeName.Replace("\"", "");
                    if (typeName == "plug_outlet")
                    {
                        //Create SmartSwitch to use in this program
                        SmartSwitch tempSwitch=new SmartSwitch();
                        tempSwitch.ControllerId = controller.GetNamedValue("id").ToString();
                        tempSwitch.ControllerId = tempSwitch.ControllerId.Replace("\"", "");
                        tempSwitch.ControllerName = controller.GetNamedValue("name").ToString();
                        tempSwitch.ControllerName = tempSwitch.ControllerName.Replace("\"", "");
                        tempSwitch.DeviceId = device.GetNamedValue("id").ToString();
                        tempSwitch.DeviceId = tempSwitch.DeviceId.Replace("\"", "");
                        tempSwitch.DeviceName = device.GetNamedValue("name").ToString();
                        tempSwitch.DeviceName = tempSwitch.DeviceName.Replace("\"", "");
                        switchList.Add(tempSwitch);
                    }
                }
            }
            return switchList;
        }

        public static async Task<string> CreateHomeWizardSession(string userName, string Password)
        {
            //Open session on homewizard website
            //first set you homewizard credentials
            string passwordSHA1 = SHA1.HashString(Password);
            var http = new HttpClient();
            string credentials = userName + ":" + passwordSHA1;
            var byteArray = Encoding.ASCII.GetBytes(credentials);
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            //Get json from homewizard website
            var json = await http.GetStringAsync("https://cloud.homewizard.com/account/login").ConfigureAwait(false);
            //get sessionId from Json
            JsonObject sessionIdJson = JsonObject.Parse(json);
            var sessionId = sessionIdJson.GetNamedValue("session").ToString();
            //remove quotes from sessionId
            sessionId = sessionId.Replace("\"", "");
            return sessionId;
        }

        public static async Task<JsonArray> GetDevicesJson(string SessionId)
        {
            //Get devices accessable in current session from Homewizard website
            HttpClient httpClient = new HttpClient();
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://plug.homewizard.com/plugs");
            requestMessage.Headers.Add("X-Session-Token", SessionId);
            // Send the request to the server
            HttpResponseMessage response = await httpClient.SendAsync(requestMessage).ConfigureAwait((false));
            // Wait for the answer of the server
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            //return jsonobject from homewizard website
            return await Task.Run(() => JsonArray.Parse(json));
        }

        public void DoAction(string controllerId, string deviceId, string action, string userName, string passWord)
        {
            //create session
            string sessionId = CreateHomeWizardSession(userName, passWord).GetAwaiter().GetResult();
            //create uri where Json string has to be post
            string url = "https://plug.homewizard.com/plugs/" + controllerId + "/devices/" + deviceId + "/action";
            HttpClient httpClient=new HttpClient();
            Uri uri =new Uri(url);
            //define header for webclient
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("X-Session-Token", sessionId);
            //create Json what has to be post to website
            JsonObject _json=new JsonObject();
            JsonValue _action = JsonValue.Parse("\""+action+"\"");
            _json.Add("action",_action);
            HttpContent content = new StringContent(_json.ToString(),Encoding.UTF8,"application/json");
            //start created httpclient and post JSON to website
            var result = httpClient.PostAsync(uri, content);
        }
    }
}