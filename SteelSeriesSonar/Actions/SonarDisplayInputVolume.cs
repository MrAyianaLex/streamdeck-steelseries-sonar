using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using static SteelSeriesSonar.SSSManager;

namespace SteelSeriesSonar
{
    [PluginActionId("com.mrayianalex.sonar.displayinputdevice")]
    class SonarDisplayInputAction : KeypadBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    OutputDevice = typeReturn.Monitoring,
                    InputDevice = typeRole.none,
                    RenderHorizontal = true,
                    RenderVertical = false,
                    RenderCircle = false
                };
                return instance;
            }

            [JsonProperty(PropertyName = "outputDevice")]
            public typeReturn OutputDevice { get; set; }

            [JsonProperty(PropertyName = "inputDevice")]
            public typeRole InputDevice { get; set; }

            [JsonProperty(PropertyName = "renderHorizontal")]
            public bool RenderHorizontal { get; set; }

            [JsonProperty(PropertyName = "renderVertical")]
            public bool RenderVertical { get; set; }

            [JsonProperty(PropertyName = "renderCircle")]
            public bool RenderCircle { get; set; }
        }

        #region Private members

        private readonly PluginSettings settings;
        private readonly string[] DEFAULT_IMAGES = new string[]
        {
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

        public SonarDisplayInputAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
                Logger.Instance.LogMessage(TracingLevel.WARN, "SonarDisplayInputAction : Global Settings do not exist!");
                SetGlobalSettings();
            }
            // configuration de l'image
            if (global.ModeValue == "classic" && settings.OutputDevice == typeReturn.Streaming)
            {
                switch (settings.InputDevice)
                {
                    case typeRole.game:
                        PrefetchImages(DEFAULT_IMAGES, 0);
                        break;
                    case typeRole.chatRender:
                        PrefetchImages(DEFAULT_IMAGES, 1);
                        break;
                    case typeRole.media:
                        PrefetchImages(DEFAULT_IMAGES, 2);
                        break;
                    case typeRole.aux:
                        PrefetchImages(DEFAULT_IMAGES, 3);
                        break;
                    case typeRole.chatCapture:
                        PrefetchImages(DEFAULT_IMAGES, 4);
                        break;
                }
            }
            else
            {
                // création de l'image dans la volée
                Bitmap image = new Bitmap(144, 144);
                Graphics graphics = Graphics.FromImage(image);
                Pen WhitePen = new Pen(Color.White);
                SolidBrush WhiteBrush = new SolidBrush(Color.White);
                if (settings.RenderHorizontal)
                {
                    // Draw two white lines
                    graphics.DrawLine(WhitePen, 1, 59, 144, 59);
                    graphics.DrawLine(WhitePen, 1, 87, 144, 87);
                    // graphics.DrawLine(WhitePen, new PointF(59, 1), new PointF(144, 1));
                    // draw a rectangle représente volume level
                    graphics.FillRectangle(WhiteBrush, 1, 59, (int)Math.Ceiling(144 * ReturnVolume(global, settings.OutputDevice, settings.InputDevice)), 29);
                }
                else if (settings.RenderVertical)
                {
                    // Draw two white lines
                    graphics.DrawLine(WhitePen, 59, 1, 59, 144);
                    graphics.DrawLine(WhitePen, 87, 1, 87, 144);
                    // graphics.DrawLine(WhitePen, new PointF(59, 1), new PointF(144, 1));
                    // draw a rectangle représente volume level
                    graphics.FillRectangle(WhiteBrush, 59, 144 - (int)Math.Ceiling(144 * ReturnVolume(global, settings.OutputDevice, settings.InputDevice)), 29, (int)Math.Ceiling(144 * ReturnVolume(global, settings.OutputDevice, settings.InputDevice)));
                }
                else if (settings.RenderCircle)
                {
                    // Draw white circle
                    graphics.DrawEllipse(WhitePen, 2, 2, 140, 140);
                    // draw a rectangle représente volume level
                    int radius = (int)Math.Ceiling(70 * ReturnVolume(global, settings.OutputDevice, settings.InputDevice));
                    graphics.FillEllipse(WhiteBrush, 72 - radius, 72 - radius, radius * 2, radius * 2);
                }

                // retour de l'image
                mainImageStr = Tools.ImageToBase64(image, true);

                // Dispose of the graphics object and image
                graphics.Dispose();
                image.Dispose();
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
