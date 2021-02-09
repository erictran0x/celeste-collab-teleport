using System.ComponentModel;

namespace Celeste.Mod.CollabTeleport
{
    public class CollabTeleportSettings : EverestModuleSettings
    {
        public enum FilterType { ClearOnly, DeathlessBerry, GoldSpeedberry, BothBerries }

        public bool AutoTeleportOnComplete { get; set; } = false;

        public FilterType IgnoreLevelBy { get; set; } = FilterType.ClearOnly;
    }
}
