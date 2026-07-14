using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


public class NewTestScript
{
    // A Test behaves as an ordinary method
    [Test]
    public void NewTestScriptSimplePasses()
    {
        // Arrange  (Set test data and conditions)
        int a = 2;
        int b = 3;

        // Act      (Execute the code you're testing)
        int c = a + b;

        // Assert   (Make sure results are what you expect)
        Assert.AreEqual(5, c);

        // Cleanup  (Optional: Clean up any resources used in the test)
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScript1WithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
