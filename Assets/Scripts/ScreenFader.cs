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
        
        // Skip automatic fading for overworld scenes when returning from battle
        // SceneTransitionManager will handle fading for these cases
        bool isOverworldScene = scene.name.StartsWith("Overworld_");
        bool isComingFromBattle = SceneManager.GetActiveScene().name.StartsWith("Battle_");
        
        if (isOverworldScene && SceneTransitionManager.Instance != null)
        {
            Debug.Log($"ScreenFader skipping automatic fade for overworld scene: {scene.name} - letting SceneTransitionManager handle it");
            return;
        }
        
        // CRITICAL FIX: Reset the screen to clear when a new scene loads
        // This prevents permanent black screens during transitions
        if (fadeImage != null && fadeImage.color.a > 0.5f)
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
} 