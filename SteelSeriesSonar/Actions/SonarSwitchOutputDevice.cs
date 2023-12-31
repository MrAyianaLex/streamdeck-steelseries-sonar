﻿using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using static SteelSeriesSonar.SSSManager;

namespace SteelSeriesSonar
{
    [PluginActionId("com.mrayianalex.sonar.switchoutputdevice")]
    class SonarSwitchOutputDeviceAction : KeypadBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    ModeTarget = ActionEnum.Switch,
                    InputDeviceClassic = typeRole.game,
                    InputDeviceStream = typeReturn.Monitoring,
                    ActionSet = true,
                    ActionSwitch = false,
                    DevicesSet = null,
                    DeviceSet = string.Empty,
                    TypeDeviceHeadphone = false,
                    TypeDeviceSpeaker = false,
                    DevicesOne = null,
                    DeviceOne = string.Empty,
                    DevicesTwo = null,
                    DeviceTwo = string.Empty
                };
                return instance;
            }

            [JsonProperty(PropertyName = "modeTarget")]
            public ActionEnum ModeTarget { get; set; }

            [JsonProperty(PropertyName = "inputDeviceClassic")]
            public typeRole InputDeviceClassic { get; set; }

            [JsonProperty(PropertyName = "inputDeviceStream")]
            public typeReturn InputDeviceStream { get; set; }

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

            [JsonProperty(PropertyName = "typeDeviceHeadphone")]
            public bool TypeDeviceHeadphone { get; set; }

            [JsonProperty(PropertyName = "typeDeviceSpeaker")]
            public bool TypeDeviceSpeaker { get; set; }
        }

        #region Private members

        private readonly PluginSettings settings;
        private readonly string[] DEFAULT_IMAGES = new string[]
        {
            @"Images/set_monitoring_headphones_match@2x.png",
            @"Images/set_monitoring_headphones_mono@2x.png",
            @"Images/set_monitoring_headphones_lock@2x.png",
            @"Images/set_monitoring_speaker_match@2x.png",
            @"Images/set_monitoring_speaker_mono@2x.png",
            @"Images/set_monitoring_speaker_lock@2x.png",
            @"Images/switch_monitoring_match1@2x.png",
            @"Images/switch_monitoring_match2@2x.png",
            @"Images/switch_monitoring_mono@2x.png",
            @"Images/switch_monitoring_lock@2x.png"
        };
        private string mainImageStr;
        private int imageDeclage = 0;
        private bool didSetNotConnected = false;

        private bool dialWasRotated = false;
        private string PutString = "";
        private string tmpDeviceId = "";

        // ajout test globalSettings
        private SonarGlobalSettings global = null;
        private bool globalSettingsLoaded = false;

        #endregion

        #region Public Methods

        public SonarSwitchOutputDeviceAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            if (global.ModeValue == "classic" && settings.ModeTarget == ActionEnum.Stream)
                return;
            PutString = "";
            tmpDeviceId = "";
            if (global.ModeValue == "classic")
            {
                switch (settings.InputDeviceClassic)
                {
                    case typeRole.game:
                        PutString = "classicRedirections/game/deviceId/";
                        tmpDeviceId = global.ClassicGameDeviceId;
                        break;
                    case typeRole.chatRender:
                        PutString = "classicRedirections/chat/deviceId/";
                        tmpDeviceId = global.ClassicChatDeviceId;
                        break;
                    case typeRole.media:
                        PutString = "classicRedirections/media/deviceId/";
                        tmpDeviceId = global.ClassicMediaDeviceId;
                        break;
                    case typeRole.aux:
                        PutString = "classicRedirections/aux/deviceId/";
                        tmpDeviceId = global.ClassicAuxDeviceId;
                        break;
                }
            }
            else
            {
                switch (settings.InputDeviceStream)
                {
                    case typeReturn.Monitoring:
                        PutString = "streamRedirections/monitoring/deviceId/";
                        tmpDeviceId = global.StreamerMonitoringDeviceId;
                        break;
                    case typeReturn.Streaming:
                        PutString = "streamRedirections/streaming/deviceId/";
                        tmpDeviceId = global.StreamerStreamingDeviceId;
                        break;
                }
            }
            if (settings.ActionSet)
            {
                PutString += settings.DeviceSet;
            }
            else if (settings.ActionSwitch)
            {
                if (tmpDeviceId == settings.DeviceTwo)
                    PutString += settings.DeviceOne;
                else
                    PutString += settings.DeviceTwo;
            }
            // envoie de la maj
            if (PutString != "" && tmpDeviceId != "")
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
                Logger.Instance.LogMessage(TracingLevel.WARN, "SonarSwitchOutputDeviceAction : Global Settings do not exist!");
                SetGlobalSettings();
            }
            // configuration de l'image
            tmpDeviceId = "";
            if (settings.ModeTarget == ActionEnum.Stream && global.ModeValue == "classic")
            {
                if (settings.ActionSet && settings.TypeDeviceHeadphone)
                    PrefetchImages(DEFAULT_IMAGES, 2);
                else if (settings.ActionSet && settings.TypeDeviceSpeaker)
                    PrefetchImages(DEFAULT_IMAGES, 5);
                else if (settings.ActionSwitch)
                    PrefetchImages(DEFAULT_IMAGES, 9);
            }
            else if (settings.ModeTarget == ActionEnum.Classic && global.ModeValue == "stream")
            {
                if (settings.ActionSet && settings.TypeDeviceHeadphone)
                    PrefetchImages(DEFAULT_IMAGES, 2);
                else if (settings.ActionSet && settings.TypeDeviceSpeaker)
                    PrefetchImages(DEFAULT_IMAGES, 5);
                else if (settings.ActionSwitch)
                    PrefetchImages(DEFAULT_IMAGES, 9);
            }
            else
            {
                if (global.ModeValue == "classic")
                {
                    switch (settings.InputDeviceClassic)
                    {
                        case typeRole.game:
                            tmpDeviceId = global.ClassicGameDeviceId;
                            break;
                        case typeRole.chatRender:
                            tmpDeviceId = global.ClassicChatDeviceId;
                            break;
                        case typeRole.media:
                            tmpDeviceId = global.ClassicMediaDeviceId;
                            break;
                        case typeRole.aux:
                            tmpDeviceId = global.ClassicAuxDeviceId;
                            break;
                    }
                }
                else
                {
                    switch (settings.InputDeviceStream)
                    {
                        case typeReturn.Monitoring:
                            tmpDeviceId = global.StreamerMonitoringDeviceId;
                            break;
                        case typeReturn.Streaming:
                            tmpDeviceId = global.StreamerStreamingDeviceId;
                            break;
                    }
                }
                if (settings.ActionSet)
                {
                    if (settings.TypeDeviceHeadphone)
                        imageDeclage = 0;
                    else if (settings.TypeDeviceSpeaker)
                        imageDeclage = 3;

                    if (tmpDeviceId == settings.DeviceSet)
                        PrefetchImages(DEFAULT_IMAGES, 0 + imageDeclage);
                    else
                        PrefetchImages(DEFAULT_IMAGES, 1 + imageDeclage);
                }
                else if (settings.ActionSwitch)
                {
                    if (tmpDeviceId == settings.DeviceOne)
                        PrefetchImages(DEFAULT_IMAGES, 6);
                    else if (tmpDeviceId == settings.DeviceTwo)
                        PrefetchImages(DEFAULT_IMAGES, 7);
                    else
                        PrefetchImages(DEFAULT_IMAGES, 8);
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
            settings.DevicesSet = settings.DevicesOne = settings.DevicesTwo = ListDevice()["render"];

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
