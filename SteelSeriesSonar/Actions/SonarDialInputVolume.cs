using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Text.Json;
using static SteelSeriesSonar.SSSManager;

namespace SteelSeriesSonar
{
    [PluginActionId("com.mrayianalex.sonar.dialinputvolume")]
    class SonarDialInputVolumeAction : EncoderBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    Step = DEFAULT_STEP_SIZE,
                    OutputDevice = typeReturn.Monitoring,
                    InputDevice = typeRole.game
                };
                return instance;
            }

            [JsonProperty(PropertyName = "step")]
            public double Step { get; set; }

            [JsonProperty(PropertyName = "outputDevice")]
            public typeReturn OutputDevice { get; set; }

            [JsonProperty(PropertyName = "inputDevice")]
            public typeRole InputDevice { get; set; }
        }

        #region Private members

        private const double MIN_VALUE = 0;
        private const double MAX_VALUE = 1;
        private const double DEFAULT_STEP_SIZE = 1;

        private readonly PluginSettings settings;
        private readonly string[] DEFAULT_IMAGES_MONITORING = new string[]
        {
            @"Images/dial_game_monitoring@2x.png",
            @"Images/dial_chat_monitoring@2x.png",
            @"Images/dial_media_monitoring@2x.png",
            @"Images/dial_aux_monitoring@2x.png",
            @"Images/dial_micro_monitoring@2x.png",
            @"Images/dial_game_monitoring_mute@2x.png",
            @"Images/dial_chat_monitoring_mute@2x.png",
            @"Images/dial_media_monitoring_mute@2x.png",
            @"Images/dial_aux_monitoring_mute@2x.png",
            @"Images/dial_micro_monitoring_mute@2x.png"
        };
        private readonly string[] DEFAULT_IMAGES_STREAMING = new string[]
        {
            @"Images/dial_game_streaming@2x.png",
            @"Images/dial_chat_streaming@2x.png",
            @"Images/dial_media_streaming@2x.png",
            @"Images/dial_aux_streaming@2x.png",
            @"Images/dial_micro_streaming@2x.png",
            @"Images/dial_game_streaming_mute@2x.png",
            @"Images/dial_chat_streaming_mute@2x.png",
            @"Images/dial_media_streaming_mute@2x.png",
            @"Images/dial_aux_streaming_mute@2x.png",
            @"Images/dial_micro_streaming_mute@2x.png",
            @"Images/dial_game_streaming_lock@2x.png",
            @"Images/dial_chat_streaming_lock@2x.png",
            @"Images/dial_media_streaming_lock@2x.png",
            @"Images/dial_aux_streaming_lock@2x.png",
            @"Images/dial_micro_streaming_lock@2x.png"
        };
        private string mainImageStr;
        private bool didSetNotConnected = false;

        private bool dialWasRotated = false;

        // ajout test globalSettings
        private SonarGlobalSettings global = null;
        private bool globalSettingsLoaded = false;

        #endregion

        #region Public Methods

        public SonarDialInputVolumeAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            if (global.ModeValue == "classic" && settings.OutputDevice == typeReturn.Streaming)
                return;
            SonarApiLink();
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
            if (sonarWebServerAddress == "error")
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Key pressed but Sonar is not connected!");
                await Connection.ShowAlert();
                return;
            }
            dialWasRotated = true;
            double increment = (payload.Ticks * settings.Step) / 100;
            double outputValue = ReturnVolume(global, settings.OutputDevice, settings.InputDevice) + increment;
            outputValue = Math.Max(MIN_VALUE, outputValue);
            outputValue = Math.Min(MAX_VALUE, outputValue);
            // affectation de la valeur
            PutData(GenerateURI(global, settings.OutputDevice, "SetVolume", settings.InputDevice) + outputValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            SetGlobalSettings();
        }

        public async override void DialDown(DialPayload payload)
        {
            if (global.ModeValue == "classic" && settings.OutputDevice == typeReturn.Streaming)
                return;
            SonarApiLink();
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
            if (sonarWebServerAddress == "error")
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Key pressed but Sonar is not connected!");
                await Connection.ShowAlert();
                return;
            }
            PutData(GenerateURI(global, settings.OutputDevice, "SetMute", settings.InputDevice) + BoolToString(!ReturnMute(global, settings.OutputDevice, settings.InputDevice)));
            dialWasRotated = false;
            SetGlobalSettings();
        }

        public async override void DialUp(DialPayload payload)
        {
            dialWasRotated = false;
        }

        public async override void TouchPress(TouchpadPressPayload payload)
        {
            if (global.ModeValue == "classic" && settings.OutputDevice == typeReturn.Streaming)
                return;
            SonarApiLink();
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
            if (sonarWebServerAddress == "error")
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Key pressed but Sonar is not connected!");
                await Connection.ShowAlert();
                return;
            }
            PutData(GenerateURI(global, settings.OutputDevice, "SetMute", settings.InputDevice) + BoolToString(!ReturnMute(global, settings.OutputDevice, settings.InputDevice)));
            dialWasRotated = false;
            SetGlobalSettings();
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
                Logger.Instance.LogMessage(TracingLevel.WARN, "SonarDialInputVolumeAction : Global Settings do not exist!");
                SetGlobalSettings();
            }

            // mise à jour du layout
            Dictionary<string, string> dkv = new Dictionary<string, string>();
            string LibelleReturn = "";
            string IndicatorReturn = "";
            if (global.ModeValue == "classic")
            {
                if (settings.OutputDevice == typeReturn.Streaming)
                {
                    switch (settings.InputDevice)
                    {
                        case typeRole.game:
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 10);
                            break;
                        case typeRole.chatRender:
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 11);
                            break;
                        case typeRole.media:
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 12);
                            break;
                        case typeRole.aux:
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 13);
                            break;
                        case typeRole.chatCapture:
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 14);
                            break;
                    }
                    LibelleReturn = "Lock";
                }
                else if (settings.OutputDevice == typeReturn.Monitoring)
                {
                    if (settings.InputDevice == typeRole.game)
                    {
                        if (!global.ClassicGameMute)
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 0);
                        }
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 5);
                            LibelleReturn = "Mute";
                        }
                    }
                    else if (settings.InputDevice == typeRole.chatRender)
                    {
                        if (!global.ClassicChatMute)
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 1);
                        }
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 6);
                            LibelleReturn = "Mute";
                        }
                    }
                    else if (settings.InputDevice == typeRole.media)
                    {
                        if (!global.ClassicMediaMute)
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 2);
                        }
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 7);
                            LibelleReturn = "Mute";
                        }
                    }
                    else if (settings.InputDevice == typeRole.aux)
                    {
                        if (!global.ClassicAuxMute)
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 3);
                        }
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 8);
                            LibelleReturn = "Mute";
                        }
                    }
                    else if (settings.InputDevice == typeRole.chatCapture)
                    {
                        if (!global.ClassicMicroMute)
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 4);
                        }
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 9);
                            LibelleReturn = "Mute";
                        }
                    }
                }
            }
            else if (global.ModeValue == "stream")
            {
                if (settings.OutputDevice == typeReturn.Streaming)
                {
                    if (settings.InputDevice == typeRole.game)
                    {
                        if (!global.StreamerStreamingGameMute)
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 0);
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 5);
                            LibelleReturn = "Mute";
                        }
                    }
                    else if (settings.InputDevice == typeRole.chatRender)
                    {
                        if (!global.StreamerStreamingChatMute)
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 1);
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 6);
                            LibelleReturn = "Mute";
                        }
                    }
                    else if (settings.InputDevice == typeRole.media)
                    {
                        if (!global.StreamerStreamingMediaMute)
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 2);
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 7);
                            LibelleReturn = "Mute";
                        }
                    }
                    else if (settings.InputDevice == typeRole.aux)
                    {
                        if (!global.StreamerStreamingAuxMute)
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 3);
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 8);
                            LibelleReturn = "Mute";
                        }
                    }
                    else if (settings.InputDevice == typeRole.chatCapture)
                    {
                        if (!global.StreamerStreamingMicroMute)
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 4);
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_STREAMING, 9);
                            LibelleReturn = "Mute";
                        }
                    }
                }
                else if (settings.OutputDevice == typeReturn.Monitoring)
                {
                    if (settings.InputDevice == typeRole.game)
                    {
                        if (!global.StreamerMonitoringGameMute)
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 0);
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 5);
                            LibelleReturn = "Mute";
                        }
                    }
                    else if (settings.InputDevice == typeRole.chatRender)
                    {
                        if (!global.StreamerMonitoringChatMute)
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 1);
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 6);
                            LibelleReturn = "Mute";
                        }
                    }
                    else if (settings.InputDevice == typeRole.media)
                    {
                        if (!global.StreamerMonitoringMediaMute)
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 2);
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 7);
                            LibelleReturn = "Mute";
                        }
                    }
                    else if (settings.InputDevice == typeRole.aux)
                    {
                        if (!global.StreamerMonitoringAuxMute)
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 3);
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 8);
                            LibelleReturn = "Mute";
                        }
                    }
                    else if (settings.InputDevice == typeRole.chatCapture)
                    {
                        if (!global.StreamerMonitoringMicroMute)
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 4);
                        else
                        {
                            PrefetchImages(DEFAULT_IMAGES_MONITORING, 9);
                            LibelleReturn = "Mute";
                        }
                    }
                }
            }
            if (LibelleReturn == "")
                LibelleReturn = Math.Round(ReturnVolume(global, settings.OutputDevice, settings.InputDevice) * 100, 0).ToString() + "%";
            IndicatorReturn = Tools.RangeToPercentage((int)(ReturnVolume(global, settings.OutputDevice, settings.InputDevice) * 100), (int)(MIN_VALUE * 100), (int)(MAX_VALUE * 100)).ToString();

            dkv["icon"] = mainImageStr;
            dkv["value"] = LibelleReturn;
            dkv["indicator"] = IndicatorReturn;
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
