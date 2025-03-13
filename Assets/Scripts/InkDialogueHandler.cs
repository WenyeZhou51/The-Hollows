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
            _story = new Story(inkJSON.text);
            _isInitialized = true;
            Debug.Log($"Initialized Ink story for {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"No Ink JSON file assigned to {gameObject.name}");
        }
    }

    public string GetNextDialogueLine()
    {
        if (!_isInitialized)
        {
            InitializeStory();
        }

        if (_story == null)
        {
            return "Error: No story loaded.";
        }

        if (_story.canContinue)
        {
            string text = _story.Continue();
            text = text.Trim();
            
            // Process any tags
            ProcessTags();
            
            return text;
        }
        else if (_story.currentChoices.Count > 0)
        {
            // Return choices as a formatted string
            string choices = "Choose an option:\n";
            for (int i = 0; i < _story.currentChoices.Count; i++)
            {
                choices += $"{i + 1}. {_story.currentChoices[i].text}\n";
            }
            return choices;
        }

        return "End of dialogue.";
    }

    public void MakeChoice(int choiceIndex)
    {
        if (_story != null && choiceIndex >= 0 && choiceIndex < _story.currentChoices.Count)
        {
            _story.ChooseChoiceIndex(choiceIndex);
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
} 