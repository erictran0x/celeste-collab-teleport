using System;
using System.Collections.Generic;
using System.Text;

namespace Celeste.Mod.CollabTeleport
{
    public class CollabTeleportSettings : EverestModuleSettings
    {
        [SettingName("Auto-Teleport on Level Complete")]
        public bool AutoTeleportOnComplete { get; set; } = false;
    }
}
