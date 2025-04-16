using UnityEngine;

/// <summary>
/// Simple script to ensure PersistentGameManager exists in the scene.
/// Add this to an object in your starting scene.
/// </summary>
public class PersistentGameManagerInitializer : MonoBehaviour
{
    private void Awake()
    {
        Debug.LogError("[BUILD DEBUG] PersistentGameManagerInitializer.Awake() - Initializing PersistentGameManager");
        
        // Check if the PersistentGameManager instance already exists
        if (PersistentGameManager.Instance != null)
        {
            Debug.LogError("[BUILD DEBUG] PersistentGameManager.Instance already exists with ID: " + PersistentGameManager.Instance.GetInstanceID());
        }
        else
        {
            Debug.LogError("[BUILD DEBUG] PersistentGameManager.Instance is NULL - creating via EnsureExists");
        }
        
        // Make sure the persistent game manager exists
        PersistentGameManager.EnsureExists();
        
        // Verify after EnsureExists
        if (PersistentGameManager.Instance != null)
        {
            Debug.LogError("[BUILD DEBUG] PersistentGameManager.Instance successfully created/verified with ID: " + PersistentGameManager.Instance.GetInstanceID());
        }
        else
        {
            Debug.LogError("[BUILD DEBUG] CRITICAL ERROR: PersistentGameManager.Instance is STILL NULL after EnsureExists!");
        }
    }
} 