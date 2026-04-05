namespace DiscordStatus
{
    using System.Text.Json.Serialization;
    using CounterStrikeSharp.API.Core;

    public sealed class DSconfig : BasePluginConfig
    {
        [JsonPropertyName("GeneralConfig")]
        public GeneralConfig GeneralConfig { get; set; } = new GeneralConfig();

        [JsonPropertyName("WebhookConfig")]
        public WebhookConfig WebhookConfig { get; set; } = new WebhookConfig();

        [JsonPropertyName("EmbedConfig")]
        public EmbedConfig EmbedConfig { get; set; } = new EmbedConfig();

        [JsonPropertyName("ConfigVersion")]
        public override int Version { get; set; } = 6; // Bump version
    }

    public sealed class GeneralConfig
    {
        [JsonPropertyName("ServerIP")]
        public string ServerIP { get; set; } = string.Empty;

        [JsonPropertyName("PHPURL")]
        public string PHPURL { get; set; } = string.Empty;
    }

    public sealed class EmbedConfig
    {
        [JsonPropertyName("WebhookUsername")]
        public string WebhookUsername { get; set; } = "THE OWLS - PLACAR";

        [JsonPropertyName("WebhookAvatarUrl")]
        public string WebhookAvatarUrl { get; set; } = "https://images-ext-1.discordapp.net/external/4Tw6wzN5XaaCGxEd2Sbukw3yql9LLTMr3t3KcXjdSz0/%3Fsize%3D2048/https/cdn.discordapp.com/avatars/1379614604048470026/56c3370ef900262b2140813227713fb7.png?format=webp&quality=lossless";

        [JsonPropertyName("MapImg")]
        public string MapImg { get; set; } = "{MAPNAME}.jpg";

        [JsonPropertyName("EmbedColor")]
        public string EmbedColor { get; set; } = "#00ffff";

        [JsonPropertyName("RandomColor")]
        public bool RandomColor { get; set; } = true;

        [JsonPropertyName("DemosUrl")]
        public string DemosUrl { get; set; } = "http://137.131.134.232";
        
        [JsonPropertyName("SkinsUrl")]
        public string SkinsUrl { get; set; } = "https://inventory.cstrike.app";

        [JsonPropertyName("NameFormat")]
        public string NameFormat { get; set; } = "{CLAN} {NAME}: {K} - {D}";

        [JsonPropertyName("EmbedSteamLink")]
        public bool EmbedSteamLink { get; set; } = false;
    }

    public sealed class WebhookConfig
    {
        [JsonPropertyName("NotifyMembersRoleID")]
        public ulong NotifyMembersRoleID { get; set; } = 0;

        [JsonPropertyName("GameEndScoreboard")]
        public bool GameEndScoreboard { get; set; } = true;

        [JsonPropertyName("RequestPlayersURL")]
        public string RequestPlayersURL { get; set; } = "";

        [JsonPropertyName("ScoreboardURL")]
        public string ScoreboardURL { get; set; } = "";
    }
}