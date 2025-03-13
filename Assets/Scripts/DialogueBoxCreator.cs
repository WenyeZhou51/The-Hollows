using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueBoxCreator : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;

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
        
        if (panelField != null && textField != null)
        {
            panelField.SetValue(dialogueManager, dialoguePanel);
            textField.SetValue(dialogueManager, dialogueText);
        }

        // Hide dialogue panel initially
        dialoguePanel.SetActive(false);
    }
} 