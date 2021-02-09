using System.ComponentModel;

namespace Celeste.Mod.CollabTeleport
{
    public class CollabTeleportSettings : EverestModuleSettings
    {
        public enum FilterType { ClearOnly, DeathlessBerry, GoldSpeedberry, BothBerries }

        [SettingName("Auto-Teleport on Level Complete")]
        public bool AutoTeleportOnComplete { get; set; } = false;

        [SettingName("Ignore Level by Criterion")]
        public FilterType IgnoreLevelBy { get; set; } = FilterType.ClearOnly;
    }
}
