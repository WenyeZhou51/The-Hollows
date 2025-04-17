using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartRoomDialogueManager : MonoBehaviour
{
    [SerializeField] private TextAsset inkDialogueFile;
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float initialDelayTime = 0.5f;
    
    private InkDialogueHandler inkHandler;
    private bool dialogueCompleted = false;
    private bool hasSetBlackScreen = false;
    private bool isDialogueStarting = false;
    private bool isCoroutineRunning = false;
    private Coroutine keepBlackCoroutine = null;
    
    private void Awake()
    {
        // Add the InkDialogueHandler component if it doesn't exist
        inkHandler = GetComponent<InkDialogueHandler>();
        if (inkHandler == null)
        {
            inkHandler = gameObject.AddComponent<InkDialogueHandler>();
        }
        
        // Set the ink file if provided
        if (inkDialogueFile != null)
        {
            inkHandler.InkJSON = inkDialogueFile;
        }
        
        Debug.Log("[StartRoomDialogueManager] Awake called - Component initialized");
    }
    
    private void OnEnable()
    {
        // Subscribe to dialogue events
        DialogueManager.OnDialogueStateChanged += HandleDialogueStateChanged;
        // Subscribe to scene loaded event to catch any scene transitions
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        Debug.Log("[StartRoomDialogueManager] OnEnable called - Subscribed to events");
    }
    
    private void OnDisable()
    {
        // Unsubscribe from dialogue events
        DialogueManager.OnDialogueStateChanged -= HandleDialogueStateChanged;
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Stop any running coroutines
        if (keepBlackCoroutine != null)
        {
            StopCoroutine(keepBlackCoroutine);
            keepBlackCoroutine = null;
        }
        
        Debug.Log("[StartRoomDialogueManager] OnDisable called - Unsubscribed from events");
    }
    
    // This will catch scene transitions when the scene is being loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Startroom") || scene.name.Contains("start_room"))
        {
            // Check if this is our special case from Overworld_entrance
            bool shouldSkipDialogue = PlayerPrefs.GetInt("SkipStartRoomDialogue", 0) == 1;
            
            if (shouldSkipDialogue)
            {
                Debug.Log($"[StartRoomDialogueManager] Special case detected in OnSceneLoaded: Skipping black screen setup for Overworld_entrance transition");
                return; // Don't enforce black screen for the special case
            }
            
            Debug.Log($"[StartRoomDialogueManager] OnSceneLoaded detected for {scene.name} - Will override any existing fades");
            // Cancel any existing fade operations by setting screen to black immediately
            StartCoroutine(EnsureBlackScreenOnStartup());
        }
    }
    
    private IEnumerator EnsureBlackScreenOnStartup()
    {
        // Wait for any existing fades to initialize first
        yield return new WaitForEndOfFrame();
        
        // Then force screen to black, overriding any existing fade operations
        ScreenFader.EnsureExists();
        if (ScreenFader.Instance != null)
        {
            // Stop any active fades in ScreenFader
            Debug.Log("[StartRoomDialogueManager] Stopping any active fades in ScreenFader");
            StopAllCoroutinesInScreenFader();
            
            Debug.Log("[StartRoomDialogueManager] Forcing screen to black to override any existing fades");
            ScreenFader.Instance.SetBlackScreen();
            
            // Check if it actually worked
            Image fadeImage = ScreenFader.Instance.GetComponentInChildren<Image>();
            if (fadeImage != null)
            {
                Debug.Log($"[StartRoomDialogueManager] Screen fade alpha after SetBlackScreen: {fadeImage.color.a}");
                
                // If screen isn't actually black, try again with more force
                if (fadeImage.color.a < 0.9f)
                {
                    Debug.LogWarning("[StartRoomDialogueManager] Screen not fully black! Manual override...");
                    // Directly set the image color to full black
                    Color blackColor = fadeImage.color;
                    blackColor.a = 1.0f;
                    fadeImage.color = blackColor;
                    fadeImage.gameObject.SetActive(true);
                    Debug.Log($"[StartRoomDialogueManager] Manual override result: {fadeImage.color.a}");
                }
            }
            
            hasSetBlackScreen = true;
            
            // Start the "keep black" coroutine to ensure it stays black until dialogue starts
            if (keepBlackCoroutine != null)
            {
                StopCoroutine(keepBlackCoroutine);
            }
            keepBlackCoroutine = StartCoroutine(KeepScreenBlack());
        }
        else
        {
            Debug.LogError("[StartRoomDialogueManager] Failed to get ScreenFader.Instance");
        }
    }
    
    private IEnumerator KeepScreenBlack()
    {
        isCoroutineRunning = true;
        Debug.Log("[StartRoomDialogueManager] Starting KeepScreenBlack coroutine");
        
        while (!isDialogueStarting)
        {
            // Check if screen is still black
            if (ScreenFader.Instance != null)
            {
                Image fadeImage = ScreenFader.Instance.GetComponentInChildren<Image>();
                if (fadeImage != null && fadeImage.color.a < 0.9f)
                {
                    Debug.LogWarning($"[StartRoomDialogueManager] Screen fade being changed! Current alpha: {fadeImage.color.a}. Forcing black again.");
                    StopAllCoroutinesInScreenFader();
                    ScreenFader.Instance.SetBlackScreen();
                    
                    // Double-check it worked
                    if (fadeImage.color.a < 0.9f)
                    {
                        Debug.LogWarning("[StartRoomDialogueManager] Direct color override required");
                        Color blackColor = fadeImage.color;
                        blackColor.a = 1.0f;
                        fadeImage.color = blackColor;
                        fadeImage.gameObject.SetActive(true);
                    }
                }
            }
            
            yield return new WaitForSeconds(0.05f); // Check frequently
        }
        
        Debug.Log("[StartRoomDialogueManager] KeepScreenBlack coroutine finished - dialogue is starting");
        isCoroutineRunning = false;
    }
    
    private void StopAllCoroutinesInScreenFader()
    {
        if (ScreenFader.Instance != null)
        {
            // Get all MonoBehaviours in the ScreenFader GameObject and its children
            MonoBehaviour[] behaviours = ScreenFader.Instance.GetComponentsInChildren<MonoBehaviour>();
            
            // Stop all coroutines on each MonoBehaviour
            foreach (MonoBehaviour behaviour in behaviours)
            {
                behaviour.StopAllCoroutines();
                Debug.Log($"[StartRoomDialogueManager] Stopped coroutines on {behaviour.gameObject.name}");
            }
        }
    }
    
    private void Start()
    {
        // Check if we should skip dialogue due to coming from Overworld_Entrance
        bool shouldSkipDialogue = PlayerPrefs.GetInt("SkipStartRoomDialogue", 0) == 1;
        
        Debug.Log($"[StartRoomDialogueManager] Start called - SkipStartRoomDialogue flag = {shouldSkipDialogue}");
        
        if (shouldSkipDialogue)
        {
            Debug.Log("[StartRoomDialogueManager] Special case: Skipping dialogue due to transition from Overworld_entrance");
            // Clear the flag immediately to prevent this from affecting future StartRoom visits
            PlayerPrefs.SetInt("SkipStartRoomDialogue", 0);
            PlayerPrefs.Save();
            
            // Mark dialogue as completed so we don't show it
            dialogueCompleted = true;
            
            // Also fade in the screen immediately instead of keeping it black
            ScreenFader.EnsureExists();
            if (ScreenFader.Instance != null)
            {
                Debug.Log("[StartRoomDialogueManager] Special case: Fading in screen immediately for Overworld_entrance transition");
                StartCoroutine(ScreenFader.Instance.FadeFromBlack());
            }
            
            // Skip black screen setup entirely and proceed directly to player positioning
            return;
        }
        
        Debug.Log("[StartRoomDialogueManager] Normal StartRoom behavior - Beginning black screen setup");
        
        // Normal StartRoom behavior below for all other cases
        
        // Ensure ScreenFader exists and set the screen to black
        ScreenFader.EnsureExists();
        if (ScreenFader.Instance != null)
        {
            // Stop any active fades in ScreenFader first
            StopAllCoroutinesInScreenFader();
            
            Debug.Log("[StartRoomDialogueManager] Setting screen to black");
            ScreenFader.Instance.SetBlackScreen();
            
            // Debug screen fade state
            Image fadeImage = ScreenFader.Instance.GetComponentInChildren<Image>();
            if (fadeImage != null)
            {
                Debug.Log($"[StartRoomDialogueManager] Screen fade alpha in Start: {fadeImage.color.a}");
                
                // If not fully black, apply direct fix
                if (fadeImage.color.a < 0.9f)
                {
                    Debug.LogWarning("[StartRoomDialogueManager] Screen not fully black! Applying direct fix...");
                    Color blackColor = fadeImage.color;
                    blackColor.a = 1.0f;
                    fadeImage.color = blackColor;
                    fadeImage.gameObject.SetActive(true);
                    Debug.Log($"[StartRoomDialogueManager] After direct fix: {fadeImage.color.a}");
                }
            }
            
            hasSetBlackScreen = true;
            
            // Start the "keep black" coroutine if not already running
            if (keepBlackCoroutine == null && !isCoroutineRunning)
            {
                keepBlackCoroutine = StartCoroutine(KeepScreenBlack());
            }
        }
        else
        {
            Debug.LogError("[StartRoomDialogueManager] ScreenFader.Instance is null in Start!");
        }
        
        // Make sure DialogueManager exists
        if (DialogueManager.Instance == null)
        {
            Debug.Log("[StartRoomDialogueManager] DialogueManager not found, creating instance");
            DialogueManager.CreateInstance();
        }
        
        // Start dialogue with a delay to ensure everything is loaded and any scene transitions are complete
        Debug.Log($"[StartRoomDialogueManager] Scheduling dialogue to start after {initialDelayTime} seconds");
        StartCoroutine(StartDialogueWithDelay(initialDelayTime));
    }
    
    private IEnumerator StartDialogueWithDelay(float delay)
    {
        Debug.Log($"[StartRoomDialogueManager] Waiting {delay} seconds before starting dialogue");
        yield return new WaitForSeconds(delay);
        
        isDialogueStarting = true;
        
        // Double-check screen is still black before starting dialogue
        if (ScreenFader.Instance != null)
        {
            Image fadeImage = ScreenFader.Instance.GetComponentInChildren<Image>();
            if (fadeImage != null)
            {
                Debug.Log($"[StartRoomDialogueManager] Screen alpha before starting dialogue: {fadeImage.color.a}");
                if (fadeImage.color.a < 0.9f)
                {
                    Debug.LogWarning($"[StartRoomDialogueManager] Screen not fully black before dialogue! Alpha: {fadeImage.color.a} - Forcing black");
                    StopAllCoroutinesInScreenFader();
                    ScreenFader.Instance.SetBlackScreen();
                    
                    // Direct fix if needed
                    if (fadeImage.color.a < 0.9f)
                    {
                        Color blackColor = fadeImage.color;
                        blackColor.a = 1.0f;
                        fadeImage.color = blackColor;
                        fadeImage.gameObject.SetActive(true);
                        Debug.Log($"[StartRoomDialogueManager] After direct fix: {fadeImage.color.a}");
                    }
                    
                    hasSetBlackScreen = true;
                }
            }
        }
        
        // Make sure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        // Get death count from PersistentGameManager
        int deathCount = 0;
        if (PersistentGameManager.Instance != null)
        {
            deathCount = PersistentGameManager.Instance.GetDeaths();
            Debug.Log($"[StartRoomDialogueManager] Current death count: {deathCount}");
        }
        
        // Initialize the story
        if (inkHandler != null)
        {
            inkHandler.InitializeStory();
            
            // IMPORTANT: Convert death count to integer string to fix dialogue selection
            // Set the death count in the ink story
            try
            {
                // Force reset story first to clear any previous state
                inkHandler.ResetStory();
                inkHandler.InitializeStory();
                
                // Set the variable as an integer (Ink is now treating deathCount as an integer)
                Debug.Log($"[StartRoomDialogueManager] Setting deathCount to {deathCount} as integer in Ink");
                inkHandler.SetStoryVariable("deathCount", deathCount.ToString());
                
                // Debug all variables in the story
                inkHandler.DebugInkVariables();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StartRoomDialogueManager] Failed to set deathCount variable in Ink story: {e.Message}");
            }
            
            // Start the dialogue
            if (DialogueManager.Instance != null)
            {
                Debug.Log("[StartRoomDialogueManager] Starting ink dialogue");
                DialogueManager.Instance.StartInkDialogue(inkHandler);
                Debug.Log("[StartRoomDialogueManager] Started start room dialogue based on death count");
            }
            else
            {
                Debug.LogError("[StartRoomDialogueManager] DialogueManager instance not found!");
                // Fallback to fade in if dialogue can't be shown
                StartCoroutine(FadeInScreen());
            }
        }
        else
        {
            Debug.LogError("[StartRoomDialogueManager] InkDialogueHandler not found!");
            // Fallback to fade in if dialogue can't be shown
            StartCoroutine(FadeInScreen());
        }
    }
    
    private void HandleDialogueStateChanged(bool isActive)
    {
        Debug.Log($"[StartRoomDialogueManager] DialogueStateChanged event: isActive={isActive}, dialogueCompleted={dialogueCompleted}");
        
        // Special case: coming from Overworld_Entrance - dialogue is already marked as completed
        bool isSpecialCase = dialogueCompleted && PlayerPrefs.GetInt("NeedsPlayerSetup", 0) == 1;
        bool isOverworldToStartroomTransition = PlayerPrefs.GetInt("SkipStartRoomDialogue", 0) == 1;
        
        // If this is our special case transition, make sure dialogue is marked as completed
        if (isOverworldToStartroomTransition)
        {
            Debug.Log("[StartRoomDialogueManager] Special case detected in HandleDialogueStateChanged: Ensuring dialogue is marked as completed");
            dialogueCompleted = true;
            
            // Fade from black immediately if not already
            if (ScreenFader.Instance != null)
            {
                Debug.Log("[StartRoomDialogueManager] Special case: Ensuring screen is faded in");
                StartCoroutine(ScreenFader.Instance.FadeFromBlack());
            }
            
            // Clear the flag
            PlayerPrefs.SetInt("SkipStartRoomDialogue", 0);
            PlayerPrefs.Save();
            return;
        }
        
        // If dialogue just ended and wasn't already completed
        if (!isActive && !dialogueCompleted)
        {
            Debug.Log("[StartRoomDialogueManager] Start room dialogue completed - fading in screen");
            dialogueCompleted = true;
            
            // Fade in the screen
            StartCoroutine(FadeInScreen());
        }
        // For the special case where we're skipping dialogue but still need to fade in
        else if (isSpecialCase && ScreenFader.Instance != null)
        {
            Debug.Log("[StartRoomDialogueManager] Special case: Skipping dialogue but still need to fade in screen");
            // Fade from black immediately if player setup is needed
            StartCoroutine(ScreenFader.Instance.FadeFromBlack());
        }
    }
    
    private IEnumerator FadeInScreen()
    {
        // Ensure ScreenFader exists
        ScreenFader.EnsureExists();
        
        if (ScreenFader.Instance != null)
        {
            // Stop all coroutines that might be affecting the ScreenFader
            StopAllCoroutinesInScreenFader();
            
            // Debug screen state before fade
            Image fadeImage = ScreenFader.Instance.GetComponentInChildren<Image>();
            if (fadeImage != null)
            {
                Debug.Log($"[StartRoomDialogueManager] Screen fade alpha before FadeFromBlack: {fadeImage.color.a}");
                
                // If screen isn't actually black, force it black first
                if (fadeImage.color.a < 0.9f && hasSetBlackScreen)
                {
                    Debug.LogWarning("[StartRoomDialogueManager] Screen isn't black before fade in! Setting to black first");
                    ScreenFader.Instance.SetBlackScreen();
                    
                    // Direct fix if needed
                    if (fadeImage.color.a < 0.9f)
                    {
                        Color blackColor = fadeImage.color;
                        blackColor.a = 1.0f;
                        fadeImage.color = blackColor;
                        fadeImage.gameObject.SetActive(true);
                    }
                    
                    yield return new WaitForSeconds(0.1f); // Short delay
                }
            }
            
            Debug.Log($"[StartRoomDialogueManager] Starting fade from black with duration {fadeInDuration}");
            yield return StartCoroutine(ScreenFader.Instance.FadeFromBlack(fadeInDuration));
            
            // Debug screen state after fade
            if (fadeImage != null)
            {
                Debug.Log($"[StartRoomDialogueManager] Screen fade alpha after FadeFromBlack: {fadeImage.color.a}");
            }
            
            Debug.Log("[StartRoomDialogueManager] Screen faded in after start room dialogue");
        }
        else
        {
            Debug.LogError("[StartRoomDialogueManager] ScreenFader.Instance is null during FadeInScreen!");
        }
    }
    
    // Add this method to access necessary components
    private void Update()
    {
        // Debug key to check screen fade state at any time (press F5)
        if (Input.GetKeyDown(KeyCode.F5))
        {
            if (ScreenFader.Instance != null)
            {
                Image fadeImage = ScreenFader.Instance.GetComponentInChildren<Image>();
                if (fadeImage != null)
                {
                    Debug.Log($"[StartRoomDialogueManager] Current screen fade alpha: {fadeImage.color.a}");
                }
                else
                {
                    Debug.LogError("[StartRoomDialogueManager] No fade image found in ScreenFader!");
                }
            }
            else
            {
                Debug.LogError("[StartRoomDialogueManager] No ScreenFader.Instance during debug check!");
            }
        }
    }
} 