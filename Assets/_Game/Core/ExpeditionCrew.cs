using System;
using System.Collections.Generic;
using System.IO;
using PlanetHarvest.Art;
using PlanetHarvest.Logistics;
using PlanetHarvest.Mining;
using PlanetHarvest.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetHarvest.Core
{
    [Serializable]
    internal sealed class CrewPacket
    {
        public string kind, world;
        public Vector3 position, eye, forward;
        public Quaternion rotation;
        public DrillProfile profile;
        public int choice, epoch;
        public Vector3Int contact;
        public ExpeditionSnapshot state;
    }

    public sealed class ExpeditionCrew : MonoBehaviour
    {
        public const int Port = 24781;
        private EarthMining miner;
        private readonly CrewConnection link = new CrewConnection();
        private readonly HarvestInventory otherPack = new HarvestInventory(EarthMining.PackCapacity);
        private readonly MiningTools otherTools = new MiningTools();
        private readonly List<Vector3Int> pendingCells = new List<Vector3Int>();
        private Transform avatar;
        private Material suit;
        private Rigidbody body;
        private Vector3 otherPosition;
        private Quaternion otherRotation = Quaternion.identity;
        private DrillProfile otherProfile;
        private float nextTick, nextGuestCut;
        private bool ready;
        private int epoch;
        private float handshakeDeadline;
        private ExpeditionSnapshot priorSolo;
        private bool previousBackground;
        public bool IsGuest => priorSolo != null;
        public bool Hosting => link.Hosting;
        public bool Paired => ready;
        public string LinkStatus => IsGuest ? "Gast" : Hosting ? "Host" : "Solo";
        public long PartnerLoad => otherPack.Amount;
        public string Status { get; private set; } = "Solo";
        private string SaveDirectory => Application.isBatchMode ? Application.temporaryCachePath : Application.persistentDataPath;
        public string SavePath => Path.Combine(SaveDirectory, "expedition-v1.json.gz");
        private string AutoPath => Path.Combine(SaveDirectory, "expedition-crew-auto.json.gz");

        internal void Initialize(EarthMining mining)
        {
            miner = mining;
            body = GetComponent<Rigidbody>();
            Vector3 spawn = miner.Planet.SpawnPosition();
            otherPosition = spawn + miner.Planet.transform.right * 2;
            otherRotation = Quaternion.FromToRotation(Vector3.up, (spawn - miner.Planet.transform.position).normalized);
            link.Opened += OnOpen;
            link.Closed += OnClose;
            link.Received += OnPacket;
            miner.Planet.CellMined += TrackEdit;
            previousBackground = Application.runInBackground;
        }

        private void Start()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-harvestHost") Host();
                if (args[i] == "-harvestJoin" && i + 1 < args.Length) Join(args[++i]);
            }
        }

        private void TrackEdit(Vector3Int cell)
        {
            if (ready && link.Hosting) pendingCells.Add(cell);
        }

        public void Host()
        {
            if (Hosting || IsGuest) return;
            try
            {
                link.Host(Port);
                Application.runInBackground = true;
                Status = $"Host · wartet auf Crew (TCP {Port})";
            }
            catch (Exception e) { Status = "Hoststart fehlgeschlagen · " + e.Message; }
        }

        public void Join(string address)
        {
            if (Hosting || IsGuest || string.IsNullOrWhiteSpace(address)) return;
            try
            {
                priorSolo = Capture(true);
                link.Join(address.Trim(), Port, Time.unscaledTime);
                Application.runInBackground = true;
                miner.Elevator.Replica = true;
                Status = "Verbindung wird hergestellt …";
            }
            catch (Exception e) { StopSession(); Status = "Beitritt fehlgeschlagen · " + e.Message; }
        }

        public void StopSession()
        {
            link.Dispose();
            if (priorSolo != null) RestoreSolo();
            Application.runInBackground = previousBackground;
            Status = "Solo";
        }

        private void RestoreSolo()
        {
            var saved = priorSolo;
            priorSolo = null;
            miner.Elevator.Replica = false;
            Apply(saved, true, false);
        }

        private void OnOpen()
        {
            ready = false;
            handshakeDeadline = Time.unscaledTime + 15;
            if (link.Joined) Send(new CrewPacket { kind = "hello", world = miner.Planet.WorldKey });
        }

        private void OnClose()
        {
            bool wasHost = ready && priorSolo == null;
            if (wasHost) Save(AutoPath);
            ready = false;
            pendingCells.Clear();
            if (avatar) avatar.gameObject.SetActive(false);
            if (priorSolo != null) RestoreSolo();
            if (!Hosting) Application.runInBackground = previousBackground;
            Status = Hosting ? "Host · wartet auf Wiederbeitritt" : "Getrennt · Solo-Welt wiederhergestellt";
        }

        private ExpeditionSnapshot Capture(bool full)
        {
            return new ExpeditionSnapshot
            {
                version = ExpeditionSnapshot.CurrentVersion, world = miner.Planet.WorldKey,
                removed = full ? new List<Vector3Int>(miner.Planet.Data.Removed).ToArray() : pendingCells.ToArray(),
                host = MinerSnapshot.Capture(miner.Inventory, miner.Equipment.Tools, body.position, body.rotation, miner.Profile),
                guest = MinerSnapshot.Capture(otherPack, otherTools, otherPosition, otherRotation, otherProfile),
                freight = miner.Elevator.Freight.Capture()
            };
        }

        private void Apply(ExpeditionSnapshot state, bool full, bool guest)
        {
            var program = miner.Elevator.Freight.Program;
            state.Upgrade(program);
            state.Validate(miner.Planet, program, full);
            if (full || state.removed.Length > 0) miner.Planet.ApplyCells(state.removed, full);
            var local = guest ? state.guest : state.host;
            var remote = guest ? state.host : state.guest;
            local.Apply(miner.Inventory, miner.Equipment.Tools);
            remote.Apply(otherPack, otherTools);
            otherPosition = remote.position;
            otherRotation = remote.rotation;
            otherProfile = remote.profile;
            miner.Elevator.Freight.Restore(state.freight);
            if (full)
            {
                miner.Profile = local.profile;
                body.position = local.position;
                body.rotation = local.rotation;
                body.linearVelocity = Vector3.zero;
                Physics.SyncTransforms();
            }
            miner.Equipment.Refresh(miner.Inventory);
            if (ready) ShowPartner();
        }

        public void Save() => Save(SavePath);

        public void Save(string path)
        {
            if (IsGuest) { Status = "Der Host speichert die Expedition."; return; }
            try
            {
                var state = Capture(true);
                state.Validate(miner.Planet, miner.Elevator.Freight.Program, true);
                ExpeditionSnapshot.Write(path, state);
                Status = "Expedition gespeichert";
            }
            catch (Exception e) { Status = "Speichern fehlgeschlagen · " + e.Message; }
        }

        public void Load() => Load(File.Exists(AutoPath) && (!File.Exists(SavePath) ||
            File.GetLastWriteTimeUtc(AutoPath) > File.GetLastWriteTimeUtc(SavePath)) ? AutoPath : SavePath);

        public void Load(string path)
        {
            if (IsGuest) { Status = "Der Host lädt die Expedition."; return; }
            try
            {
                var state = ExpeditionSnapshot.Read(path);
                Apply(state, true, false);
                pendingCells.Clear();
                if (ready) Send(new CrewPacket { kind = "world", epoch = ++epoch, state = state });
                Status = "Expedition geladen";
            }
            catch (Exception e) { Status = "Laden fehlgeschlagen · " + e.Message; }
        }

        public void Request(string kind, int choice, Vector3Int contact = default)
        {
            if (!link.Joined || !ready) return;
            var command = Pose(kind, choice);
            command.contact = contact;
            Send(command);
        }

        private CrewPacket Pose(string kind, int choice = 0) => new CrewPacket
        {
            kind = kind, epoch = epoch, position = body.position, rotation = body.rotation, profile = miner.Profile,
            forward = miner.View.transform.forward, eye = miner.View.transform.position, choice = choice
        };

        private void Send(CrewPacket packet) => link.Send(JsonUtility.ToJson(packet));

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys?.f5Key.wasPressedThisFrame == true) Save();
            if (keys?.f9Key.wasPressedThisFrame == true) Load();
            try
            {
                link.Tick(Time.unscaledTime);
                if (link.Connected && !ready && Time.unscaledTime > handshakeDeadline) throw new IOException("Keine Antwort der Crew.");
                if (!ready || Time.unscaledTime < nextTick) return;
                nextTick = Time.unscaledTime + .1f;
                if (link.Hosting)
                {
                    Send(new CrewPacket { kind = "update", state = Capture(false) });
                    pendingCells.Clear();
                }
                else Send(Pose("pose"));
            }
            catch (Exception e)
            {
                link.DropPeer();
                Status = "Verbindung unterbrochen · " + e.Message;
            }
        }

        private void OnPacket(string json)
        {
            var packet = JsonUtility.FromJson<CrewPacket>(json);
            if (packet == null) throw new InvalidDataException("Leeres Paket.");
            if (link.Hosting)
            {
                if (!ready)
                {
                    if (packet.kind != "hello" || packet.world != miner.Planet.WorldKey)
                        throw new InvalidDataException("Unterschiedliche Planetenversionen.");
                    Send(new CrewPacket { kind = "world", epoch = ++epoch, state = Capture(true) });
                    ready = true;
                    Status = "Host · Crew verbunden";
                    ShowPartner();
                    return;
                }
                if (packet.epoch == epoch) HandleIntent(packet);
            }
            else
            {
                bool full = packet.kind == "world";
                if ((!full && (!ready || packet.kind != "update")) || packet.state?.removed == null)
                    throw new InvalidDataException("Weltstand fehlt.");
                if (full) epoch = packet.epoch;
                Apply(packet.state, full, true);
                if (!ready) { ready = true; Status = "Gast · Crew verbunden"; ShowPartner(); }
            }
        }

        private void HandleIntent(CrewPacket p)
        {
            if (!MinerSnapshot.ValidPose(p.position, p.rotation) || (uint)p.profile > 2) return;
            otherPosition = p.position;
            otherRotation = p.rotation;
            otherProfile = p.profile;
            ShowPartner();
            if (p.kind == "pose") return;
            if (p.kind == "equip")
            {
                bool nearShop = (p.position - miner.Workshop.transform.position).sqrMagnitude < 25;
                if ((uint)p.choice < MiningTools.Count) otherTools.Select((MiningTool)p.choice, miner.Elevator.Freight, nearShop);
                return;
            }
            if (!((p.eye - p.position).sqrMagnitude < 4 && p.forward.sqrMagnitude > .98f && p.forward.sqrMagnitude < 1.02f)) return;
            var spec = MiningTools.Spec(otherTools.Active);
            float reach = p.kind == "load" ? EarthMining.InteractReach : spec.Reach;
            if (!Physics.Raycast(p.eye, p.forward, out var hit, reach, ~0, QueryTriggerInteraction.Ignore)) return;
            if (p.kind == "load" && hit.collider.TryGetComponent(out EarthCargoBin bin) && (int)bin.Material == p.choice)
                if (miner.Elevator.Freight.Load(otherPack, bin.Material) == 0) miner.Elevator.Freight.Dispatch(bin.Material);
            if (p.kind == "cut" && Time.unscaledTime >= nextGuestCut && hit.collider.GetComponentInParent<Voxel.VoxelPlanet>() == miner.Planet)
            {
                nextGuestCut = Time.unscaledTime + spec.RepeatSeconds;
                if (miner.Planet.CellAt(hit) == p.contact) miner.Cut(otherPack, otherTools.Active, p.profile, p.contact, p.forward);
            }
        }

        private void ShowPartner()
        {
            if (!avatar)
            {
                avatar = new GameObject("Alien · Crew").transform;
                suit = FieldProps.Finish();
                Color green = new Color(.47f,.88f,.62f);
                FieldProps.Part(avatar,"Anzug",PrimitiveType.Capsule,Vector3.down*.18f,new Vector3(.62f,.67f,.58f),new Color(.98f,.6f,.16f),suit);
                FieldProps.Part(avatar,"Kopf",PrimitiveType.Sphere,Vector3.up*.66f,new Vector3(.8f,.74f,.68f),green,suit);
                FieldProps.Part(avatar,"Rucksack",PrimitiveType.Cube,new Vector3(0,0,-.32f),new Vector3(.5f,.64f,.27f),new Color(.38f,.29f,.55f),suit);
                foreach (int side in new[] { -1,1 })
                    FieldProps.Part(avatar,"Auge",PrimitiveType.Sphere,new Vector3(side*.18f,.73f,.26f),
                        new Vector3(.16f,.22f,.12f),new Color(.11f,.14f,.20f),suit);
            }
            avatar.gameObject.SetActive(true);
            avatar.SetPositionAndRotation(otherPosition, otherRotation);
        }

        private void OnApplicationQuit()
        {
            if (Hosting && ready) Save(AutoPath);
        }

        private void OnDestroy()
        {
            link.Closed -= OnClose;
            link.Dispose();
            if (miner && miner.Planet) miner.Planet.CellMined -= TrackEdit;
            if (avatar) Destroy(avatar.gameObject);
            if (suit) Destroy(suit);
            Application.runInBackground = previousBackground;
        }
    }
}
