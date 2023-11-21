using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using static SteelSeriesSonar.SSSManager;

namespace SteelSeriesSonar
{
    [PluginActionId("com.mrayianalex.sonar.inputvolume")]
    class SonarInputVolumeAction : KeypadBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    OutputDevice = typeReturn.Monitoring,
                    InputDevice = typeRole.game,
                    ActionMute = false,
                    ActionAdjust = false,
                    ActionSet = false,
                    ActionRouting = false,
                    MuteOn = false,
                    MuteSwitch = false,
                    MuteOff = false,
                    Step = DEFAULT_STEP_SIZE,
                    VolumeSet = 0.00,
                    RoutingOn = false,
                    RoutingSwitch = false,
                    RoutingOff = false
                };
                return instance;
            }

            [JsonProperty(PropertyName = "outputDevice")]
            public typeReturn OutputDevice { get; set; }

            [JsonProperty(PropertyName = "inputDevice")]
            public typeRole InputDevice { get; set; }

            [JsonProperty(PropertyName = "muteDevice")]
            public bool ActionMute { get; set; }

            [JsonProperty(PropertyName = "adjustVolume")]
            public bool ActionAdjust { get; set; }

            [JsonProperty(PropertyName = "setVolume")]
            public bool ActionSet { get; set; }

            [JsonProperty(PropertyName = "setRouting")]
            public bool ActionRouting { get; set; }

            [JsonProperty(PropertyName = "mute_on")]
            public bool MuteOn { get; set; }

            [JsonProperty(PropertyName = "mute_switch")]
            public bool MuteSwitch { get; set; }

            [JsonProperty(PropertyName = "mute_off")]
            public bool MuteOff { get; set; }

            [JsonProperty(PropertyName = "step")]
            public double Step { get; set; }

            [JsonProperty(PropertyName = "volumeValue")]
            public double VolumeSet { get; set; }

            [JsonProperty(PropertyName = "routing_on")]
            public bool RoutingOn { get; set; }

            [JsonProperty(PropertyName = "routing_switch")]
            public bool RoutingSwitch { get; set; }

            [JsonProperty(PropertyName = "routing_off")]
            public bool RoutingOff { get; set; }
        }

        #region Private members

        private const double MIN_VALUE = 0;
        private const double MAX_VALUE = 1;
        private const double DEFAULT_STEP_SIZE = 1;

        private readonly PluginSettings settings;
        private readonly string[] DEFAULT_IMAGES = new string[]
        {
            @"Images/volume_minus@2x.png",
            @"Images/volume_set@2x.png",
            @"Images/volume_plus@2x.png",
            @"Images/volume_mute_off@2x.png",
            @"Images/volume_mute_on@2x.png",
            @"Images/volume_mute_switch@2x.png",
            @"Images/volume_mute_switch_off@2x.png",
            @"Images/volume_mute_switch_on@2x.png",
            @"Images/set_micro_mono@2x.png",
            @"Images/set_micro_mute_off@2x.png",
            @"Images/set_micro_mute_switch@2x.png",
            @"Images/set_micro_mute_switch_off@2x.png",
            @"Images/set_micro_mute_switch_on@2x.png",
            @"Images/routing_stream_game@2x.png",
            @"Images/routing_stream_chat@2x.png",
            @"Images/routing_stream_media@2x.png",
            @"Images/routing_stream_aux@2x.png",
            @"Images/routing_stream_micro@2x.png",
            @"Images/routing_stream_game_lock@2x.png",
            @"Images/routing_stream_chat_lock@2x.png",
            @"Images/routing_stream_media_lock@2x.png",
            @"Images/routing_stream_aux_lock@2x.png",
            @"Images/routing_stream_micro_lock@2x.png",
            @"Images/routing_monitoring_game@2x.png",
            @"Images/routing_monitoring_chat@2x.png",
            @"Images/routing_monitoring_media@2x.png",
            @"Images/routing_monitoring_aux@2x.png",
            @"Images/routing_monitoring_micro@2x.png",
            @"Images/routing_monitoring_game_lock@2x.png",
            @"Images/routing_monitoring_chat_lock@2x.png",
            @"Images/routing_monitoring_media_lock@2x.png",
            @"Images/routing_monitoring_aux_lock@2x.png",
            @"Images/routing_monitoring_micro_lock@2x.png"
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

        public SonarInputVolumeAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            if (global.ModeValue == "classic" && settings.OutputDevice == typeReturn.Streaming)
                return;
            else if (global.ModeValue == "classic" && settings.ActionRouting)
                return;
            // action
            if (settings.ActionMute)
            {
                if (settings.MuteSwitch)
                    PutString = GenerateURI(global, settings.OutputDevice, "SetMute", settings.InputDevice) + BoolToString(!ReturnMute(global, settings.OutputDevice, settings.InputDevice));
                else if (settings.MuteOff)
                    PutString = GenerateURI(global, settings.OutputDevice, "SetMute", settings.InputDevice) + "false";
                else if (settings.MuteOn)
                    PutString = GenerateURI(global, settings.OutputDevice, "SetMute", settings.InputDevice) + "true";
            }
            else if (settings.ActionAdjust)
            {
                // calcul de la nouvelle valeur
                double outputValue = ReturnVolume(global, settings.OutputDevice, settings.InputDevice) + settings.Step / 100;
                outputValue = Math.Max(MIN_VALUE, outputValue);
                outputValue = Math.Min(MAX_VALUE, outputValue);
                // affectation de la valeur
                PutString = GenerateURI(global, settings.OutputDevice, "SetVolume", settings.InputDevice) + outputValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (settings.ActionSet)
                PutString = GenerateURI(global, settings.OutputDevice, "SetVolume", settings.InputDevice) + (settings.VolumeSet / 100).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            else if (settings.ActionRouting)
            {
                if (settings.RoutingSwitch)
                    PutString = GenerateURI(global, settings.OutputDevice, "SetRouting", settings.InputDevice) + BoolToString(!ReturnRouting(global, settings.OutputDevice, settings.InputDevice));
                else if (settings.RoutingOff)
                    PutString = GenerateURI(global, settings.OutputDevice, "SetRouting", settings.InputDevice) + "false";
                else if (settings.RoutingOn)
                    PutString = GenerateURI(global, settings.OutputDevice, "SetRouting", settings.InputDevice) + "true";
            }
            // envoie de la maj
            PutData(PutString);
            SetGlobalSettings();
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
                Logger.Instance.LogMessage(TracingLevel.WARN, "SonarInputVolumeAction : Global Settings do not exist!");
                SetGlobalSettings();
            }

            // configuration de l'image
            if (settings.ActionMute && settings.InputDevice == typeRole.chatCapture)
            {
                if (settings.MuteSwitch)
                    if (ReturnMute(global, settings.OutputDevice, settings.InputDevice))
                        PrefetchImages(DEFAULT_IMAGES, 12);
                    else
                        PrefetchImages(DEFAULT_IMAGES, 11);
                else if (settings.MuteOff)
                    PrefetchImages(DEFAULT_IMAGES, 8);
                else if (settings.MuteOn)
                    PrefetchImages(DEFAULT_IMAGES, 9);
            }
            else if (settings.ActionMute && settings.InputDevice != typeRole.chatCapture)
            {
                if (settings.MuteSwitch)
                    if (ReturnMute(global, settings.OutputDevice, settings.InputDevice))
                        PrefetchImages(DEFAULT_IMAGES, 7);
                    else
                        PrefetchImages(DEFAULT_IMAGES, 6);
                else if (settings.MuteOff)
                    PrefetchImages(DEFAULT_IMAGES, 3);
                else if (settings.MuteOn)
                    PrefetchImages(DEFAULT_IMAGES, 4);
            }
            else if (settings.ActionAdjust)
            {
                if (settings.Step >= 0)
                    PrefetchImages(DEFAULT_IMAGES, 2);
                else
                    PrefetchImages(DEFAULT_IMAGES, 0);
            }
            else if (settings.ActionSet)
                PrefetchImages(DEFAULT_IMAGES, 1);
            else if (settings.ActionRouting)
            {
                var delta_img_lock = 0;
                if (global.ModeValue == "classic")
                    delta_img_lock = 5;
                if (settings.OutputDevice == typeReturn.Monitoring)
                    delta_img_lock += 10;
                switch (settings.InputDevice)
                {
                    case typeRole.game:
                        PrefetchImages(DEFAULT_IMAGES, 13 + delta_img_lock);
                        break;
                    case typeRole.chatRender:
                        PrefetchImages(DEFAULT_IMAGES, 14 + delta_img_lock);
                        break;
                    case typeRole.media:
                        PrefetchImages(DEFAULT_IMAGES, 15 + delta_img_lock);
                        break;
                    case typeRole.aux:
                        PrefetchImages(DEFAULT_IMAGES, 16 + delta_img_lock);
                        break;
                    case typeRole.chatCapture:
                        PrefetchImages(DEFAULT_IMAGES, 17 + delta_img_lock);
                        break;
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
        }

        #endregion
    }
}
