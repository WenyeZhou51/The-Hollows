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
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Image fadeImage;
    private Canvas fadeCanvas;
    private CanvasGroup canvasGroup;
    private bool isFading = false;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupFadeComponents();
            
            // Register for scene loaded events as a backup fade mechanism
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Debug.Log("ScreenFader initialized and registered for scene transitions");
        }
        else
        {
            Destroy(gameObject);
        }
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
    private void SetupFadeComponents()
    {
        // Create canvas
        fadeCanvas = gameObject.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999; // Ensure it renders on top of everything
        
        // Add canvas scaler for proper scaling on different resolutions
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Add canvas group for easy fade control
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // Start fully transparent
        canvasGroup.blocksRaycasts = false; // Don't block input when invisible
        
        // Create image that covers the entire screen
        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(transform);
        
        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = fadeColor;
        
        // Set the image to cover the entire screen
        RectTransform rectTransform = fadeImage.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Ensures an instance of ScreenFader exists in the scene
    /// </summary>
    /// <returns>The ScreenFader instance</returns>
    public static ScreenFader EnsureExists()
    {
        // Don't create a new instance if the application is quitting or we're switching scenes
        if (isQuitting || SceneManager.GetActiveScene().isLoaded == false)
        {
            return Instance;
        }
        
        if (Instance == null)
        {
            // Look for existing instance
            ScreenFader[] faders = FindObjectsOfType<ScreenFader>();
            
            if (faders.Length > 0)
            {
                // Use first instance found
                Instance = faders[0];
                Debug.Log("Found existing ScreenFader");
                
                // Destroy any extras
                for (int i = 1; i < faders.Length; i++)
                {
                    Debug.LogWarning("Destroying extra ScreenFader instance");
                    Destroy(faders[i].gameObject);
                }
            }
            else
            {
                // Only create a new instance if we're not during scene unloading
                GameObject faderObj = new GameObject("ScreenFader");
                Instance = faderObj.AddComponent<ScreenFader>();
                Debug.Log("Created new ScreenFader");
            }
        }
        
        return Instance;
    }

    /// <summary>
    /// Fades the screen to black
    /// </summary>
    /// <param name="onFadeComplete">Action to call when fade completes</param>
    /// <returns>Coroutine that can be awaited</returns>
    public IEnumerator FadeToBlack(Action onFadeComplete = null)
    {
        yield return FadeTo(1f, onFadeComplete);
    }

    /// <summary>
    /// Fades the screen from black to clear
    /// </summary>
    /// <param name="onFadeComplete">Action to call when fade completes</param>
    /// <returns>Coroutine that can be awaited</returns>
    public IEnumerator FadeFromBlack(Action onFadeComplete = null)
    {
        // Ensure screen isn't permanently black
        if (canvasGroup.alpha > 0.99f)
        {
            Debug.Log("Screen is fully black, ensuring fade from black will run");
        }
        
        // Add a safety check to ensure canvasGroup is available
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup is null when trying to fade from black - attempting to recreate components");
            SetupFadeComponents();
            
            // If still null after attempting to recreate, force reset and exit
            if (canvasGroup == null)
            {
                Debug.LogError("Failed to recreate CanvasGroup - forcing immediate visibility");
                ResetToVisible();
                yield break;
            }
        }
        
        yield return FadeTo(0f, onFadeComplete);
        
        // Extra safety check to ensure screen is fully visible after fade completes
        if (canvasGroup.alpha > 0.1f)
        {
            Debug.LogWarning("Screen may still be partially black after fade - forcing visibility");
            ResetToVisible();
        }
    }

    /// <summary>
    /// Performs the fade operation to the target alpha
    /// </summary>
    /// <param name="targetAlpha">Target alpha value (0-1)</param>
    /// <param name="onFadeComplete">Action to call when fade completes</param>
    /// <returns>Coroutine that can be awaited</returns>
    private IEnumerator FadeTo(float targetAlpha, Action onFadeComplete = null)
    {
        if (isFading)
        {
            yield break;
        }

        isFading = true;
        canvasGroup.blocksRaycasts = targetAlpha > 0; // Block input only when fading to black
        
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / fadeDuration);
            float curveValue = fadeCurve.Evaluate(normalizedTime);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            
            yield return null;
        }
        
        canvasGroup.alpha = targetAlpha;
        isFading = false;
        
        onFadeComplete?.Invoke();
    }

    /// <summary>
    /// Immediately resets the screen to be fully visible (no fade animation)
    /// </summary>
    public void ResetToVisible()
    {
        if (canvasGroup != null)
        {
            isFading = false;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            Debug.Log("ScreenFader immediately reset to visible");
        }
    }

    // Safety method to ensure screen doesn't stay black during scene changes
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Wait a short time to let other scripts register their fade operations
        StartCoroutine(DelayedFadeCheck(scene.name));
    }
    
    private IEnumerator DelayedFadeCheck(string sceneName)
    {
        // Wait for other scripts to potentially trigger a fade
        yield return new WaitForSeconds(0.5f);
        
        // If the screen is still mostly black, force it to become visible
        if (canvasGroup != null && canvasGroup.alpha > 0.9f)
        {
            Debug.LogWarning($"ScreenFader: Screen still black 0.5s after loading scene {sceneName} - forcing visibility");
            // Use direct method call for reliability
            ResetToVisible();
        }
    }
} 