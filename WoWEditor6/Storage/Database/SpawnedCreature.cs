﻿using System;
using SharpDX;

namespace WoWEditor6.Storage.Database
{
    class SpawnedCreature : ISpawnedCreature
    {
        public int SpawnGUID { get; set; }
        // Also known as the field "id"
        public Creature Creature { get; set; }
        public int Map { get; set; }
        public int ZoneID { get; set; }
        public int AreaID { get; set; }
        public SpawnMask SpawnMask { get; set; }
        public int PhaseMask { get; set; }
        public int ModellID { get; set; }
        public int EquipmentID { get; set; }
        public Vector3 Position { get; set; }
        public float Orientation { get; set; }
        public int SpawnTimeSecs { get; set; }
        public int SpawnDist { get; set; }
        public int CurrentWayPoint { get; set; }
        public int CurrentHealth { get; set; }
        public int CurrentMana { get; set; }
        public MovementType MovementType { get; set; }
        public NPCFlag NPCFlag { get; set; }
        public UnitFlags UnitFlags { get; set; }
        public DynamicFlags DynamicFlags { get; set; }
        public int VerifiedBuild { get; set; }

        public string GetUpdateSQLQuery()
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
