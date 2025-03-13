using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class SetupInitializer
{
    static SetupInitializer()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }
    
    static void OnHierarchyChanged()
    {
        // Check if we're in the Overworld_entrance scene
        if (EditorSceneManager.GetActiveScene().name == "Overworld_entrance")
        {
            // Check if setup object already exists
            GameObject setupObj = GameObject.Find("AutoSetup");
            if (setupObj == null)
            {
                // Create setup object
                setupObj = new GameObject("AutoSetup");
                setupObj.AddComponent<AutoSceneSetup>();
                Debug.Log("Added AutoSceneSetup to scene");
                
                // Mark scene as dirty
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }
}
#endif

// Runtime component to ensure the setup runs when the game starts
public class RuntimeSetupInitializer : MonoBehaviour
{
    private void Awake()
    {
        // Check if setup object already exists
        GameObject setupObj = GameObject.Find("AutoSetup");
        if (setupObj == null)
        {
            // Create setup object
            setupObj = new GameObject("AutoSetup");
            setupObj.AddComponent<AutoSceneSetup>();
            Debug.Log("Added AutoSceneSetup to scene at runtime");
        }
    }
} 