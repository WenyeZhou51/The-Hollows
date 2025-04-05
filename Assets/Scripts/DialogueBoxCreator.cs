using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueBoxCreator : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject dialogueButtonContainer;

    private void Awake()
    {
        if (canvas == null)
        {
            // Create canvas if it doesn't exist
            GameObject canvasObj = new GameObject("DialogueCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // Add canvas scaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Add graphic raycaster
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (dialoguePanel == null)
        {
            // Create dialogue panel
            dialoguePanel = new GameObject("DialoguePanel");
            dialoguePanel.transform.SetParent(canvas.transform, false);
            
            // Add image component for background
            Image panelImage = dialoguePanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            // Set panel position and size
            RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.3f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }

        if (dialogueText == null)
        {
            // Create text object
            GameObject textObj = new GameObject("DialogueText");
            textObj.transform.SetParent(dialoguePanel.transform, false);
            
            // Add TextMeshPro component
            dialogueText = textObj.AddComponent<TextMeshProUGUI>();
            dialogueText.alignment = TextAlignmentOptions.Left;
            dialogueText.fontSize = 36;
            dialogueText.color = Color.white;
            
            // Set text position and size
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.1f);
            textRect.anchorMax = new Vector2(0.95f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
        
        // Create dialogue button container inside the panel
        if (dialogueButtonContainer == null)
        {
            // Check if it already exists as a child of the panel
            Transform existingContainer = dialoguePanel.transform.Find("DialogueButtonContainer");
            if (existingContainer != null)
            {
                dialogueButtonContainer = existingContainer.gameObject;
                Debug.Log("Found existing DialogueButtonContainer in the panel");
            }
            else
            {
                // Create the container
                dialogueButtonContainer = new GameObject("DialogueButtonContainer");
                dialogueButtonContainer.transform.SetParent(dialoguePanel.transform, false);
                
                // Configure the container
                RectTransform containerRect = dialogueButtonContainer.AddComponent<RectTransform>();
                containerRect.anchorMin = new Vector2(0.05f, 0.25f);
                containerRect.anchorMax = new Vector2(0.95f, 0.95f);
                containerRect.offsetMin = Vector2.zero;
                containerRect.offsetMax = Vector2.zero;
                
                // Add layout group
                VerticalLayoutGroup layoutGroup = dialogueButtonContainer.AddComponent<VerticalLayoutGroup>();
                layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                layoutGroup.spacing = 10;
                layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                layoutGroup.childControlHeight = true;
                layoutGroup.childControlWidth = true;
                
                Debug.Log("Created DialogueButtonContainer inside DialoguePanel");
            }
            
            // Initially hide the button container
            dialogueButtonContainer.SetActive(false);
        }

        // Get or create DialogueManager
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager == null)
        {
            GameObject managerObj = new GameObject("DialogueManager");
            dialogueManager = managerObj.AddComponent<DialogueManager>();
        }

        // Set references using reflection to access private serialized fields
        System.Type type = dialogueManager.GetType();
        System.Reflection.FieldInfo panelField = type.GetField("dialoguePanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo textField = type.GetField("dialogueText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo buttonContainerField = type.GetField("dialogueButtonContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (panelField != null)
        {
            panelField.SetValue(dialogueManager, dialoguePanel);
            Debug.Log("Set dialoguePanel reference in DialogueManager");
        }
        
        if (textField != null)
        {
            textField.SetValue(dialogueManager, dialogueText);
            Debug.Log("Set dialogueText reference in DialogueManager");
        }
        
        if (buttonContainerField != null)
        {
            buttonContainerField.SetValue(dialogueManager, dialogueButtonContainer);
            Debug.Log("Set dialogueButtonContainer reference in DialogueManager");
        }

        // Hide dialogue panel initially
        dialoguePanel.SetActive(false);
    }
} 