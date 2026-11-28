using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using PlanetHarvest.Mining;
using PlanetHarvest.Player;
using PlanetHarvest.Progression;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Core
{
    [Serializable]
    public sealed class MinerSnapshot
    {
        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
        public MiningTool active;
        public int owned = 1;
        public long[] amounts = new long[(int)MaterialId.Quartz + 1];
        public DrillProfile profile;

        public static MinerSnapshot Capture(HarvestInventory pack, MiningTools tools, Vector3 position,
            Quaternion rotation, DrillProfile profile)
        {
            var state = new MinerSnapshot { position = position, rotation = rotation, profile = profile,
                owned = tools.Ownership, active = tools.Active };
            for (int i = 1; i < state.amounts.Length; i++) state.amounts[i] = pack.AmountOf((MaterialId)i);
            return state;
        }

        public void Validate()
        {
            if (amounts == null || amounts.Length != (int)MaterialId.Quartz + 1 || amounts[0] != 0 ||
                amounts[(int)MaterialId.Regolith] != 0 || owned < 1 || owned > 7 || (owned & 1) == 0 ||
                (uint)active >= MiningTools.Count || (owned & (1 << (int)active)) == 0 || (uint)profile > 2 ||
                !ValidPose(position, rotation)) throw new InvalidDataException("Ungültiger Crewzustand.");
            long total = 0;
            foreach (long amount in amounts)
            {
                if (amount < 0 || amount > EarthMining.PackCapacity - total) throw new InvalidDataException("Ungültige Ladung.");
                total += amount;
            }
        }

        internal static bool ValidPose(Vector3 p, Quaternion q) => p.sqrMagnitude < 200 * 200 &&
            Quaternion.Dot(q, q) > .99f && Quaternion.Dot(q, q) < 1.01f;

        public void Apply(HarvestInventory pack, MiningTools tools)
        {
            pack.Empty();
            for (int i = 1; i < amounts.Length; i++) pack.TryAdd((MaterialId)i, amounts[i]);
            tools.Restore(owned, active);
        }
    }

    [Serializable]
    public sealed class ExpeditionSnapshot
    {
        public int version;
        public string world;
        public Vector3Int[] removed;
        public MinerSnapshot host, guest;
        public FreightSnapshot freight;

        public void Validate(VoxelMoon moon, bool full)
        {
            if (version != 1 || world != moon.WorldKey || host == null || guest == null || freight == null)
                throw new InvalidDataException("Spielstand oder Planetenversion passt nicht.");
            host.Validate();
            guest.Validate();
            int count = EarthFreight.LevelCount;
            if (freight.loads?.Length != count || freight.delivered?.Length != count || freight.credits < 0 ||
                !(freight.progress >= 0 && freight.progress <= 1))
                throw new InvalidDataException("Ungültiger Liftzustand.");
            bool validFlight = freight.flying == MaterialId.None && !freight.returning && freight.progress == 0;
            for (int i = 0; i < count; i++)
            {
                var order = EarthFreight.Order(i);
                long capacity = order.Volume * HarvestInventory.CubicMetre;
                if (freight.loads[i] < 0 || freight.loads[i] > capacity || freight.delivered[i] < 0 ||
                    freight.delivered[i] > moon.Data.InitialCount * HarvestInventory.CubicMetre)
                    throw new InvalidDataException("Ungültiger Frachtbestand.");
                if (freight.flying == order.Material)
                    validFlight = freight.returning ? freight.loads[i] == 0 : freight.loads[i] > 0;
            }
            if (!validFlight || (full && removed == null)) throw new InvalidDataException("Unvollständiger Weltstand.");
            if (removed == null) return;
            if (removed.Length > moon.Data.InitialCount) throw new InvalidDataException("Ungültige Terrainmenge.");
            var quantities = full ? new long[(int)MaterialId.Quartz + 1] : null;
            var unique = full ? new HashSet<Vector3Int>() : null;
            foreach (var cell in removed)
            {
                if (cell.x < -moon.Data.Radius || cell.y < -moon.Data.Radius || cell.z < -moon.Data.Radius ||
                    cell.x >= moon.Data.Radius || cell.y >= moon.Data.Radius || cell.z >= moon.Data.Radius ||
                    !moon.Data.IsOriginalSolid(cell)) throw new InvalidDataException("Ungültige Terrainänderung.");
                if (!full) continue;
                if (!unique.Add(cell)) throw new InvalidDataException("Doppelte Terrainänderung.");
                quantities[(int)moon.Data.OriginalMaterial(cell)] += HarvestInventory.CubicMetre;
            }
            if (!full) return;
            for (int i = 0; i < count; i++)
            {
                int id = (int)EarthFreight.Order(i).Material;
                long booked = host.amounts[id] + guest.amounts[id] + freight.loads[i] + freight.delivered[i];
                if (booked != quantities[id]) throw new InvalidDataException("Materialbilanz stimmt nicht.");
            }
        }

        public static void Write(string path, ExpeditionSnapshot state)
        {
            string temp = path + ".tmp";
            try
            {
                using (var file = File.Create(temp))
                using (var zip = new GZipStream(file, System.IO.Compression.CompressionLevel.Fastest))
                using (var writer = new StreamWriter(zip, Encoding.UTF8)) writer.Write(JsonUtility.ToJson(state));
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        public static ExpeditionSnapshot Read(string path)
        {
            using var file = File.OpenRead(path);
            using var zip = new GZipStream(file, CompressionMode.Decompress);
            using var reader = new StreamReader(zip, Encoding.UTF8);
            return JsonUtility.FromJson<ExpeditionSnapshot>(reader.ReadToEnd()) ?? throw new InvalidDataException("Leerer Spielstand.");
        }
    }
}
