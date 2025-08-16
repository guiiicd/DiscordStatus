namespace DiscordStatus
{
    using CounterStrikeSharp.API.Core;

    public sealed partial class DiscordStatus : BasePlugin
    {
        public override string ModuleName => "DiscordStatus";
        public override string ModuleVersion => "v3.4";
        public override string ModuleAuthor => "Guilheme";
        public override string ModuleDescription => "Showing Server Status on Discord";
    }
}