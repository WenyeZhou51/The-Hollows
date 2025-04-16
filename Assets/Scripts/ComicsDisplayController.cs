using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Define delegate for comic sequence completion
public delegate void ComicSequenceCompletedHandler();

public class ComicsDisplayController : MonoBehaviour
{
    public static ComicsDisplayController Instance { get; private set; }
    
    // Event that fires when comic sequence completes
    public static event ComicSequenceCompletedHandler OnComicSequenceComplete;

    [Header("Comic Panel Settings")]
    [SerializeField] private List<ComicPanel> comicPanels = new List<ComicPanel>();
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float panelSlideDistance = 100f;

    [Header("Input Settings")]
    [SerializeField] private KeyCode advanceKey = KeyCode.Z;
    [SerializeField] private KeyCode triggerKey = KeyCode.F5;

    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = true;

    private int currentPanelIndex = -1;
    private bool isSequenceActive = false;
    private GameObject currentPanel = null;
    private Coroutine currentAnimation = null;
    private float keyPressCheckTimer = 0f;
    private bool isAdvancingPanel = false; // Flag to prevent multiple advance requests

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            if (debugMode) Debug.Log("[ComicsDisplay] Instance created and set as singleton");
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            if (debugMode) Debug.Log("[ComicsDisplay] Duplicate instance detected, destroying this one");
            Destroy(gameObject);
            return;
        }

        // Ensure all panels are initially inactive
        int panelCount = 0;
        foreach (ComicPanel panel in comicPanels)
        {
            if (panel.panelObject != null)
            {
                panel.panelObject.SetActive(false);
                
                // Set initial alpha to zero
                Image panelImage = panel.panelObject.GetComponent<Image>();
                if (panelImage != null)
                {
                    Color initialColor = panelImage.color;
                    initialColor.a = 0f;
                    panelImage.color = initialColor;
                }
                panelCount++;
            }
        }
        
        if (debugMode) Debug.Log($"[ComicsDisplay] Initialized with {panelCount} panels");
    }

    private void OnEnable()
    {
        if (debugMode) Debug.Log("[ComicsDisplay] Controller enabled");
    }

    private void Start()
    {
        if (debugMode)
        {
            Debug.Log($"[ComicsDisplay] Started with {comicPanels.Count} panels configured");
            Debug.Log($"[ComicsDisplay] Trigger key set to {triggerKey}, advance key set to {advanceKey}");
            StartCoroutine(PeriodicInputCheck());
        }
    }

    private IEnumerator PeriodicInputCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            if (Input.GetKey(triggerKey))
            {
                Debug.Log($"[ComicsDisplay] {triggerKey} is currently being held down but not detected in Update");
            }
        }
    }

    private void Update()
    {
        // Reduce frequency of debug logs for key detection
        keyPressCheckTimer += Time.unscaledDeltaTime;
        if (debugMode && keyPressCheckTimer > 1f)
        {
            keyPressCheckTimer = 0f;
            Debug.Log($"[ComicsDisplay] Update running, TimeScale: {Time.timeScale}, isSequenceActive: {isSequenceActive}, panels: {comicPanels.Count}");
        }

        // Check for trigger key press when sequence isn't active
        if (!isSequenceActive && Input.GetKeyDown(triggerKey))
        {
            if (debugMode) Debug.Log($"[ComicsDisplay] Trigger key ({triggerKey}) detected!");
            StartComicSequence();
        }
        else if (!isSequenceActive && Input.anyKeyDown && debugMode)
        {
            foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(kcode))
                {
                    Debug.Log($"[ComicsDisplay] Key pressed: {kcode} (looking for {triggerKey})");
                    break;
                }
            }
        }

        // Check for advance key press when sequence is active
        if (isSequenceActive && Input.GetKeyDown(advanceKey) && !isAdvancingPanel)
        {
            if (debugMode) Debug.Log($"[ComicsDisplay] Advance key ({advanceKey}) detected!");
            AdvanceToNextPanel();
        }
    }

    /// <summary>
    /// Starts the comic panel sequence from the beginning
    /// </summary>
    public void StartComicSequence()
    {
        if (debugMode) Debug.Log("[ComicsDisplay] StartComicSequence called");
        
        if (isSequenceActive)
        {
            if (debugMode) Debug.Log("[ComicsDisplay] Sequence already active, ignoring start request");
            return;
        }
        
        if (comicPanels.Count == 0)
        {
            Debug.LogWarning("[ComicsDisplay] No comic panels configured, cannot start sequence");
            return;
        }

        // Freeze player and gameplay
        FreezeGameplay(true);
        
        isSequenceActive = true;
        currentPanelIndex = -1;
        isAdvancingPanel = false;
        if (debugMode) Debug.Log("[ComicsDisplay] Sequence started successfully");
        AdvanceToNextPanel();
    }

    /// <summary>
    /// Advance to the next panel in the sequence
    /// </summary>
    private void AdvanceToNextPanel()
    {
        if (debugMode) Debug.Log($"[ComicsDisplay] AdvanceToNextPanel called, current index: {currentPanelIndex}, animation active: {currentAnimation != null}");
        
        // Prevent multiple advance requests
        if (isAdvancingPanel)
        {
            if (debugMode) Debug.Log("[ComicsDisplay] Already advancing panel, ignoring duplicate request");
            return;
        }
        
        // If there's an animation in progress, don't do anything
        if (currentAnimation != null)
        {
            if (debugMode) Debug.Log("[ComicsDisplay] Animation in progress, ignoring advance request");
            return;
        }
        
        isAdvancingPanel = true;
        
        // Hide current panel if there is one
        if (currentPanel != null)
        {
            if (debugMode) Debug.Log("[ComicsDisplay] Fading out current panel");
            currentAnimation = StartCoroutine(FadeOutPanel(currentPanel));
            return; // Wait for fade out to complete before showing next panel
        }

        // Increment panel index to advance to the next panel
        currentPanelIndex++;
        
        // If we've reached the end of the sequence, end the comic display
        if (currentPanelIndex >= comicPanels.Count)
        {
            if (debugMode) Debug.Log("[ComicsDisplay] Reached end of sequence, ending comic display");
            EndComicSequence();
            return;
        }

        // Show the next panel
        ComicPanel panel = comicPanels[currentPanelIndex];
        if (panel.panelObject != null)
        {
            if (debugMode) Debug.Log($"[ComicsDisplay] Showing panel {currentPanelIndex + 1} with transition: {panel.transitionDirection}");
            currentPanel = panel.panelObject;
            currentPanel.SetActive(true);
            currentAnimation = StartCoroutine(FadeInPanel(currentPanel, panel.transitionDirection));
        }
        else
        {
            if (debugMode) Debug.LogWarning($"[ComicsDisplay] Panel {currentPanelIndex + 1} is null, skipping to next");
            // If panel is null, just skip to next
            isAdvancingPanel = false;
            AdvanceToNextPanel();
        }
    }

    /// <summary>
    /// Ends the comic sequence and unfreezes gameplay
    /// </summary>
    private void EndComicSequence()
    {
        if (debugMode) Debug.Log("[ComicsDisplay] EndComicSequence called");
        isSequenceActive = false;
        currentPanelIndex = -1;
        currentPanel = null;
        isAdvancingPanel = false;
        
        // Unfreeze player and gameplay
        FreezeGameplay(false);
        
        // Trigger the completion event
        if (OnComicSequenceComplete != null)
        {
            if (debugMode) Debug.Log("[ComicsDisplay] Firing OnComicSequenceComplete event");
            OnComicSequenceComplete();
        }
    }

    /// <summary>
    /// Fade in a panel with animation based on transition direction
    /// </summary>
    private IEnumerator FadeInPanel(GameObject panel, TransitionDirection direction)
    {
        if (debugMode) Debug.Log($"[ComicsDisplay] FadeInPanel started for direction: {direction}");
        Image panelImage = panel.GetComponent<Image>();
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        
        if (panelImage == null || rectTransform == null)
        {
            Debug.LogError("[ComicsDisplay] Panel is missing Image or RectTransform component");
            currentAnimation = null;
            isAdvancingPanel = false;
            yield break;
        }

        // Calculate starting position based on direction
        Vector2 targetPosition = rectTransform.anchoredPosition;
        Vector2 startPosition = targetPosition;
        
        switch (direction)
        {
            case TransitionDirection.TOP:
                startPosition.y = targetPosition.y + panelSlideDistance;
                break;
            case TransitionDirection.BOTTOM:
                startPosition.y = targetPosition.y - panelSlideDistance;
                break;
            case TransitionDirection.LEFT:
                startPosition.x = targetPosition.x - panelSlideDistance;
                break;
            case TransitionDirection.RIGHT:
                startPosition.x = targetPosition.x + panelSlideDistance;
                break;
        }
        
        rectTransform.anchoredPosition = startPosition;
        
        // Fade in with position animation
        float elapsedTime = 0;
        Color color = panelImage.color;
        color.a = 0f;
        panelImage.color = color;
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Use unscaledDeltaTime since we pause the game
            float t = Mathf.Clamp01(elapsedTime / fadeInDuration);
            
            // Smoothstep for easier animation
            float smoothT = t * t * (3f - 2f * t);
            
            // Update position
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, smoothT);
            
            // Update alpha
            color.a = smoothT;
            panelImage.color = color;
            
            yield return null;
        }
        
        // Ensure final state is correct
        rectTransform.anchoredPosition = targetPosition;
        color.a = 1f;
        panelImage.color = color;
        
        if (debugMode) Debug.Log("[ComicsDisplay] FadeInPanel completed");
        currentAnimation = null;
        isAdvancingPanel = false;
    }

    /// <summary>
    /// Fade out a panel
    /// </summary>
    private IEnumerator FadeOutPanel(GameObject panel)
    {
        if (debugMode) Debug.Log("[ComicsDisplay] FadeOutPanel started");
        Image panelImage = panel.GetComponent<Image>();
        
        if (panelImage == null)
        {
            Debug.LogError("[ComicsDisplay] Panel is missing Image component for fade out");
            panel.SetActive(false);
            currentPanel = null; // Clear the current panel reference
            currentAnimation = null;
            isAdvancingPanel = false;
            
            // Increment index and continue to the next panel
            currentPanelIndex++;
            StartCoroutine(PrepareNextPanelAfterFadeOut());
            yield break;
        }

        // Fade out
        float elapsedTime = 0;
        Color color = panelImage.color;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Use unscaledDeltaTime since we pause the game
            float t = Mathf.Clamp01(elapsedTime / fadeOutDuration);
            
            // Update alpha
            color.a = 1f - t;
            panelImage.color = color;
            
            yield return null;
        }
        
        // Ensure final state is correct
        color.a = 0f;
        panelImage.color = color;
        panel.SetActive(false);
        
        // Clear the current panel reference
        currentPanel = null;
        
        if (debugMode) Debug.Log("[ComicsDisplay] FadeOutPanel completed");
        currentAnimation = null;
        
        // Increment index and continue to the next panel
        currentPanelIndex++;
        StartCoroutine(PrepareNextPanelAfterFadeOut());
    }
    
    /// <summary>
    /// Prepare the next panel after the current one has faded out
    /// </summary>
    private IEnumerator PrepareNextPanelAfterFadeOut()
    {
        // Wait a frame to ensure any animations are complete
        yield return null;
        
        isAdvancingPanel = false;
        
        // Check if we've reached the end
        if (currentPanelIndex >= comicPanels.Count)
        {
            if (debugMode) Debug.Log("[ComicsDisplay] Last panel reached, ending sequence");
            EndComicSequence();
            yield break;
        }
        
        // Show the next panel
        ComicPanel nextPanel = comicPanels[currentPanelIndex];
        if (nextPanel.panelObject != null)
        {
            if (debugMode) Debug.Log($"[ComicsDisplay] Preparing next panel ({currentPanelIndex + 1}) after fade out");
            currentPanel = nextPanel.panelObject;
            currentPanel.SetActive(true);
            currentAnimation = StartCoroutine(FadeInPanel(currentPanel, nextPanel.transitionDirection));
        }
        else
        {
            if (debugMode) Debug.LogWarning($"[ComicsDisplay] Next panel ({currentPanelIndex + 1}) is null, skipping");
            AdvanceToNextPanel();
        }
    }

    /// <summary>
    /// Freezes or unfreezes player movement and gameplay
    /// </summary>
    private void FreezeGameplay(bool freeze)
    {
        if (debugMode) Debug.Log($"[ComicsDisplay] FreezeGameplay called with freeze={freeze}");
        
        // Find player controller to disable movement
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            // Use reflection to access the private canMove field
            System.Reflection.FieldInfo canMoveField = typeof(PlayerController).GetField(
                "canMove", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (canMoveField != null)
            {
                canMoveField.SetValue(playerController, !freeze);
                if (debugMode) Debug.Log($"[ComicsDisplay] Player movement set to {!freeze}");
            }
            else
            {
                Debug.LogError("[ComicsDisplay] Could not find canMove field in PlayerController");
            }
        }
        else
        {
            Debug.LogWarning("[ComicsDisplay] PlayerController not found in scene");
        }
        
        // Optional: Pause the game's time scale
        if (freeze)
        {
            Time.timeScale = 0f;
            if (debugMode) Debug.Log("[ComicsDisplay] Time.timeScale set to 0");
        }
        else
        {
            Time.timeScale = 1f;
            if (debugMode) Debug.Log("[ComicsDisplay] Time.timeScale set to 1");
        }
    }

    /// <summary>
    /// Add a comic panel to the sequence
    /// </summary>
    public void AddComicPanel(GameObject panelObject, TransitionDirection direction)
    {
        if (panelObject == null)
        {
            Debug.LogError("[ComicsDisplay] Attempted to add null panel object");
            return;
        }
        
        ComicPanel panel = new ComicPanel
        {
            panelObject = panelObject,
            transitionDirection = direction
        };
        
        comicPanels.Add(panel);
        if (debugMode) Debug.Log($"[ComicsDisplay] Panel added, total count: {comicPanels.Count}");
    }

    /// <summary>
    /// Clear all panels from the sequence
    /// </summary>
    public void ClearPanels()
    {
        comicPanels.Clear();
        if (debugMode) Debug.Log("[ComicsDisplay] All panels cleared");
    }

    /// <summary>
    /// Check if the controller has any panels configured
    /// </summary>
    public bool HasPanels()
    {
        return comicPanels.Count > 0;
    }
}

/// <summary>
/// Represents a single comic panel with its transition properties
/// </summary>
[System.Serializable]
public class ComicPanel
{
    public GameObject panelObject;
    public TransitionDirection transitionDirection = TransitionDirection.RIGHT;
}

/// <summary>
/// Enum defining possible transition directions
/// </summary>
public enum TransitionDirection
{
    TOP,
    BOTTOM,
    LEFT,
    RIGHT
} 