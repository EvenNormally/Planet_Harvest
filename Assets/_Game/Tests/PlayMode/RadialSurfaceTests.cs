using System.Collections;
using NUnit.Framework;
using PlanetHarvest.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace PlanetHarvest.Tests
{
    public class RadialSurfaceTests
    {
        private Gamepad testPad;
        private float originalTimeScale;

        [SetUp]
        public void SetUp() => originalTimeScale = Time.timeScale;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = originalTimeScale;
            if (testPad != null) InputSystem.RemoveDevice(testPad);
        }

        private IEnumerator LoadPlanet()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/Scenes/PlanetHarvest_Test200m.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("Assets/_Game/Scenes/PlanetHarvest_Test200m.unity");
#endif
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerStandsAtSameHeightOnEveryHemisphere()
        {
            yield return LoadPlanet();
            var player = Object.FindFirstObjectByType<RadialPlayer>();
            var controller = player.GetComponent<CharacterController>();
            var body = player.GetComponent<Rigidbody>();
            var directions = new[] { Vector3.forward, Vector3.down, Vector3.left, Vector3.back,
                Vector3.right, new Vector3(1, 1, 1).normalized, new Vector3(-1, -1, -1).normalized };
            foreach (var up in directions)
            {
                if (controller != null) controller.enabled = false;
                player.transform.SetPositionAndRotation(up * 202, Quaternion.FromToRotation(Vector3.up, up));
                if (body != null)
                {
                    body.position = player.transform.position;
                    body.rotation = player.transform.rotation;
                    body.linearVelocity = Vector3.zero;
                }
                if (controller != null && body == null) controller.enabled = true;
                Physics.SyncTransforms();
                yield return new WaitForSeconds(1.5f);
                Assert.That(player.transform.position.magnitude, Is.InRange(200.9f, 201.3f),
                    $"Player must stand on their feet, with the same clearance at {up}");
                Assert.That(Vector3.Dot(player.transform.up, up), Is.GreaterThan(0.999f));
                Vector3 settled = player.transform.position;
                yield return new WaitForSeconds(0.3f);
                Assert.That(Vector3.Distance(player.transform.position, settled), Is.LessThan(0.05f), "Standing must not accumulate inward fall speed");
            }
        }

        private static void Place(RadialPlayer player, Vector3 up, float radius, Vector3 forward)
        {
            var body = player.GetComponent<Rigidbody>();
            body.position = up * radius;
            body.rotation = Quaternion.LookRotation(forward, up);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator FreeFallAccelerationIsEarthGravityOnAllSixSides()
        {
            yield return LoadPlanet();
            var player = Object.FindFirstObjectByType<RadialPlayer>();
            var body = player.GetComponent<Rigidbody>();
            var directions = new[] { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (var up in directions)
            {
                Vector3 forward = Vector3.Cross(up, Mathf.Abs(up.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
                Place(player, up, 220, forward);
                yield return new WaitForFixedUpdate();
                float before = Vector3.Dot(body.linearVelocity, up);
                for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
                float acceleration = (Vector3.Dot(body.linearVelocity, up) - before) / (20 * Time.fixedDeltaTime);
                Assert.That(acceleration, Is.EqualTo(-9.81f).Within(0.05f), $"Unequal gravity at {up}");
            }
        }

        [UnityTest]
        public IEnumerator FullPlanetCircuitNeedsNoJumpAndDoesNotStick()
        {
            yield return LoadPlanet();
            var player = Object.FindFirstObjectByType<RadialPlayer>();
            var body = player.GetComponent<Rigidbody>();
            Vector3 startUp = new Vector3(0, 1, 1).normalized;
            Place(player, startUp, 201.2f, Vector3.right);
            yield return new WaitForSeconds(0.5f);
            testPad = InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(testPad, new GamepadState { leftStick = Vector2.up });
            Time.timeScale = 15;
            float began = Time.time;
            float totalAngle = 0;
            float oppositeDot = 1;
            Vector3 previousUp = body.position.normalized;
            while (Time.time - began < 220)
            {
                yield return new WaitForFixedUpdate();
                Vector3 up = body.position.normalized;
                totalAngle += Mathf.Atan2(Vector3.Cross(previousUp, up).magnitude, Vector3.Dot(previousUp, up)) * Mathf.Rad2Deg;
                previousUp = up;
                oppositeDot = Mathf.Min(oppositeDot, Vector3.Dot(startUp, up));
                Assert.That(body.position.magnitude, Is.InRange(200.85f, 201.4f),
                    $"Walking must stay on the rounded surface: elapsed={Time.time - began}, position={body.position}, velocity={body.linearVelocity}, grounded={player.IsGrounded}, alignment={Vector3.Dot(body.rotation * Vector3.up, up)}");
            }
            Assert.That(oppositeDot, Is.LessThan(-0.99f), "Must reach the opposite hemisphere");
            Assert.That(totalAngle, Is.GreaterThan(355), "Must complete a circuit using movement input without jumping");
            Assert.That(body.linearVelocity.magnitude, Is.GreaterThan(5), "Player must still be moving after returning");
        }
    }
}
