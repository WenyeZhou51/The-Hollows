using UnityEngine;
using Ink.Runtime;
using System.Collections;

public class InkDialogueHandler : MonoBehaviour
{
    [Header("Ink Settings")]
    [SerializeField] private TextAsset inkJSON;
    [Tooltip("If true, the dialogue will start from the beginning each time")]
    [SerializeField] private bool resetOnInteract = false;

    private Story _story;
    private bool _isInitialized = false;

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
            
            // Check if the text is empty or just "..." and there are tags and the story can continue BEFORE processing tags
            bool hasTags = _story.currentTags.Count > 0;
            if ((text == "" || text == "...") && hasTags && _story.canContinue)
            {
                Debug.Log($"GetNextDialogueLine: Found empty node with tags ({_story.currentTags.Count}), skipping to next content...");
                // Process tags before skipping
                ProcessTags();
                // Skip to next content
                return GetNextDialogueLine();
            }
            
            // Process any tags
            ProcessTags();
            
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
        return "End of dialogue.";
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
        if (_story.currentTags.Count > 0)
        {
            foreach (string tag in _story.currentTags)
            {
                // Get the itemName from story variables
                string dynamicItemName = (string)_story.variablesState["itemName"];
                
                if (tag == "GIVE_ITEM")
                {
                    Debug.Log($"Giving item: {dynamicItemName}");
                    // Add your item giving logic here using dynamicItemName
                }
                // Process any special tags here
                Debug.Log($"Tag: {tag}");
                
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