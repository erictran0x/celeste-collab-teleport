using System;
using System.Collections.Generic;

namespace Celeste.Mod.CollabTeleport
{
    public class CollabTeleportModule : EverestModule
    {
        public static CollabTeleportModule Instance;
        public override Type SettingsType => typeof(CollabTeleportSettings);
        public static CollabTeleportSettings Settings => (CollabTeleportSettings)Instance._Settings;

        private bool autoTPed = false;

        public Dictionary<string, AreaStats> foundAreas;
        public Dictionary<string, string> levelnameToDirectory;
        public List<EntityData> collabChapters;
        public Level currentLevel;
        private Player currentPlayer;

        public CollabTeleportModule()
        {
            Instance = this;
            foundAreas = new Dictionary<string, AreaStats>();
            levelnameToDirectory = new Dictionary<string, string>();
            collabChapters = new List<EntityData>();
        }

        public override void Load()
        {
            Everest.Events.Level.OnLoadLevel += OnLevelLoad;
            Everest.Events.Level.OnExit += OnLevelExit;
            Everest.Events.Player.OnSpawn += OnPlayerSpawn;
        }

        public override void Unload()
        {
            Everest.Events.Level.OnLoadLevel -= OnLevelLoad;
            Everest.Events.Level.OnExit -= OnLevelExit;
            Everest.Events.Player.OnSpawn -= OnPlayerSpawn;
        }

        private void OnLevelLoad(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
        {
            // Init current status
            foundAreas.Clear();
            levelnameToDirectory.Clear();
            currentLevel = level;

            // Only handle lobbies
            if (!level.Session.Area.SID.Contains("/0-Lobbies/"))
            {
                autoTPed = false;
                return;
            }

            // Get all ChapterPanelTriggers from current level
            collabChapters = level.Session.LevelData.Triggers.FindAll(t => t.Name.Equals("CollabUtils2/ChapterPanelTrigger"));

            // Map from map name to its AreaStats object
            foreach (EntityData t in collabChapters)
            {
                string name = t.Attr("map");
                if (!foundAreas.ContainsKey(name))
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
                        levelnameToDirectory.Add(Dialog.Get(a.SID).ToLower(), a.SID);
                    }
                }
            }

            // Only auto-teleport player once
            if (currentPlayer != null && !autoTPed && level.Session.Area.SID.Contains("/0-Lobbies/") && Settings.AutoTeleportOnComplete)
            {
                CollabTeleportCommand.TeleportToCollabLevel(currentPlayer, (string)null, false);
                autoTPed = true;
            }
        }

        private void OnLevelExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow)
        {
            autoTPed = false;
        }

        private void OnPlayerSpawn(Player obj)
        {
            currentPlayer = obj;
        }
    }
}
