using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace FunnyHostages
{
    public partial class FunnyHostages : BasePlugin
    {
        public override string ModuleName => "CS2 FunnyHostages";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        private readonly Dictionary<int, Dictionary<string, string>> _hostages = new();
        private readonly Random _random = new();
        private bool _isDuringRound = false;

        public override void Load(bool hotReload)
        {
            RegisterListener<Listeners.OnTick>(OnTick);
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventHostageFollows>(OnHostageFollows);
            RegisterEventHandler<EventHostageStopsFollowing>(OnHostageStopsFollowing);

            if (hotReload && Config.Enabled)
            {
                FindHostages();
                _isDuringRound = true;
            }
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<Listeners.OnTick>(OnTick);
            DeregisterEventHandler<EventRoundStart>(OnRoundStart);
            DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
            DeregisterEventHandler<EventHostageFollows>(OnHostageFollows);
            DeregisterEventHandler<EventHostageStopsFollowing>(OnHostageStopsFollowing);
        }

        private void OnTick()
        {
            if (!Config.Enabled || !_isDuringRound || _hostages.Count == 0) return;

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var hostageCopy = new Dictionary<int, Dictionary<string, string>>(_hostages);

            foreach (var (hostageId, hostageData) in hostageCopy)
            {
                var entity = Utilities.GetEntityFromIndex<CHostage>(hostageId);
                if (entity == null || !entity.IsValid || entity.AbsOrigin == null)
                {
                    DebugPrint($"Hostage {hostageId} is dead or not found. Removing from list.");
                    _hostages.Remove(hostageId);
                    continue;
                }

                if (int.Parse(hostageData["next_random_sound"]) >= currentTime) continue;

                if (!Config.Hostages[hostageData["type"]].Sounds.TryGetValue("on_call", out var soundsCall)) continue;

                var nearestPlayer = Utilities.GetPlayers()
                    .Where(p => p.IsValid && p.PawnIsAlive && !p.IsHLTV && p.Pawn?.Value?.AbsOrigin != null)
                    .Where(p => LengthSq(p.Pawn.Value!.AbsOrigin!, entity.AbsOrigin!) <= Config.Hostages[hostageData["type"]].MaxDistance)
                    .OrderBy(p => LengthSq(p.Pawn.Value!.AbsOrigin!, entity.AbsOrigin!))
                    .FirstOrDefault();

                var randomSound = GetRandomSound(nearestPlayer, hostageData["type"], soundsCall);
                DebugPrint($"Hostage {hostageId} is alive. Playing {randomSound}.");
                entity.EmitSound(randomSound);

                _hostages[hostageId]["next_random_sound"] = (currentTime + _random.Next(
                    Config.Hostages[hostageData["type"]].MinWaitTime,
                    Config.Hostages[hostageData["type"]].MaxWaitTime)).ToString();
            }
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            if (!Config.Enabled) return HookResult.Continue;
            Server.NextFrame(() =>
            {
                FindHostages();
                if (_hostages.Count > 0) _isDuringRound = true;
            });
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            _isDuringRound = false;
            return HookResult.Continue;
        }

        private HookResult OnHostageFollows(EventHostageFollows @event, GameEventInfo info)
        {
            return HandleHostageEvent(@event.Hostage, "on_follow");
        }

        private HookResult OnHostageStopsFollowing(EventHostageStopsFollowing @event, GameEventInfo info)
        {
            return HandleHostageEvent(@event.Hostage, "on_stop");
        }

        private HookResult HandleHostageEvent(int hostageId, string soundKey)
        {
            if (!Config.Enabled || !_isDuringRound || _hostages.Count == 0 || !_hostages.ContainsKey(hostageId)) return HookResult.Continue;

            var entity = Utilities.GetEntityFromIndex<CHostage>(hostageId);
            if (entity == null || !entity.IsValid)
            {
                DebugPrint($"Hostage {hostageId} is dead or not found. Removing from list.");
                _hostages.Remove(hostageId);
                return HookResult.Continue;
            }

            if (Config.Hostages[_hostages[hostageId]["type"]].Sounds.TryGetValue(soundKey, out var sounds))
            {
                var randomSound = sounds.ElementAt(_random.Next(sounds.Count));
                DebugPrint($"Hostage {hostageId} triggered {soundKey}. Playing {randomSound}.");
                entity.EmitSound(randomSound);

                _hostages[hostageId]["next_random_sound"] = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _random.Next(
                    Config.Hostages[_hostages[hostageId]["type"]].MinWaitTime,
                    Config.Hostages[_hostages[hostageId]["type"]].MaxWaitTime)).ToString();
            }

            return HookResult.Continue;
        }

        private void FindHostages()
        {
            DebugPrint("Finding hostages...");
            _hostages.Clear();

            if (Config.Hostages.Count == 0)
            {
                DebugPrint("No random hostage(s) in configuration. Skipping task.");
                return;
            }

            var hostages = Utilities.FindAllEntitiesByDesignerName<CHostage>("hostage_entity");
            foreach (var hostage in hostages)
            {
                DebugPrint($"Found hostage {hostage.Index} at {hostage.AbsOrigin}");
                var availableHostages = Config.Hostages.Keys.Except(_hostages.Values.Select(h => h["type"])).ToList();
                if (!availableHostages.Any())
                {
                    DebugPrint("No unique hostage types available. Skipping this hostage.");
                    continue;
                }
                var randomHostageKey = availableHostages[_random.Next(availableHostages.Count)];
                var randomHostage = Config.Hostages[randomHostageKey];
                _hostages.Add((int)hostage.Index, new Dictionary<string, string>
                {
                    { "type", randomHostageKey },
                    { "next_random_sound", (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _random.Next(
                    Config.Hostages[randomHostageKey].MinWaitTime,
                    Config.Hostages[randomHostageKey].MaxWaitTime)).ToString() }
                });
            }
        }
    }
}
