using PlanetHarvest.Art;
using UnityEngine;

namespace PlanetHarvest.Mining
{
    internal sealed class MiningDebris : MonoBehaviour
    {
        private ParticleSystem dust;
        private Material finish;

        public void Prepare(Mesh chip)
        {
            dust = gameObject.AddComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 96;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = dust.emission;
            emission.enabled = false;
            var shape = dust.shape;
            shape.enabled = false;
            var renderer = dust.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = chip;
            finish = FieldProps.Finish();
            renderer.sharedMaterial = finish;
        }

        public void Burst(Vector3 point, Vector3 normal, Vector3 up, MaterialId material)
        {
            Color tint = material switch
            {
                MaterialId.Soil => new Color(.67f,.45f,.26f), MaterialId.IronOre => new Color(.82f,.36f,.18f),
                MaterialId.CopperOre => new Color(.97f,.62f,.32f), MaterialId.Quartz => new Color(.84f,.76f,.98f),
                MaterialId.Water => new Color(.45f,.7f,1f), MaterialId.Biomass => new Color(.45f,.78f,.35f),
                MaterialId.Crystal => new Color(.55f,.95f,1f),
                _ => new Color(.6f,.65f,.67f)
            };
            var force = dust.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.World;
            force.x = -up.x * 7; force.y = -up.y * 7; force.z = -up.z * 7;
            for (int i = 0; i < 13; i++)
            {
                var chip = new ParticleSystem.EmitParams
                {
                    position = point + normal * .06f,
                    velocity = normal * Random.Range(.8f,2.4f) + Random.insideUnitSphere * 1.4f,
                    startSize = Random.Range(.024f,.083f),
                    startLifetime = Random.Range(.24f,.52f),
                    startColor = tint * Random.Range(.74f,1f),
                    rotation3D = Random.insideUnitSphere * 180
                };
                dust.Emit(chip, 1);
            }
            dust.Play();
        }

        /// <summary>Colourful party burst for the confetti mushroom.</summary>
        public void Confetti(Vector3 point, Vector3 up)
        {
            var force = dust.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.World;
            force.x = -up.x * 3; force.y = -up.y * 3; force.z = -up.z * 3;
            Color[] colors = { new Color(.98f,.6f,.16f), new Color(.62f,.38f,.95f), new Color(.38f,.84f,.68f), new Color(1f,.85f,.25f), Color.white };
            for (int i = 0; i < 60; i++)
            {
                dust.Emit(new ParticleSystem.EmitParams
                {
                    position = point,
                    velocity = up * Random.Range(2.5f, 5f) + Random.insideUnitSphere * 2.2f,
                    startSize = Random.Range(.04f, .09f),
                    startLifetime = Random.Range(.8f, 1.6f),
                    startColor = colors[i % colors.Length],
                    rotation3D = Random.insideUnitSphere * 180
                }, 1);
            }
            dust.Play();
        }

        private void OnDestroy() => Destroy(finish);
    }
}
