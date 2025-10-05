using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to test tutorial highlighting in play mode
/// </summary>
[CustomEditor(typeof(TutorialHighlighter))]
public class TutorialHighlightTester : Editor
{
    private string testElementName = "player_health";
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        TutorialHighlighter highlighter = (TutorialHighlighter)target;
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test highlighting", MessageType.Info);
            return;
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Test Highlighting", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        testElementName = EditorGUILayout.TextField("Element Name:", testElementName);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Highlight"))
        {
            highlighter.HighlightElement(testElementName);
        }
        
        if (GUILayout.Button("Remove Highlight"))
        {
            highlighter.RemoveHighlight(testElementName);
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Clear All Highlights"))
        {
            highlighter.RemoveAllHighlights();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Test Buttons", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Player HP"))
        {
            highlighter.HighlightElement("player_health");
        }
        if (GUILayout.Button("Player Mind"))
        {
            highlighter.HighlightElement("player_mind");
        }
        if (GUILayout.Button("Player Action"))
        {
            highlighter.HighlightElement("player_action");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Attack Button"))
        {
            highlighter.HighlightElement("attack_button");
        }
        if (GUILayout.Button("Guard Button"))
        {
            highlighter.HighlightElement("guard_button");
        }
        if (GUILayout.Button("Skills Button"))
        {
            highlighter.HighlightElement("skill_button");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Available element names:\n" +
            "• player_health, player_mind, player_action\n" +
            "• enemy_health, enemy_action\n" +
            "• attack_button, guard_button, skill_button, item_button\n" +
            "• Or use exact GameObject names from hierarchy",
            MessageType.Info
        );
    }
}

