using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.Linq;
using System.Collections;

public class CombatUI : MonoBehaviour
{
    public GameObject actionMenu;  // Keep this as it's the main menu
    
    [Header("Character UI")]
    public CharacterUIElements[] characterUI;

    [Header("Text Panel")]
    [Tooltip("Reference to the text panel that displays turn and action information")]
    public GameObject textPanel;
    [Tooltip("Reference to the TextMeshProUGUI component in the text panel")]
    public TextMeshProUGUI turnText;
    [Tooltip("How long to display action messages before executing the action")]
    [SerializeField] private float actionMessageDuration = 1f;
    private Coroutine currentTextCoroutine;

    [Header("Action Display")]
    [Tooltip("Reference to the label that displays player actions")]
    public GameObject actionDisplayLabel;
    [Tooltip("Reference to the TextMeshProUGUI component in the action display label")]
    public TextMeshProUGUI actionDisplayText;
    [Tooltip("How long to pause the game when displaying action")]
    [SerializeField] private float actionDisplayDuration = 1.0f;

    [Header("Skill UI")]
    public GameObject skillPanel; // Assign this in the Inspector
    public RectTransform skillButtonsContainer; // Assign this in the Inspector
    [Tooltip("Reference to a menu button to use as template for skill buttons")]
    public GameObject menuButtonTemplate; // Assign one of your menu buttons in the Inspector

    [Header("Skill Parameters")]
    [Tooltip("Damage dealt by the Fiend Fire skill per hit")]
    [SerializeField] private float fiendFireDamage = 10f;
    [Tooltip("Damage dealt by the Slam skill to all enemies")]
    [SerializeField] private float slamDamage = 10f;
    [Tooltip("Damage dealt by the Piercing Shot skill")]
    [SerializeField] private float piercingShotDamage = 10f;
    [Tooltip("Amount of health restored by Healing Words")]
    [SerializeField] private float healingWordsHealthAmount = 50f;
    [Tooltip("Amount of sanity restored by Healing Words")]
    [SerializeField] private float healingWordsSanityAmount = 30f;

    [Header("Visual Effects")]
    [Tooltip("Prefab for healing number popup")]
    [SerializeField] private GameObject healingPopupPrefab;
    [Tooltip("Prefab for damage number popup")]
    [SerializeField] private GameObject damagePopupPrefab;

    private GameObject characterStatsPanel;
    private GameObject skillMenu;
    private GameObject itemMenu;
    private GameObject buttonPrefab;
    private GridLayoutGroup skillButtonsGrid;
    private List<GameObject> currentSkillButtons = new List<GameObject>();
    private CombatManager combatManager;
    private MenuSelector menuSelector;

    private void Start()
    {
        Debug.Log("[SkillButton Lifecycle] CombatUI Start - Initializing UI components");
        combatManager = GetComponent<CombatManager>();
        menuSelector = GetComponent<MenuSelector>();
        
        // Find references by name instead of using GameObject.Find
        if (characterStatsPanel == null)
            characterStatsPanel = GameObject.Find("CharacterStatsPanel");
        
        // Initialize text panel if not assigned
        if (textPanel == null)
            textPanel = GameObject.Find("TextPanel");
            
        if (textPanel != null && turnText == null)
            turnText = textPanel.GetComponentInChildren<TextMeshProUGUI>();
            
        if (turnText == null)
            Debug.LogWarning("Turn text not found! Make sure TextPanel has a TextMeshProUGUI component.");
            
        // Hide the text panel at start - only show during specific turns
        if (textPanel != null)
            textPanel.SetActive(false);
            
        // Initialize action display label if not assigned
        if (actionDisplayLabel == null)
            actionDisplayLabel = GameObject.Find("ActionDisplayLabel");
            
        if (actionDisplayLabel != null && actionDisplayText == null)
            actionDisplayText = actionDisplayLabel.GetComponentInChildren<TextMeshProUGUI>();
            
        if (actionDisplayText == null)
            Debug.LogWarning("Action display text not found! Make sure ActionDisplayLabel has a TextMeshProUGUI component.");
            
        // Hide the action display label at start - only show during actions
        if (actionDisplayLabel != null)
            actionDisplayLabel.SetActive(false);
            
        // Don't try to find skillPanel by name if it's already assigned
        // Don't overwrite existing reference
        skillMenu = combatManager.skillMenu;
        itemMenu = combatManager.itemMenu;
        buttonPrefab = combatManager.buttonPrefab;
        
        // If menu button template is not assigned, try to use the first menu option from MenuSelector
        if (menuButtonTemplate == null && menuSelector != null && menuSelector.menuOptions.Length > 0)
        {
            menuButtonTemplate = menuSelector.menuOptions[0].gameObject;
            Debug.Log($"[SkillButton Lifecycle] Using first menu option as template: {menuButtonTemplate.name}");
        }
        
        // Add debug logging
        Debug.Log($"SkillPanel: {skillPanel != null}");
        Debug.Log($"SkillButtonsContainer: {skillButtonsContainer != null}");
        Debug.Log($"ButtonPrefab: {buttonPrefab != null}");
        Debug.Log($"MenuButtonTemplate: {menuButtonTemplate != null}");
        
        if (skillPanel != null && skillButtonsContainer == null)
        {
            // Try to find the container within the skill panel
            skillButtonsContainer = skillPanel.GetComponentInChildren<RectTransform>();
            if (skillButtonsContainer == skillPanel.GetComponent<RectTransform>())
            {
                // Create a child container if needed
                GameObject container = new GameObject("ButtonsContainer");
                skillButtonsContainer = container.AddComponent<RectTransform>();
                skillButtonsContainer.SetParent(skillPanel.transform, false);
                skillButtonsContainer.anchorMin = new Vector2(0, 0);
                skillButtonsContainer.anchorMax = new Vector2(1, 1);
                skillButtonsContainer.offsetMin = new Vector2(10, 10);
                skillButtonsContainer.offsetMax = new Vector2(-10, -10);
            }
            
            skillButtonsGrid = skillButtonsContainer.GetComponent<GridLayoutGroup>();
            if (skillButtonsGrid == null)
            {
                skillButtonsGrid = skillButtonsContainer.gameObject.AddComponent<GridLayoutGroup>();
                skillButtonsGrid.cellSize = new Vector2(120, 40);
                skillButtonsGrid.spacing = new Vector2(10, 10);
                skillButtonsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                skillButtonsGrid.constraintCount = 2;
            }
        }
        
        // Always show the menu
        actionMenu.SetActive(true);
        
        // Initialize all character UI elements
        foreach (var ui in characterUI)
        {
            ui.Initialize();
        }
        
        menuSelector.SetMenuItemsEnabled(false);
        UpdateUI();
    }

    public void UpdateUI()
    {
        for (int i = 0; i < combatManager.players.Count; i++)
        {
            if (i < characterUI.Length)
            {
                characterUI[i].UpdateUI(combatManager.players[i], combatManager.ActiveCharacter == combatManager.players[i]);
            }
        }
    }

    public void ShowActionMenu(CombatStats character)
    {
        // Hide other menus
        if (skillMenu != null) skillMenu.SetActive(false);
        if (itemMenu != null) itemMenu.SetActive(false);
        
        // Make sure the action menu is visible
        actionMenu.SetActive(true);
        
        // Ensure the menu is properly enabled
        menuSelector.SetMenuItemsEnabled(true);
        menuSelector.EnableMenu();
    }

    public void ShowSkillMenu()
    {
        Debug.Log("[SkillButton Lifecycle] ShowSkillMenu called - Beginning skill menu setup");
        if (skillPanel != null)
        {
            if (characterStatsPanel != null) characterStatsPanel.SetActive(false);
            actionMenu.SetActive(false);
            skillPanel.SetActive(true);
            
            // Hide the menu button template if it's assigned
            if (menuButtonTemplate != null)
            {
                // Store the original state to restore it later
                bool wasTemplateActive = menuButtonTemplate.activeSelf;
                menuButtonTemplate.SetActive(false);
                
                // We'll restore this when returning to the action menu
                menuButtonTemplate.tag = wasTemplateActive ? "ActiveTemplate" : "InactiveTemplate";
                Debug.Log($"[SkillButton Lifecycle] Hiding menu button template: {menuButtonTemplate.name}");
            }
            
            // First, create a local copy of the buttons to destroy
            List<GameObject> buttonsToDestroy = new List<GameObject>(currentSkillButtons);
            
            // Clear the list before destroying to prevent access to destroyed objects
            currentSkillButtons.Clear();
            
            // Now destroy the buttons
            if (buttonsToDestroy.Count > 0)
            {
                Debug.Log($"[SkillButton Lifecycle] Destroying {buttonsToDestroy.Count} existing skill buttons");
                foreach (var button in buttonsToDestroy)
                {
                    if (button != null)
                    {
                        Debug.Log($"[SkillButton Lifecycle] Destroying button: {(button.GetComponentInChildren<TextMeshProUGUI>()?.text ?? "unknown")}");
                        Destroy(button);
                    }
                }
            }
            
            // Debug to check container status
            Debug.Log($"SkillButtonsContainer: {skillButtonsContainer != null}");
            
            // Create buttons for each skill
            var activeCharStats = combatManager.ActiveCharacter;
            if (activeCharStats != null)
            {
                // Check if the container is a prefab asset
                bool isPrefabAsset = false;
                #if UNITY_EDITOR
                isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(skillButtonsContainer);
                #endif
                
                // Create a runtime container if needed
                RectTransform runtimeContainer;
                if (isPrefabAsset || skillButtonsContainer == null)
                {
                    // Create a new container at runtime
                    GameObject containerObj = new GameObject("RuntimeSkillButtonsContainer");
                    runtimeContainer = containerObj.AddComponent<RectTransform>();
                    runtimeContainer.SetParent(skillPanel.transform, false);
                    
                    // Set up the container's layout
                    runtimeContainer.anchorMin = new Vector2(0, 0);
                    runtimeContainer.anchorMax = new Vector2(1, 1);
                    runtimeContainer.offsetMin = new Vector2(10, 10);
                    runtimeContainer.offsetMax = new Vector2(-10, -10);
                    
                    // Add a grid layout group
                    GridLayoutGroup gridLayout = containerObj.AddComponent<GridLayoutGroup>();
                    gridLayout.cellSize = new Vector2(120, 40);
                    gridLayout.spacing = new Vector2(10, 10);
                    gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    gridLayout.constraintCount = 2;
                    
                    // If we already have a container in the scene, destroy it to avoid duplicates
                    if (skillButtonsContainer != null && skillButtonsContainer.gameObject != null && 
                        !isPrefabAsset && skillButtonsContainer.gameObject.scene.IsValid())
                    {
                        Destroy(skillButtonsContainer.gameObject);
                    }
                    
                    // Update the reference
                    skillButtonsContainer = runtimeContainer;
                }
                else
                {
                    // Use the existing container
                    runtimeContainer = skillButtonsContainer;
                    
                    // Make sure it has a grid layout
                    GridLayoutGroup gridLayout = runtimeContainer.GetComponent<GridLayoutGroup>();
                    if (gridLayout == null)
                    {
                        gridLayout = runtimeContainer.gameObject.AddComponent<GridLayoutGroup>();
                        gridLayout.cellSize = new Vector2(120, 40);
                        gridLayout.spacing = new Vector2(10, 10);
                        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                        gridLayout.constraintCount = 2;
                    }
                }
                
                // Now create buttons for each skill using the runtime container
                Debug.Log($"[SkillButton Lifecycle] Creating skill buttons for character: {activeCharStats.name}");
                
                // Debug log to show all skills on the active character
                Debug.Log($"[SkillButton Lifecycle] Active character has {activeCharStats.skills.Count} skills: {string.Join(", ", activeCharStats.skills.Select(s => s.name))}");
                
                // Create a HashSet to track skills we've already created buttons for
                HashSet<string> processedSkills = new HashSet<string>();
                
                foreach (var skill in activeCharStats.skills)
                {
                    // Skip if we've already processed this skill
                    if (processedSkills.Contains(skill.name))
                    {
                        Debug.Log($"[SkillButton Lifecycle] Skipping duplicate skill: {skill.name}");
                        continue;
                    }
                    
                    // Add to processed skills
                    processedSkills.Add(skill.name);
                    
                    // Skip if this is the template button's skill (safety check)
                    if (menuButtonTemplate != null && 
                        menuButtonTemplate.GetComponent<SkillButtonData>() != null && 
                        menuButtonTemplate.GetComponent<SkillButtonData>().skill == skill)
                    {
                        Debug.Log($"[SkillButton Lifecycle] Skipping template button's skill: {skill.name}");
                        continue;
                    }
                    
                    Debug.Log($"[SkillButton Lifecycle] Creating button for skill: {skill.name}");
                    
                    // Use menu button template if available, otherwise fall back to buttonPrefab
                    GameObject skillButton;
                    if (menuButtonTemplate != null)
                    {
                        // Instantiate from menu button template
                        skillButton = Instantiate(menuButtonTemplate);
                        Debug.Log($"[SkillButton Lifecycle] Using menu button template: {menuButtonTemplate.name}");
                        
                        // Remove any existing components that might interfere
                        Button existingButton = skillButton.GetComponent<Button>();
                        if (existingButton != null)
                        {
                            Destroy(existingButton);
                        }
                        
                        // Remove any existing SkillButtonData component
                        SkillButtonData existingSkillData = skillButton.GetComponent<SkillButtonData>();
                        if (existingSkillData != null)
                        {
                            Destroy(existingSkillData);
                        }
                    }
                    else
                    {
                        // Fall back to the original button prefab
                        skillButton = Instantiate(buttonPrefab);
                        Debug.Log($"[SkillButton Lifecycle] Using fallback button prefab");
                    }
                    
                    // Give the button a meaningful name
                    skillButton.name = $"SkillButton_{skill.name}";
                    
                    // Then set the parent to our runtime container
                    skillButton.transform.SetParent(runtimeContainer, false);
                    
                    // Ensure the button is active
                    skillButton.SetActive(true);
                    
                    // Ensure the button has the correct size
                    RectTransform buttonRect = skillButton.GetComponent<RectTransform>();
                    if (buttonRect != null)
                    {
                        // Use the original size from the template
                        if (menuButtonTemplate != null)
                        {
                            RectTransform templateRect = menuButtonTemplate.GetComponent<RectTransform>();
                            if (templateRect != null)
                            {
                                buttonRect.sizeDelta = templateRect.sizeDelta;
                                
                                // Copy anchoring settings
                                buttonRect.anchorMin = templateRect.anchorMin;
                                buttonRect.anchorMax = templateRect.anchorMax;
                                buttonRect.pivot = templateRect.pivot;
                                
                                Debug.Log($"[SkillButton Lifecycle] Copied RectTransform properties from template - Size: {buttonRect.sizeDelta}, Anchors: {buttonRect.anchorMin}-{buttonRect.anchorMax}");
                            }
                        }
                    }
                    
                    // Copy visual components from template
                    if (menuButtonTemplate != null)
                    {
                        // Copy Image component settings
                        Image templateImage = menuButtonTemplate.GetComponent<Image>();
                        Image buttonImage = skillButton.GetComponent<Image>();
                        if (templateImage != null && buttonImage != null)
                        {
                            buttonImage.sprite = templateImage.sprite;
                            buttonImage.type = templateImage.type;
                            buttonImage.fillMethod = templateImage.fillMethod;
                            buttonImage.color = templateImage.color;
                            Debug.Log($"[SkillButton Lifecycle] Copied Image properties from template");
                        }
                        
                        // Copy CanvasGroup settings if present
                        CanvasGroup templateCanvasGroup = menuButtonTemplate.GetComponent<CanvasGroup>();
                        if (templateCanvasGroup != null)
                        {
                            CanvasGroup buttonCanvasGroup = skillButton.GetComponent<CanvasGroup>();
                            if (buttonCanvasGroup == null)
                            {
                                buttonCanvasGroup = skillButton.AddComponent<CanvasGroup>();
                            }
                            buttonCanvasGroup.alpha = 1f; // Always make visible
                            buttonCanvasGroup.interactable = true;
                            buttonCanvasGroup.blocksRaycasts = true;
                        }
                    }
                    
                    // Find or create TextMeshProUGUI component
                    TextMeshProUGUI buttonText = skillButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        // Store original font and style settings
                        TMP_FontAsset originalFont = buttonText.font;
                        float originalFontSize = buttonText.fontSize;
                        Color originalColor = buttonText.color;
                        TextAlignmentOptions originalAlignment = buttonText.alignment;
                        
                        // Set the text
                        buttonText.text = skill.name;
                        
                        // Restore original style settings
                        buttonText.font = originalFont;
                        buttonText.fontSize = originalFontSize;
                        buttonText.color = originalColor;
                        buttonText.alignment = originalAlignment;
                        
                        // Force update to ensure text is visible
                        buttonText.ForceMeshUpdate();
                        
                        Debug.Log($"[SkillButton Lifecycle] Skill button created and configured - Text: '{skill.name}', " +
                                 $"Font: {buttonText.font?.name ?? "null"}, " +
                                 $"Size: {buttonText.fontSize}, " +
                                 $"Color: {buttonText.color}, " +
                                 $"Position: {skillButton.transform.position}, " +
                                 $"Size: {skillButton.GetComponent<RectTransform>().sizeDelta}");
                        
                        // Find the cost text component (should be a separate TextMeshProUGUI in the prefab)
                        TextMeshProUGUI costText = null;
                        
                        // Look for a child object with "Cost" in its name
                        foreach (Transform child in skillButton.transform)
                        {
                            if (child.name.Contains("Cost"))
                            {
                                costText = child.GetComponent<TextMeshProUGUI>();
                                break;
                            }
                        }
                        
                        // If we didn't find it by name, look for a second TextMeshProUGUI component
                        if (costText == null)
                        {
                            TextMeshProUGUI[] allTexts = skillButton.GetComponentsInChildren<TextMeshProUGUI>();
                            if (allTexts.Length > 1)
                            {
                                // Use the second text component as the cost text
                                costText = allTexts[1];
                            }
                        }
                        
                        // If we found the cost text component, update it
                        if (costText != null)
                        {
                            costText.text = $"{skill.sanityCost} SP";
                            costText.ForceMeshUpdate();
                            Debug.Log($"[SkillButton Lifecycle] Updated cost text: '{costText.text}'");
                        }
                        else
                        {
                            Debug.LogWarning("[SkillButton Lifecycle] Cost text component not found on skill button!");
                        }
                    }
                    else
                    {
                        Debug.LogError("[SkillButton Lifecycle] Button text not found on skill button!");
                        
                        // Try to find the text in a deeper hierarchy
                        buttonText = skillButton.GetComponentInChildren<TextMeshProUGUI>(true);
                        if (buttonText != null)
                        {
                            Debug.Log("[SkillButton Lifecycle] Found text component in deeper hierarchy, enabling it");
                            buttonText.gameObject.SetActive(true);
                            buttonText.text = skill.name;
                            buttonText.ForceMeshUpdate();
                        }
                        else
                        {
                            // Create a new text component if none exists
                            Debug.Log("[SkillButton Lifecycle] Creating new TextMeshProUGUI component");
                            GameObject textObj = new GameObject("ButtonText");
                            textObj.transform.SetParent(skillButton.transform, false);
                            buttonText = textObj.AddComponent<TextMeshProUGUI>();
                            buttonText.text = skill.name;
                            buttonText.alignment = TextAlignmentOptions.Center;
                            buttonText.fontSize = 14;
                            buttonText.color = Color.white;
                            
                            // Set up RectTransform for the text
                            RectTransform textRect = textObj.GetComponent<RectTransform>();
                            textRect.anchorMin = new Vector2(0, 0);
                            textRect.anchorMax = new Vector2(1, 1);
                            textRect.offsetMin = Vector2.zero;
                            textRect.offsetMax = Vector2.zero;
                        }
                    }
                    
                    // Store the skill data directly on the GameObject
                    SkillButtonData skillData = skillButton.AddComponent<SkillButtonData>();
                    skillData.skill = skill;
                    Debug.Log($"[SkillButton Lifecycle] SkillButtonData component added - Skill: {skill.name}, SanityCost: {skill.sanityCost}, RequiresTarget: {skill.requiresTarget}");
                    
                    currentSkillButtons.Add(skillButton);
                }
                
                Debug.Log($"[SkillButton Lifecycle] Created {currentSkillButtons.Count} skill buttons, updating MenuSelector");
                menuSelector.UpdateSkillMenuOptions(currentSkillButtons.ToArray());
            }
        }
    }

    private void OnSkillButtonClicked(SkillData skill)
    {
        Debug.Log($"[SkillButton Lifecycle] Skill button clicked - Skill: {skill.name}");
        if (skill.requiresTarget)
        {
            Debug.Log($"[SkillButton Lifecycle] Skill requires target, starting target selection");
            menuSelector.StartTargetSelection();
            // Store the selected skill for when target is selected
            menuSelector.SetSelectedSkill(skill);
        }
        else
        {
            Debug.Log($"[SkillButton Lifecycle] Skill does not require target, executing immediately");
            ExecuteSkill(skill, null);
        }
    }

    public void ExecuteSkill(SkillData skill, CombatStats target)
    {
        Debug.Log($"[SkillButton Lifecycle] Executing skill: {skill.name}, Target: {target?.name ?? "none"}");
        var activeCharacter = combatManager.ActiveCharacter;
        if (activeCharacter == null || activeCharacter.currentSanity < skill.sanityCost) 
        {
            Debug.Log($"[SkillButton Lifecycle] Cannot execute skill - ActiveCharacter: {activeCharacter != null}, CurrentSanity: {activeCharacter?.currentSanity ?? 0}, Required: {skill.sanityCost}");
            return;
        }

        // Hide the text panel when player selects an action
        HideTextPanel();
        
        // Display just the skill name in the action display label
        DisplayActionLabel(skill.name);
        
        // Execute skill after action label is shown
        StartCoroutine(ExecuteSkillAfterMessage(skill, target, activeCharacter));
    }
    
    private IEnumerator ExecuteSkillAfterMessage(SkillData skill, CombatStats target, CombatStats activeCharacter)
    {
        // Wait for the action display duration before executing skill
        yield return new WaitForSeconds(actionDisplayDuration);
        
        // Hide the action display label after waiting
        if (actionDisplayLabel != null)
        {
            actionDisplayLabel.SetActive(false);
        }

        // Execute the skill based on name
        switch (skill.name)
        {
            case "Before Your Eyes":
                // Check if target is a valid enemy
                if (target != null && target.isEnemy)
                {
                    Debug.Log($"[Skill Execution] Before Your Eyes! {activeCharacter.name} is using the skill on {target.name}");
                    
                    // Reset target's action gauge to 0
                    target.ResetAction();
                    
                    // Use sanity
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] Before Your Eyes! Invalid target: {target?.name ?? "null"}");
                }
                break;
                
            case "Fiend Fire":
                // Check if target is a valid enemy
                if (target != null && target.isEnemy)
                {
                    Debug.Log($"[Skill Execution] Fiend Fire! {activeCharacter.name} is attacking {target.name}");
                    
                    // Calculate random number of hits (1-5)
                    int hits = UnityEngine.Random.Range(1, 6); // 1 to 5 hits
                    
                    // Calculate total damage
                    float totalDamage = fiendFireDamage * hits;
                    
                    // Apply damage to the enemy
                    target.TakeDamage(totalDamage);
                    
                    // Show the number of hits in the text panel
                    DisplayTurnAndActionMessage($"Hit {hits} times for a total of {totalDamage} damage!");
                    
                    // Use sanity
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] Fiend Fire! Invalid target: {target?.name ?? "null"}");
                }
                break;
                
            case "Slam!":
                Debug.Log($"[Skill Execution] Slam! {activeCharacter.name} is using area attack");
                
                // Get all living enemies
                var enemies = combatManager.GetLivingEnemies();
                
                // Apply random damage to each enemy
                foreach (var enemy in enemies)
                {
                    // Calculate random damage between 15-30
                    float baseDamage = UnityEngine.Random.Range(15f, 30.1f); // 30.1 to include 30 in the range
                    float calculatedDamage = activeCharacter.CalculateDamage(baseDamage);
                    enemy.TakeDamage(calculatedDamage);
                    Debug.Log($"[Skill Execution] Slam! hit {enemy.name} for {calculatedDamage} damage");
                }
                
                // Apply Strength status to the user
                StatusManager statusManager = StatusManager.Instance;
                if (statusManager != null)
                {
                    statusManager.ApplyStatus(activeCharacter, StatusType.Strength, 2);
                    Debug.Log($"[Skill Execution] Slam! {activeCharacter.name} gains Strength status for 2 turns");
                }
                
                // Use sanity
                activeCharacter.UseSanity(skill.sanityCost);
                break;
                
            case "Human Shield!":
                // Check if target is a valid ally (not an enemy and not self)
                if (target != null && !target.isEnemy && target != activeCharacter)
                {
                    Debug.Log($"[Skill Execution] Human Shield! {activeCharacter.name} is protecting {target.name}");
                    
                    // First, stop any existing guarding
                    activeCharacter.StopGuarding();
                    
                    // Set up the guarding relationship
                    activeCharacter.GuardAlly(target);
                    
                    // Use sanity (0 in this case, but keeping the code for consistency)
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] Human Shield! Invalid target: {target?.name ?? "null"}");
                }
                break;
                
            case "Healing Words":
                // Check if target is valid (can heal allies or self)
                if (target != null && !target.isEnemy)
                {
                    Debug.Log($"[Skill Execution] Healing Words! {activeCharacter.name} is healing {target.name}");
                    
                    // Heal the target for the configured amounts
                    target.HealHealth(healingWordsHealthAmount);
                    target.HealSanity(healingWordsSanityAmount);
                    
                    Debug.Log($"[Skill Execution] Healing Words healed {target.name} for {healingWordsHealthAmount} HP and {healingWordsSanityAmount} sanity");
                    
                    // Use sanity
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] Healing Words! Invalid target: {target?.name ?? "null"}");
                }
                break;
                
            case "Piercing Shot":
                // Check if target is a valid enemy
                if (target != null && target.isEnemy)
                {
                    Debug.Log($"[Skill Execution] Piercing Shot! {activeCharacter.name} is shooting {target.name}");
                    
                    // Deal damage using the configurable amount
                    target.TakeDamage(piercingShotDamage);
                    
                    // Apply defense reduction for 2 turns
                    if (!target.IsDead())
                    {
                        // Apply defense reduction effect using the proper method
                        target.ApplyDefenseReduction();
                        
                        Debug.Log($"[Skill Execution] Piercing Shot reduced {target.name}'s defense by 50% for 2 turns");
                    }
                    
                    // Use sanity (0 in this case, but keeping the code for consistency)
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] Piercing Shot! Invalid target: {target?.name ?? "null"}");
                }
                break;
                
            case "Disappearing Trick":
                Debug.Log($"[Skill Execution] Disappearing Trick! {activeCharacter.name} is removing status effects from allies");
                
                // Get all players
                var allPlayers = combatManager.players;
                
                // Get StatusManager instance
                StatusManager statusMgr = StatusManager.Instance;
                if (statusMgr != null)
                {
                    int clearedCount = 0;
                    
                    // Loop through all players
                    foreach (CombatStats player in allPlayers)
                    {
                        // Skip self (the caster)
                        if (player == activeCharacter) continue;
                        
                        if (player != null && !player.IsDead())
                        {
                            // Clear all status effects from this ally
                            statusMgr.ClearAllStatuses(player);
                            clearedCount++;
                            
                            Debug.Log($"[Skill Execution] Disappearing Trick cleared all status effects from {player.characterName}");
                        }
                    }
                    
                    if (clearedCount > 0)
                    {
                        DisplayTurnAndActionMessage($"Cleared status effects from {clearedCount} allies!");
                    }
                    else
                    {
                        DisplayTurnAndActionMessage("No allies had status effects to clear!");
                    }
                }
                else
                {
                    Debug.LogWarning("[Skill Execution] Disappearing Trick! StatusManager not found. Skill had no effect.");
                }
                
                // Use sanity
                activeCharacter.UseSanity(skill.sanityCost);
                break;
                
            case "Take a Break!":
                // Check if target is a valid ally (not an enemy)
                if (target != null && !target.isEnemy)
                {
                    Debug.Log($"[Skill Execution] Take a Break! {activeCharacter.name} is helping {target.name} rest");
                    
                    // Heal the target for 20 HP and 20 Mind
                    target.HealHealth(20f);
                    target.HealSanity(20f);
                    
                    // Apply SLOW status to target
                    StatusManager slowStatusMgr = StatusManager.Instance;
                    if (slowStatusMgr != null)
                    {
                        // Apply Slowed status with the status system
                        slowStatusMgr.ApplyStatus(target, StatusType.Slowed, 2);
                        Debug.Log($"[Skill Execution] Take a Break! {target.characterName} healed for 20 HP and 20 Mind, and is now SLOW for 2 turns");
                    }
                    else
                    {
                        // Fallback to direct modification if status manager not available
                        float baseActionSpeed = target.actionSpeed;
                        float newSpeed = baseActionSpeed * 0.5f; // 50% reduction
                        target.actionSpeed = newSpeed;
                        Debug.LogWarning($"[Skill Execution] Take a Break! StatusManager not found, applied direct speed reduction to {target.characterName}");
                    }
                    
                    // Use sanity
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] Take a Break! Invalid target: {target?.name ?? "null"}. This skill requires an ally target.");
                }
                break;

            case "What Doesn't Kill You":
                // Check if target is a valid ally (not an enemy)
                if (target != null && !target.isEnemy)
                {
                    Debug.Log($"[Skill Execution] What Doesn't Kill You! {activeCharacter.name} is using on ally {target.name}");
                    
                    // Deal damage to the ally
                    target.TakeDamage(10f);
                    
                    // Apply STRENGTH status to the target
                    StatusManager wdkyStatusMgr = StatusManager.Instance;
                    if (wdkyStatusMgr != null)
                    {
                        // Apply Strength status with the status system
                        wdkyStatusMgr.ApplyStatus(target, StatusType.Strength, 2);
                        Debug.Log($"[Skill Execution] What Doesn't Kill You! {target.characterName} took 10 damage and gained Strength for 2 turns");
                        
                        // Show message in the text panel
                        DisplayTurnAndActionMessage($"{target.characterName} gained STRENGTH but took 10 damage!");
                    }
                    else
                    {
                        // Fallback to direct modification if status manager not available
                        target.attackMultiplier = 1.5f; // 50% increase
                        Debug.LogWarning($"[Skill Execution] What Doesn't Kill You! StatusManager not found, applied direct attack multiplier to {target.characterName}");
                    }
                    
                    // Use sanity
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] What Doesn't Kill You! Invalid target: {target?.name ?? "null"}. This skill requires an ally target.");
                }
                break;

            case "Fortify":
                Debug.Log($"[Skill Execution] Fortify! {activeCharacter.name} is fortifying themselves");
                
                // Heal the user for 10 HP
                activeCharacter.HealHealth(10f);
                
                // Apply TOUGH status to the user
                StatusManager fortifyStatusMgr = StatusManager.Instance;
                if (fortifyStatusMgr != null)
                {
                    // Apply Tough status with the status system
                    fortifyStatusMgr.ApplyStatus(activeCharacter, StatusType.Tough, 2);
                    Debug.Log($"[Skill Execution] Fortify! {activeCharacter.characterName} healed for 10 HP and gained Tough status for 2 turns");
                }
                else
                {
                    // Fallback to direct modification if status manager not available
                    activeCharacter.defenseMultiplier = 0.5f; // 50% damage reduction
                    Debug.LogWarning($"[Skill Execution] Fortify! StatusManager not found, applied direct defense multiplier to {activeCharacter.characterName}");
                }
                
                // Use sanity
                activeCharacter.UseSanity(skill.sanityCost);
                break;
        }
        
        // Make sure we're not in target selection mode before ending the turn
        if (menuSelector.IsSelectingTarget())
        {
            menuSelector.CancelTargetSelection();
        }
        
        BackToActionMenu();
        combatManager.EndPlayerTurn();
    }

    public void BackToActionMenu()
    {
        Debug.Log("[SkillButton Lifecycle] Returning to action menu, hiding skill panel");
        if (skillPanel != null) skillPanel.SetActive(false);
        if (characterStatsPanel != null) characterStatsPanel.SetActive(true);
        actionMenu.SetActive(true);
        
        // Restore the menu button template's original state
        if (menuButtonTemplate != null)
        {
            bool shouldBeActive = menuButtonTemplate.CompareTag("ActiveTemplate");
            menuButtonTemplate.SetActive(shouldBeActive);
            // Reset the tag
            menuButtonTemplate.tag = "Untagged";
            Debug.Log($"[SkillButton Lifecycle] Restoring menu button template: {menuButtonTemplate.name}, Active: {shouldBeActive}");
        }
        
        // First, create a local copy of the buttons to destroy
        List<GameObject> buttonsToDestroy = new List<GameObject>(currentSkillButtons);
        
        // Clear the list before destroying to prevent access to destroyed objects
        currentSkillButtons.Clear();
        
        // Now destroy the buttons
        if (buttonsToDestroy.Count > 0)
        {
            Debug.Log($"[SkillButton Lifecycle] Cleaning up {buttonsToDestroy.Count} skill buttons when returning to action menu");
            foreach (var button in buttonsToDestroy)
            {
                if (button != null)
                {
                    Debug.Log($"[SkillButton Lifecycle] Destroying button: {(button.GetComponentInChildren<TextMeshProUGUI>()?.text ?? "unknown")}");
                    Destroy(button);
                }
            }
        }
        
        // Reset the menu state
        menuSelector.EnableMenu();
    }

    public void ShowItemMenu()
    {
        Debug.Log("[ItemButton Lifecycle] ShowItemMenu called - Beginning item menu setup");
        
        // Get the active character's items or the party's shared inventory
        var activeCharStats = combatManager.ActiveCharacter;
        List<ItemData> partyItems = new List<ItemData>();
        foreach (var player in combatManager.players)
        {
            if (player != null && !player.IsDead())
            {
                partyItems.AddRange(player.items);
            }
        }
        
        // Check if there are any items before proceeding
        if (partyItems.Count == 0)
        {
            Debug.Log("[ItemButton Lifecycle] No items in inventory, showing message and returning to action menu");
            // Display a message to the player
            DisplayTurnAndActionMessage("No items in inventory!");
            // Stay in the action menu
            return;
        }
        
        // Set the item menu active flag in the combat manager
        combatManager.isItemMenuActive = true;
        
        if (itemMenu != null)
        {
            if (characterStatsPanel != null) characterStatsPanel.SetActive(false);
            actionMenu.SetActive(false);
            // Hide skill menu if it's active
            if (skillMenu != null) skillMenu.SetActive(false);
            itemMenu.SetActive(true);
            
            // Hide the menu button template if it's assigned
            if (menuButtonTemplate != null)
            {
                // Store the original state to restore it later
                bool wasTemplateActive = menuButtonTemplate.activeSelf;
                menuButtonTemplate.SetActive(false);
                
                // We'll restore this when returning to the action menu
                menuButtonTemplate.tag = wasTemplateActive ? "ActiveTemplate" : "InactiveTemplate";
                Debug.Log($"[ItemButton Lifecycle] Hiding menu button template: {menuButtonTemplate.name}");
            }
            
            // First, create a local copy of the buttons to destroy
            List<GameObject> buttonsToDestroy = new List<GameObject>(currentSkillButtons);
            
            // Clear the list before destroying to prevent access to destroyed objects
            currentSkillButtons.Clear();
            
            // Now destroy the buttons
            if (buttonsToDestroy.Count > 0)
            {
                Debug.Log($"[ItemButton Lifecycle] Destroying {buttonsToDestroy.Count} existing buttons");
                foreach (var button in buttonsToDestroy)
                {
                    if (button != null)
                    {
                        Debug.Log($"[ItemButton Lifecycle] Destroying button: {(button.GetComponentInChildren<TextMeshProUGUI>()?.text ?? "unknown")}");
                        Destroy(button);
                    }
                }
            }
            
            // Find the container for the item buttons - look for a direct child of itemMenu
            Transform containerTransform = itemMenu.transform.Find("ItemButtonsContainer");
            RectTransform itemButtonsContainer;
            
            if (containerTransform != null)
            {
                // Use the existing container
                itemButtonsContainer = containerTransform.GetComponent<RectTransform>();
                Debug.Log("[ItemButton Lifecycle] Found existing ItemButtonsContainer");
                
                // Check for and destroy any existing RuntimeItemButtons container
                Transform existingRuntimeContainer = containerTransform.Find("RuntimeItemButtons");
                if (existingRuntimeContainer != null)
                {
                    Debug.Log("[ItemButton Lifecycle] Destroying existing RuntimeItemButtons container");
                    Destroy(existingRuntimeContainer.gameObject);
                }
            }
            else
            {
                // If we didn't find a direct child container, check if there are any children at all
                if (itemMenu.transform.childCount > 0)
                {
                    // Use the first child as the container
                    itemButtonsContainer = itemMenu.transform.GetChild(0).GetComponent<RectTransform>();
                    Debug.Log($"[ItemButton Lifecycle] Using first child as container: {itemButtonsContainer.name}");
                    
                    // Check for and destroy any existing RuntimeItemButtons container
                    Transform existingRuntimeContainer = itemButtonsContainer.Find("RuntimeItemButtons");
                    if (existingRuntimeContainer != null)
                    {
                        Debug.Log("[ItemButton Lifecycle] Destroying existing RuntimeItemButtons container in first child");
                        Destroy(existingRuntimeContainer.gameObject);
                    }
                }
                else
                {
                    // Create a child container if needed
                    GameObject container = new GameObject("ItemButtonsContainer");
                    itemButtonsContainer = container.AddComponent<RectTransform>();
                    itemButtonsContainer.SetParent(itemMenu.transform, false);
                    itemButtonsContainer.anchorMin = new Vector2(0, 0);
                    itemButtonsContainer.anchorMax = new Vector2(1, 1);
                    itemButtonsContainer.offsetMin = new Vector2(10, 10);
                    itemButtonsContainer.offsetMax = new Vector2(-10, -10);
                    Debug.Log("[ItemButton Lifecycle] Created new ItemButtonsContainer");
                }
            }
            
            // Ensure the container has a grid layout
            GridLayoutGroup itemButtonsGrid = itemButtonsContainer.GetComponent<GridLayoutGroup>();
            if (itemButtonsGrid == null)
            {
                itemButtonsGrid = itemButtonsContainer.gameObject.AddComponent<GridLayoutGroup>();
                itemButtonsGrid.cellSize = new Vector2(120, 40);
                itemButtonsGrid.spacing = new Vector2(10, 10);
                itemButtonsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                itemButtonsGrid.constraintCount = 2;
            }
            
            // Create a runtime container for the buttons
            GameObject runtimeContainer = new GameObject("RuntimeItemButtons");
            RectTransform runtimeContainerRect = runtimeContainer.AddComponent<RectTransform>();
            runtimeContainerRect.SetParent(itemButtonsContainer, false);
            runtimeContainerRect.anchorMin = new Vector2(0, 0);
            runtimeContainerRect.anchorMax = new Vector2(1, 1);
            runtimeContainerRect.offsetMin = Vector2.zero;
            runtimeContainerRect.offsetMax = Vector2.zero;
            
            // Add a grid layout to the runtime container
            GridLayoutGroup runtimeGrid = runtimeContainer.AddComponent<GridLayoutGroup>();
            runtimeGrid.cellSize = itemButtonsGrid.cellSize;
            runtimeGrid.spacing = itemButtonsGrid.spacing;
            runtimeGrid.constraint = itemButtonsGrid.constraint;
            runtimeGrid.constraintCount = itemButtonsGrid.constraintCount;
            
            // Create buttons for each item
            if (activeCharStats != null)
            {
                // Track processed items to avoid duplicates
                HashSet<string> processedItems = new HashSet<string>();
                
                // Create a button for each unique item
                foreach (var item in partyItems)
                {
                    // Skip if we've already processed this item
                    if (processedItems.Contains(item.name))
                    {
                        Debug.Log($"[ItemButton Lifecycle] Skipping duplicate item: {item.name}");
                        continue;
                    }
                    
                    // Add to processed items
                    processedItems.Add(item.name);
                    
                    Debug.Log($"[ItemButton Lifecycle] Creating button for item: {item.name}");
                    
                    // Use menu button template if available, otherwise fall back to buttonPrefab
                    GameObject itemButton;
                    if (menuButtonTemplate != null)
                    {
                        // Instantiate from menu button template
                        itemButton = Instantiate(menuButtonTemplate);
                        Debug.Log($"[ItemButton Lifecycle] Using menu button template: {menuButtonTemplate.name}");
                        
                        // Remove any existing components that might interfere
                        Button existingButton = itemButton.GetComponent<Button>();
                        if (existingButton != null)
                        {
                            Destroy(existingButton);
                        }
                        
                        // Remove any existing SkillButtonData component
                        SkillButtonData existingSkillData = itemButton.GetComponent<SkillButtonData>();
                        if (existingSkillData != null)
                        {
                            Destroy(existingSkillData);
                        }
                        
                        // Remove any existing ItemButtonData component
                        ItemButtonData existingItemData = itemButton.GetComponent<ItemButtonData>();
                        if (existingItemData != null)
                        {
                            Destroy(existingItemData);
                        }
                    }
                    else
                    {
                        // Fall back to the original button prefab
                        itemButton = Instantiate(buttonPrefab);
                        Debug.Log($"[ItemButton Lifecycle] Using fallback button prefab");
                    }
                    
                    // Give the button a meaningful name
                    itemButton.name = $"ItemButton_{item.name}";
                    
                    // Then set the parent to our runtime container
                    itemButton.transform.SetParent(runtimeContainer.transform, false);
                    
                    // Ensure the button is active
                    itemButton.SetActive(true);
                    
                    // Ensure the button has the correct size
                    RectTransform buttonRect = itemButton.GetComponent<RectTransform>();
                    if (buttonRect != null)
                    {
                        buttonRect.anchorMin = new Vector2(0, 0);
                        buttonRect.anchorMax = new Vector2(1, 1);
                        buttonRect.sizeDelta = Vector2.zero;
                    }
                    
                    // Add a button component for click handling
                    Button button = itemButton.GetComponent<Button>();
                    if (button == null)
                    {
                        button = itemButton.AddComponent<Button>();
                    }
                    
                    // Store the item data for reference
                    ItemButtonData itemData = itemButton.AddComponent<ItemButtonData>();
                    itemData.item = item;
                    
                    // Set up the button click handler
                    ItemData capturedItem = item; // Capture for lambda
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnItemButtonClicked(capturedItem));
                    
                    // Add a debug click handler to verify the button is working
                    button.onClick.AddListener(() => Debug.Log($"[ItemButton Lifecycle] Button clicked for item: {capturedItem.name}"));
                    
                    // Make sure the button is interactable
                    button.interactable = true;
                    
                    // Find the Name and Cost text components
                    Transform nameTransform = itemButton.transform.Find("Name");
                    Transform costTransform = itemButton.transform.Find("Cost");
                    
                    if (nameTransform != null && costTransform != null)
                    {
                        TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                        TextMeshProUGUI costText = costTransform.GetComponent<TextMeshProUGUI>();
                        
                        if (nameText != null && costText != null)
                        {
                            // Set the name and amount
                            nameText.text = item.name;
                            costText.text = item.amount.ToString();
                            
                            Debug.Log($"[ItemButton Lifecycle] Set text - Name: {item.name}, Amount: {item.amount}");
                        }
                        else
                        {
                            Debug.LogError("[ItemButton Lifecycle] TextMeshProUGUI components not found on Name or Cost objects");
                        }
                    }
                    else
                    {
                        Debug.LogError("[ItemButton Lifecycle] Name or Cost objects not found in button hierarchy");
                        
                        // Try to find any TextMeshProUGUI component
                        TextMeshProUGUI buttonText = itemButton.GetComponentInChildren<TextMeshProUGUI>();
                        if (buttonText != null)
                        {
                            buttonText.text = $"{item.name} x{item.amount}";
                            Debug.Log($"[ItemButton Lifecycle] Set fallback text: {item.name} x{item.amount}");
                        }
                        else
                        {
                            Debug.LogError("[ItemButton Lifecycle] Button text not found on item button!");
                        }
                    }
                    
                    currentSkillButtons.Add(itemButton);
                }
                
                Debug.Log($"[ItemButton Lifecycle] Created {currentSkillButtons.Count} item buttons, updating MenuSelector");
                menuSelector.UpdateSkillMenuOptions(currentSkillButtons.ToArray());
            }
        }
    }
    
    public void OnItemButtonClicked(ItemData item)
    {
        Debug.Log($"[DEBUG TARGETING] OnItemButtonClicked - Item: {item.name}, Amount: {item.amount}, RequiresTarget: {item.requiresTarget}");
        if (item.requiresTarget)
        {
            Debug.Log($"[DEBUG TARGETING] Item requires target, starting target selection for {item.name}");
            // First set the selected item so StartTargetSelection can identify it correctly
            menuSelector.SetSelectedItem(item);
            // Then start target selection
            menuSelector.StartTargetSelection();
        }
        else
        {
            Debug.Log($"[DEBUG TARGETING] Item does not require target, executing immediately: {item.name}");
            ExecuteItem(item, null);
        }
    }
    
    public void ExecuteItem(ItemData item, CombatStats target)
    {
        Debug.Log($"[DEBUG TARGETING] ExecuteItem - Item: {item.name}, Target: {target?.name ?? "none"}, TargetIsEnemy: {target?.isEnemy.ToString() ?? "N/A"}");
        
        // Get the active character
        var activeCharacter = combatManager.ActiveCharacter;
        if (activeCharacter == null) 
        {
            Debug.LogError("[DEBUG TARGETING] ExecuteItem - Active character is null, cannot execute item");
            return;
        }
        
        // Double-check the target for ally-targeting items (defensive programming)
        if (string.Equals(item.name, "Super Espress-O", StringComparison.OrdinalIgnoreCase) && target != null && target.isEnemy)
        {
            Debug.LogError($"[DEBUG TARGETING] ExecuteItem - ERROR: Tried to use {item.name} on enemy {target.name}! Redirecting to self.");
            // Redirect to self instead of an enemy
            target = activeCharacter;
        }
        
        // Double-check the target for enemy-targeting items (defensive programming)
        if (string.Equals(item.name, "Shiny Bead", StringComparison.OrdinalIgnoreCase) && target != null && !target.isEnemy)
        {
            Debug.LogError($"[DEBUG TARGETING] ExecuteItem - ERROR: Tried to use {item.name} on ally {target.name}! Finding an enemy target instead.");
            // Try to find an enemy target
            var enemies = combatManager.GetLivingEnemies();
            if (enemies.Count > 0)
            {
                target = enemies[0]; // Use the first available enemy
                Debug.Log($"[DEBUG TARGETING] Redirected {item.name} to enemy target: {target.name}");
            }
            else
            {
                Debug.LogError($"[DEBUG TARGETING] No enemy targets available for {item.name}, cannot use item!");
                return; // Cannot use item if no enemies
            }
        }
        
        // Hide the text panel when player selects an action
        HideTextPanel();
        
        // Display just the item name in the action display label
        DisplayActionLabel(item.name);
        
        // Execute item after action label is shown
        StartCoroutine(ExecuteItemAfterMessage(item, target, activeCharacter));
    }
    
    private IEnumerator ExecuteItemAfterMessage(ItemData item, CombatStats target, CombatStats activeCharacter)
    {
        Debug.Log($"[DEBUG TARGETING] ExecuteItemAfterMessage - Item: {item.name}, Target: {target?.name ?? "none"}, TargetIsEnemy: {target?.isEnemy.ToString() ?? "N/A"}");
        
        // Wait a tiny amount just to ensure the action label coroutine has started
        yield return null;
        
        // Wait for the game to resume (after action display is done)
        while (Time.timeScale == 0)
            yield return null;
        
        // Implement item effects
        switch (item.name)
        {
            case "Fruit Juice":
                Debug.Log($"[DEBUG TARGETING] Executing Fruit Juice effect");
                // Always heal all party members, regardless of target
                Debug.Log($"[DEBUG TARGETING] Healing all party members with Fruit Juice");
                foreach (var player in combatManager.players)
                {
                    if (player != null && !player.IsDead())
                    {
                        player.HealHealth(30f);
                        Debug.Log($"Healed {player.name} for 30 HP using Fruit Juice");
                    }
                }
                break;
                
            case "Super Espress-O":
                Debug.Log($"[DEBUG TARGETING] Executing Super Espress-O effect");
                if (target != null && !target.isEnemy)
                {
                    Debug.Log($"[DEBUG TARGETING] Target is ally: {target.name}");
                    // Restore SP (sanity)
                    target.HealSanity(50f);
                    
                    // Boost action speed
                    target.BoostActionSpeed(0.5f, 3); // 50% boost for 3 turns
                    
                    Debug.Log($"Super Espress-O used: Restored 50 SP and boosted speed by 50% for {target.name} for 3 turns");
                }
                else if (target != null && target.isEnemy)
                {
                    Debug.Log($"[DEBUG TARGETING] ERROR: Target is enemy: {target.name} - Super Espress-O should only target allies!");
                    // This should never happen with proper targeting
                }
                else if (target == null)
                {
                    Debug.Log($"[DEBUG TARGETING] No target specified, using Super Espress-O on self: {activeCharacter.name}");
                    // Use on self if no target
                    activeCharacter.HealSanity(50f);
                    activeCharacter.BoostActionSpeed(0.5f, 3);
                    
                    Debug.Log($"Super Espress-O used: Restored 50 SP and boosted speed by 50% for {activeCharacter.name} for 3 turns");
                }
                break;
                
            case "Shiny Bead":
                Debug.Log($"[DEBUG TARGETING] Executing Shiny Bead effect");
                if (target != null && target.isEnemy)
                {
                    // Deal damage to the enemy target
                    float damage = 20f;
                    target.TakeDamage(damage);
                    Debug.Log($"[DEBUG TARGETING] Shiny Bead dealt {damage} damage to enemy: {target.name}");
                }
                else if (target != null && !target.isEnemy)
                {
                    Debug.Log($"[DEBUG TARGETING] ERROR: Target is ally: {target.name} - Shiny Bead should only target enemies!");
                    // This should never happen with proper targeting
                }
                else
                {
                    Debug.LogWarning($"[DEBUG TARGETING] No target specified for Shiny Bead, cannot execute");
                }
                break;
                
            default:
                Debug.LogWarning($"[DEBUG TARGETING] Unknown item: {item.name}");
                break;
        }
        
        // Reduce item count
        item.amount--;
        Debug.Log($"{item.name} amount reduced to {item.amount}");
        
        // Remove item if amount is 0
        if (item.amount <= 0)
        {
            Debug.Log($"{item.name} used up, removing from inventory");
            
            // Get the combat manager's inventory
            var playerInventory = combatManager.GetPlayerInventoryItems();
            if (playerInventory != null)
            {
                Debug.Log($"Removing {item.name} from combat manager's inventory");
                // Remove the item from the combat manager's inventory
                playerInventory.RemoveAll(i => i.name == item.name && i.amount <= 0);
            }
            
            // Also remove from all players' inventories
            foreach (var player in combatManager.players)
            {
                if (player != null)
                {
                    Debug.Log($"Removing {item.name} from {player.characterName}'s inventory");
                    player.items.RemoveAll(i => i.name == item.name && i.amount <= 0);
                }
            }
        }
        
        // Update the UI with the new inventory state
        var updatedInventory = combatManager.GetPlayerInventoryItems();
        PopulateItemMenu(updatedInventory);
        
        // Make sure we're not in target selection mode before ending the turn
        if (menuSelector.IsSelectingTarget())
        {
            menuSelector.CancelTargetSelection();
        }
        
        // Update UI to reflect changes immediately
        UpdateUI();
        
        // Return to action menu and end the player's turn
        BackToItemMenu();
        combatManager.EndPlayerTurn();
    }
    
    public void BackToItemMenu()
    {
        Debug.Log("[ItemButton Lifecycle] Returning to action menu from item menu");
        
        // Clear the item menu active flag in the combat manager
        combatManager.isItemMenuActive = false;
        
        if (itemMenu != null) itemMenu.SetActive(false);
        if (characterStatsPanel != null) characterStatsPanel.SetActive(true);
        if (skillMenu != null) skillMenu.SetActive(false); // Ensure skill menu is hidden
        actionMenu.SetActive(true);
        
        // Restore the menu button template's original state
        if (menuButtonTemplate != null)
        {
            bool shouldBeActive = menuButtonTemplate.CompareTag("ActiveTemplate");
            menuButtonTemplate.SetActive(shouldBeActive);
            // Reset the tag
            menuButtonTemplate.tag = "Untagged";
            Debug.Log($"[ItemButton Lifecycle] Restoring menu button template: {menuButtonTemplate.name}, Active: {shouldBeActive}");
        }
        
        // First, create a local copy of the buttons to destroy
        List<GameObject> buttonsToDestroy = new List<GameObject>(currentSkillButtons);
        
        // Clear the list before destroying to prevent access to destroyed objects
        currentSkillButtons.Clear();
        
        // Now destroy the buttons
        if (buttonsToDestroy.Count > 0)
        {
            Debug.Log($"[ItemButton Lifecycle] Destroying {buttonsToDestroy.Count} item buttons");
            foreach (var button in buttonsToDestroy)
            {
                if (button != null)
                {
                    Debug.Log($"[ItemButton Lifecycle] Destroying button: {(button.GetComponentInChildren<TextMeshProUGUI>()?.text ?? "unknown")}");
                    Destroy(button);
                }
            }
        }
        
        // Find and destroy any RuntimeItemButtons container that might have been left behind
        Transform containerTransform = itemMenu.transform.Find("ItemButtonsContainer");
        if (containerTransform != null)
        {
            Transform runtimeContainer = containerTransform.Find("RuntimeItemButtons");
            if (runtimeContainer != null)
            {
                Debug.Log("[ItemButton Lifecycle] Destroying leftover RuntimeItemButtons container");
                Destroy(runtimeContainer.gameObject);
            }
        }
        
        // Update UI to reflect any changes
        UpdateUI();
        
        // Reset the menu selector state
        menuSelector.EnableMenu();
    }

    public void HideActionMenu()
    {
        menuSelector.SetMenuItemsEnabled(false);
        menuSelector.DisableMenu();
    }

    public void OnAttackSelected()
    {
        // This is now handled by MenuSelector's target selection
    }

    public void OnTargetSelected(CombatStats target)
    {
        // Make sure we're not in target selection mode
        if (menuSelector.IsSelectingTarget())
        {
            menuSelector.CancelTargetSelection();
        }
        
        // Execute the attack action
        combatManager.ExecutePlayerAction("attack", target);
    }

    public void OnSkillSelected()
    {
        // Hide the text panel when player selects an action
        HideTextPanel();
        
        // Show the skill menu
        ShowSkillMenu();
    }

    public void OnGuardSelected()
    {
        // Get the active character
        CombatStats activeCharacter = combatManager.ActiveCharacter;
        
        if (activeCharacter != null && !activeCharacter.isEnemy)
        {
            // Hide the text panel when player selects an action
            HideTextPanel();
            
            // Display just "Guard" in the action display label
            DisplayActionLabel("Guard");
            
            // Activate guard stance after the action label is shown
            StartCoroutine(GuardAfterMessage(activeCharacter));
        }
    }
    
    private IEnumerator GuardAfterMessage(CombatStats character)
    {
        // Wait a tiny amount just to ensure the action label coroutine has started
        yield return null;
        
        // Wait for the game to resume (after action display is done)
        while (Time.timeScale == 0)
            yield return null;
        
        // Activate guard stance
        character.ActivateGuard();
        
        // End the player's turn
        combatManager.EndPlayerTurn();
    }

    public void OnHealSelected()
    {
        combatManager.ExecutePlayerAction("heal");
    }

    public void UpdateCharacterUI(CombatStats[] allCharacters, CombatStats activeCharacter)
    {
        for (int i = 0; i < characterUI.Length && i < allCharacters.Length; i++)
        {
            if (characterUI[i] != null && allCharacters[i] != null)
            {
                bool isActive = (allCharacters[i] == activeCharacter);
                characterUI[i].UpdateUI(allCharacters[i], isActive);
            }
        }
    }

    public void DisplayTurnAndActionMessage(string message)
    {
        if (currentTextCoroutine != null)
        {
            StopCoroutine(currentTextCoroutine);
        }

        currentTextCoroutine = StartCoroutine(DisplayMessage(message));
    }

    private IEnumerator DisplayMessage(string message)
    {
        turnText.text = message;
        
        // Pause game for exactly 1.0 seconds
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(1.0f);
        Time.timeScale = 1;
        
        // Clear the message
        turnText.text = "";
        
        currentTextCoroutine = null;
    }

    /// <summary>
    /// Populates the item menu with items from the player's inventory
    /// </summary>
    /// <param name="items">The items to populate the menu with</param>
    public void PopulateItemMenu(List<ItemData> items)
    {
        Debug.Log("=== INVENTORY DEBUG: CombatUI.PopulateItemMenu ===");
        
        if (itemMenu == null || items == null)
        {
            Debug.LogWarning("Cannot populate item menu: itemMenu or items list is null");
            return;
        }
        
        Debug.Log($"Populating item menu with {items.Count} items from combat inventory");
        
        // Clear existing buttons
        foreach (Transform child in itemMenu.transform)
        {
            // Don't destroy layout groups or other UI components
            if (child.GetComponent<Button>() != null || child.name.Contains("button", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"Destroying existing item button: {child.name}");
                Destroy(child.gameObject);
            }
        }
        
        // Find or create the container for buttons
        Transform itemsContainer = itemMenu.transform.Find("ItemsContainer");
        if (itemsContainer == null)
        {
            GameObject container = new GameObject("ItemsContainer");
            container.transform.SetParent(itemMenu.transform, false);
            RectTransform rectTransform = container.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.offsetMin = new Vector2(10, 10);
            rectTransform.offsetMax = new Vector2(-10, -10);
            
            // Add grid layout
            GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(300, 40);
            grid.spacing = new Vector2(30, 30);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            
            itemsContainer = container.transform;
            Debug.Log("Created new ItemsContainer with GridLayoutGroup");
        }
        
        // Add buttons for each non-KeyItem
        int filteredItems = 0;
        foreach (ItemData item in items)
        {
            // Final safety check - NEVER show KeyItems in combat
            if (item.IsKeyItem() || item.type == ItemData.ItemType.KeyItem || item.name == "Cold Key")
            {
                Debug.LogWarning($"CRITICAL: Found KeyItem in combat UI items - filtering out {item.name}");
                filteredItems++;
                continue;
            }
            
            if (item.amount > 0)
            {
                Debug.Log($"Creating button for item: {item.name} x{item.amount}, Type: {item.type}");
                GameObject buttonObj = Instantiate(buttonPrefab, itemsContainer);
                ItemButtonData buttonData = buttonObj.AddComponent<ItemButtonData>();
                buttonData.item = item;
                
                // Set button text
                Text buttonText = buttonObj.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = $"{item.name} x{item.amount}";
                }
                else
                {
                    TextMeshProUGUI tmpText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmpText != null)
                    {
                        tmpText.text = $"{item.name} x{item.amount}";
                    }
                    else
                    {
                        Debug.LogWarning($"No Text or TextMeshProUGUI found on button for item {item.name}");
                    }
                }
                
                // Add click listener
                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => UseItem(item));
                }
            }
        }
        
        if (filteredItems > 0)
        {
            Debug.LogWarning($"CRITICAL NOTICE: Filtered {filteredItems} KeyItems from combat UI that shouldn't have been there!");
        }
    }
    
    /// <summary>
    /// Use an item in combat
    /// </summary>
    /// <param name="item">The item to use</param>
    private void UseItem(ItemData item)
    {
        // Use the newer implementation that correctly manages inventory
        Debug.Log($"=== INVENTORY DEBUG: CombatUI.UseItem ===");
        Debug.Log($"Using item: {item.name}, Amount: {item.amount}");
        OnItemButtonClicked(item);
    }

    public bool IsDisplayingMessage()
    {
        return currentTextCoroutine != null;
    }
    
    public void DisplayActionLabel(string actionText)
    {
        if (actionDisplayLabel == null || actionDisplayText == null)
        {
            Debug.LogWarning("Action display label or text not found!");
            return;
        }
        
        StartCoroutine(ShowActionLabel(actionText));
    }
    
    private IEnumerator ShowActionLabel(string actionText)
    {
        // Show the action display label
        actionDisplayLabel.SetActive(true);
        
        // Set the text
        actionDisplayText.text = actionText;
        
        // Set alpha to 1 (fully opaque)
        Image labelBackground = actionDisplayLabel.GetComponent<Image>();
        if (labelBackground != null)
        {
            Color color = labelBackground.color;
            color.a = 1f;
            labelBackground.color = color;
        }
        
        // Pause the game
        Time.timeScale = 0;
        
        // Wait for 0.5 seconds in real time (not affected by time scale)
        yield return new WaitForSecondsRealtime(actionDisplayDuration);
        
        // Resume the game
        Time.timeScale = 1;
        
        // Hide the action display label
        actionDisplayLabel.SetActive(false);
    }

    public void ShowTextPanel(string message, float opacity = 0.5f)
    {
        if (textPanel == null || turnText == null)
        {
            Debug.LogWarning("Text panel or turn text not found!");
            return;
        }
        
        // Show the text panel
        textPanel.SetActive(true);
        
        // Set the text
        turnText.text = message;
        
        // Set the opacity of the text panel
        Image panelImage = textPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            Color color = panelImage.color;
            color.a = opacity;
            panelImage.color = color;
        }
    }
    
    public void HideTextPanel()
    {
        if (textPanel != null)
        {
            textPanel.SetActive(false);
        }
    }
} 