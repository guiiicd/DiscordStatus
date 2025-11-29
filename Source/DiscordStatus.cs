using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Threading.Tasks;
using System.Linq;
using DiscordStatus.Utils;
using System; // Necessário para Exception

namespace DiscordStatus
{
    public partial class DiscordStatus : BasePlugin, IPluginConfig<DSconfig>
    {
        private System.Timers.Timer? _update;

        private readonly IWebhook _webhook;
        private readonly IQuery _query;
        private readonly IChores _chores;
        private readonly Globals _g;
        private bool init = false;

        // --- MODIFICAÇÃO (PARTE 2) ---
        // Variável estática para acessar a API da Steam de qualquer lugar do plugin
        public static SteamAPI? SteamApiHelper;
        // -----------------------------

        public DSconfig Config { get; set; }

        public DiscordStatus(IWebhook webhook, IQuery query, IChores chores, Globals g)
        {
            _webhook = webhook;
            _query = query;
            _chores = chores;
            _g = g;
            // Inicializa a Config com um valor padrão para satisfazer o compilador.
            Config = new DSconfig();
        }

        public override async void Load(bool hotReload)
        {
            // --- MODIFICAÇÃO (PARTE 2) ---
            // Inicializa o Helper da Steam para capturar o SDR assim que o plugin carregar
            try 
            {
                SteamApiHelper = new SteamAPI();
                SteamApiHelper.Initialize();
                // Usando DSLog para manter o padrão do seu plugin
                DSLog.Log(1, "SteamAPI Helper initialized for SDR.");
            }
            catch (Exception ex)
            {
                DSLog.Log(2, $"Failed to initialize SteamAPI Helper: {ex.Message}");
            }
            // -----------------------------

            Server.NextFrame(() =>
            {
                _g.MapName = Server.MapName;
            });
            RegisterListeners();
            if (!hotReload)
            {
                if (string.IsNullOrEmpty(_g.MapName))
                {
                    DSLog.Log(2, "Map Invalid, Waiting Listeners");
                }
                else
                {
                    await LoadDiscordStatusAsync();
                    DSLog.Log(0, $"Map valid ({_g.MapName}), starting bot!");
                }
                DSLog.Log(1, $"{ModuleName} {ModuleVersion} Loaded");
            }
            else
            {
                DSLog.Log(0, "Hot Reloading, try starting bot!");
                await LoadDiscordStatusAsync();
            }
        }

        public override void Unload(bool hotReload)
        {
            _webhook.ServerOffiline();
            _update?.Stop();
            _update?.Dispose();
            DSLog.Log(2, $"{ModuleName} version {ModuleVersion} unloaded");
        }

        public void OnConfigParsed(DSconfig config)
        {
            ConfigManager.GetPath(ModuleDirectory, ModuleName);
            if (config.Version < _g.Config.Version)
            {
                DSLog.Log(2, $"Config version mismatch (Expected: {_g.Config.Version} | Current: {config.Version})");
                Task.Run(async () => await ConfigManager.RenameAsync(_g.Config));
                DSLog.Log(1, "Renamed old one, go update your config");
            }
            else
            {
                Config = config;
                _g.Config = Config;
                _g.GConfig = Config.GeneralConfig;
                _g.WConfig = Config.WebhookConfig;
                _g.EConfig = Config.EmbedConfig;
                _g.NameFormat = _g.EConfig.NameFormat;
                
                // Força a desativação da busca de país, já que as flags foram removidas.
                _g.HasCC = false;
                _g.HasRC = _g.EConfig.NameFormat.Contains("{RC}");

                _g.ServerIP = _g.GConfig.ServerIP;
                DSLog.Log(1, "Finished loading config file");
            }
        }

        private async Task LoadDiscordStatusAsync()
        {
            DSLog.Log(0, "Starting~");
            _g.ConnectURL = _chores.IsURLValid(_g.GConfig.PHPURL) ? string.Concat(_g.GConfig.PHPURL, $"?ip={_g.ServerIP}") : "ConnectURL Error";
            await _webhook.InitialMessageAsync();
            if (_g.MessageID != 0)
            {
                // Timer de atualização periódica desativado conforme solicitado
                // _update = new System.Timers.Timer(TimeSpan.FromSeconds(_g.GConfig.UpdateInterval).TotalMilliseconds);
                // _update.Elapsed += async (sender, e) => await UpdateAsync();
                // _update.Start();
                DSLog.Log(1, "Initialization completed successfully! (Periodic updates disabled)");
            }
        }

        public async Task UpdateAsync()
        {
            await Task.Run(() =>
            {
                Server.NextFrame(() =>
                {
                    _g.MapName = Server.MapName;

                    var _players = Utilities.GetPlayers().Where(p => _chores.IsPlayerValid(p));
                    foreach (var player in _players)
                    {
                        _chores.UpdatePlayer(player);
                    }

                    var players = _g.PlayerList;

                    if (players.Count > 0)
                    {
                        _chores.SortPlayers();

                        var tPlayerList = players
                            .Where(kv => kv.Value != null && kv.Value.TeamID == 2)
                            .Select(kv => _chores.FormatStats(kv.Value));

                        var ctPlayerList = players
                            .Where(kv => kv.Value != null && kv.Value.TeamID == 3)
                            .Select(kv => _chores.FormatStats(kv.Value));

                        _g.TPlayersName.AddRange(tPlayerList);
                        _g.CtPlayersName.AddRange(ctPlayerList);
                    }
                });
            });

            await _webhook.UpdateEmbed();
        }
    }
}