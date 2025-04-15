using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OverworldMenuUI : MonoBehaviour
{
    [Header("Menu Button Colors")]
    [SerializeField] private Color defaultButtonColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color highlightedButtonColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);

    // Private references - no fields exposed in Inspector
    private GameObject itemsMenu;
    private GameObject charactersMenu;
    
    private GameObject magicianPanel;
    private GameObject fighterPanel;
    private GameObject bardPanel;
    private GameObject rangerPanel;
    
    private GameObject navigationPanel;
    private Button characterMenuButton;
    private Button inventoryMenuButton;
    private KeyCode menuToggleKey = KeyCode.Tab;
    
    private Dictionary<GameObject, TextMeshProUGUI> characterNameTexts = new Dictionary<GameObject, TextMeshProUGUI>();
    private Dictionary<GameObject, TextMeshProUGUI> healthTexts = new Dictionary<GameObject, TextMeshProUGUI>();
    private Dictionary<GameObject, TextMeshProUGUI> mindTexts = new Dictionary<GameObject, TextMeshProUGUI>();
    private Dictionary<GameObject, Slider> healthSliders = new Dictionary<GameObject, Slider>();
    private Dictionary<GameObject, Slider> mindSliders = new Dictionary<GameObject, Slider>();
    
    private void Awake()
    {
        Debug.Log("[OverworldMenuUI] Initializing...");
        FindAllMenuComponents();
    }

    private void Start()
    {
        // In case some references weren't found in Awake, try again
        if (characterMenuButton == null || inventoryMenuButton == null)
        {
            Debug.Log("[OverworldMenuUI] Retrying component initialization in Start...");
            FindAllMenuComponents();
        }
    }

    private void FindAllMenuComponents()
    {
        Debug.Log("[OverworldMenuUI] Finding all menu components...");
        
        // Find menus first
        Transform menuPanel = transform.Find("MenuPanel");
        if (menuPanel != null)
        {
            Debug.Log($"[OverworldMenuUI] Found MenuPanel with {menuPanel.childCount} children");
            
            // Find the menus
            itemsMenu = menuPanel.Find("ItemsMenu")?.gameObject;
            charactersMenu = menuPanel.Find("CharactersMenu")?.gameObject;
            
            // Try to find character panels
            if (charactersMenu != null) 
            {
                Debug.Log($"[OverworldMenuUI] Finding character panels in {charactersMenu.name}");
                FindExistingCharacterPanels();
                
                // If panels not found, create them
                if (magicianPanel == null || fighterPanel == null || bardPanel == null || rangerPanel == null)
                {
                    Debug.Log("[OverworldMenuUI] Some character panels missing, creating them...");
                    CreateCharacterPanelsIfNeeded();
                }
                
                // Initialize the panels
                Debug.Log("[OverworldMenuUI] Initializing character panels with UI components...");
                if (magicianPanel != null) InitializeCharacterPanel(magicianPanel, "Magician");
                if (fighterPanel != null) InitializeCharacterPanel(fighterPanel, "Fighter");
                if (bardPanel != null) InitializeCharacterPanel(bardPanel, "Bard");
                if (rangerPanel != null) InitializeCharacterPanel(rangerPanel, "Ranger");
                
                // Log debug info about panels
                LogDebugInfo();
            }
            else
            {
                Debug.LogError("[OverworldMenuUI] CharactersMenu not found!");
            }
            
            // Find NavBar and buttons
            Transform navBar = menuPanel.Find("NavBar");
            if (navBar != null)
            {
                Debug.Log($"[OverworldMenuUI] Found NavBar with {navBar.childCount} children");
                navigationPanel = navBar.gameObject;
                
                // List all children in NavBar for debugging
                for (int i = 0; i < navBar.childCount; i++)
                {
                    Transform child = navBar.GetChild(i);
                    Debug.Log($"[OverworldMenuUI] NavBar child {i}: {child.name}");
                }
                
                // Try multiple possible button names
                string[] characterButtonNames = { "CharactersButton", "Characters Button", "CharacterButton", "Character Button" };
                string[] inventoryButtonNames = { "InventoryButton", "Inventory Button", "ItemsButton", "Items Button" };
                
                // Try to find character button
                foreach (string buttonName in characterButtonNames)
                {
                    Transform buttonTransform = navBar.Find(buttonName);
                    if (buttonTransform != null)
                    {
                        characterMenuButton = buttonTransform.GetComponent<Button>();
                        if (characterMenuButton != null)
                        {
                            Debug.Log($"[OverworldMenuUI] Found character button: {buttonName}");
                            break;
                        }
                    }
                }
                
                // Try to find inventory button
                foreach (string buttonName in inventoryButtonNames)
                {
                    Transform buttonTransform = navBar.Find(buttonName);
                    if (buttonTransform != null)
                    {
                        inventoryMenuButton = buttonTransform.GetComponent<Button>();
                        if (inventoryMenuButton != null)
                        {
                            Debug.Log($"[OverworldMenuUI] Found inventory button: {buttonName}");
                            break;
                        }
                    }
                }
                
                // If buttons still not found, try to find by component type
                if (characterMenuButton == null || inventoryMenuButton == null)
                {
                    Debug.LogWarning("[OverworldMenuUI] Could not find buttons by name, trying to find by component type...");
                    
                    Button[] buttons = navBar.GetComponentsInChildren<Button>();
                    foreach (Button btn in buttons)
                    {
                        string btnName = btn.gameObject.name.ToLower();
                        Debug.Log($"[OverworldMenuUI] Found button: {btn.gameObject.name}");
                        
                        if (characterMenuButton == null && 
                            (btnName.Contains("character") || btnName.Contains("char")))
                        {
                            characterMenuButton = btn;
                            Debug.Log("[OverworldMenuUI] Found Characters button by name contains!");
                        }
                        else if (inventoryMenuButton == null && 
                                 (btnName.Contains("item") || btnName.Contains("inventory")))
                        {
                            inventoryMenuButton = btn;
                            Debug.Log("[OverworldMenuUI] Found Items button by name contains!");
                        }
                    }
                }
                
                // If still not found, create them
                if (characterMenuButton == null || inventoryMenuButton == null)
                {
                    Debug.LogWarning("[OverworldMenuUI] Buttons not found, creating new ones...");
                    CreateNavigationPanelIfNeeded();
                }
            }
            else
            {
                Debug.LogError("[OverworldMenuUI] NavBar not found in MenuPanel!");
                CreateNavigationPanelIfNeeded();
            }
        }
        else
        {
            Debug.LogError("[OverworldMenuUI] MenuPanel not found!");
        }
        
        // Setup button callbacks if buttons were found
        if (characterMenuButton != null && inventoryMenuButton != null)
        {
            SetupButtonCallbacks();
        }
        
        // Make sure charactersMenu is active by default and itemsMenu is inactive
        if (charactersMenu != null && itemsMenu != null)
        {
            Debug.Log("[OverworldMenuUI] Setting charactersMenu active by default");
            charactersMenu.SetActive(true);
            itemsMenu.SetActive(false);
            
            // Update button visuals to reflect default state
            if (characterMenuButton != null && inventoryMenuButton != null)
            {
                Image charButtonImage = characterMenuButton.GetComponent<Image>();
                if (charButtonImage != null)
                {
                    charButtonImage.color = highlightedButtonColor;
                }
                
                Image invButtonImage = inventoryMenuButton.GetComponent<Image>();
                if (invButtonImage != null)
                {
                    invButtonImage.color = defaultButtonColor;
                }
                
                Debug.Log("[OverworldMenuUI] Button colors set to reflect default active menu");
            }
        }
    }
    
    private void LogDebugInfo()
    {
        Debug.Log("[OverworldMenuUI] === DEBUG INFORMATION ===");
        Debug.Log($"[OverworldMenuUI] CharactersMenu found: {charactersMenu != null}");
        Debug.Log($"[OverworldMenuUI] ItemsMenu found: {itemsMenu != null}");
        
        // Log character panels
        Debug.Log($"[OverworldMenuUI] Magician panel: {(magicianPanel != null ? magicianPanel.name : "NULL")}");
        Debug.Log($"[OverworldMenuUI] Fighter panel: {(fighterPanel != null ? fighterPanel.name : "NULL")}");
        Debug.Log($"[OverworldMenuUI] Bard panel: {(bardPanel != null ? bardPanel.name : "NULL")}");
        Debug.Log($"[OverworldMenuUI] Ranger panel: {(rangerPanel != null ? rangerPanel.name : "NULL")}");
        
        // Log slider references
        LogSliderInfo(magicianPanel, "Magician");
        LogSliderInfo(fighterPanel, "Fighter");
        LogSliderInfo(bardPanel, "Bard");
        LogSliderInfo(rangerPanel, "Ranger");
    }
    
    private void LogSliderInfo(GameObject panel, string characterName)
    {
        if (panel == null)
        {
            Debug.LogWarning($"[OverworldMenuUI] {characterName} panel is NULL - can't log slider info");
            return;
        }
        
        // Check for HP slider
        Slider hpSlider = null;
        Image hpFillImage = null;
        if (healthSliders.TryGetValue(panel, out hpSlider))
        {
            hpFillImage = hpSlider.fillRect?.GetComponent<Image>();
            
            Debug.Log($"[OverworldMenuUI] {characterName} HP Slider: {(hpSlider != null ? hpSlider.name : "NULL")}");
            Debug.Log($"[OverworldMenuUI] {characterName} HP FillRect: {(hpSlider != null && hpSlider.fillRect != null ? hpSlider.fillRect.name : "NULL")}");
            Debug.Log($"[OverworldMenuUI] {characterName} HP Fill Image: {(hpFillImage != null ? "Found" : "NULL")}");
            
            if (hpFillImage != null)
            {
                Debug.Log($"[OverworldMenuUI] {characterName} HP Fill Color: R={hpFillImage.color.r:F2}, G={hpFillImage.color.g:F2}, B={hpFillImage.color.b:F2}, A={hpFillImage.color.a:F2}");
            }
        }
        else
        {
            Debug.LogWarning($"[OverworldMenuUI] {characterName} HP Slider not in dictionary!");
        }
        
        // Check for Mind slider
        Slider mindSlider = null;
        Image mindFillImage = null;
        if (mindSliders.TryGetValue(panel, out mindSlider))
        {
            mindFillImage = mindSlider.fillRect?.GetComponent<Image>();
            
            Debug.Log($"[OverworldMenuUI] {characterName} Mind Slider: {(mindSlider != null ? mindSlider.name : "NULL")}");
            Debug.Log($"[OverworldMenuUI] {characterName} Mind FillRect: {(mindSlider != null && mindSlider.fillRect != null ? mindSlider.fillRect.name : "NULL")}");
            Debug.Log($"[OverworldMenuUI] {characterName} Mind Fill Image: {(mindFillImage != null ? "Found" : "NULL")}");
            
            if (mindFillImage != null)
            {
                Debug.Log($"[OverworldMenuUI] {characterName} Mind Fill Color: R={mindFillImage.color.r:F2}, G={mindFillImage.color.g:F2}, B={mindFillImage.color.b:F2}, A={mindFillImage.color.a:F2}");
            }
        }
        else
        {
            Debug.LogWarning($"[OverworldMenuUI] {characterName} Mind Slider not in dictionary!");
        }
        
        // Check for existing children with HP or Mind bar-like names (might be missing our detection)
        foreach (Transform child in panel.transform)
        {
            if (child.name.Contains("HP") || child.name.Contains("Health") || child.name.Contains("Mind") || 
                child.name.Contains("Mana") || child.name.Contains("Bar"))
            {
                Debug.Log($"[OverworldMenuUI] {characterName} has child named: {child.name}");
                
                // Check if this has a slider component
                Slider childSlider = child.GetComponent<Slider>();
                if (childSlider != null)
                {
                    Debug.Log($"[OverworldMenuUI] {characterName}'s {child.name} has Slider component");
                    Debug.Log($"[OverworldMenuUI] {characterName}'s {child.name} fillRect: {(childSlider.fillRect != null ? childSlider.fillRect.name : "NULL")}");
                    
                    // Check fill image
                    Image childFillImage = childSlider.fillRect?.GetComponent<Image>();
                    if (childFillImage != null)
                    {
                        Debug.Log($"[OverworldMenuUI] {characterName}'s {child.name} fill color: R={childFillImage.color.r:F2}, G={childFillImage.color.g:F2}, B={childFillImage.color.b:F2}, A={childFillImage.color.a:F2}");
                    }
                }
            }
        }
    }
    
    private void Update()
    {
        Debug.Log("[OverworldMenuUI] Update method called");
        
        // Debug state before UpdateCharacterStats()
        Debug.Log($"[OverworldMenuUI] PRE-UPDATE STATUS: charactersMenu exists: {charactersMenu != null}, " + 
                 $"charactersMenu active: {charactersMenu != null && charactersMenu.activeSelf}, " +
                 $"PersistentGameManager.Instance exists: {PersistentGameManager.Instance != null}, " +
                 $"Character panels initialized: Magician={magicianPanel != null}, Fighter={fighterPanel != null}, " +
                 $"Bard={bardPanel != null}, Ranger={rangerPanel != null}");
        
        // Toggle between menus when the toggle key is pressed
        if (Input.GetKeyDown(menuToggleKey))
        {
            if (charactersMenu.activeSelf)
            {
                ShowItemsMenu();
            }
            else
            {
                ShowCharactersMenu();
            }
        }
        
        // Update character stats every frame
        UpdateCharacterStats();
        
        // Log if character panels have sliders
        if (Time.frameCount % 300 == 0) // Only log every 300 frames to avoid spamming
        {
            Debug.Log($"[OverworldMenuUI] PANEL SLIDERS STATUS: " +
                     $"Magician HP Slider: {(magicianPanel != null && healthSliders.ContainsKey(magicianPanel))}, " +
                     $"Fighter HP Slider: {(fighterPanel != null && healthSliders.ContainsKey(fighterPanel))}, " +
                     $"Bard HP Slider: {(bardPanel != null && healthSliders.ContainsKey(bardPanel))}, " +
                     $"Ranger HP Slider: {(rangerPanel != null && healthSliders.ContainsKey(rangerPanel))}");
        }
    }
    
    private void FindMenus()
    {
        // Since this script is attached to the Menu Canvas, we can start from there
        Debug.Log("[OverworldMenuUI] Starting menu search from " + gameObject.name);
        
        // List all children for debugging
        for (int i = 0; i < transform.childCount; i++)
        {
            Debug.Log($"[OverworldMenuUI] Child {i}: {transform.GetChild(i).name}");
        }

        // Find the MenuPanel (direct child of Menu Canvas)
        Transform menuPanel = transform.GetChild(0);
        if (menuPanel == null)
        {
            Debug.LogWarning("[OverworldMenuUI] Menu Canvas has no children!");
            return;
        }
        
        Debug.Log($"[OverworldMenuUI] Found panel: {menuPanel.name}");
        
        // List all MenuPanel children for debugging
        for (int i = 0; i < menuPanel.childCount; i++)
        {
            Debug.Log($"[OverworldMenuUI] MenuPanel child {i}: {menuPanel.GetChild(i).name}");
        }
        
        // Find the menus as children of MenuPanel - more permissive naming
        foreach (Transform child in menuPanel)
        {
            string childName = child.name.ToLower();
            
            if (childName.Contains("character") || childName.Contains("char"))
            {
                charactersMenu = child.gameObject;
                Debug.Log($"[OverworldMenuUI] Found CharactersMenu: {child.name}");
            }
            else if (childName.Contains("item") || childName.Contains("inventory"))
            {
                itemsMenu = child.gameObject;
                Debug.Log($"[OverworldMenuUI] Found ItemsMenu: {child.name}");
            }
        }
        
        if (charactersMenu == null)
        {
            // Try to find by index if the MenuPanel has at least 2-3 children
            if (menuPanel.childCount >= 2)
            {
                // Typically character menu is at index 1 or 2
                if (menuPanel.childCount >= 3)
                {
                    charactersMenu = menuPanel.GetChild(2).gameObject;
                    Debug.Log($"[OverworldMenuUI] Assigned CharactersMenu by index (2): {charactersMenu.name}");
                }
                else
                {
                    charactersMenu = menuPanel.GetChild(1).gameObject;
                    Debug.Log($"[OverworldMenuUI] Assigned CharactersMenu by index (1): {charactersMenu.name}");
                }
            }
            else
            {
                Debug.LogWarning("[OverworldMenuUI] Could not find CharactersMenu");
                return;
            }
        }
        
        // If found, look at the CharactersMenu hierarchy
        Debug.Log($"[OverworldMenuUI] CharactersMenu has {charactersMenu.transform.childCount} children");
        for (int i = 0; i < charactersMenu.transform.childCount; i++)
        {
            Debug.Log($"[OverworldMenuUI] CharactersMenu child {i}: {charactersMenu.transform.GetChild(i).name}");
        }
        
        // If CharactersMenu has exactly 4 children, they're probably the character panels
        if (charactersMenu.transform.childCount == 4)
        {
            magicianPanel = charactersMenu.transform.GetChild(0).gameObject;
            fighterPanel = charactersMenu.transform.GetChild(1).gameObject;
            bardPanel = charactersMenu.transform.GetChild(2).gameObject;
            rangerPanel = charactersMenu.transform.GetChild(3).gameObject;
            
            Debug.Log($"[OverworldMenuUI] Found character panels directly as children: {magicianPanel.name}, {fighterPanel.name}, {bardPanel.name}, {rangerPanel.name}");
        }
        else if (charactersMenu.transform.childCount > 0)
        {
            // Look for a container panel that might hold the character panels
            Transform characterPanel = null;
            foreach (Transform child in charactersMenu.transform)
            {
                if (child.name.Contains("Character") || child.name.Contains("Panel") || child.name.Contains("Grid"))
                {
                    characterPanel = child;
                    Debug.Log($"[OverworldMenuUI] Found character container panel: {characterPanel.name}");
                    break;
                }
            }
            
            // If found, check its children
            if (characterPanel != null && characterPanel.childCount >= 4)
            {
                magicianPanel = characterPanel.GetChild(0).gameObject;
                fighterPanel = characterPanel.GetChild(1).gameObject;
                bardPanel = characterPanel.GetChild(2).gameObject;
                rangerPanel = characterPanel.GetChild(3).gameObject;
                
                Debug.Log($"[OverworldMenuUI] Found character panels: {magicianPanel.name}, {fighterPanel.name}, {bardPanel.name}, {rangerPanel.name}");
            }
            else if (characterPanel != null)
            {
                Debug.LogWarning($"[OverworldMenuUI] Character container panel has only {characterPanel.childCount} children, expected 4");
            }
            else 
            {
                // Just use the first child of charactersMenu as a last resort
                Transform firstChild = charactersMenu.transform.GetChild(0);
                Debug.Log($"[OverworldMenuUI] Using first child as character panel: {firstChild.name}");
                
                // Check if this is a container with 4 children
                if (firstChild.childCount >= 4)
                {
                    magicianPanel = firstChild.GetChild(0).gameObject;
                    fighterPanel = firstChild.GetChild(1).gameObject;
                    bardPanel = firstChild.GetChild(2).gameObject;
                    rangerPanel = firstChild.GetChild(3).gameObject;
                    
                    Debug.Log($"[OverworldMenuUI] Found character panels from first child: {magicianPanel.name}, {fighterPanel.name}, {bardPanel.name}, {rangerPanel.name}");
                }
            }
        }
    }
    
    private void CreateCharacterPanelsIfNeeded()
    {
        // First, try to find existing character panels
        FindExistingCharacterPanels();
        
        // Create any missing panels
        if (magicianPanel == null) magicianPanel = CreateCharacterPanel("MagicianPanel", 0);
        if (fighterPanel == null) fighterPanel = CreateCharacterPanel("FighterPanel", 1);
        if (bardPanel == null) bardPanel = CreateCharacterPanel("BardPanel", 2);
        if (rangerPanel == null) rangerPanel = CreateCharacterPanel("RangerPanel", 3);
    }
    
    private void FindExistingCharacterPanels()
    {
        if (charactersMenu == null) return;
        
        Debug.Log($"[OverworldMenuUI] Looking for character panels in {charactersMenu.name}");
        
        // Look for panels by name
        foreach (Transform child in charactersMenu.transform)
        {
            Debug.Log($"[OverworldMenuUI] Checking child: {child.name}");
            
            string childName = child.name.ToLower();
            
            if (childName.Contains("magician") || childName == "characterpanel" || childName == "characterpanel (0)")
            {
                magicianPanel = child.gameObject;
                Debug.Log($"[OverworldMenuUI] Assigned {child.name} as Magician panel");
            }
            else if (childName.Contains("fighter") || childName == "characterpanel (1)")
            {
                fighterPanel = child.gameObject;
                Debug.Log($"[OverworldMenuUI] Assigned {child.name} as Fighter panel");
            }
            else if (childName.Contains("bard") || childName == "characterpanel (2)")
            {
                bardPanel = child.gameObject;
                Debug.Log($"[OverworldMenuUI] Assigned {child.name} as Bard panel");
            }
            else if (childName.Contains("ranger") || childName == "characterpanel (3)")
            {
                rangerPanel = child.gameObject;
                Debug.Log($"[OverworldMenuUI] Assigned {child.name} as Ranger panel");
            }
        }
        
        // If we still don't have 4 panels and there are exactly 4 children of charactersMenu
        // that could be our panels, assign them in order
        if ((magicianPanel == null || fighterPanel == null || bardPanel == null || rangerPanel == null) 
            && charactersMenu.transform.childCount == 4)
        {
            Debug.Log("[OverworldMenuUI] Assigning character panels by index...");
            
            int panelIndex = 0;
            foreach (Transform child in charactersMenu.transform)
            {
                if (child.name.Contains("Character") || child.name.Contains("Panel"))
                {
                    if (panelIndex == 0 && magicianPanel == null)
                    {
                        magicianPanel = child.gameObject;
                        Debug.Log($"[OverworldMenuUI] Assigned {child.name} as Magician panel by index");
                    }
                    else if (panelIndex == 1 && fighterPanel == null)
                    {
                        fighterPanel = child.gameObject;
                        Debug.Log($"[OverworldMenuUI] Assigned {child.name} as Fighter panel by index");
                    }
                    else if (panelIndex == 2 && bardPanel == null)
                    {
                        bardPanel = child.gameObject;
                        Debug.Log($"[OverworldMenuUI] Assigned {child.name} as Bard panel by index");
                    }
                    else if (panelIndex == 3 && rangerPanel == null)
                    {
                        rangerPanel = child.gameObject;
                        Debug.Log($"[OverworldMenuUI] Assigned {child.name} as Ranger panel by index");
                    }
                    
                    panelIndex++;
                }
            }
        }
    }
    
    private GameObject CreateCharacterPanel(string panelName, int index)
    {
        if (charactersMenu == null) return null;
        
        Debug.Log($"[OverworldMenuUI] Creating new character panel: {panelName}");
        
        GameObject panel = new GameObject(panelName);
        panel.transform.SetParent(charactersMenu.transform, false);
        
        // Add RectTransform
        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        
        // Calculate position based on index (2x2 grid layout)
        float panelWidth = 180f;
        float panelHeight = 120f;
        float spacing = 20f;
        
        float x = (index % 2 == 0) ? -panelWidth/2 - spacing/2 : panelWidth/2 + spacing/2;
        float y = (index < 2) ? panelHeight/2 + spacing/2 : -panelHeight/2 - spacing/2;
        
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(panelWidth, panelHeight);
        rectTransform.anchoredPosition = new Vector2(x, y);
        
        // Add Image component (background)
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        return panel;
    }
    
    private void CreateNavigationPanelIfNeeded()
    {
        // First try to find an existing navigation panel
        FindNavigationPanel();
        
        // If not found, create one
        if (navigationPanel == null)
        {
            CreateNavigationPanel();
        }
    }
    
    private void FindNavigationPanel()
    {
        Debug.Log("[OverworldMenuUI] Looking for navigation panel and buttons...");
        
        // First, look for the MenuPanel
        Transform menuPanel = transform.Find("MenuPanel");
        if (menuPanel == null)
        {
            Debug.LogWarning("[OverworldMenuUI] Could not find MenuPanel directly, trying GetChild(0)");
            menuPanel = transform.GetChild(0);
            if (menuPanel == null)
            {
                Debug.LogError("[OverworldMenuUI] Menu Canvas has no children!");
                return;
            }
        }
        
        Debug.Log($"[OverworldMenuUI] Found MenuPanel: {menuPanel.name}");
        
        // Look for NavBar
        navigationPanel = menuPanel.Find("NavBar")?.gameObject;
        if (navigationPanel == null)
        {
            Debug.LogError("[OverworldMenuUI] Could not find NavBar!");
            // List all children of MenuPanel to help debug
            for (int i = 0; i < menuPanel.childCount; i++)
            {
                Debug.Log($"[OverworldMenuUI] MenuPanel child {i}: {menuPanel.GetChild(i).name}");
            }
            return;
        }
        
        Debug.Log($"[OverworldMenuUI] Found NavBar with {navigationPanel.transform.childCount} children");
        
        // List all children in NavBar
        for (int i = 0; i < navigationPanel.transform.childCount; i++)
        {
            Debug.Log($"[OverworldMenuUI] NavBar child {i}: {navigationPanel.transform.GetChild(i).name}");
        }
        
        // Find buttons by their exact names (note the case sensitivity)
        Transform charactersButtonTransform = navigationPanel.transform.Find("Characters Button");
        Transform itemsButtonTransform = navigationPanel.transform.Find("Items button");
        
        if (charactersButtonTransform != null)
        {
            characterMenuButton = charactersButtonTransform.GetComponent<Button>();
            if (characterMenuButton == null)
            {
                Debug.Log("[OverworldMenuUI] Adding Button component to Characters Button");
                characterMenuButton = charactersButtonTransform.gameObject.AddComponent<Button>();
            }
            Debug.Log("[OverworldMenuUI] Found Characters Button!");
        }
        else
        {
            Debug.LogError("[OverworldMenuUI] Could not find Characters Button!");
        }
        
        if (itemsButtonTransform != null)
        {
            inventoryMenuButton = itemsButtonTransform.GetComponent<Button>();
            if (inventoryMenuButton == null)
            {
                Debug.Log("[OverworldMenuUI] Adding Button component to Items button");
                inventoryMenuButton = itemsButtonTransform.gameObject.AddComponent<Button>();
            }
            Debug.Log("[OverworldMenuUI] Found Items button!");
        }
        else
        {
            Debug.LogError("[OverworldMenuUI] Could not find Items button!");
        }
        
        // If buttons still not found, try case-insensitive search
        if (characterMenuButton == null || inventoryMenuButton == null)
        {
            Debug.Log("[OverworldMenuUI] Some buttons not found, trying case-insensitive search...");
            foreach (Transform child in navigationPanel.transform)
            {
                string childName = child.name.ToLower();
                if (childName.Contains("characters"))
                {
                    characterMenuButton = child.GetComponent<Button>();
                    if (characterMenuButton == null)
                    {
                        characterMenuButton = child.gameObject.AddComponent<Button>();
                    }
                    Debug.Log($"[OverworldMenuUI] Found Characters button by partial match: {child.name}");
                }
                else if (childName.Contains("items") || childName.Contains("inventory"))
                {
                    inventoryMenuButton = child.GetComponent<Button>();
                    if (inventoryMenuButton == null)
                    {
                        inventoryMenuButton = child.gameObject.AddComponent<Button>();
                    }
                    Debug.Log($"[OverworldMenuUI] Found Items button by partial match: {child.name}");
                }
            }
        }
        
        // Final status report
        Debug.Log($"[OverworldMenuUI] Button search results:");
        Debug.Log($"- Characters Button: {(characterMenuButton != null ? characterMenuButton.gameObject.name : "NULL")}");
        Debug.Log($"- Items Button: {(inventoryMenuButton != null ? inventoryMenuButton.gameObject.name : "NULL")}");
        
        if (characterMenuButton != null)
        {
            Image charImage = characterMenuButton.GetComponent<Image>();
            if (charImage != null)
            {
                Debug.Log($"[OverworldMenuUI] Characters button current color: {charImage.color}");
            }
            else
            {
                Debug.LogWarning("[OverworldMenuUI] Characters button has no Image component!");
            }
        }
        
        if (inventoryMenuButton != null)
        {
            Image invImage = inventoryMenuButton.GetComponent<Image>();
            if (invImage != null)
            {
                Debug.Log($"[OverworldMenuUI] Items button current color: {invImage.color}");
            }
            else
            {
                Debug.LogWarning("[OverworldMenuUI] Items button has no Image component!");
            }
        }
    }
    
    private void CreateNavigationPanel()
    {
        // Find the parent MenuPanel
        Transform menuPanel = transform.Find("MenuPanel");
        if (menuPanel == null)
        {
            Transform menuCanvas = transform;
            if (menuCanvas.name != "Menu Canvas")
            {
                menuCanvas = transform.Find("Menu Canvas");
                if (menuCanvas == null) return;
            }
            
            menuPanel = menuCanvas.Find("MenuPanel");
            if (menuPanel == null) return;
        }
        
        // Create navigation panel
        navigationPanel = new GameObject("NavigationPanel");
        navigationPanel.transform.SetParent(menuPanel, false);
        
        // Set up RectTransform
        RectTransform rectTransform = navigationPanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0.9f);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Add horizontal layout group
        HorizontalLayoutGroup layoutGroup = navigationPanel.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 10;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.padding = new RectOffset(10, 10, 5, 5);
        
        // Create character menu button
        characterMenuButton = CreateButton("Characters", navigationPanel.transform);
        
        // Create inventory menu button
        inventoryMenuButton = CreateButton("Inventory", navigationPanel.transform);
    }
    
    private void SetupButtonCallbacks()
    {
        if (characterMenuButton != null)
        {
            // Clear existing listeners
            characterMenuButton.onClick.RemoveAllListeners();
            characterMenuButton.onClick.AddListener(ShowCharactersMenu);
        }
        
        if (inventoryMenuButton != null)
        {
            // Clear existing listeners
            inventoryMenuButton.onClick.RemoveAllListeners();
            inventoryMenuButton.onClick.AddListener(ShowItemsMenu);
        }
    }
    
    private Button CreateButton(string text, Transform parent)
    {
        GameObject buttonObj = new GameObject(text + "Button");
        buttonObj.transform.SetParent(parent, false);
        
        // Set up RectTransform
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 30);
        
        // Add image component (button background)
        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // Add button component
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        
        // Set up button colors using the serialized fields
        ColorBlock colors = button.colors;
        colors.normalColor = defaultButtonColor;
        colors.highlightedColor = new Color(
            highlightedButtonColor.r + 0.1f,
            highlightedButtonColor.g + 0.1f,
            highlightedButtonColor.b + 0.1f,
            highlightedButtonColor.a
        );
        colors.pressedColor = new Color(
            defaultButtonColor.r - 0.1f,
            defaultButtonColor.g - 0.1f,
            defaultButtonColor.b - 0.1f,
            defaultButtonColor.a
        );
        colors.selectedColor = highlightedButtonColor;
        button.colors = colors;
        
        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRectTransform = textObj.AddComponent<RectTransform>();
        textRectTransform.anchorMin = Vector2.zero;
        textRectTransform.anchorMax = Vector2.one;
        textRectTransform.offsetMin = Vector2.zero;
        textRectTransform.offsetMax = Vector2.zero;
        
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 14;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        
        return button;
    }
    
    private void InitializeCharacterPanel(GameObject panel, string characterName)
    {
        if (panel == null)
        {
            Debug.LogWarning($"[OverworldMenuUI] Cannot initialize {characterName} panel: panel is null");
            return;
        }
        
        Debug.Log($"[OverworldMenuUI] Initializing {characterName} panel: {panel.name}");
        
        // Skip creating the name text component to avoid white text with character names
        // We still keep the dictionary entry but don't create or set the text
        TextMeshProUGUI nameText = panel.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            // If it already exists, disable it
            nameText.gameObject.SetActive(false);
        }
        
        // Add a reference to the dictionary with null to maintain code compatibility
        characterNameTexts[panel] = null;

        // Get or create health text component
        TextMeshProUGUI healthText = panel.transform.Find("HealthText")?.GetComponent<TextMeshProUGUI>();
        if (healthText == null)
        {
            Debug.Log($"[OverworldMenuUI] Creating HealthText for {characterName}");
            GameObject healthTextObj = new GameObject("HealthText");
            healthTextObj.transform.SetParent(panel.transform, false);
            
            RectTransform rectTransform = healthTextObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(1, 0.7f);
            rectTransform.offsetMin = new Vector2(10, 0);
            rectTransform.offsetMax = new Vector2(-10, 0);
            
            healthText = healthTextObj.AddComponent<TextMeshProUGUI>();
            healthText.fontSize = 25;
            healthText.alignment = TextAlignmentOptions.Left;
            healthText.color = Color.white;
        }
        else
        {
            healthText.fontSize = 25;
        }
        
        healthTexts[panel] = healthText;

        // Get or create mind text component
        TextMeshProUGUI mindText = panel.transform.Find("MindText")?.GetComponent<TextMeshProUGUI>();
        if (mindText == null)
        {
            Debug.Log($"[OverworldMenuUI] Creating MindText for {characterName}");
            GameObject mindTextObj = new GameObject("MindText");
            mindTextObj.transform.SetParent(panel.transform, false);
            
            RectTransform rectTransform = mindTextObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.2f);
            rectTransform.anchorMax = new Vector2(1, 0.4f);
            rectTransform.offsetMin = new Vector2(10, 0);
            rectTransform.offsetMax = new Vector2(-10, 0);
            
            mindText = mindTextObj.AddComponent<TextMeshProUGUI>();
            mindText.fontSize = 25;
            mindText.alignment = TextAlignmentOptions.Left;
            mindText.color = Color.white;
        }
        else
        {
            mindText.fontSize = 25;
        }
        
        mindTexts[panel] = mindText;
        
        // Find or create HP slider
        Debug.Log($"[OverworldMenuUI] Searching for HP slider for {characterName}");
        Slider hpSlider = panel.transform.Find("HPBar")?.GetComponent<Slider>();
        if (hpSlider == null)
        {
            Debug.Log($"[OverworldMenuUI] No 'HPBar' found, checking for 'HP Bar'");
            hpSlider = panel.transform.Find("HP Bar")?.GetComponent<Slider>();
        }
        
        // Try harder to find the HP bar with a wider search
        if (hpSlider == null)
        {
            foreach (Transform child in panel.transform)
            {
                Debug.Log($"[OverworldMenuUI] Checking child for HP bar: {child.name}");
                if (child.name.Contains("HP") || child.name.Contains("Health") || child.name.ToLower().Contains("health"))
                {
                    Slider childSlider = child.GetComponent<Slider>();
                    if (childSlider != null)
                    {
                        hpSlider = childSlider;
                        Debug.Log($"[OverworldMenuUI] Found HP slider in child: {child.name}");
                        break;
                    }
                }
            }
        }
        
        if (hpSlider == null)
        {
            Debug.Log($"[OverworldMenuUI] Creating new HP slider for {characterName}");
            hpSlider = CreateSlider("HP Bar", panel.transform, new Color(0.8f, 0.2f, 0.2f, 1.0f), 0.4f);
        }
        else
        {
            Debug.Log($"[OverworldMenuUI] Found existing HP slider: {hpSlider.name}");
            
            // Check if fill exists and is properly set
            if (hpSlider.fillRect == null)
            {
                Debug.LogWarning($"[OverworldMenuUI] HP slider {hpSlider.name} has no fillRect!");
                
                // Try to find the fill rect
                Transform fillArea = hpSlider.transform.Find("Fill Area");
                if (fillArea != null)
                {
                    Transform fill = fillArea.Find("Fill");
                    if (fill != null)
                    {
                        Debug.Log($"[OverworldMenuUI] Found Fill rect for {hpSlider.name}, assigning it");
                        hpSlider.fillRect = fill.GetComponent<RectTransform>();
                    }
                }
            }
            
            // Make sure the fill is red
            Image fillImage = hpSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                Debug.Log($"[OverworldMenuUI] Setting HP fill color to red for {characterName} (was {fillImage.color})");
                fillImage.color = new Color(0.8f, 0.2f, 0.2f, 1.0f);
            }
            else
            {
                Debug.LogWarning($"[OverworldMenuUI] HP slider {hpSlider.name} has fillRect but no Image component!");
            }
        }
        
        // Find or create Mind slider
        Debug.Log($"[OverworldMenuUI] Searching for Mind slider for {characterName}");
        Slider mindSlider = panel.transform.Find("MindBar")?.GetComponent<Slider>();
        if (mindSlider == null)
        {
            Debug.Log($"[OverworldMenuUI] No 'MindBar' found, checking for 'ManaBar'");
            mindSlider = panel.transform.Find("ManaBar")?.GetComponent<Slider>();
        }
        
        // Try checking for 'Sanity Bar' as mentioned in the error
        if (mindSlider == null)
        {
            Debug.Log($"[OverworldMenuUI] No 'ManaBar' found, checking for 'Sanity Bar'");
            mindSlider = panel.transform.Find("Sanity Bar")?.GetComponent<Slider>();
        }
        
        // Try harder to find the Mind/Sanity bar with a wider search
        if (mindSlider == null)
        {
            foreach (Transform child in panel.transform)
            {
                Debug.Log($"[OverworldMenuUI] Checking child for Mind/Sanity bar: {child.name}");
                if (child.name.Contains("Mind") || child.name.Contains("Mana") || 
                    child.name.Contains("Sanity") || child.name.ToLower().Contains("sanity"))
                {
                    Slider childSlider = child.GetComponent<Slider>();
                    if (childSlider != null)
                    {
                        mindSlider = childSlider;
                        Debug.Log($"[OverworldMenuUI] Found Mind/Sanity slider in child: {child.name}");
                        break;
                    }
                }
            }
        }
        
        if (mindSlider == null)
        {
            Debug.Log($"[OverworldMenuUI] Creating new Sanity slider for {characterName}");
            mindSlider = CreateSlider("Sanity Bar", panel.transform, new Color(0.2f, 0.4f, 0.8f, 1.0f), 0.65f);
        }
        else
        {
            Debug.Log($"[OverworldMenuUI] Found existing Mind/Sanity slider: {mindSlider.name}");
            
            // Check if fill exists and is properly set
            if (mindSlider.fillRect == null)
            {
                Debug.LogWarning($"[OverworldMenuUI] Mind/Sanity slider {mindSlider.name} has no fillRect!");
                
                // Try to find the fill rect
                Transform fillArea = mindSlider.transform.Find("Fill Area");
                if (fillArea != null)
                {
                    Transform fill = fillArea.Find("Fill");
                    if (fill != null)
                    {
                        Debug.Log($"[OverworldMenuUI] Found Fill rect for {mindSlider.name}, assigning it");
                        mindSlider.fillRect = fill.GetComponent<RectTransform>();
                    }
                }
            }
            
            // Make sure the fill is blue
            Image fillImage = mindSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                Debug.Log($"[OverworldMenuUI] Setting Mind/Sanity fill color to blue for {characterName} (was {fillImage.color})");
                fillImage.color = new Color(0.2f, 0.4f, 0.8f, 1.0f);
            }
            else
            {
                Debug.LogWarning($"[OverworldMenuUI] Mind/Sanity slider {mindSlider.name} has fillRect but no Image component!");
            }
        }
        
        // Store references
        healthSliders[panel] = hpSlider;
        mindSliders[panel] = mindSlider;
    }
    
    private Slider CreateSlider(string name, Transform parent, Color fillColor, float yPosition)
    {
        Debug.Log($"[OverworldMenuUI] Creating new slider: {name} with color: {fillColor}");
        
        GameObject sliderObject = new GameObject(name);
        sliderObject.transform.SetParent(parent, false);
        
        // Set up RectTransform
        RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.1f, yPosition - 0.1f);
        sliderRect.anchorMax = new Vector2(0.9f, yPosition + 0.1f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;
        
        // Add Slider component
        Slider slider = sliderObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        
        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObject.transform, false);
        
        RectTransform backgroundRect = background.AddComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 1);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        // Create fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);
        
        // Create fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;
        
        // Setup slider references
        slider.fillRect = fillRect;
        slider.targetGraphic = backgroundImage;
        slider.direction = Slider.Direction.LeftToRight;
        
        // Set initial value
        slider.value = 1.0f;
        
        return slider;
    }
    
    private void UpdateCharacterStats()
    {
        // Debug why we might exit early
        if (charactersMenu == null)
        {
            Debug.LogWarning("[OverworldMenuUI] UpdateCharacterStats early exit: charactersMenu is null");
            return;
        }
        
        if (!charactersMenu.activeSelf)
        {
            Debug.LogWarning("[OverworldMenuUI] UpdateCharacterStats early exit: charactersMenu is not active");
            return;
        }
        
        // Make sure PersistentGameManager exists
        if (PersistentGameManager.Instance == null)
        {
            Debug.LogWarning("[OverworldMenuUI] UpdateCharacterStats early exit: PersistentGameManager.Instance is null");
            return;
        }
        
        // Debug character panel status
        Debug.Log($"[OverworldMenuUI] UpdateCharacterStats proceeding with panels: " +
                 $"Magician={magicianPanel != null}, Fighter={fighterPanel != null}, " +
                 $"Bard={bardPanel != null}, Ranger={rangerPanel != null}");
        
        // Check if panels are initialized with UI components
        bool panelsInitialized = 
            (magicianPanel != null && healthSliders.ContainsKey(magicianPanel)) ||
            (fighterPanel != null && healthSliders.ContainsKey(fighterPanel)) ||
            (bardPanel != null && healthSliders.ContainsKey(bardPanel)) ||
            (rangerPanel != null && healthSliders.ContainsKey(rangerPanel));
            
        if (!panelsInitialized)
        {
            Debug.LogWarning("[OverworldMenuUI] Character panels not initialized with UI components. Initializing now...");
            if (magicianPanel != null) InitializeCharacterPanel(magicianPanel, "Magician");
            if (fighterPanel != null) InitializeCharacterPanel(fighterPanel, "Fighter");
            if (bardPanel != null) InitializeCharacterPanel(bardPanel, "Bard");
            if (rangerPanel != null) InitializeCharacterPanel(rangerPanel, "Ranger");
        }
        
        // Update each character panel with stats from PersistentGameManager
        UpdateCharacterPanel(magicianPanel, "Magician");
        UpdateCharacterPanel(fighterPanel, "Fighter");
        UpdateCharacterPanel(bardPanel, "Bard");
        UpdateCharacterPanel(rangerPanel, "Ranger");
    }
    
    private void UpdateCharacterPanel(GameObject panel, string characterName)
    {
        if (panel == null) return;
        
        // Get character stats
        int health = PersistentGameManager.Instance.GetCharacterHealth(characterName, 100);
        int maxHealth = PersistentGameManager.Instance.GetCharacterMaxHealth(characterName);
        int mind = PersistentGameManager.Instance.GetCharacterMind(characterName, 100);
        int maxMind = PersistentGameManager.Instance.GetCharacterMaxMind(characterName);
        
        // Debug log UI component status
        Debug.Log($"[OverworldMenuUI] {characterName} UI Components Status:");
        Debug.Log($"- Health Text Found: {healthTexts.TryGetValue(panel, out TextMeshProUGUI _)}");
        Debug.Log($"- Mind Text Found: {mindTexts.TryGetValue(panel, out TextMeshProUGUI _)}");
        Debug.Log($"- HP Slider Found: {healthSliders.TryGetValue(panel, out Slider _)}");
        Debug.Log($"- Mind Slider Found: {mindSliders.TryGetValue(panel, out Slider _)}");
        
        // Update text displays
        if (healthTexts.TryGetValue(panel, out TextMeshProUGUI healthText))
        {
            healthText.text = $"Health: {health}/{maxHealth}";
        }
        
        if (mindTexts.TryGetValue(panel, out TextMeshProUGUI mindText))
        {
            mindText.text = $"Mind: {mind}/{maxMind}";
        }
        
        // Debug log current stats
        if (Time.frameCount % 300 == 0) // Log every 300 frames to avoid spamming
        {
            Debug.Log($"[OverworldMenuUI] {characterName} Stats - HP: {health}/{maxHealth}, Mind: {mind}/{maxMind}");
        }
        
        // Update HP slider
        if (healthSliders.TryGetValue(panel, out Slider hpSlider) && hpSlider != null)
        {
            float hpRatio = (float)health / maxHealth;
            hpSlider.value = hpRatio;
            
            // Check if the value is actually changing
            if (Time.frameCount % 300 == 0) // Log every 300 frames to avoid spamming
            {
                Debug.Log($"[OverworldMenuUI] Setting {characterName} HP slider to {hpRatio:F2} (current value: {hpSlider.value:F2})");
                
                // Double-check fill rect
                if (hpSlider.fillRect == null)
                {
                    Debug.LogError($"[OverworldMenuUI] {characterName} HP slider has no fillRect!");
                }
                else
                {
                    Image fillImage = hpSlider.fillRect.GetComponent<Image>();
                    if (fillImage == null)
                    {
                        Debug.LogError($"[OverworldMenuUI] {characterName} HP fillRect has no Image component!");
                    }
                    else
                    {
                        Debug.Log($"[OverworldMenuUI] {characterName} HP fill color: {fillImage.color}");
                    }
                }
            }
        }
        else
        {
            if (Time.frameCount % 300 == 0) // Log every 300 frames to avoid spamming
            {
                Debug.LogWarning($"[OverworldMenuUI] {characterName} HP slider not found or null!");
            }
        }
        
        // Update Mind slider
        if (mindSliders.TryGetValue(panel, out Slider mindSlider) && mindSlider != null)
        {
            float mindRatio = (float)mind / maxMind;
            mindSlider.value = mindRatio;
            
            // Check if the value is actually changing
            if (Time.frameCount % 300 == 0) // Log every 300 frames to avoid spamming
            {
                Debug.Log($"[OverworldMenuUI] Setting {characterName} Mind slider to {mindRatio:F2} (current value: {mindSlider.value:F2})");
                
                // Double-check fill rect
                if (mindSlider.fillRect == null)
                {
                    Debug.LogError($"[OverworldMenuUI] {characterName} Mind slider has no fillRect!");
                }
                else
                {
                    Image fillImage = mindSlider.fillRect.GetComponent<Image>();
                    if (fillImage == null)
                    {
                        Debug.LogError($"[OverworldMenuUI] {characterName} Mind fillRect has no Image component!");
                    }
                    else
                    {
                        Debug.Log($"[OverworldMenuUI] {characterName} Mind fill color: {fillImage.color}");
                    }
                }
            }
        }
        else
        {
            if (Time.frameCount % 300 == 0) // Log every 300 frames to avoid spamming
            {
                Debug.LogWarning($"[OverworldMenuUI] {characterName} Mind slider not found or null!");
            }
        }
    }
    
    public void ShowCharactersMenu()
    {
        Debug.Log("[OverworldMenuUI] Showing Characters Menu");
        
        if (charactersMenu != null) charactersMenu.SetActive(true);
        if (itemsMenu != null) itemsMenu.SetActive(false);
        
        // Update button visuals to show active state
        if (characterMenuButton != null && inventoryMenuButton != null)
        {
            Debug.Log("[OverworldMenuUI] Found both buttons, updating colors...");
            
            // Update the character button colors
            Image charButtonImage = characterMenuButton.GetComponent<Image>();
            if (charButtonImage != null)
            {
                Debug.Log($"[OverworldMenuUI] Characters button before color change: {charButtonImage.color}");
                charButtonImage.color = highlightedButtonColor;
                Debug.Log($"[OverworldMenuUI] Characters button after color change: {charButtonImage.color}");
            }
            else
            {
                Debug.LogError("[OverworldMenuUI] Characters button has no Image component!");
                charButtonImage = characterMenuButton.gameObject.AddComponent<Image>();
                charButtonImage.color = highlightedButtonColor;
            }
            
            // Reset the inventory button colors
            Image invButtonImage = inventoryMenuButton.GetComponent<Image>();
            if (invButtonImage != null)
            {
                Debug.Log($"[OverworldMenuUI] Items button before color change: {invButtonImage.color}");
                invButtonImage.color = defaultButtonColor;
                Debug.Log($"[OverworldMenuUI] Items button after color change: {invButtonImage.color}");
            }
            else
            {
                Debug.LogError("[OverworldMenuUI] Items button has no Image component!");
                invButtonImage = inventoryMenuButton.gameObject.AddComponent<Image>();
                invButtonImage.color = defaultButtonColor;
            }
            
            Debug.Log($"[OverworldMenuUI] Updated button colors - Characters highlighted (highlighted={highlightedButtonColor}, default={defaultButtonColor})");
        }
        else
        {
            Debug.LogError($"[OverworldMenuUI] Cannot update button colors - buttons not found! Characters button: {(characterMenuButton != null ? "Found" : "NULL")}, Items button: {(inventoryMenuButton != null ? "Found" : "NULL")}");
        }
    }
    
    public void ShowItemsMenu()
    {
        Debug.Log("[OverworldMenuUI] Showing Items Menu");
        
        if (charactersMenu != null) charactersMenu.SetActive(false);
        if (itemsMenu != null) itemsMenu.SetActive(true);
        
        // Update button visuals to show active state
        if (characterMenuButton != null && inventoryMenuButton != null)
        {
            Debug.Log("[OverworldMenuUI] Found both buttons, updating colors...");
            
            // Reset the character button colors
            Image charButtonImage = characterMenuButton.GetComponent<Image>();
            if (charButtonImage != null)
            {
                Debug.Log($"[OverworldMenuUI] Characters button before color change: {charButtonImage.color}");
                charButtonImage.color = defaultButtonColor;
                Debug.Log($"[OverworldMenuUI] Characters button after color change: {charButtonImage.color}");
            }
            else
            {
                Debug.LogError("[OverworldMenuUI] Characters button has no Image component!");
                charButtonImage = characterMenuButton.gameObject.AddComponent<Image>();
                charButtonImage.color = defaultButtonColor;
            }
            
            // Update the inventory button colors
            Image invButtonImage = inventoryMenuButton.GetComponent<Image>();
            if (invButtonImage != null)
            {
                Debug.Log($"[OverworldMenuUI] Items button before color change: {invButtonImage.color}");
                invButtonImage.color = highlightedButtonColor;
                Debug.Log($"[OverworldMenuUI] Items button after color change: {invButtonImage.color}");
            }
            else
            {
                Debug.LogError("[OverworldMenuUI] Items button has no Image component!");
                invButtonImage = inventoryMenuButton.gameObject.AddComponent<Image>();
                invButtonImage.color = highlightedButtonColor;
            }
            
            Debug.Log($"[OverworldMenuUI] Updated button colors - Items highlighted (highlighted={highlightedButtonColor}, default={defaultButtonColor})");
        }
        else
        {
            Debug.LogError($"[OverworldMenuUI] Cannot update button colors - buttons not found! Characters button: {(characterMenuButton != null ? "Found" : "NULL")}, Items button: {(inventoryMenuButton != null ? "Found" : "NULL")}");
        }
    }
} 