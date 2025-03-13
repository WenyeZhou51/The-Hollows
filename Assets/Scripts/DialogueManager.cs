using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ink.Runtime;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private GameObject choiceButtonPrefab;
    
    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.04f;
    [SerializeField] private bool useTypewriterEffect = true;
    
    private bool isDialogueActive = false;
    private InkDialogueHandler currentInkHandler;
    private List<GameObject> choiceButtons = new List<GameObject>();
    private Coroutine typingCoroutine;

    private void Awake()
    {
        // Ensure this object persists between scenes
        DontDestroyOnLoad(gameObject);
        
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("DialogueManager instance created");
        }
        else
        {
            Debug.Log("Duplicate DialogueManager found, destroying this one");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Make sure dialogue panel is initially hidden
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            Debug.Log("Dialogue panel initialized and hidden");
        }
        else
        {
            Debug.LogError("Dialogue panel is not assigned!");
            
            // Try to find the dialogue panel in the scene
            GameObject panelObj = GameObject.Find("DialoguePanel");
            if (panelObj != null)
            {
                dialoguePanel = panelObj;
                Debug.Log("Found DialoguePanel in scene");
                
                // Try to find the dialogue text
                TextMeshProUGUI[] textComponents = panelObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (TextMeshProUGUI text in textComponents)
                {
                    if (text.gameObject.name == "DialogueText")
                    {
                        dialogueText = text;
                        Debug.Log("Found DialogueText in DialoguePanel");
                        break;
                    }
                }
            }
            else
            {
                Debug.LogError("Could not find DialoguePanel in scene");
            }
        }
        
        // Make sure choices panel is initially hidden
        if (choicesPanel != null)
        {
            choicesPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Choices panel is not assigned!");
            
            // Try to find the choices panel in the scene
            GameObject choicesPanelObj = GameObject.Find("ChoicesPanel");
            if (choicesPanelObj != null)
            {
                choicesPanel = choicesPanelObj;
                Debug.Log("Found ChoicesPanel in scene");
            }
        }
        
        // Make sure choice button prefab is assigned
        if (choiceButtonPrefab == null)
        {
            Debug.LogError("Choice button prefab is not assigned!");
            
            // Try to find the choice button prefab in the scene
            GameObject prefabObj = GameObject.Find("ChoiceButtonPrefab");
            if (prefabObj != null)
            {
                choiceButtonPrefab = prefabObj;
                Debug.Log("Found ChoiceButtonPrefab in scene");
            }
        }
    }

    public void ShowDialogue(string message)
    {
        Debug.Log("ShowDialogue called with message: " + message);
        
        // Debug all the references first
        Debug.Log($"Reference check - Panel: {(dialoguePanel != null ? dialoguePanel.name : "NULL")}, " +
                 $"Text: {(dialogueText != null ? dialogueText.name : "NULL")}");
        
        if (dialoguePanel != null && dialogueText != null)
        {
            // Debug the hierarchy
            Transform parent = dialoguePanel.transform.parent;
            Debug.Log($"Hierarchy check - Panel parent: {(parent != null ? parent.name : "NULL")} " +
                     $"(Active: {(parent != null ? parent.gameObject.activeSelf : false)})");
            
            // Make sure the parent canvas is active
            if (parent != null && !parent.gameObject.activeSelf)
            {
                Debug.Log("Activating parent canvas: " + parent.name);
                parent.gameObject.SetActive(true);
            }
            
            // Make sure the panel is active and visible
            Debug.Log($"Setting dialoguePanel active (currently: {dialoguePanel.activeSelf})");
            dialoguePanel.SetActive(true);
            
            // Debug the RectTransform
            RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                Debug.Log($"Panel RectTransform - AnchorMin: {panelRect.anchorMin}, AnchorMax: {panelRect.anchorMax}, " +
                         $"SizeDelta: {panelRect.sizeDelta}, Position: {panelRect.position}");
                
                // Make sure the panel is visible within the canvas
                panelRect.anchorMin = new Vector2(0.1f, 0.1f);
                panelRect.anchorMax = new Vector2(0.9f, 0.3f);
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                
                Debug.Log($"Updated Panel RectTransform - AnchorMin: {panelRect.anchorMin}, AnchorMax: {panelRect.anchorMax}");
            }
            
            // Debug the Image component
            Image panelImage = dialoguePanel.GetComponent<Image>();
            if (panelImage != null)
            {
                Debug.Log($"Panel Image - Color: {panelImage.color}, Enabled: {panelImage.enabled}");
                
                // Ensure the image is visible
                if (panelImage.color.a < 0.1f)
                {
                    Color color = panelImage.color;
                    color.a = 0.8f;
                    panelImage.color = color;
                    Debug.Log($"Updated Panel Image Color: {panelImage.color}");
                }
            }
            
            // Debug the text component
            Debug.Log($"Text component - Color: {dialogueText.color}, Text: '{dialogueText.text}', " +
                     $"Enabled: {dialogueText.enabled}, Font size: {dialogueText.fontSize}");
            
            // Set the text
            if (useTypewriterEffect)
            {
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                }
                Debug.Log("Starting typewriter effect for: " + message);
                typingCoroutine = StartCoroutine(TypeText(message));
            }
            else
            {
                Debug.Log("Setting text directly: " + message);
                dialogueText.text = message;
            }
            
            isDialogueActive = true;
            Debug.Log("Dialogue shown successfully");
        }
        else
        {
            Debug.LogError("Dialogue panel or text component not assigned! Panel: " + 
                          (dialoguePanel != null ? "OK" : "NULL") + ", Text: " + 
                          (dialogueText != null ? "OK" : "NULL"));
        }
    }
    
    public void StartInkDialogue(InkDialogueHandler inkHandler)
    {
        if (inkHandler == null)
        {
            Debug.LogError("Null InkDialogueHandler passed to StartInkDialogue");
            return;
        }
        
        // Debug UI hierarchy
        if (dialoguePanel != null)
        {
            Transform canvas = dialoguePanel.transform.parent;
            Debug.Log($"UI Hierarchy - Canvas: {(canvas ? canvas.name : "NULL")} (Active: {(canvas ? canvas.gameObject.activeSelf : false)}), " +
                     $"Panel: {dialoguePanel.name} (Active: {dialoguePanel.activeSelf})");
        }
        
        currentInkHandler = inkHandler;
        
        if (inkHandler.InkJSON == null)
        {
            ShowDialogue("Error: No ink story assigned to this object.");
            return;
        }
        
        // Reset the story if needed
        if (inkHandler.GetComponent<InteractableBox>() != null || 
            inkHandler.GetComponent<InteractableNPC>() != null)
        {
            // Get the resetOnInteract value using reflection
            System.Type type = inkHandler.GetType();
            System.Reflection.FieldInfo resetField = type.GetField("resetOnInteract", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (resetField != null && (bool)resetField.GetValue(inkHandler))
            {
                inkHandler.ResetStory();
            }
        }
        
        // Show the first line of dialogue
        ContinueInkStory();
    }
    
    public void ContinueInkStory()
    {
        if (currentInkHandler == null)
        {
            Debug.LogError("No active ink story to continue");
            return;
        }
        
        // Clear any existing choice buttons
        ClearChoices();
        
        if (currentInkHandler.HasNextLine())
        {
            string nextLine = currentInkHandler.GetNextDialogueLine();
            ShowDialogue(nextLine);
            
            // Check if we need to show choices
            Story story = GetStoryFromHandler();
            if (story != null && story.currentChoices.Count > 0)
            {
                StartCoroutine(ShowChoicesAfterDelay(0.5f));
            }
        }
        else
        {
            CloseDialogue();
        }
    }
    
    private Story GetStoryFromHandler()
    {
        if (currentInkHandler == null) return null;
        
        // Use reflection to access the private _story field
        System.Type type = currentInkHandler.GetType();
        System.Reflection.FieldInfo storyField = type.GetField("_story", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (storyField != null)
        {
            return (Story)storyField.GetValue(currentInkHandler);
        }
        
        return null;
    }
    
    private IEnumerator ShowChoicesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Story story = GetStoryFromHandler();
        if (story == null || story.currentChoices.Count == 0) yield break;
        
        // Show the choices panel
        if (choicesPanel != null)
        {
            choicesPanel.SetActive(true);
            
            // Create buttons for each choice
            for (int i = 0; i < story.currentChoices.Count; i++)
            {
                GameObject choiceObj = Instantiate(choiceButtonPrefab, choicesPanel.transform);
                choiceObj.SetActive(true); // Ensure the button is active
                choiceButtons.Add(choiceObj);
                
                TextMeshProUGUI choiceText = choiceObj.GetComponentInChildren<TextMeshProUGUI>();
                if (choiceText != null)
                {
                    choiceText.text = story.currentChoices[i].text;
                }
                else
                {
                    Debug.LogError("No TextMeshProUGUI component found in choice button children");
                }
                
                Button button = choiceObj.GetComponent<Button>();
                if (button != null)
                {
                    int choiceIndex = i; // Need to store this in a local variable for the closure
                    button.onClick.AddListener(() => MakeChoice(choiceIndex));
                }
                else
                {
                    Debug.LogError("No Button component found on choice button");
                }
            }
            
            // Log that choices were created
            Debug.Log($"Created {story.currentChoices.Count} choice buttons");
        }
        else
        {
            Debug.LogError("Choices panel is null");
        }
    }
    
    public void MakeChoice(int choiceIndex)
    {
        if (currentInkHandler == null) return;
        
        currentInkHandler.MakeChoice(choiceIndex);
        ClearChoices();
        ContinueInkStory();
    }
    
    private void ClearChoices()
    {
        if (choicesPanel != null)
        {
            choicesPanel.SetActive(false);
        }
        
        foreach (GameObject button in choiceButtons)
        {
            Destroy(button);
        }
        choiceButtons.Clear();
    }

    public void CloseDialogue()
    {
        Debug.Log("CloseDialogue called");
        
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            isDialogueActive = false;
            Debug.Log("Dialogue closed successfully");
        }
        
        // Make sure choices panel is closed
        ClearChoices();
        currentInkHandler = null;
        
        // Reset player control
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            PlayerController playerController = playerObj.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Use reflection to set canMove to true
                System.Type type = playerController.GetType();
                System.Reflection.FieldInfo field = type.GetField("canMove", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    field.SetValue(playerController, true);
                    Debug.Log("Reset player control");
                }
            }
        }
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
    
    private IEnumerator TypeText(string text)
    {
        dialogueText.text = "";
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    public void Initialize(GameObject panel, TextMeshProUGUI text, GameObject choices, GameObject buttonPrefab)
    {
        Debug.Log("DialogueManager.Initialize called with direct references");
        
        // Set all the references directly
        dialoguePanel = panel;
        dialogueText = text;
        choicesPanel = choices;
        choiceButtonPrefab = buttonPrefab;
        
        // Verify the references
        Debug.Log($"DialogueManager initialized with Panel: {(dialoguePanel != null ? dialoguePanel.name : "NULL")}, " +
                 $"Text: {(dialogueText != null ? dialogueText.name : "NULL")}, " +
                 $"Choices: {(choicesPanel != null ? choicesPanel.name : "NULL")}, " +
                 $"Button: {(choiceButtonPrefab != null ? choiceButtonPrefab.name : "NULL")}");
    }
} 