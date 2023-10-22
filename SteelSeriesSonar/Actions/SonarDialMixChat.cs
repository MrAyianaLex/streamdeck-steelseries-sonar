using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Text.Json;
using static SteelSeriesSonar.SSSManager;

namespace SteelSeriesSonar
{
    [PluginActionId("com.mrayianalex.sonar.dialchatmix")]
    class ChatMixDialAction : EncoderBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    ChatMixStep = 1
                };
                return instance;
            }

            [JsonProperty(PropertyName = "step")]
            public double ChatMixStep { get; set; }
        }

        #region Private members

        private const double MIN_VALUE = -1;
        private const double MAX_VALUE = 1;
        private const double DEFAULT_STEP_SIZE = 1;

        private readonly PluginSettings settings;
        private readonly string[] DEFAULT_IMAGES = new string[]
       {
            @"Images/dial_chatmix_colo@2x.png",
            @"Images/dial_chatmix_lock@2x.png"
       };
        private string mainImageStr;
        private bool didSetNotConnected = false;

        private bool dialWasRotated = false;

        // ajout test globalSettings
        private SonarGlobalSettings global = null;
        private bool globalSettingsLoaded = false;

        #endregion

        #region Public Methods

        public ChatMixDialAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            if (global.ChatMixEnabled)
            {
                SonarApiLink();
                Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
                if (sonarWebServerAddress == "error")
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Key pressed but Sonar is not connected!");
                    await Connection.ShowAlert();
                    return;
                }

                dialWasRotated = true;
                // calcul de la nouvelle valeur
                double increment = (payload.Ticks * settings.ChatMixStep) / 100;
                double outputValue = global.ChatMixBalance + increment;
                outputValue = Math.Max(MIN_VALUE, outputValue);
                outputValue = Math.Min(MAX_VALUE, outputValue);
                // affectation de la valeur
                PutData("chatMix?balance=" + outputValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                SetGlobalSettings();
            }
        }

        public async override void DialDown(DialPayload payload)
        {
            if (global.ChatMixEnabled)
            {
                SonarApiLink();
                Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
                if (sonarWebServerAddress == "error")
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Key pressed but Sonar is not connected!");
                    await Connection.ShowAlert();
                    return;
                }
                // affectation de la valeur
                PutData("chatMix?balance=0.00");
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
            if (global.ChatMixEnabled)
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
                double outputValue = global.ChatMixBalance * -1;
                // affectation de la valeur
                PutData("chatMix?balance=" + outputValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
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
                Logger.Instance.LogMessage(TracingLevel.WARN, "ChatMixDialAction : Global Settings do not exist!");
                SetGlobalSettings();
            }
            Dictionary<string, string> dkv = new Dictionary<string, string>();
            if (global.ChatMixEnabled)
            {
                // initialisation de l'image
                PrefetchImages(DEFAULT_IMAGES, 0);
                // mise à jour de l'affichage
                dkv["icon"] = mainImageStr;
                dkv["value"] = Math.Round(global.ChatMixBalance * 100, 0).ToString() + "%";
                dkv["indicator"] = Tools.RangeToPercentage((int)(global.ChatMixBalance * 100), (int)(MIN_VALUE * 100), (int)(MAX_VALUE * 100)).ToString();
            }
            else
            {
                // initialisation de l'image
                PrefetchImages(DEFAULT_IMAGES, 1);
                // mise à jour de l'affichage
                dkv["icon"] = mainImageStr;
                dkv["value"] = "Lock";
                dkv["indicator"] = "";
            }
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
