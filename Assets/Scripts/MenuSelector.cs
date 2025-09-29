using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

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
    private ItemData selectedItem;
    private GameObject[] skillOptions;
    private bool isInSkillMenu = false;
    private int skillMenuColumns = 1; // Changed to single column
    
    // Cycling skill system variables
    private CombatUI cyclingCombatUI;
    private List<SkillData> cyclingAllSkills;
    private int cyclingScrollIndex;

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
            Debug.Log("[Menu Navigation] Back button pressed");
            
            // Implement back functionality
            if (isInSkillMenu)
            {
                Debug.Log("[Menu Navigation] Returning from skill menu to action menu");
                combatUI.BackToActionMenu();
                isInSkillMenu = false;
            }
            else if (combatManager.isItemMenuActive)
            {
                Debug.Log("[Menu Navigation] Returning from item menu to action menu");
                combatUI.BackToItemMenu();
                combatManager.isItemMenuActive = false;
            }
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

        // Check for team-wide or group targeting items
        bool isTeamWideItem = selectedItem != null && (
            string.Equals(selectedItem.name, "Fruit Juice", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selectedItem.name, "Otherworldly Tome", StringComparison.OrdinalIgnoreCase));
            
        bool isAllEnemyItem = selectedItem != null && (
            string.Equals(selectedItem.name, "Pocket Sand", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selectedItem.name, "Unstable Catalyst", StringComparison.OrdinalIgnoreCase));
            
        bool isSingleEnemyItem = selectedItem != null && 
            string.Equals(selectedItem.name, "Shiny Bead", StringComparison.OrdinalIgnoreCase);
        
        if (!isTeamWideItem && !isAllEnemyItem && !isSingleEnemyItem)
        {
            Debug.Log($"[DEBUG TARGETING] HandleTargetSelection - Current target: {currentTargetSelection} of {currentTargets.Count}, " +
                    $"Target: {currentTargets[currentTargetSelection].name}, isEnemy: {currentTargets[currentTargetSelection].isEnemy}");

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
        }
        else if (isTeamWideItem)
        {
            Debug.Log($"[DEBUG TARGETING] {selectedItem.name} team targeting active - waiting for confirmation");
        }
        else if (isAllEnemyItem)
        {
            Debug.Log($"[DEBUG TARGETING] {selectedItem.name} all-enemy targeting active - waiting for confirmation");
        }

        // Check for confirm/cancel with more detailed logging
        bool confirmPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z);
        bool cancelPressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X);
        
        if (confirmPressed)
        {
            if (isTeamWideItem)
            {
                Debug.Log($"[DEBUG TARGETING] Confirm key pressed for team-wide {selectedItem.name}");
                ItemData itemToUse = selectedItem; // Store reference before ending target selection
                
                // We need to end target selection BEFORE executing the item to prevent references from getting null
                EndTargetSelection();
                
                // Use the item on entire team (null target indicates team-wide effect)
                combatUI.ExecuteItem(itemToUse, null);
            }
            else if (isAllEnemyItem)
            {
                Debug.Log($"[DEBUG TARGETING] Confirm key pressed for all-enemy {selectedItem.name}");
                ItemData itemToUse = selectedItem; // Store reference before ending target selection
                
                // For all-enemy items, we still need a reference enemy to pass to the ExecuteItem method
                // This ensures the target parameter is not null (preventing the error)
                CombatStats targetEnemy = null;
                if (currentTargets != null && currentTargets.Count > 0)
                {
                    targetEnemy = currentTargets[0]; // Use first enemy as reference
                    Debug.Log($"[DEBUG TARGETING] Using {targetEnemy.name} as reference target for all-enemy effect");
                }
                
                // We need to end target selection BEFORE executing the item to prevent references from getting null
                EndTargetSelection();
                
                // Use the item with a reference enemy target
                combatUI.ExecuteItem(itemToUse, targetEnemy);
            }
            else if (isSingleEnemyItem)
            {
                Debug.Log($"[DEBUG TARGETING] Confirm key pressed for single-enemy targeting {selectedItem.name}");
                ItemData itemToUse = selectedItem; // Store reference before ending target selection
                
                // For Shiny Bead, we need to target the specific selected enemy
                CombatStats targetEnemy = null;
                if (currentTargets != null && currentTargets.Count > 0 && currentTargetSelection >= 0 && currentTargetSelection < currentTargets.Count)
                {
                    targetEnemy = currentTargets[currentTargetSelection];
                    Debug.Log($"[DEBUG TARGETING] Using selected enemy {targetEnemy.name} as target for {selectedItem.name}");
                }
                else if (currentTargets != null && currentTargets.Count > 0)
                {
                    // Fallback to first enemy if selection index is invalid
                    targetEnemy = currentTargets[0];
                    Debug.Log($"[DEBUG TARGETING] Using fallback enemy {targetEnemy.name} as target for {selectedItem.name}");
                }
                
                // We need to end target selection BEFORE executing the item to prevent references from getting null
                EndTargetSelection();
                
                // Use the item on the selected enemy
                combatUI.ExecuteItem(itemToUse, targetEnemy);
            }
            else
            {
                Debug.Log($"[DEBUG TARGETING] Confirm key pressed for target selection");
                CombatStats target = currentTargets[currentTargetSelection];
                Debug.Log($"[DEBUG TARGETING] Selected target: {target.name}, isEnemy: {target.isEnemy}");
                
                if (selectedSkill != null)
                {
                    // Store a local reference to the skill before ending target selection
                    SkillData skill = selectedSkill;
                    Debug.Log($"[DEBUG TARGETING] Executing skill {skill.name} on target {target.name}");
                    EndTargetSelection();
                    combatUI.ExecuteSkill(skill, target);
                }
                else if (selectedItem != null)
                {
                    ItemData itemToUse = selectedItem; // Store reference before ending target selection
                    Debug.Log($"[DEBUG TARGETING] Using item: {itemToUse.name} on target: {target.name}, isEnemy: {target.isEnemy}");
                    
                    // We need to end target selection BEFORE executing the item to prevent references from getting null
                    EndTargetSelection();
                    
                    // Now use the stored reference
                    combatUI.ExecuteItem(itemToUse, target);
                }
                else
                {
                    Debug.Log($"[DEBUG TARGETING] Executing attack on target {target.name}");
                    EndTargetSelection();
                    combatUI.OnTargetSelected(target);
                }
            }
        }
        else if (cancelPressed)
        {
            Debug.Log("[Target Selection] Cancel key pressed");
            EndTargetSelection();
            EnableMenu();
        }
    }

    public void SetCyclingSkillSystem(CombatUI combatUI, List<SkillData> allSkills, int scrollIndex)
    {
        cyclingCombatUI = combatUI;
        cyclingAllSkills = allSkills;
        cyclingScrollIndex = scrollIndex;
        Debug.Log($"[Cycling Skill Menu] Set cycling system with {allSkills.Count} skills, scroll index: {scrollIndex}");
    }

    public void ClearCyclingSystem()
    {
        cyclingCombatUI = null;
        cyclingAllSkills = null;
        cyclingScrollIndex = 0;
        Debug.Log("[Cycling Skill Menu] Cleared cycling system variables");
    }

    private void HandleSkillMenuNavigation()
    {
        // Check if we're using the cycling system
        if (cyclingCombatUI != null && cyclingAllSkills != null)
        {
            HandleCyclingSkillNavigation();
            return;
        }

        // Fallback to original system
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

        // Simplified vertical navigation for single column layout
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            // Move to next valid button
            int currentIndex = validButtonIndices.IndexOf(currentSelection);
            if (currentIndex >= 0 && currentIndex < validButtonIndices.Count - 1) {
                currentSelection = validButtonIndices[currentIndex + 1];
                Debug.Log($"[SkillButton Lifecycle] Navigation - Moved down to skill {currentSelection}");
            }
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            // Move to previous valid button
            int currentIndex = validButtonIndices.IndexOf(currentSelection);
            if (currentIndex > 0) {
                currentSelection = validButtonIndices[currentIndex - 1];
                Debug.Log($"[SkillButton Lifecycle] Navigation - Moved up to skill {currentSelection}");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentSelection < skillOptions.Length && skillOptions[currentSelection] != null)
            {
                // Check for both SkillButtonData and ItemButtonData
                var skillData = skillOptions[currentSelection].GetComponent<SkillButtonData>();
                var itemData = skillOptions[currentSelection].GetComponent<ItemButtonData>();
                
                if (skillData != null)
                {
                    Debug.Log($"[SkillButton Lifecycle] Skill selected with Z key - Skill: {skillData.skill.name}");
                    OnSkillSelected(skillData.skill);
                }
                else if (itemData != null)
                {
                    Debug.Log($"[ItemButton Lifecycle] Item selected with Z key - Item: {itemData.item.name}");
                    // Call the CombatUI's OnItemButtonClicked method
                    combatUI.OnItemButtonClicked(itemData.item);
                }
                else
                {
                    Debug.LogWarning("[Button Lifecycle] Selected button has neither SkillButtonData nor ItemButtonData component");
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
            UpdateCurrentSelectionDescription();
        }
    }

    private void HandleCyclingSkillNavigation()
    {
        // Safety check
        if (cyclingCombatUI == null || cyclingAllSkills == null || skillOptions == null)
        {
            Debug.LogWarning("[Cycling Skill Menu] Cycling system not properly initialized");
            isInSkillMenu = false;
            combatUI.BackToActionMenu();
            return;
        }

        // Make sure current selection is valid (0, 1, or 2)
        if (currentSelection < 0 || currentSelection >= 3)
        {
            currentSelection = 0;
        }

        int oldSelection = currentSelection;

        // Handle navigation with cycling
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentSelection < 2)
            {
                // Move to next button in the 3-button window
                currentSelection++;
                Debug.Log($"[Cycling Skill Menu] Moved down to button {currentSelection}");
            }
            else if (cyclingCombatUI.CanScrollDown())
            {
                // We're at the bottom button and can scroll down
                cyclingCombatUI.ScrollDown();
                Debug.Log($"[Cycling Skill Menu] Scrolled down, now showing skills from index {cyclingCombatUI.currentSkillScrollIndex}");
            }
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentSelection > 0)
            {
                // Move to previous button in the 3-button window
                currentSelection--;
                Debug.Log($"[Cycling Skill Menu] Moved up to button {currentSelection}");
            }
            else if (cyclingCombatUI.CanScrollUp())
            {
                // We're at the top button and can scroll up
                cyclingCombatUI.ScrollUp();
                Debug.Log($"[Cycling Skill Menu] Scrolled up, now showing skills from index {cyclingCombatUI.currentSkillScrollIndex}");
            }
        }

        // Handle skill selection
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentSelection < skillOptions.Length && skillOptions[currentSelection] != null)
            {
                // Get the skill data from the cycling system
                SkillData skill = cyclingCombatUI.GetSkillAtButtonIndex(currentSelection);
                if (skill != null)
                {
                    Debug.Log($"[Cycling Skill Menu] Skill selected with Z key - Skill: {skill.name}");
                    OnSkillSelected(skill);
                }
                else
                {
                    Debug.LogWarning("[Cycling Skill Menu] No skill found at button index " + currentSelection);
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("[Cycling Skill Menu] Exiting skill menu with X key");
            isInSkillMenu = false;
            ClearCyclingSystem(); // Clear cycling system properly
            combatUI.BackToActionMenu();
            return;
        }

        if (oldSelection != currentSelection)
        {
            UpdateSkillSelection();
            UpdateCurrentSelectionDescription();
        }
    }

    private void OnSkillSelected(SkillData skill)
    {
        Debug.Log($"[SkillButton Lifecycle] Skill selected - Name: {skill.name}, RequiresTarget: {skill.requiresTarget}, SanityCost: {skill.sanityCost}");
        
        // Clear cycling system when a skill is selected
        ClearCyclingSystem();
        
        // Set the selected skill BEFORE starting target selection
        SetSelectedSkill(skill);
        
        if (skill.requiresTarget)
        {
            Debug.Log($"[SkillButton Lifecycle] Skill requires target, starting target selection");
            StartTargetSelection();
        }
        else
        {
            // Skills like "Cleansing Wave" or "Signal Flare" that don't require a target
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
            
            // Get ALL TextMeshProUGUI components (skill name and cost text)
            TextMeshProUGUI[] allTexts = skillOptions[i].GetComponentsInChildren<TextMeshProUGUI>();
            Image buttonImage = skillOptions[i].GetComponent<Image>();

            if (i == currentSelection)
            {
                // Update all text components (skill name and cost)
                foreach (TextMeshProUGUI text in allTexts)
                {
                    if (text != null) 
                    {
                        text.color = selectedTextColor;
                        Debug.Log($"[SkillButton Lifecycle] Set selected text color for button {i}: '{text.text}'");
                    }
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
                
                // Scroll to show the selected button
                combatUI.ScrollToSkillButton(i, skillOptions);
            }
            else
            {
                // Update all text components (skill name and cost)
                foreach (TextMeshProUGUI text in allTexts)
                {
                    if (text != null) 
                    {
                        text.color = normalTextColor;
                    }
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

        // Update text colors and button colors
        for (int i = 0; i < menuTexts.Length; i++)
        {
            // Update text color
            menuTexts[i].color = (i == currentSelection) ? selectedTextColor : normalTextColor;
            
            // Update button background color
            Image buttonImage = menuOptions[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = (i == currentSelection) ? selectedButtonColor : normalButtonColor;
            }
        }
    }

    private void ExecuteSelection()
    {
        if (currentSelection < 0 || currentSelection >= menuOptions.Length) return;
        
        // Get the name of the selected option, trim whitespace and newlines, and convert to lowercase
        string selectedOption = menuTexts[currentSelection].text.Trim().ToLower();
        
        Debug.Log($"[MenuSelector] Executing selection: {selectedOption}");
        
        switch (selectedOption)
        {
            case "attack":
                Debug.Log("[MenuSelector] Attack selected, starting target selection");
                StartTargetSelection();
                break;
            case "skill":
            case "skills":
                Debug.Log("[MenuSelector] Skill selected, showing skill menu");
                isInSkillMenu = true;
                combatUI.OnSkillSelected();
                break;
            case "item":
            case "items":
                Debug.Log("[MenuSelector] Item selected, showing item menu");
                isInSkillMenu = true; // Reuse the skill menu navigation for items
                combatUI.ShowItemMenu();
                break;
            case "guard":
                Debug.Log("[MenuSelector] Guard selected");
                combatUI.OnGuardSelected();
                break;
            case "heal":
                Debug.Log("[MenuSelector] Heal selected");
                combatUI.OnHealSelected();
                break;
            default:
                Debug.Log($"[MenuSelector] Unknown option: {selectedOption}");
                break;
        }
    }

    public void StartTargetSelection()
    {
        Debug.Log($"[DEBUG TARGETING] StartTargetSelection - SelectedSkill: {selectedSkill?.name ?? "none"}, SelectedItem: {selectedItem?.name ?? "none"}");

        // Check if we're selecting a target for ally-targeting skills
        if (selectedSkill != null && (selectedSkill.name == "Human Shield!" || 
                                    selectedSkill.name == "Healing Words" || 
                                    selectedSkill.name == "What Doesn't Kill You" ||
                                    selectedSkill.name == "Crescendo" ||
                                    selectedSkill.name == "Encore" ||
                                    selectedSkill.name == "Respite"))
        {
            Debug.Log($"[DEBUG TARGETING] Detected ally-targeting SKILL: {selectedSkill.name}");
            // For ally-targeting skills, we target allies instead of enemies
            currentTargets = new List<CombatStats>(combatManager.players);
            
            // Remove the active character for skills that can't target self
            if (selectedSkill.name == "Human Shield!") {
                currentTargets.Remove(combatManager.ActiveCharacter);
            }
            
            Debug.Log($"[Ally Targeting] Starting ally selection with {currentTargets.Count} potential targets for {selectedSkill.name}");
            
            // Add detailed logging for each potential target
            for (int i = 0; i < currentTargets.Count; i++)
            {
                Debug.Log($"[Ally Targeting] Potential target {i}: {currentTargets[i].name}, isEnemy: {currentTargets[i].isEnemy}");
            }
        }
        // Check if we're selecting targets for Fruit Juice (team-wide effect)
        else if (selectedItem != null && string.Equals(selectedItem.name, "Fruit Juice", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[DEBUG TARGETING] Detected team-targeting item: {selectedItem.name}");
            // For Fruit Juice, include all allies and highlight them all
            currentTargets = new List<CombatStats>(combatManager.players);
            
            // No need to remove the active character since it affects everyone
            Debug.Log($"[DEBUG TARGETING] Highlighting entire team for {selectedItem.name}");
            
            // Add detailed logging for each potential target
            for (int i = 0; i < currentTargets.Count; i++)
            {
                Debug.Log($"[DEBUG TARGETING] Team target {i}: {currentTargets[i].name}, isEnemy: {currentTargets[i].isEnemy}");
            }
            
            // Highlight all team members
            HighlightAllTeamMembers();
        }
        // Check if we're selecting a target for ally-targeting item (Super Espress-O, Panacea, Tower Shield, Ramen)
        else if (selectedItem != null && (
            string.Equals(selectedItem.name, "Super Espress-O", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selectedItem.name, "Panacea", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selectedItem.name, "Tower Shield", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selectedItem.name, "Ramen", StringComparison.OrdinalIgnoreCase)))
        {
            Debug.Log($"[DEBUG TARGETING] Detected ally-targeting ITEM: {selectedItem.name}");
            // For ally-targeting items, target allies instead of enemies
            currentTargets = new List<CombatStats>(combatManager.players);
            
            // Allow self-targeting
            Debug.Log($"[DEBUG TARGETING] Allowing self-targeting for {selectedItem.name}");
            
            Debug.Log($"[DEBUG TARGETING] Starting ally selection with {currentTargets.Count} potential targets for item {selectedItem.name}");
            
            // Add detailed logging for each potential target
            for (int i = 0; i < currentTargets.Count; i++)
            {
                Debug.Log($"[DEBUG TARGETING] Potential ally target {i}: {currentTargets[i].name}, isEnemy: {currentTargets[i].isEnemy}");
            }
        }
        // Check if we're selecting a target for enemy-targeting item (Shiny Bead)
        else if (selectedItem != null && string.Equals(selectedItem.name, "Shiny Bead", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[DEBUG TARGETING] Detected enemy-targeting ITEM: {selectedItem.name}");
            // For enemy-targeting items, target enemies
            currentTargets = combatManager.GetLivingEnemies();
            
            Debug.Log($"[DEBUG TARGETING] Starting enemy selection with {currentTargets.Count} potential targets for item {selectedItem.name}");
            
            // Add detailed logging for each potential target
            for (int i = 0; i < currentTargets.Count; i++)
            {
                Debug.Log($"[DEBUG TARGETING] Potential enemy target {i}: {currentTargets[i].name}, isEnemy: {currentTargets[i].isEnemy}");
            }
            
            // Make sure we actually highlight and select an enemy
            if (currentTargets.Count > 0)
            {
                currentTargetSelection = 0;
                HighlightSelectedTarget();
                Debug.Log($"[DEBUG TARGETING] Initially selected enemy target: {currentTargets[currentTargetSelection].name}");
            }
        }
        // Check if we're selecting for team-wide items (Otherworldly Tome)
        else if (selectedItem != null && string.Equals(selectedItem.name, "Otherworldly Tome", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[DEBUG TARGETING] Detected team-targeting item: {selectedItem.name}");
            // Include all allies and highlight them all (similar to Fruit Juice)
            currentTargets = new List<CombatStats>(combatManager.players);
            
            Debug.Log($"[DEBUG TARGETING] Highlighting entire team for {selectedItem.name}");
            
            // Add detailed logging for each potential target
            for (int i = 0; i < currentTargets.Count; i++)
            {
                Debug.Log($"[DEBUG TARGETING] Team target {i}: {currentTargets[i].name}, isEnemy: {currentTargets[i].isEnemy}");
            }
            
            // Highlight all team members
            HighlightAllTeamMembers();
        }
        // Check if we're selecting for all-enemy items (Pocket Sand, Unstable Catalyst)
        else if (selectedItem != null && (
            string.Equals(selectedItem.name, "Pocket Sand", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selectedItem.name, "Unstable Catalyst", StringComparison.OrdinalIgnoreCase)))
        {
            Debug.Log($"[DEBUG TARGETING] Detected all-enemy targeting item: {selectedItem.name}");
            // Get all living enemies
            currentTargets = combatManager.GetLivingEnemies();
            
            Debug.Log($"[DEBUG TARGETING] Highlighting all enemies for {selectedItem.name}");
            
            // Add detailed logging for each potential target
            for (int i = 0; i < currentTargets.Count; i++)
            {
                Debug.Log($"[DEBUG TARGETING] Enemy target {i}: {currentTargets[i].name}, isEnemy: {currentTargets[i].isEnemy}");
            }
            
            // Highlight all enemies
            foreach (var enemy in currentTargets)
            {
                if (enemy != null)
                {
                    enemy.HighlightCharacter(true);
                    Debug.Log($"[DEBUG TARGETING] Highlighted enemy: {enemy.name}");
                }
            }
        }
        else
        {
            // Default behavior - target enemies
            Debug.Log($"[DEBUG TARGETING] No ally-targeting skill/item detected, defaulting to ENEMY targeting");
            if (selectedItem != null)
            {
                Debug.Log($"[DEBUG TARGETING] Selected item: {selectedItem.name} is targeting ENEMIES");
            }
            currentTargets = combatManager.GetLivingEnemies();
            Debug.Log($"[DEBUG TARGETING] Starting enemy selection with {currentTargets.Count} potential targets");
            
            // Add detailed logging for each potential target
            for (int i = 0; i < currentTargets.Count; i++)
            {
                Debug.Log($"[DEBUG TARGETING] Potential enemy target {i}: {currentTargets[i].name}, isEnemy: {currentTargets[i].isEnemy}");
            }
        }
        
        if (currentTargets.Count > 0)
        {
            isSelectingTarget = true;
            currentTargetSelection = 0;
            SetMenuItemsEnabled(false);
            
            // Only highlight the selected target if it's not a team-wide effect
            if (selectedItem == null || !string.Equals(selectedItem.name, "Fruit Juice", StringComparison.OrdinalIgnoreCase))
            {
                HighlightSelectedTarget();
                Debug.Log($"[Target Selection] Initial target selected: {currentTargets[currentTargetSelection].name}");
            }
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
            
            // Always unhighlight all targets, regardless of team effect
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
        
        if (selectedItem != null)
        {
            Debug.Log($"[Target Selection] Clearing selected item: {selectedItem.name}");
        }
        
        // Reset state variables
        isSelectingTarget = false;
        selectedSkill = null;
        selectedItem = null;
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
            UpdateCurrentSelectionDescription(); // Show description for initial selection
        }
        else
        {
            Debug.LogWarning("[SkillButton Lifecycle] No active skill buttons found!");
            cursor.gameObject.SetActive(false);
            combatUI.ClearDescription(); // Clear description if no buttons
        }
    }

    public void UpdateCurrentSelectionDescription()
    {
        // Check if we're using the cycling system
        if (cyclingCombatUI != null && cyclingAllSkills != null)
        {
            // Use cycling system to get the correct skill
            SkillData skill = cyclingCombatUI.GetSkillAtButtonIndex(currentSelection);
            if (skill != null)
            {
                combatUI.UpdateSkillDescription(skill);
                Debug.Log($"[Cycling Skill Menu] Updated description for skill: {skill.name} at button index {currentSelection}");
                return;
            }
            else
            {
                combatUI.ClearDescription();
                return;
            }
        }
        
        // Fallback to original system
        // Update description based on currently selected button
        if (skillOptions != null && currentSelection >= 0 && currentSelection < skillOptions.Length && skillOptions[currentSelection] != null)
        {
            // Check if it's a skill button
            var skillData = skillOptions[currentSelection].GetComponent<SkillButtonData>();
            if (skillData != null && skillData.skill != null)
            {
                combatUI.UpdateSkillDescription(skillData.skill);
                return;
            }
            
            // Check if it's an item button
            var itemData = skillOptions[currentSelection].GetComponent<ItemButtonData>();
            if (itemData != null && itemData.item != null)
            {
                combatUI.UpdateItemDescription(itemData.item);
                return;
            }
        }
        
        // Clear description if nothing valid is selected
        combatUI.ClearDescription();
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
        
        // Special handling for team-wide effects
        bool isTeamWideItem = selectedItem != null && (
            string.Equals(selectedItem.name, "Fruit Juice", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selectedItem.name, "Otherworldly Tome", StringComparison.OrdinalIgnoreCase));
            
        bool isAllEnemyItem = selectedItem != null && (
            string.Equals(selectedItem.name, "Pocket Sand", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selectedItem.name, "Unstable Catalyst", StringComparison.OrdinalIgnoreCase));
        
        bool isSingleEnemyItem = selectedItem != null && 
            string.Equals(selectedItem.name, "Shiny Bead", StringComparison.OrdinalIgnoreCase);
            
        if (isTeamWideItem)
        {
            // For team-wide effects, we don't unhighlight anyone
            Debug.Log($"[DEBUG TARGETING] Maintaining team-wide highlighting for {selectedItem.name}");
            return;
        }
        
        if (isAllEnemyItem)
        {
            // For all-enemy effects, we don't unhighlight any enemies
            Debug.Log($"[DEBUG TARGETING] Maintaining all-enemy highlighting for {selectedItem.name}");
            return;
        }
        
        // For enemy-targeting items like Shiny Bead, make sure we're highlighting a valid enemy
        if (isSingleEnemyItem && currentTargets.Count > 0)
        {
            Debug.Log($"[DEBUG TARGETING] Updating highlighted enemy for {selectedItem.name}");
            
            // Reset highlight for all targets
            foreach (var target in currentTargets)
            {
                if (target != null)
                {
                    target.HighlightCharacter(false);
                }
            }
            
            // Highlight the selected enemy
            if (currentTargets[currentTargetSelection] != null)
            {
                currentTargets[currentTargetSelection].HighlightCharacter(true);
                Debug.Log($"[DEBUG TARGETING] Highlighted enemy target: {currentTargets[currentTargetSelection].name}");
            }
            
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
    
    public bool IsInSkillOrItemMenu()
    {
        return isInSkillMenu;
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

    public void SetSelectedItem(ItemData item)
    {
        selectedItem = item;
        Debug.Log($"[DEBUG TARGETING] Selected item set: {item.name}, RequiresTarget: {item.requiresTarget}");
    }

    // Highlight all team members for team-wide effects
    private void HighlightAllTeamMembers()
    {
        Debug.Log($"[DEBUG TARGETING] Highlighting all team members for team effect");
        
        foreach (var target in currentTargets)
        {
            if (target != null)
            {
                // Ensure active character also gets highlighted for team effects
                if (target == combatManager.ActiveCharacter)
                {
                    Debug.Log($"[DEBUG TARGETING] Highlighting active character {target.name} for team effect");
                }
                
                target.HighlightCharacter(true);
                Debug.Log($"[DEBUG TARGETING] Highlighted team member: {target.name}");
            }
        }
    }
} 