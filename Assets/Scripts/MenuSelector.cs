using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MenuSelector : MonoBehaviour
{
    public RectTransform cursor;
    public RectTransform[] menuOptions;
    
    [Header("Text Colors")]
    [Tooltip("Color of the text when the menu item is selected")]
    public Color selectedTextColor = Color.yellow;
    [Tooltip("Color of the text when the menu is active but item is not selected")]
    public Color normalTextColor = Color.white;
    [Tooltip("Color of the text when the menu is disabled but visible")]
    public Color disabledTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [Tooltip("Color of the text when the menu is fully disabled")]
    public Color disabledMenuTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    
    [Header("Button Colors")]
    [Tooltip("Color of the button background when selected")]
    public Color selectedButtonColor = new Color(1f, 1f, 1f, 1f);
    [Tooltip("Color of the button background when not selected but menu is active")]
    public Color normalButtonColor = new Color(1f, 1f, 1f, 1f);
    [Tooltip("Color of the button background when menu is disabled")]
    public Color disabledButtonColor = new Color(1f, 1f, 1f, 0.5f);
    
    private int currentSelection = 0;
    private TextMeshProUGUI[] menuTexts;
    private CombatUI combatUI;
    private bool isActive = false;
    public GameObject actionMenu;
    private bool isSelectingTarget = false;
    private List<CombatStats> currentTargets;
    private int currentTargetSelection = 0;
    private CombatManager combatManager;
    private bool menuItemsEnabled = true;

    void Start()
    {
        combatUI = GetComponent<CombatUI>();
        combatManager = GetComponent<CombatManager>();
        menuTexts = new TextMeshProUGUI[menuOptions.Length];
        
        for (int i = 0; i < menuOptions.Length; i++)
        {
            menuTexts[i] = menuOptions[i].GetComponentInChildren<TextMeshProUGUI>(true);
            
            if (menuTexts[i] != null)
            {
                // Move the text component to be a direct child of the button
                menuTexts[i].transform.SetParent(menuOptions[i], false);
                menuTexts[i].transform.SetAsFirstSibling();

                Canvas canvas = actionMenu.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = 10; // Higher than panel's order
                }
                
                // Remove any existing Canvas component we might have added before
                Canvas existingCanvas = menuTexts[i].gameObject.GetComponent<Canvas>();
                if (existingCanvas != null)
                {
                    Destroy(existingCanvas);
                }
                
                // Add CanvasGroup to the button (parent) instead
                CanvasGroup buttonGroup = menuOptions[i].gameObject.GetComponent<CanvasGroup>();
                if (buttonGroup == null)
                {
                    buttonGroup = menuOptions[i].gameObject.AddComponent<CanvasGroup>();
                }
                
                // Ensure text is the last sibling to render on top
                menuTexts[i].enabled = true;
                menuTexts[i].gameObject.SetActive(true);
            }
        }
        
        SetMenuItemsEnabled(false);
    }

    void Update()
    {
        if (!isActive) return;

        if (isSelectingTarget)
        {
            HandleTargetSelection();
        }
        else
        {
            HandleMenuSelection();
        }
    }

    private void HandleMenuSelection()
    {
        // Only allow selection changes and execution if items are enabled
        if (menuItemsEnabled)
        {
            // Vertical navigation
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                currentSelection--;
                if (currentSelection < 0) currentSelection = menuOptions.Length - 1;
                UpdateSelection();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                currentSelection++;
                if (currentSelection >= menuOptions.Length) currentSelection = 0;
                UpdateSelection();
            }

            // Confirm selection
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z))
            {
                ExecuteSelection();
            }
        }

        // Cancel/Back (allow this even when disabled)
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X))
        {
            // Implement back functionality
        }
    }

    private void HandleTargetSelection()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentTargetSelection = (currentTargetSelection + 1) % currentTargets.Count;
            HighlightSelectedTarget();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z))
        {
            CombatStats target = currentTargets[currentTargetSelection];
            combatUI.OnTargetSelected(target);
            EndTargetSelection();
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X))
        {
            EndTargetSelection();
            // Return to main menu
            EnableMenu();
        }
    }

    private void HighlightSelectedTarget()
    {
        // Reset highlight for all targets
        foreach (var target in currentTargets)
        {
            target.HighlightCharacter(false);
        }
        // Highlight selected target
        currentTargets[currentTargetSelection].HighlightCharacter(true);
    }

    void UpdateSelection()
    {
        // Move cursor to selected option
        cursor.position = new Vector3(
            cursor.position.x,
            menuOptions[currentSelection].position.y,
            cursor.position.z
        );

        // Keep cursor's x position relative to menu
        cursor.localPosition = new Vector3(
            -20f, // Adjust this value to position cursor
            cursor.localPosition.y,
            cursor.localPosition.z
        );

        // Update text colors
        for (int i = 0; i < menuTexts.Length; i++)
        {
            menuTexts[i].color = (i == currentSelection) ? selectedTextColor : normalTextColor;
        }
    }

    void ExecuteSelection()
    {
        switch (currentSelection)
        {
            case 0: // Attack
                StartTargetSelection();
                break;
            case 1: // Skills
                combatUI.OnSkillSelected();
                break;
            case 2: // Guard
                combatUI.OnGuardSelected();
                break;
            case 3: // Heal
                combatUI.OnHealSelected();
                break;
        }
    }

    private void StartTargetSelection()
    {
        currentTargets = combatManager.GetLivingEnemies();
        if (currentTargets.Count > 0)
        {
            isSelectingTarget = true;
            currentTargetSelection = 0;
            SetMenuItemsEnabled(false); // Just disable interaction
            HighlightSelectedTarget();
        }
    }

    private void EndTargetSelection()
    {
        isSelectingTarget = false;
        foreach (var target in currentTargets)
        {
            if (target != null)
            {
                target.HighlightCharacter(false);
            }
        }
        currentTargets = null;
    }

    public void EnableMenu()
    {
        isActive = true;
        currentSelection = 0;
        
        // Ensure menu and all children are visible
        if (actionMenu != null)
        {
            actionMenu.SetActive(true);
            foreach (Transform child in actionMenu.transform)
            {
                child.gameObject.SetActive(true);
            }
        }
        
        SetMenuItemsEnabled(true);
        UpdateSelection();
    }

    public void DisableMenu()
    {
        isActive = false;
        
        if (actionMenu != null)
        {
            actionMenu.SetActive(true);
            
            // First activate all objects
            foreach (Transform child in actionMenu.transform)
            {
                child.gameObject.SetActive(true);
                
                // Get the panel's Canvas component
                Canvas buttonCanvas = child.gameObject.GetComponent<Canvas>();
                if (buttonCanvas == null)
                {
                    buttonCanvas = child.gameObject.AddComponent<Canvas>();
                }
                buttonCanvas.overrideSorting = true;
                buttonCanvas.sortingOrder = 1; // Panel order
                
                // Get text component
                TextMeshProUGUI text = child.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    Canvas textCanvas = text.gameObject.GetComponent<Canvas>();
                    if (textCanvas == null)
                    {
                        textCanvas = text.gameObject.AddComponent<Canvas>();
                    }
                    textCanvas.overrideSorting = true;
                    textCanvas.sortingOrder = 2;
                    
                    text.enabled = true;
                    text.color = disabledMenuTextColor;
                }
            }
        }
        
        SetMenuItemsEnabled(false);
    }

    public void SetMenuItemsEnabled(bool enabled)
    {
        menuItemsEnabled = enabled;
        
        for (int i = 0; i < menuOptions.Length; i++)
        {
            if (menuTexts[i] != null)
            {
                // Set button background color
                Image buttonImage = menuOptions[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = enabled ? 
                        (i == currentSelection ? selectedButtonColor : normalButtonColor) : 
                        disabledButtonColor;
                }
                
                // Set text color
                Color finalTextColor = enabled ? 
                    (i == currentSelection ? selectedTextColor : normalTextColor) : 
                    disabledTextColor;
                menuTexts[i].color = finalTextColor;
                
                // Handle interactivity
                CanvasGroup buttonGroup = menuOptions[i].gameObject.GetComponent<CanvasGroup>();
                if (buttonGroup != null)
                {
                    buttonGroup.interactable = enabled;
                    buttonGroup.blocksRaycasts = enabled;
                }
            }
        }
        
        if (cursor != null)
        {
            cursor.gameObject.SetActive(enabled);
        }
    }
} 