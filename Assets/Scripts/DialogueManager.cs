using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ink.Runtime;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Prefab Reference")]
    [SerializeField] private GameObject dialogueCanvasPrefab;
    [Tooltip("Assign this in the inspector! This is the button prefab used for dialogue choices")]
    [SerializeField] public GameObject choiceButtonPrefab; // Made public to ensure it's exposed in the inspector

    [Header("UI Components")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject dialogueButtonContainer; // Changed from choicesPanel to match your prefab
    
    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.04f;
    [SerializeField] private bool useTypewriterEffect = true;
    [SerializeField] private Color normalButtonColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color highlightedButtonColor = new Color(0.4f, 0.6f, 1f, 1f);
    [SerializeField] private KeyCode interactKey = KeyCode.Z;
    
    private bool isDialogueActive = false;
    private InkDialogueHandler currentInkHandler;
    private List<GameObject> choiceButtons = new List<GameObject>();
    private Coroutine typingCoroutine;
    private GameObject instantiatedCanvas;
    private int currentChoiceIndex = 0;

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

    private void Update()
    {
        // Handle choice navigation with keyboard
        if (isDialogueActive && choiceButtons.Count > 0)
        {
            // Navigate up
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                currentChoiceIndex--;
                if (currentChoiceIndex < 0)
                    currentChoiceIndex = choiceButtons.Count - 1;
                
                UpdateChoiceHighlights();
            }
            // Navigate down
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                currentChoiceIndex++;
                if (currentChoiceIndex >= choiceButtons.Count)
                    currentChoiceIndex = 0;
                
                UpdateChoiceHighlights();
            }
            // Select choice
            else if (Input.GetKeyDown(interactKey))
            {
                if (currentChoiceIndex >= 0 && currentChoiceIndex < choiceButtons.Count)
                {
                    MakeChoice(currentChoiceIndex);
                }
            }
        }
        // Continue dialogue with interact key
        else if (isDialogueActive && Input.GetKeyDown(interactKey))
        {
            if (typingCoroutine != null)
            {
                // Skip typing animation
                StopCoroutine(typingCoroutine);
                if (dialogueText != null && currentInkHandler != null)
                {
                    Story story = GetStoryFromHandler();
                    if (story != null && story.currentText != null)
                    {
                        dialogueText.text = story.currentText;
                    }
                }
                typingCoroutine = null;
            }
            else
            {
                // Continue to next dialogue line
                ContinueInkStory();
            }
        }
    }

    private void Start()
    {
        // Initialize the dialogue UI from the prefab if it's assigned
        if (dialogueCanvasPrefab != null)
        {
            InstantiateDialogueCanvas();
        }
        else
        {
            Debug.LogError("DialogueCanvas prefab is not assigned! Please assign it in the inspector.");
            
            // Try to find the dialogue panel in the scene as a fallback
            TryFindDialogueUIInScene();
        }
        
        // Make sure dialogue panel is initially hidden
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            Debug.Log("Dialogue panel initialized and hidden");
        }
        
        // Make sure dialogue button container is initially hidden
        if (dialogueButtonContainer != null)
        {
            dialogueButtonContainer.SetActive(false);
        }
        
        // Log warning if choice button prefab is not assigned
        if (choiceButtonPrefab == null)
        {
            Debug.LogError("CHOICE BUTTON PREFAB IS NOT ASSIGNED! Please assign it in the inspector!");
        }
        else
        {
            Debug.Log("Choice button prefab is assigned: " + choiceButtonPrefab.name);
        }
    }
    
    private void InstantiateDialogueCanvas()
    {
        // Instantiate the dialogue canvas prefab
        instantiatedCanvas = Instantiate(dialogueCanvasPrefab);
        
        // Make sure it persists between scenes
        DontDestroyOnLoad(instantiatedCanvas);
        
        // Find the dialogue panel in the instantiated prefab
        Transform panelTransform = instantiatedCanvas.transform.Find("DialoguePanel");
        if (panelTransform != null)
        {
            dialoguePanel = panelTransform.gameObject;
            
            // Find the dialogue text
            Transform textTransform = panelTransform.Find("DialogueText");
            if (textTransform != null)
            {
                dialogueText = textTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                Debug.LogError("DialogueText not found in the DialogueCanvas prefab!");
            }
        }
        else
        {
            Debug.LogError("DialoguePanel not found in the DialogueCanvas prefab!");
        }
        
        // Find the dialogue button container in the instantiated prefab
        Transform buttonContainerTransform = instantiatedCanvas.transform.Find("DialogueButtonContainer");
        if (buttonContainerTransform != null)
        {
            dialogueButtonContainer = buttonContainerTransform.gameObject;
            Debug.Log("Found DialogueButtonContainer in the DialogueCanvas prefab");
        }
        else
        {
            Debug.LogWarning("DialogueButtonContainer not found in the DialogueCanvas prefab. Choices will not be displayed.");
            // Create a container for choices if it doesn't exist
            dialogueButtonContainer = new GameObject("DialogueButtonContainer");
            dialogueButtonContainer.transform.SetParent(instantiatedCanvas.transform, false);
            
            // Add RectTransform
            RectTransform containerRect = dialogueButtonContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.3f, 0.35f);
            containerRect.anchorMax = new Vector2(0.7f, 0.65f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            
            // Add VerticalLayoutGroup
            VerticalLayoutGroup layoutGroup = dialogueButtonContainer.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.spacing = 10;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            
            Debug.Log("Created DialogueButtonContainer as a fallback");
        }
        
        Debug.Log("Dialogue UI initialized from prefab successfully");
    }
    
    private void TryFindDialogueUIInScene()
    {
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
        
        // Try to find the dialogue button container in the scene
        GameObject buttonContainerObj = GameObject.Find("DialogueButtonContainer");
        if (buttonContainerObj != null)
        {
            dialogueButtonContainer = buttonContainerObj;
            Debug.Log("Found DialogueButtonContainer in scene");
        }
        
        // Try to find the choice button prefab in the scene
        GameObject prefabObj = GameObject.Find("DialogueButton");
        if (prefabObj != null)
        {
            choiceButtonPrefab = prefabObj;
            prefabObj.SetActive(false);
            Debug.Log("Found DialogueButton in scene");
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
        
        if (currentInkHandler == null)
        {
            Debug.LogError("No active ink story to show choices for");
            yield break;
        }
        
        Story story = GetStoryFromHandler();
        if (story == null)
        {
            Debug.LogError("Failed to get story from handler");
            yield break;
        }
        
        if (story.currentChoices.Count > 0)
        {
            Debug.Log($"Showing {story.currentChoices.Count} choices");
            
            // Clear any existing choice buttons
            ClearChoices();
            
            // Reset current choice index
            currentChoiceIndex = 0;
            
            // Make sure the dialogue button container is active
            if (dialogueButtonContainer != null)
            {
                dialogueButtonContainer.SetActive(true);
                
                // Make sure the parent canvas is active
                Transform parent = dialogueButtonContainer.transform.parent;
                if (parent != null && !parent.gameObject.activeSelf)
                {
                    parent.gameObject.SetActive(true);
                }
                
                // Check if choiceButtonPrefab is assigned
                if (choiceButtonPrefab == null)
                {
                    Debug.LogError("Choice button prefab is not assigned! Please assign it in the inspector!");
                    yield break;
                }
                
                // Create a button for each choice
                for (int i = 0; i < story.currentChoices.Count; i++)
                {
                    Choice choice = story.currentChoices[i];
                    
                    // Create a button for this choice
                    GameObject buttonObj = Instantiate(choiceButtonPrefab, dialogueButtonContainer.transform);
                    buttonObj.SetActive(true);
                    
                    // Find the text component
                    TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = choice.text;
                    }
                    else
                    {
                        Debug.LogError("No TextMeshProUGUI component found in choice button prefab");
                    }
                    
                    // Add a button component if it doesn't exist
                    Button button = buttonObj.GetComponent<Button>();
                    if (button == null)
                    {
                        button = buttonObj.AddComponent<Button>();
                    }
                    
                    // Set up the button click event
                    int choiceIndex = i; // Need to store this in a local variable for the closure
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => MakeChoice(choiceIndex));
                    
                    // Add to our list of buttons
                    choiceButtons.Add(buttonObj);
                    
                    Debug.Log($"Created choice button for: {choice.text}");
                }
                
                // Set initial highlight
                UpdateChoiceHighlights();
            }
            else
            {
                Debug.LogError("DialogueButtonContainer is not assigned! Cannot display choices.");
            }
        }
        else
        {
            Debug.Log("No choices to show");
        }
    }
    
    private void UpdateChoiceHighlights()
    {
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            Image buttonImage = choiceButtons[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = (i == currentChoiceIndex) ? highlightedButtonColor : normalButtonColor;
            }
            
            // Also update the text color for better visibility
            TextMeshProUGUI buttonText = choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.color = (i == currentChoiceIndex) ? Color.white : new Color(0.8f, 0.8f, 0.8f);
            }
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
        if (dialogueButtonContainer != null)
        {
            dialogueButtonContainer.SetActive(false);
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
        
        // Stop any typing coroutine
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        // Clear any choices
        ClearChoices();
        
        // Hide the dialogue panel
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            Debug.Log("Dialogue panel hidden");
        }
        
        // Hide the dialogue button container
        if (dialogueButtonContainer != null)
        {
            dialogueButtonContainer.SetActive(false);
            Debug.Log("DialogueButtonContainer hidden");
        }
        
        // Reset the current ink handler
        currentInkHandler = null;
        
        isDialogueActive = false;
        Debug.Log("Dialogue closed");
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
        Debug.Log("Initialize called with explicit references");
        
        // Set the references
        dialoguePanel = panel;
        dialogueText = text;
        dialogueButtonContainer = choices;
        choiceButtonPrefab = buttonPrefab;
        
        // Make sure panels are initially hidden
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        
        if (dialogueButtonContainer != null)
        {
            dialogueButtonContainer.SetActive(false);
        }
        
        Debug.Log("DialogueManager initialized with explicit references");
    }
    
    public void SetDialogueCanvasPrefab(GameObject prefab)
    {
        if (prefab != null)
        {
            // Clean up any existing instantiated canvas
            if (instantiatedCanvas != null)
            {
                Destroy(instantiatedCanvas);
            }
            
            // Set the new prefab
            dialogueCanvasPrefab = prefab;
            
            // Instantiate the new prefab
            InstantiateDialogueCanvas();
            
            Debug.Log("DialogueCanvas prefab set and instantiated");
        }
        else
        {
            Debug.LogError("Attempted to set null DialogueCanvas prefab");
        }
    }
} 