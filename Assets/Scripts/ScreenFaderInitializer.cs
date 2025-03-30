using UnityEngine;

/// <summary>
/// Simple script to ensure ScreenFader exists in the scene.
/// Add this to an object in your starting scene.
/// </summary>
public class ScreenFaderInitializer : MonoBehaviour
{
    private void Awake()
    {
        // Make sure the screen fader exists
        ScreenFader.EnsureExists();
    }
} 