using UnityEngine;
using Ink.Runtime;
using System.Collections;
using UnityEngine.SceneManagement;

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

        Debug.Log($"GetNextDialogueLine: Story state - canContinue: {_story.canContinue}, choiceCount: {_story.currentChoices.Count}");

        if (_story.canContinue)
        {
            Debug.Log("GetNextDialogueLine: Story can continue, getting next content...");
            string text = _story.Continue();
            text = text.Trim();
            
            // Process any tags before checking the text
            ProcessTags();
            
            // Check if the text is empty or just "..." - if so, consider it as an end of dialogue marker
            if (text == "" || text == "...")
            {
                Debug.Log("GetNextDialogueLine: Found empty node or '...' marker - treating as end of dialogue");
                // Return a special end-of-dialogue signal that DialogueManager will recognize
                return "END_OF_DIALOGUE";
            }
            
            Debug.Log($"GetNextDialogueLine: Returning text: \"{text.Substring(0, Mathf.Min(50, text.Length))}...\"");
            return text;
        }
        else if (_story.currentChoices.Count > 0)
        {
            Debug.Log($"GetNextDialogueLine: Story has {_story.currentChoices.Count} choices, formatting choice text...");
            // Return choices as a formatted string
            string choices = "Choose an option:\n";
            for (int i = 0; i < _story.currentChoices.Count; i++)
            {
                choices += $"{i + 1}. {_story.currentChoices[i].text}\n";
                Debug.Log($"Choice {i}: {_story.currentChoices[i].text}");
            }
            return choices;
        }

        Debug.Log("GetNextDialogueLine: No more content, returning end message");
        return "END_OF_DIALOGUE";
    }

    public void MakeChoice(int choiceIndex)
    {
        if (_story != null && choiceIndex >= 0 && choiceIndex < _story.currentChoices.Count)
        {
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
        StartCoroutine(ScreenFader.Instance.FadeToBlack(afterFadeAction));
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

    public TextAsset InkJSON
    {
        get { return inkJSON; }
        set { inkJSON = value; _isInitialized = false; }
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
} 