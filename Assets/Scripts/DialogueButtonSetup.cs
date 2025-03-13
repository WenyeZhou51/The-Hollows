using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

// This script is used to help set up the DialogueButton prefab
[ExecuteInEditMode]
public class DialogueButtonSetup : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI buttonText;
    
    [Header("Button Settings")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color highlightedColor = new Color(0.4f, 0.6f, 1f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    
    private void OnValidate()
    {
        // Auto-find components if not assigned
        if (button == null)
            button = GetComponent<Button>();
        
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
        
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
        
        // Set up button colors
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            button.colors = colors;
        }
        
        // Set up image color
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
        }
        
        // Set up text
        if (buttonText != null)
        {
            buttonText.color = Color.white;
            if (string.IsNullOrEmpty(buttonText.text))
                buttonText.text = "Choice Option";
        }
    }
    
    [ContextMenu("Setup Button Components")]
    public void SetupButtonComponents()
    {
        // Add Button component if it doesn't exist
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
            Debug.Log("Added Button component");
        }
        
        // Add Image component if it doesn't exist
        if (buttonImage == null)
        {
            buttonImage = gameObject.AddComponent<Image>();
            buttonImage.color = normalColor;
            Debug.Log("Added Image component");
        }
        
        // Add TextMeshProUGUI if it doesn't exist
        if (buttonText == null)
        {
            // Check if there's a child with TextMeshProUGUI
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
            
            if (buttonText == null)
            {
                // Create a child for the text
                GameObject textObj = new GameObject("Text (TMP)");
                textObj.transform.SetParent(transform, false);
                
                // Set up RectTransform
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.offsetMin = new Vector2(10, 5);
                textRect.offsetMax = new Vector2(-10, -5);
                
                // Add TextMeshProUGUI component
                buttonText = textObj.AddComponent<TextMeshProUGUI>();
                buttonText.text = "Choice Option";
                buttonText.color = Color.white;
                buttonText.alignment = TextAlignmentOptions.Center;
                buttonText.fontSize = 24;
                
                Debug.Log("Created Text (TMP) child with TextMeshProUGUI component");
            }
        }
        
        // Set up button colors
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        button.colors = colors;
        
        Debug.Log("Button components set up successfully");
    }
    
    // This method can be called from the editor to create a prefab
    [ContextMenu("Create DialogueButton Prefab")]
    public void CreatePrefab()
    {
        #if UNITY_EDITOR
        // First make sure all components are set up
        SetupButtonComponents();
        
        // Create the prefab
        string prefabPath = "Assets/Prefabs/DialogueButton.prefab";
        
        // Check if Resources folder exists, if not create it
        if (!System.IO.Directory.Exists("Assets/Resources"))
        {
            System.IO.Directory.CreateDirectory("Assets/Resources");
            Debug.Log("Created Resources folder");
        }
        
        // Create a copy in the Resources folder
        string resourcesPrefabPath = "Assets/Resources/DialogueButton.prefab";
        
        // Create or update the prefab
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null)
        {
            // Update existing prefab
            PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);
            Debug.Log("Updated DialogueButton prefab at: " + prefabPath);
            
            // Also update the Resources copy
            PrefabUtility.SaveAsPrefabAsset(gameObject, resourcesPrefabPath);
            Debug.Log("Updated DialogueButton prefab in Resources folder");
        }
        else
        {
            // Create new prefab
            PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);
            Debug.Log("Created DialogueButton prefab at: " + prefabPath);
            
            // Also create a copy in the Resources folder
            PrefabUtility.SaveAsPrefabAsset(gameObject, resourcesPrefabPath);
            Debug.Log("Created DialogueButton prefab in Resources folder");
        }
        #endif
    }
}

#if UNITY_EDITOR
// Custom editor for DialogueButtonSetup
[CustomEditor(typeof(DialogueButtonSetup))]
public class DialogueButtonSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        DialogueButtonSetup setup = (DialogueButtonSetup)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Setup Button Components"))
        {
            setup.SetupButtonComponents();
        }
        
        if (GUILayout.Button("Create/Update Prefab"))
        {
            setup.CreatePrefab();
        }
    }
}
#endif

#endif // UNITY_EDITOR 