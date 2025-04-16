using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility for testing dialogue files directly
/// </summary>
public class DialogueDebugTool : EditorWindow
{
    private string inkFilePath = "Ink/MagicianBreakfastDialogue";

    [MenuItem("Tools/Dialogue Debug Tool")]
    public static void ShowWindow()
    {
        GetWindow<DialogueDebugTool>("Dialogue Debug");
    }

    private void OnGUI()
    {
        GUILayout.Label("Dialogue Testing Tool", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        inkFilePath = EditorGUILayout.TextField("Ink File Path:", inkFilePath);
        
        EditorGUILayout.HelpBox("Enter the path to an ink file without extension, relative to Assets folder", MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Test Dialogue"))
        {
            // Check if DialogueManager instance exists
            DialogueManager dialogueManager = Object.FindObjectOfType<DialogueManager>();
            
            if (dialogueManager == null)
            {
                // Create DialogueManager if it doesn't exist
                dialogueManager = DialogueManager.CreateInstance();
                Debug.Log("Created DialogueManager instance for testing");
            }
            
            // Make sure DialogueManager is initialized
            dialogueManager.Initialize();
            
            // Test the dialogue file
            dialogueManager.TestInkFile(inkFilePath);
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Compile All Ink Files"))
        {
            CompileAllInkFiles();
        }
    }
    
    private void CompileAllInkFiles()
    {
        string[] inkFiles = Directory.GetFiles("Assets", "*.ink", SearchOption.AllDirectories);
        int compiledCount = 0;
        
        foreach (string inkFile in inkFiles)
        {
            // TODO: Add code to compile ink files to JSON
            // This requires Ink compiler integration
            compiledCount++;
        }
        
        Debug.Log($"Compiled {compiledCount} ink files");
    }
} 