using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordStatus
{
    public class Chores : IChores
    {
        private readonly Globals _g;
        private readonly IQuery _query;
        private EmbedConfig EConfig => _g.EConfig;

        public Chores(Globals globals, IQuery query)
        {
            _g = globals;
            _query = query;
        }

        public string FormatStats(PlayerInfo playerinfo)
        {
            var nameBuilder = new StringBuilder(_g.NameFormat);
            nameBuilder.Replace("{NAME}", playerinfo.Name ?? "Player");
            nameBuilder.Replace("{K}", playerinfo.Kills.ToString());
            nameBuilder.Replace("{D}", playerinfo.Deaths.ToString());
            nameBuilder.Replace("{A}", playerinfo.Assists.ToString());
            nameBuilder.Replace("{KD}", playerinfo.KD);
            nameBuilder.Replace("{CLAN}", playerinfo.Clan ?? "");
            nameBuilder.Replace("{CC}", playerinfo.Country ?? "");
            nameBuilder.Replace("{FLAG}", $":flag_{playerinfo.Country?.ToLower()}:");
            nameBuilder.Replace("{RC}", playerinfo.Region ?? "");
            
            var formattedName = nameBuilder.ToString().Replace(" - ", " / ");

            if (EConfig.EmbedSteamLink)
            {
                formattedName = $"[{formattedName}](https://steamcommunity.com/profiles/{playerinfo.SteamId})";
            }
            return formattedName;
        }

        public string FormatStatsForScoreboard(PlayerInfo playerinfo)
        {
            const int nameWidth = 16;
            string playerName = playerinfo.Name ?? "Unknown";
            playerName = playerName.Length > nameWidth ? playerName.Substring(0, nameWidth - 1) + "…" : playerName;

            string namePadded = playerName.PadRight(nameWidth);
            string killsPadded = (playerinfo.Kills?.ToString() ?? "0").PadLeft(3);
            string assistsPadded = (playerinfo.Assists?.ToString() ?? "0").PadLeft(3);
            string deathsPadded = (playerinfo.Deaths?.ToString() ?? "0").PadLeft(3);
            string scorePadded = (playerinfo.Score?.ToString() ?? "0").PadLeft(5);

            return $"{namePadded} {killsPadded} {assistsPadded} {deathsPadded} {scorePadded}";
        }

        public Color GetEmbedColor()
        {
            if (EConfig.RandomColor)
            {
                byte[] randomBytes = new byte[3];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }
                return new Color(randomBytes[0], randomBytes[1], randomBytes[2]);
            }
            else
            {
                return new Color(uint.Parse(EConfig.EmbedColor.TrimStart('#'), System.Globalization.NumberStyles.HexNumber));
            }
        }

        public void GetScore(IEnumerable<CCSTeam> Teams)
        {
            foreach (var team in Teams)
            {
                if (team.TeamNum == 2)
                {
                    _g.TScore = team.Score;
                    _g.TName = team.ClanTeamname;
                }
                else if (team.TeamNum == 3)
                {
                    _g.CTScore = team.Score;
                    _g.CTName = team.ClanTeamname;
                }
            }
        }

        public void InitPlayers(CCSPlayerController player)
        {
            if (!IsPlayerValid(player)) return;
            PlayerInfo playerInfo = new()
            {
                UserId = player?.UserId,
                SteamId = player?.AuthorizedSteamID?.SteamId64.ToString(),
                Name = player?.PlayerName,
                IpAddress = player?.IpAddress?.Split(":")[0],
                Clan = player?.Clan
            };
            if (_g.HasRC)
            {
                Task.Run(async () => playerInfo.Region = await _query.IPQueryAsync(playerInfo.IpAddress ?? "", "region_code").ConfigureAwait(false) ?? string.Empty);
            }
            if (_g.HasCC)
            {
                Task.Run(async () => playerInfo.Country = await _query.GetCountryCodeAsync(playerInfo.IpAddress ?? "").ConfigureAwait(false) ?? string.Empty);
            }
            _g.PlayerList[player.Slot] = playerInfo;
        }

        public bool IsPlayerValid(CCSPlayerController? player)
        {
            return player is { IsValid: true, IsBot: false, IsHLTV: false };
        }

        public bool IsURLValid(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        public void SortPlayers()
        {
            _g.TPlayersName.Clear();
            _g.CtPlayersName.Clear();
            _g.PlayerList = _g.PlayerList.OrderByDescending(x => x.Value.Kills).ToDictionary(x => x.Key, x => x.Value);
        }

        public void UpdatePlayer(CCSPlayerController updatedPlayer)
        {
            // Usamos o operador '?' para acessar MatchStats de forma segura.
            var matchStats = updatedPlayer.ActionTrackingServices?.MatchStats;
            var kills = matchStats?.Kills ?? 0;
            var deaths = matchStats?.Deaths ?? 0;
            var assists = matchStats?.Assists ?? 0;
            var score = updatedPlayer.Score;
            var clan = updatedPlayer.Clan ?? "";
            var TeamID = updatedPlayer.TeamNum;
            string kdRatio = deaths != 0 ? (kills / (double)deaths).ToString("G2") : kills.ToString();
            
            if (_g.PlayerList.TryGetValue(updatedPlayer.Slot, out var existingPlayer))
            {
                existingPlayer.Score = score;
                existingPlayer.Kills = kills;
                existingPlayer.Deaths = deaths;
                existingPlayer.Assists = assists;
                existingPlayer.Clan = clan;
                existingPlayer.KD = kdRatio;
                existingPlayer.TeamID = TeamID;
            }
        }
    }
}