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
    private bool canvasInitialized = false;
    private bool waitForKeyRelease = false;
    private bool textFullyRevealed = false; // Track if text has been revealed but not advanced

    /// <summary>
    /// Creates a DialogueManager instance if one doesn't exist already.
    /// This can be called from code instead of using the DialogueInitializer component.
    /// </summary>
    /// <returns>The DialogueManager instance</returns>
    public static DialogueManager CreateInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }
        
        // Create a new DialogueManager GameObject
        GameObject managerObj = new GameObject("DialogueManager");
        DialogueManager dialogueManager = managerObj.AddComponent<DialogueManager>();
        Debug.Log("Created new DialogueManager through CreateInstance method");
        
        return dialogueManager;
    }

    private void Awake()
    {
        // Ensure this object persists between scenes
        DontDestroyOnLoad(gameObject);
        
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("DialogueManager instance created");
            
            // Try to load prefabs from Resources if not assigned in inspector
            if (dialogueCanvasPrefab == null)
            {
                GameObject prefabFromResources = Resources.Load<GameObject>("DialogueCanvas");
                if (prefabFromResources != null)
                {
                    dialogueCanvasPrefab = prefabFromResources;
                    Debug.Log("Loaded DialogueCanvas prefab from Resources");
                }
                else
                {
                    Debug.LogError("Could not find DialogueCanvas prefab in Resources folder!");
                }
            }
            
            if (choiceButtonPrefab == null)
            {
                GameObject buttonPrefabFromResources = Resources.Load<GameObject>("DialogueButton");
                if (buttonPrefabFromResources != null)
                {
                    choiceButtonPrefab = buttonPrefabFromResources;
                    Debug.Log("Loaded DialogueButton prefab from Resources");
                }
                else
                {
                    Debug.LogError("Could not find DialogueButton prefab in Resources folder!");
                }
            }
        }
        else
        {
            Debug.Log("Duplicate DialogueManager found, destroying this one");
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Check if we're waiting for the interact key to be released
        if (waitForKeyRelease && Input.GetKeyUp(interactKey))
        {
            waitForKeyRelease = false;
            Debug.Log("Interact key released, ready for next input");
        }

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
            else if (Input.GetKeyDown(interactKey) && !waitForKeyRelease)
            {
                if (currentChoiceIndex >= 0 && currentChoiceIndex < choiceButtons.Count)
                {
                    Debug.Log($"DialogueManager.Update - Selecting choice {currentChoiceIndex} via keyboard");
                    waitForKeyRelease = true; // Wait for key release after selecting a choice
                    MakeChoice(currentChoiceIndex);
                }
            }
        }
        // Continue dialogue with interact key ONLY if no choices are displayed
        else if (isDialogueActive && choiceButtons.Count == 0 && Input.GetKeyDown(interactKey) && !waitForKeyRelease)
        {
            Debug.Log("DialogueManager.Update - Continuing dialogue via keyboard (no choices active)");
            if (typingCoroutine != null)
            {
                // Skip typing animation - only reveal the text, don't advance
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
                waitForKeyRelease = true; // Wait for key release after skipping typing
                textFullyRevealed = true; // Mark that text is now fully revealed
                Debug.Log("Text fully revealed, waiting for next input to continue");
            }
            else if (textFullyRevealed)
            {
                // If text is already fully revealed, advance to next dialogue
                textFullyRevealed = false; // Reset flag
                waitForKeyRelease = true; // Wait for key release after continuing dialogue
                Debug.Log("Text was already revealed, now continuing to next dialogue");
                ContinueInkStory();
            }
            else
            {
                // If this is a fresh dialogue line (no coroutine and not revealed), continue
                waitForKeyRelease = true; // Wait for key release after continuing dialogue
                ContinueInkStory();
            }
        }
    }

    private void Start()
    {
        // Initialize the dialogue UI from the prefab if it's assigned and not already initialized
        if (dialogueCanvasPrefab != null && !canvasInitialized)
        {
            InstantiateDialogueCanvas();
        }
        else if (dialogueCanvasPrefab == null)
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
        
        canvasInitialized = true;
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
        
        // Reset the text fully revealed flag when starting a new dialogue
        textFullyRevealed = false;
        
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
        if (isDialogueActive) // Prevent overlapping dialogues
        {
            Debug.Log("Dialogue already active, ignoring new request");
            return;
        }
        
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
        
        Debug.Log("ContinueInkStory - Starting to continue the story");
        
        // Reset the text fully revealed flag when starting a new dialogue line
        textFullyRevealed = false;
        
        // Clear any existing choice buttons
        ClearChoices();
        
        // Check if there's more content
        bool hasNextLine = currentInkHandler.HasNextLine();
        Debug.Log($"HasNextLine returned: {hasNextLine}");
        
        if (hasNextLine)
        {
            // Get the next line of dialogue
            string nextLine = currentInkHandler.GetNextDialogueLine();
            Debug.Log($"GetNextDialogueLine returned: \"{nextLine.Substring(0, Mathf.Min(50, nextLine.Length))}...\"");
            
            // Show the dialogue
            ShowDialogue(nextLine);
            
            // Set waitForKeyRelease to true when new dialogue is shown
            waitForKeyRelease = true;
            
            // Check if we need to show choices
            Story story = GetStoryFromHandler();
            if (story != null)
            {
                Debug.Log($"Story state after getting dialogue: canContinue={story.canContinue}, choiceCount={story.currentChoices.Count}");
                
                if (story.currentChoices.Count > 0)
                {
                    Debug.Log($"Starting coroutine to show {story.currentChoices.Count} choices after delay");
                    StartCoroutine(ShowChoicesAfterDelay(0.5f));
                }
            }
        }
        else
        {
            Debug.Log("No more content in the story, closing dialogue");
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
        if (currentInkHandler == null) 
        {
            Debug.LogError("Cannot make choice: No active ink handler");
            return;
        }
        
        Debug.Log($"DialogueManager.MakeChoice({choiceIndex}) - Starting choice selection");
        
        // Get the story before making the choice to compare states
        Story storyBefore = GetStoryFromHandler();
        if (storyBefore != null)
        {
            Debug.Log($"Before choice: canContinue={storyBefore.canContinue}, choiceCount={storyBefore.currentChoices.Count}");
        }
        
        // Tell the ink handler to make the choice (this now ONLY selects the choice without continuing)
        currentInkHandler.MakeChoice(choiceIndex);
        
        // Get the story after making the choice to see how the state changed
        Story storyAfter = GetStoryFromHandler();
        if (storyAfter != null)
        {
            Debug.Log($"After choice selection: canContinue={storyAfter.canContinue}, choiceCount={storyAfter.currentChoices.Count}");
        }
        
        // Set waitForKeyRelease to true to require the player to release and press the key again for next dialogue
        waitForKeyRelease = true;
        
        // Clear the choice buttons from the UI
        ClearChoices();
        
        // Continue the story to show the next dialogue
        Debug.Log("Calling ContinueInkStory to advance dialogue after choice");
        ContinueInkStory();
        
        // Check the final state after continuing
        Story storyFinal = GetStoryFromHandler();
        if (storyFinal != null)
        {
            Debug.Log($"Final state after continuing: canContinue={storyFinal.canContinue}, choiceCount={storyFinal.currentChoices.Count}");
        }
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
        
        // Reset typing state
        textFullyRevealed = false;
        
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
        
        // Set dialogue inactive - this is what PlayerController checks
        isDialogueActive = false;
        
        // Try to find the player and enable movement
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Use reflection to set the canMove field to true
                System.Type type = playerController.GetType();
                System.Reflection.FieldInfo canMoveField = type.GetField("canMove", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (canMoveField != null)
                {
                    canMoveField.SetValue(playerController, true);
                    Debug.Log("Set player canMove to true");
                }
            }
        }
        
        Debug.Log("Dialogue closed");
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
    
    // New method to check if dialogue can be advanced to the next line
    public bool CanAdvanceDialogue()
    {
        // Can only advance if text is fully revealed and we're not waiting for key release
        return textFullyRevealed && !waitForKeyRelease;
    }
    
    private IEnumerator TypeText(string text)
    {
        dialogueText.text = "";
        string visibleText = "";
        // Parse rich text tags to handle them properly
        List<int> tagStarts = new List<int>();
        List<int> tagEnds = new List<int>();
        
        // Find all tag positions
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '<')
                tagStarts.Add(i);
            else if (text[i] == '>')
                tagEnds.Add(i);
        }
        
        // Display text character by character
        for (int i = 0; i < text.Length; i++)
        {
            // Check if current position is inside a tag
            bool insideTag = false;
            for (int j = 0; j < tagStarts.Count; j++)
            {
                if (i >= tagStarts[j] && i <= tagEnds[j])
                {
                    insideTag = true;
                    break;
                }
            }
            
            // Add the current character
            visibleText += text[i];
            
            // Only pause for visible characters (not tags)
            if (!insideTag)
            {
                dialogueText.text = visibleText;
                yield return new WaitForSeconds(typingSpeed);
            }
        }
        
        // Ensure the final text is set correctly
        dialogueText.text = text;
        textFullyRevealed = true;
        Debug.Log("Text typing completed naturally, marked as fully revealed");
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
                canvasInitialized = false;
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