using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Special transition trigger that checks if the player has completed the tutorial.
/// If not, it triggers the tutorial intro sequence instead of going directly to Overworld_Entrance.
/// Place this on the exit from Startroom.
/// </summary>
public class FirstTutorialTrigger : MonoBehaviour
{
    [Header("Normal Transition Settings")]
    [Tooltip("Scene to transition to after tutorial is completed")]
    [SerializeField] private string targetSceneName = "Overworld_Entrance";
    [Tooltip("Marker ID in the target scene")]
    [SerializeField] private string targetMarkerId = "bottom_entrance";
    
    [Header("Tutorial Settings")]
    [Tooltip("Ink dialogue to show before tutorial combat")]
    [SerializeField] private TextAsset tutorialIntroDialogue;
    
    [Header("Optional Settings")]
    [Tooltip("If true, transition happens automatically on trigger enter")]
    [SerializeField] private bool autoTransition = true;
    
    private bool hasTriggered = false;
    private InkDialogueHandler dialogueHandler;
    private bool dialogueCompleted = false;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && autoTransition && !hasTriggered)
        {
            hasTriggered = true;
            TriggerTransition(other.gameObject);
        }
    }
    
    private void TriggerTransition(GameObject player)
    {
        // Ensure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        // Check if tutorial has been completed
        bool tutorialCompleted = PersistentGameManager.Instance.GetCustomDataValue("TutorialCompleted", false);
        
        Debug.Log($"[FirstTutorialTrigger] Tutorial completed: {tutorialCompleted}");
        
        if (tutorialCompleted)
        {
            // Tutorial already done - proceed with normal transition
            Debug.Log("[FirstTutorialTrigger] Tutorial already completed - normal transition");
            PerformNormalTransition(player);
        }
        else
        {
            // Tutorial not completed yet - trigger tutorial sequence
            Debug.Log("[FirstTutorialTrigger] Starting tutorial sequence");
            StartCoroutine(StartTutorialSequence());
        }
    }
    
    private void PerformNormalTransition(GameObject player)
    {
        // Store transition data
        PlayerPrefs.SetString("LastTargetSceneName", targetSceneName);
        PlayerPrefs.SetString("LastTargetMarkerId", targetMarkerId);
        PlayerPrefs.SetInt("NeedsPlayerSetup", 1);
        PlayerPrefs.Save();
        
        // Make sure SceneTransitionManager exists
        SceneTransitionManager.EnsureExists();
        
        // Call the transition method
        SceneTransitionManager.Instance.TransitionToScene(targetSceneName, targetMarkerId, player);
        
        Debug.Log($"[FirstTutorialTrigger] Normal transition to {targetSceneName} at {targetMarkerId}");
    }
    
    private IEnumerator StartTutorialSequence()
    {
        Debug.Log("[FirstTutorialTrigger] Starting tutorial intro sequence");
        
        // Fade to black
        ScreenFader.EnsureExists();
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeToBlack());
        }
        
        // Store where we need to go after tutorial
        PlayerPrefs.SetString("PostTutorialScene", targetSceneName);
        PlayerPrefs.SetString("PostTutorialMarker", targetMarkerId);
        PlayerPrefs.SetInt("PostTutorialTransition", 1);
        PlayerPrefs.Save();
        
        // Setup dialogue handler
        if (tutorialIntroDialogue == null)
        {
            Debug.LogError("[FirstTutorialTrigger] No tutorial intro dialogue assigned!");
            // Skip straight to combat
            SceneManager.LoadScene("Battle_Tutorial");
            yield break;
        }
        
        // Create dialogue handler if needed
        dialogueHandler = GetComponent<InkDialogueHandler>();
        if (dialogueHandler == null)
        {
            dialogueHandler = gameObject.AddComponent<InkDialogueHandler>();
        }
        dialogueHandler.InkJSON = tutorialIntroDialogue;
        
        // Ensure DialogueManager exists
        if (DialogueManager.Instance == null)
        {
            DialogueManager.CreateInstance();
        }
        
        // Subscribe to dialogue state changes
        DialogueManager.OnDialogueStateChanged += OnDialogueStateChanged;
        
        // Wait a moment for everything to be ready
        yield return new WaitForSeconds(0.5f);
        
        // Initialize and start dialogue
        dialogueHandler.InitializeStory();
        DialogueManager.Instance.StartInkDialogue(dialogueHandler);
        
        Debug.Log("[FirstTutorialTrigger] Started tutorial intro dialogue");
    }
    
    private void OnDialogueStateChanged(bool isActive)
    {
        if (!isActive && !dialogueCompleted)
        {
            dialogueCompleted = true;
            
            // Unsubscribe
            DialogueManager.OnDialogueStateChanged -= OnDialogueStateChanged;
            
            Debug.Log("[FirstTutorialTrigger] Dialogue completed - Loading Battle_Tutorial");
            
            // Load tutorial combat scene
            SceneManager.LoadScene("Battle_Tutorial");
        }
    }
    
    private void OnDestroy()
    {
        // Clean up event subscription
        DialogueManager.OnDialogueStateChanged -= OnDialogueStateChanged;
    }
    
    // Visualize in editor
    private void OnDrawGizmos()
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            // Draw in yellow to distinguish from regular transition areas
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawCube(transform.position + (Vector3)boxCollider.offset, boxCollider.size);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.yellow;
            string infoText = $"TUTORIAL TRIGGER\n→ Normal: {targetSceneName}\n   Marker: {targetMarkerId}\n→ Tutorial: Battle_Tutorial";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, infoText);
            #endif
        }
    }
}

