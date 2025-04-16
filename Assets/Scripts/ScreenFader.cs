using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
    // Singleton pattern
    public static ScreenFader Instance { get; private set; }

    // Track if application is quitting
    private static bool isQuitting = false;

    [Header("Fade Settings")]
    [SerializeField] private float defaultFadeDuration = 1.0f;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Image fadeImage;
    private Canvas fadeCanvas;
    private CanvasGroup canvasGroup;
    private bool isFading = false;
    
    // Public property to check if a fade is in progress
    public bool IsFading => isFading;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Set up the fade canvas and image
        InitializeFadeComponents();
        
        // Register for scene loaded events as a backup fade mechanism
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        Debug.Log("ScreenFader initialized and registered for scene transitions");
    }

    private void OnApplicationQuit()
    {
        // Set flag to avoid creating objects during application exit
        isQuitting = true;
    }

    private void OnDestroy()
    {
        // Only unregister if this is the Instance
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    /// <summary>
    /// Create all the necessary components for the fade effect
    /// </summary>
    private void InitializeFadeComponents()
    {
        // Create canvas if it doesn't exist
        if (fadeCanvas == null)
        {
            fadeCanvas = GetComponentInChildren<Canvas>();
            
            if (fadeCanvas == null)
            {
                // Create a new Canvas as a child
                GameObject canvasObj = new GameObject("FadeCanvas");
                canvasObj.transform.SetParent(transform);
                
                fadeCanvas = canvasObj.AddComponent<Canvas>();
                fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                fadeCanvas.sortingOrder = 999; // Make sure it's above everything
                
                // Add a CanvasScaler
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                // Add a GraphicRaycaster
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }
        
        // Create fade image if it doesn't exist
        if (fadeImage == null)
        {
            fadeImage = GetComponentInChildren<Image>();
            
            if (fadeImage == null)
            {
                // Create a new Image as a child of the canvas
                GameObject imageObj = new GameObject("FadeImage");
                imageObj.transform.SetParent(fadeCanvas.transform);
                
                // Set to stretch to fill canvas
                RectTransform rect = imageObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                
                // Add an Image component
                fadeImage = imageObj.AddComponent<Image>();
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0); // Start fully transparent
            }
        }
    }

    /// <summary>
    /// Ensure ScreenFader exists in the scene, creating it if needed
    /// </summary>
    public static void EnsureExists()
    {
        if (Instance == null && !isQuitting)
        {
            GameObject faderObj = new GameObject("ScreenFader");
            Instance = faderObj.AddComponent<ScreenFader>();
            Debug.Log("ScreenFader created");
        }
    }

    /// <summary>
    /// Fade the screen to black (or specified color)
    /// </summary>
    public IEnumerator FadeToBlack(float duration = -1)
    {
        if (duration < 0)
            duration = defaultFadeDuration;
            
        if (isFading)
            yield break;
            
        isFading = true;
        
        // Make sure the fade components exist
        InitializeFadeComponents();
        
        fadeImage.gameObject.SetActive(true);
        
        float startAlpha = fadeImage.color.a;
        float targetAlpha = 1.0f;
        float elapsedTime = 0;
        
        // Ensure we're at least slightly visible at start to avoid popping
        if (startAlpha <= 0.01f)
        {
            Color startColor = fadeImage.color;
            startColor.a = 0.01f;
            fadeImage.color = startColor;
        }
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // Update alpha
            Color newColor = fadeImage.color;
            newColor.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = newColor;
            
            yield return null;
        }
        
        // Ensure we end at exactly 1 alpha
        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;
        
        isFading = false;
    }
    
    /// <summary>
    /// Fade from black (or specified color) to clear
    /// </summary>
    public IEnumerator FadeFromBlack(float duration = -1)
    {
        if (duration < 0)
            duration = defaultFadeDuration;
            
        if (isFading)
            yield break;
            
        isFading = true;
        
        // Make sure the fade components exist
        InitializeFadeComponents();
        
        float startAlpha = fadeImage.color.a;
        float targetAlpha = 0f;
        float elapsedTime = 0;
        
        // Ensure we're at maximum opacity at start to avoid popping
        if (startAlpha < 0.99f)
        {
            Color startColor = fadeImage.color;
            startColor.a = 1.0f;
            fadeImage.color = startColor;
            startAlpha = 1.0f;
        }
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // Update alpha
            Color newColor = fadeImage.color;
            newColor.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = newColor;
            
            yield return null;
        }
        
        // Ensure we end at exactly 0 alpha
        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;
        
        if (finalColor.a <= 0.01f)
        {
            fadeImage.gameObject.SetActive(false);
        }
        
        isFading = false;
        
        // ADDED: Also reset the transition state in SceneTransitionManager when fade completes
        // This ensures we never get stuck in a transition state even if the normal reset fails
        if (SceneTransitionManager.Instance != null)
        {
            // Use a slight delay to make sure all other events have completed first
            StartCoroutine(ResetTransitionStateAfterDelay());
        }
    }

    // New helper method to reset transition state with a small delay
    private IEnumerator ResetTransitionStateAfterDelay()
    {
        // Short delay to allow other transition processes to complete first
        yield return new WaitForSecondsRealtime(0.1f);
        
        // Reset the transition state in SceneTransitionManager
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.CleanupTransitionState();
        }
    }
    
    /// <summary>
    /// Set the screen to black immediately without fading
    /// </summary>
    public void SetBlackScreen()
    {
        InitializeFadeComponents();
        fadeImage.gameObject.SetActive(true);
        
        Color blackColor = fadeColor;
        blackColor.a = 1.0f;
        fadeImage.color = blackColor;
    }
    
    /// <summary>
    /// Set the screen to clear immediately without fading
    /// </summary>
    public void SetClearScreen()
    {
        InitializeFadeComponents();
        
        Color clearColor = fadeColor;
        clearColor.a = 0.0f;
        fadeImage.color = clearColor;
        fadeImage.gameObject.SetActive(false);
    }

    // Safety method to ensure screen doesn't stay black during scene changes
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Make sure our fade components are still valid after scene change
        InitializeFadeComponents();
        
        // IMPORTANT: Don't reset transition state if we're in the start room - this would
        // interfere with the deliberately black screen for the initial dialogue
        bool isStartRoom = scene.name.Contains("Startroom") || scene.name.Contains("start_room");
        
        // Only reset transition state if we're not in the start room
        if (!isStartRoom && SceneTransitionManager.Instance != null)
        {
            // Give a small delay to let the actual transition complete first
            StartCoroutine(ResetTransitionStateAfterDelay(0.5f));
        }
        
        // IMPORTANT: We should only skip auto-fading if we're not explicitly calling a fade
        // from the SceneTransitionManager. The problem is we can't easily detect if a
        // fade has been explicitly requested. Therefore, we'll only skip if no fade is in progress.
        bool isOverworldScene = scene.name.StartsWith("Overworld_");
        
        // Skip automatic backup fading ONLY if another fade isn't already in progress
        // or if this is the start room where we want a black screen initially
        if ((isOverworldScene && SceneTransitionManager.Instance != null && !isFading) || isStartRoom)
        {
            Debug.Log($"ScreenFader skipping automatic fade for scene: {scene.name}");
            return;
        }
        
        // CRITICAL FIX: Reset the screen to clear when a new scene loads (except start room)
        // This prevents permanent black screens during transitions
        if (!isStartRoom && fadeImage != null && fadeImage.color.a > 0.5f && !isFading)
        {
            Debug.Log($"ScreenFader detected black screen on scene load for {scene.name}, resetting to visible");
            StartCoroutine(FadeFromBlack(defaultFadeDuration));
        }
    }
    
    /// <summary>
    /// Immediately resets the screen to be fully visible (no fade animation)
    /// </summary>
    public void ResetToVisible()
    {
        // Initialize components to ensure they exist
        InitializeFadeComponents();
        
        // Reset the fadeImage alpha to 0 (fully transparent)
        if (fadeImage != null)
        {
            isFading = false;
            Color clearColor = fadeImage.color;
            clearColor.a = 0f;
            fadeImage.color = clearColor;
            fadeImage.gameObject.SetActive(false);
            Debug.Log("ScreenFader immediately reset to visible using fadeImage");
        }
        // Fallback to canvasGroup if it exists (legacy support)
        else if (canvasGroup != null)
        {
            isFading = false;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            Debug.Log("ScreenFader immediately reset to visible using canvasGroup");
        }
    }

    /// <summary>
    /// Fade to black (or specified color) - Alias for FadeToBlack for better naming consistency
    /// </summary>
    public IEnumerator FadeOut(float duration = -1)
    {
        return FadeToBlack(duration);
    }
    
    /// <summary>
    /// Fade from black (or specified color) to clear - Alias for FadeFromBlack for better naming consistency
    /// </summary>
    public IEnumerator FadeIn(float duration = -1)
    {
        return FadeFromBlack(duration);
    }

    // Updated helper method to reset transition state with specified delay
    private IEnumerator ResetTransitionStateAfterDelay(float delay = 0.1f)
    {
        // Delay to allow other transition processes to complete first
        yield return new WaitForSecondsRealtime(delay);
        
        // Don't reset if we're in the start room, as it needs to stay black initially
        bool isStartRoom = SceneManager.GetActiveScene().name.Contains("Startroom") || 
                          SceneManager.GetActiveScene().name.Contains("start_room");
        
        // Skip reset in start room
        if (isStartRoom)
        {
            Debug.Log("[SCREEN FADER] Skipping transition state reset in start room to preserve initial black screen");
            yield break;
        }
        
        // Reset the transition state in SceneTransitionManager
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.CleanupTransitionState();
            Debug.Log($"[SCREEN FADER] Reset transition state after {delay}s delay");
        }
    }
} 