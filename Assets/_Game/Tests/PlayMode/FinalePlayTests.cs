using System.Collections;
using NUnit.Framework;
using PlanetHarvest.Core;
using PlanetHarvest.Mining;
using PlanetHarvest.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlanetHarvest.Tests
{
    public class FinalePlayTests
    {
        [UnityTest]
        public IEnumerator SettingsOpenWithF10ApplyAtOnceAndEndingShowsStatistics()
        {
#if UNITY_EDITOR
            bool previousBackground = Application.runInBackground;
            var previousBehavior = InputSystem.settings.backgroundBehavior;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var previousRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var previousLanguage = Localization.Language;
            try
            {
                yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
                yield return new WaitForSeconds(.4f);
                var mining = Object.FindFirstObjectByType<EarthMining>();
                var hud = mining.GetComponent<EarthHud>();
                yield return Press(keyboard, Key.F10);
                Assert.AreEqual(FieldMenu.Settings, mining.Menu);
                var settings = hud.Settings;
                var before = settings.Settings.language;
                settings.ToggleLanguage();
                Assert.AreNotEqual(before, settings.Settings.language, "Toggling switches the language.");
                Assert.AreEqual(settings.Settings.language, Localization.Language);
                settings.Settings.masterVolume = .4f;
                settings.Apply(false);
                Assert.AreEqual(.4f, AudioListener.volume, 1e-5);
                yield return Press(keyboard, Key.Escape);
                Assert.AreEqual(FieldMenu.Closed, mining.Menu);

                StringAssert.Contains(Localization.Text("scanner.locked"), mining.Scan());

                mining.ShowEnding();
                Assert.AreEqual(FieldMenu.Ending, mining.Menu);
                yield return null;
                Assert.IsTrue(GameObject.Find("Endsequenz").activeInHierarchy);
                StringAssert.Contains("Credits", hud.Ending.Describe(mining.Elevator.Freight,
                    Progression.PlanetProgress.Measure(mining.Planet.Data.InitialCount, mining.Elevator.Freight, 0), 3725));
                yield return Press(keyboard, Key.Escape);
                Assert.AreEqual(FieldMenu.Closed, mining.Menu);
            }
            finally
            {
                Localization.Language = previousLanguage;
                AudioListener.volume = 1;
                InputSystem.RemoveDevice(keyboard);
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = previousRouting;
                InputSystem.settings.backgroundBehavior = previousBehavior;
                Application.runInBackground = previousBackground;
#endif
            }
        }

        private static IEnumerator Press(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }
    }
}
