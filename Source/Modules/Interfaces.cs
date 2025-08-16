using CounterStrikeSharp.API.Core;
using Discord;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordStatus
{
    public interface IWebhook
    {
        Task InitialMessageAsync();
        Embed CreateStatusEmbed();
        Task ServerOffiline();
        Task UpdateEmbed();
        Task RequestPlayers(string name);
        Task GameEnd(string mvp, List<string> tPlayers, List<string> ctPlayers);
        Task NewMap(string mapname, int playercounts);
    }

    public interface IQuery
    {
        Task<string> GetCountryCodeAsync(string ipAddress);
        Task<string> IPQueryAsync(string ipAddress, string endpoint);
    }

    public interface IChores
    {
        void InitPlayers(CCSPlayerController player);
        bool IsPlayerValid(CCSPlayerController? player);
        bool IsURLValid(string? url);
        Color GetEmbedColor();
        void GetScore(IEnumerable<CCSTeam> teams);
        void SortPlayers();
        string FormatStats(PlayerInfo playerinfo);
        string FormatStatsForScoreboard(PlayerInfo playerinfo); // <-- ADICIONADO AQUI
        void UpdatePlayer(CCSPlayerController updatedPlayer);
    }
}