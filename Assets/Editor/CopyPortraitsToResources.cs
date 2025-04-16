using UnityEngine;
using UnityEditor;
using System.IO;

public class CopyPortraitsToResources : EditorWindow
{
    [MenuItem("Tools/Copy Portraits to Resources")]
    public static void ShowWindow()
    {
        GetWindow<CopyPortraitsToResources>("Copy Portraits");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Portrait System Helper", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "This tool will copy all portrait images from 'Assets/Sprites/Portraits/' to 'Assets/Resources/Portraits/' " +
            "to ensure they work in builds. It will also create consistent filenames using lowercase and underscores.", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Copy All Portraits to Resources"))
        {
            CopyAllPortraits();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Create Only Missing Portraits"))
        {
            CopyMissingPortraits();
        }
    }
    
    private void CopyAllPortraits()
    {
        string sourceDir = "Assets/Sprites/Portraits";
        string targetDir = "Assets/Resources/Portraits";
        
        // Ensure target directory exists
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
            AssetDatabase.Refresh();
        }
        
        if (Directory.Exists(sourceDir))
        {
            string[] portraitFiles = Directory.GetFiles(sourceDir, "*.png");
            int count = 0;
            
            foreach (string sourcePath in portraitFiles)
            {
                // Get filename and create standardized version
                string originalFilename = Path.GetFileNameWithoutExtension(sourcePath);
                string standardizedFilename = originalFilename.Replace(" ", "_").ToLowerInvariant();
                string targetPath = Path.Combine(targetDir, standardizedFilename + ".png");
                
                // Copy the file
                File.Copy(sourcePath, targetPath, true);
                count++;
                
                // Log the copy operation
                Debug.Log($"Copied portrait: {originalFilename} -> {standardizedFilename}");
            }
            
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Portrait Copy Complete", 
                $"Successfully copied {count} portraits to Resources folder.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", 
                "Source directory 'Assets/Sprites/Portraits' not found!", "OK");
        }
    }
    
    private void CopyMissingPortraits()
    {
        string sourceDir = "Assets/Sprites/Portraits";
        string targetDir = "Assets/Resources/Portraits";
        
        // Ensure target directory exists
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
            AssetDatabase.Refresh();
        }
        
        if (Directory.Exists(sourceDir))
        {
            string[] portraitFiles = Directory.GetFiles(sourceDir, "*.png");
            int count = 0;
            
            foreach (string sourcePath in portraitFiles)
            {
                // Get filename and create standardized version
                string originalFilename = Path.GetFileNameWithoutExtension(sourcePath);
                string standardizedFilename = originalFilename.Replace(" ", "_").ToLowerInvariant();
                string targetPath = Path.Combine(targetDir, standardizedFilename + ".png");
                
                // Only copy if the file doesn't exist in the target directory
                if (!File.Exists(targetPath))
                {
                    File.Copy(sourcePath, targetPath, false);
                    count++;
                    
                    // Log the copy operation
                    Debug.Log($"Copied missing portrait: {originalFilename} -> {standardizedFilename}");
                }
            }
            
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Portrait Copy Complete", 
                $"Successfully copied {count} missing portraits to Resources folder.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", 
                "Source directory 'Assets/Sprites/Portraits' not found!", "OK");
        }
    }
} 