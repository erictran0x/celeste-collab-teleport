using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.CollabTeleport
{
    static class CollabUtils2Helper
    {
        private const string COLLAB_MAP_DATA_PROCESSOR = "Celeste.Mod.CollabUtils2.CollabMapDataProcessor, CollabUtils2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        private const string SPEED_BERRY_INFO = "Celeste.Mod.CollabUtils2.CollabMapDataProcessor+SpeedBerryInfo, CollabUtils2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        private static Type typeCMDP, typeSBI;
        
        public struct SpeedBerryInfo
        {
            #pragma warning disable CS0649
            public EntityID ID;
            public int Time;
        }

        public static Dictionary<string, EntityID> GetSilverBerries(string levelset)
        {
            if (levelset == null)
                return new Dictionary<string, EntityID>();

            // Init CollabMapDataProcessor type if needed
            if (typeCMDP == null)
                typeCMDP = Type.GetType(COLLAB_MAP_DATA_PROCESSOR, true);

            // Get SilverBerries value
            FieldInfo f = typeCMDP.GetField("SilverBerries", BindingFlags.Static | BindingFlags.Public);
            if (f != null)
            {
                Dictionary<string, Dictionary<string, EntityID>> v = (Dictionary<string, Dictionary<string, EntityID>>)f.GetValue(null);
                return v != null ? v[levelset] : new Dictionary<string, EntityID>();
            }
            return null;
        }

        public static SpeedBerryInfo? GetSpeedBerryInfo(string sid)
        {
            if (sid == null)
                return null;

            // Init CollabMapDataProcessor type if needed
            if (typeCMDP == null)
                typeCMDP = Type.GetType(COLLAB_MAP_DATA_PROCESSOR, true);

            // Get SpeedBerries value
            FieldInfo f = typeCMDP.GetField("SpeedBerries", BindingFlags.Static | BindingFlags.Public);
            if (f != null)
            {
                IDictionary v = (IDictionary)f.GetValue(null);

                // Check if speedberries exist
                if (v[sid] == null)
                    return null;

                // Init CollabMapDataProcessor.SpeedBerryInfo if needed
                if (typeSBI == null)
                    typeSBI = Type.GetType(SPEED_BERRY_INFO, true);
                
                // Get ID and Gold values
                FieldInfo f_id = typeSBI.GetField("ID");
                FieldInfo f_time = typeSBI.GetField("Gold");

                SpeedBerryInfo sbi = new SpeedBerryInfo
                {
                    ID = f_id != null ? (EntityID)f_id.GetValue(v[sid]) : EntityID.None,
                    Time = f_time != null ? (int)f_time.GetValue(v[sid]) : -1
                };
                return sbi;
            }
            return null;
        }

        public static Dictionary<string, long> GetSpeedBerryPBs()
        {
            // Attempt to find module CollabUtils2
            EverestModule mod = Everest.Modules.First(m => m.Metadata.Name.Equals("CollabUtils2"));

            // Check if CollabUtils2 was found
            if (mod != null)
            {
                // Get SpeedBerryPBs value
                PropertyInfo p = mod.SaveDataType.GetProperty("SpeedBerryPBs");
                return p != null ? (Dictionary<string, long>)p.GetValue(mod._SaveData) : null;
            }
            return null;
        }

        public static bool IsHeartSide(string mapname) => mapname.EndsWith("ZZ-HeartSide");
        public static bool IsGym(string mapname) => mapname.Contains("/0-Gyms/");
        public static bool IsLobby(string mapname) => mapname.Contains("/0-Lobbies/");
    }
}
