﻿using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Celeste.Mod.CollabTeleport
{
    public class CollabTeleportCommand
    {
        [Command("collabtp", "Teleports to nearest noncompleted collab level.")]
        private static void TeleportToNextCollabLevel()
        {
            Player player = Engine.Scene.Tracker.GetEntity<Player>();

            // Check if player is not null - stop exec if so
            if (player == null)
            {
                Engine.Commands.Log("player is null for some unknown reason.");
                return;
            }

            // Remove collab levels if completed level or gym-type level
            List<EntityData> filteredLevels = CollabTeleportModule.Instance.collabChapters.FindAll(t =>
            {
                bool success = CollabTeleportModule.Instance.foundAreas.TryGetValue(t.Attr("map"), out AreaStats a);
                return success /* && !a.SID.Contains($"/0-Gyms/") */ && !a.Modes[0].Completed;
            });

            // Check if there are no noncompleted collab levels left - return if so
            if (filteredLevels.Count == 0)
            {
                Engine.Commands.Log("No noncompleted collab levels found.");
                return;
            }

            Engine.Commands.Log($"{filteredLevels.Count} noncompleted collab levels found.");

            // Find closest collab level from player
            float minDist = float.PositiveInfinity;
            Vector2 pos = player.Position;
            int w = 0, h = 0;
            string nextMap = "unknown";
            foreach (EntityData t in filteredLevels)
            {
                string name = t.Attr("map");

                // Ignore heart-side if there is more than one collab level available
                if (filteredLevels.Count > 1 && name.EndsWith("ZZ-HeartSide"))
                    continue;

                // Calculate distance if area exists
                if (CollabTeleportModule.Instance.foundAreas.TryGetValue(name, out AreaStats area))
                {
                    // Calculate horizontal distance
                    float dx = t.Position.X - player.Position.X;
                    float ddx = Math.Min(Math.Abs(dx), Math.Abs(dx + t.Width));

                    // Calculate vertical distance
                    float dy = t.Position.Y - player.Position.Y;
                    float ddy = Math.Min(Math.Abs(dy), Math.Abs(dy + t.Height));

                    // Compare current highest dist
                    float dist = (float)(Math.Pow(ddx, 2) + Math.Pow(ddy, 2));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        pos = t.Position;
                        w = t.Width;
                        h = t.Height;
                        nextMap = name;
                    }
                }
            }

            // Find open air in trigger box
            // Bruteforce for now since I'm too stupid to find a better solution
            for (int j = 0; j <= h/2; j += 4)  // middle to vertical edges
            {
                for (int i = 0; i <= w; i += 7)  // left to right
                {
                    Vector2 v_top = new Vector2(pos.X + i, pos.Y + h/2 - j);
                    Vector2 v_bot = new Vector2(pos.X + i, pos.Y + h/2 + j);
                    if (!player.CollideCheck<Solid>(v_top))
                    {
                        // Teleport player to position
                        Engine.Commands.Log($"Found open spot at {v_top} - teleporting to {nextMap} .");
                        player.X = v_top.X;
                        player.Y = v_top.Y;
                        return;
                    }
                    if (!player.CollideCheck<Solid>(v_bot))
                    {
                        // Teleport player to position
                        Engine.Commands.Log($"Found open spot at {v_bot} - teleporting to {nextMap} .");
                        player.X = v_bot.X;
                        player.Y = v_bot.Y;
                        return;
                    }
                }
            }

            // Can't find open air - do nothing
            Engine.Commands.Log($"No open spot found in trigger near {pos} .");
        }
    }
}
