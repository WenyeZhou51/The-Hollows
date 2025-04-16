#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class CharacterSpeedUpdater : EditorWindow
{
    [MenuItem("Tools/Update Character Speeds")]
    public static void UpdateCharacterSpeeds()
    {
        // Make sure PersistentGameManager exists
        if (PersistentGameManager.Instance == null)
        {
            Debug.LogError("PersistentGameManager.Instance is null. Creating one...");
            PersistentGameManager.EnsureExists();
        }

        if (PersistentGameManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not create PersistentGameManager instance", "OK");
            return;
        }

        // Update speed values directly in the persistent storage
        PersistentGameManager.Instance.SaveCharacterActionSpeed("The Magician", 40f);
        PersistentGameManager.Instance.SaveCharacterActionSpeed("The Fighter", 20f);
        PersistentGameManager.Instance.SaveCharacterActionSpeed("The Bard", 35f);
        PersistentGameManager.Instance.SaveCharacterActionSpeed("The Ranger", 30f);

        // Find any active CombatManager and update current player speeds
        CombatManager combatManager = FindObjectOfType<CombatManager>();
        if (combatManager != null)
        {
            combatManager.UpdatePlayerActionSpeeds();
            EditorUtility.DisplayDialog("Success", "Character speeds updated for current combat scene!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Note", "Character speeds updated in PersistentGameManager. Will take effect in next combat.", "OK");
        }
    }
}
#endif 