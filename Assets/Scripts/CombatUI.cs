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
    [Tooltip("Reference to the skill description panel")]
    public GameObject skillDescriptionPanel;
    [Tooltip("Reference to the TextMeshProUGUI component for skill descriptions")]
    public TextMeshProUGUI skillDescriptionText;
    [Tooltip("Reference to the scroll view for skills")]
    public ScrollRect skillScrollRect;
    [Tooltip("Reference to the viewport for skills")]
    public RectTransform skillViewport;

    [Header("Skill Menu Layout")]
    [Tooltip("Height of individual skill buttons")]
    [SerializeField] private float skillButtonHeight = 40f;
    [Tooltip("Width of individual skill buttons")]
    [SerializeField] private float skillButtonWidth = 200f;
    [Tooltip("Spacing between skill buttons")]
    [SerializeField] private float skillButtonSpacing = 5f;
    [Tooltip("Number of skill buttons visible without scrolling")]
    [SerializeField] private int visibleSkillButtonCount = 3;
    
    // Cycling scroll system variables
    public int currentSkillScrollIndex = 0; // Which skill is at the top of the visible window
    private List<SkillData> allAvailableSkills = new List<SkillData>(); // All skills for the current character
    [Header("Skill Container Padding")]
    [Tooltip("Left padding inside the skill container")]
    [SerializeField] private float skillContainerPaddingLeft = 10f;
    [Tooltip("Right padding inside the skill container")]
    [SerializeField] private float skillContainerPaddingRight = 10f;
    [Tooltip("Top padding inside the skill container")]
    [SerializeField] private float skillContainerPaddingTop = 10f;
    [Tooltip("Bottom padding inside the skill container")]
    [SerializeField] private float skillContainerPaddingBottom = 10f;
    
    [Header("Skill Parameters")]
    [Tooltip("Damage dealt by the Fiend Fire skill per hit")]
    [SerializeField] private float fiendFireDamage = 10f;
    
    // Public getters for other components to access layout values
    public float GetSkillButtonSpacing() => skillButtonSpacing;
    public float GetSkillButtonHeight() => skillButtonHeight;
    public float GetSkillButtonWidth() => skillButtonWidth;
    public int GetVisibleSkillButtonCount() => visibleSkillButtonCount;
    public RectOffset GetSkillContainerPadding() => new RectOffset(
        (int)skillContainerPaddingLeft,
        (int)skillContainerPaddingRight,
        (int)skillContainerPaddingTop,
        (int)skillContainerPaddingBottom
    );
    [Tooltip("Damage dealt by the Slam skill to all enemies")]
    [SerializeField] private float slamDamage = 10f;
    [Tooltip("Damage dealt by the Piercing Shot skill")]
    [SerializeField] private float piercingShotDamage = 10f;
    [Tooltip("Amount of health restored by Healing Words")]
    [SerializeField] private float healingWordsHealthAmount = 70f;
    [Tooltip("Amount of sanity restored by Healing Words")]
    [SerializeField] private float healingWordsSanityAmount = 50f;

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
            
        // Initialize skill description panel if not assigned
        if (skillDescriptionPanel == null)
            skillDescriptionPanel = GameObject.Find("Skill Description");
            
        if (skillDescriptionPanel != null && skillDescriptionText == null)
            skillDescriptionText = skillDescriptionPanel.GetComponentInChildren<TextMeshProUGUI>();
            
        if (skillDescriptionText == null)
            Debug.LogWarning("Skill description text not found! Make sure Skill Description panel has a TextMeshProUGUI component.");
        else
        {
            // Set up description text properties to prevent overflow
            SetupDescriptionTextConstraints();
            // Note: Removed SetupDescriptionPanelConstraints() to respect original inspector settings
        }
            
        // Hide the skill description panel at start - only show when skill is selected
        if (skillDescriptionPanel != null)
            skillDescriptionPanel.SetActive(false);
            
        // Initialize scroll components if not assigned
        if (skillScrollRect == null && skillPanel != null)
            skillScrollRect = skillPanel.GetComponentInChildren<ScrollRect>();
            
        if (skillViewport == null && skillScrollRect != null)
            skillViewport = skillScrollRect.viewport;
            
        // Set up viewport constraints to show only 3 skills at a time
        SetupSkillViewportConstraints();
        
        // If no ScrollRect is available, set up basic container constraints
        if (skillScrollRect == null && skillButtonsContainer != null)
        {
            SetupBasicSkillContainerConstraints();
        }
            
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
                skillButtonsContainer.offsetMin = new Vector2(skillContainerPaddingLeft, skillContainerPaddingBottom);
                skillButtonsContainer.offsetMax = new Vector2(-skillContainerPaddingRight, -skillContainerPaddingTop);
            }
            
            // Check if we already have a VerticalLayoutGroup, if not remove GridLayoutGroup and add VerticalLayoutGroup
            VerticalLayoutGroup verticalLayout = skillButtonsContainer.GetComponent<VerticalLayoutGroup>();
            GridLayoutGroup gridLayout = skillButtonsContainer.GetComponent<GridLayoutGroup>();
            
            if (gridLayout != null)
            {
                DestroyImmediate(gridLayout);
            }
            
            if (verticalLayout == null)
            {
                verticalLayout = skillButtonsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                verticalLayout.spacing = skillButtonSpacing;
                verticalLayout.childAlignment = TextAnchor.UpperCenter;
                verticalLayout.childControlWidth = true;
                verticalLayout.childControlHeight = false;
                verticalLayout.childForceExpandWidth = false;
                verticalLayout.childForceExpandHeight = false;
                
                // Apply padding from skillContainerPadding settings
                verticalLayout.padding = new RectOffset(
                    (int)skillContainerPaddingLeft,   // left
                    (int)skillContainerPaddingRight,  // right
                    (int)skillContainerPaddingTop,    // top
                    (int)skillContainerPaddingBottom  // bottom
                );
            }
            
            // Don't add ContentSizeFitter here - it conflicts with ScrollRect and causes infinite growth
            // The ScrollRect will handle the sizing properly
            ContentSizeFitter existingSizeFitter = skillButtonsContainer.GetComponent<ContentSizeFitter>();
            if (existingSizeFitter != null)
            {
                Debug.Log("[SkillButton Lifecycle] Removing ContentSizeFitter that conflicts with ScrollRect");
                DestroyImmediate(existingSizeFitter);
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
        Debug.Log("[SkillButton Lifecycle] ShowSkillMenu called - Beginning cycling skill menu setup");
        if (skillPanel != null)
        {
            if (characterStatsPanel != null) characterStatsPanel.SetActive(false);
            actionMenu.SetActive(false);
            skillPanel.SetActive(true);
            
            // Hide the menu button template if it's assigned
            if (menuButtonTemplate != null)
            {
                bool wasTemplateActive = menuButtonTemplate.activeSelf;
                menuButtonTemplate.SetActive(false);
                menuButtonTemplate.tag = wasTemplateActive ? "ActiveTemplate" : "InactiveTemplate";
                Debug.Log($"[SkillButton Lifecycle] Hiding menu button template: {menuButtonTemplate.name}");
            }
            
            // Get the active character's skills
            var activeCharStats = combatManager.ActiveCharacter;
            if (activeCharStats != null)
            {
                // Store all available skills for cycling
                allAvailableSkills = new List<SkillData>(activeCharStats.skills);
                currentSkillScrollIndex = 0; // Start at the beginning
                
                Debug.Log($"[SkillButton Lifecycle] Active character has {allAvailableSkills.Count} skills: {string.Join(", ", allAvailableSkills.Select(s => s.name))}");
                
                // Create or update the 3 cycling buttons
                CreateCyclingSkillButtons();
                
                // Update MenuSelector with the cycling system
                menuSelector.SetCyclingSkillSystem(this, allAvailableSkills, currentSkillScrollIndex);
            }
        }
    }

    // Cycling scroll system methods
    private void CreateCyclingSkillButtons()
    {
        // Clear existing buttons
        foreach (var button in currentSkillButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        currentSkillButtons.Clear();

        // Set up the container properly first
        SetupCyclingContainer();

        // Create exactly 3 buttons for cycling
        for (int i = 0; i < 3; i++)
        {
            GameObject skillButton = CreateSkillButton(i);
            if (skillButton != null)
            {
                currentSkillButtons.Add(skillButton);
            }
        }

        Debug.Log($"[Cycling Skill Menu] Created {currentSkillButtons.Count} cycling skill buttons");
        
        // Update MenuSelector with the cycling buttons
        menuSelector.UpdateSkillMenuOptions(currentSkillButtons.ToArray());
    }

    private void SetupCyclingContainer()
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
            runtimeContainer.offsetMin = new Vector2(skillContainerPaddingLeft, skillContainerPaddingBottom);
            runtimeContainer.offsetMax = new Vector2(-skillContainerPaddingRight, -skillContainerPaddingTop);
            
            // Add a vertical layout group
            VerticalLayoutGroup verticalLayout = containerObj.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = skillButtonSpacing;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = false;
            verticalLayout.childForceExpandHeight = false;
            
            // Apply padding from skillContainerPadding settings
            verticalLayout.padding = new RectOffset(
                (int)skillContainerPaddingLeft,   // left
                (int)skillContainerPaddingRight,  // right
                (int)skillContainerPaddingTop,    // top
                (int)skillContainerPaddingBottom  // bottom
            );
            
            // For ScrollRect content, we need ContentSizeFitter to size the content area properly
            if (skillScrollRect != null)
            {
                ContentSizeFitter sizeFitter = containerObj.AddComponent<ContentSizeFitter>();
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                Debug.Log("[Cycling Skill Menu] Added ContentSizeFitter for ScrollRect content");
            }
            
            // If we already have a container in the scene, destroy it to avoid duplicates
            if (skillButtonsContainer != null && skillButtonsContainer.gameObject != null && 
                !isPrefabAsset && skillButtonsContainer.gameObject.scene.IsValid())
            {
                Destroy(skillButtonsContainer.gameObject);
            }
            
            // Update the reference
            skillButtonsContainer = runtimeContainer;
            
            // Set this as the content of the ScrollRect if available
            if (skillScrollRect != null)
            {
                skillScrollRect.content = runtimeContainer;
                Debug.Log("[Cycling Skill Menu] Set runtime container as ScrollRect content");
            }
        }
        else
        {
            // Use the existing container
            runtimeContainer = skillButtonsContainer;
            
            // Make sure it has a vertical layout
            VerticalLayoutGroup verticalLayout = runtimeContainer.GetComponent<VerticalLayoutGroup>();
            GridLayoutGroup gridLayout = runtimeContainer.GetComponent<GridLayoutGroup>();
            
            if (gridLayout != null)
            {
                DestroyImmediate(gridLayout);
            }
            
            if (verticalLayout == null)
            {
                verticalLayout = runtimeContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                verticalLayout.spacing = skillButtonSpacing;
                verticalLayout.childAlignment = TextAnchor.UpperCenter;
                verticalLayout.childControlWidth = true;
                verticalLayout.childControlHeight = false;
                verticalLayout.childForceExpandWidth = false;
                verticalLayout.childForceExpandHeight = false;
                
                // Apply padding from skillContainerPadding settings
                verticalLayout.padding = new RectOffset(
                    (int)skillContainerPaddingLeft,   // left
                    (int)skillContainerPaddingRight,  // right
                    (int)skillContainerPaddingTop,    // top
                    (int)skillContainerPaddingBottom  // bottom
                );
            }
            else
            {
                // Update existing VerticalLayoutGroup padding
                verticalLayout.padding = new RectOffset(
                    (int)skillContainerPaddingLeft,   // left
                    (int)skillContainerPaddingRight,  // right
                    (int)skillContainerPaddingTop,    // top
                    (int)skillContainerPaddingBottom  // bottom
                );
            }
            
            // Handle ContentSizeFitter based on ScrollRect presence
            ContentSizeFitter existingSizeFitter = runtimeContainer.GetComponent<ContentSizeFitter>();
            if (skillScrollRect != null)
            {
                // We need ContentSizeFitter for ScrollRect content
                if (existingSizeFitter == null)
                {
                    existingSizeFitter = runtimeContainer.gameObject.AddComponent<ContentSizeFitter>();
                    existingSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    existingSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    Debug.Log("[Cycling Skill Menu] Added ContentSizeFitter for existing container with ScrollRect");
                }
                
                // Set this as the content of the ScrollRect
                skillScrollRect.content = runtimeContainer;
                Debug.Log("[Cycling Skill Menu] Set existing container as ScrollRect content");
            }
            else
            {
                // Remove ContentSizeFitter when no ScrollRect (conflicts with basic constraints)
                if (existingSizeFitter != null)
                {
                    Debug.Log("[Cycling Skill Menu] Removing ContentSizeFitter from container without ScrollRect");
                    DestroyImmediate(existingSizeFitter);
                }
            }
        }
        
        // Ensure viewport constraints are applied
        SetupSkillViewportConstraints();
        
        // If no ScrollRect is available, set up basic container constraints
        if (skillScrollRect == null)
        {
            SetupBasicSkillContainerConstraints();
        }
    }

    private GameObject CreateSkillButton(int buttonIndex)
    {
        // Calculate which skill this button should display
        int skillIndex = currentSkillScrollIndex + buttonIndex;
        
        // If we don't have enough skills, don't create the button
        if (skillIndex >= allAvailableSkills.Count)
        {
            return null;
        }

        SkillData skill = allAvailableSkills[skillIndex];
        
        // Use menu button template if available, otherwise fall back to buttonPrefab
        GameObject skillButton;
        if (menuButtonTemplate != null)
        {
            skillButton = Instantiate(menuButtonTemplate);
            
            // Remove any existing components that might interfere
            Button existingButton = skillButton.GetComponent<Button>();
            if (existingButton != null)
            {
                Destroy(existingButton);
            }
            
            SkillButtonData existingSkillData = skillButton.GetComponent<SkillButtonData>();
            if (existingSkillData != null)
            {
                Destroy(existingSkillData);
            }
        }
        else
        {
            skillButton = Instantiate(buttonPrefab);
        }
        
        // Set up the button
        skillButton.name = $"CyclingSkillButton_{buttonIndex}";
        skillButton.transform.SetParent(skillButtonsContainer, false);
        skillButton.SetActive(true);
        
        // Configure the button's visual properties
        SetupSkillButtonVisuals(skillButton, skill);
        
        // Add the skill data component
        SkillButtonData skillData = skillButton.AddComponent<SkillButtonData>();
        skillData.skill = skill;
        
        // Add click handler
        Button buttonComponent = skillButton.GetComponent<Button>();
        if (buttonComponent == null)
        {
            buttonComponent = skillButton.AddComponent<Button>();
        }
        buttonComponent.onClick.AddListener(() => OnSkillButtonClicked(skill));
        
        // Add hover description handler
        HoverDescriptionHandler hoverHandler = skillButton.AddComponent<HoverDescriptionHandler>();
        
        return skillButton;
    }

    private void SetupSkillButtonVisuals(GameObject skillButton, SkillData skill)
    {
        // Set up RectTransform
        RectTransform buttonRect = skillButton.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            // Set consistent size for scrolling
            buttonRect.sizeDelta = new Vector2(skillButtonWidth, skillButtonHeight);
            
            // Use the original size from the template if available
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
                    
                    Debug.Log($"[Cycling Skill Menu] Copied RectTransform properties from template - Size: {buttonRect.sizeDelta}, Anchors: {buttonRect.anchorMin}-{buttonRect.anchorMax}");
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
                Debug.Log($"[Cycling Skill Menu] Copied Image properties from template");
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
        
        // Set up text
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
                Debug.Log($"[Cycling Skill Menu] Updated cost text: '{costText.text}'");
            }
            else
            {
                Debug.LogWarning("[Cycling Skill Menu] Cost text component not found on skill button!");
            }
        }
        else
        {
            Debug.LogError("[Cycling Skill Menu] Button text not found on skill button!");
            
            // Try to find the text in a deeper hierarchy
            buttonText = skillButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (buttonText != null)
            {
                Debug.Log("[Cycling Skill Menu] Found text component in deeper hierarchy, enabling it");
                buttonText.gameObject.SetActive(true);
                buttonText.text = skill.name;
                buttonText.ForceMeshUpdate();
            }
            else
            {
                // Create a new text component if none exists
                Debug.Log("[Cycling Skill Menu] Creating new TextMeshProUGUI component");
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
        
        // Add LayoutElement for consistent sizing
        LayoutElement layoutElement = skillButton.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = skillButton.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = skillButtonHeight;
        layoutElement.preferredHeight = skillButtonHeight;
        layoutElement.flexibleHeight = 0f; // Prevent buttons from expanding beyond preferred height
        
        // CRITICAL: Add width constraints to respect skillButtonWidth and prevent full-width expansion
        layoutElement.minWidth = skillButtonWidth;
        layoutElement.preferredWidth = skillButtonWidth;
        layoutElement.flexibleWidth = 0f; // Prevent buttons from expanding beyond preferred width
        
        // Ensure the button RectTransform is properly configured
        RectTransform skillButtonRect = skillButton.GetComponent<RectTransform>();
        if (skillButtonRect != null)
        {
            skillButtonRect.sizeDelta = new Vector2(skillButtonRect.sizeDelta.x, skillButtonHeight);
        }
    }

    public void UpdateCyclingSkillButtons()
    {
        Debug.Log($"[Cycling Skill Menu] Updating cycling buttons, scroll index: {currentSkillScrollIndex}");
        
        for (int i = 0; i < currentSkillButtons.Count && i < 3; i++)
        {
            GameObject button = currentSkillButtons[i];
            if (button != null)
            {
                int skillIndex = currentSkillScrollIndex + i;
                
                if (skillIndex < allAvailableSkills.Count)
                {
                    SkillData skill = allAvailableSkills[skillIndex];
                    
                    // Update the button's skill data
                    SkillButtonData skillData = button.GetComponent<SkillButtonData>();
                    if (skillData != null)
                    {
                        skillData.skill = skill;
                    }
                    
                    // Update the button's text
                    TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = skill.name;
                        buttonText.ForceMeshUpdate();
                        
                        // Update cost text
                        TextMeshProUGUI[] allTexts = button.GetComponentsInChildren<TextMeshProUGUI>();
                        if (allTexts.Length > 1)
                        {
                            allTexts[1].text = $"{skill.sanityCost} SP";
                            allTexts[1].ForceMeshUpdate();
                        }
                    }
                    
                    // Update click handler
                    Button buttonComponent = button.GetComponent<Button>();
                    if (buttonComponent != null)
                    {
                        buttonComponent.onClick.RemoveAllListeners();
                        buttonComponent.onClick.AddListener(() => OnSkillButtonClicked(skill));
                    }
                    
                    button.SetActive(true);
                }
                else
                {
                    // Hide button if no skill to display
                    button.SetActive(false);
                }
            }
        }
        
        // Update the description for the currently selected button
        if (menuSelector != null)
        {
            menuSelector.UpdateCurrentSelectionDescription();
        }
    }

    public bool CanScrollUp()
    {
        return currentSkillScrollIndex > 0;
    }

    public bool CanScrollDown()
    {
        return currentSkillScrollIndex + 3 < allAvailableSkills.Count;
    }

    public void ScrollUp()
    {
        if (CanScrollUp())
        {
            currentSkillScrollIndex--;
            UpdateCyclingSkillButtons();
            Debug.Log($"[Cycling Skill Menu] Scrolled up to index {currentSkillScrollIndex}");
        }
    }

    public void ScrollDown()
    {
        if (CanScrollDown())
        {
            currentSkillScrollIndex++;
            UpdateCyclingSkillButtons();
            Debug.Log($"[Cycling Skill Menu] Scrolled down to index {currentSkillScrollIndex}");
        }
    }

    public SkillData GetSkillAtButtonIndex(int buttonIndex)
    {
        int skillIndex = currentSkillScrollIndex + buttonIndex;
        if (skillIndex >= 0 && skillIndex < allAvailableSkills.Count)
        {
            return allAvailableSkills[skillIndex];
        }
        return null;
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

    public void UpdateSkillDescription(SkillData skill)
    {
        if (skillDescriptionText != null && skill != null)
        {
            string descriptionText = skill.description;
            skillDescriptionText.text = descriptionText;
            
            if (skillDescriptionPanel != null)
            {
                skillDescriptionPanel.SetActive(true);
            }
        }
    }
    
    public void UpdateItemDescription(ItemData item)
    {
        if (skillDescriptionText != null && item != null)
        {
            string descriptionText = item.description;
            skillDescriptionText.text = descriptionText;
            
            if (skillDescriptionPanel != null)
            {
                skillDescriptionPanel.SetActive(true);
            }
        }
    }
    
    public void ClearDescription()
    {
        if (skillDescriptionPanel != null)
        {
            skillDescriptionPanel.SetActive(false);
        }
    }
    
    private void SetupSkillViewportConstraints()
    {
        if (skillViewport == null) return;
        
        // Calculate height for visible skills (button height + spacing)
        float viewportHeight = (skillButtonHeight * visibleSkillButtonCount) + (skillButtonSpacing * (visibleSkillButtonCount - 1)); // buttons + spaces between them
        
        // Set the viewport height constraint
        RectTransform viewportRect = skillViewport;
        if (viewportRect != null)
        {
            // Set a fixed height for the viewport
            viewportRect.sizeDelta = new Vector2(viewportRect.sizeDelta.x, viewportHeight);
            
            // Add or update LayoutElement to enforce the height
            LayoutElement viewportLayout = viewportRect.GetComponent<LayoutElement>();
            if (viewportLayout == null)
            {
                viewportLayout = viewportRect.gameObject.AddComponent<LayoutElement>();
            }
            viewportLayout.preferredHeight = viewportHeight;
            viewportLayout.minHeight = viewportHeight;
            viewportLayout.flexibleHeight = 0f; // Don't allow viewport to expand
            
            Debug.Log($"[Skill Viewport] Set viewport height to {viewportHeight} pixels for {visibleSkillButtonCount} skills");
        }
        
        // Ensure the ScrollRect is properly configured
        if (skillScrollRect != null)
        {
            skillScrollRect.horizontal = false; // Only vertical scrolling
            skillScrollRect.vertical = true;
            skillScrollRect.movementType = ScrollRect.MovementType.Clamped;
            skillScrollRect.scrollSensitivity = 20f;
            Debug.Log("[Skill Viewport] Configured ScrollRect for vertical scrolling");
        }
    }
    
    private void SetupDescriptionTextConstraints()
    {
        if (skillDescriptionText == null) return;
        
        // Enable text wrapping and set overflow mode
        skillDescriptionText.enableWordWrapping = true;
        skillDescriptionText.overflowMode = TextOverflowModes.Ellipsis;
        
        // Set text alignment
        skillDescriptionText.alignment = TextAlignmentOptions.TopLeft;
        
        // Ensure the text stays within its container bounds
        RectTransform textRect = skillDescriptionText.GetComponent<RectTransform>();
        if (textRect != null)
        {
            // Make sure the text fills its container properly
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(skillContainerPaddingLeft, skillContainerPaddingBottom); // Add configurable padding
            textRect.offsetMax = new Vector2(-skillContainerPaddingRight, -skillContainerPaddingTop); // Add configurable padding
        }
        
        // Note: Removed font size modification to respect original inspector settings
        
        Debug.Log("[Skill Description] Set up text constraints with word wrapping and proper bounds");
    }
    
    
    private void SetupBasicSkillContainerConstraints()
    {
        if (skillButtonsContainer == null) return;
        
        // Calculate height for visible skills as fallback when no ScrollRect is available
        float maxHeight = (skillButtonHeight * visibleSkillButtonCount) + (skillButtonSpacing * (visibleSkillButtonCount - 1));
        
        // Only apply constraints if we don't have a ScrollRect
        if (skillScrollRect == null)
        {
            // Add or update LayoutElement to constrain the container height
            LayoutElement containerLayout = skillButtonsContainer.GetComponent<LayoutElement>();
            if (containerLayout == null)
            {
                containerLayout = skillButtonsContainer.gameObject.AddComponent<LayoutElement>();
            }
            containerLayout.preferredHeight = maxHeight;
            containerLayout.minHeight = maxHeight;
            containerLayout.flexibleHeight = 0f; // Don't allow expansion
            
            // Set the container size directly
            RectTransform containerRect = skillButtonsContainer;
            if (containerRect != null)
            {
                containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, maxHeight);
            }
            
            Debug.Log($"[Skill Container] Set up basic height constraints: {maxHeight} pixels for {visibleSkillButtonCount} skills (no ScrollRect)");
        }
        else
        {
            Debug.Log("[Skill Container] Skipping basic constraints - ScrollRect is handling sizing");
        }
    }
    
    public void ScrollToSkillButton(int buttonIndex, GameObject[] skillButtons)
    {
        if (skillScrollRect == null || skillViewport == null || skillButtons == null || buttonIndex < 0 || buttonIndex >= skillButtons.Length)
            return;
            
        GameObject selectedButton = skillButtons[buttonIndex];
        if (selectedButton == null) return;
        
        RectTransform contentRect = skillScrollRect.content;
        if (contentRect == null) return;
        
        // Force layout update to ensure correct calculations
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        
        // Calculate button height (consistent with configured values)
        float totalButtonHeight = skillButtonHeight + skillButtonSpacing;
        
        // Calculate the position of this button in the content (from top)
        float buttonPosition = buttonIndex * totalButtonHeight;
        
        // Calculate the viewport height
        float viewportHeight = skillViewport.rect.height;
        
        // Calculate the total content height
        float contentHeight = skillButtons.Length * totalButtonHeight - skillButtonSpacing; // Remove last spacing
        
        // Only scroll if content is larger than viewport
        if (contentHeight > viewportHeight)
        {
            // Calculate how many buttons can fit in the viewport (aim for 3)
            int visibleButtons = Mathf.FloorToInt(viewportHeight / totalButtonHeight);
            
            // Calculate scroll position to keep selected button in view
            // Try to center the selection, but ensure we don't scroll past content bounds
            float targetScrollTop = buttonPosition - (visibleButtons / 2f) * totalButtonHeight;
            
            // Clamp the target position
            float maxScrollTop = contentHeight - viewportHeight;
            targetScrollTop = Mathf.Clamp(targetScrollTop, 0f, maxScrollTop);
            
            // Convert to normalized position (1 = top, 0 = bottom for Unity's vertical scroll)
            float normalizedPosition = 1f - (targetScrollTop / maxScrollTop);
            
            // Set the scroll position smoothly
            skillScrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
            
            Debug.Log($"[Scroll] Button {buttonIndex}, Position: {buttonPosition}, ViewportHeight: {viewportHeight}, ContentHeight: {contentHeight}, NormalizedPos: {normalizedPosition}");
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
                    
                    float totalDamage = 0f;
                    // Deal 10 damage for each hit individually
                    for (int i = 0; i < hits; i++)
                    {
                        float damage = 10f; // Fixed 10 damage per hit
                        target.TakeDamage(damage);
                        totalDamage += damage;
                    }
                    
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
                
            case "Crescendo":
                // Check if target is a valid ally (not an enemy)
                if (target != null && !target.isEnemy)
                {
                    Debug.Log($"[Skill Execution] Crescendo! {activeCharacter.name} is making {target.name} AGILE");
                    
                    // Apply AGILE status to the target
                    StatusManager crescendoStatusMgr = StatusManager.Instance;
                    if (crescendoStatusMgr != null)
                    {
                        // Apply Agile status with the status system
                        crescendoStatusMgr.ApplyStatus(target, StatusType.Agile, 2);
                        Debug.Log($"[Skill Execution] Crescendo made {target.characterName} AGILE for 2 turns");
                        
                        // Show message in the text panel
                        DisplayTurnAndActionMessage($"{target.characterName} is now AGILE!");
                    }
                    else
                    {
                        // Fallback to direct modification if status manager not available
                        target.BoostActionSpeed(0.5f, 2); // 50% speed boost for 2 turns
                        Debug.LogWarning($"[Skill Execution] Crescendo! StatusManager not found, applied direct speed boost to {target.characterName}");
                    }
                    
                    // Use sanity
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] Crescendo! Invalid target: {target?.name ?? "null"}. This skill requires an ally target.");
                }
                break;
                
            case "Primordial Pile":
                // Check if target is a valid enemy
                if (target != null && target.isEnemy)
                {
                    Debug.Log($"[Skill Execution] Primordial Pile! {activeCharacter.name} is attacking {target.name}");
                    
                    float totalDamage = 0f;
                    // Deal 7-10 damage 3 times
                    for (int i = 0; i < 3; i++)
                    {
                        float damage = UnityEngine.Random.Range(7f, 10f);
                        // Apply the attackMultiplier for strength/weakness statuses
                        damage *= activeCharacter.attackMultiplier;
                        target.TakeDamage(damage);
                        totalDamage += damage;
                    }
                    
                    // Apply WEAKNESS status to the target if not dead
                    if (!target.IsDead())
                    {
                        StatusManager pileStatusMgr = StatusManager.Instance;
                        if (pileStatusMgr != null)
                        {
                            // Apply Weakness status with the status system
                            pileStatusMgr.ApplyStatus(target, StatusType.Weakness, 2);
                            Debug.Log($"[Skill Execution] Primordial Pile dealt {totalDamage} damage to {target.name} and applied WEAKNESS for 2 turns");
                            
                            // Show message in the text panel
                            DisplayTurnAndActionMessage($"Hit for {totalDamage:F1} damage and applied WEAKNESS!");
                        }
                        else
                        {
                            // Fallback to direct modification if status manager not available
                            target.attackMultiplier = 0.5f; // 50% reduction
                            Debug.LogWarning($"[Skill Execution] Primordial Pile! StatusManager not found, applied direct attack reduction to {target.name}");
                        }
                    }
                    
                    // Use sanity
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] Primordial Pile! Invalid target: {target?.name ?? "null"}. This skill requires an enemy target.");
                }
                break;
                
            case "Encore":
                // Check if target is a valid ally (not an enemy)
                if (target != null && !target.isEnemy)
                {
                    Debug.Log($"[Skill Execution] Encore! {activeCharacter.name} is filling {target.name}'s action bar");
                    
                    // Fill the target's action bar to maximum
                    float currentAction = target.currentAction;
                    float maxAction = target.maxAction;
                    
                    // Set the current action to max
                    target.currentAction = maxAction;
                    
                    Debug.Log($"[Skill Execution] Encore filled {target.characterName}'s action bar from {currentAction} to {maxAction}");
                    
                    // Show message in the text panel
                    DisplayTurnAndActionMessage($"{target.characterName}'s action bar filled!");
                    
                    // No sanity cost for this skill
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] Encore! Invalid target: {target?.name ?? "null"}. This skill requires an ally target.");
                }
                break;
                
            case "Piercing Shot":
                // Check if target is a valid enemy
                if (target != null && target.isEnemy)
                {
                    Debug.Log($"[Skill Execution] Piercing Shot! {activeCharacter.name} is shooting {target.name}");
                    
                    // Calculate random damage between 10-15
                    float baseDamage = UnityEngine.Random.Range(10f, 15f);
                    float calculatedDamage = activeCharacter.CalculateDamage(baseDamage);
                    
                    // Deal damage
                    target.TakeDamage(calculatedDamage);
                    
                    // Apply Vulnerable status for 2 turns
                    if (!target.IsDead())
                    {
                        StatusManager piercingShotStatusMgr = StatusManager.Instance;
                        if (piercingShotStatusMgr != null)
                        {
                            // Apply Vulnerable status with the status system
                            piercingShotStatusMgr.ApplyStatus(target, StatusType.Vulnerable, 2);
                            Debug.Log($"[Skill Execution] Piercing Shot hit {target.name} for {calculatedDamage} damage and applied VULNERABLE for 2 turns");
                            
                            // Show message in the text panel
                            DisplayTurnAndActionMessage($"Hit for {calculatedDamage:F1} damage and applied VULNERABLE!");
                        }
                        else
                        {
                            // Fallback to direct modification if status manager not available
                            Debug.LogWarning($"[Skill Execution] Piercing Shot! StatusManager not found, applied direct vulnerable effect to {target.name}");
                        }
                    }
                    
                    // Use sanity
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] Piercing Shot! Invalid target: {target?.name ?? "null"}");
                }
                break;
                
            case "Gaintkiller":
                // Check if target is a valid enemy
                if (target != null && target.isEnemy)
                {
                    Debug.Log($"[Skill Execution] Gaintkiller! {activeCharacter.name} is attacking {target.name}");
                    
                    // Calculate random damage between 60-80
                    float baseDamage = UnityEngine.Random.Range(60f, 80f);
                    float calculatedDamage = activeCharacter.CalculateDamage(baseDamage);
                    
                    // Deal damage
                    target.TakeDamage(calculatedDamage);
                    
                    Debug.Log($"[Skill Execution] Gaintkiller hit {target.name} for {calculatedDamage} damage");
                    
                    // Show message in the text panel
                    DisplayTurnAndActionMessage($"Hit for {calculatedDamage:F1} damage!");
                    
                    // Use sanity
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] Gaintkiller! Invalid target: {target?.name ?? "null"}");
                }
                break;
                
            case "Signal Flare":
                Debug.Log($"[Skill Execution] Signal Flare! {activeCharacter.name} is removing status effects from enemies");
                
                // Get all living enemies
                var allEnemies = combatManager.GetLivingEnemies();
                
                // Get StatusManager instance
                StatusManager signalFlareStatusMgr = StatusManager.Instance;
                if (signalFlareStatusMgr != null)
                {
                    int clearedCount = 0;
                    
                    // Loop through all enemies
                    foreach (CombatStats enemy in allEnemies)
                    {
                        if (enemy != null && !enemy.IsDead())
                        {
                            // Clear all status effects from this enemy
                            signalFlareStatusMgr.ClearAllStatuses(enemy);
                            clearedCount++;
                            
                            Debug.Log($"[Skill Execution] Signal Flare cleared all status effects from {enemy.characterName}");
                        }
                    }
                    
                    if (clearedCount > 0)
                    {
                        DisplayTurnAndActionMessage($"Cleared status effects from {clearedCount} enemies!");
                    }
                    else
                    {
                        DisplayTurnAndActionMessage("No enemies had status effects to clear!");
                    }
                }
                else
                {
                    Debug.LogWarning("[Skill Execution] Signal Flare! StatusManager not found. Skill had no effect.");
                }
                
                // Use sanity
                activeCharacter.UseSanity(skill.sanityCost);
                break;
                
            case "Bola":
                // Check if target is a valid enemy
                if (target != null && target.isEnemy)
                {
                    Debug.Log($"[Skill Execution] Bola! {activeCharacter.name} is throwing a bola at {target.name}");
                    
                    // Calculate random damage between 2-4
                    float baseDamage = UnityEngine.Random.Range(2f, 4f);
                    float calculatedDamage = activeCharacter.CalculateDamage(baseDamage);
                    
                    // Deal damage
                    target.TakeDamage(calculatedDamage);
                    
                    // Apply SLOWED status to the target if not dead
                    if (!target.IsDead())
                    {
                        StatusManager bolaStatusMgr = StatusManager.Instance;
                        if (bolaStatusMgr != null)
                        {
                            // Apply Slowed status with the status system
                            bolaStatusMgr.ApplyStatus(target, StatusType.Slowed, 2);
                            Debug.Log($"[Skill Execution] Bola hit {target.name} for {calculatedDamage} damage and applied SLOWED for 2 turns");
                            
                            // Show message in the text panel
                            DisplayTurnAndActionMessage($"Hit for {calculatedDamage:F1} damage and applied SLOWED!");
                        }
                        else
                        {
                            // Fallback to direct modification if status manager not available
                            float baseActionSpeed = target.actionSpeed;
                            float newSpeed = baseActionSpeed * 0.5f; // 50% reduction
                            target.actionSpeed = newSpeed;
                            Debug.LogWarning($"[Skill Execution] Bola! StatusManager not found, applied direct speed reduction to {target.name}");
                        }
                    }
                    
                    // Use sanity
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] Bola! Invalid target: {target?.name ?? "null"}. This skill requires an enemy target.");
                }
                break;
                
            case "Cleansing Wave":
                Debug.Log($"[Skill Execution] Cleansing Wave! {activeCharacter.name} is removing negative status effects from self and allies");
                
                // Get all players
                var allPlayers = combatManager.players;
                
                // Get StatusManager instance
                StatusManager statusMgr = StatusManager.Instance;
                if (statusMgr != null)
                {
                    int clearedCount = 0;
                    
                    // Loop through all players including self
                    foreach (CombatStats player in allPlayers)
                    {
                        if (player != null && !player.IsDead())
                        {
                            // Clear negative status effects from this ally (including self)
                            statusMgr.ClearNegativeStatuses(player);
                            clearedCount++;
                            
                            Debug.Log($"[Skill Execution] Cleansing Wave cleared negative status effects from {player.characterName}");
                        }
                    }
                    
                    if (clearedCount > 0)
                    {
                        DisplayTurnAndActionMessage($"Cleared negative status effects from {clearedCount} party members!");
                    }
                    else
                    {
                        DisplayTurnAndActionMessage("No party members had negative status effects to clear!");
                    }
                }
                else
                {
                    Debug.LogWarning("[Skill Execution] Cleansing Wave! StatusManager not found. Skill had no effect.");
                }
                
                // Use sanity
                activeCharacter.UseSanity(skill.sanityCost);
                break;
                
            case "Respite":
                // Check if target is a valid ally (not an enemy)
                if (target != null && !target.isEnemy)
                {
                    Debug.Log($"[Skill Execution] Respite! {activeCharacter.name} is helping {target.name} rest");
                    
                    // Heal the target for 20 HP and 20 Mind
                    target.HealHealth(20f);
                    target.HealSanity(20f);
                    
                    // Apply SLOW status to target
                    StatusManager slowStatusMgr = StatusManager.Instance;
                    if (slowStatusMgr != null)
                    {
                        // Apply Slowed status with the status system
                        slowStatusMgr.ApplyStatus(target, StatusType.Slowed, 2);
                        Debug.Log($"[Skill Execution] Respite! {target.characterName} healed for 20 HP and 20 Mind, and is now SLOW for 2 turns");
                    }
                    else
                    {
                        // Fallback to direct modification if status manager not available
                        float baseActionSpeed = target.actionSpeed;
                        float newSpeed = baseActionSpeed * 0.5f; // 50% reduction
                        target.actionSpeed = newSpeed;
                        Debug.LogWarning($"[Skill Execution] Respite! StatusManager not found, applied direct speed reduction to {target.characterName}");
                    }
                    
                    // Use sanity
                    activeCharacter.UseSanity(skill.sanityCost);
                }
                else
                {
                    Debug.LogWarning($"[Skill Execution] Respite! Invalid target: {target?.name ?? "null"}. This skill requires an ally target.");
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
                
                // Heal the user for 40 HP
                activeCharacter.HealHealth(40f);
                
                // Apply TOUGH status to the user
                StatusManager fortifyStatusMgr = StatusManager.Instance;
                if (fortifyStatusMgr != null)
                {
                    // Apply Tough status with the status system
                    fortifyStatusMgr.ApplyStatus(activeCharacter, StatusType.Tough, 2);
                    Debug.Log($"[Skill Execution] Fortify! {activeCharacter.characterName} healed for 40 HP and gained Tough status for 2 turns");
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
        
        // Clear skill description when leaving skill menu
        ClearDescription();
        
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
        
        // Clear cycling system variables
        menuSelector.ClearCyclingSystem();
        
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
                    itemButtonsContainer.offsetMin = new Vector2(skillContainerPaddingLeft, skillContainerPaddingBottom);
                    itemButtonsContainer.offsetMax = new Vector2(-skillContainerPaddingRight, -skillContainerPaddingTop);
                    Debug.Log("[ItemButton Lifecycle] Created new ItemButtonsContainer");
                }
            }
            
            // Ensure the container has a vertical layout
            VerticalLayoutGroup itemVerticalLayout = itemButtonsContainer.GetComponent<VerticalLayoutGroup>();
            GridLayoutGroup existingItemGrid = itemButtonsContainer.GetComponent<GridLayoutGroup>();
            
            if (existingItemGrid != null)
            {
                DestroyImmediate(existingItemGrid);
            }
            
            if (itemVerticalLayout == null)
            {
                itemVerticalLayout = itemButtonsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                itemVerticalLayout.spacing = skillButtonSpacing;
                itemVerticalLayout.childAlignment = TextAnchor.UpperCenter;
                itemVerticalLayout.childControlWidth = true;
                itemVerticalLayout.childControlHeight = false;
                itemVerticalLayout.childForceExpandWidth = false;
                itemVerticalLayout.childForceExpandHeight = false;
                
                // Apply padding from skillContainerPadding settings
                itemVerticalLayout.padding = new RectOffset(
                    (int)skillContainerPaddingLeft,   // left
                    (int)skillContainerPaddingRight,  // right
                    (int)skillContainerPaddingTop,    // top
                    (int)skillContainerPaddingBottom  // bottom
                );
                
                // Don't add ContentSizeFitter - it causes infinite growth and breaks the fixed container size
                // Remove any existing ContentSizeFitter to ensure proper sizing
                ContentSizeFitter existingItemSizeFitter = itemButtonsContainer.GetComponent<ContentSizeFitter>();
                if (existingItemSizeFitter != null)
                {
                    Debug.Log("[ItemButton Lifecycle] Removing ContentSizeFitter that causes infinite growth");
                    DestroyImmediate(existingItemSizeFitter);
                }
            }
            
            // Create a runtime container for the buttons
            GameObject runtimeContainer = new GameObject("RuntimeItemButtons");
            RectTransform runtimeContainerRect = runtimeContainer.AddComponent<RectTransform>();
            runtimeContainerRect.SetParent(itemButtonsContainer, false);
            runtimeContainerRect.anchorMin = new Vector2(0, 0);
            runtimeContainerRect.anchorMax = new Vector2(1, 1);
            runtimeContainerRect.offsetMin = Vector2.zero;
            runtimeContainerRect.offsetMax = Vector2.zero;
            
            // Add a vertical layout to the runtime container
            VerticalLayoutGroup runtimeVerticalLayout = runtimeContainer.AddComponent<VerticalLayoutGroup>();
            runtimeVerticalLayout.spacing = skillButtonSpacing;
            runtimeVerticalLayout.childAlignment = TextAnchor.UpperCenter;
            runtimeVerticalLayout.childControlWidth = true;
            runtimeVerticalLayout.childControlHeight = false;
            runtimeVerticalLayout.childForceExpandWidth = false;
            runtimeVerticalLayout.childForceExpandHeight = false;
            
            // Apply padding from skillContainerPadding settings
            runtimeVerticalLayout.padding = new RectOffset(
                (int)skillContainerPaddingLeft,   // left
                (int)skillContainerPaddingRight,  // right
                (int)skillContainerPaddingTop,    // top
                (int)skillContainerPaddingBottom  // bottom
            );
            
            // Don't add ContentSizeFitter to the runtime container - it causes infinite growth
            // The container should respect the item menu's original size from the editor
            
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
                        buttonRect.sizeDelta = new Vector2(skillButtonWidth, skillButtonHeight);
                    }
                    
                    // Add LayoutElement to ensure consistent sizing in VerticalLayoutGroup
                    LayoutElement layoutElement = itemButton.GetComponent<LayoutElement>();
                    if (layoutElement == null)
                    {
                        layoutElement = itemButton.AddComponent<LayoutElement>();
                    }
                    layoutElement.minHeight = skillButtonHeight;
                    layoutElement.preferredHeight = skillButtonHeight;
                    
                    // Add a button component for click handling
                    Button button = itemButton.GetComponent<Button>();
                    if (button == null)
                    {
                        button = itemButton.AddComponent<Button>();
                    }
                    
                    // Store the item data for reference
                    ItemButtonData itemData = itemButton.AddComponent<ItemButtonData>();
                    itemData.item = item;
                    
                    // Add hover description handler for mouse support
                    HoverDescriptionHandler hoverHandler = itemButton.AddComponent<HoverDescriptionHandler>();
                    Debug.Log($"[ItemButton Lifecycle] HoverDescriptionHandler component added for item: {item.name}");
                    
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
                
                // Clear the cycling system since items use simple navigation
                menuSelector.ClearCyclingSystem();
                Debug.Log($"[ItemButton Lifecycle] Cleared cycling system for item menu navigation");
            }
        }
    }
    
    public void OnItemButtonClicked(ItemData item)
    {
        Debug.Log($"[DEBUG TARGETING] OnItemButtonClicked - Item: {item.name}, Amount: {item.amount}, RequiresTarget: {item.requiresTarget}");
        
        // Special handling for items that MUST have targets
        bool mustHaveTarget = 
            string.Equals(item.name, "Shiny Bead", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(item.name, "Super Espress-O", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.name, "Panacea", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.name, "Tower Shield", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.name, "Ramen", StringComparison.OrdinalIgnoreCase);
        
        // Force target selection for these items even if requiresTarget is false
        if (mustHaveTarget || item.requiresTarget)
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
                    Debug.LogError($"[DEBUG TARGETING] ERROR: Target is ally: {target.name} - Shiny Bead should only target enemies!");
                }
                else
                {
                    Debug.LogError($"[DEBUG TARGETING] No target specified for Shiny Bead, cannot execute - this should never happen!");
                }
                break;
                
            case "Panacea":
                Debug.Log($"[DEBUG TARGETING] Executing Panacea effect");
                if (target != null && !target.isEnemy)
                {
                    // Heal Health and Sanity
                    target.HealHealth(100f);
                    target.HealSanity(100f);
                    
                    // Remove negative status effects
                    StatusManager statusManager = StatusManager.Instance;
                    if (statusManager != null)
                    {
                        // Check and remove each negative status
                        if (statusManager.HasStatus(target, StatusType.Weakness))
                            statusManager.RemoveStatus(target, StatusType.Weakness);
                            
                        if (statusManager.HasStatus(target, StatusType.Vulnerable))
                            statusManager.RemoveStatus(target, StatusType.Vulnerable);
                            
                        if (statusManager.HasStatus(target, StatusType.Slowed))
                            statusManager.RemoveStatus(target, StatusType.Slowed);
                    }
                    
                    Debug.Log($"Panacea used: Healed {target.name} for 100 HP and 100 SP and removed negative status effects");
                }
                else if (target != null && target.isEnemy)
                {
                    Debug.Log($"[DEBUG TARGETING] ERROR: Target is enemy: {target.name} - Panacea should only target allies!");
                }
                else
                {
                    // Use on self if no target
                    activeCharacter.HealHealth(100f);
                    activeCharacter.HealSanity(100f);
                    
                    // Remove negative status effects
                    StatusManager statusManager = StatusManager.Instance;
                    if (statusManager != null)
                    {
                        if (statusManager.HasStatus(activeCharacter, StatusType.Weakness))
                            statusManager.RemoveStatus(activeCharacter, StatusType.Weakness);
                            
                        if (statusManager.HasStatus(activeCharacter, StatusType.Vulnerable))
                            statusManager.RemoveStatus(activeCharacter, StatusType.Vulnerable);
                            
                        if (statusManager.HasStatus(activeCharacter, StatusType.Slowed))
                            statusManager.RemoveStatus(activeCharacter, StatusType.Slowed);
                    }
                    
                    Debug.Log($"Panacea used: Healed {activeCharacter.name} for 100 HP and 100 SP and removed negative status effects");
                }
                break;
                
            case "Tower Shield":
                Debug.Log($"[DEBUG TARGETING] Executing Tower Shield effect");
                if (target != null && !target.isEnemy)
                {
                    // Apply TOUGH status for 3 turns
                    StatusManager statusManager = StatusManager.Instance;
                    if (statusManager != null)
                    {
                        statusManager.ApplyStatus(target, StatusType.Tough, 3);
                        Debug.Log($"Tower Shield used: Applied TOUGH status to {target.name} for 3 turns");
                    }
                }
                else if (target != null && target.isEnemy)
                {
                    Debug.Log($"[DEBUG TARGETING] ERROR: Target is enemy: {target.name} - Tower Shield should only target allies!");
                }
                else
                {
                    // Use on self if no target
                    StatusManager statusManager = StatusManager.Instance;
                    if (statusManager != null)
                    {
                        statusManager.ApplyStatus(activeCharacter, StatusType.Tough, 3);
                        Debug.Log($"Tower Shield used: Applied TOUGH status to {activeCharacter.name} for 3 turns");
                    }
                }
                break;
                
            case "Pocket Sand":
                Debug.Log($"[DEBUG TARGETING] Executing Pocket Sand effect");
                List<CombatStats> enemies = combatManager.GetLivingEnemies();
                
                if (enemies.Count > 0)
                {
                    // Apply WEAKNESS to all enemies
                    StatusManager statusManager = StatusManager.Instance;
                    if (statusManager != null)
                    {
                        foreach (var enemy in enemies)
                        {
                            statusManager.ApplyStatus(enemy, StatusType.Weakness, 3);
                            Debug.Log($"Pocket Sand used: Applied WEAKNESS status to {enemy.name} for 3 turns");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[DEBUG TARGETING] No enemies found for Pocket Sand");
                }
                break;
                
            case "Otherworldly Tome":
                Debug.Log($"[DEBUG TARGETING] Executing Otherworldly Tome effect");
                // Apply STRENGTH to all party members
                StatusManager strengthStatusManager = StatusManager.Instance;
                if (strengthStatusManager != null)
                {
                    foreach (var player in combatManager.players)
                    {
                        if (player != null && !player.IsDead())
                        {
                            strengthStatusManager.ApplyStatus(player, StatusType.Strength, 3);
                            Debug.Log($"Otherworldly Tome used: Applied STRENGTH status to {player.name} for 3 turns");
                        }
                    }
                }
                break;
                
            case "Unstable Catalyst":
                Debug.Log($"[DEBUG TARGETING] Executing Unstable Catalyst effect");
                List<CombatStats> catalystEnemies = combatManager.GetLivingEnemies();
                
                if (catalystEnemies.Count > 0)
                {
                    // Deal damage to all enemies
                    foreach (var enemy in catalystEnemies)
                    {
                        enemy.TakeDamage(40f);
                        Debug.Log($"Unstable Catalyst used: Dealt 40 damage to {enemy.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[DEBUG TARGETING] No enemies found for Unstable Catalyst");
                }
                break;
                
            case "Ramen":
                Debug.Log($"[DEBUG TARGETING] Executing Ramen effect");
                if (target != null && !target.isEnemy)
                {
                    // Heal target
                    target.HealHealth(15f);
                    Debug.Log($"Ramen used: Healed {target.name} for 15 HP");
                }
                else if (target != null && target.isEnemy)
                {
                    Debug.Log($"[DEBUG TARGETING] ERROR: Target is enemy: {target.name} - Ramen should only target allies!");
                }
                else
                {
                    // Use on self if no target
                    activeCharacter.HealHealth(15f);
                    Debug.Log($"Ramen used: Healed {activeCharacter.name} for 15 HP");
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
        
        // Clear skill/item description when leaving item menu
        ClearDescription();
        
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
            rectTransform.offsetMin = new Vector2(skillContainerPaddingLeft, skillContainerPaddingBottom);
            rectTransform.offsetMax = new Vector2(-skillContainerPaddingRight, -skillContainerPaddingTop);
            
            // Add vertical layout
            VerticalLayoutGroup verticalLayout = container.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = skillButtonSpacing;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = false;
            verticalLayout.childForceExpandHeight = false;
            
            // Apply padding from skillContainerPadding settings
            verticalLayout.padding = new RectOffset(
                (int)skillContainerPaddingLeft,   // left
                (int)skillContainerPaddingRight,  // right
                (int)skillContainerPaddingTop,    // top
                (int)skillContainerPaddingBottom  // bottom
            );
            
            // DO NOT add ContentSizeFitter - it causes the container to expand infinitely
            // The container must respect the item menu's original fixed size from the editor
            
            itemsContainer = container.transform;
            Debug.Log("Created new ItemsContainer with VerticalLayoutGroup");
        }
        
        // Add buttons for each non-KeyItem
        int filteredItems = 0;
        foreach (ItemData item in items)
        {
            // Final safety check - NEVER show KeyItems in combat
            if (item.IsKeyItem() || 
                item.type == ItemData.ItemType.KeyItem || 
                item.name == "Cold Key" || 
                item.name.Contains("Medallion") || 
                item.name.StartsWith("Medal"))
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
                
                // Add hover description handler for mouse support
                HoverDescriptionHandler hoverHandler = buttonObj.AddComponent<HoverDescriptionHandler>();
                Debug.Log($"[ItemButton Lifecycle] HoverDescriptionHandler component added for item: {item.name}");
                
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
            return;
            
        // Set the text
        actionDisplayText.text = actionText;
        
        // Show the label
        actionDisplayLabel.SetActive(true);
        
        // Pause game briefly to show the action
        StartCoroutine(ShowActionLabel(actionText));
    }
    
    private IEnumerator ShowActionLabel(string actionText)
    {
        // Pause the game
        Time.timeScale = 0;
        
        // Wait for the specified duration
        yield return new WaitForSecondsRealtime(actionDisplayDuration);
        
        // Resume the game
        Time.timeScale = 1;
        
        // Hide the label
        if (actionDisplayLabel != null)
        {
            actionDisplayLabel.SetActive(false);
        }
    }

    public void ShowTextPanel(string message)
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
        
        // Note: Removed opacity modification to respect original inspector settings
    }
    
    public void HideTextPanel()
    {
        if (textPanel != null)
        {
            textPanel.SetActive(false);
        }
    }
} 