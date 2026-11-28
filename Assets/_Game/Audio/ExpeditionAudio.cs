using PlanetHarvest.Mining;
using UnityEngine;

namespace PlanetHarvest.Audio
{
    // Short synthesized cues keep the prototype self-contained.
    public sealed class ExpeditionAudio : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip drill, launch, arrival;
        private MaterialId lastFlight;
        private bool returning;
        private EarthMining miner;

        public void Initialize(EarthMining owner)
        {
            miner = owner;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            drill = Tone("Bohrkontakt", .14f, 195, 72, .45f);
            launch = Tone("Liftstart", .54f, 82, 380, .08f);
            arrival = Tone("Orbitbestätigung", .26f, 475, 760, 0);
        }

        public void Contact(MaterialId material)
        {
            source.pitch = material == MaterialId.Soil ? .78f : material == MaterialId.Stone ? 1.05f : 1.45f;
            source.PlayOneShot(drill, .24f);
        }

        private void Update()
        {
            var freight = miner.Elevator.Freight;
            if (freight.Flying != MaterialId.None && freight.Flying != lastFlight && !freight.Returning)
            {
                source.pitch = 1;
                source.PlayOneShot(launch, .29f);
            }
            if (freight.Returning && !returning)
            {
                source.pitch = 1;
                source.PlayOneShot(arrival, .2f);
            }
            lastFlight = freight.Flying;
            returning = freight.Returning;
        }

        private static AudioClip Tone(string name, float seconds, float start, float end, float grit)
        {
            const int rate = 22050;
            var data = new float[(int)(seconds * rate)];
            var noise = new System.Random(27);
            float phase = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)data.Length;
                phase += 2 * Mathf.PI * Mathf.Lerp(start, end, t) / rate;
                float envelope = Mathf.Min(t * 28, 1) * (1 - t) * (1 - t);
                float harmonic = .72f * Mathf.Sin(phase) + .24f * Mathf.Sin(2 * phase);
                data[i] = envelope * Mathf.Lerp(harmonic, 2 * (float)noise.NextDouble() - 1, grit);
            }
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDestroy()
        {
            Destroy(drill);
            Destroy(launch);
            Destroy(arrival);
        }
    }
}
