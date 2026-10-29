using RyansLibrary.Labyrinth;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SeedSweepTests
{
    private const int k_baseSeedCount = 200;
    private const float k_timeout = 10f;

    // A Test behaves as an ordinary method
    [UnitySetUp]
    public IEnumerator SeedSweepTestsSimplePasses()
    {
        // Use the Assert class to test conditions
        yield return SceneManager.LoadSceneAsync("GenerationTest");
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator SeedSweepTestsWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        MapGeneratorController controller = MapGeneratorController.Instance;
        if (controller == null)
            Debug.LogError("MapGeneratorController does not exist.");

        controller.ToggleOperationStepwiseDebugging(false);



        yield return null;
    }
}
