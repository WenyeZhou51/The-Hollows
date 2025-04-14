using UnityEngine;

public class ObeliskDialogueTransition : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "Battle_Obelisk";
    [SerializeField] private string targetMarkerId = "PlayerStart";
    private bool dialogueCompleted = false;
    
    private void OnEnable()
    {
        // Subscribe to dialogue events
        DialogueManager.OnDialogueStateChanged += HandleDialogueStateChanged;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from dialogue events
        DialogueManager.OnDialogueStateChanged -= HandleDialogueStateChanged;
    }
    
    private void HandleDialogueStateChanged(bool isActive)
    {
        // If dialogue just ended and our ink story was the active one
        if (!isActive && !dialogueCompleted) 
        {
            // Mark as completed to prevent multiple transitions
            dialogueCompleted = true;
            
            // Get the InkDialogueHandler
            InkDialogueHandler inkHandler = GetComponent<InkDialogueHandler>();
            
            // Only transition if we have a valid handler and it's initialized
            if (inkHandler != null && inkHandler.IsInitialized())
            {
                // Check if we've reached the end of the story (no more content to display)
                if (!inkHandler.HasNextLine())
                {
                    Debug.Log("Obelisk dialogue completed - transitioning to battle");
                    
                    // Find the player object
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player == null)
                    {
                        Debug.LogError("Could not find player for scene transition!");
                        return;
                    }
                    
                    // Transition to the battle scene
                    SceneTransitionManager.EnsureExists();
                    SceneTransitionManager.Instance.TransitionToScene(battleSceneName, targetMarkerId, player);
                }
            }
        }
    }
} 