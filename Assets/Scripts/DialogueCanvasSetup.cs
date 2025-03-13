using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

// This script is used to help set up the DialogueCanvas prefab
[ExecuteInEditMode]
public class DialogueCanvasSetup : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject dialogueButtonContainer;
    
    [ContextMenu("Setup Canvas Components")]
    public void SetupCanvasComponents()
    {
        // Add Canvas component if it doesn't exist
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Debug.Log("Added Canvas component");
            }
        }
        
        // Add CanvasScaler if it doesn't exist
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            Debug.Log("Added CanvasScaler component");
        }
        
        // Add GraphicRaycaster if it doesn't exist
        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log("Added GraphicRaycaster component");
        }
        
        // Set up DialoguePanel if it doesn't exist
        if (dialoguePanel == null)
        {
            // Check if it already exists as a child
            Transform panelTransform = transform.Find("DialoguePanel");
            if (panelTransform != null)
            {
                dialoguePanel = panelTransform.gameObject;
            }
            else
            {
                // Create DialoguePanel
                dialoguePanel = new GameObject("DialoguePanel");
                dialoguePanel.transform.SetParent(transform, false);
                
                // Add RectTransform
                RectTransform panelRect = dialoguePanel.AddComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.1f, 0.1f);
                panelRect.anchorMax = new Vector2(0.9f, 0.3f);
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                
                // Add Image component
                Image panelImage = dialoguePanel.AddComponent<Image>();
                panelImage.color = new Color(0, 0, 0, 0.8f);
                
                Debug.Log("Created DialoguePanel");
            }
        }
        
        // Set up DialogueText if it doesn't exist
        if (dialogueText == null)
        {
            // Check if it already exists as a child of DialoguePanel
            Transform textTransform = dialoguePanel.transform.Find("DialogueText");
            if (textTransform != null)
            {
                dialogueText = textTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                // Create DialogueText
                GameObject textObj = new GameObject("DialogueText");
                textObj.transform.SetParent(dialoguePanel.transform, false);
                
                // Add RectTransform
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.05f, 0.1f);
                textRect.anchorMax = new Vector2(0.95f, 0.9f);
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                
                // Add TextMeshProUGUI component
                dialogueText = textObj.AddComponent<TextMeshProUGUI>();
                dialogueText.alignment = TextAlignmentOptions.Left;
                dialogueText.fontSize = 36;
                dialogueText.color = Color.white;
                dialogueText.text = "";
                
                Debug.Log("Created DialogueText");
            }
        }
        
        // Set up DialogueButtonContainer if it doesn't exist
        if (dialogueButtonContainer == null)
        {
            // Check if it already exists as a child
            Transform containerTransform = transform.Find("DialogueButtonContainer");
            if (containerTransform != null)
            {
                dialogueButtonContainer = containerTransform.gameObject;
            }
            else
            {
                // Create DialogueButtonContainer
                dialogueButtonContainer = new GameObject("DialogueButtonContainer");
                dialogueButtonContainer.transform.SetParent(transform, false);
                
                // Add RectTransform
                RectTransform containerRect = dialogueButtonContainer.AddComponent<RectTransform>();
                containerRect.anchorMin = new Vector2(0.5f, 0.5f);
                containerRect.anchorMax = new Vector2(0.5f, 0.5f);
                containerRect.anchoredPosition = new Vector2(0, -323);
                containerRect.sizeDelta = new Vector2(960, 136.25f);
                
                // Add VerticalLayoutGroup
                VerticalLayoutGroup layoutGroup = dialogueButtonContainer.AddComponent<VerticalLayoutGroup>();
                layoutGroup.padding = new RectOffset(0, 0, 0, 0);
                layoutGroup.spacing = 0;
                layoutGroup.childAlignment = TextAnchor.UpperCenter;
                layoutGroup.childControlWidth = false;
                layoutGroup.childControlHeight = false;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.childForceExpandHeight = true;
                
                Debug.Log("Created DialogueButtonContainer");
            }
        }
        
        // Initially hide panels
        dialoguePanel.SetActive(false);
        dialogueButtonContainer.SetActive(false);
        
        Debug.Log("Canvas components set up successfully");
    }
    
    // This method can be called from the editor to create a prefab
    [ContextMenu("Create DialogueCanvas Prefab")]
    public void CreatePrefab()
    {
        #if UNITY_EDITOR
        // First make sure all components are set up
        SetupCanvasComponents();
        
        // Create the prefab
        string prefabPath = "Assets/Prefabs/DialogueCanvas.prefab";
        
        // Check if Resources folder exists, if not create it
        if (!System.IO.Directory.Exists("Assets/Resources"))
        {
            System.IO.Directory.CreateDirectory("Assets/Resources");
            Debug.Log("Created Resources folder");
        }
        
        // Create a copy in the Resources folder
        string resourcesPrefabPath = "Assets/Resources/DialogueCanvas.prefab";
        
        // Create or update the prefab
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null)
        {
            // Update existing prefab
            PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);
            Debug.Log("Updated DialogueCanvas prefab at: " + prefabPath);
            
            // Also update the Resources copy
            PrefabUtility.SaveAsPrefabAsset(gameObject, resourcesPrefabPath);
            Debug.Log("Updated DialogueCanvas prefab in Resources folder");
        }
        else
        {
            // Create new prefab
            PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);
            Debug.Log("Created DialogueCanvas prefab at: " + prefabPath);
            
            // Also create a copy in the Resources folder
            PrefabUtility.SaveAsPrefabAsset(gameObject, resourcesPrefabPath);
            Debug.Log("Created DialogueCanvas prefab in Resources folder");
        }
        #endif
    }
}

#if UNITY_EDITOR
// Custom editor for DialogueCanvasSetup
[CustomEditor(typeof(DialogueCanvasSetup))]
public class DialogueCanvasSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        DialogueCanvasSetup setup = (DialogueCanvasSetup)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Setup Canvas Components"))
        {
            setup.SetupCanvasComponents();
        }
        
        if (GUILayout.Button("Create/Update Prefab"))
        {
            setup.CreatePrefab();
        }
    }
}
#endif

#endif // UNITY_EDITOR 