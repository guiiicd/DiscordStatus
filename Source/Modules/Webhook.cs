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
        // NOME E AVATAR FIXOS PARA TODOS OS WEBHOOKS
        private const string WebhookUsername = "THE OWLS - PLACAR";
        private const string WebhookAvatarUrl = "https://images-ext-1.discordapp.net/external/4Tw6wzN5XaaCGxEd2Sbukw3yql9LLTMr3t3KcXjdSz0/%3Fsize%3D2048/https/cdn.discordapp.com/avatars/1379614604048470026/56c3370ef900262b2140813227713fb7.png?format=webp&quality=lossless";

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

        public async Task InitialMessageAsync()
        {
            DSLog.Log(0, "Initializing Embed");
            try
            {
                List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.StatusWebhookURL);
                foreach (var webhookClient in webhookClients)
                {
                    if (WConfig.StatusMessageID == 0)
                    {
                        DSLog.Log(2, "MessageID is not set, creating a new one.");
                        ulong messageId = await webhookClient.SendMessageAsync(
                            embeds: new[] { CreateStatusEmbed() },
                            username: WebhookUsername,
                            avatarUrl: WebhookAvatarUrl);
                        _g.MessageID = messageId;
                        await ConfigManager.SaveAsync("WebhookConfig", "StatusMessageID", _g.MessageID);
                    }
                    else
                    {
                        _g.MessageID = WConfig.StatusMessageID;
                        await webhookClient.ModifyMessageAsync(_g.MessageID, props =>
                        {
                            props.Embeds = new[] { CreateStatusEmbed() };
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DSLog.Log(2, $"Failed Initializing: {ex}");
            }
        }

        public async Task UpdateEmbed()
        {
            try
            {
                List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.StatusWebhookURL);
                foreach (var webhookClient in webhookClients)
                {
                    await webhookClient.ModifyMessageAsync(_g.MessageID, props =>
                    {
                        props.Embeds = new[] { CreateStatusEmbed() };
                    });
                }
                DSLog.Log(1, "Embed updated successfully!");
            }
            catch (Exception ex)
            {
                DSLog.Log(2, $"Error updating embed: {ex.Message}");
            }
        }

        public Embed CreateStatusEmbed()
        {
            string ctTeamName = string.IsNullOrEmpty(_g.CTName) ? "Contra-Terroristas" : _g.CTName;
            string tTeamName = string.IsNullOrEmpty(_g.TName) ? "Terroristas" : _g.TName;

            var builder = new EmbedBuilder()
                .WithTitle("STATUS DO SERVIDOR")
                .WithColor(_chores.GetEmbedColor())
                .WithTimestamp(DateTimeOffset.UtcNow);

            string imageUrl = EConfig.MapImg.Replace("{MAPNAME}", _g.MapName);
            if (!_g.PlayerList.Any() && _chores.IsURLValid(EConfig.IdleImg))
            {
                imageUrl = EConfig.IdleImg;
            }
            if (_chores.IsURLValid(imageUrl))
            {
                builder.WithImageUrl(imageUrl);
            }

            if (_g.PlayerList.Any())
            {
                string tnames = !_g.TPlayersName.Any() ? "Nenhum jogador" : string.Join("\n", _g.TPlayersName);
                string ctnames = !_g.CtPlayersName.Any() ? "Nenhum jogador" : string.Join("\n", _g.CtPlayersName);
                builder.WithDescription($"**Mapa:** `{_g.MapName}`\n**Placar:** {ctTeamName} **{_g.CTScore}** vs **{_g.TScore}** {tTeamName}");
                builder.AddField(ctTeamName, ctnames, true);
                builder.AddField(tTeamName, tnames, true);
                builder.AddField("\u200B", "\u200B", false);
            }
            else
            {
                builder.WithDescription($"**Mapa:** `{_g.MapName}`\n");
                builder.AddField("Jogadores", "Nenhum jogador conectado.", false);
            }

            string connectInfo = _chores.IsURLValid(GConfig.PHPURL) ? $"[Conectar via Steam]({_g.ConnectURL})" : $"```connect {_g.ServerIP}```";
            builder.AddField("Servidor", connectInfo, false);
            return builder.Build();
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

                string connectInfo = _chores.IsURLValid(GConfig.PHPURL) ? $"[Conectar via Steam]({_g.ConnectURL})" : $"```connect {_g.ServerIP}```";
                builder.AddField("Entre no Servidor", connectInfo, false);

                string content = (WConfig.NotifyMembersRoleID != 0) ? $"<@&{WConfig.NotifyMembersRoleID}>" : "";
                await webhookClient.SendMessageAsync(
                    text: content,
                    embeds: new[] { builder.Build() },
                    username: WebhookUsername,
                    avatarUrl: WebhookAvatarUrl);
            }
        }

        public async Task NewMap(string mapname, int counts)
        {
            List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.NotifyWebhookURL);
            foreach (var webhookClient in webhookClients)
            {
                var builder = new EmbedBuilder()
                    .WithTitle("🗺️ MUDANÇA DE MAPA")
                    .WithDescription($"O mapa foi alterado para `{mapname}`.")
                    .WithColor(_chores.GetEmbedColor())
                    .WithTimestamp(DateTimeOffset.UtcNow);

                if (_chores.IsURLValid(EConfig.MapImg))
                {
                    builder.WithImageUrl(EConfig.MapImg.Replace("{MAPNAME}", mapname));
                }
                
                builder.AddField("Jogadores", $"`{counts}/{_g.MaxPlayers}`", false);
                string connectInfo = _chores.IsURLValid(GConfig.PHPURL) ? $"[Conectar via Steam]({_g.ConnectURL})" : $"```connect {_g.ServerIP}```";
                builder.AddField("Junte-se a Nós!", connectInfo, false);

                await webhookClient.SendMessageAsync(
                    embeds: new[] { builder.Build() },
                    username: WebhookUsername,
                    avatarUrl: WebhookAvatarUrl);
            }
        }

        // public async Task GameEnd(string mvp, List<string> tplayersName, List<string> ctplayersName)
        // {
        //     List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.ScoreboardURL);
        //     foreach (var webhookClient in webhookClients)
        //     {
        //         string tnames = !tplayersName.Any() ? "Nenhum jogador" : string.Join("\n", tplayersName);
        //         string ctnames = !ctplayersName.Any() ? "Nenhum jogador" : string.Join("\n", ctplayersName);
        //         string ctTeamName = string.IsNullOrEmpty(_g.CTName) ? "Contra-Terroristas" : _g.CTName;
        //         string tTeamName = string.IsNullOrEmpty(_g.TName) ? "Terroristas" : _g.TName;

        //         var builder = new EmbedBuilder()
        //             .WithTitle($"Fim de Partida `{_g.MapName}`")
        //             .WithDescription($"**Placar**\n {ctTeamName} **{_g.CTScore}** vs **{_g.TScore}** {tTeamName}")
        //             .WithColor(_chores.GetEmbedColor())
        //             .WithTimestamp(DateTimeOffset.UtcNow);

        //         builder.AddField("👑 MVP da Partida", mvp, false);
        //         builder.AddField(ctTeamName, ctnames, true);
        //         builder.AddField(tTeamName, tnames, true);

        //         string connectInfo = _chores.IsURLValid(GConfig.PHPURL) ? $"[Conectar via Steam]({_g.ConnectURL})" : $"```connect {_g.ServerIP}````";
        //         builder.AddField("Servidor", connectInfo, false);

        //         if (_chores.IsURLValid(EConfig.MapImg))
        //         {
        //             builder.WithImageUrl(EConfig.MapImg.Replace("{MAPNAME}", _g.MapName));
        //         }

        //         await webhookClient.SendMessageAsync(
        //             embeds: new[] { builder.Build() },
        //             username: WebhookUsername,
        //             avatarUrl: WebhookAvatarUrl);
        //     }
        // }
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

                string connectInfo = _chores.IsURLValid(GConfig.PHPURL) ? $"[Conectar via Steam]({_g.ConnectURL})" : $"```connect {_g.ServerIP}```";
                builder.AddField("Servidor", connectInfo, false);
                
                // Adiciona link de demos
                builder.AddField("📹 Demos da Partida", "[Baixar Demos](http://137.131.134.232)", false);

                if (_chores.IsURLValid(EConfig.MapImg))
                {
                    builder.WithImageUrl(EConfig.MapImg.Replace("{MAPNAME}", _g.MapName));
                }

                await webhookClient.SendMessageAsync(
                    embeds: new[] { builder.Build() },
                    username: WebhookUsername,
                    avatarUrl: WebhookAvatarUrl);
            }
        }


        public async Task ServerOffiline()
        {
            try
            {
                List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.StatusWebhookURL);
                foreach (var webhookClient in webhookClients)
                {
                    var builder = new EmbedBuilder()
                        .WithTitle("STATUS DO SERVIDOR")
                        .WithDescription("🔴 **O servidor está offline.**")
                        .WithColor(new Color(220, 20, 60)) // Crimson Red
                        .WithTimestamp(DateTimeOffset.UtcNow);

                    if (_chores.IsURLValid(EConfig.OfflineImg))
                    {
                        builder.WithImageUrl(EConfig.OfflineImg);
                    }

                    await webhookClient.ModifyMessageAsync(_g.MessageID, props =>
                    {
                        props.Embeds = new[] { builder.Build() };
                    });
                }
            }
            catch (Exception ex)
            {
                DSLog.Log(2, $"Error setting server to offline: {ex.Message}");
            }
        }

        public async Task SendServerOnlineMessageAsync(string connectUrl, string mapName) // connectUrl é o fallback PHP
        {
            List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.ServerRestartWebhookURL);
            
            if (!webhookClients.Any())
            {
                DSLog.Log(0, "ServerRestartWebhookURL não está configurada. Pulando mensagem de servidor online.");
                return;
            }

            DSLog.Log(1, "Enviando mensagem de 'Servidor Online' para o Discord...");

            // ### LÓGICA DE ESCOLHA DO IP ###
            string connectInfo;
            if (!string.IsNullOrEmpty(_g.FakeIP) && _g.FakeIPPort != 0)
            {
                // Se o FakeIP foi detectado, usa ele.
                connectInfo = $"```connect {_g.FakeIP}:{_g.FakeIPPort}```";
                DSLog.Log(1, "Usando FakeIP para a mensagem de 'Servidor Online'.");
            }
            else
            {
                // Se não, usa o IP público (ServerIP)
                connectInfo = $"```connect {_g.ServerIP}```";
                DSLog.Log(1, "Usando IP Público para a mensagem de 'Servidor Online'.");
            }
            // ### FIM DA LÓGICA ###

            foreach (var webhookClient in webhookClients)
            {
                var builder = new EmbedBuilder()
                    .WithTitle("✅ Servidor Reiniciado e Online!")
                    .WithDescription($"O servidor está pronto e rodando o mapa **{mapName}**.")
                    .WithColor(new Color(0, 255, 0)) // Cor Verde
                    .WithTimestamp(DateTimeOffset.UtcNow);
                
                // Adiciona o campo "Servidor" com o bloco de código
                builder.AddField("Servidor", connectInfo, false);

                try
                {
                    await webhookClient.SendMessageAsync(
                        embeds: new[] { builder.Build() },
                        username: WebhookUsername,
                        avatarUrl: WebhookAvatarUrl);
                }
                catch (Exception ex)
                {
                    DSLog.Log(2, $"Falha ao enviar mensagem de servidor online: {ex.Message}");
                }
            }
        }
    }
}
