using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using System.Text.Json.Serialization;

namespace FunnyHostages
{
    public class HostagesConfig
    {
        // hostage min wait time after sound
        [JsonPropertyName("min_wait_time")] public int MinWaitTime { get; set; } = 8;
        // hostage max wait time after sound
        [JsonPropertyName("max_wait_time")] public int MaxWaitTime { get; set; } = 20;
        // max distance for nearest player
        [JsonPropertyName("max_distance")] public int MaxDistance { get; set; } = 1000;
        // hostage sounds
        [JsonPropertyName("sounds")] public Dictionary<string, List<string>> Sounds { get; set; } = [];
    }

    public class PluginConfig : BasePluginConfig
    {
        // disabled
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // debug prints
        [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
        // info messages
        [JsonPropertyName("hostages")] public Dictionary<string, HostagesConfig> Hostages { get; set; } = [];
    }

    public partial class FunnyHostages : BasePlugin, IPluginConfig<PluginConfig>
    {
        public required PluginConfig Config { get; set; }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            // sort messages by key
            Config.Hostages = Config.Hostages.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            // update config and write new values from plugin to config file if changed after update
            Config.Update();
            Console.WriteLine(Localizer["core.config"]);
        }
    }
}
