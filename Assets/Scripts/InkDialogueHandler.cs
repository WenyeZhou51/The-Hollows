using UnityEngine;
using Ink.Runtime;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class InkDialogueHandler : MonoBehaviour
{
    [Header("Ink Settings")]
    [SerializeField] private TextAsset inkJSON;
    [Tooltip("If true, the dialogue will start from the beginning each time")]
    [SerializeField] private bool resetOnInteract = false;
    
    [Header("Ravenbond Penalty Settings")]
    [Tooltip("Amount to reduce max HP by when failing the Ravenbond challenge")]
    [SerializeField] private int ravenbondMaxHPPenalty = 10;
    [Tooltip("Amount to reduce max sanity by when failing the Ravenbond challenge")]
    [SerializeField] private int ravenbondMaxSanityPenalty = 10;
    [Tooltip("Debug key to manually trigger the Ravenbond penalty (editor only)")]
    [SerializeField] private KeyCode ravenbondDebugKey = KeyCode.F8;

    private Story _story;
    private bool _isInitialized = false;
    private string lastSelectedChoiceText = null;

    // Property to get/set the InkJSON
    public TextAsset InkJSON
    {
        get { return inkJSON; }
        set { inkJSON = value; }
    }

    private void Update()
    {
        // Debug trigger for Ravenbond penalty (only in editor)
        #if UNITY_EDITOR
        if (Input.GetKeyDown(ravenbondDebugKey))
        {
            Debug.Log("DEBUG: Manually triggering Ravenbond penalty");
            ApplyRavenbondPenalty();
        }
        #endif
    }

    public void InitializeStory()
    {
        if (inkJSON != null)
        {
            // If already initialized and not set to reset, don't reinitialize
            if (_isInitialized && !resetOnInteract)
            {
                Debug.Log($"Story already initialized for {gameObject.name}, not resetting due to resetOnInteract=false");
                return;
            }
            
            _story = new Story(inkJSON.text);
            _isInitialized = true;
            Debug.Log($"Initialized Ink story for {gameObject.name} (resetOnInteract={resetOnInteract})");
        }
        else
        {
            Debug.LogError($"No Ink JSON file assigned to {gameObject.name}");
        }
    }

    public string GetNextDialogueLine()
    {
        if (!_isInitialized)
        {
            Debug.Log("GetNextDialogueLine: Story not initialized, initializing now...");
            InitializeStory();
        }

        if (_story == null)
        {
            Debug.LogError("GetNextDialogueLine: No story loaded.");
            return "Error: No story loaded.";
        }

        Debug.Log($"[DEBUG OBELISK TRANSITION] GetNextDialogueLine: Story state - canContinue: {_story.canContinue}, choiceCount: {_story.currentChoices.Count}");

        if (_story.canContinue)
        {
            Debug.Log("[DEBUG OBELISK TRANSITION] Story can continue, getting next content...");
            string text = _story.Continue();
            text = text.Trim();
            
            // Process any tags before checking the text
            ProcessTags();
            
            // Debug log the full text being returned with explicit line count/end of text markers
            Debug.Log($"[DEBUG OBELISK TRANSITION] Dialogue line: \"{text}\" (Length: {text.Length}, Lines: {text.Split('\n').Length})");
            Debug.Log($"[DEBUG OBELISK TRANSITION] Can continue after this line: {_story.canContinue}");
            
            // Check if the text is empty or just "..." - if so, consider it as an end of dialogue marker
            if (text == "" || text == "...")
            {
                Debug.Log("[DEBUG OBELISK TRANSITION] Found empty node or '...' marker - treating as end of dialogue");
                // Return a special end-of-dialogue signal that DialogueManager will recognize
                return "END_OF_DIALOGUE";
            }
            
            // If we have a stored choice text and the text starts with it, remove it
            if (lastSelectedChoiceText != null && text.StartsWith(lastSelectedChoiceText, System.StringComparison.OrdinalIgnoreCase))
            {
                string originalText = text;
                text = text.Substring(lastSelectedChoiceText.Length).Trim();
                Debug.Log($"[DEBUG OBELISK TRANSITION] Removed choice text from beginning of dialogue. Original: '{originalText}', New: '{text}'");
            }
            
            // Clear the lastSelectedChoiceText to avoid affecting subsequent dialogue lines
            lastSelectedChoiceText = null;
            
            Debug.Log($"[DEBUG OBELISK TRANSITION] Returning text: \"{text.Substring(0, Mathf.Min(50, text.Length))}...\"");
            return text;
        }
        else if (_story.currentChoices.Count > 0)
        {
            // CRITICAL FIX: Instead of formatting choices as text, return a special signal
            // that tells DialogueManager to handle choices properly.
            // This prevents "Choose an option:" from showing up in dialogue.
            Debug.Log($"[DEBUG OBELISK TRANSITION] Story has {_story.currentChoices.Count} choices, returning choice signal");
            return "SHOW_CHOICES";
        }

        Debug.Log("[DEBUG OBELISK TRANSITION] No more content, returning end message");
        return "END_OF_DIALOGUE";
    }

    // Get Ink story choices directly
    public List<Choice> GetCurrentChoices()
    {
        if (_story == null || !_isInitialized)
        {
            Debug.LogError("GetCurrentChoices: No active story is available");
            return new List<Choice>();
        }
        
        return _story.currentChoices;
    }

    public void MakeChoice(int choiceIndex)
    {
        if (_story != null && choiceIndex >= 0 && choiceIndex < _story.currentChoices.Count)
        {
            // Store the selected choice text before selecting it
            lastSelectedChoiceText = _story.currentChoices[choiceIndex].text;
            Debug.Log($"Stored selected choice text: '{lastSelectedChoiceText}'");
            
            // ONLY select the choice, do NOT continue the story here
            // Let DialogueManager.ContinueInkStory handle the continuation through GetNextDialogueLine
            _story.ChooseChoiceIndex(choiceIndex);
            Debug.Log($"Selected choice {choiceIndex} in InkDialogueHandler");
            
            // DO NOT call _story.Continue() here - this was causing the issue
            // The story will be continued properly in GetNextDialogueLine when called by DialogueManager
        }
    }

    public bool HasNextLine()
    {
        // Initialize the story if it's not initialized yet
        if (!_isInitialized)
        {
            Debug.Log($"[{gameObject.name}] HasNextLine: Story not initialized, initializing now...");
            InitializeStory();
            // If still not initialized (no ink file), return false
            if (!_isInitialized)
            {
                Debug.Log($"[{gameObject.name}] HasNextLine: Failed to initialize story, returning false");
                return false;
            }
        }
        
        bool hasNext = _story != null && (_story.canContinue || _story.currentChoices.Count > 0);
        Debug.Log($"[{gameObject.name}] HasNextLine: {hasNext} (canContinue: {(_story != null ? _story.canContinue : false)}, choices: {(_story != null ? _story.currentChoices.Count : 0)})");
        return hasNext;
    }

    public void ResetStory()
    {
        if (_isInitialized)
        {
            _story = new Story(inkJSON.text);
            Debug.Log($"Reset Ink story for {gameObject.name}");
        }
    }

    private void ProcessTags()
    {
        bool penaltyApplied = false;
        bool coldKeyAdded = false;
        
        if (_story.currentTags.Count > 0)
        {
            foreach (string tag in _story.currentTags)
            {
                // Process any special tags here
                Debug.Log($"Tag: {tag}");
                
                // Try to get item name from story variables
                string dynamicItemName = "";
                if (_story.variablesState.GlobalVariableExistsWithName("itemName"))
                {
                    dynamicItemName = (string)_story.variablesState["itemName"];
                }
                
                // Check for Cold Key tag - CRITICAL FIX for timing issue
                if (tag == "GIVE_COLD_KEY" && !coldKeyAdded)
                {
                    Debug.Log("GIVE_COLD_KEY tag detected - adding Cold Key to inventory");
                    AddColdKeyToInventory();
                    coldKeyAdded = true;
                }
                
                // This check is still here as a backup, but won't be the main trigger anymore
                // because of timing issues with variable assignment
                if (_story.variablesState.GlobalVariableExistsWithName("has_cold_key") && 
                    (bool)_story.variablesState["has_cold_key"] == true && !coldKeyAdded)
                {
                    Debug.Log("Ravenbond win detected via variable - adding Cold Key to inventory");
                    AddColdKeyToInventory();
                    coldKeyAdded = true;
                }
                
                if (tag == "GIVE_ITEM")
                {
                    Debug.Log($"Giving item: {dynamicItemName}");
                    // Add your item giving logic here using dynamicItemName
                }
                
                // Check for Ravenbond failure effect
                if (tag == "RAVENBOND_FAILURE" && !penaltyApplied)
                {
                    Debug.Log("Ravenbond failure detected via tag - reducing Magician's max HP and sanity");
                    ApplyRavenbondPenalty();
                    penaltyApplied = true;
                }
                
                // Check for exit comics tag
                if (tag == "SHOW_EXIT_COMICS")
                {
                    Debug.Log("Exit comics sequence tag detected - will show comics before exiting");
                    StartCoroutine(TriggerExitComics());
                }
                
                // Example: Parse commands like "GIVE_ITEM:HealthPotion"
                if (tag.Contains(":"))
                {
                    string[] parts = tag.Split(':');
                    string command = parts[0];
                    string parameter = parts[1];
                    
                    switch (command)
                    {
                        case "GIVE_ITEM":
                            Debug.Log($"Would give item: {parameter}");
                            // Implement item giving logic here
                            break;
                        case "SET_FLAG":
                            Debug.Log($"Would set flag: {parameter}");
                            // Implement flag setting logic here
                            break;
                    }
                }
            }
        }
        
        // Debug - print the current text for inspection
        if (_story.currentText != null)
        {
            Debug.Log($"Current story text: \"{_story.currentText}\"");
        }
        
        // Check for the specific Ravenbond failure message in the dialogue
        // Only apply if not already applied via tag
        if (!penaltyApplied && _story.currentText != null && 
            (_story.currentText.Contains("You lose max HP and max Sanity.") || 
             _story.currentText.Contains("You lose 10 max HP and max Sanity."))) // Check both formats for backward compatibility
        {
            Debug.Log("Ravenbond penalty message detected via text content - reducing Magician's max HP and sanity");
            ApplyRavenbondPenalty();
        }
    }
    
    /// <summary>
    /// Applies the Ravenbond game penalty - reduces The Magician's max HP and sanity
    /// </summary>
    private void ApplyRavenbondPenalty()
    {
        // Make sure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        // The character ID for The Magician
        string magicianId = "The Magician";
        
        // Get current max values
        int currentMaxHealth = PersistentGameManager.Instance.GetCharacterMaxHealth(magicianId);
        int currentMaxSanity = PersistentGameManager.Instance.GetCharacterMaxMind(magicianId);
        
        // Get current values
        int currentHealth = PersistentGameManager.Instance.GetCharacterHealth(magicianId, currentMaxHealth);
        int currentSanity = PersistentGameManager.Instance.GetCharacterMind(magicianId, currentMaxSanity);
        
        Debug.Log($"Before Ravenbond penalty - The Magician: HP={currentHealth}/{currentMaxHealth}, Mind={currentSanity}/{currentMaxSanity}");
        Debug.Log($"Applying Ravenbond penalty: -{ravenbondMaxHPPenalty} Max HP, -{ravenbondMaxSanityPenalty} Max Sanity");
        
        // Apply the configured penalties
        int newMaxHealth = currentMaxHealth - ravenbondMaxHPPenalty;
        int newMaxSanity = currentMaxSanity - ravenbondMaxSanityPenalty;
        
        // Allow max values to reach 0 to trigger game over
        newMaxHealth = Mathf.Max(0, newMaxHealth);
        newMaxSanity = Mathf.Max(0, newMaxSanity);
        
        // Current values should not exceed max values
        int newHealth = Mathf.Min(currentHealth, newMaxHealth);
        int newSanity = Mathf.Min(currentSanity, newMaxSanity);
        
        // Save the updated values
        PersistentGameManager.Instance.SaveCharacterStats(magicianId, newHealth, newMaxHealth, newSanity, newMaxSanity);
        
        Debug.Log($"After Ravenbond penalty - The Magician: HP={newHealth}/{newMaxHealth}, Mind={newSanity}/{newMaxSanity}");
        
        // Check if the Magician is now dead (HP or maxHP reached 0)
        if (newHealth <= 0 || newMaxHealth <= 0)
        {
            Debug.Log("The Magician's health has reached 0 - triggering game over");
            StartCoroutine(TriggerGameOver());
        }
    }
    
    /// <summary>
    /// Triggers a game over sequence when the Magician's health reaches 0
    /// </summary>
    private IEnumerator TriggerGameOver()
    {
        Debug.Log("Starting game over sequence...");
        
        // Make sure ScreenFader exists
        ScreenFader.EnsureExists();
        
        // Make sure DialogueManager exists and close any active dialogue
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.CloseDialogue();
        }
        
        // Wait a moment before starting the transition
        yield return new WaitForSeconds(0.5f);
        
        // Display the collapse message
        DialogueManager.Instance.ShowDialogue("You collapsed onto the floor. The stars are distant. Your vision fades to black.");
        
        // Wait for the message to be displayed
        yield return new WaitForSeconds(3.0f);
        
        Debug.Log("Initiating fade to black...");
        
        // Create a callback to execute after the fade completes
        System.Action afterFadeAction = () => {
            Debug.Log("Fade completed, resetting game data and loading start menu");
            // Reset all game data except deaths
            ResetGameDataExceptDeaths();
            
            // Check if the scene exists in build settings
            string startMenuSceneName = "Start_Menu";
            if (IsSceneInBuildSettings(startMenuSceneName))
            {
                // Load the start menu scene
                Debug.Log($"Loading start menu scene: {startMenuSceneName}");
                SceneManager.LoadScene(startMenuSceneName);
            }
            else
            {
                Debug.LogError($"Scene '{startMenuSceneName}' not found in build settings. Make sure to add it to the build settings!");
            }
        };
        
        // Start the fade with the callback
        ScreenFader.EnsureExists();
        if (ScreenFader.Instance != null)
        {
            StartCoroutine(ScreenFader.Instance.FadeToBlack());
            // Wait for fade to complete, then execute the action
            StartCoroutine(ExecuteAfterFade(afterFadeAction));
        }
        else
        {
            // If fader not available, just execute the action directly
            afterFadeAction.Invoke();
        }
    }
    
    /// <summary>
    /// Execute an action after a short delay (used for fade completion)
    /// </summary>
    private IEnumerator ExecuteAfterFade(Action action)
    {
        // Wait for fade duration plus a small buffer
        yield return new WaitForSeconds(1.2f);
        action.Invoke();
    }
    
    /// <summary>
    /// Check if a scene is included in the build settings
    /// </summary>
    private bool IsSceneInBuildSettings(string sceneName)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Resets all game data while preserving the death counter
    /// </summary>
    private void ResetGameDataExceptDeaths()
    {
        // Make sure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        if (PersistentGameManager.Instance != null)
        {
            // Store the current death count
            int currentDeaths = PersistentGameManager.Instance.GetDeaths();
            
            Debug.Log($"Resetting game data after Ravenbond game over. Current deaths: {currentDeaths}");
            
            // Reset all game data
            PersistentGameManager.Instance.ResetAllData();
            
            // Restore the death count
            for (int i = 0; i < currentDeaths; i++)
            {
                PersistentGameManager.Instance.IncrementDeaths();
            }
            
            // Increment deaths one more time for this death
            PersistentGameManager.Instance.IncrementDeaths();
            
            Debug.Log($"Game data reset complete. Deaths after increment: {PersistentGameManager.Instance.GetDeaths()}");
        }
    }

    /// <summary>
    /// Triggers the exit comics sequence using the existing ComicsDisplayController
    /// </summary>
    private IEnumerator TriggerExitComics()
    {
        Debug.Log("Triggering exit comics sequence");
        
        // Find the ComicsDisplayController in the scene
        ComicsDisplayController controller = ComicsDisplayController.Instance;
        
        // If controller exists, use it to display comics
        if (controller != null)
        {
            // Wait a moment for dialogue to close
            yield return new WaitForSeconds(0.2f);
            
            // Start the comic sequence - the controller already has the panels configured
            controller.StartComicSequence();
            
            // Since we can't directly check how many panels there are (private field),
            // use a reasonable default delay based on typical comic sequences
            float comicDisplayTime = 10f; // Default reasonable time for viewing comics
            StartCoroutine(DelayedQuit(comicDisplayTime));
        }
        else
        {
            Debug.LogWarning("No ComicsDisplayController found in scene - cannot show exit comics");
            // If no controller, just quit immediately
            Application.Quit();
        }
    }

    /// <summary>
    /// Delays quitting the application to allow viewing comics
    /// </summary>
    private IEnumerator DelayedQuit(float delay)
    {
        Debug.Log($"Will quit application after {delay} seconds");
        yield return new WaitForSeconds(delay);
        
        // Quit the application
        Debug.Log("Quitting application after comic display");
        Application.Quit();
    }

    public void SetStoryVariable(string variableName, string value)
    {
        if (_story != null)
        {
            // Check if the variable exists in the story before setting it
            if (_story.variablesState.GlobalVariableExistsWithName(variableName))
            {
                _story.variablesState[variableName] = value;
                Debug.Log($"Set Ink variable {variableName} to {value}");
            }
            else
            {
                Debug.LogWarning($"Cannot set variable '{variableName}' - it doesn't exist in the Ink story for {gameObject.name}");
                // Don't throw an exception, but log a warning
            }
        }
        else
        {
            Debug.LogWarning($"Cannot set variable '{variableName}' - story is null for {gameObject.name}");
        }
    }

    public void SetStoryVariable(string variableName, bool value)
    {
        if (_story != null)
        {
            // Check if the variable exists in the story before setting it
            if (_story.variablesState.GlobalVariableExistsWithName(variableName))
            {
                _story.variablesState[variableName] = value;
                Debug.Log($"Set Ink variable {variableName} to {value}");
            }
            else
            {
                Debug.LogWarning($"Cannot set variable '{variableName}' - it doesn't exist in the Ink story for {gameObject.name}");
                // Don't throw an exception, but log a warning
            }
        }
        else
        {
            Debug.LogWarning($"Cannot set variable '{variableName}' - story is null for {gameObject.name}");
        }
    }

    public bool IsInitialized()
    {
        return _isInitialized;
    }

    // Add a method to get the current text with choice prefix removed
    public string GetCleanCurrentText()
    {
        if (_story == null)
        {
            Debug.LogError("GetCleanCurrentText: No story loaded.");
            return "Error: No story loaded.";
        }
        
        string currentText = _story.currentText;
        
        // If we have a stored choice text and the current text starts with it, remove it
        if (lastSelectedChoiceText != null && currentText != null && 
            currentText.StartsWith(lastSelectedChoiceText, System.StringComparison.OrdinalIgnoreCase))
        {
            string originalText = currentText;
            currentText = currentText.Substring(lastSelectedChoiceText.Length).Trim();
            Debug.Log($"GetCleanCurrentText: Removed choice text from dialogue. Original: '{originalText}', New: '{currentText}'");
        }
        
        // Clear the lastSelectedChoiceText to avoid affecting subsequent dialogue lines
        lastSelectedChoiceText = null;
        
        return currentText;
    }

    /// <summary>
    /// Add the Cold Key to the player's inventory
    /// </summary>
    private void AddColdKeyToInventory()
    {
        // Make sure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        if (PersistentGameManager.Instance == null)
        {
            Debug.LogError("CRITICAL ERROR: Failed to access PersistentGameManager for Cold Key - this will prevent the key from being added to inventory");
            return;
        }
        
        Debug.Log("=== BEGIN COLD KEY PROCESS ===");
        
        // Create the Cold Key as a KeyItem
        ItemData coldKey = new ItemData(
            "Cold Key", 
            "A frigid key that seems to emanate cold. You won it from a mysterious figure in a game of Ravenbond.", 
            1, 
            false, 
            ItemData.ItemType.KeyItem
        );
        
        // Log inventory state before adding key
        var existingInventory = PersistentGameManager.Instance.GetPlayerInventory();
        bool alreadyHasKey = existingInventory.ContainsKey("Cold Key");
        Debug.Log($"Current inventory has {existingInventory.Count} items. Already has Cold Key: {alreadyHasKey}");
        
        // Add to player's inventory via PersistentGameManager
        PersistentGameManager.Instance.AddItemToInventory(coldKey.name, coldKey.amount);
        
        // Verify the key was added
        var updatedInventory = PersistentGameManager.Instance.GetPlayerInventory();
        bool keyAdded = updatedInventory.ContainsKey("Cold Key");
        Debug.Log($"After adding to PersistentGameManager - Key present: {keyAdded}");
        
        // Try to add to the active player inventory if we're in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("Found player in scene, attempting to add Cold Key directly to PlayerInventory component");
            PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                // First check if player already has the key to avoid duplication
                bool playerHasKey = false;
                foreach (var item in playerInventory.Items)
                {
                    if (item.name == "Cold Key")
                    {
                        playerHasKey = true;
                        Debug.Log("Player already has Cold Key in inventory");
                        break;
                    }
                }
                
                if (!playerHasKey)
                {
                    playerInventory.AddItem(coldKey);
                    Debug.Log("Successfully added Cold Key to active player inventory component");
                    
                    // Trigger inventory UI refresh if available
                    var inventoryUI = FindObjectOfType<InventoryUI>();
                    if (inventoryUI != null)
                    {
                        Debug.Log("Found InventoryUI, refreshing display");
                        inventoryUI.RefreshInventoryUI();
                    }
                }
            }
            else
            {
                Debug.LogWarning("Player found but has no PlayerInventory component");
            }
        }
        else
        {
            Debug.Log("Player not found in scene - Cold Key will be loaded from PersistentGameManager when player returns to overworld");
        }
        
        Debug.Log("CRITICAL NOTICE: Cold Key has been added to inventory through the GIVE_COLD_KEY tag");
        Debug.Log("=== END COLD KEY PROCESS ===");
    }
} 