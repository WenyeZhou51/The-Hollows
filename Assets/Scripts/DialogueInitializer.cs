using UnityEngine;

public class DialogueInitializer : MonoBehaviour
{
    [SerializeField] private GameObject dialogueCanvasPrefab;
    [SerializeField] private GameObject dialogueButtonPrefab;
    
    private void Awake()
    {
        // Find or create the DialogueManager
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        
        if (dialogueManager == null)
        {
            // Create a new DialogueManager GameObject
            GameObject managerObj = new GameObject("DialogueManager");
            dialogueManager = managerObj.AddComponent<DialogueManager>();
            Debug.Log("Created new DialogueManager");
        }
        
        // Set the DialogueCanvas prefab
        if (dialogueCanvasPrefab != null)
        {
            dialogueManager.SetDialogueCanvasPrefab(dialogueCanvasPrefab);
            Debug.Log("Set DialogueCanvas prefab on DialogueManager");
        }
        else
        {
            Debug.LogError("DialogueCanvas prefab not assigned to DialogueInitializer!");
            
            // Try to load the prefab from Resources as a fallback
            GameObject prefabFromResources = Resources.Load<GameObject>("DialogueCanvas");
            if (prefabFromResources != null)
            {
                dialogueManager.SetDialogueCanvasPrefab(prefabFromResources);
                Debug.Log("Loaded DialogueCanvas prefab from Resources");
            }
            else
            {
                Debug.LogError("Could not find DialogueCanvas prefab in Resources folder!");
            }
        }
        
        // Set the DialogueButton prefab
        if (dialogueButtonPrefab != null)
        {
            dialogueManager.choiceButtonPrefab = dialogueButtonPrefab;
            Debug.Log("Set DialogueButton prefab on DialogueManager");
        }
        else
        {
            Debug.LogWarning("DialogueButton prefab not assigned to DialogueInitializer!");
            
            // Try to load the prefab from Resources as a fallback
            GameObject buttonPrefabFromResources = Resources.Load<GameObject>("DialogueButton");
            if (buttonPrefabFromResources != null)
            {
                dialogueManager.choiceButtonPrefab = buttonPrefabFromResources;
                Debug.Log("Loaded DialogueButton prefab from Resources");
            }
            else
            {
                Debug.LogError("Could not find DialogueButton prefab in Resources folder!");
            }
        }
    }
} 