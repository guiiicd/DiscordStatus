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
        // 1. NOME E AVATAR FIXOS PARA O WEBHOOK
        // Conforme solicitado, o nome e a foto do webhook são definidos aqui.
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

            string[] list = url.Split(',', StringSplitOptions.TrimEntries);
            foreach (string u in list)
            {
                if (_chores.IsURLValid(u))
                {
                    clients.Add(new DiscordWebhookClient(u));
                }
                else
                {
                    DSLog.Log(2, "Invalid webhook URL.");
                }
            }

            if (clients.Count == 0)
            {
                DSLog.Log(2, "No valid webhook URLs provided.");
            }

            return clients;
        }

        public async Task InitialMessageAsync()
        {
            DSLog.Log(0, "Initializing Embed");
            try
            {
                List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.StatusWebhookURL);
                foreach (DiscordWebhookClient webhookClient in webhookClients)
                {
                    if (webhookClient == null)
                    {
                        continue;
                    }

                    if (WConfig.StatusMessageID == 0)
                    {
                        DSLog.Log(2, "MessageID is not set up yet, Creating a new one now!");
                        ulong message = await webhookClient.SendMessageAsync(
                            embeds: new[] { CreateStatusEmbed() },
                            username: WebhookUsername,
                            avatarUrl: WebhookAvatarUrl);
                        _g.MessageID = message;
                        await ConfigManager.SaveAsync("WebhookConfig", "StatusMessageID", _g.MessageID);
                    }
                    else
                    {
                        _g.MessageID = WConfig.StatusMessageID;
                        Embed embed = CreateStatusEmbed();
                        using (webhookClient) // Dispose of the client after use
                        {
                            await webhookClient.ModifyMessageAsync(_g.MessageID, properties =>
                            {
                                properties.Embeds = new[] { embed };
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DSLog.Log(2, "Failed Initializing: " + ex.ToString());
            }
        }

        public async Task UpdateEmbed()
        {
            List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.StatusWebhookURL);
            foreach (DiscordWebhookClient webhookClient in webhookClients)
            {
                if (webhookClient == null)
                {
                    continue;
                }

                try
                {
                    using (webhookClient) // Dispose of the client after use
                    {
                        await webhookClient.ModifyMessageAsync(_g.MessageID, properties =>
                        {
                            properties.Embeds = new[] { CreateStatusEmbed() };
                        });
                    }
                    DSLog.Log(1, $"Updated embed!");
                }
                catch (Exception ex)
                {
                    DSLog.Log(2, $"Error updating embed: {ex.Message}");
                }
            }
        }

        // public Embed CreateStatusEmbed()
        // {
        //     List<string> tplayersName = _g.TPlayersName;
        //     List<string> ctplayersName = _g.CtPlayersName;
        //     string tnames;
        //     string ctnames;

        //     if (_g.PlayerList.Count > 0)
        //     {
        //         if (_g.HasCC)
        //         {
        //             tnames = !tplayersName.Any() ? "ㅤ" : string.Join("\n", tplayersName);
        //             ctnames = !ctplayersName.Any() ? "ㅤ" : string.Join("\n", ctplayersName);
        //         }
        //         else
        //         {
        //             ctnames = !ctplayersName.Any() ? "```ㅤ```" : $"```ansi\r\n\u001b[0;34m{string.Join("\n", ctplayersName)}\u001b[0m\r\n```";
        //             tnames = !tplayersName.Any() ? "```ㅤ```" : $"```ansi\r\n\u001b[0;33m{string.Join("\n", tplayersName)}\u001b[0m\r\n```";
        //         }
        //         EmbedBuilder builder = new EmbedBuilder()
        //             .WithTitle(EConfig.Title)
        //             .AddField($"{EConfig.MapField}", $"```ansi\r\n\u001b[2;31m{_g.MapName}\u001b[0m\r\n```", inline: true)
        //             .AddField(EConfig.OnlineField, $"```ansi\r\n\u001b[2;31m{_g.PlayerList.Count}\u001b[0m/\u001b[2;32m{_g.MaxPlayers}\u001b[0m\r\n```", inline: true);
        //         _ = EConfig.PlayersInline ? builder.AddField("ㅤ", "​─────────────────────────────────────") : null;
        //         _ = builder
        //             .AddField(EConfig.CTField.Replace("{SCORE}", _g.CTScore.ToString()), ctnames, inline: EConfig.PlayersInline)
        //             .AddField(EConfig.TField.Replace("{SCORE}", _g.TScore.ToString()), tnames, inline: EConfig.PlayersInline)
        //             .AddField("ㅤ", _chores.IsURLValid(GConfig.PHPURL) ? $"[**`connect {_g.ServerIP}`**]({_g.ConnectURL})ㅤ{EConfig.JoinHere}" : $"**`connect {_g.ServerIP}`**ㅤ{EConfig.JoinHere}")
        //             .WithColor(_chores.GetEmbedColor())
        //             .WithCurrentTimestamp();
        //         _ = _chores.IsURLValid(_g.ConnectURL) ? builder.WithUrl(_g.ConnectURL) : null;
        //         _ = _chores.IsURLValid(EConfig.MapImg) ? builder.WithImageUrl(EConfig.MapImg.Replace("{MAPNAME}", _g.MapName)) : null;
        //         return builder.Build();
        //     }
        //     else
        //     {
        //         EmbedBuilder builder = new EmbedBuilder()
        //             .WithTitle(EConfig.Title)
        //             .AddField(EConfig.MapField, $"```ansi\r\n\u001b[2;31m{_g.MapName}\u001b[0m\r\n```", inline: true)
        //             .AddField(EConfig.OnlineField, $"```ansi\n [2;33m [2;31m{EConfig.ServerEmpty} [0m [2;33m [0m [2;33m [0m\n```", inline: true)
        //             .AddField("ㅤ", _chores.IsURLValid(GConfig.PHPURL) ? $"[**`connect {_g.ServerIP}`**]( {_g.ConnectURL})ㅤ{EConfig.JoinHere}" : $"**`connect {_g.ServerIP}`**ㅤ{EConfig.JoinHere}")
        //             .WithColor(_chores.GetEmbedColor())
        //             .WithCurrentTimestamp();
        //         _ = _chores.IsURLValid(_g.ConnectURL) ? builder.WithUrl(_g.ConnectURL) : null;
        //         _ = _chores.IsURLValid(EConfig.IdleImg) ? builder.WithImageUrl(EConfig.IdleImg.Replace("{MAPNAME}", _g.MapName)) : null;
        //         return builder.Build();
        //     }
        // }
        public Embed CreateStatusEmbed()
        {
            // Usa os nomes das equipes (ou um padrão se estiverem vazios)
            string ctTeamName = string.IsNullOrEmpty(_g.CTName) ? "Contra-Terroristas" : _g.CTName;
            string tTeamName = string.IsNullOrEmpty(_g.TName) ? "Terroristas" : _g.TName;

            var builder = new EmbedBuilder()
                .WithTitle("THE OWLS - PLACAR") // Título padrão para o status
                .WithColor(_chores.GetEmbedColor())
                .WithTimestamp(DateTimeOffset.UtcNow);

            // Adiciona a imagem do mapa se a URL for válida
            if (_chores.IsURLValid(EConfig.MapImg))
            {
                // Se não houver jogadores, usa a IdleImg (se configurada), senão a MapImg
                string imageUrl = EConfig.MapImg.Replace("{MAPNAME}", _g.MapName);
                if (!_g.PlayerList.Any() && _chores.IsURLValid(EConfig.IdleImg)) {
                    imageUrl = EConfig.IdleImg;
                }
                builder.WithImageUrl(imageUrl);
            }

            // Lógica para quando o servidor está COM jogadores
            if (_g.PlayerList.Any())
            {
                List<string> tplayersName = _g.TPlayersName;
                List<string> ctplayersName = _g.CtPlayersName;

                string tnames = !tplayersName.Any() ? "Nenhum jogador" : string.Join("\n", tplayersName);
                string ctnames = !ctplayersName.Any() ? "Nenhum jogador" : string.Join("\n", ctplayersName);

                builder.WithDescription($"**Mapa:** `{_g.MapName}`\n**Placar:** {ctTeamName} **{_g.CTScore}** vs **{_g.TScore}** {tTeamName}");
                
                builder.AddField($"{ctTeamName}", ctnames, true);
                builder.AddField($"{tTeamName}", tnames, true);
                builder.AddField("\u200B", "\u200B", false); // Adiciona um espaçamento para organização
            }
            // Lógica para quando o servidor está VAZIO
            else
            {
                builder.WithDescription($"**Mapa:** `{_g.MapName}`\n\nO servidor está aguardando jogadores!");
                builder.AddField("Jogadores Online", "Nenhum jogador conectado.", false);
            }

            // Informação de conexão (comum a ambos os casos)
            string connectInfo = _chores.IsURLValid(GConfig.PHPURL)
                ? $"[Conectar via Steam]({_g.ConnectURL})"
                : $"`connect {_g.ServerIP}`";
            builder.AddField("Servidor", connectInfo, false);
            
            return builder.Build();
        }


        public async Task RequestPlayers(string name)
        {
            List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.RequestPlayersURL);
            foreach (DiscordWebhookClient webhookClient in webhookClients)
            {
                if (webhookClient == null)
                {
                    continue;
                }

                EmbedBuilder builder = new EmbedBuilder()
                    .WithTitle(EConfig.Title)
                    .WithDescription($"||<@&{WConfig.NotifyMembersRoleID}>||\n```ansi\r\n\u001b[2;31m{name} {EConfig.RequestPlayers}\u001b[0m\r\n```")
                    .AddField($"{EConfig.MapField}", $"```ansi\r\n\u001b[2;31m{_g.MapName}\u001b[0m\r\n```", inline: true)
                    .AddField(EConfig.OnlineField, $"```ansi\r\n\u001b[2;31m{_g.PlayerList.Count}\u001b[0m/\u001b[2;32m{_g.MaxPlayers}\u001b[0m\r\n```", inline: true)
                    .AddField("ㅤ", _chores.IsURLValid(GConfig.PHPURL) ? $"[**`connect {_g.ServerIP}`**]({_g.ConnectURL})ㅤ{EConfig.JoinHere}" : $"**`connect {_g.ServerIP}`**ㅤ{EConfig.JoinHere}")
                    .WithColor(new Color(255, 0, 0))
                    .WithCurrentTimestamp();
                _ = _chores.IsURLValid(EConfig.RequestImg) ? builder.WithImageUrl(EConfig.RequestImg.Replace("{MAPNAME}", _g.MapName)) : null;
                
                using (webhookClient)
                {
                    await webhookClient.SendMessageAsync(
                        embeds: new[] { builder.Build() },
                        username: WebhookUsername,
                        avatarUrl: WebhookAvatarUrl);
                }
            }
        }

        public async Task NewMap(string mapname, int counts)
        {
            List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.NotifyWebhookURL);
            foreach (DiscordWebhookClient webhookClient in webhookClients)
            {
                if (webhookClient == null)
                {
                    continue;
                }

                EmbedBuilder builder = new EmbedBuilder()
                    .WithTitle(EConfig.Title)
                    .WithDescription($"```ansi\r\n\u001b[2;31m{EConfig.MapChange.Replace("{mapname}", mapname)}\u001b[0m\r\n```")
                    .AddField($"{EConfig.MapField}", $"```ansi\r\n\u001b[2;31m{mapname}\u001b[0m\r\n```", inline: true)
                    .AddField(EConfig.OnlineField, $"```ansi\r\n\u001b[2;31m{counts}\u001b[0m/\u001b[2;32m{_g.MaxPlayers}\u001b[0m\r\n```", inline: true)
                    .AddField("ㅤ", _chores.IsURLValid(GConfig.PHPURL) ? $"[**`connect {_g.ServerIP}`**]({_g.ConnectURL})ㅤ{EConfig.JoinHere}" : $"**`connect {_g.ServerIP}`**ㅤ{EConfig.JoinHere}")
                    .WithColor(_chores.GetEmbedColor())
                    .WithCurrentTimestamp();
                _ = _chores.IsURLValid(_g.ConnectURL) ? builder.WithUrl(_g.ConnectURL) : null;
                _ = _chores.IsURLValid(EConfig.MapImg) ? builder.WithImageUrl(EConfig.MapImg.Replace("{MAPNAME}", mapname)) : null;

                using (webhookClient)
                {
                    _ = await webhookClient.SendMessageAsync(
                        embeds: new[] { builder.Build() },
                        username: WebhookUsername,
                        avatarUrl: WebhookAvatarUrl);
                }
            }
        }

        // 2. NOVO LAYOUT PARA O PLACAR DE FIM DE PARTIDA
        // public async Task GameEnd(string mvp, List<string> tplayersName, List<string> ctplayersName)
        // {
        //     List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.ScoreboardURL);
        //     foreach (DiscordWebhookClient webhookClient in webhookClients)
        //     {
        //         if (webhookClient == null)
        //         {
        //             continue;
        //         }

        //         if (tplayersName.Any() || ctplayersName.Any())
        //         {
        //             string tnames = !tplayersName.Any() ? "Nenhum jogador" : string.Join("\n", tplayersName);
        //             string ctnames = !ctplayersName.Any() ? "Nenhum jogador" : string.Join("\n", ctplayersName);

        //             var builder = new EmbedBuilder()
        //                 .WithTitle($"Fim de Partida em {_g.MapName}")
        //                 .WithDescription($"**Placar Final:** CT **{_g.CTScore}** vs **{_g.TScore}** T")
        //                 .WithColor(_chores.GetEmbedColor())
        //                 .WithTimestamp(DateTimeOffset.UtcNow);

        //             builder.AddField("👑 MVP da Partida", mvp, false);
        //             builder.AddField($"🛡️ Contra-Terroristas ({_g.CTScore})", ctnames, true);
        //             builder.AddField($"💣 Terroristas ({_g.TScore})", tnames, true);
        //             builder.AddField("\u200B", "\u200B", false); // Campo em branco para espaçamento

        //             string connectInfo = _chores.IsURLValid(GConfig.PHPURL)
        //                 ? $"[Conectar via Steam]({_g.ConnectURL})"
        //                 : $"`connect {_g.ServerIP}`";
        //             builder.AddField("Servidor", connectInfo, false);

        //             if (_chores.IsURLValid(EConfig.MapImg))
        //             {
        //                 builder.WithImageUrl(EConfig.MapImg.Replace("{MAPNAME}", _g.MapName));
        //             }

        //             builder.WithFooter($"Jogadores na partida: {tplayersName.Count + ctplayersName.Count}/{_g.MaxPlayers}");

        //             using (webhookClient)
        //             {
        //                 await webhookClient.SendMessageAsync(
        //                     embeds: new[] { builder.Build() },
        //                     username: WebhookUsername,
        //                     avatarUrl: WebhookAvatarUrl
        //                 );
        //             }
        //         }
        //     }
        // }
        public async Task GameEnd(string mvp, List<string> tplayersName, List<string> ctplayersName)
        {
            List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.ScoreboardURL);
            foreach (DiscordWebhookClient webhookClient in webhookClients)
            {
                if (webhookClient == null)
                {
                    continue;
                }

                if (tplayersName.Any() || ctplayersName.Any())
                {
                    string tnames = !tplayersName.Any() ? "Nenhum jogador" : string.Join("\n", tplayersName);
                    string ctnames = !ctplayersName.Any() ? "Nenhum jogador" : string.Join("\n", ctplayersName);

                    // Usa os nomes das equipes (ou um padrão se estiverem vazios)
                    string ctTeamName = string.IsNullOrEmpty(_g.CTName) ? "Contra-Terroristas" : _g.CTName;
                    string tTeamName = string.IsNullOrEmpty(_g.TName) ? "Terroristas" : _g.TName;

                    var builder = new EmbedBuilder()
                        .WithTitle($"Fim de Partida `{_g.MapName}`")
                        .WithDescription($"**Placar**\n {ctTeamName} **{_g.CTScore}** vs **{_g.TScore}** {tTeamName}")
                        .WithColor(_chores.GetEmbedColor())
                        .WithTimestamp(DateTimeOffset.UtcNow); // Define o rodapé apenas com o horário

                    builder.AddField("👑 MVP da Partida", mvp, false);
                    builder.AddField($"{ctTeamName}", ctnames, true);
                    builder.AddField($"{tTeamName}", tnames, true);

                    string connectInfo = _chores.IsURLValid(GConfig.PHPURL)
                        ? $"[Conectar via Steam]({_g.ConnectURL})"
                        : $"`connect {_g.ServerIP}`";
                    builder.AddField("Servidor", connectInfo, false);

                    if (_chores.IsURLValid(EConfig.MapImg))
                    {
                        builder.WithImageUrl(EConfig.MapImg.Replace("{MAPNAME}", _g.MapName));
                    }
                    
                    // O rodapé agora só mostra o timestamp (horário)
                    // A linha .WithFooter() foi removida e .WithTimestamp() já cuida disso.

                    using (webhookClient)
                    {
                        await webhookClient.SendMessageAsync(
                            embeds: new[] { builder.Build() },
                            username: WebhookUsername,
                            avatarUrl: WebhookAvatarUrl
                        );
                    }
                }
            }
        }

        public async Task ServerOffiline()
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            List<DiscordWebhookClient> webhookClients = CreateWebhookClients(WConfig.StatusWebhookURL);
            foreach (DiscordWebhookClient webhookClient in webhookClients)
            {
                if (webhookClient == null)
                {
                    continue;
                }
                await webhookClient.ModifyMessageAsync(_g.MessageID, properties =>
                {
                    EmbedBuilder builder = new EmbedBuilder()
                    .WithTitle(EConfig.Title + " (Offline)")
                    .WithDescription($"```ansi\r\n\u001b[2;31mOffline since: \u001b[0m\r\n```<t:{timestamp}:R>")
                    .WithColor(new Color(255, 0, 0))
                    .WithCurrentTimestamp();
                    _ = _chores.IsURLValid(EConfig.OfflineImg) ? builder.WithImageUrl(EConfig.OfflineImg) : null;
                    properties.Embeds = new[] { builder.Build() };
                });
                using (webhookClient)
                {
                    // Additional logic if needed after modifying the message
                }
            }
        }
    }
}