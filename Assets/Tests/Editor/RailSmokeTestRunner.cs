using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CeskeDrahy.Tests
{
    /// <summary>Runs the Play Mode integration checks from the Test Runner (Window → General → Test Runner → EditMode).</summary>
    public sealed class RailSmokeTestRunner
    {
        const string ScenePath = "Assets/Scenes/OlomoucHlavniNadrazi.unity";

        [UnityTest]
        public IEnumerator IntegrationChecksPassInPlayMode()
        {
            // Never discard someone's unsaved work: only switch scenes when nothing is dirty.
            var active = SceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                Assert.IsFalse(active.isDirty, "Save or discard the changes in " + active.name + " before running the integration checks");
                EditorSceneManager.OpenScene(ScenePath);
            }
            yield return new EnterPlayMode();
            yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<RailGame>(), "The scene bootstraps the game");
            string result = RailSmokeTests.Run();
            StringAssert.StartsWith("PASS", result);
            yield return new ExitPlayMode();
        }
    }
}
