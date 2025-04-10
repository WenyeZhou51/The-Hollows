using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSceneSetup : MonoBehaviour
{
    [Header("Dialogue Components")]
    [SerializeField] private GameObject dialogueCanvasPrefab;
    [SerializeField] private GameObject choiceButtonPrefab;
    
    [Header("Battle Dialogue")]
    [SerializeField] private TextAsset victoryDialogueJSON;
    
    private void Awake()
    {
        // Create the dialogue manager if it doesn't exist
        DialogueManager dialogueManager = DialogueManager.Instance;
        if (dialogueManager == null)
        {
            dialogueManager = DialogueManager.CreateInstance();
            
            // Assign prefabs if specified
            if (dialogueCanvasPrefab != null)
            {
                dialogueManager.SetDialogueCanvasPrefab(dialogueCanvasPrefab);
            }
        }
        
        // Create a GameObject for battle dialogue triggers
        GameObject dialogueTriggerObj = new GameObject("BattleDialogueTrigger");
        BattleDialogueTrigger trigger = dialogueTriggerObj.AddComponent<BattleDialogueTrigger>();
        
        // Set victory dialogue directly
        // Access the field through reflection to avoid build errors
        if (victoryDialogueJSON != null)
        {
            var field = typeof(BattleDialogueTrigger).GetField("victoryDialogueJSON", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(trigger, victoryDialogueJSON);
            }
        }
        
        Debug.Log("BattleSceneSetup: Dialogue system initialized");
    }
} 