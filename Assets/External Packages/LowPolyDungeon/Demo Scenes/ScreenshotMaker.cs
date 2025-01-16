using System.IO;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class ScreenshotMaker : MonoBehaviour
{
    void Update()
    {
        if (Input.GetButtonDown("Submit"))
        {
            var dir = Directory.GetParent((Application.dataPath)).ToString();
            int counter = 0;
            while (true)
            {
                var path = Path.Combine(dir, $"Screenshot_{counter}.png");
                if (File.Exists(path))
                {
                    counter++;
                }
                else
                {
                    ScreenCapture.CaptureScreenshot(path);
                    print($"Saved screenshot to {path}");
                    return;
                }
            }
        }
    }
}
#endif