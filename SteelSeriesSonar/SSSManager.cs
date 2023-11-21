using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace SteelSeriesSonar
{
    public class SSSManager
    {
        public static readonly HttpClient client = new HttpClient();
        public static string ggEncryptedAddress = "error";
        public static string sonarWebServerAddress = "error";

        public static async void SonarApiLink()
        {
            // Lecture du fichier de configuration de SteelSeries GG afin de trouver l'adresse d'appel
            try
            {
                // Lecture du fichier pour le passer en JSON
                using (JsonDocument corePropsJSON = JsonDocument.Parse(File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\SteelSeries\SteelSeries Engine 3\coreProps.json")))
                {
                    ggEncryptedAddress = corePropsJSON.RootElement.GetProperty("ggEncryptedAddress").ToString();
                }
                // appel de l'adresse ggEncryptedAddress afin de récuperer l'adresse de Sonar
                bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
                { return true; }
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
                // @"https://" + ggEncryptedAddress + @"/subApps"
                WebClient client = new WebClient();
                try
                {
                    string ggEncryptedAddressReturn = client.DownloadString(@"https://" + ggEncryptedAddress + @"/subApps");
                    using (JsonDocument ggEncryptedAddressJSON = JsonDocument.Parse(ggEncryptedAddressReturn))
                    {
                        JsonElement ggEncryptedAddressJSONData = ggEncryptedAddressJSON.RootElement;
                        if (ggEncryptedAddressJSONData.GetProperty("subApps").GetProperty("sonar").GetProperty("isEnabled").ToString() == "True")
                        {
                            if (ggEncryptedAddressJSONData.GetProperty("subApps").GetProperty("sonar").GetProperty("isReady").ToString() == "True")
                            {
                                if (ggEncryptedAddressJSONData.GetProperty("subApps").GetProperty("sonar").GetProperty("isRunning").ToString() == "True")
                                {
                                    string oldSonarWebServerAddress = sonarWebServerAddress;
                                    sonarWebServerAddress = ggEncryptedAddressJSONData.GetProperty("subApps").GetProperty("sonar").GetProperty("metadata").GetProperty("webServerAddress").ToString() + "/";
                                    if (oldSonarWebServerAddress != sonarWebServerAddress)
                                        Logger.Instance.LogMessage(TracingLevel.INFO, $"sonarWebServerAddress : " + sonarWebServerAddress);
                                }
                                else
                                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"error : Sonar doesn't run");
                            }
                            else
                                Logger.Instance.LogMessage(TracingLevel.ERROR, $"error : Sonar doesn't ready");
                        }
                        else
                            Logger.Instance.LogMessage(TracingLevel.ERROR, $"error : Sonar doesn't enabled");
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"error : " + e);
                }
            }
            catch
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"error : SteelSeries's config file not found");
            }
        }
        public static string GetDataToString(string target)
        {
            HttpResponseMessage response = client.GetAsync(sonarWebServerAddress + target).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                var responseContent = response.Content;
                return responseContent.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            else
                return "error";
        }
        public static JsonElement GetDataToJson(string target)
        {
            string DataString = GetDataToString(target);
            using (JsonDocument DataJSON = JsonDocument.Parse(DataString))
            {
                return DataJSON.RootElement;
            }
        }
        public static string GetDataFromJSON(string target, string content)
        {
            string DataString = GetDataToString(target);
            using (JsonDocument DataJSON = JsonDocument.Parse(DataString))
            {
                JsonElement Data = DataJSON.RootElement;
                return Data.GetProperty(content).ToString();
            }
        }
        public static void PutData(string target)
        {
            HttpResponseMessage response = client.PutAsync(sonarWebServerAddress + target, null).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var responseContent = response.Content;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"PutData(" + target + ") : " + responseContent.ReadAsStringAsync().GetAwaiter().GetResult());
        }
        public static void SwitchMode(string mode = "switch")
        {
            if (mode == "classic")
                PutData("mode/classic");
            else if (mode == "stream")
                PutData("mode/stream");
            else
            {
                if (GetDataToString("mode") == "\"classic\"")
                    PutData("mode/stream");
                else
                    PutData("mode/classic");
            }

        }
        public static string BoolToString(bool input)
        {
            if (input)
                return "true";
            else
                return "false";
        }
        public static SonarGlobalSettings UpdateGlobalSetting(ISDConnection Connection)
        {
            SonarApiLink();
            SonarGlobalSettings global = new SonarGlobalSettings();
            // récupération du mode (Classic ou Stream)
            global.ModeValue = GetDataToString("mode").Replace("\"", "");
            // récupération des données du ChatMix (Volume + Activation)
            global.ChatMixBalance = float.Parse(GetDataFromJSON("chatMix", "balance"), new CultureInfo("en-US"));
            if (GetDataFromJSON("chatMix", "state") == "enabled")
                global.ChatMixEnabled = true;
            else
                global.ChatMixEnabled = false;
            // récupération de l'information du retour vocal
            if (GetDataToString("streamRedirections/isStreamMonitoringEnabled") == "true")
                global.StreamerReturnToMonitoring = true;
            else
                global.StreamerReturnToMonitoring = false;
            // pause d'une second
            Thread.Sleep(50);
            // récupération des informations sur l'envoie des données sur le streaming
            using (JsonDocument DataJSON = JsonDocument.Parse(GetDataToString("streamRedirections/monitoring")))
            {
                JsonElement StreamerRedirectionsData = DataJSON.RootElement;
                for (int i = 0; i < StreamerRedirectionsData.GetArrayLength(); i++)
                {
                    for (int j = 0; j < StreamerRedirectionsData[i].GetProperty("status").GetArrayLength(); j++)
                    {
                        switch (StreamerRedirectionsData[i].GetProperty("streamRedirectionId").ToString() + " : " + StreamerRedirectionsData[i].GetProperty("status")[j].GetProperty("role").ToString())
                        {
                            case "streaming : chatCapture":
                                global.StreamerStreamingMicroSend = StreamerRedirectionsData[i].GetProperty("status")[j].GetProperty("isEnabled").GetBoolean();
                                break;
                            case "streaming : chatRender":
                                global.StreamerStreamingChatSend = StreamerRedirectionsData[i].GetProperty("status")[j].GetProperty("isEnabled").GetBoolean();
                                break;
                            case "streaming : game":
                                global.StreamerStreamingGameSend = StreamerRedirectionsData[i].GetProperty("status")[j].GetProperty("isEnabled").GetBoolean();
                                break;
                            case "streaming : media":
                                global.StreamerStreamingMediaSend = StreamerRedirectionsData[i].GetProperty("status")[j].GetProperty("isEnabled").GetBoolean();
                                break;
                            case "streaming : aux":
                                global.StreamerStreamingAuxSend = StreamerRedirectionsData[i].GetProperty("status")[j].GetProperty("isEnabled").GetBoolean();
                                break;
                            case "monitoring : chatCapture":
                                global.StreamerMonitoringMicroSend = StreamerRedirectionsData[i].GetProperty("status")[j].GetProperty("isEnabled").GetBoolean();
                                break;
                            case "monitoring : chatRender":
                                global.StreamerMonitoringChatSend = StreamerRedirectionsData[i].GetProperty("status")[j].GetProperty("isEnabled").GetBoolean();
                                break;
                            case "monitoring : game":
                                global.StreamerMonitoringGameSend = StreamerRedirectionsData[i].GetProperty("status")[j].GetProperty("isEnabled").GetBoolean();
                                break;
                            case "monitoring : media":
                                global.StreamerMonitoringMediaSend = StreamerRedirectionsData[i].GetProperty("status")[j].GetProperty("isEnabled").GetBoolean();
                                break;
                            case "monitoring : aux":
                                global.StreamerMonitoringAuxSend = StreamerRedirectionsData[i].GetProperty("status")[j].GetProperty("isEnabled").GetBoolean();
                                break;
                        }
                    }
                }
            }
            // récupération des informations sur le mode classic
            using (JsonDocument DataJSON = JsonDocument.Parse(GetDataToString("volumeSettings/classic")))
            {
                JsonElement ClassicData = DataJSON.RootElement;
                // volume
                global.ClassicGameVolume = float.Parse(ClassicData.GetProperty("devices").GetProperty("game").GetProperty("classic").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.ClassicChatVolume = float.Parse(ClassicData.GetProperty("devices").GetProperty("chatRender").GetProperty("classic").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.ClassicMediaVolume = float.Parse(ClassicData.GetProperty("devices").GetProperty("media").GetProperty("classic").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.ClassicAuxVolume = float.Parse(ClassicData.GetProperty("devices").GetProperty("aux").GetProperty("classic").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.ClassicMicroVolume = float.Parse(ClassicData.GetProperty("devices").GetProperty("chatCapture").GetProperty("classic").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.ClassicMasterVolume = float.Parse(ClassicData.GetProperty("masters").GetProperty("classic").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                // mute
                global.ClassicGameMute = ClassicData.GetProperty("devices").GetProperty("game").GetProperty("classic").GetProperty("muted").GetBoolean();
                global.ClassicChatMute = ClassicData.GetProperty("devices").GetProperty("chatRender").GetProperty("classic").GetProperty("muted").GetBoolean();
                global.ClassicMediaMute = ClassicData.GetProperty("devices").GetProperty("media").GetProperty("classic").GetProperty("muted").GetBoolean();
                global.ClassicAuxMute = ClassicData.GetProperty("devices").GetProperty("aux").GetProperty("classic").GetProperty("muted").GetBoolean();
                global.ClassicMicroMute = ClassicData.GetProperty("devices").GetProperty("chatCapture").GetProperty("classic").GetProperty("muted").GetBoolean();
                global.ClassicMasterMute = ClassicData.GetProperty("masters").GetProperty("classic").GetProperty("muted").GetBoolean();
            }
            // récupération des informations sur le mode streamer
            using (JsonDocument DataJSON = JsonDocument.Parse(GetDataToString("volumeSettings/streamer")))
            {
                JsonElement StreamerData = DataJSON.RootElement;
                // monitoring volume
                global.StreamerMonitoringGameVolume = float.Parse(StreamerData.GetProperty("devices").GetProperty("game").GetProperty("stream").GetProperty("monitoring").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.StreamerMonitoringChatVolume = float.Parse(StreamerData.GetProperty("devices").GetProperty("chatRender").GetProperty("stream").GetProperty("monitoring").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.StreamerMonitoringMediaVolume = float.Parse(StreamerData.GetProperty("devices").GetProperty("media").GetProperty("stream").GetProperty("monitoring").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.StreamerMonitoringAuxVolume = float.Parse(StreamerData.GetProperty("devices").GetProperty("aux").GetProperty("stream").GetProperty("monitoring").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.StreamerMonitoringMicroVolume = float.Parse(StreamerData.GetProperty("devices").GetProperty("chatCapture").GetProperty("stream").GetProperty("monitoring").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.StreamerMonitoringMasterVolume = float.Parse(StreamerData.GetProperty("masters").GetProperty("stream").GetProperty("monitoring").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                // monitoring mute
                global.StreamerMonitoringGameMute = StreamerData.GetProperty("devices").GetProperty("game").GetProperty("stream").GetProperty("monitoring").GetProperty("muted").GetBoolean();
                global.StreamerMonitoringChatMute = StreamerData.GetProperty("devices").GetProperty("chatRender").GetProperty("stream").GetProperty("monitoring").GetProperty("muted").GetBoolean();
                global.StreamerMonitoringMediaMute = StreamerData.GetProperty("devices").GetProperty("media").GetProperty("stream").GetProperty("monitoring").GetProperty("muted").GetBoolean();
                global.StreamerMonitoringAuxMute = StreamerData.GetProperty("devices").GetProperty("aux").GetProperty("stream").GetProperty("monitoring").GetProperty("muted").GetBoolean();
                global.StreamerMonitoringMicroMute = StreamerData.GetProperty("devices").GetProperty("chatCapture").GetProperty("stream").GetProperty("monitoring").GetProperty("muted").GetBoolean();
                global.StreamerMonitoringMasterMute = StreamerData.GetProperty("masters").GetProperty("stream").GetProperty("monitoring").GetProperty("muted").GetBoolean();
                // streaming volume
                global.StreamerStreamingGameVolume = float.Parse(StreamerData.GetProperty("devices").GetProperty("game").GetProperty("stream").GetProperty("streaming").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.StreamerStreamingChatVolume = float.Parse(StreamerData.GetProperty("devices").GetProperty("chatRender").GetProperty("stream").GetProperty("streaming").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.StreamerStreamingMediaVolume = float.Parse(StreamerData.GetProperty("devices").GetProperty("media").GetProperty("stream").GetProperty("streaming").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.StreamerStreamingAuxVolume = float.Parse(StreamerData.GetProperty("devices").GetProperty("aux").GetProperty("stream").GetProperty("streaming").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.StreamerStreamingMicroVolume = float.Parse(StreamerData.GetProperty("devices").GetProperty("chatCapture").GetProperty("stream").GetProperty("streaming").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                global.StreamerStreamingMasterVolume = float.Parse(StreamerData.GetProperty("masters").GetProperty("stream").GetProperty("streaming").GetProperty("volume").ToString(), new CultureInfo("en-US"));
                // streaming mute
                global.StreamerStreamingGameMute = StreamerData.GetProperty("devices").GetProperty("game").GetProperty("stream").GetProperty("streaming").GetProperty("muted").GetBoolean();
                global.StreamerStreamingChatMute = StreamerData.GetProperty("devices").GetProperty("chatRender").GetProperty("stream").GetProperty("streaming").GetProperty("muted").GetBoolean();
                global.StreamerStreamingMediaMute = StreamerData.GetProperty("devices").GetProperty("media").GetProperty("stream").GetProperty("streaming").GetProperty("muted").GetBoolean();
                global.StreamerStreamingAuxMute = StreamerData.GetProperty("devices").GetProperty("aux").GetProperty("stream").GetProperty("streaming").GetProperty("muted").GetBoolean();
                global.StreamerStreamingMicroMute = StreamerData.GetProperty("devices").GetProperty("chatCapture").GetProperty("stream").GetProperty("streaming").GetProperty("muted").GetBoolean();
                global.StreamerStreamingMasterMute = StreamerData.GetProperty("masters").GetProperty("stream").GetProperty("streaming").GetProperty("muted").GetBoolean();
            }
            // récupération des deviceId pour le mode classic
            using (JsonDocument DataJSON = JsonDocument.Parse(GetDataToString("classicRedirections")))
            {
                JsonElement classicRedirections = DataJSON.RootElement;
                for (int i = 0; i < classicRedirections.GetArrayLength(); i++)
                {
                    switch (classicRedirections[i].GetProperty("id").ToString())
                    {
                        case "game":
                            global.ClassicGameDeviceId = classicRedirections[i].GetProperty("deviceId").ToString();
                            break;
                        case "chat":
                            global.ClassicChatDeviceId = classicRedirections[i].GetProperty("deviceId").ToString();
                            break;
                        case "media":
                            global.ClassicMediaDeviceId = classicRedirections[i].GetProperty("deviceId").ToString();
                            break;
                        case "aux":
                            global.ClassicAuxDeviceId = classicRedirections[i].GetProperty("deviceId").ToString();
                            break;
                        case "mic":
                            global.ClassicMicroDeviceId = classicRedirections[i].GetProperty("deviceId").ToString();
                            break;
                    }
                }
            }
            // récupération des deviceId pour le mode stream
            using (JsonDocument DataJSON = JsonDocument.Parse(GetDataToString("streamRedirections")))
            {
                JsonElement streamRedirections = DataJSON.RootElement;
                for (int i = 0; i < streamRedirections.GetArrayLength(); i++)
                {
                    switch (streamRedirections[i].GetProperty("streamRedirectionId").ToString())
                    {
                        case "streaming":
                            global.StreamerStreamingDeviceId = streamRedirections[i].GetProperty("deviceId").ToString();
                            break;
                        case "monitoring":
                            global.StreamerMonitoringDeviceId = streamRedirections[i].GetProperty("deviceId").ToString();
                            break;
                        case "mic":
                            global.StreamerMicroDeviceId = streamRedirections[i].GetProperty("deviceId").ToString();
                            break;
                    }
                }
            }
            Connection.SetGlobalSettingsAsync(JObject.FromObject(global));
            return global;
        }
        public static string GenerateURI(SonarGlobalSettings global, typeReturn OutputDevice, string Action, typeRole InputDevice = typeRole.none)
        {
            if (Action == "SetVolume")
            {
                if (global.ModeValue == "classic")
                {
                    switch (InputDevice)
                    {
                        case typeRole.game:
                            return "volumeSettings/classic/game/volume/";
                        case typeRole.chatRender:
                            return "volumeSettings/classic/chatRender/volume/";
                        case typeRole.media:
                            return "volumeSettings/classic/media/volume/";
                        case typeRole.aux:
                            return "volumeSettings/classic/aux/volume/";
                        case typeRole.chatCapture:
                            return "volumeSettings/classic/chatCapture/volume/";
                        default:
                            return "volumeSettings/classic/master/volume/";
                    }
                }
                else
                {
                    if (OutputDevice == typeReturn.Streaming)
                    {
                        switch (InputDevice)
                        {
                            case typeRole.game:
                                return "volumeSettings/streamer/streaming/game/volume/";
                            case typeRole.chatRender:
                                return "volumeSettings/streamer/streaming/chatRender/volume/";
                            case typeRole.media:
                                return "volumeSettings/streamer/streaming/media/volume/";
                            case typeRole.aux:
                                return "volumeSettings/streamer/streaming/aux/volume/";
                            case typeRole.chatCapture:
                                return "volumeSettings/streamer/streaming/chatCapture/volume/";
                            default:
                                return "volumeSettings/streamer/streaming/master/volume/";
                        }
                    }
                    else
                    {
                        switch (InputDevice)
                        {
                            case typeRole.game:
                                return "volumeSettings/streamer/monitoring/game/volume/";
                            case typeRole.chatRender:
                                return "volumeSettings/streamer/monitoring/chatRender/volume/";
                            case typeRole.media:
                                return "volumeSettings/streamer/monitoring/media/volume/";
                            case typeRole.aux:
                                return "volumeSettings/streamer/monitoring/aux/volume/";
                            case typeRole.chatCapture:
                                return "volumeSettings/streamer/monitoring/chatCapture/volume/";
                            default:
                                return "volumeSettings/streamer/monitoring/master/volume/";
                        }
                    }
                }
            }
            else if (Action == "SetMute")
            {
                if (global.ModeValue == "classic")
                {
                    switch (InputDevice)
                    {
                        case typeRole.game:
                            return "volumeSettings/classic/game/Mute/";
                        case typeRole.chatRender:
                            return "volumeSettings/classic/chatRender/Mute/";
                        case typeRole.media:
                            return "volumeSettings/classic/media/Mute/";
                        case typeRole.aux:
                            return "volumeSettings/classic/aux/Mute/";
                        case typeRole.chatCapture:
                            return "volumeSettings/classic/chatCapture/Mute/";
                        default:
                            return "volumeSettings/classic/Master/Mute/";
                    }
                }
                else
                {
                    if (OutputDevice == typeReturn.Streaming)
                    {
                        switch (InputDevice)
                        {
                            case typeRole.game:
                                return "volumeSettings/streamer/streaming/game/isMuted/";
                            case typeRole.chatRender:
                                return "volumeSettings/streamer/streaming/chatRender/isMuted/";
                            case typeRole.media:
                                return "volumeSettings/streamer/streaming/media/isMuted/";
                            case typeRole.aux:
                                return "volumeSettings/streamer/streaming/aux/isMuted/";
                            case typeRole.chatCapture:
                                return "volumeSettings/streamer/streaming/chatCapture/isMuted/";
                            default:
                                return "volumeSettings/streamer/streaming/master/isMuted/";
                        }
                    }
                    else
                    {
                        switch (InputDevice)
                        {
                            case typeRole.game:
                                return "volumeSettings/streamer/monitoring/game/isMuted/";
                            case typeRole.chatRender:
                                return "volumeSettings/streamer/monitoring/chatRender/isMuted/";
                            case typeRole.media:
                                return "volumeSettings/streamer/monitoring/media/isMuted/";
                            case typeRole.aux:
                                return "volumeSettings/streamer/monitoring/aux/isMuted/";
                            case typeRole.chatCapture:
                                return "volumeSettings/streamer/monitoring/chatCapture/isMuted/";
                            default:
                                return "volumeSettings/streamer/monitoring/master/isMuted/";
                        }
                    }
                }
            }
            return "toto";
        }
        public static bool ReturnMute(SonarGlobalSettings global, typeReturn OutputDevice, typeRole InputDevice = typeRole.none)
        {
            if (global.ModeValue == "classic")
            {
                switch (InputDevice)
                {
                    case typeRole.game:
                        return global.ClassicGameMute;
                    case typeRole.chatRender:
                        return global.ClassicChatMute;
                    case typeRole.media:
                        return global.ClassicMediaMute;
                    case typeRole.aux:
                        return global.ClassicAuxMute;
                    case typeRole.chatCapture:
                        return global.ClassicMicroMute;
                    default:
                        return global.ClassicMasterMute;
                }
            }
            else
            {
                if (OutputDevice == typeReturn.Streaming)
                {
                    switch (InputDevice)
                    {
                        case typeRole.game:
                            return global.StreamerStreamingGameMute;
                        case typeRole.chatRender:
                            return global.StreamerStreamingChatMute;
                        case typeRole.media:
                            return global.StreamerStreamingMediaMute;
                        case typeRole.aux:
                            return global.StreamerStreamingAuxMute;
                        case typeRole.chatCapture:
                            return global.StreamerStreamingMicroMute;
                        default:
                            return global.StreamerStreamingMasterMute;
                    }
                }
                else
                {
                    switch (InputDevice)
                    {
                        case typeRole.game:
                            return global.StreamerMonitoringGameMute;
                        case typeRole.chatRender:
                            return global.StreamerMonitoringChatMute;
                        case typeRole.media:
                            return global.StreamerMonitoringMediaMute;
                        case typeRole.aux:
                            return global.StreamerMonitoringAuxMute;
                        case typeRole.chatCapture:
                            return global.StreamerMonitoringMicroMute;
                        default:
                            return global.StreamerStreamingMasterMute;
                    }
                }
            }
        }
        public static double ReturnVolume(SonarGlobalSettings global, typeReturn OutputDevice, typeRole InputDevice = typeRole.none)
        {
            if (OutputDevice == typeReturn.Streaming)
            {
                switch (InputDevice)
                {
                    case typeRole.game:
                        return global.StreamerStreamingGameVolume;
                    case typeRole.chatRender:
                        return global.StreamerStreamingChatVolume;
                    case typeRole.media:
                        return global.StreamerStreamingMediaVolume;
                    case typeRole.aux:
                        return global.StreamerStreamingAuxVolume;
                    case typeRole.chatCapture:
                        return global.StreamerStreamingMicroVolume;
                    default:
                        return global.StreamerStreamingMasterVolume;
                }
            }
            else
            {
                if (global.ModeValue == "classic")
                {
                    switch (InputDevice)
                    {
                        case typeRole.game:
                            return global.ClassicGameVolume;
                        case typeRole.chatRender:
                            return global.ClassicChatVolume;
                        case typeRole.media:
                            return global.ClassicMediaVolume;
                        case typeRole.aux:
                            return global.ClassicAuxVolume;
                        case typeRole.chatCapture:
                            return global.ClassicMicroVolume;
                        default:
                            return global.ClassicMasterVolume;
                    }
                }
                else
                {
                    switch (InputDevice)
                    {
                        case typeRole.game:
                            return global.StreamerMonitoringGameVolume;
                        case typeRole.chatRender:
                            return global.StreamerMonitoringChatVolume;
                        case typeRole.media:
                            return global.StreamerMonitoringMediaVolume;
                        case typeRole.aux:
                            return global.StreamerMonitoringAuxVolume;
                        case typeRole.chatCapture:
                            return global.StreamerMonitoringMicroVolume;
                        default:
                            return global.StreamerStreamingMasterVolume;
                    }
                }
            }
        }
        public static Dictionary<string, List<Device>> ListDevice()
        {
            List<Device> ListRender = new List<Device>();
            List<Device> ListCapture = new List<Device>();

            using (JsonDocument DataJSON = JsonDocument.Parse(GetDataToString("audioDevices")))
            {
                JsonElement audioDevices = DataJSON.RootElement;
                for (int i = 0; i < audioDevices.GetArrayLength(); i++)
                {
                    if (audioDevices[i].GetProperty("dataFlow").ToString() == "render")
                        ListRender.Add(new Device(audioDevices[i].GetProperty("id").ToString(), audioDevices[i].GetProperty("friendlyName").ToString()));
                    else
                        ListCapture.Add(new Device(audioDevices[i].GetProperty("id").ToString(), audioDevices[i].GetProperty("friendlyName").ToString()));
                }
            }
            Dictionary<string, List<Device>> ListDevice = new Dictionary<string, List<Device>>
            {
                { "render", ListRender },
                { "capture", ListCapture }
            };
            return ListDevice;
        }
    }
}
