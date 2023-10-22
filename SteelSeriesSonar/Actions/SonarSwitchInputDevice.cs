using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using static SteelSeriesSonar.SSSManager;

namespace SteelSeriesSonar
{
    [PluginActionId("com.mrayianalex.sonar.switchinputdevice")]
    class SonarSwitchInputDeviceAction : KeypadBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    ActionSet = true,
                    ActionSwitch = false,
                    DevicesSet = null,
                    DeviceSet = string.Empty,
                    DevicesOne = null,
                    DeviceOne = string.Empty,
                    DevicesTwo = null,
                    DeviceTwo = string.Empty
                };
                return instance;
            }

            [JsonProperty(PropertyName = "actionSet")]
            public bool ActionSet { get; set; }

            [JsonProperty(PropertyName = "actionSwitch")]
            public bool ActionSwitch { get; set; }

            [JsonProperty(PropertyName = "devicesSet")]
            public List<Device> DevicesSet { get; set; }

            [JsonProperty(PropertyName = "deviceSet")]
            public string DeviceSet { get; set; }

            [JsonProperty(PropertyName = "devicesOne")]
            public List<Device> DevicesOne { get; set; }

            [JsonProperty(PropertyName = "deviceOne")]
            public string DeviceOne { get; set; }

            [JsonProperty(PropertyName = "devicesTwo")]
            public List<Device> DevicesTwo { get; set; }

            [JsonProperty(PropertyName = "deviceTwo")]
            public string DeviceTwo { get; set; }
        }

        #region Private members

        private readonly PluginSettings settings;
        private readonly string[] DEFAULT_IMAGES = new string[]
        {
            @"Images/set_micro_match@2x.png",
            @"Images/set_micro_mono@2x.png",
            @"Images/switch_micro_match1@2x.png",
            @"Images/switch_micro_match2@2x.png",
            @"Images/switch_micro_mono@2x.png"
        };
        private string mainImageStr;
        private bool didSetNotConnected = false;

        private bool dialWasRotated = false;
        private string PutString = "";

        // ajout test globalSettings
        private SonarGlobalSettings global = null;
        private bool globalSettingsLoaded = false;

        #endregion

        #region Public Methods

        public SonarSwitchInputDeviceAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                Connection.SetSettingsAsync(JObject.FromObject(settings));
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
            InitializeSettings();
            Connection.GetGlobalSettingsAsync();
        }

        #endregion

        #region IPluginable

        public async override void KeyPressed(KeyPayload payload)
        {
            SonarApiLink();
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
            if (sonarWebServerAddress == "error")
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Key pressed but Sonar is not connected!");
                await Connection.ShowAlert();
                return;
            }
            PutString = "";
            if (settings.ActionSet)
            {
                if (global.ModeValue == "classic")
                    PutString = "classicRedirections/chatCapture/deviceId/" + settings.DeviceSet;
                else
                    PutString = "streamRedirections/mic/deviceId/" + settings.DeviceSet;
            }
            else if (settings.ActionSwitch)
            {
                if (global.ModeValue == "classic")
                {
                    if (global.ClassicMicroDeviceId == settings.DeviceTwo)
                        PutString = "classicRedirections/chatCapture/deviceId/" + settings.DeviceOne;
                    else
                        PutString = "classicRedirections/chatCapture/deviceId/" + settings.DeviceTwo;
                }
                else
                {
                    if (global.StreamerMicroDeviceId == settings.DeviceTwo)
                        PutString = "streamRedirections/mic/deviceId/" + settings.DeviceOne;
                    else
                        PutString = "streamRedirections/mic/deviceId/" + settings.DeviceTwo;
                }
            }
            // envoie de la maj
            if (PutString != "")
            {
                PutData(PutString);
                SetGlobalSettings();
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick() { }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }

        public async override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            InitializeSettings();
            // Used to return the correct filename back to the Property Inspector
            await SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            globalSettingsLoaded = true;
            // Global Settings exist
            if (payload?.Settings != null && payload.Settings.Count > 0)
            {
                global = payload.Settings.ToObject<SonarGlobalSettings>();
            }
            else // Global settings do not exist
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "SonarSwitchInputDeviceAction : Global Settings do not exist!");
                SetGlobalSettings();
            }
            // configuration de l'image
            if (settings.ActionSet)
            {
                if ((global.ModeValue == "classic" && settings.DeviceSet == global.ClassicMicroDeviceId) || (global.ModeValue == "stream" && settings.DeviceSet == global.StreamerMicroDeviceId))
                    PrefetchImages(DEFAULT_IMAGES, 0);
                else
                    PrefetchImages(DEFAULT_IMAGES, 1);
            }
            else if (settings.ActionSwitch)
            {
                if (global.ModeValue == "classic")
                {
                    if (global.ClassicMicroDeviceId == settings.DeviceOne)
                        PrefetchImages(DEFAULT_IMAGES, 2);
                    else if (global.ClassicMicroDeviceId == settings.DeviceTwo)
                        PrefetchImages(DEFAULT_IMAGES, 3);
                    else
                        PrefetchImages(DEFAULT_IMAGES, 4);
                }
                else
                {
                    if (global.StreamerMicroDeviceId == settings.DeviceOne)
                        PrefetchImages(DEFAULT_IMAGES, 2);
                    else if (global.StreamerMicroDeviceId == settings.DeviceTwo)
                        PrefetchImages(DEFAULT_IMAGES, 3);
                    else
                        PrefetchImages(DEFAULT_IMAGES, 4);
                }
            }
            Connection.SetImageAsync(mainImageStr);
        }

        private bool SetGlobalSettings()
        {
            if (!globalSettingsLoaded)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Ignoring SetGlobalSettings as they were not yet loaded");
                return false;
            }

            if (global == null)
                Logger.Instance.LogMessage(TracingLevel.WARN, "SetGlobalSettings called while Global Settings are null");

            global = UpdateGlobalSetting(Connection);

            return true;
        }

        #endregion

        #region Private Methods
        private void PrefetchImages(string[] defaultImages, int image = 0)
        {
            if (defaultImages.Length < 1)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"{this.GetType()} PrefetchImages: Invalid default images list");
                return;
            }

            mainImageStr = Tools.ImageToBase64(Image.FromFile(defaultImages[image]), true);
        }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private void InitializeSettings()
        {
            SaveSettings();
            SetGlobalSettings();
            FetchDevices();
        }

        private async void FetchDevices()
        {
            SonarApiLink();
            if (sonarWebServerAddress == "error")
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Sonar is not connected!");
                return;
            }
            settings.DevicesSet = settings.DevicesOne = settings.DevicesTwo = ListDevice()["capture"];

            if (settings.DevicesSet == null)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"{GetType()} GetAllPlaybackDevices called but returned null");
                return;
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType()} FetchPlaybackDevices returned {settings.DevicesSet.Count} captures devices");
            //settings.Devices.Insert(0, new DeviceEndpoint(DEFAULT_PLAYBACK_DEVICE_NAME));
            await SaveSettings();
        }

        private void Connection_OnSendToPlugin(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.SendToPlugin> e)
        {
            var payload = e.Event.Payload;

            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLowerInvariant())
                {
                    case "refreshDevice":
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType()} refreshApplications called");
                        FetchDevices();
                        break;
                }
            }
        }

        private void Connection_OnPropertyInspectorDidAppear(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.PropertyInspectorDidAppear> e)
        {
            FetchDevices();
        }

        #endregion
    }
}
