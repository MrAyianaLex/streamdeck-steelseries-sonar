using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using static SteelSeriesSonar.SSSManager;

namespace SteelSeriesSonar
{
    [PluginActionId("com.mrayianalex.sonar.outputvolume")]
    class SonarOutputVolumeAction : KeypadBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    OutputDevice = typeReturn.Monitoring,
                    ActionMute = false,
                    ActionAdjust = false,
                    ActionSet = false,
                    MuteOn = false,
                    MuteSwitch = false,
                    MuteOff = false,
                    Step = DEFAULT_STEP_SIZE,
                    VolumeSet = 0.00
                };
                return instance;
            }

            [JsonProperty(PropertyName = "outputDevice")]
            public typeReturn OutputDevice { get; set; }

            [JsonProperty(PropertyName = "muteDevice")]
            public bool ActionMute { get; set; }

            [JsonProperty(PropertyName = "adjustVolume")]
            public bool ActionAdjust { get; set; }

            [JsonProperty(PropertyName = "setVolume")]
            public bool ActionSet { get; set; }

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
            @"Images/volume_mute_switch_on@2x.png"
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

        public SonarOutputVolumeAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            // action
            if (settings.ActionMute)
            {
                if (settings.MuteSwitch)
                    PutString = GenerateURI(global, settings.OutputDevice, "SetMute") + BoolToString(!ReturnMute(global, settings.OutputDevice));
                else if (settings.MuteOff)
                    PutString = GenerateURI(global, settings.OutputDevice, "SetMute") + "false";
                else if (settings.MuteOn)
                    PutString = GenerateURI(global, settings.OutputDevice, "SetMute") + "true";
            }
            else if (settings.ActionAdjust)
            {
                // calcul de la nouvelle valeur
                double outputValue = ReturnVolume(global, settings.OutputDevice) + settings.Step / 100;
                outputValue = Math.Max(MIN_VALUE, outputValue);
                outputValue = Math.Min(MAX_VALUE, outputValue);
                // affectation de la valeur
                PutString = GenerateURI(global, settings.OutputDevice, "SetVolume") + outputValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (settings.ActionSet)
                PutString = GenerateURI(global, settings.OutputDevice, "SetVolume") + (settings.VolumeSet / 100).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
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
                Logger.Instance.LogMessage(TracingLevel.WARN, "SonarOutputVolumeAction : Global Settings do not exist!");
                SetGlobalSettings();
            }

            // configuration de l'image
            if (settings.ActionMute)
            {
                if (settings.MuteSwitch)
                    if (ReturnMute(global, settings.OutputDevice))
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
