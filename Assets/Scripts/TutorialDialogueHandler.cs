using UnityEngine;
using Ink.Runtime;

/// <summary>
/// Extended InkDialogueHandler that processes tutorial-specific tags for highlighting UI elements
/// </summary>
public class TutorialDialogueHandler : InkDialogueHandler
{
    private TutorialHighlighter highlighter;
    
    private void Awake()
    {
        // Ensure TutorialHighlighter exists
        highlighter = FindObjectOfType<TutorialHighlighter>();
        if (highlighter == null)
        {
            GameObject highlighterObj = new GameObject("TutorialHighlighter");
            highlighter = highlighterObj.AddComponent<TutorialHighlighter>();
            Debug.Log("[Tutorial] Created TutorialHighlighter instance");
        }
    }
    
    /// <summary>
    /// Override GetNextDialogueLine to process tutorial tags
    /// </summary>
    public override string GetNextDialogueLine()
    {
        // Get the base dialogue line
        string line = base.GetNextDialogueLine();
        
        // Process tutorial tags after getting the line
        ProcessTutorialTags();
        
        return line;
    }
    
    /// <summary>
    /// Processes tutorial-specific tags from the current Ink story
    /// </summary>
    private void ProcessTutorialTags()
    {
        // Access the story using reflection (same pattern as InkDialogueHandler)
        Story story = GetStoryFromHandler();
        if (story == null || story.currentTags.Count == 0)
        {
            return;
        }
        
        foreach (string tag in story.currentTags)
        {
            Debug.Log($"[Tutorial] Processing tag: {tag}");
            
            // Parse highlight tags: HIGHLIGHT:element_name
            if (tag.StartsWith("HIGHLIGHT:", System.StringComparison.OrdinalIgnoreCase))
            {
                string elementName = tag.Substring(10).Trim();
                if (!string.IsNullOrEmpty(elementName))
                {
                    highlighter.HighlightElement(elementName);
                    Debug.Log($"[Tutorial] Highlighting element: {elementName}");
                }
            }
            // Parse unhighlight tags: UNHIGHLIGHT:element_name
            else if (tag.StartsWith("UNHIGHLIGHT:", System.StringComparison.OrdinalIgnoreCase))
            {
                string elementName = tag.Substring(12).Trim();
                if (!string.IsNullOrEmpty(elementName))
                {
                    highlighter.RemoveHighlight(elementName);
                    Debug.Log($"[Tutorial] Removing highlight from: {elementName}");
                }
            }
            // Parse flash tags: FLASH:element_name
            else if (tag.StartsWith("FLASH:", System.StringComparison.OrdinalIgnoreCase))
            {
                string elementName = tag.Substring(6).Trim();
                if (!string.IsNullOrEmpty(elementName))
                {
                    highlighter.StartFlashing(elementName);
                    Debug.Log($"[Tutorial] Starting flash on: {elementName}");
                }
            }
            // Parse stop flash tags: STOPFLASH:element_name
            else if (tag.StartsWith("STOPFLASH:", System.StringComparison.OrdinalIgnoreCase))
            {
                string elementName = tag.Substring(10).Trim();
                if (!string.IsNullOrEmpty(elementName))
                {
                    highlighter.StopFlashing(elementName);
                    Debug.Log($"[Tutorial] Stopping flash on: {elementName}");
                }
            }
            // Clear all highlights
            else if (tag.Equals("CLEAR_HIGHLIGHTS", System.StringComparison.OrdinalIgnoreCase))
            {
                highlighter.RemoveAllHighlights();
                Debug.Log("[Tutorial] Cleared all highlights");
            }
        }
    }
    
    /// <summary>
    /// Helper method to access the private _story field using reflection
    /// </summary>
    private Story GetStoryFromHandler()
    {
        System.Type type = typeof(InkDialogueHandler);
        System.Reflection.FieldInfo storyField = type.GetField("_story", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (storyField != null)
        {
            return (Story)storyField.GetValue(this);
        }
        
        return null;
    }
    
    private void OnDestroy()
    {
        // Clean up highlights when dialogue handler is destroyed
        if (highlighter != null)
        {
            highlighter.RemoveAllHighlights();
        }
    }
}

