using UnityEngine;
using UnityEngine.SceneManagement;

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
            
            Debug.LogError($"[CRITICAL DEBUG] ObeliskDialogueTransition.HandleDialogueStateChanged called in scene: {SceneManager.GetActiveScene().name}");
            
            // Get the InkDialogueHandler
            InkDialogueHandler inkHandler = GetComponent<InkDialogueHandler>();
            
            // Only transition if we have a valid handler and it's initialized
            if (inkHandler != null && inkHandler.IsInitialized())
            {
                Debug.LogError($"[CRITICAL DEBUG] InkDialogueHandler is initialized: {inkHandler.IsInitialized()}, HasNextLine: {inkHandler.HasNextLine()}");
                
                // Check if we've reached the end of the story (no more content to display)
                if (!inkHandler.HasNextLine())
                {
                    Debug.LogError("[CRITICAL DEBUG] Obelisk dialogue completed - transitioning to Battle_Obelisk");
                    
                    // Find the player object
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player == null)
                    {
                        Debug.LogError("[OBELISK DEBUG] Could not find player for scene transition!");
                        return;
                    }
                    
                    // Ensure SceneTransitionManager exists
                    SceneTransitionManager.EnsureExists();
                    
                    // CRITICAL FIX: Store the current scene name in SceneTransitionManager 
                    // so it knows where to return after combat
                    string currentScene = SceneManager.GetActiveScene().name;
                    Debug.LogError($"[CRITICAL DEBUG] About to set return scene to: {currentScene}, Scene exists in build settings: {SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + currentScene + ".unity") != -1}");
                    
                    // Check if SceneTransitionManager instance exists
                    if (SceneTransitionManager.Instance == null)
                    {
                        Debug.LogError("[CRITICAL DEBUG] SEVERE ERROR: SceneTransitionManager.Instance is NULL when trying to set return scene!");
                    }
                    else
                    {
                        Debug.LogError($"[CRITICAL DEBUG] SceneTransitionManager instance exists with ID: {SceneTransitionManager.Instance.GetInstanceID()}");
                        
                        // VITAL - Write to PlayerPrefs directly as a backup
                        PlayerPrefs.SetString("ReturnSceneName", currentScene);
                        PlayerPrefs.Save();
                        Debug.LogError($"[CRITICAL DEBUG] Also directly saved return scene to PlayerPrefs: {currentScene}");
                        
                        SceneTransitionManager.Instance.SetReturnScene(currentScene);
                        Debug.LogError($"[CRITICAL DEBUG] Set return scene to: {currentScene} for after battle");
                        
                        // Verify PlayerPrefs were set
                        string savedValue = PlayerPrefs.GetString("ReturnSceneName", "NOT_SET");
                        Debug.LogError($"[CRITICAL DEBUG] Verified PlayerPrefs value is: {savedValue}");
                    }
                    
                    // Transition to the battle scene
                    Debug.LogError($"[CRITICAL DEBUG] Now calling TransitionToScene to: {battleSceneName}, marker: {targetMarkerId}");
                    SceneTransitionManager.Instance.TransitionToScene(battleSceneName, targetMarkerId, player);
                }
            }
        }
    }
} 