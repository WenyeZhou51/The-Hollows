using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    
    [Header("Scene Management")]
    [SerializeField] private string overworldSceneName = "Overworld_entrance";
    
    private bool isAnimating = false;
    private int currentPanelIndex = 0;
    private GameObject[] menuPanels;
    private Image[] panelImages;
    
    private void Start()
    {
        // Setup the navigation system
        SetupPanelNavigation();
    }
    
    private void SetupPanelNavigation()
    {
        // Create arrays of menu panels and their image components
        menuPanels = new GameObject[] { quitPanel, startPanel, continuePanel };
        panelImages = new Image[menuPanels.Length];
        
        // Get the Image component from each panel
        for (int i = 0; i < menuPanels.Length; i++)
        {
            if (menuPanels[i] != null)
            {
                panelImages[i] = menuPanels[i].GetComponent<Image>();
                
                // Make sure initial panel colors are set correctly
                if (panelImages[i] != null)
                {
                    // Make panel background transparent or clear
                    panelImages[i].color = normalColor;
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
        // Reset all panels to normal color
        for (int i = 0; i < panelImages.Length; i++)
        {
            if (panelImages[i] != null)
            {
                panelImages[i].color = normalColor;
            }
        }
        
        // Highlight the selected panel
        if (panelImages[currentPanelIndex] != null)
        {
            panelImages[currentPanelIndex].color = highlightColor;
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
        
        // Load the overworld scene
        SceneManager.LoadScene(overworldSceneName);
    }
} 