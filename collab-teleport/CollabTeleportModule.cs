using System;
using System.Collections.Generic;

namespace Celeste.Mod.CollabTeleport
{
    public class CollabTeleportModule : EverestModule
    {
        public static CollabTeleportModule Instance;
        private int fileSlot = -9999;

        public Dictionary<string, AreaStats> foundAreas;
        public List<EntityData> collabChapters;
        public Level currentLevel;

        public CollabTeleportModule()
        {
            Instance = this;
            foundAreas = new Dictionary<string, AreaStats>();
            collabChapters = new List<EntityData>();
        }

        public override void Load()
        {
            Everest.Events.Level.OnLoadLevel += OnLevelLoad;
        }

        public override void Unload()
        {
            Everest.Events.Level.OnLoadLevel -= OnLevelLoad;
        }

        public void OnLevelLoad(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
        {
            // Check if file slot is the same - clear found areas if not
            if (fileSlot != SaveData.Instance.FileSlot)
            {
                fileSlot = SaveData.Instance.FileSlot;
                foundAreas.Clear();
            }

            currentLevel = level;

            // Get all ChapterPanelTriggers from current level
            collabChapters = level.Session.LevelData.Triggers.FindAll(t => t.Name.Equals("CollabUtils2/ChapterPanelTrigger"));

            // Map from map name to its AreaStats object
            foreach (EntityData t in collabChapters)
            {
                string name = t.Attr("map");
                AreaStats area = null;
                if (!foundAreas.TryGetValue(name, out area))
                {
                    // Find the area we want (might want to store nearby ones)
                    string dir = name.Substring(0, name.LastIndexOf("/"));
                    LevelSetStats lss = SaveData.Instance.GetLevelSetStatsFor(dir);
                    foreach (AreaStats a in lss.Areas)
                    {
                        // Ignore gyms and lobbies
                        if (a.SID.Contains("/0-Gyms/") || a.SID.Contains("/0-Lobbies/"))
                            continue;

                        foundAreas.Add(a.SID, a);
                        if (a.SID.Equals(name))
                            area = a;
                    }
                }
            }
        }
    }
}
