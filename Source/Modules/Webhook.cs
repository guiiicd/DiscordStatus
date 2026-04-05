using Discord;
using Discord.Webhook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordStatus
{
    public class Webhook : IWebhook
    {
        private readonly IChores _chores;
        private readonly Globals _g;
        private WebhookConfig WConfig => _g.WConfig;
        private EmbedConfig EConfig => _g.EConfig;
        private GeneralConfig GConfig => _g.GConfig;

        public Webhook(IChores chores, Globals globals)
        {
            _g = globals;
            _chores = chores;
        }

        // --- MÉTODO NOVO (PARTE 3) ---
        // Verifica se temos um ID SDR da Valve. Se sim, usa ele. Se não, usa o IP.
        private string GetConnectCommand()
        {
            // Acessa a variável estática que criamos no passo anterior
            string? sdrId = DiscordStatus.SteamApiHelper?.GetSdrConnectString();

            if (!string.IsNullOrEmpty(sdrId))
            {
                return $"connect {sdrId}";
            }

            // Fallback para o IP clássico se o SDR não carregou
            return $"connect {_g.ServerIP}";
        }
        // -----------------------------

        private List<DiscordWebhookClient> CreateWebhookClients(string url)
        {
            List<DiscordWebhookClient> clients = new();
            if (string.IsNullOrEmpty(url)) return clients;

            string[] list = url.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string u in list)
            {
                if (_chores.IsURLValid(u))
                {
                    clients.Add(new DiscordWebhookClient(u));
                }
                else
                {
                    DSLog.Log(2, $"Invalid webhook URL provided: {u}");
                }
            }
            return clients;
        }

        public async Task RequestPlayers(string name)
        {
            List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.RequestPlayersURL);
            foreach (var webhookClient in webhookClients)
            {
                var builder = new EmbedBuilder()
                    .WithTitle("📢 CHAMANDO JOGADORES!")
                    .WithDescription($"`{name}` está chamando jogadores para a partida!")
                    .WithColor(new Color(255, 204, 0)) // Cor amarela para destaque
                    .WithTimestamp(DateTimeOffset.UtcNow);

                builder.AddField("Mapa Atual", $"`{_g.MapName}`", true);
                builder.AddField("Jogadores", $"`{_g.PlayerList.Count}/{_g.MaxPlayers}`", true);

                // ATUALIZADO PARA USAR SDR
                string connectInfo = _chores.IsURLValid(GConfig.PHPURL) ? $"[Conectar via Steam]({_g.ConnectURL})" : $"```{GetConnectCommand()}```";
                builder.AddField("Entre no Servidor", connectInfo, false);

                string content = (WConfig.NotifyMembersRoleID != 0) ? $"<@&{WConfig.NotifyMembersRoleID}>" : "";
                await webhookClient.SendMessageAsync(
                    text: content,
                    embeds: new[] { builder.Build() },
                    username: EConfig.WebhookUsername,
                    avatarUrl: EConfig.WebhookAvatarUrl);
            }
        }

        public async Task GameEnd(string mvp, List<string> tplayersName, List<string> ctplayersName)
        {
            List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.ScoreboardURL);
            foreach (var webhookClient in webhookClients)
            {
                string ctTeamName = string.IsNullOrEmpty(_g.CTName) ? "Contra-Terroristas" : _g.CTName;
                string tTeamName = string.IsNullOrEmpty(_g.TName) ? "Terroristas" : _g.TName;

                // Cabeçalho da tabela do placar sem a coluna de pontos
                string scoreboardHeader = "JOGADOR            K   A   D\n" +
                                          "------------------ --- --- ---";

                string ctnames = !ctplayersName.Any() 
                    ? "```Nenhum jogador```" 
                    : $"```{scoreboardHeader}\n{string.Join("\n", ctplayersName)}```";
            
                string tnames = !tplayersName.Any() 
                    ? "```Nenhum jogador```" 
                    : $"```{scoreboardHeader}\n{string.Join("\n", tplayersName)}```";

                var builder = new EmbedBuilder()
                    .WithTitle($"Fim de Partida `{_g.MapName}`")
                    .WithDescription($"**Placar**\n{ctTeamName} **{_g.CTScore}** vs **{_g.TScore}** {tTeamName}")
                    .WithColor(_chores.GetEmbedColor())
                    .WithTimestamp(DateTimeOffset.UtcNow);

                builder.AddField("👑 MVP da Partida", mvp, false);
                
                builder.AddField($"{ctTeamName}", ctnames, false);
                builder.AddField($"{tTeamName}", tnames, false);

                // ATUALIZADO PARA USAR SDR
                string connectInfo = _chores.IsURLValid(GConfig.PHPURL) ? $"[Conectar via Steam]({_g.ConnectURL})" : $"```{GetConnectCommand()}```";
                builder.AddField("Servidor", connectInfo, false);
                
                // Adiciona link de demos referenciando a Config
                if (_chores.IsURLValid(EConfig.DemosUrl)) 
                {
                    builder.AddField("Demos da Partida", $"[Acessar Demos]({EConfig.DemosUrl})", false);
                }
                
                if (_chores.IsURLValid(EConfig.SkinsUrl)) 
                {
                    builder.AddField("Trocar Skins", $"[Altere suas skins]({EConfig.SkinsUrl})", false);
                }

                if (_chores.IsURLValid(EConfig.MapImg))
                {
                    builder.WithImageUrl(EConfig.MapImg.Replace("{MAPNAME}", _g.MapName));
                }

                await webhookClient.SendMessageAsync(
                    embeds: new[] { builder.Build() },
                    username: EConfig.WebhookUsername,
                    avatarUrl: EConfig.WebhookAvatarUrl);
            }
        }
    }
}