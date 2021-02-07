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
        public Dictionary<string, EntityID> silverBerries;
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
            string levelset = null;

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

                // Ignore gyms and lobbies
                if (name.Contains("/0-Gyms/") || name.Contains("/0-Lobbies/"))
                    continue;

                AreaKey ak = AreaData.Get(name).ToKey();
                levelset = ak.LevelSet;

                if (!foundAreas.ContainsKey(name))
                {
                    foundAreas.Add(name, SaveData.Instance.GetAreaStatsFor(ak));
                    levelnameToDirectory.Add(Dialog.Get(name).ToLower(), name);
                }
            }

            // Get silver and speed berry data
            silverBerries = CollabUtils2Helper.GetAllSilverBerries(levelset);
            // TODO: get speed berry data

            // Only auto-teleport player once
            if (currentPlayer != null && !autoTPed && Settings.AutoTeleportOnComplete)
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
