using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.CollabTeleport
{
    static class CollabUtils2Helper
    {
        private static Type type;

        public struct SpeedBerryInfo
        {
            public EntityID ID;
            public int Gold;
            public int Silver;
            public int Bronze;
        }

        public static Dictionary<string, EntityID> GetAllSilverBerries(string levelset)
        {
            if (type == null)
                type = Type.GetType("Celeste.Mod.CollabUtils2.CollabMapDataProcessor, CollabUtils2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", true);

            FieldInfo f = type.GetField("SilverBerries", BindingFlags.Static | BindingFlags.Public);
            if (f != null)
            {
                Dictionary<string, Dictionary<string, EntityID>> v = (Dictionary<string, Dictionary<string, EntityID>>)f.GetValue(null);
                return v[levelset];
            }
            return null;
        }

        public static SpeedBerryInfo? GetAllSpeedBerries(string levelset)
        {
            if (type == null)
                type = Type.GetType("Celeste.Mod.CollabUtils2.CollabMapDataProcessor, CollabUtils2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

            FieldInfo f = type.GetField("SpeedBerries", BindingFlags.Static | BindingFlags.Public);
            if (f != null)
            {
                Dictionary<string, SpeedBerryInfo> v = (Dictionary<string, SpeedBerryInfo>)f.GetValue(null);
                return v[levelset];
            }
            return null;
        }
    }
}
