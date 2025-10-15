using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ink.Runtime;
using Pathfinding;
using System.Text;
using System.Text.RegularExpressions;

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
    
    [Header("Portrait Components")]
    [SerializeField] private GameObject portraitObject; // The GameObject containing the portrait image
    [SerializeField] private Image portraitImage; // The Image component for displaying portraits
    [SerializeField] private TextMeshProUGUI portraitText; // Text component for dialogue when portrait is shown
    
    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.04f;
    [SerializeField] private bool useTypewriterEffect = true;
    [SerializeField] private Color normalButtonColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color highlightedButtonColor = new Color(0.4f, 0.6f, 1f, 1f);
    [SerializeField] private KeyCode interactKey = KeyCode.Z;
    
    [Header("Game Pause Settings")]
    [SerializeField] private bool pauseGameDuringDialogue = true;
    [SerializeField] private bool affectEnemies = true;
    
    [Header("Positioning Settings")]
    [SerializeField] private bool useTopPosition = false;
    
    private bool isDialogueActive = false;
    private InkDialogueHandler currentInkHandler;
    private List<GameObject> choiceButtons = new List<GameObject>();
    private Coroutine typingCoroutine;
    private GameObject instantiatedCanvas;
    private int currentChoiceIndex = -1;
    private bool canvasInitialized = false;
    private bool waitForKeyRelease = false;
    private bool textFullyRevealed = false;
    private bool isPortraitMode = false;
    private string currentCleanDialogueText = "";

    // Public event that other systems can subscribe to
    public delegate void DialogueStateChanged(bool isActive);
    public static event DialogueStateChanged OnDialogueStateChanged;

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
            
            // Register for scene loaded events to reset state when a new scene is loaded
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            
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
                Debug.Log("Choice button prefab not assigned in inspector, attempting to load from Resources...");
                GameObject buttonPrefabFromResources = Resources.Load<GameObject>("UI/DialogueButton");
                if (buttonPrefabFromResources != null)
                {
                    choiceButtonPrefab = buttonPrefabFromResources;
                    Debug.Log("Loaded DialogueButton prefab from Resources/UI folder");
                }
                else
                {
                    Debug.LogError("Could not find DialogueButton prefab in Resources folder! Choice buttons will not appear correctly.");
                }
            }
            else
            {
                Debug.Log($"Choice button prefab is assigned: {choiceButtonPrefab.name}");
            }
        }
        else
        {
            Debug.Log("Duplicate DialogueManager found, destroying this one");
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // Unregister scene loaded event when DialogueManager is destroyed
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    /// <summary>
    /// Re-initialize dialogue UI after destroying the old canvas
    /// </summary>
    private IEnumerator ReinitializeDialogueUI()
    {
        // Wait a frame to ensure the scene is fully loaded
        yield return null;
        
        Debug.Log("[DIALOGUE MANAGER] Starting re-initialization of dialogue UI");
        
        // Try to find dialogue UI in the current scene first
        TryFindDialogueUIInScene();
        
        // If we found the UI in the scene, we're done
        if (dialoguePanel != null)
        {
            Debug.Log("[DIALOGUE MANAGER] Successfully found dialogue UI in overworld scene");
            canvasInitialized = true;
            
            // Make sure it's initially hidden
            dialoguePanel.SetActive(false);
            if (dialogueButtonContainer != null)
            {
                dialogueButtonContainer.SetActive(false);
            }
            
            // Clear any placeholder text
            if (dialogueText != null)
            {
                dialogueText.text = "";
            }
            if (portraitText != null)
            {
                portraitText.text = "";
            }
            
            // Hide portrait
            if (portraitObject != null)
            {
                portraitObject.SetActive(false);
            }
            
            // Also hide the parent canvas if it exists
            Canvas parentCanvas = dialoguePanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.gameObject != dialoguePanel)
            {
                // Only hide the canvas if the panel is already inactive
                // This prevents issues if the canvas contains other UI elements
                parentCanvas.enabled = false;
                Debug.Log("[DIALOGUE MANAGER] Also disabled parent canvas");
            }
            
            Debug.Log("[DIALOGUE MANAGER] Dialogue UI hidden and cleared");
        }
        // Otherwise, instantiate from prefab if available
        else if (dialogueCanvasPrefab != null)
        {
            Debug.Log("[DIALOGUE MANAGER] No dialogue UI in scene, instantiating from prefab");
            InstantiateDialogueCanvas();
            canvasInitialized = true;
        }
        else
        {
            Debug.LogError("[DIALOGUE MANAGER] CRITICAL: Cannot find dialogue UI in scene and no prefab assigned!");
        }
    }
    
    // Add a new method to handle scene loading and reset dialogue state
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log("[DEBUG NEW] Scene loaded: " + scene.name + " - Resetting dialogue state");
        
        // Reset critical dialogue state flags
        waitForKeyRelease = false;
        textFullyRevealed = false;
        
        // If dialogue was still active, force close it
        if (isDialogueActive)
        {
            Debug.Log("[DEBUG NEW] Dialogue was still active during scene change - forcing close");
            CloseDialogue();
        }
        
        // CRITICAL FIX: Destroy the persisted dialogue canvas when transitioning from combat to overworld
        bool isCombatScene = scene.name.StartsWith("Battle_");
        bool isOverworldScene = scene.name.StartsWith("Overworld_");
        
        // If we're loading an overworld scene and have an instantiated canvas from a previous scene
        if (isOverworldScene && instantiatedCanvas != null)
        {
            Debug.Log("[DIALOGUE MANAGER] Transitioning from combat to overworld - destroying old dialogue canvas and re-initializing");
            Destroy(instantiatedCanvas);
            instantiatedCanvas = null;
            
            // Clear references so they can be re-initialized from the new scene
            dialoguePanel = null;
            dialogueText = null;
            dialogueButtonContainer = null;
            portraitObject = null;
            portraitImage = null;
            portraitText = null;
            
            // CRITICAL: Reset the initialized flag so it can re-initialize
            canvasInitialized = false;
            
            Debug.Log("[DIALOGUE MANAGER] Cleared all dialogue canvas references, will re-initialize from scene");
            
            // Re-initialize from the new scene
            StartCoroutine(ReinitializeDialogueUI());
        }
        
        Debug.Log("[DEBUG NEW] Dialogue state after scene load - isDialogueActive: " + isDialogueActive + 
                  ", waitForKeyRelease: " + waitForKeyRelease + ", textFullyRevealed: " + textFullyRevealed);
    }

    private void Update()
    {
        // Skip if no dialogue is active
        if (!isDialogueActive) return;
        
        // CRITICAL FIX: If no Ink handler is set but dialogue is active, close the dialogue
        // This prevents the "Cannot continue story: No Ink handler set" error
        if (currentInkHandler == null)
        {
            Debug.LogWarning("Dialogue active but no Ink handler set. Forcing dialogue to close.");
            CloseDialogue();
            return;
        }
        
        // If ESC is pressed, close the dialogue
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDialogue();
            return;
        }

        // Check if we're waiting for the interact key to be released
        if (waitForKeyRelease && Input.GetKeyUp(interactKey))
        {
            waitForKeyRelease = false;
            Debug.Log("[DEBUG NEW] Interact key released, flag reset: waitForKeyRelease = false");
        }

        // Handle choice navigation with keyboard
        if (choiceButtons.Count > 0)
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
        else if (choiceButtons.Count == 0 && Input.GetKeyDown(interactKey) && !waitForKeyRelease)
        {
            Debug.Log("[DEBUG NEW] Continuing dialogue via keyboard (no choices active)");
            if (typingCoroutine != null)
            {
                // Skip typing animation - only reveal the text, don't advance
                StopCoroutine(typingCoroutine);
                if (dialogueText != null)
                {
                    // FIXED CODE: Always use the stored clean text that was properly processed before starting the typewriter
                    // This ensures the fastforwarded text is EXACTLY the same as the normal text
                    TextMeshProUGUI targetTextComponent = isPortraitMode && portraitText != null ? portraitText : dialogueText;
                    
                    if (!string.IsNullOrEmpty(currentCleanDialogueText))
                    {
                        targetTextComponent.text = currentCleanDialogueText;
                        Debug.Log($"Using stored clean text when skipping typewriter: '{currentCleanDialogueText}'");
                    }
                    else
                    {
                        Debug.LogWarning("No stored clean text available for fastforward - this should not happen");
                        // Fallback to accessing story directly if no stored text is available
                        Story story = GetStoryFromHandler();
                        if (story != null && story.currentText != null)
                        {
                            targetTextComponent.text = story.currentText;
                        }
                    }
                }
                typingCoroutine = null;
                waitForKeyRelease = true; // Wait for key release after skipping typing
                Debug.Log("[DEBUG NEW] Set waitForKeyRelease = true after skipping typing");
                textFullyRevealed = true; // Mark that text is now fully revealed
                Debug.Log("Text fully revealed, waiting for next input to continue");
            }
            else if (textFullyRevealed)
            {
                // If text is already fully revealed, advance to next dialogue
                textFullyRevealed = false; // Reset flag
                waitForKeyRelease = true; // Wait for key release after continuing dialogue
                Debug.Log("[DEBUG NEW] Set waitForKeyRelease = true before continuing to next dialogue");
                Debug.Log("Text was already revealed, now continuing to next dialogue");
                ContinueInkStory();
            }
            else
            {
                // If this is a fresh dialogue line (no coroutine and not revealed), continue
                waitForKeyRelease = true; // Wait for key release after continuing dialogue
                Debug.Log("[DEBUG NEW] Set waitForKeyRelease = true for fresh dialogue line");
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
            
            // Find the portrait components
            Transform portraitTransform = panelTransform.Find("Portrait");
            if (portraitTransform != null)
            {
                portraitObject = portraitTransform.gameObject;
                portraitImage = portraitObject.GetComponent<Image>();
                Debug.Log("Found Portrait in the DialogueCanvas prefab");
            }
            else
            {
                Debug.LogError("Portrait not found in the DialogueCanvas prefab!");
            }
            
            // Find the portrait text
            Transform portraitTextTransform = panelTransform.Find("PortraitText");
            if (portraitTextTransform != null)
            {
                portraitText = portraitTextTransform.GetComponent<TextMeshProUGUI>();
                Debug.Log("Found PortraitText in the DialogueCanvas prefab");
            }
            else
            {
                Debug.LogError("PortraitText not found in the DialogueCanvas prefab!");
            }
        }
        else
        {
            Debug.LogError("DialoguePanel not found in the DialogueCanvas prefab!");
        }
        
        // First look for DialogueButtonContainer as a child of the panel
        Transform buttonContainerTransform = dialoguePanel.transform.Find("DialogueButtonContainer");
        
        // If not found in panel, check if it exists at canvas level (legacy location)
        if (buttonContainerTransform == null)
        {
            buttonContainerTransform = instantiatedCanvas.transform.Find("DialogueButtonContainer");
            if (buttonContainerTransform != null)
            {
                Debug.Log("Found DialogueButtonContainer at canvas level - will relocate it to be inside the DialoguePanel");
                // Relocate it to be a child of the panel
                buttonContainerTransform.SetParent(dialoguePanel.transform, false);
            }
        }
        
        if (buttonContainerTransform != null)
        {
            dialogueButtonContainer = buttonContainerTransform.gameObject;
            Debug.Log("Found DialogueButtonContainer in the DialogueCanvas prefab");
            
            // Comment out this section to preserve the original prefab's settings
            // RectTransform containerRect = dialogueButtonContainer.GetComponent<RectTransform>();
            // if (containerRect != null)
            // {
            //     // Position it within the upper part of the dialogue panel
            //     containerRect.anchorMin = new Vector2(0.1f, 0.4f);
            //     containerRect.anchorMax = new Vector2(0.9f, 0.9f);
            //     containerRect.offsetMin = Vector2.zero;
            //     containerRect.offsetMax = Vector2.zero;
            //     Debug.Log("Adjusted DialogueButtonContainer position to be within the dialogue panel");
            // }
            
            // Instead, log the original settings for debugging
            RectTransform containerRect = dialogueButtonContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                Debug.Log($"Using existing DialogueButtonContainer settings - anchorMin: {containerRect.anchorMin}, anchorMax: {containerRect.anchorMax}");
            }
        }
        else
        {
            Debug.Log("DialogueButtonContainer not found in either DialoguePanel or canvas - creating a new one inside DialoguePanel");
            // Create the button container inside the dialogue panel if not found
            Debug.Log("Creating DialogueButtonContainer inside DialoguePanel");
            GameObject buttonContainerObj = new GameObject("DialogueButtonContainer");
            buttonContainerObj.transform.SetParent(dialoguePanel.transform, false);
            
            // Add RectTransform - use your preferred size and position values here
            // Based on your prefab's original settings
            RectTransform containerRect = buttonContainerObj.AddComponent<RectTransform>();
            
            // These values should match your original prefab's settings
            // Make sure you set these values to match what you want in your prefab
            containerRect.anchorMin = new Vector2(0.05f, 0.25f); // Adjust these to match your prefab
            containerRect.anchorMax = new Vector2(0.95f, 0.95f); // Adjust these to match your prefab
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            
            // Add VerticalLayoutGroup
            VerticalLayoutGroup layoutGroup = buttonContainerObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.spacing = 10;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            
            dialogueButtonContainer = buttonContainerObj;
            Debug.Log("Created DialogueButtonContainer inside DialoguePanel");
        }
        
        Debug.Log("Dialogue UI initialized from prefab successfully");
        
        // CRITICAL FIX: Make sure dialogue panel starts hidden and text is cleared
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            Debug.Log("Set instantiated dialogue panel to inactive");
        }
        
        // Clear any placeholder text from the prefab
        if (dialogueText != null)
        {
            dialogueText.text = "";
            Debug.Log("Cleared placeholder text from DialogueText");
        }
        
        if (portraitText != null)
        {
            portraitText.text = "";
            Debug.Log("Cleared placeholder text from PortraitText");
        }
        
        // Hide portrait object
        if (portraitObject != null)
        {
            portraitObject.SetActive(false);
        }
        
        // Also disable the parent canvas to ensure it's completely hidden
        Canvas parentCanvas = instantiatedCanvas.GetComponent<Canvas>();
        if (parentCanvas != null)
        {
            parentCanvas.enabled = false;
            Debug.Log("Disabled instantiated Canvas component");
        }
        
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
            
            // Try to find the portrait components
            Transform portraitTransform = panelObj.transform.Find("Portrait");
            if (portraitTransform != null)
            {
                portraitObject = portraitTransform.gameObject;
                portraitImage = portraitObject.GetComponent<Image>();
                Debug.Log("Found Portrait in scene");
            }
            
            // Try to find the portrait text
            Transform portraitTextTransform = panelObj.transform.Find("PortraitText");
            if (portraitTextTransform != null)
            {
                portraitText = portraitTextTransform.GetComponent<TextMeshProUGUI>();
                Debug.Log("Found PortraitText in scene");
            }
        }
        else
        {
            Debug.LogError("Could not find DialoguePanel in scene");
        }
        
        // First check if DialogueButtonContainer is a child of the panel (desired structure)
        GameObject buttonContainerObj = null;
        if (dialoguePanel != null)
        {
            Transform containerTransform = dialoguePanel.transform.Find("DialogueButtonContainer");
            if (containerTransform != null)
            {
                buttonContainerObj = containerTransform.gameObject;
                Debug.Log("Found DialogueButtonContainer inside DialoguePanel (correct structure)");
                
                // Comment out the code that adjusts the position
                // RectTransform containerRect = buttonContainerObj.GetComponent<RectTransform>();
                // if (containerRect != null)
                // {
                //     containerRect.anchorMin = new Vector2(0.1f, 0.4f);
                //     containerRect.anchorMax = new Vector2(0.9f, 0.9f);
                //     containerRect.offsetMin = Vector2.zero;
                //     containerRect.offsetMax = Vector2.zero;
                //     Debug.Log("Adjusted relocated DialogueButtonContainer position");
                // }
                
                // Instead, just log the current values
                RectTransform containerRect = buttonContainerObj.GetComponent<RectTransform>();
                if (containerRect != null)
                {
                    Debug.Log($"Preserving DialogueButtonContainer settings after reparenting - anchorMin: {containerRect.anchorMin}, anchorMax: {containerRect.anchorMax}");
                }
            }
        }
        
        // If not found in panel, look for it at scene root level
        if (buttonContainerObj == null)
        {
            buttonContainerObj = GameObject.Find("DialogueButtonContainer");
            if (buttonContainerObj != null)
            {
                Debug.Log("Found DialogueButtonContainer at scene root - moving it to be a child of DialoguePanel");
                // Move it to be a child of the dialogue panel
                if (dialoguePanel != null)
                {
                    buttonContainerObj.transform.SetParent(dialoguePanel.transform, false);
                    
                    // Comment out the code that adjusts the position
                    // RectTransform containerRect = buttonContainerObj.GetComponent<RectTransform>();
                    // if (containerRect != null)
                    // {
                    //     containerRect.anchorMin = new Vector2(0.1f, 0.4f);
                    //     containerRect.anchorMax = new Vector2(0.9f, 0.9f);
                    //     containerRect.offsetMin = Vector2.zero;
                    //     containerRect.offsetMax = Vector2.zero;
                    //     Debug.Log("Adjusted relocated DialogueButtonContainer position");
                    // }
                    
                    // Instead, just log the current values
                    RectTransform containerRect = buttonContainerObj.GetComponent<RectTransform>();
                    if (containerRect != null)
                    {
                        Debug.Log($"Preserving DialogueButtonContainer settings after reparenting - anchorMin: {containerRect.anchorMin}, anchorMax: {containerRect.anchorMax}");
                    }
                }
            }
        }
        
        if (buttonContainerObj != null)
        {
            dialogueButtonContainer = buttonContainerObj;
            Debug.Log("DialogueButtonContainer setup complete");
        }
        else if (dialoguePanel != null)
        {
            // Create the button container inside the dialogue panel if not found
            Debug.Log("Creating DialogueButtonContainer inside DialoguePanel");
            buttonContainerObj = new GameObject("DialogueButtonContainer");
            buttonContainerObj.transform.SetParent(dialoguePanel.transform, false);
            
            // Add RectTransform - use your preferred size and position values here
            // Based on your prefab's original settings
            RectTransform containerRect = buttonContainerObj.AddComponent<RectTransform>();
            
            // These values should match your original prefab's settings
            // Make sure you set these values to match what you want in your prefab
            containerRect.anchorMin = new Vector2(0.05f, 0.25f); // Adjust these to match your prefab
            containerRect.anchorMax = new Vector2(0.95f, 0.95f); // Adjust these to match your prefab
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            
            // Add VerticalLayoutGroup
            VerticalLayoutGroup layoutGroup = buttonContainerObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.spacing = 10;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            
            dialogueButtonContainer = buttonContainerObj;
            Debug.Log("Created DialogueButtonContainer inside DialoguePanel");
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

    // Add new method to preprocess text with variables, HTML tags, and portrait information
    private string PreprocessText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        Debug.Log($"PreprocessText starting with: \"{text}\"");
        
        // Step 1: Check for portrait tag - pattern: portrait: portraitID
        string processedText = text;
        bool wasPortraitMode = isPortraitMode; // Store previous state
        isPortraitMode = false;
        
        // UPDATED regex to handle portrait IDs with spaces (capturing everything up to the comma)
        Regex portraitPattern = new Regex(@"^portrait:\s*([^,;]+)");
        Match portraitMatch = portraitPattern.Match(text);
        
        if (portraitMatch.Success)
        {
            string portraitID = portraitMatch.Groups[1].Value.Trim();
            Debug.Log($"Found portrait ID: {portraitID}");
            
            // Set portrait mode and try to load the portrait
            isPortraitMode = true;
            SetPortrait(portraitID);
            
            // Remove the portrait prefix from the text
            processedText = text.Substring(portraitMatch.Length).TrimStart();
            
            // Remove any leading comma that might appear after the portrait tag
            if (processedText.StartsWith(","))
            {
                processedText = processedText.Substring(1).TrimStart();
                Debug.Log($"Removed leading comma from dialogue text");
            }
            
            Debug.Log($"Text after removing portrait tag: \"{processedText}\"");
            
            // IMPORTANT: If the portrait mode has changed, make sure to update text visibility
            if (wasPortraitMode != isPortraitMode)
            {
                UpdateTextVisibility();
            }
        }
        else
        {
            // Hide portrait if no portrait tag is present
            HidePortrait();
            
            // IMPORTANT: If the portrait mode has changed, make sure to update text visibility
            if (wasPortraitMode != isPortraitMode)
            {
                UpdateTextVisibility();
            }
        }
        
        // Step 2: Extract speaker tag if present (pattern: # speaker: Name)
        // This should be done BEFORE processing quotation marks
        string speakerName = null;
        Regex speakerPattern = new Regex(@"#\s*speaker:\s*([^#\r\n]+)");
        Match speakerMatch = speakerPattern.Match(processedText);
        
        if (speakerMatch.Success)
        {
            speakerName = speakerMatch.Groups[1].Value.Trim();
            Debug.Log($"Found speaker tag: {speakerName}");
            
            // Remove the speaker tag from the text
            processedText = processedText.Substring(0, speakerMatch.Index).TrimEnd();
            Debug.Log($"Text after removing speaker tag: \"{processedText}\"");
        }
        
        // Step 3: FIXED - Properly handle quotation marks in dialogue text
        // If the text starts and ends with quotation marks, process it as direct speech
        if (processedText.StartsWith("\"") && processedText.EndsWith("\""))
        {
            // Remove the outer quotation marks to display clean text
            processedText = processedText.Substring(1, processedText.Length - 2);
            Debug.Log($"Removed surrounding quotation marks: \"{processedText}\"");
        }
        // Handle case where there's only an opening quote (happens with some dialogue in Ink)
        else if (processedText.StartsWith("\""))
        {
            // Remove just the opening quotation mark
            processedText = processedText.Substring(1);
            Debug.Log($"Removed opening quotation mark: \"{processedText}\"");
        }
        
        // Step 4: IMPROVED - Process any Ink variables in the text - pattern: {variableName}
        // This would usually be handled by Ink itself, but for direct dialogue we need to check
        
        // Use iterative approach to handle nested variables
        bool hasVariables = true;
        int safetyCounter = 0;
        int maxIterations = 10; // Maximum iterations to prevent infinite loops
        
        while (hasVariables && safetyCounter < maxIterations)
        {
            safetyCounter++;
            
            // Use regex to find all variable patterns {name}
            Regex variablePattern = new Regex(@"\{([^{}]+)\}");
            MatchCollection matches = variablePattern.Matches(processedText);
            
            if (matches.Count == 0)
            {
                hasVariables = false;
                continue;
            }
            
            Debug.Log($"Found {matches.Count} variable patterns in text (iteration {safetyCounter})");
            
            // If we have currentInkHandler and active story, we can try to resolve variables
            if (currentInkHandler != null)
            {
                // Get the Story object using reflection
                Story story = GetStoryFromHandler();
                
                if (story != null)
                {
                    // Build a list of all replacements to make
                    Dictionary<string, string> replacements = new Dictionary<string, string>();
                    
                    foreach (Match match in matches)
                    {
                        string variableName = match.Groups[1].Value.Trim();
                        Debug.Log($"Processing variable: {variableName}");
                        
                        // Try to get variable value from story
                        try
                        {
                            if (story.variablesState.GlobalVariableExistsWithName(variableName))
                            {
                                object variableValue = story.variablesState[variableName];
                                string replacement = variableValue?.ToString() ?? "";
                                replacements[match.Value] = replacement;
                                Debug.Log($"Will replace {match.Value} with {replacement}");
                            }
                            else
                            {
                                Debug.LogWarning($"Variable {variableName} not found in Ink story");
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Error processing variable {variableName}: {e.Message}");
                        }
                    }
                    
                    // Apply all replacements at once
                    foreach (var replacement in replacements)
                    {
                        processedText = processedText.Replace(replacement.Key, replacement.Value);
                    }
                }
                else
                {
                    // No story found, break the loop
                    hasVariables = false;
                }
            }
            else
            {
                // No ink handler, break the loop
                hasVariables = false;
            }
        }
        
        if (safetyCounter >= maxIterations)
        {
            Debug.LogWarning($"Reached maximum variable processing iterations ({maxIterations}). Some variables might not be fully resolved.");
        }
        
        // Final verification - check specifically for Ravenbond dialogue issues
        // If we detect text that looks like Ravenbond dialogue, ensure quotes are properly removed
        if (processedText.Contains("Care to play a game of Ravenbond"))
        {
            // Log that we detected the Ravenbond dialogue
            Debug.Log("DETECTED RAVENBOND DIALOGUE - Ensuring quotes are properly removed");
            
            // Make sure there are no leading quote marks
            if (processedText.StartsWith("\""))
            {
                processedText = processedText.Substring(1);
                Debug.Log("Removed extra starting quote from Ravenbond dialogue");
            }
            
            // Ensure proper formatting by checking if there's only a closing quote
            if (processedText.EndsWith("\"") && !processedText.StartsWith("\""))
            {
                processedText = processedText.Substring(0, processedText.Length - 1);
                Debug.Log("Removed trailing quote from Ravenbond dialogue");
            }
        }
        
        Debug.Log($"FINAL preprocessed text: \"{processedText}\"");
        return processedText;
    }

    // Add new method to ensure only one text component is visible at a time
    private void UpdateTextVisibility()
    {
        if (dialogueText != null && portraitText != null)
        {
            // Set visibility based on portrait mode
            dialogueText.gameObject.SetActive(!isPortraitMode);
            portraitText.gameObject.SetActive(isPortraitMode);
            
            Debug.Log($"Updated text visibility - Portrait mode: {isPortraitMode}, " +
                     $"DialogueText active: {dialogueText.gameObject.activeSelf}, " +
                     $"PortraitText active: {portraitText.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogWarning("Cannot update text visibility - missing text components");
        }
    }

    private void SetPortrait(string portraitID)
    {
        if (portraitObject == null || portraitImage == null)
        {
            Debug.LogError("Portrait components not assigned!");
            return;
        }
        
        // Try direct loading
        Sprite portraitSprite = null;
        
        // Clean up the portraitID by trimming any whitespace
        portraitID = portraitID.Trim();
        Debug.Log($"[PORTRAIT SYSTEM] Attempting to load portrait: '{portraitID}'");
        
        // CASE 1: Load directly from the Portraits folder using direct AssetDatabase path
        // This is the most reliable method in the editor
        #if UNITY_EDITOR
        string assetPath = $"Assets/Sprites/Portraits/{portraitID}.png";
        portraitSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (portraitSprite != null) {
            Debug.Log($"[PORTRAIT SYSTEM] Found portrait using direct asset path: '{assetPath}'");
        } else {
            // Try without extension
            assetPath = $"Assets/Sprites/Portraits/{portraitID}";
            portraitSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (portraitSprite != null) {
                Debug.Log($"[PORTRAIT SYSTEM] Found portrait using direct asset path without extension: '{assetPath}'");
            }
        }
        #endif
        
        // CASE 2: If still not found, or in a build, try Resources.Load
        if (portraitSprite == null) {
            // Try multiple formats of the portrait ID with Resources.Load
            
            // Format 1: Try the exact ID as provided
            portraitSprite = Resources.Load<Sprite>($"Portraits/{portraitID}");
            if (portraitSprite != null) {
                Debug.Log($"[PORTRAIT SYSTEM] Found portrait with Resources.Load using exact ID: 'Portraits/{portraitID}'");
            }
            
            // Format 2: Try lowercase version of the ID
            if (portraitSprite == null) {
                string lowercaseID = portraitID.ToLowerInvariant();
                portraitSprite = Resources.Load<Sprite>($"Portraits/{lowercaseID}");
                if (portraitSprite != null) {
                    Debug.Log($"[PORTRAIT SYSTEM] Found portrait with Resources.Load using lowercase: 'Portraits/{lowercaseID}'");
                }
            }
            
            // Format 3: Try with underscores instead of spaces
            if (portraitSprite == null && portraitID.Contains(" ")) {
                string underscoreID = portraitID.Replace(" ", "_").ToLowerInvariant();
                portraitSprite = Resources.Load<Sprite>($"Portraits/{underscoreID}");
                if (portraitSprite != null) {
                    Debug.Log($"[PORTRAIT SYSTEM] Found portrait with Resources.Load using underscores: 'Portraits/{underscoreID}'");
                }
            }
            
            // Format 4: Try direct ID without Portraits/ prefix
            if (portraitSprite == null) {
                portraitSprite = Resources.Load<Sprite>(portraitID);
                if (portraitSprite != null) {
                    Debug.Log($"[PORTRAIT SYSTEM] Found portrait with direct ID: '{portraitID}'");
                }
            }
        }
        
        if (portraitSprite != null)
        {
            portraitImage.sprite = portraitSprite;
            portraitObject.SetActive(true);
            
            // IMPORTANT: Make sure to update text visibility when setting a portrait
            UpdateTextVisibility();
            
            Debug.Log($"[PORTRAIT SYSTEM] Successfully set portrait: {portraitID}");
        }
        else
        {
            Debug.LogError($"[PORTRAIT SYSTEM] Portrait sprite not found after trying all paths: {portraitID}");
            
            #if UNITY_EDITOR
            // List available sprites in the Sprites/Portraits folder
            if (System.IO.Directory.Exists("Assets/Sprites/Portraits")) {
                Debug.Log("[PORTRAIT SYSTEM] Available portraits in Assets/Sprites/Portraits:");
                string[] portraitFiles = System.IO.Directory.GetFiles("Assets/Sprites/Portraits", "*.png");
                foreach (string file in portraitFiles) {
                    Debug.Log($"  - {System.IO.Path.GetFileNameWithoutExtension(file)}");
                }
            }
            
            // List available sprites in Resources/Portraits folder
            if (System.IO.Directory.Exists("Assets/Resources/Portraits")) {
                Debug.Log("[PORTRAIT SYSTEM] Available portraits in Resources/Portraits:");
                string[] resourceFiles = System.IO.Directory.GetFiles("Assets/Resources/Portraits", "*.png");
                foreach (string file in resourceFiles) {
                    Debug.Log($"  - {System.IO.Path.GetFileNameWithoutExtension(file)}");
                }
            }
            #endif
            
            // Instead of failing completely, let's try to load a default portrait as fallback
            Sprite defaultSprite = Resources.Load<Sprite>("Portraits/magician_neutral_1");
            if (defaultSprite != null) {
                Debug.Log("[PORTRAIT SYSTEM] Using default portrait as fallback");
                portraitImage.sprite = defaultSprite;
                portraitObject.SetActive(true);
                
                // IMPORTANT: Make sure to update text visibility even when using the default portrait
                UpdateTextVisibility();
            } else {
                portraitObject.SetActive(false);
                isPortraitMode = false;
                
                // IMPORTANT: Make sure to update text visibility when hiding the portrait
                UpdateTextVisibility();
            }
        }
    }
    
    // New method to hide the portrait
    private void HidePortrait()
    {
        if (portraitObject != null)
        {
            portraitObject.SetActive(false);
        }
        isPortraitMode = false;
        
        // IMPORTANT: Make sure to update text visibility when hiding the portrait
        UpdateTextVisibility();
    }

    public void ShowDialogue(string message)
    {
        Debug.Log("ShowDialogue called with message: " + message);
        
        // Check for null or empty messages
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogError("ShowDialogue received null or empty message");
            return;
        }
        
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
            
            // Also re-enable the Canvas component if it was disabled
            Canvas parentCanvas = dialoguePanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && !parentCanvas.enabled)
            {
                Debug.Log("Re-enabling parent Canvas component");
                parentCanvas.enabled = true;
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
            }
            
            // IMPORTANT: Preprocess the text to handle variables, portraits, and tags BEFORE starting typewriter effect
            string preprocessedText = PreprocessText(message);
            
            // Store the clean text for use when skipping typewriter
            currentCleanDialogueText = preprocessedText;
            Debug.Log($"Stored clean preprocessed text: '{currentCleanDialogueText}'");
            
            // Determine which text component to use based on portrait mode
            TextMeshProUGUI targetTextComponent = isPortraitMode && portraitText != null ? portraitText : dialogueText;
            
            // IMPORTANT: Ensure text visibility is correctly set
            UpdateTextVisibility();
            
            // Set the text
            if (useTypewriterEffect)
            {
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                    typingCoroutine = null;
                }
                
                // Important: For short messages like "Nothing Left", ensure the text component is properly prepared
                if (preprocessedText.Length <= 15) // For short messages
                {
                    Debug.Log($"Short message detected: '{preprocessedText}' - Ensuring proper display");
                    // Pre-set the text to ensure it's properly initialized
                    targetTextComponent.text = "";
                }
                
                Debug.Log($"Starting typewriter effect for: {preprocessedText} using {(isPortraitMode ? "portraitText" : "dialogueText")}");
                typingCoroutine = StartCoroutine(TypeText(preprocessedText, targetTextComponent));
            }
            else
            {
                Debug.Log($"Setting text directly: {preprocessedText} using {(isPortraitMode ? "portraitText" : "dialogueText")}");
                targetTextComponent.text = preprocessedText;
                textFullyRevealed = true;
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
        
        // Store the reference to the ink handler - IMPORTANT: do this before any other calls
        currentInkHandler = inkHandler;
        
        // Set dialogue active flag first so event handlers respond correctly
        isDialogueActive = true;
        
        // Explicitly stop player movement when dialogue starts
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Use the public method to disable movement
                playerController.SetCanMove(false);
            }
        }
        
        if (inkHandler.InkJSON == null)
        {
            ShowDialogue("Error: No ink story assigned to this object.");
            return;
        }
        
        // IMPORTANT: Always initialize the story before continuing
        // This will handle respecting the resetOnInteract flag as needed
        inkHandler.InitializeStory();
        
        // Make sure the dialogue panel is visible
        if (dialoguePanel != null && !dialoguePanel.activeSelf)
        {
            dialoguePanel.SetActive(true);
        }
        
        // Also re-enable the Canvas component if it was disabled
        if (dialoguePanel != null)
        {
            Canvas parentCanvas = dialoguePanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && !parentCanvas.enabled)
            {
                Debug.Log("Re-enabling parent Canvas component in StartInkDialogue");
                parentCanvas.enabled = true;
            }
        }
        
        // Show the first line of dialogue
        ContinueInkStory();
        
        // Pause the game if needed
        if (pauseGameDuringDialogue)
        {
            PauseGame(true);
        }
        
        // Notify subscribers that dialogue has started
        OnDialogueStateChanged?.Invoke(true);
    }
    
    public void ContinueInkStory()
    {
        if (currentInkHandler == null)
        {
            Debug.LogError("Cannot continue story: No Ink handler set");
            // CRITICAL FIX: Auto-close dialogue when there's no handler to prevent getting stuck
            CloseDialogue();
            return;
        }
        
        if (!currentInkHandler.HasNextLine())
        {
            Debug.Log("[DEBUG OBELISK TRANSITION] No more dialogue lines, closing dialogue");
            CloseDialogue();
            return;
        }
        
        string nextLine = currentInkHandler.GetNextDialogueLine();
        Debug.Log($"[DEBUG OBELISK TRANSITION] Next line from Ink: '{nextLine}'");
        
        // Clear any existing choices
        ClearChoices();
        
        // Special strings for control flow
        if (nextLine == "SHOW_CHOICES")
        {
            Debug.Log("[DEBUG OBELISK TRANSITION] Got SHOW_CHOICES signal, creating choice buttons");
            CreateChoiceButtons();
            return;
        }
        else if (nextLine == "END_OF_DIALOGUE")
        {
            Debug.Log("[DEBUG OBELISK TRANSITION] Got END_OF_DIALOGUE signal, closing dialogue");
            CloseDialogue();
            return;
        }
        
        // Process any portrait tags in the line
        nextLine = PreprocessText(nextLine);
        
        // Store the clean dialogue text for use if we need to fast-forward
        currentCleanDialogueText = nextLine;
        
        // Show the dialogue
        if (dialoguePanel != null && dialogueText != null)
        {
            // Show the dialogue panel
            dialoguePanel.SetActive(true);
            
            // IMPORTANT: Ensure visibility is correctly set before displaying text
            UpdateTextVisibility();
            
            // Display text based on portrait mode
            TextMeshProUGUI targetTextComponent = isPortraitMode && portraitText != null ? portraitText : dialogueText;
            
            // Play typewriter effect if enabled
            if (useTypewriterEffect)
            {
                // Clear any existing typewriter coroutine
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                }
                
                Debug.Log($"[DEBUG OBELISK TRANSITION] Starting typewriter effect for text: '{nextLine.Substring(0, Mathf.Min(30, nextLine.Length))}'...");
                typingCoroutine = StartCoroutine(TypeText(nextLine, targetTextComponent));
            }
            else
            {
                // Just set the text directly
                targetTextComponent.text = nextLine;
                textFullyRevealed = true;
            }
        }
        else
        {
            Debug.LogError("[DEBUG OBELISK TRANSITION] Missing dialogue panel or text component!");
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
    
    // New method to create choice buttons directly
    private void CreateChoiceButtons()
    {
        if (currentInkHandler == null)
        {
            Debug.LogError("No active ink story to show choices for");
            return;
        }
        
        // Get choices directly from the InkDialogueHandler
        List<Choice> choices = currentInkHandler.GetCurrentChoices();
        if (choices.Count == 0)
        {
            Debug.LogError("No choices available when trying to create choice buttons");
            return;
        }
        
        Debug.Log($"Creating {choices.Count} choice buttons directly from Ink choices");
        
        // Clear any existing choice buttons
        ClearChoices();
        
        // Reset current choice index
        currentChoiceIndex = 0;
        
        // Make sure the dialogue button container is active
        if (dialogueButtonContainer != null)
        {
            // Ensure correct parenting
            if (dialogueButtonContainer.transform.parent != dialoguePanel.transform)
            {
                Debug.LogWarning("DialogueButtonContainer is not a child of DialoguePanel! Fixing hierarchy...");
                dialogueButtonContainer.transform.SetParent(dialoguePanel.transform, false);
            }
            
            // IMPORTANT: Adjust button container layout based on portrait mode
            RectTransform containerRect = dialogueButtonContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                if (isPortraitMode && portraitObject != null && portraitObject.activeSelf)
                {
                    // Portrait is active - adjust button container to avoid overlapping with portrait
                    // Portrait takes up roughly the left 20% of the dialogue box, so shift buttons to the right
                    containerRect.anchorMin = new Vector2(0.25f, 0.5f); // Start further to the right
                    containerRect.anchorMax = new Vector2(0.95f, 0.5f); // Keep right edge the same
                    containerRect.pivot = new Vector2(0.5f, 0.5f);
                    containerRect.anchoredPosition = new Vector2(0, -40); // Adjust vertical position as needed
                    containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, 122.4f); // Maintain height
                    
                    Debug.Log("Adjusted button container layout for portrait mode - shifted right to avoid overlap");
                }
                else
                {
                    // No portrait - use full width of dialogue box
                    containerRect.anchorMin = new Vector2(0.05f, 0.5f);
                    containerRect.anchorMax = new Vector2(0.95f, 0.5f);
                    containerRect.pivot = new Vector2(0.5f, 0.5f);
                    containerRect.anchoredPosition = new Vector2(0, -40);
                    containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, 122.4f);
                    
                    Debug.Log("Using full-width button container layout (no portrait)");
                }
            }
            
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
                return;
            }
            
            // Create a button for each choice
            for (int i = 0; i < choices.Count; i++)
            {
                Choice choice = choices[i];
                
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
            }
            
            // Highlight the first choice by default
            UpdateChoiceHighlights();
        }
        else
        {
            Debug.LogError("DialogueButtonContainer is not assigned!");
        }
    }
    
    private IEnumerator ShowChoicesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Use the new direct method to create choice buttons
        CreateChoiceButtons();
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
        Debug.Log("[DEBUG NEW] CloseDialogue called - State before closing: isDialogueActive=" + isDialogueActive + 
                  ", waitForKeyRelease=" + waitForKeyRelease + ", textFullyRevealed=" + textFullyRevealed);
        
        // Stop any typing coroutine
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        // Reset ALL state variables
        textFullyRevealed = false;
        waitForKeyRelease = false; // CRITICAL FIX: Make sure this flag is reset when dialogue is closed
        
        // Clear any choices
        ClearChoices();
        
        // Hide the dialogue panel
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            Debug.Log("Dialogue panel hidden");
            
            // Also disable the parent canvas to ensure it's completely hidden
            Canvas parentCanvas = dialoguePanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.gameObject != dialoguePanel)
            {
                parentCanvas.enabled = false;
                Debug.Log("Disabled parent Canvas component");
            }
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
        
        // Unpause the game
        if (pauseGameDuringDialogue)
        {
            PauseGame(false);
        }
        
        // Try to find the player and enable movement
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Use the public method to enable movement
                playerController.SetCanMove(true);
            }
        }
        
        // Notify subscribers that dialogue has ended
        OnDialogueStateChanged?.Invoke(false);
        
        Debug.Log("[DEBUG NEW] Dialogue closed - Final state: isDialogueActive=" + isDialogueActive + 
                  ", waitForKeyRelease=" + waitForKeyRelease + ", textFullyRevealed=" + textFullyRevealed);
    }

    public bool IsDialogueActive()
    {
        Debug.Log($"IsDialogueActive called, returning: {isDialogueActive}");
        return isDialogueActive;
    }
    
    // New method to check if dialogue can be advanced to the next line
    public bool CanAdvanceDialogue()
    {
        // Can only advance if text is fully revealed and we're not waiting for key release
        return textFullyRevealed && !waitForKeyRelease;
    }
    
    private IEnumerator TypeText(string text, TextMeshProUGUI textComponent)
    {
        textFullyRevealed = false;
        
        // Clear and append the first character immediately to avoid delay
        textComponent.text = "";
        
        // Process all variables in the text at the start - fixing the jarring text change issue
        string processedText = text;
        
        // Check for any remaining unprocessed variable patterns - this is a safety check
        Regex variablePattern = new Regex(@"\{([^{}]+)\}");
        MatchCollection matches = variablePattern.Matches(processedText);
        
        if (matches.Count > 0 && currentInkHandler != null)
        {
            Story story = GetStoryFromHandler();
            if (story != null)
            {
                foreach (Match match in matches)
                {
                    string variableName = match.Groups[1].Value.Trim();
                    try
                    {
                        if (story.variablesState.GlobalVariableExistsWithName(variableName))
                        {
                            object variableValue = story.variablesState[variableName];
                            string replacement = variableValue?.ToString() ?? "";
                            processedText = processedText.Replace(match.Value, replacement);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error processing variable {variableName} in TypeText: {e.Message}");
                    }
                }
            }
        }
        
        int visibleCharacters = 0;
        float timePerChar = typingSpeed;
        float elapsedTime = 0;
        
        // If the text is short, slow down slightly to make sure it's readable
        if (processedText.Length < 20)
        {
            timePerChar *= 1.5f;
        }

        Debug.Log($"[DEBUG OBELISK TRANSITION] TypeText starting for text: '{processedText}', Length: {processedText.Length}");
        
        while (visibleCharacters < processedText.Length)
        {
            // Update elapsed time
            elapsedTime += Time.unscaledDeltaTime;
            
            // Calculate how many characters should be visible
            int newVisibleCount = Mathf.FloorToInt(elapsedTime / timePerChar);
            
            // If we need to add more characters
            if (newVisibleCount > visibleCharacters)
            {
                // How many new characters to add this frame
                int charsToAdd = newVisibleCount - visibleCharacters;
                
                // Make sure we don't exceed the length of the string
                if (visibleCharacters + charsToAdd > processedText.Length)
                {
                    charsToAdd = processedText.Length - visibleCharacters;
                }
                
                // Get the next portion of characters to add
                if (charsToAdd > 0 && visibleCharacters < processedText.Length)
                {
                    int endIndex = Mathf.Min(visibleCharacters + charsToAdd, processedText.Length);
                    string newText = processedText.Substring(0, endIndex);
                    textComponent.text = newText;
                    visibleCharacters = endIndex;
                }
            }
            
            yield return null;
        }
        
        // Ensure the final text is set correctly
        textComponent.text = processedText;
        
        Debug.Log($"[DEBUG OBELISK TRANSITION] TypeText completed for text: '{processedText}'");
        
        // Set flag that text is fully revealed
        textFullyRevealed = true;
        typingCoroutine = null;
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

    // New method to handle game pausing
    private void PauseGame(bool pause)
    {
        // Don't change the time scale - this would break the typewriter effect
        // Instead, directly disable enemy components
        
        if (affectEnemies)
        {
            // Find all enemies and pause/unpause them
            EnemyController[] enemies = FindObjectsOfType<EnemyController>();
            foreach (EnemyController enemy in enemies)
            {
                // Access the AIPath component directly
                AIPath aiPath = enemy.GetComponent<AIPath>();
                if (aiPath != null)
                {
                    aiPath.canMove = !pause;
                }
            }
        }
        
        // Broadcast the pause state change in case other systems need to respond
        Debug.Log($"Game {(pause ? "paused" : "unpaused")} due to dialogue");
    }
    
    /// <summary>
    /// Repositions the dialogue panel to the top of the screen (for tutorial battles)
    /// </summary>
    public void SetDialoguePositionTop()
    {
        if (dialoguePanel != null)
        {
            RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(0.1f, 0.7f);
                panelRect.anchorMax = new Vector2(0.9f, 0.9f);
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                Debug.Log("Dialogue panel repositioned to TOP of screen");
            }
        }
        else
        {
            Debug.LogWarning("Cannot reposition dialogue panel - panel is null");
        }
    }
    
    /// <summary>
    /// Repositions the dialogue panel to the bottom of the screen (default position)
    /// </summary>
    public void SetDialoguePositionBottom()
    {
        if (dialoguePanel != null)
        {
            RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(0.1f, 0.1f);
                panelRect.anchorMax = new Vector2(0.9f, 0.3f);
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                Debug.Log("Dialogue panel repositioned to BOTTOM of screen");
            }
        }
        else
        {
            Debug.LogWarning("Cannot reposition dialogue panel - panel is null");
        }
    }

    // Helper class to store tag information
    private class TagInfo
    {
        public string tag;
        public int startIndex;
        public int endIndex;
        public bool isOpening;
        
        public TagInfo(string tag, int startIndex, int endIndex, bool isOpening)
        {
            this.tag = tag;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
            this.isOpening = isOpening;
        }
    }
    
    // Helper method to extract rich text tags from text
    private List<TagInfo> ExtractRichTextTags(string text)
    {
        List<TagInfo> tags = new List<TagInfo>();
        
        // Regular expression to match rich text tags
        Regex tagRegex = new Regex(@"</?[a-zA-Z][^>]*>");
        MatchCollection matches = tagRegex.Matches(text);
        
        foreach (Match match in matches)
        {
            bool isOpening = !match.Value.StartsWith("</");
            tags.Add(new TagInfo(match.Value, match.Index, match.Index + match.Length - 1, isOpening));
        }
        
        return tags;
    }

    // Public method to test loading a specific Ink file directly
    public void TestInkFile(string inkFilePath)
    {
        Debug.Log($"[DIALOGUE TEST] Attempting to load Ink file: {inkFilePath}");
        
        // Check if the file exists
        TextAsset inkAsset = Resources.Load<TextAsset>(inkFilePath);
        
        if (inkAsset == null)
        {
            // Try loading directly from Assets path
            #if UNITY_EDITOR
            string fullPath = $"Assets/{inkFilePath}.ink";
            if (System.IO.File.Exists(fullPath))
            {
                Debug.Log($"[DIALOGUE TEST] Found ink file at path: {fullPath}");
                
                // We can't load .ink files directly in runtime, we need a compiled .json
                // But we can check it exists
                Debug.LogWarning($"[DIALOGUE TEST] Can't load .ink files directly. Need to use InkDialogueHandler.");
                return;
            }
            else
            {
                // Try with .json extension
                fullPath = $"Assets/{inkFilePath}.json";
                if (System.IO.File.Exists(fullPath))
                {
                    Debug.Log($"[DIALOGUE TEST] Found compiled ink file at path: {fullPath}");
                    inkAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(fullPath);
                }
                else
                {
                    Debug.LogError($"[DIALOGUE TEST] Could not find ink file at path: {inkFilePath}");
                    return;
                }
            }
            #else
            Debug.LogError($"[DIALOGUE TEST] Could not find ink file as TextAsset: {inkFilePath}");
            return;
            #endif
        }
        
        // Create an Ink runtime story from the asset
        Ink.Runtime.Story story = new Ink.Runtime.Story(inkAsset.text);
        
        // Set up an InkDialogueHandler
        // We'll need a GameObject with the handler component
        GameObject handlerObject = new GameObject("TestInkHandler");
        InkDialogueHandler handler = handlerObject.AddComponent<InkDialogueHandler>();
        
        // Use reflection to set the private _inkAsset and _story fields
        System.Type type = handler.GetType();
        
        System.Reflection.FieldInfo assetField = type.GetField("_inkAsset", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        System.Reflection.FieldInfo storyField = type.GetField("_story", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (assetField != null && storyField != null)
        {
            assetField.SetValue(handler, inkAsset);
            storyField.SetValue(handler, story);
            
            // Start dialogue with this handler
            StartInkDialogue(handler);
            Debug.Log($"[DIALOGUE TEST] Started dialogue with Ink file: {inkFilePath}");
        }
        else
        {
            Debug.LogError("[DIALOGUE TEST] Could not access required fields in InkDialogueHandler");
            Destroy(handlerObject);
        }
    }

    // Public method to initialize the manager (ensures canvas is created)
    public void Initialize()
    {
        Debug.Log("[DIALOGUE MANAGER] Initializing DialogueManager");
        
        if (!canvasInitialized)
        {
            InstantiateDialogueCanvas();
            canvasInitialized = true;
            Debug.Log("[DIALOGUE MANAGER] DialogueCanvas initialized");
        }
        else
        {
            Debug.Log("[DIALOGUE MANAGER] DialogueCanvas already initialized");
        }
    }
} 