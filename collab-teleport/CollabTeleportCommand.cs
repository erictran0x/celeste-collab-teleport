using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Celeste.Mod.CollabTeleport
{
    public class CollabTeleportCommand
    {
        [Command("collabtp", "Teleports to specified collab level (replace spaces with underscores). Default: nearest noncompleted")]
        private static void HandleCollabTP(string mapname)
        {
            TeleportToCollabLevel(Engine.Scene.Tracker.GetEntity<Player>(), mapname);
        }

        [Command("collablist", "List all collab levels.")]
        private static void HandleCollabList()
        {
            Engine.Commands.Log(CollabTeleportModule.Instance.ListAllCollabMaps());
        }

        public static void TeleportToCollabLevel(Player player, string mapname)
        {
            if (!string.IsNullOrEmpty(mapname))
            {
                // Check if player is not null - stop exec if so
                if (player == null)
                {
                    Engine.Commands.Log("player is null for some unknown reason.");
                    return;
                }

                // Check if map can be searched by name - stop exec if not found
                if (!CollabTeleportModule.Instance.TryGetLevelname(mapname, out string dir))
                {
                    Engine.Commands.Log($"Unable to find {mapname} .");
                    return;
                }

                if (CollabUtils2Helper.IsHeartSide(dir))
                {
                    List<EntityData> filteredLevels = CollabTeleportModule.Instance.GetFilteredCollabLevels(CollabTeleportSettings.FilterType.ClearOnly);

                    // If more than one left, then there exists a level other than heart-side noncompleted
                    if (filteredLevels.Count > 1)
                    {
                        Engine.Commands.Log($"Cannot teleport to heart-side - {filteredLevels.Count - 1} levels to complete for unlock.");
                        return;
                    }
                }

                EntityData level = CollabTeleportModule.Instance.collabChapters.Find(t => t.Attr("map").Equals(dir));
                TeleportToCollabLevel(player, level);
            }
            else
            {
                List<EntityData> filteredLevels = CollabTeleportModule.Instance.GetFilteredCollabLevels(CollabTeleportModule.Settings.IgnoreLevelBy);
                Engine.Commands.Log($"{filteredLevels.Count} noncompleted collab level(s) found.");

                // Ignore heart-side if there is more than one collab level available
                if (filteredLevels.Count > 1)
                    filteredLevels = filteredLevels.FindAll(t => !CollabUtils2Helper.IsHeartSide(t.Attr("map")));

                EntityData next = FindNearestEntityFromPosition(player.Position - CollabTeleportModule.Instance.currentLevel.LevelOffset, filteredLevels);
                if (next != null)
                    TeleportToCollabLevel(player, next);
            }
        }

        private static EntityData FindNearestEntityFromPosition(Vector2 pos, List<EntityData> data)
        {
            float minDist = float.PositiveInfinity;
            EntityData entity = null;
            foreach (EntityData t in data)
            {
                Vector2 diff = t.Position - pos;

                // Calculate min horiz and vert distances
                float ddx = Math.Min(Math.Abs(diff.X), Math.Abs(diff.X + t.Width));
                float ddy = Math.Min(Math.Abs(diff.Y), Math.Abs(diff.Y + t.Height));

                // Compare current highest Euclidean dist
                float dist = (float)(Math.Pow(ddx, 2) + Math.Pow(ddy, 2));
                if (dist < minDist)
                {
                    minDist = dist;
                    entity = t;
                }
            }
            return entity;
        }

        private static Vector2 FindNearestPointFromPosition(Vector2 pos, List<Vector2> points)
        {
            float minDist = float.PositiveInfinity;
            Vector2 near = Vector2.Zero;
            foreach (Vector2 p in points)
            {
                float dist = (p - pos).LengthSquared();
                if (dist < minDist)
                {
                    minDist = dist;
                    near = p;
                }
            }
            return near;
        }

        private static void TeleportToCollabLevel(Player player, EntityData t)
        {
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

            // Set respawn point to one that is closest to next position
            List<Vector2> spawnpoints = CollabTeleportModule.Instance.currentLevel.Session.LevelData.Spawns.Select(vec => vec - offset).ToList();
            Vector2 nearestSpawn = offset + FindNearestPointFromPosition(pos, spawnpoints);
            CollabTeleportModule.Instance.currentLevel.Session.RespawnPoint = nearestSpawn;

            if (v.HasValue)
            {
                // Teleport player to position
                Engine.Commands.Log($"Teleporting to {t.Attr("map")} . ({v.Value.X}, {v.Value.Y})");
                player.Position = v.Value;
            }
            else
            {
                // Can't find open air - teleport to nearest spawnpoint instead
                Engine.Commands.Log($"No open spot found in trigger near {pos} .");
                player.Position = nearestSpawn;
            }

            // Set player state to normal if it is currently in an intro-type (entering chapter)
            if (player.StateMachine.State >= 12 && player.StateMachine.State <= 15
                || player.StateMachine.State == 23
                || player.StateMachine.State == 25)
                player.StateMachine.State = 0;

            // "Teleport" camera to its target - it'll micro-adjust but would be near player
            player.CameraAnchorLerp = Vector2.Zero;
            CollabTeleportModule.Instance.currentLevel.Camera.Position = player.CameraTarget;

            // Remove "intro transition" if it's there
            if (CollabTeleportModule.Instance.currentLevel.Wipe != null)
                CollabTeleportModule.Instance.currentLevel.Wipe.Completed = true;
        }
    }
}
