using UnityEngine;

/// <summary>
/// Simple script to ensure PersistentGameManager exists in the scene.
/// Add this to an object in your starting scene.
/// </summary>
public class PersistentGameManagerInitializer : MonoBehaviour
{
    private void Awake()
    {
        // Make sure the persistent game manager exists
        PersistentGameManager.EnsureExists();
    }
} 