using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace CeskeDrahy.Tests
{
    /// <summary>Runs the Play Mode integration checks from the Test Runner (Window → General → Test Runner → EditMode).</summary>
    public sealed class RailSmokeTestRunner
    {
        [UnityTest]
        public IEnumerator IntegrationChecksPassInPlayMode()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/OlomoucHlavniNadrazi.unity");
            yield return new EnterPlayMode();
            yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<RailGame>(), "The scene bootstraps the game");
            string result = RailSmokeTests.Run();
            StringAssert.StartsWith("PASS", result);
            yield return new ExitPlayMode();
        }
    }
}
