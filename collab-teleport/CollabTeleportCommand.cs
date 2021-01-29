using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
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

            // Filter collab levels by non-completion
            List<EntityData> noncompleted = CollabTeleportModule.Instance.collabChapters.FindAll(t =>
            {
                bool success = CollabTeleportModule.Instance.foundAreas.TryGetValue(t.Attr("map"), out AreaStats a);
                return success && !a.Modes[0].Completed;
            });

            // Check if there are no noncompleted collab levels left - return if so
            if (noncompleted.Count == 0)
            {
                Engine.Commands.Log("No noncompleted collab levels found.");
                return;
            }

            Engine.Commands.Log($"{noncompleted.Count} noncompleted collab levels found.");

            float minDist = float.PositiveInfinity;
            Vector2 pos = player.Position;
            int w = 0, h = 0;
            string nextMap = "unknown";
            foreach (EntityData t in noncompleted)
            {
                string name = t.Attr("map");
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
            for (int j = h; j >= 0; j -= 8)
            {
                for (int i = 0; i <= w; i += 7)
                {
                    Vector2 v = new Vector2(pos.X + i, pos.Y + j);
                    if (!player.CollideCheck<Solid>(v))
                    {
                        // Teleport player to position
                        Engine.Commands.Log($"Found open spot at {v} - teleporting to {nextMap} .");
                        player.X = v.X;
                        player.Y = v.Y;
                        return;
                    }
                }
            }

            // Can't find open air - do nothing
            Engine.Commands.Log($"No open spot found in trigger at {pos} .");
        }
    }
}
