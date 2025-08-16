using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using System;
using System.Threading.Tasks;

namespace DiscordStatus
{
    public partial class DiscordStatus
    {
        private DateTime _globalCooldown = DateTime.MinValue;
        private readonly TimeSpan _globalCooldownDuration = TimeSpan.FromSeconds(60);

        [ConsoleCommand("css_request", "Request players from discord")]
        [ConsoleCommand("css_need", "Request players from discord")]
        public async void RequestPlayers(CCSPlayerController? player, CommandInfo command)
        {
            // A verificação `_chores.IsPlayerValid(player)` já garante que `player` não é nulo.
            if (_chores.IsPlayerValid(player))
            {
                if (IsGlobalCooldownActive())
                {
                    DSLog.LogToChat(player, "{RED}Já estamos chamando um complete, por favor aguarde");
                    return;
                }
                // Como IsPlayerValid é true, podemos acessar PlayerName com segurança.
                await _webhook.RequestPlayers(player!.PlayerName);
                SetGlobalCooldown();
                DSLog.LogToChat(player, "{GREEN}Estamos chamando um complete");
            }
            else
            {
                await _webhook.RequestPlayers("Admin");
                DSLog.Log(1, $"Estamos chamando um complete");
            }
        }

        [ConsoleCommand("css_update_names", "Update Name formats and save it to config")]
        [CommandHelper(minArgs: 1, usage: "[css_update_names {FLAG} {NAME}: KD | {KD}]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/root")]
        public async void UpdateNames(CCSPlayerController? player, CommandInfo command)
        {
            _update?.Stop(); // Usa o operador '?' para parar o timer apenas se ele não for nulo.
            var names = command.ArgString;
            _g.NameFormat = names;
            await ConfigManager.SaveAsync("EmbedConfig", "NameFormat", names);
            await UpdateAsync();
            _update?.Start(); // Usa o operador '?' para iniciar o timer apenas se ele não for nulo.
            if (!_chores.IsPlayerValid(player)) return;
            DSLog.LogToChat(player, $"{{GREEN}}Name format updated to '{names}'!");
        }

        [ConsoleCommand("css_update_settings", "update config settings")]
        [RequiresPermissions("@css/root")]
        public async void UpdateSettings(CCSPlayerController? player, CommandInfo command)
        {
            _update?.Stop(); // Usa o operador '?' para parar o timer apenas se ele não for nulo.
            await ConfigManager.UpdateAsync(_g);
            await UpdateAsync();
            _update?.Start(); // Usa o operador '?' para iniciar o timer apenas se ele não for nulo.
            if (!_chores.IsPlayerValid(player)) return;
            DSLog.LogToChat(player, $"color: {_g.EConfig.RandomColor}{_chores.GetEmbedColor()}");
            DSLog.LogToChat(player, "{GREEN}Updated config settings!");
        }

        private bool IsGlobalCooldownActive()
        {
            return DateTime.Now - _globalCooldown < _globalCooldownDuration;
        }

        private void SetGlobalCooldown()
        {
            _globalCooldown = DateTime.Now;
        }
    }
}
