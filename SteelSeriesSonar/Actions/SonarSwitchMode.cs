using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using static SteelSeriesSonar.SSSManager;

namespace SteelSeriesSonar
{
    [PluginActionId("com.mrayianalex.sonar.switchmode")]
    class SwitchModeAction : KeypadBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    ActionTarget = ActionEnum.Switch
                };
                return instance;
            }

            [JsonProperty(PropertyName = "actionTarget")]
            public ActionEnum ActionTarget { get; set; }
        }

        #region Private members

        private readonly PluginSettings settings;
        private bool didSetNotConnected = false;

        // ajout test globalSettings
        private SonarGlobalSettings global = null;
        private bool globalSettingsLoaded = false;

        #endregion

        #region Public Methods

        public SwitchModeAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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

            switch (settings.ActionTarget)
            {
                case ActionEnum.Switch:
                    SwitchMode();
                    break;
                case ActionEnum.Classic:
                    SwitchMode("classic");
                    break;
                case ActionEnum.Stream:
                    SwitchMode("stream");
                    break;
                default:
                    SwitchMode();
                    break;
            }
            SetGlobalSettings();
        }

        public override void KeyReleased(KeyPayload payload)
        {
            SonarApiLink();
            switch (global.ModeValue)
            {
                case "classic":
                    Connection.SetStateAsync(0);
                    break;
                case "stream":
                    Connection.SetStateAsync(1);
                    break;
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

            if (settings.ActionTarget == ActionEnum.Switch)
            {
                switch (global.ModeValue)
                {
                    case "classic":
                        Connection.SetImageAsync(Tools.ImageToBase64(Image.FromFile("Images/switch_mode_switch_classic@2x.png"), true));
                        break;
                    case "stream":
                        Connection.SetImageAsync(Tools.ImageToBase64(Image.FromFile("Images/switch_mode_switch_stream@2x.png"), true));
                        break;
                }
            }
            else
            {
                switch (global.ModeValue)
                {
                    case "classic":
                        Connection.SetStateAsync(0);
                        break;
                    case "stream":
                        Connection.SetStateAsync(1);
                        break;
                }
            }
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
