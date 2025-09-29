using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StartMenuManager : MonoBehaviour
{
    [Header("Background Reference")]
    [SerializeField] private StartMenuBackground background;
    
    [Header("Menu Panels")]
    [SerializeField] private GameObject continuePanel;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject quitPanel;
    [SerializeField] private GameObject buttonsContainer; // Parent container of all buttons
    
    [Header("Panel Highlight Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.white;
    
    [Header("Text Colors")]
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color highlightTextColor = Color.white;
    
    [Header("Scene Management")]
    [SerializeField] private string overworldSceneName = "Overworld_Startroom";
    
    private bool isAnimating = false;
    private int currentPanelIndex = 0;
    private GameObject[] menuPanels;
    private Image[] panelImages;
    private TextMeshProUGUI[] buttonTexts;
    
    private void Start()
    {
        // Setup the navigation system
        SetupPanelNavigation();
        
        // Ensure screen is visible (in case we came from combat loss)
        EnsureScreenIsVisible();
    }
    
    /// <summary>
    /// Ensures screen is faded in when start menu loads
    /// </summary>
    private void EnsureScreenIsVisible()
    {
        // Check if ScreenFader exists and make sure screen is faded in
        if (ScreenFader.Instance != null)
        {
            // First try immediate reset for better user experience
            ScreenFader.Instance.ResetToVisible();
            
            // Also start the normal fade as a backup
            StartCoroutine(ScreenFader.Instance.FadeFromBlack());
            
            Debug.Log("Start Menu ensuring screen visibility");
        }
        
        // Check for and clean up any remaining combat UI elements
        CleanupLingeringCombatElements();
    }
    
    /// <summary>
    /// Clean up any combat UI elements that might have persisted through scene transition
    /// </summary>
    private void CleanupLingeringCombatElements()
    {
        Debug.Log("StartMenuManager: Checking for lingering combat elements");
        
        // Common combat canvas and panel names
        string[] combatElementNames = new string[] {
            "CombatUI", "BattleUI", "CombatCanvas", "BattleCanvas", 
            "TextPanel", "ActionMenu", "SkillMenu", "ItemMenu", 
            "CharacterStatsPanel", "ActionDisplayLabel"
        };
        
        // Try to find and destroy these elements
        int elementsDestroyed = 0;
        foreach (string elementName in combatElementNames)
        {
            GameObject element = GameObject.Find(elementName);
            if (element != null)
            {
                Debug.Log($"Found and destroying lingering combat element: {elementName}");
                Destroy(element);
                elementsDestroyed++;
            }
        }
        
        // Look for any canvases with combat-related names
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in allCanvases)
        {
            string canvasName = canvas.gameObject.name.ToLower();
            if (canvasName.Contains("combat") || canvasName.Contains("battle") || 
                canvasName.Contains("enemy") || canvasName.Contains("action") || 
                canvasName.Contains("menu") || canvasName.Contains("skill") || 
                canvasName.Contains("item") || canvasName.Contains("stats") ||
                canvasName.Contains("panel"))
            {
                Debug.Log($"Found and destroying combat-related canvas: {canvas.gameObject.name}");
                Destroy(canvas.gameObject);
                elementsDestroyed++;
            }
        }
        
        if (elementsDestroyed > 0)
        {
            Debug.Log($"Cleaned up {elementsDestroyed} lingering combat elements");
        }
        else
        {
            Debug.Log("No lingering combat elements found");
        }
    }
    
    private void SetupPanelNavigation()
    {
        // Create arrays of menu panels and their image components
        menuPanels = new GameObject[] { quitPanel, startPanel, continuePanel };
        panelImages = new Image[menuPanels.Length];
        buttonTexts = new TextMeshProUGUI[menuPanels.Length];
        
        // Get the Image component and TextMeshPro component from each panel
        for (int i = 0; i < menuPanels.Length; i++)
        {
            if (menuPanels[i] != null)
            {
                panelImages[i] = menuPanels[i].GetComponent<Image>();
                
                // Find the TextMeshPro component in the panel
                buttonTexts[i] = menuPanels[i].GetComponentInChildren<TextMeshProUGUI>();
                
                // Make sure initial panel colors are set correctly
                if (panelImages[i] != null)
                {
                    // Make panel background transparent or clear
                    panelImages[i].color = normalColor;
                }
                
                // Set initial text color
                if (buttonTexts[i] != null)
                {
                    buttonTexts[i].color = normalTextColor;
                }
            }
        }
        
        // Set initial selection
        UpdateSelection();
    }
    
    private void Update()
    {
        // Don't allow navigation while animating
        if (isAnimating)
            return;
            
        // Left/Right arrow keys to navigate
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            // Increment index to move selection right
            currentPanelIndex = (currentPanelIndex + 1) % menuPanels.Length;
            UpdateSelection();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            // Decrement index to move selection left
            currentPanelIndex = (currentPanelIndex - 1 + menuPanels.Length) % menuPanels.Length;
            UpdateSelection();
        }
        
        // Z key to confirm selection
        if (Input.GetKeyDown(KeyCode.Z))
        {
            ConfirmSelection();
        }
    }
    
    private void UpdateSelection()
    {
        // Reset all panels to normal color and text to normal color
        for (int i = 0; i < panelImages.Length; i++)
        {
            if (panelImages[i] != null)
            {
                panelImages[i].color = normalColor;
            }
            
            if (buttonTexts[i] != null)
            {
                buttonTexts[i].color = normalTextColor;
            }
        }
        
        // Highlight the selected panel and text
        if (panelImages[currentPanelIndex] != null)
        {
            panelImages[currentPanelIndex].color = highlightColor;
        }
        
        if (buttonTexts[currentPanelIndex] != null)
        {
            buttonTexts[currentPanelIndex].color = highlightTextColor;
        }
    }
    
    private void ConfirmSelection()
    {
        // Handle the confirmed selection based on the current index
        switch (currentPanelIndex)
        {
            case 0: // Quit (now the leftmost panel)
                // Quit button functionality not implemented as requested
                break;
                
            case 1: // Start (middle panel)
                // Start button functionality not implemented as requested
                break;
                
            case 2: // Continue (now the rightmost panel)
                if (!isAnimating)
                {
                    StartCoroutine(PlayAnimationAndLoadScene());
                }
                break;
        }
    }
    
    // Public method to be called from UI button click events
    public void OnContinueButtonClicked()
    {
        if (!isAnimating)
        {
            // Immediately hide all buttons before starting animation
            if (buttonsContainer != null)
            {
                buttonsContainer.SetActive(false);
            }
            
            StartCoroutine(PlayAnimationAndLoadScene());
        }
    }
    
    private IEnumerator PlayAnimationAndLoadScene()
    {
        isAnimating = true;
        
        // Hide all buttons - kept for keyboard navigation path
        if (buttonsContainer != null && buttonsContainer.activeSelf)
        {
            buttonsContainer.SetActive(false);
        }
        
        // Reset all game data while preserving death counter
        ResetGameDataExceptDeaths();
        
        // Make sure ScreenFader exists
        ScreenFader.EnsureExists();
        
        // Play animation through the background script
        if (background != null)
        {
            yield return StartCoroutine(background.PlayAnimation());
        }
        else
        {
            // If no background script, just wait a bit
            yield return new WaitForSeconds(1.0f);
        }
        
        // Double check that the scene exists in build settings to avoid loading a non-existent scene
        if (!IsSceneValid(overworldSceneName))
        {
            Debug.LogError($"Scene '{overworldSceneName}' not found in build settings. Make sure to add it to the build settings!");
            // Ensure screen doesn't stay black
            StartCoroutine(ScreenFader.Instance.FadeFromBlack());
            isAnimating = false;
            yield break;
        }
        
        // Fade to black
        yield return StartCoroutine(ScreenFader.Instance.FadeToBlack());
        
        // Load the overworld scene and register for scene loaded event
        Debug.Log($"CRITICAL DEBUG: Registering OnSceneLoaded event handler before loading {overworldSceneName}");
        // Unregister first in case it's already registered
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log($"Loading scene: {overworldSceneName}");
        SceneManager.LoadScene(overworldSceneName);
    }
    
    // Handle scene loading completion and fade from black
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("CRITICAL DEBUG: OnSceneLoaded was called - will attempt to fade screen");
        
        // Skip handling for battle scenes - let SceneTransitionManager handle them
        if (scene.name.StartsWith("Battle_"))
        {
            Debug.Log($"CRITICAL DEBUG: Skipping StartMenuManager fade for battle scene: {scene.name}");
            // Unregister to prevent memory leaks and multiple calls
            SceneManager.sceneLoaded -= OnSceneLoaded;
            isAnimating = false;
            return;
        }
        
        // Safety check for ScreenFader
        if (ScreenFader.Instance == null)
        {
            Debug.LogError("CRITICAL ERROR: ScreenFader.Instance is null in OnSceneLoaded - creating new instance");
            ScreenFader.EnsureExists();
            
            // Double check it was created
            if (ScreenFader.Instance == null)
            {
                Debug.LogError("CRITICAL ERROR: Failed to create ScreenFader - scene will remain black");
                isAnimating = false;
                return;
            }
        }
        
        // Additional safety check - immediately fade using secondary method
        try
        {
            // Force reset canvas alpha to make screen visible in case coroutine fails
            var fader = ScreenFader.Instance;
            if (fader != null)
            {
                // This direct method call is a fallback to ensure at minimum we reset the screen
                Debug.Log("CRITICAL DEBUG: Calling ResetToVisible as additional safety measure");
                fader.ResetToVisible();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CRITICAL ERROR: Exception in direct ScreenFader access: {e.Message}");
        }
        
        // Ensure we're fading from black on any scene (being more permissive)
        Debug.Log($"CRITICAL DEBUG: Starting fade from black for scene: {scene.name}");
        StartCoroutine(ScreenFader.Instance.FadeFromBlack());
        
        // Unregister to prevent memory leaks and multiple calls
        SceneManager.sceneLoaded -= OnSceneLoaded;
        isAnimating = false;
    }
    
    /// <summary>
    /// Check if a scene is included in the build settings
    /// </summary>
    private bool IsSceneValid(string sceneName)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Resets all game data while preserving the death counter
    /// </summary>
    private void ResetGameDataExceptDeaths()
    {
        // Make sure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        if (PersistentGameManager.Instance != null)
        {
            // Store the current death count
            int currentDeaths = PersistentGameManager.Instance.GetDeaths();
            
            Debug.Log($"Resetting game data. Current deaths: {currentDeaths}");
            
            // Reset all game data
            PersistentGameManager.Instance.ResetAllData();
            
            // Restore the death count
            for (int i = 0; i < currentDeaths; i++)
            {
                PersistentGameManager.Instance.IncrementDeaths();
            }
            
            Debug.Log($"Game data reset complete. Deaths restored to: {PersistentGameManager.Instance.GetDeaths()}");
        }
    }
} 