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
        public override int Version { get; set; } = 7;
    }

    public sealed class GeneralConfig
    {
        [JsonPropertyName("IP_Do_Servidor")]
        public string ServerIP { get; set; } = "connect 103.14.27.41:27625; password 151605";
    }

    public sealed class EmbedConfig
    {
        [JsonPropertyName("Nome_Do_Bot")]
        public string WebhookUsername { get; set; } = "THE OWLS - PLACAR";

        [JsonPropertyName("Avatar_Do_Bot")]
        public string WebhookAvatarUrl { get; set; } = "https://images-ext-1.discordapp.net/external/4Tw6wzN5XaaCGxEd2Sbukw3yql9LLTMr3t3KcXjdSz0/%3Fsize%3D2048/https/cdn.discordapp.com/avatars/1379614604048470026/56c3370ef900262b2140813227713fb7.png?format=webp&quality=lossless";

        [JsonPropertyName("Cor_Hexadecimal")]
        public string EmbedColor { get; set; } = "#00ffff";

        [JsonPropertyName("Cor_Aleatoria")]
        public bool RandomColor { get; set; } = true;

        [JsonPropertyName("Link_Das_Demos")]
        public string DemosUrl { get; set; } = "http://137.131.134.232";
        
        [JsonPropertyName("Link_Da_Loja_De_Skins")]
        public string SkinsUrl { get; set; } = "https://inventory.cstrike.app";

        [JsonPropertyName("Formato_De_Nome")]
        public string NameFormat { get; set; } = "{CLAN} {NAME}: {K} - {D}";

        [JsonPropertyName("Link_Da_Steam_Ativo")]
        public bool EmbedSteamLink { get; set; } = false;
    }

    public sealed class WebhookConfig
    {
        [JsonPropertyName("RoleID_Avisar_Cargos")]
        public ulong NotifyMembersRoleID { get; set; } = 0;

        [JsonPropertyName("Final_Partida_Ativo")]
        public bool GameEndScoreboard { get; set; } = true;

        [JsonPropertyName("Webhook_Para_O_Need")]
        public string RequestPlayersURL { get; set; } = "";

        [JsonPropertyName("Webhook_Para_O_Placar")]
        public string ScoreboardURL { get; set; } = "";
    }
}