using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace FunnyHostages
{
    public partial class FunnyHostages
    {
        private void DebugPrint(string message)
        {
            if (Config.Debug)
            {
                Console.WriteLine(Localizer["core.debugprint"].Value.Replace("{message}", message));
            }
        }

        private static double LengthSq(Vector a, Vector b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        private string GetRandomSound(CCSPlayerController? nearestPlayer, string hostageType, List<string> defaultSounds)
        {
            if (nearestPlayer == null)
            {
                return GetRandomElement(defaultSounds);
            }

            DebugPrint($"Found nearest player: {nearestPlayer.PlayerName} at {nearestPlayer.AbsOrigin}");

            var sounds = nearestPlayer.Team switch
            {
                CsTeam.CounterTerrorist => GetHostageSounds(hostageType, "nearby_ct"),
                CsTeam.Terrorist => GetHostageSounds(hostageType, "nearby_t"),
                _ => null
            };

            return sounds != null && sounds.Count > 0
                ? GetRandomElement(sounds)
                : GetRandomElement(defaultSounds);
        }

        private List<string>? GetHostageSounds(string hostageType, string key)
        {
            return Config.Hostages[hostageType].Sounds.TryGetValue(key, out var sounds) ? sounds : null;
        }

        private static string GetRandomElement(List<string> list)
        {
            return list[new Random().Next(list.Count)];
        }
    }
}