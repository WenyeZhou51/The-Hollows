using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

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
    private SkillData selectedSkill;
    private GameObject[] skillOptions;
    private bool isInSkillMenu = false;
    private int skillMenuColumns = 2;

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
            return;
        }

        if (isInSkillMenu)
        {
            HandleSkillMenuNavigation();
        }
        else
        {
            HandleMainMenuNavigation();
        }
    }

    private void HandleMainMenuNavigation()
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
        // Safety check to prevent null reference exceptions
        if (currentTargets == null || currentTargets.Count == 0)
        {
            Debug.LogWarning("[Target Selection] No targets available, ending target selection");
            EndTargetSelection();
            EnableMenu();
            return;
        }

        Debug.Log($"[Target Selection] Current target: {currentTargetSelection} of {currentTargets.Count}");

        // Check for any key press to help debug
        if (Input.anyKeyDown)
        {
            Debug.Log($"[Target Selection] Key detected: {Input.inputString}");
        }

        // Check for arrow keys with more flexible detection
        bool leftPressed = Input.GetKeyDown(KeyCode.LeftArrow);
        bool rightPressed = Input.GetKeyDown(KeyCode.RightArrow);
        
        if (leftPressed || rightPressed)
        {
            Debug.Log($"[Target Selection] Arrow key pressed: {(leftPressed ? "Left" : "Right")}");
            int oldSelection = currentTargetSelection;
            
            // For left arrow, move backward in the list
            if (leftPressed)
            {
                currentTargetSelection--;
                if (currentTargetSelection < 0) 
                    currentTargetSelection = currentTargets.Count - 1;
            }
            // For right arrow, move forward in the list
            else
            {
                currentTargetSelection++;
                if (currentTargetSelection >= currentTargets.Count) 
                    currentTargetSelection = 0;
            }
            
            Debug.Log($"[Target Selection] Changed target from {oldSelection} to {currentTargetSelection}");
            HighlightSelectedTarget();
        }

        // Also allow up/down arrows for navigation
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            Debug.Log($"[Target Selection] Up/Down arrow pressed");
            int oldSelection = currentTargetSelection;
            
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                currentTargetSelection--;
                if (currentTargetSelection < 0) 
                    currentTargetSelection = currentTargets.Count - 1;
            }
            else
            {
                currentTargetSelection++;
                if (currentTargetSelection >= currentTargets.Count) 
                    currentTargetSelection = 0;
            }
            
            Debug.Log($"[Target Selection] Changed target from {oldSelection} to {currentTargetSelection}");
            HighlightSelectedTarget();
        }

        // Check for confirm/cancel with more detailed logging
        bool confirmPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z);
        bool cancelPressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X);
        
        if (confirmPressed)
        {
            Debug.Log($"[Target Selection] Confirm key pressed");
            CombatStats target = currentTargets[currentTargetSelection];
            Debug.Log($"[Target Selection] Selected target: {target.name}");
            
            if (selectedSkill != null)
            {
                // Store a local reference to the skill before ending target selection
                SkillData skill = selectedSkill;
                Debug.Log($"[Target Selection] Executing skill {skill.name} on target {target.name}");
                EndTargetSelection();
                combatUI.ExecuteSkill(skill, target);
            }
            else
            {
                Debug.Log($"[Target Selection] Executing attack on target {target.name}");
                EndTargetSelection();
                combatUI.OnTargetSelected(target);
            }
        }
        else if (cancelPressed)
        {
            Debug.Log("[Target Selection] Cancel key pressed");
            EndTargetSelection();
            EnableMenu();
        }
    }

    private void HandleSkillMenuNavigation()
    {
        // Safety check to prevent errors if skillOptions is null or empty
        if (skillOptions == null || skillOptions.Length == 0)
        {
            Debug.LogWarning("[SkillButton Lifecycle] Skill options array is null or empty in HandleSkillMenuNavigation");
            isInSkillMenu = false;
            combatUI.BackToActionMenu();
            return;
        }

        // Create a list of valid buttons and their indices
        List<int> validButtonIndices = new List<int>();
        for (int i = 0; i < skillOptions.Length; i++) {
            if (skillOptions[i] != null && skillOptions[i].activeSelf) {
                validButtonIndices.Add(i);
            }
        }

        // If no valid buttons, exit skill menu
        if (validButtonIndices.Count == 0) {
            Debug.LogWarning("[SkillButton Lifecycle] No valid skill buttons found, exiting skill menu");
            isInSkillMenu = false;
            combatUI.BackToActionMenu();
            return;
        }

        // Make sure current selection is valid
        if (!validButtonIndices.Contains(currentSelection)) {
            currentSelection = validButtonIndices[0];
            UpdateSkillSelection();
        }

        int oldSelection = currentSelection;
        int totalSkills = skillOptions.Length;
        int rows = (totalSkills + skillMenuColumns - 1) / skillMenuColumns;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            // Find the next valid button to the right
            int currentIndex = validButtonIndices.IndexOf(currentSelection);
            if (currentIndex >= 0 && currentIndex < validButtonIndices.Count - 1) {
                currentSelection = validButtonIndices[currentIndex + 1];
                Debug.Log($"[SkillButton Lifecycle] Navigation - Moved right to skill {currentSelection}");
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            // Find the next valid button to the left
            int currentIndex = validButtonIndices.IndexOf(currentSelection);
            if (currentIndex > 0) {
                currentSelection = validButtonIndices[currentIndex - 1];
                Debug.Log($"[SkillButton Lifecycle] Navigation - Moved left to skill {currentSelection}");
            }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            // Find a valid button below (approximately)
            int currentRow = currentSelection / skillMenuColumns;
            int currentCol = currentSelection % skillMenuColumns;
            
            foreach (int index in validButtonIndices) {
                int row = index / skillMenuColumns;
                int col = index % skillMenuColumns;
                if (row > currentRow && col == currentCol) {
                    currentSelection = index;
                    Debug.Log($"[SkillButton Lifecycle] Navigation - Moved down to skill {currentSelection}");
                    break;
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            // Find a valid button above (approximately)
            int currentRow = currentSelection / skillMenuColumns;
            int currentCol = currentSelection % skillMenuColumns;
            
            // Iterate in reverse to find the closest button above
            for (int i = validButtonIndices.Count - 1; i >= 0; i--) {
                int index = validButtonIndices[i];
                int row = index / skillMenuColumns;
                int col = index % skillMenuColumns;
                if (row < currentRow && col == currentCol) {
                    currentSelection = index;
                    Debug.Log($"[SkillButton Lifecycle] Navigation - Moved up to skill {currentSelection}");
                    break;
                }
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentSelection < skillOptions.Length && skillOptions[currentSelection] != null)
            {
                var skillData = skillOptions[currentSelection].GetComponent<SkillButtonData>();
                if (skillData != null)
                {
                    Debug.Log($"[SkillButton Lifecycle] Skill selected with Z key - Skill: {skillData.skill.name}");
                    OnSkillSelected(skillData.skill);
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("[SkillButton Lifecycle] Exiting skill menu with X key");
            isInSkillMenu = false;
            combatUI.BackToActionMenu();
            return;
        }

        if (oldSelection != currentSelection)
        {
            UpdateSkillSelection();
        }
    }

    private void OnSkillSelected(SkillData skill)
    {
        Debug.Log($"[SkillButton Lifecycle] Skill selected - Name: {skill.name}, RequiresTarget: {skill.requiresTarget}, SanityCost: {skill.sanityCost}");
        
        // Set the selected skill BEFORE starting target selection
        SetSelectedSkill(skill);
        
        if (skill.requiresTarget)
        {
            Debug.Log($"[SkillButton Lifecycle] Skill requires target, starting target selection");
            StartTargetSelection();
        }
        else
        {
            Debug.Log($"[SkillButton Lifecycle] Skill does not require target, executing immediately");
            combatUI.ExecuteSkill(skill, null);
        }
    }

    private void UpdateSkillSelection()
    {
        // Safety check to prevent errors if skillOptions is null or empty
        if (skillOptions == null || skillOptions.Length == 0)
        {
            Debug.LogWarning("[SkillButton Lifecycle] Skill options array is null or empty in UpdateSkillSelection");
            return;
        }
        
        Debug.Log($"[SkillButton Lifecycle] Updating skill selection - Current selection: {currentSelection}");
        
        // First, create a list of valid buttons
        List<GameObject> validButtons = new List<GameObject>();
        for (int i = 0; i < skillOptions.Length; i++) {
            if (skillOptions[i] != null && skillOptions[i].activeSelf) {
                validButtons.Add(skillOptions[i]);
            }
        }
        
        // Update all valid buttons
        for (int i = 0; i < skillOptions.Length; i++)
        {
            // Skip null or inactive buttons
            if (skillOptions[i] == null || !skillOptions[i].activeSelf)
            {
                continue;
            }
            
            // Get the TextMeshProUGUI component directly from the GameObject
            TextMeshProUGUI text = skillOptions[i].GetComponentInChildren<TextMeshProUGUI>();
            Image buttonImage = skillOptions[i].GetComponent<Image>();

            if (i == currentSelection)
            {
                if (text != null) 
                {
                    text.color = selectedTextColor;
                    Debug.Log($"[SkillButton Lifecycle] Set selected text color for button {i}: '{text.text}'");
                }
                if (buttonImage != null) 
                {
                    buttonImage.color = selectedButtonColor;
                }
                
                // If we have a cursor, position it at the selected button
                if (cursor != null)
                {
                    RectTransform buttonRect = skillOptions[i].GetComponent<RectTransform>();
                    if (buttonRect != null)
                    {
                        cursor.position = buttonRect.position;
                        cursor.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                if (text != null) 
                {
                    text.color = normalTextColor;
                }
                if (buttonImage != null) 
                {
                    buttonImage.color = normalButtonColor;
                }
            }
        }
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

    public void StartTargetSelection()
    {
        // Check if we're selecting a target for Human Shield skill
        if (selectedSkill != null && selectedSkill.name == "Human Shield!")
        {
            // For Human Shield, we target allies instead of enemies
            currentTargets = new List<CombatStats>(combatManager.players);
            
            // Remove the active character (can't shield yourself)
            currentTargets.Remove(combatManager.ActiveCharacter);
            
            Debug.Log($"[Human Shield] Starting ally selection with {currentTargets.Count} potential targets");
            
            // Add detailed logging for each potential target
            for (int i = 0; i < currentTargets.Count; i++)
            {
                Debug.Log($"[Human Shield] Potential target {i}: {currentTargets[i].name}, isEnemy: {currentTargets[i].isEnemy}");
            }
        }
        else
        {
            // Default behavior - target enemies
            currentTargets = combatManager.GetLivingEnemies();
            Debug.Log($"[Target Selection] Starting enemy selection with {currentTargets.Count} potential targets");
        }
        
        if (currentTargets.Count > 0)
        {
            isSelectingTarget = true;
            currentTargetSelection = 0;
            SetMenuItemsEnabled(false);
            
            // Ensure the first target is highlighted
            HighlightSelectedTarget();
            Debug.Log($"[Target Selection] Initial target selected: {currentTargets[currentTargetSelection].name}");
        }
        else
        {
            Debug.LogWarning("[Target Selection] No valid targets found!");
            // If no targets, go back to menu
            isSelectingTarget = false;
            SetMenuItemsEnabled(true);
        }
    }

    private void EndTargetSelection()
    {
        Debug.Log("[Target Selection] Ending target selection");
        
        // Only attempt to clear highlights if currentTargets exists
        if (currentTargets != null)
        {
            Debug.Log($"[Target Selection] Clearing highlights for {currentTargets.Count} targets");
            foreach (var target in currentTargets)
            {
                if (target != null)
                {
                    target.HighlightCharacter(false);
                    Debug.Log($"[Target Selection] Unhighlighted: {target.name}");
                }
            }
            
            // Clear the list
            currentTargets.Clear();
        }
        
        // Log the skill that was being used
        if (selectedSkill != null)
        {
            Debug.Log($"[Target Selection] Clearing selected skill: {selectedSkill.name}");
        }
        
        // Reset state variables
        isSelectingTarget = false;
        selectedSkill = null;
        currentTargets = null;
        currentTargetSelection = 0;
        
        Debug.Log("[Target Selection] Target selection state reset");
    }

    public void EnableMenu()
    {
        isActive = true;
        currentSelection = 0;
        
        // Reset any ongoing target selection
        if (isSelectingTarget)
        {
            EndTargetSelection();
        }
        
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

    public void UpdateSkillMenuOptions(GameObject[] skillButtons)
    {
        Debug.Log($"[SkillButton Lifecycle] UpdateSkillMenuOptions called with {skillButtons.Length} buttons");
        
        // Filter out any inactive buttons (like the template)
        List<GameObject> activeButtons = new List<GameObject>();
        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i] != null && skillButtons[i].activeSelf)
            {
                activeButtons.Add(skillButtons[i]);
                Debug.Log($"[SkillButton Lifecycle] Added active button: {skillButtons[i].name}");
            }
            else if (skillButtons[i] != null)
            {
                Debug.Log($"[SkillButton Lifecycle] Skipping inactive button: {skillButtons[i].name}");
            }
        }
        
        // Store the active GameObjects
        skillOptions = activeButtons.ToArray();
        Debug.Log($"[SkillButton Lifecycle] Using {skillOptions.Length} active buttons for navigation");
        
        // Reset selection to first button if we have any
        currentSelection = skillOptions.Length > 0 ? 0 : -1;
        isInSkillMenu = skillOptions.Length > 0;
        
        // Log details about each skill button
        for (int i = 0; i < skillOptions.Length; i++)
        {
            TextMeshProUGUI text = skillOptions[i].GetComponentInChildren<TextMeshProUGUI>();
            SkillButtonData skillData = skillOptions[i].GetComponent<SkillButtonData>();
            Debug.Log($"[SkillButton Lifecycle] Skill button {i} - Text: '{text?.text ?? "unknown"}', " +
                      $"Skill: {skillData?.skill?.name ?? "unknown"}, " +
                      $"Position: {skillOptions[i].transform.position}, " +
                      $"Size: {skillOptions[i].GetComponent<RectTransform>().sizeDelta}");
            
            // Ensure the button has the correct components for menu-style buttons
            Image buttonImage = skillOptions[i].GetComponent<Image>();
            if (buttonImage == null)
            {
                buttonImage = skillOptions[i].AddComponent<Image>();
                Debug.Log($"[SkillButton Lifecycle] Added missing Image component to button {i}");
            }
            
            // Make sure the button is active and visible
            skillOptions[i].SetActive(true);
            
            // If the button has a CanvasGroup, make sure it's interactable
            CanvasGroup canvasGroup = skillOptions[i].GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
        
        // Only update selection if we have buttons
        if (skillOptions.Length > 0)
        {
            UpdateSkillSelection();
        }
        else
        {
            Debug.LogWarning("[SkillButton Lifecycle] No active skill buttons found!");
            cursor.gameObject.SetActive(false);
        }
    }

    public void SetSelectedSkill(SkillData skill)
    {
        Debug.Log($"[SkillButton Lifecycle] Setting selected skill: {skill.name}");
        selectedSkill = skill;
    }

    private void HighlightSelectedTarget()
    {
        // Safety check
        if (currentTargets == null || currentTargets.Count == 0 || currentTargetSelection < 0 || currentTargetSelection >= currentTargets.Count)
        {
            Debug.LogError("[Target Selection] Invalid target selection state in HighlightSelectedTarget");
            return;
        }
        
        Debug.Log($"[Target Selection] Highlighting target {currentTargetSelection}: {currentTargets[currentTargetSelection].name}");
        
        // Reset highlight for all targets
        foreach (var target in currentTargets)
        {
            if (target != null)
            {
                target.HighlightCharacter(false);
                Debug.Log($"[Target Selection] Unhighlighted: {target.name}");
            }
        }
        
        // Highlight selected target
        if (currentTargets[currentTargetSelection] != null)
        {
            currentTargets[currentTargetSelection].HighlightCharacter(true);
            Debug.Log($"[Target Selection] Highlighted: {currentTargets[currentTargetSelection].name}");
        }
    }

    public bool IsSelectingTarget()
    {
        return isSelectingTarget;
    }
    
    public void CancelTargetSelection()
    {
        EndTargetSelection();
    }

    public void ResetMenuState()
    {
        // Reset all state variables
        isActive = false;
        isSelectingTarget = false;
        isInSkillMenu = false;
        menuItemsEnabled = false;
        currentSelection = 0;
        selectedSkill = null;
        
        // End any ongoing target selection
        if (currentTargets != null)
        {
            foreach (var target in currentTargets)
            {
                if (target != null)
                {
                    target.HighlightCharacter(false);
                }
            }
            currentTargets = null;
        }
        
        // Reset UI elements
        if (cursor != null)
        {
            cursor.gameObject.SetActive(false);
        }
        
        // Now enable the menu with a clean state
        DisableMenu();
        EnableMenu();
    }
} 
} 