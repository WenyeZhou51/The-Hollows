using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
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

        // Display skill action message
        DisplayTurnAndActionMessage($"{activeCharacter.characterName} uses {skill.name}!");
        
        // Execute skill after message duration
        StartCoroutine(ExecuteSkillAfterMessage(skill, target, activeCharacter));
    }
    
    private IEnumerator ExecuteSkillAfterMessage(SkillData skill, CombatStats target, CombatStats activeCharacter)
    {
        yield return new WaitForSeconds(actionMessageDuration);
        
        switch (skill.name)
        {
            case "Before Your Eyes":
                if (target != null)
                {
                    target.ResetAction();
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                break;
                
            case "Fiend Fire":
                if (target != null)
                {
                    // Determine number of hits (1-5 random)
                    int hitCount = Random.Range(1, 6); // 1 to 5 inclusive
                    float totalDamage = 0;
                    
                    Debug.Log($"[Skill Execution] Fiend Fire hits {hitCount} times for {fiendFireDamage} damage each");
                    
                    // Apply damage multiple times
                    for (int i = 0; i < hitCount; i++)
                    {
                        target.TakeDamage(fiendFireDamage);
                        totalDamage += fiendFireDamage;
                        
                        // Small delay between hits would be nice in a full implementation
                        // For now, just log each hit
                        Debug.Log($"[Skill Execution] Fiend Fire hit #{i+1} deals {fiendFireDamage} damage");
                    }
                    
                    Debug.Log($"[Skill Execution] Fiend Fire total damage: {totalDamage}");
                    
                    // Use sanity (0 in this case, but keeping the code for consistency)
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                break;
                
            case "Slam!":
                // Get all enemies
                var enemies = combatManager.enemies;
                int totalEnemiesHit = 0;
                
                Debug.Log($"[Skill Execution] Slam! targeting all {enemies.Count} enemies for {slamDamage} damage each");
                
                // Apply damage to all enemies
                foreach (var enemy in enemies)
                {
                    if (enemy != null && !enemy.IsDead())
                    {
                        enemy.TakeDamage(slamDamage);
                        totalEnemiesHit++;
                        Debug.Log($"[Skill Execution] Slam! hit enemy {enemy.name} for {slamDamage} damage");
                    }
                }
                
                Debug.Log($"[Skill Execution] Slam! hit {totalEnemiesHit} enemies for a total of {totalEnemiesHit * slamDamage} damage");
                
                // Use sanity (0 in this case, but keeping the code for consistency)
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
                // Check if target is a valid ally (not an enemy and not self)
                if (target != null && !target.isEnemy && target != activeCharacter)
                {
                    Debug.Log($"[Skill Execution] Healing Words! {activeCharacter.name} is healing {target.name}");
                    
                    // Heal the target using the configurable amount
                    target.HealHealth(healingWordsHealthAmount);
                    
                    // Restore sanity to the target using the configurable amount
                    if (target.currentSanity < target.maxSanity)
                    {
                        target.HealSanity(healingWordsSanityAmount);
                        Debug.Log($"[Skill Execution] Healing Words restored {healingWordsSanityAmount} sanity to {target.name}");
                    }
                    
                    // Use sanity from the caster
                    activeCharacter.UseSanity(skill.sanityCost);
                    
                    Debug.Log($"[Skill Execution] Healing Words healed {target.name} for {healingWordsHealthAmount} HP and {healingWordsSanityAmount} sanity");
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
            var activeCharStats = combatManager.ActiveCharacter;
            if (activeCharStats != null)
            {
                // Track processed items to avoid duplicates
                HashSet<string> processedItems = new HashSet<string>();
                
                // Get all items from the party (all player characters)
                List<ItemData> partyItems = new List<ItemData>();
                foreach (var player in combatManager.players)
                {
                    if (player != null && !player.IsDead())
                    {
                        partyItems.AddRange(player.items);
                    }
                }
                
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
        Debug.Log($"[ItemButton Lifecycle] Item button clicked - Item: {item.name}");
        if (item.requiresTarget)
        {
            Debug.Log($"[ItemButton Lifecycle] Item requires target, starting target selection");
            menuSelector.StartTargetSelection();
            // Store the selected item for when target is selected
            menuSelector.SetSelectedItem(item);
        }
        else
        {
            Debug.Log($"[ItemButton Lifecycle] Item does not require target, executing immediately");
            ExecuteItem(item, null);
        }
    }
    
    public void ExecuteItem(ItemData item, CombatStats target)
    {
        Debug.Log($"[ItemButton Lifecycle] Executing item: {item.name}, Target: {target?.name ?? "none"}");
        
        // Get the active character
        var activeCharacter = combatManager.ActiveCharacter;
        if (activeCharacter == null) return;
        
        // Display item action message
        DisplayTurnAndActionMessage($"{activeCharacter.characterName} uses {item.name}!");
        
        // Execute item after message duration
        StartCoroutine(ExecuteItemAfterMessage(item, target, activeCharacter));
    }
    
    private IEnumerator ExecuteItemAfterMessage(ItemData item, CombatStats target, CombatStats activeCharacter)
    {
        yield return new WaitForSeconds(actionMessageDuration);
        
        // Implement item effects
        switch (item.name)
        {
            case "Fruit Juice":
                // Heal all party members for 30 HP
                foreach (var player in combatManager.players)
                {
                    if (player != null && !player.IsDead())
                    {
                        player.HealHealth(30f);
                        Debug.Log($"[ItemButton Lifecycle] Healed {player.name} for 30 HP using Fruit Juice");
                    }
                }
                break;
                
            default:
                Debug.LogWarning($"[ItemButton Lifecycle] Unknown item: {item.name}");
                break;
        }
        
        // Reduce item count
        item.amount--;
        Debug.Log($"[ItemButton Lifecycle] {item.name} amount reduced to {item.amount}");
        
        // Remove item if amount is 0
        if (item.amount <= 0)
        {
            Debug.Log($"[ItemButton Lifecycle] {item.name} used up, removing from inventory");
            
            // Remove from all players' inventories
            foreach (var player in combatManager.players)
            {
                if (player != null)
                {
                    player.items.RemoveAll(i => i.name == item.name && i.amount <= 0);
                }
            }
        }
        
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
        // Show the skill menu
        ShowSkillMenu();
    }

    public void OnGuardSelected()
    {
        // Get the active character
        CombatStats activeCharacter = combatManager.ActiveCharacter;
        
        if (activeCharacter != null && !activeCharacter.isEnemy)
        {
            // Display action message
            DisplayTurnAndActionMessage($"{activeCharacter.characterName} guards...");
            
            // Activate guard stance after the message duration
            StartCoroutine(GuardAfterMessage(activeCharacter));
        }
    }
    
    private IEnumerator GuardAfterMessage(CombatStats character)
    {
        yield return new WaitForSeconds(actionMessageDuration);
        
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
        yield return new WaitForSeconds(actionMessageDuration);
        turnText.text = "";
        currentTextCoroutine = null;
    }

    /// <summary>
    /// Populates the item menu with items from the player's inventory
    /// </summary>
    /// <param name="items">The items to populate the menu with</param>
    public void PopulateItemMenu(List<ItemData> items)
    {
        if (itemMenu == null || items == null)
        {
            return;
        }
        
        // Clear existing buttons
        foreach (Transform child in itemMenu.transform)
        {
            Destroy(child.gameObject);
        }
        
        // Add buttons for each item
        foreach (ItemData item in items)
        {
            if (item.amount > 0)
            {
                GameObject buttonObj = Instantiate(buttonPrefab, itemMenu.transform);
                ItemButtonData buttonData = buttonObj.AddComponent<ItemButtonData>();
                buttonData.item = item;
                
                // Set button text
                Text buttonText = buttonObj.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = $"{item.name} x{item.amount}";
                }
                
                // Add click listener
                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => UseItem(item));
                }
            }
        }
    }
    
    /// <summary>
    /// Use an item in combat
    /// </summary>
    /// <param name="item">The item to use</param>
    private void UseItem(ItemData item)
    {
        // Implement the item use logic based on your game's design
        // This is a placeholder for demonstration purposes
        
        Debug.Log($"Using item: {item.name}");
        
        CombatStats activeCharacter = combatManager.ActiveCharacter;
        if (activeCharacter != null)
        {
            // Example: If it's a health potion, heal the character
            if (item.name.Contains("Potion") || item.name.Contains("Heal"))
            {
                activeCharacter.HealHealth(20f);
                
                // Show visual feedback
                if (healingPopupPrefab != null)
                {
                    GameObject popup = Instantiate(healingPopupPrefab, activeCharacter.transform.position, Quaternion.identity);
                    HealingPopup healingPopup = popup.GetComponent<HealingPopup>();
                    if (healingPopup != null)
                    {
                        healingPopup.Setup(20f, false); // false = healing HP, not sanity
                    }
                }
                
                // Display action message
                if (turnText != null)
                {
                    DisplayTurnAndActionMessage($"{activeCharacter.characterName} used {item.name}!");
                }
            }
            
            // Reduce item count
            item.amount--;
            
            // End player turn
            combatManager.EndPlayerTurn();
            
            // Refresh the item menu
            PopulateItemMenu(combatManager.GetPlayerInventoryItems());
        }
    }
} 