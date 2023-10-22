using Newtonsoft.Json;

namespace SteelSeriesSonar
{
    public enum ActionEnum
    {
        Switch = 0,
        Classic = 1,
        Stream = 2
    }
    public enum typeDevice
    {
        Input = 0,
        Output = 1
    }
    public enum typeRole
    {
        game = 0,
        chatRender = 1,
        media = 2,
        aux = 3,
        chatCapture = 4,
        none = 5
    }
    public enum typeReturn
    {
        Monitoring = 0,
        Streaming = 1
    }
    public class Device
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; private set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; private set; }
        public Device(string deviceId, string deviceName)
        {
            Id = deviceId;
            Name = deviceName;
        }
    }
    public class SonarGlobalSettings
    {
        [JsonProperty(PropertyName = "modeValue")]
        public String ModeValue { get; set; }

        [JsonProperty(PropertyName = "chatMixBalance")]
        public float ChatMixBalance { get; set; }

        [JsonProperty(PropertyName = "chatMixEnabled")]
        public bool ChatMixEnabled { get; set; }

        // classic
        [JsonProperty(PropertyName = "classicGameVolume")]
        public float ClassicGameVolume { get; set; }

        [JsonProperty(PropertyName = "classicGameMute")]
        public bool ClassicGameMute { get; set; }

        [JsonProperty(PropertyName = "classicChatVolume")]
        public float ClassicChatVolume { get; set; }

        [JsonProperty(PropertyName = "classicChatMute")]
        public bool ClassicChatMute { get; set; }

        [JsonProperty(PropertyName = "classicMediaVolume")]
        public float ClassicMediaVolume { get; set; }

        [JsonProperty(PropertyName = "classicMediaMute")]
        public bool ClassicMediaMute { get; set; }

        [JsonProperty(PropertyName = "classicAuxVolume")]
        public float ClassicAuxVolume { get; set; }

        [JsonProperty(PropertyName = "classicAuxMute")]
        public bool ClassicAuxMute { get; set; }

        [JsonProperty(PropertyName = "classicMicroVolume")]
        public float ClassicMicroVolume { get; set; }

        [JsonProperty(PropertyName = "classicMicroMute")]
        public bool ClassicMicroMute { get; set; }

        [JsonProperty(PropertyName = "classicMasterVolume")]
        public float ClassicMasterVolume { get; set; }

        [JsonProperty(PropertyName = "classicMasterMute")]
        public bool ClassicMasterMute { get; set; }

        // streamerMonitoring
        [JsonProperty(PropertyName = "streamerMonitoringGameVolume")]
        public float StreamerMonitoringGameVolume { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringGameMute")]
        public bool StreamerMonitoringGameMute { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringGameSend")]
        public bool StreamerMonitoringGameSend { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringChatVolume")]
        public float StreamerMonitoringChatVolume { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringChatMute")]
        public bool StreamerMonitoringChatMute { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringChatSend")]
        public bool StreamerMonitoringChatSend { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringMediaVolume")]
        public float StreamerMonitoringMediaVolume { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringMediaMute")]
        public bool StreamerMonitoringMediaMute { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringMediaSend")]
        public bool StreamerMonitoringMediaSend { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringAuxVolume")]
        public float StreamerMonitoringAuxVolume { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringAuxMute")]
        public bool StreamerMonitoringAuxMute { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringAuxSend")]
        public bool StreamerMonitoringAuxSend { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringMicroVolume")]
        public float StreamerMonitoringMicroVolume { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringMicroMute")]
        public bool StreamerMonitoringMicroMute { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringMicroSend")]
        public bool StreamerMonitoringMicroSend { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringMasterVolume")]
        public float StreamerMonitoringMasterVolume { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringMasterMute")]
        public bool StreamerMonitoringMasterMute { get; set; }

        [JsonProperty(PropertyName = "streamerMonitoringMasterSend")]
        public bool StreamerMonitoringMasterSend { get; set; }

        // streamerStreaming
        [JsonProperty(PropertyName = "streamerStreamingGameVolume")]
        public float StreamerStreamingGameVolume { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingGameMute")]
        public bool StreamerStreamingGameMute { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingGameSend")]
        public bool StreamerStreamingGameSend { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingChatVolume")]
        public float StreamerStreamingChatVolume { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingChatMute")]
        public bool StreamerStreamingChatMute { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingChatSend")]
        public bool StreamerStreamingChatSend { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingMediaVolume")]
        public float StreamerStreamingMediaVolume { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingMediaMute")]
        public bool StreamerStreamingMediaMute { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingMediaSend")]
        public bool StreamerStreamingMediaSend { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingAuxVolume")]
        public float StreamerStreamingAuxVolume { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingAuxMute")]
        public bool StreamerStreamingAuxMute { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingAuxSend")]
        public bool StreamerStreamingAuxSend { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingMicroVolume")]
        public float StreamerStreamingMicroVolume { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingMicroMute")]
        public bool StreamerStreamingMicroMute { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingMicroSend")]
        public bool StreamerStreamingMicroSend { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingMasterVolume")]
        public float StreamerStreamingMasterVolume { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingMasterMute")]
        public bool StreamerStreamingMasterMute { get; set; }

        [JsonProperty(PropertyName = "streamerStreamingMasterSend")]
        public bool StreamerStreamingMasterSend { get; set; }

        [JsonProperty(PropertyName = "streamerReturnToMonitoring")]
        public bool StreamerReturnToMonitoring { get; set; }

        // list les device id configurer
        [JsonProperty(PropertyName = "classicGameDeviceId")]
        public string ClassicGameDeviceId { get; set; }

        [JsonProperty(PropertyName = "classicChatDeviceId")]
        public string ClassicChatDeviceId { get; set; }

        [JsonProperty(PropertyName = "classicMediaDeviceId")]
        public string ClassicMediaDeviceId { get; set; }

        [JsonProperty(PropertyName = "classicAuxDeviceId")]
        public string ClassicAuxDeviceId { get; set; }

        [JsonProperty(PropertyName = "classicMicroDeviceId")]
        public string ClassicMicroDeviceId { get; set; }

        [JsonProperty(PropertyName = "streamMonitoringDeviceId")]
        public string StreamerMonitoringDeviceId { get; set; }

        [JsonProperty(PropertyName = "streamStreamingDeviceId")]
        public string StreamerStreamingDeviceId { get; set; }

        [JsonProperty(PropertyName = "streamMicroDeviceId")]
        public string StreamerMicroDeviceId { get; set; }
    }
}
