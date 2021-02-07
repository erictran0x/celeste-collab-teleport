namespace Celeste.Mod.CollabTeleport
{
    public class CollabTeleportSettings : EverestModuleSettings
    {
        [SettingName("Auto-Teleport on Level Complete")]
        public bool AutoTeleportOnComplete { get; set; } = false;
    }
}
