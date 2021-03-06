﻿using System;
using System.Text;
using System.Collections.Generic;
using FilterType = Celeste.Mod.CollabTeleport.CollabTeleportSettings.FilterType;

namespace Celeste.Mod.CollabTeleport
{
    public class CollabTeleportModule : EverestModule
    {
        public static CollabTeleportModule Instance;
        public override Type SettingsType => typeof(CollabTeleportSettings);
        public static CollabTeleportSettings Settings => (CollabTeleportSettings)Instance._Settings;

        public List<EntityData> collabChapters;
        public Level currentLevel;
        public string currentLevelSet;

        private bool autoTPed = false;
        private Player currentPlayer;
        private Dictionary<string, AreaKey> foundAreas;
        private Dictionary<string, Dictionary<string, string>> levelnameToDirectory;
        private Dictionary<string, EntityID> silverBerries;
        private Dictionary<string, CollabUtils2Helper.SpeedBerryInfo> speedBerries;
        private Dictionary<string, long> pbTimes;

        public CollabTeleportModule()
        {
            Instance = this;
            foundAreas = new Dictionary<string, AreaKey>();
            levelnameToDirectory = new Dictionary<string, Dictionary<string, string>>();
            speedBerries = new Dictionary<string, CollabUtils2Helper.SpeedBerryInfo>();
            pbTimes = new Dictionary<string, long>();
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
            currentLevel = level;

            // Only handle lobbies
            if (!CollabUtils2Helper.IsLobby(level.Session.Area.SID))
                return;

            // Get all ChapterPanelTriggers from current level, ignoring gyms and lobbies
            collabChapters = level.Session.LevelData.Triggers.FindAll(t =>
                t.Name.Equals("CollabUtils2/ChapterPanelTrigger") && !CollabUtils2Helper.IsGym(t.Attr("map")) && !CollabUtils2Helper.IsLobby(t.Attr("map"))
            );

            // Map from map name to its AreaKey object
            bool notFound = false;
            foreach (EntityData t in collabChapters)
            {
                string name = t.Attr("map");

                AreaKey ak = AreaData.Get(name).ToKey();
                currentLevelSet = ak.LevelSet;

                if (!foundAreas.ContainsKey(name))
                    foundAreas.Add(name, ak);

                if (!levelnameToDirectory.ContainsKey(currentLevelSet))
                {
                    levelnameToDirectory.Add(currentLevelSet, new Dictionary<string, string>());
                    notFound = true;
                }
                else if (!notFound)
                    break;

                CollabUtils2Helper.SpeedBerryInfo? sb = CollabUtils2Helper.GetSpeedBerryInfo(name);
                if (sb.HasValue && !speedBerries.ContainsKey(name))
                    speedBerries.Add(name, sb.Value);

                string dialogKey = Dialog.Get(name).ToLower().Replace(' ', '_');

                // Handle case where there are non-ASCII-printable chars in name
                StringBuilder dialogKeySB = new StringBuilder(dialogKey);
                for (int i = 0; i < dialogKey.Length; ++i)
                {
                    if (dialogKeySB[i] < 32 || dialogKeySB[i] > 127)
                        dialogKeySB[i] = '*';
                }
                dialogKey = dialogKeySB.ToString();

                // Handle case where two or more collab maps have the same name
                string origDK = dialogKey;
                bool sameName = true;
                int numIters = 0;
                do
                {
                    if (!levelnameToDirectory[currentLevelSet].ContainsKey(dialogKey))
                    {
                        levelnameToDirectory[currentLevelSet].Add(dialogKey, name);
                        sameName = false;
                    }
                    else
                        dialogKey = $"{origDK}({++numIters})";
                } while (sameName);
            }

            // Get silver and speed berry data
            silverBerries = CollabUtils2Helper.GetSilverBerries(currentLevelSet);
            pbTimes = CollabUtils2Helper.GetSpeedBerryPBs();

            // Only auto-teleport player once
            if (currentPlayer != null && !autoTPed && Settings.AutoTeleportOnComplete)
            {
                CollabTeleportCommand.TeleportToCollabLevel(currentPlayer, null);
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

        public List<EntityData> GetFilteredCollabLevels(FilterType ft) => collabChapters.FindAll(t =>
            {
                string map = t.Attr("map");
                AreaStats a = SaveData.Instance.GetAreaStatsFor(foundAreas[map]);

                switch (ft)
                {
                    case FilterType.BothBerries:
                        return !HasGoldTime(a, map) && !HasDeathless(a, map);
                    case FilterType.GoldSpeedberry:
                        return !HasGoldTime(a, map);
                    case FilterType.DeathlessBerry:
                        return !HasDeathless(a, map);
                    case FilterType.ClearOnly:
                    default:
                        return !HasCleared(a, map);
                }
            });

        private bool HasCleared(AreaStats a, string map) => a.Modes[0].Completed;

        private bool HasGoldTime(AreaStats a, string map) => speedBerries.ContainsKey(map) ?
            a.Modes[0].Strawberries.Contains(speedBerries[map].ID) && TimeSpan.FromTicks(pbTimes[map]).TotalSeconds <= speedBerries[map].Time
            : HasCleared(a, map);

        private bool HasDeathless(AreaStats a, string map) => CollabUtils2Helper.IsHeartSide(map) ?
            AreaData.GetMode(foundAreas[map]).MapData.Goldenberries.Exists(b => a.Modes[0].Strawberries.Contains(new EntityID(b.Level.Name, b.ID)))
            : (silverBerries.ContainsKey(map) ? a.Modes[0].Strawberries.Contains(silverBerries[map]) : HasCleared(a, map));

        public bool TryGetLevelname(string levelname, out string dir) =>
            levelnameToDirectory[currentLevelSet].TryGetValue(levelname.ToLower(), out dir);

        public string ListAllCollabMaps() => string.Join(", ", levelnameToDirectory[currentLevelSet].Keys);
    }
}
