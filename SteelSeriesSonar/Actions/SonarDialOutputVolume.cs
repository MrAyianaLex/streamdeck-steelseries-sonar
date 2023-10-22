using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Text.Json;
using static SteelSeriesSonar.SSSManager;

namespace SteelSeriesSonar
{
    [PluginActionId("com.mrayianalex.sonar.dialoutputvolume")]
    class SonarDialOutputVolumeAction : EncoderBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    Step = DEFAULT_STEP_SIZE,
                    OutputDevice = typeReturn.Monitoring
                };
                return instance;
            }

            [JsonProperty(PropertyName = "step")]
            public double Step { get; set; }

            [JsonProperty(PropertyName = "outputDevice")]
            public typeReturn OutputDevice { get; set; }
        }

        #region Private members

        private const double MIN_VALUE = 0;
        private const double MAX_VALUE = 1;
        private const double DEFAULT_STEP_SIZE = 1;

        private readonly PluginSettings settings;
        private readonly string[] DEFAULT_IMAGES = new string[]
        {
            @"Images/dial_monitoring@2x.png",
            @"Images/dial_monitoring_mute@2x.png",
            @"Images/dial_streaming@2x.png",
            @"Images/dial_streaming_mute@2x.png",
            @"Images/dial_streaming_lock@2x.png"
        };
        private string mainImageStr;
        private bool didSetNotConnected = false;

        private bool dialWasRotated = false;

        // ajout test globalSettings
        private SonarGlobalSettings global = null;
        private bool globalSettingsLoaded = false;

        #endregion

        #region Public Methods

        public SonarDialOutputVolumeAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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

        #region PluginBase

        public async override void DialRotate(DialRotatePayload payload)
        {
            dialWasRotated = true;
            if (!(settings.OutputDevice == typeReturn.Streaming && global.ModeValue == "classic"))
            {
                SonarApiLink();
                Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
                if (sonarWebServerAddress == "error")
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Key pressed but Sonar is not connected!");
                    await Connection.ShowAlert();
                    return;
                }

                // calcul de la nouvelle valeur
                double increment = (payload.Ticks * settings.Step) / 100;
                double outputValue = ReturnVolume(global, settings.OutputDevice) + increment;
                outputValue = Math.Max(MIN_VALUE, outputValue);
                outputValue = Math.Min(MAX_VALUE, outputValue);
                // affectation de la valeur
                PutData(GenerateURI(global, settings.OutputDevice, "SetVolume") + outputValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                SetGlobalSettings();
            }
        }

        public async override void DialDown(DialPayload payload)
        {
            if (!(settings.OutputDevice == typeReturn.Streaming && global.ModeValue == "classic"))
            {
                SonarApiLink();
                Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
                if (sonarWebServerAddress == "error")
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Key pressed but Sonar is not connected!");
                    await Connection.ShowAlert();
                    return;
                }
                PutData(GenerateURI(global, settings.OutputDevice, "SetMute") + BoolToString(!ReturnMute(global, settings.OutputDevice)));
                dialWasRotated = false;
                SetGlobalSettings();
            }
        }

        public async override void DialUp(DialPayload payload)
        {
            dialWasRotated = false;
        }

        public async override void TouchPress(TouchpadPressPayload payload)
        {
            if (!(settings.OutputDevice == typeReturn.Streaming && global.ModeValue == "classic"))
            {
                SonarApiLink();
                Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
                if (sonarWebServerAddress == "error")
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Key pressed but Sonar is not connected!");
                    await Connection.ShowAlert();
                    return;
                }
                PutData(GenerateURI(global, settings.OutputDevice, "SetMute") + BoolToString(!ReturnMute(global, settings.OutputDevice)));
                dialWasRotated = false;
                SetGlobalSettings();
            }
        }

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
                Logger.Instance.LogMessage(TracingLevel.WARN, "SonarDialOutputVolumeAction : Global Settings do not exist!");
                SetGlobalSettings();
            }

            // mise à jour du layout
            Dictionary<string, string> dkv = new Dictionary<string, string>();
            string LibelleReturn = "";
            if (global.ModeValue == "classic")
            {
                if (settings.OutputDevice == typeReturn.Monitoring)
                {
                    if (!global.ClassicMasterMute)
                        PrefetchImages(DEFAULT_IMAGES, 0);
                    else
                    {
                        PrefetchImages(DEFAULT_IMAGES, 1);
                        LibelleReturn = "Mute";
                    }
                }
                else
                {
                    PrefetchImages(DEFAULT_IMAGES, 4);
                    LibelleReturn = "Lock";
                }
            }
            else
            {
                if (settings.OutputDevice == typeReturn.Monitoring)
                {
                    if (!global.StreamerMonitoringMasterMute)
                        PrefetchImages(DEFAULT_IMAGES, 0);
                    else
                    {
                        PrefetchImages(DEFAULT_IMAGES, 1);
                        LibelleReturn = "Mute";
                    }
                }
                else
                {
                    if (!global.StreamerStreamingMasterMute)
                        PrefetchImages(DEFAULT_IMAGES, 2);
                    else
                    {
                        PrefetchImages(DEFAULT_IMAGES, 3);
                        LibelleReturn = "Mute";
                    }
                }
            }
            if (LibelleReturn == "")
                LibelleReturn = Math.Round(ReturnVolume(global, settings.OutputDevice) * 100, 0).ToString() + "%";
            dkv["icon"] = mainImageStr;
            dkv["value"] = LibelleReturn;
            dkv["indicator"] = Tools.RangeToPercentage((int)(ReturnVolume(global, settings.OutputDevice) * 100), (int)(MIN_VALUE * 100), (int)(MAX_VALUE * 100)).ToString(); ;
            Connection.SetFeedbackAsync(dkv);
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
