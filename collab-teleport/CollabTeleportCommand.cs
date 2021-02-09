using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CollabTeleport
{
    public class CollabTeleportCommand
    {
        [Command("collabtp", "Teleports to specified collab level (replace spaces with underscores). Default: nearest noncompleted")]
        private static void HandleCollabTP(string mapname)
        {
            TeleportToCollabLevel(Engine.Scene.Tracker.GetEntity<Player>(), mapname, true);
        }

        [Command("collablist", "List all collab levels.")]
        private static void HandleCollabList()
        {
            Engine.Commands.Log(CollabTeleportModule.Instance.ListAllCollabMaps());
        }

        public static void TeleportToCollabLevel(Player player, string mapname, bool logToConsole)
        {
            if (!string.IsNullOrEmpty(mapname))
            {
                // Check if map can be searched by name - stop exec if not found
                if (!CollabTeleportModule.Instance.TryGetLevelname(mapname, out string dir))
                {
                    if (logToConsole)
                        Engine.Commands.Log($"Unable to find {mapname} .");
                    return;
                }

                if (CollabUtils2Helper.IsHeartSide(dir))
                {
                    List<EntityData> filteredLevels = CollabTeleportModule.Instance.GetFilteredCollabLevels();

                    // If more than one left, then there exists a level other than heart-side noncompleted
                    if (filteredLevels.Count > 1)
                    {
                        if (logToConsole)
                            Engine.Commands.Log($"Cannot teleport to heart-side - {filteredLevels.Count - 1} levels to complete for unlock.");
                        return;
                    }
                }

                EntityData level = CollabTeleportModule.Instance.collabChapters.Find(t => t.Attr("map").Equals(dir));
                TeleportToCollabLevel(player, level, logToConsole);
            }
            else
            {
                EntityData next = FindNearestNoncompletedCollabLevel(player, logToConsole);
                if (next != null)
                    TeleportToCollabLevel(player, next, logToConsole);
            }
        }

        private static EntityData FindNearestNoncompletedCollabLevel(Player player, bool logToConsole)
        {
            // Check if player is not null - stop exec if so
            if (player == null)
            {
                if (logToConsole)
                    Engine.Commands.Log("player is null for some unknown reason.");
                return null;
            }

            List<EntityData> filteredLevels = CollabTeleportModule.Instance.GetFilteredCollabLevels();

            // Check if there are no noncompleted collab levels left - return if so
            if (filteredLevels.Count == 0)
            {
                if (logToConsole)
                    Engine.Commands.Log("No noncompleted collab levels found.");
                return null;
            }

            if (logToConsole)
                Engine.Commands.Log($"{filteredLevels.Count} noncompleted collab levels found.");

            // Find closest collab level from player
            float minDist = float.PositiveInfinity;
            EntityData entity = null;
            foreach (EntityData t in filteredLevels)
            {
                string name = t.Attr("map");

                // Ignore heart-side if there is more than one collab level available
                if (filteredLevels.Count > 1 && CollabUtils2Helper.IsHeartSide(name))
                    continue;

                Vector2 diff = t.Position - player.Position + CollabTeleportModule.Instance.currentLevel.LevelOffset;

                // Calculate min horiz and vert distances
                float ddx = Math.Min(Math.Abs(diff.X), Math.Abs(diff.X + t.Width));
                float ddy = Math.Min(Math.Abs(diff.Y), Math.Abs(diff.Y + t.Height));

                // Compare current highest dist
                float dist = (float)(Math.Pow(ddx, 2) + Math.Pow(ddy, 2));
                if (dist < minDist)
                {
                    minDist = dist;
                    entity = t;
                }
            }
            return entity;
        }

        private static void TeleportToCollabLevel(Player player, EntityData t, bool logToConsole)
        {
            // Check if player is not null - stop exec if so
            if (player == null)
            {
                if (logToConsole)
                    Engine.Commands.Log("player is null for some unknown reason.");
                return;
            }

            Vector2 pos = t.Position;
            int w = t.Width, h = t.Height;

            // Find open air in trigger box
            // Bruteforce for now since I'm too stupid to find a better solution
            Vector2 offset = CollabTeleportModule.Instance.currentLevel.LevelOffset;
            Vector2? v = null;
            for (int j = 0; j <= h/2; j += 4)  // middle to vertical edges
            {
                for (int i = 0; i <= w; i += 7)  // left to right
                {
                    Vector2 v_top = offset + new Vector2(pos.X + i, pos.Y + h/2 - j);
                    Vector2 v_bot = offset + new Vector2(pos.X + i, pos.Y + h/2 + j);
                    if (!player.CollideCheck<Solid>(v_top))
                        v = v_top;
                    else if (!player.CollideCheck<Solid>(v_bot))
                        v = v_bot;
                }
            }

            if (v.HasValue)
            {
                // Teleport player to position
                if (logToConsole)
                    Engine.Commands.Log($"Teleporting to {t.Attr("map")} .");
                player.Position = v.Value;

                // Set player state to normal if it is currently in an intro-type (entering chapter)
                if (player.StateMachine.State >= 12 && player.StateMachine.State <= 15
                    || player.StateMachine.State == 23
                    || player.StateMachine.State == 25)
                    player.StateMachine.State = 0;

                // "Teleport" camera to its target - it'll micro-adjust but would be near player
                player.CameraAnchorLerp = Vector2.Zero;
                CollabTeleportModule.Instance.currentLevel.Camera.Position = player.CameraTarget;
            }
            else
            {
                // Can't find open air - do nothing
                if (logToConsole)
                    Engine.Commands.Log($"No open spot found in trigger near {pos} .");
            }
        }
    }
}
