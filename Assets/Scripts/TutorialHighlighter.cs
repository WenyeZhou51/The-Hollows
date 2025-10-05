using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Handles highlighting UI elements during tutorial dialogues
/// </summary>
public class TutorialHighlighter : MonoBehaviour
{
    [Header("Highlight Settings")]
    [Tooltip("Sprite to use for highlighting (should be a white circle or frame)")]
    [SerializeField] private Sprite highlightSprite;
    
    [Tooltip("Color of the highlight overlay")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0f, 0.7f); // Yellow more opaque for visibility
    
    [Tooltip("How much larger the highlight should be than the target (1.0 = same size, 1.2 = 20% larger)")]
    [SerializeField] private float highlightScale = 1.5f; // Increased from 1.2 to 1.5 for better visibility
    
    [Tooltip("Pulse animation speed")]
    [SerializeField] private float pulseSpeed = 2f;
    
    [Tooltip("Pulse animation intensity (0 = no pulse)")]
    [SerializeField] private float pulseIntensity = 0.1f;
    
    // Active highlights tracking
    private Dictionary<string, GameObject> activeHighlights = new Dictionary<string, GameObject>();
    
    // Active flashing coroutines
    private Dictionary<string, Coroutine> activeFlashes = new Dictionary<string, Coroutine>();
    
    // Store original colors for sprites being flashed
    private Dictionary<string, Color> originalSpriteColors = new Dictionary<string, Color>();
    
    // Singleton instance
    public static TutorialHighlighter Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Highlights a UI element by name
    /// </summary>
    public void HighlightElement(string elementName, RectTransform targetTransform = null)
    {
        if (targetTransform == null)
        {
            // Try to find the element by name
            targetTransform = FindUIElement(elementName);
        }
        
        if (targetTransform == null)
        {
            Debug.LogWarning($"[Tutorial] Could not find UI element to highlight: {elementName}");
            return;
        }
        
        // Remove existing highlight if present
        if (activeHighlights.ContainsKey(elementName))
        {
            RemoveHighlight(elementName);
        }
        
        // Create highlight overlay
        GameObject highlightObj = new GameObject($"Highlight_{elementName}");
        highlightObj.transform.SetParent(targetTransform, false);
        
        Debug.Log($"[Tutorial] Creating highlight object '{highlightObj.name}' under parent '{targetTransform.name}'");
        
        // Add Image component
        Image highlightImage = highlightObj.AddComponent<Image>();
        
        // Use the highlight sprite if available, otherwise use Unity's default white sprite
        if (highlightSprite != null)
        {
            highlightImage.sprite = highlightSprite;
            Debug.Log($"[Tutorial] Using highlight sprite: {highlightSprite.name}");
        }
        else
        {
            // Use Unity's built-in UI sprite for a solid color overlay
            highlightImage.sprite = Resources.Load<Sprite>("UI/Skin/UISprite");
            if (highlightImage.sprite == null)
            {
                // Fallback: Create a simple white texture sprite
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                highlightImage.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                Debug.Log("[Tutorial] Created fallback white sprite for highlight");
            }
            else
            {
                Debug.Log("[Tutorial] Using Unity's default UI sprite");
            }
        }
        
        highlightImage.color = highlightColor;
        highlightImage.raycastTarget = false; // Don't block input
        highlightImage.type = Image.Type.Simple; // Simple fill
        highlightImage.material = null; // Use default UI material
        
        Debug.Log($"[Tutorial] Highlight image color: {highlightColor}, enabled: {highlightImage.enabled}, sprite: {(highlightImage.sprite != null ? highlightImage.sprite.name : "null")}");
        
        // Set RectTransform to cover the target
        RectTransform highlightRect = highlightObj.GetComponent<RectTransform>();
        highlightRect.anchorMin = Vector2.zero;
        highlightRect.anchorMax = Vector2.one;
        highlightRect.sizeDelta = Vector2.zero;
        highlightRect.anchoredPosition = Vector2.zero;
        
        // Scale up slightly
        highlightRect.localScale = Vector3.one * highlightScale;
        
        Debug.Log($"[Tutorial] Highlight RectTransform - size: {highlightRect.sizeDelta}, scale: {highlightRect.localScale}, position: {highlightRect.position}");
        
        // CRITICAL: Set as last sibling to render on top of other UI elements
        highlightObj.transform.SetAsLastSibling();
        
        // Make sure the object is active
        highlightObj.SetActive(true);
        
        // Add animator component for pulsing effect
        TutorialHighlightPulse pulse = highlightObj.AddComponent<TutorialHighlightPulse>();
        pulse.Initialize(pulseSpeed, pulseIntensity);
        
        // Store reference
        activeHighlights[elementName] = highlightObj;
        
        Debug.Log($"[Tutorial] Highlighted UI element: {elementName}");
    }
    
    /// <summary>
    /// Removes a highlight from a UI element
    /// </summary>
    public void RemoveHighlight(string elementName)
    {
        if (activeHighlights.ContainsKey(elementName))
        {
            Destroy(activeHighlights[elementName]);
            activeHighlights.Remove(elementName);
            Debug.Log($"[Tutorial] Removed highlight from: {elementName}");
        }
    }
    
    /// <summary>
    /// Removes all active highlights
    /// </summary>
    public void RemoveAllHighlights()
    {
        foreach (var highlight in activeHighlights.Values)
        {
            if (highlight != null)
            {
                Destroy(highlight);
            }
        }
        activeHighlights.Clear();
        Debug.Log("[Tutorial] Removed all highlights");
    }
    
    /// <summary>
    /// Finds a UI element by name or path
    /// </summary>
    private RectTransform FindUIElement(string elementName)
    {
        // Common UI element mappings for Battle_Tutorial
        CombatManager combatManager = FindObjectOfType<CombatManager>();
        CombatUI combatUI = FindObjectOfType<CombatUI>();
        
        // Map element names to actual GameObjects
        switch (elementName.ToLower())
        {
            case "player_health":
            case "player_hp":
            case "health_bar":
                return FindPlayerHealthBar();
                
            case "player_mind":
            case "player_sanity":
            case "mind_bar":
            case "sanity_bar":
                return FindPlayerMindBar();
                
            case "player_action":
            case "action_bar":
                return FindPlayerActionBar();
                
            case "enemy_health":
            case "enemy_hp":
                return FindEnemyHealthBar();
                
            case "enemy_action":
                return FindEnemyActionBar();
                
            case "attack_button":
                return FindButton("Attack");
                
            case "guard_button":
                return FindButton("Guard");
                
            case "skill_button":
            case "skills_button":
                return FindButton("Skills");
                
            case "item_button":
            case "items_button":
                return FindButton("Items");
                
            default:
                // Try finding by exact GameObject name
                GameObject foundObj = GameObject.Find(elementName);
                if (foundObj != null)
                {
                    return foundObj.GetComponent<RectTransform>();
                }
                break;
        }
        
        return null;
    }
    
    private RectTransform FindPlayerHealthBar()
    {
        CombatUI combatUI = FindObjectOfType<CombatUI>();
        if (combatUI != null && combatUI.characterUI != null && combatUI.characterUI.Length > 0)
        {
            // Get first player's health bar
            var characterPanel = combatUI.characterUI[0].characterPanel;
            if (characterPanel != null)
            {
                // Find slider named "Health" or first slider
                Slider[] sliders = characterPanel.GetComponentsInChildren<Slider>();
                if (sliders.Length > 0)
                {
                    return sliders[0].GetComponent<RectTransform>();
                }
            }
        }
        return null;
    }
    
    private RectTransform FindPlayerMindBar()
    {
        CombatUI combatUI = FindObjectOfType<CombatUI>();
        if (combatUI != null && combatUI.characterUI != null && combatUI.characterUI.Length > 0)
        {
            var characterPanel = combatUI.characterUI[0].characterPanel;
            if (characterPanel != null)
            {
                Slider[] sliders = characterPanel.GetComponentsInChildren<Slider>();
                if (sliders.Length > 1)
                {
                    return sliders[1].GetComponent<RectTransform>();
                }
            }
        }
        return null;
    }
    
    private RectTransform FindPlayerActionBar()
    {
        CombatUI combatUI = FindObjectOfType<CombatUI>();
        if (combatUI != null && combatUI.characterUI != null && combatUI.characterUI.Length > 0)
        {
            var characterPanel = combatUI.characterUI[0].characterPanel;
            if (characterPanel != null)
            {
                Slider[] sliders = characterPanel.GetComponentsInChildren<Slider>();
                if (sliders.Length > 2)
                {
                    return sliders[2].GetComponent<RectTransform>();
                }
            }
        }
        return null;
    }
    
    private RectTransform FindEnemyHealthBar()
    {
        Debug.Log("[Tutorial] FindEnemyHealthBar called");
        
        CombatManager combatManager = FindObjectOfType<CombatManager>();
        if (combatManager == null)
        {
            Debug.LogWarning("[Tutorial] CombatManager not found");
            return null;
        }
        
        if (combatManager.enemies == null || combatManager.enemies.Count == 0)
        {
            Debug.LogWarning("[Tutorial] No enemies found in CombatManager");
            return null;
        }
        
        var enemy = combatManager.enemies[0];
        Debug.Log($"[Tutorial] First enemy: {enemy.name}");
        
        // Log all child transforms
        Debug.Log($"[Tutorial] Enemy children count: {enemy.transform.childCount}");
        for (int i = 0; i < enemy.transform.childCount; i++)
        {
            Transform child = enemy.transform.GetChild(i);
            Debug.Log($"[Tutorial] Enemy child {i}: {child.name}");
        }
        
        CombatStats stats = enemy.GetComponent<CombatStats>();
        if (stats == null)
        {
            Debug.LogWarning("[Tutorial] Enemy has no CombatStats component");
        }
        
        // Try multiple naming conventions
        string[] healthBarNames = { "HealthFill", "HealthBackground", "Health", "HP", "health", "healthbar" };
        Transform healthTransform = null;
        
        foreach (string name in healthBarNames)
        {
            healthTransform = enemy.transform.Find(name);
            if (healthTransform != null)
            {
                Debug.Log($"[Tutorial] Found health bar with name: {name}");
                break;
            }
        }
        
        // Also try deep search
        if (healthTransform == null)
        {
            SpriteRenderer[] spriteRenderers = enemy.GetComponentsInChildren<SpriteRenderer>();
            Debug.Log($"[Tutorial] Found {spriteRenderers.Length} SpriteRenderers in enemy");
            
            foreach (var sr in spriteRenderers)
            {
                Debug.Log($"[Tutorial] SpriteRenderer found: {sr.name} (color: {sr.color})");
                // Look for sprites that might be health bars (often red or green)
                if (sr.name.ToLower().Contains("health") || sr.name.ToLower().Contains("hp"))
                {
                    healthTransform = sr.transform;
                    Debug.Log($"[Tutorial] Using SpriteRenderer as health bar: {sr.name}");
                    break;
                }
            }
        }
        
        if (healthTransform != null)
        {
            Debug.Log($"[Tutorial] Creating world space highlight for enemy health bar");
            return CreateWorldSpaceHighlightTarget(healthTransform);
        }
        
        // Fallback: Try to find UI sliders (in case enemy has UI bars)
        Slider[] sliders = enemy.GetComponentsInChildren<Slider>();
        Debug.Log($"[Tutorial] Found {sliders.Length} sliders in enemy");
        if (sliders.Length > 0)
        {
            Debug.Log($"[Tutorial] Using slider for enemy health bar: {sliders[0].name}");
            return sliders[0].GetComponent<RectTransform>();
        }
        
        Debug.LogWarning("[Tutorial] Could not find enemy health bar");
        return null;
    }
    
    private RectTransform FindEnemyActionBar()
    {
        Debug.Log("[Tutorial] FindEnemyActionBar called");
        
        CombatManager combatManager = FindObjectOfType<CombatManager>();
        if (combatManager == null)
        {
            Debug.LogWarning("[Tutorial] CombatManager not found");
            return null;
        }
        
        if (combatManager.enemies == null || combatManager.enemies.Count == 0)
        {
            Debug.LogWarning("[Tutorial] No enemies found in CombatManager");
            return null;
        }
        
        var enemy = combatManager.enemies[0];
        Debug.Log($"[Tutorial] First enemy: {enemy.name}");
        
        // Try multiple naming conventions
        string[] actionBarNames = { "ActionFill", "ActionBackground", "Action", "action", "actionbar" };
        Transform actionTransform = null;
        
        foreach (string name in actionBarNames)
        {
            actionTransform = enemy.transform.Find(name);
            if (actionTransform != null)
            {
                Debug.Log($"[Tutorial] Found action bar with name: {name}");
                break;
            }
        }
        
        // Also try deep search
        if (actionTransform == null)
        {
            SpriteRenderer[] spriteRenderers = enemy.GetComponentsInChildren<SpriteRenderer>();
            
            foreach (var sr in spriteRenderers)
            {
                // Look for sprites that might be action bars (often yellow or blue)
                if (sr.name.ToLower().Contains("action"))
                {
                    actionTransform = sr.transform;
                    Debug.Log($"[Tutorial] Using SpriteRenderer as action bar: {sr.name}");
                    break;
                }
            }
        }
        
        if (actionTransform != null)
        {
            Debug.Log($"[Tutorial] Creating world space highlight for enemy action bar");
            return CreateWorldSpaceHighlightTarget(actionTransform);
        }
        
        // Fallback: Try to find UI sliders (in case enemy has UI bars)
        Slider[] sliders = enemy.GetComponentsInChildren<Slider>();
        Debug.Log($"[Tutorial] Found {sliders.Length} sliders in enemy");
        if (sliders.Length > 1)
        {
            Debug.Log($"[Tutorial] Using slider for enemy action bar: {sliders[1].name}");
            return sliders[1].GetComponent<RectTransform>();
        }
        
        Debug.LogWarning("[Tutorial] Could not find enemy action bar");
        return null;
    }
    
    /// <summary>
    /// Creates a world-space canvas with a RectTransform for highlighting sprite-based UI elements
    /// </summary>
    private RectTransform CreateWorldSpaceHighlightTarget(Transform worldObject)
    {
        Debug.Log($"[Tutorial] CreateWorldSpaceHighlightTarget for: {worldObject.name}");
        
        // Create a canvas in world space at the same position as the sprite
        GameObject canvasObj = new GameObject($"HighlightCanvas_{worldObject.name}");
        canvasObj.transform.SetParent(worldObject, false);
        canvasObj.transform.localPosition = Vector3.zero;
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = Vector3.one;
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = "Default"; // Match the sprite's sorting layer
        canvas.sortingOrder = 100; // High order to render on top
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100; // Higher value for better resolution
        
        // Add GraphicRaycaster so it can be interacted with
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Set the canvas to match the sprite size
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        
        // Get the sprite renderer to match size
        SpriteRenderer spriteRenderer = worldObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds bounds = spriteRenderer.bounds;
            // Use world size for the canvas, but make it much more visible
            float worldWidth = bounds.size.x * 1.3f; // 30% wider for visibility
            float worldHeight = Mathf.Max(bounds.size.y * 3f, 0.3f); // At least 3x taller, minimum 0.3 units
            
            canvasRect.sizeDelta = new Vector2(worldWidth, worldHeight);
            
            // Position the canvas to match the sprite's center
            canvasRect.localPosition = Vector3.zero;
            
            Debug.Log($"[Tutorial] Canvas size: {worldWidth}x{worldHeight} (original bounds: {bounds.size})");
            Debug.Log($"[Tutorial] Canvas sortingOrder: {canvas.sortingOrder}, layer: {canvas.sortingLayerName}");
        }
        else
        {
            // Default size if no sprite renderer
            canvasRect.sizeDelta = new Vector2(1f, 0.4f);
            Debug.LogWarning($"[Tutorial] No sprite renderer found on {worldObject.name}, using default size");
        }
        
        // Store reference to clean up later
        string canvasKey = $"Canvas_{worldObject.name}";
        if (!activeHighlights.ContainsKey(canvasKey))
        {
            activeHighlights[canvasKey] = canvasObj;
        }
        
        Debug.Log($"[Tutorial] World space canvas created successfully");
        return canvasRect;
    }
    
    private RectTransform FindButton(string buttonName)
    {
        // Find button in action menu
        CombatUI combatUI = FindObjectOfType<CombatUI>();
        if (combatUI != null && combatUI.actionMenu != null)
        {
            TextMeshProUGUI[] texts = combatUI.actionMenu.GetComponentsInChildren<TextMeshProUGUI>(true);
            Debug.Log($"[Tutorial] Searching for button '{buttonName}'. Found {texts.Length} text components in action menu.");
            
            foreach (var text in texts)
            {
                // Trim and compare text, ignoring whitespace and case
                string cleanText = text.text.Trim().Replace("\n", "").Replace("\r", "");
                Debug.Log($"[Tutorial] Checking text: '{cleanText}' against '{buttonName}'");
                
                if (cleanText.Equals(buttonName, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Check if parent has RectTransform
                    Transform parent = text.transform.parent;
                    if (parent != null)
                    {
                        RectTransform rectTransform = parent.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            Debug.Log($"[Tutorial] Found button '{buttonName}' at {parent.name}");
                            return rectTransform;
                        }
                        else
                        {
                            // Try the text's own RectTransform if parent doesn't have one
                            rectTransform = text.GetComponent<RectTransform>();
                            if (rectTransform != null)
                            {
                                Debug.Log($"[Tutorial] Using text's own RectTransform for '{buttonName}'");
                                return rectTransform;
                            }
                        }
                    }
                }
            }
            
            Debug.LogWarning($"[Tutorial] Could not find button '{buttonName}' in action menu");
        }
        else
        {
            Debug.LogWarning($"[Tutorial] CombatUI or actionMenu not found when searching for button '{buttonName}'");
        }
        return null;
    }
    
    /// <summary>
    /// Starts flashing sprite renderers (for enemy bars)
    /// </summary>
    public void StartFlashing(string elementName)
    {
        // Stop any existing flash
        if (activeFlashes.ContainsKey(elementName))
        {
            StopFlashing(elementName);
        }
        
        // Find all matching sprites (for all enemies)
        List<SpriteRenderer> targetSprites = FindAllSpriteRenderers(elementName);
        if (targetSprites.Count > 0)
        {
            Debug.Log($"[Tutorial] Found {targetSprites.Count} sprites to flash for {elementName}");
            
            // Start flashing all found sprites
            for (int i = 0; i < targetSprites.Count; i++)
            {
                SpriteRenderer sprite = targetSprites[i];
                string uniqueKey = (i == 0) ? elementName : $"{elementName}_{i}";
                
                // Store original color
                originalSpriteColors[uniqueKey] = sprite.color;
                
                Coroutine flashCoroutine = StartCoroutine(FlashSprite(sprite, uniqueKey));
                activeFlashes[uniqueKey] = flashCoroutine;
            }
            
            Debug.Log($"[Tutorial] Started flashing {targetSprites.Count} sprites for: {elementName}");
        }
        else
        {
            Debug.LogWarning($"[Tutorial] Could not find any sprites to flash: {elementName}");
        }
    }
    
    /// <summary>
    /// Stops flashing sprite renderers
    /// </summary>
    public void StopFlashing(string elementName)
    {
        // Stop all coroutines associated with this element
        List<string> keysToRemove = new List<string>();
        foreach (var key in activeFlashes.Keys)
        {
            if (key == elementName || key.StartsWith(elementName + "_"))
            {
                StopCoroutine(activeFlashes[key]);
                keysToRemove.Add(key);
            }
        }
        
        // Remove from active flashes
        foreach (var key in keysToRemove)
        {
            activeFlashes.Remove(key);
        }
        
        // Restore original colors
        List<SpriteRenderer> sprites = FindAllSpriteRenderers(elementName);
        for (int i = 0; i < sprites.Count; i++)
        {
            string uniqueKey = $"{elementName}_{i}";
            if (i == 0) uniqueKey = elementName; // First one uses base name
            
            if (originalSpriteColors.ContainsKey(uniqueKey))
            {
                sprites[i].color = originalSpriteColors[uniqueKey];
                originalSpriteColors.Remove(uniqueKey);
            }
        }
        
        Debug.Log($"[Tutorial] Stopped flashing: {elementName}");
    }
    
    /// <summary>
    /// Coroutine to flash a sprite renderer
    /// </summary>
    private System.Collections.IEnumerator FlashSprite(SpriteRenderer sprite, string elementName)
    {
        Color originalColor = originalSpriteColors[elementName];
        float flashDuration = 0.5f; // How long each flash cycle takes
        
        while (true)
        {
            // Flash to bright
            float elapsed = 0f;
            while (elapsed < flashDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (flashDuration / 2f);
                sprite.color = Color.Lerp(originalColor, Color.white, t);
                yield return null;
            }
            
            // Flash back to original
            elapsed = 0f;
            while (elapsed < flashDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (flashDuration / 2f);
                sprite.color = Color.Lerp(Color.white, originalColor, t);
                yield return null;
            }
        }
    }
    
    /// <summary>
    /// Finds all sprite renderers for flashing (for all enemy bars)
    /// </summary>
    private List<SpriteRenderer> FindAllSpriteRenderers(string elementName)
    {
        List<SpriteRenderer> results = new List<SpriteRenderer>();
        
        CombatManager combatManager = FindObjectOfType<CombatManager>();
        if (combatManager == null || combatManager.enemies == null || combatManager.enemies.Count == 0)
        {
            Debug.LogWarning("[Tutorial] No CombatManager or enemies found");
            return results;
        }
        
        Debug.Log($"[Tutorial] Searching for {elementName} across {combatManager.enemies.Count} enemies");
        
        // Search through all enemies
        foreach (var enemy in combatManager.enemies)
        {
            if (enemy == null) continue;
            
            SpriteRenderer foundSprite = null;
            
            switch (elementName.ToLower())
            {
                case "enemy_health":
                case "enemy_hp":
                    // Find the HP fill sprite
                    SpriteRenderer[] hpSprites = enemy.GetComponentsInChildren<SpriteRenderer>();
                    Debug.Log($"[Tutorial] Enemy {enemy.name} has {hpSprites.Length} SpriteRenderers");
                    
                    foreach (var sr in hpSprites)
                    {
                        Debug.Log($"[Tutorial] Checking sprite: {sr.name}");
                        string lowerName = sr.name.ToLower();
                        if (lowerName.Contains("hpfill") || lowerName.Contains("healthfill") || 
                            lowerName.Contains("hp") && lowerName.Contains("fill"))
                        {
                            foundSprite = sr;
                            Debug.Log($"[Tutorial] Found HP sprite: {sr.name}");
                            break;
                        }
                    }
                    break;
                    
                case "enemy_action":
                    // Find the action fill sprite - try multiple naming conventions
                    SpriteRenderer[] actionSprites = enemy.GetComponentsInChildren<SpriteRenderer>();
                    Debug.Log($"[Tutorial] Enemy {enemy.name} has {actionSprites.Length} SpriteRenderers");
                    
                    foreach (var sr in actionSprites)
                    {
                        Debug.Log($"[Tutorial] Checking sprite: {sr.name}");
                        string lowerName = sr.name.ToLower();
                        // Try various naming patterns
                        if (lowerName.Contains("actionfill") || 
                            lowerName.Contains("action") && lowerName.Contains("fill") ||
                            lowerName.Contains("actfill") ||
                            lowerName.Contains("gauge") && lowerName.Contains("fill"))
                        {
                            foundSprite = sr;
                            Debug.Log($"[Tutorial] Found Action sprite: {sr.name}");
                            break;
                        }
                    }
                    
                    // If still not found, try looking for the second fill sprite (after HP)
                    if (foundSprite == null && actionSprites.Length >= 2)
                    {
                        // Often the second "Fill" sprite is the action bar
                        int fillCount = 0;
                        foreach (var sr in actionSprites)
                        {
                            if (sr.name.ToLower().Contains("fill") && !sr.name.ToLower().Contains("hp") && !sr.name.ToLower().Contains("health"))
                            {
                                fillCount++;
                                if (fillCount == 1) // First non-HP fill sprite
                                {
                                    foundSprite = sr;
                                    Debug.Log($"[Tutorial] Using fallback: found action sprite as {sr.name}");
                                    break;
                                }
                            }
                        }
                    }
                    break;
            }
            
            if (foundSprite != null)
            {
                results.Add(foundSprite);
            }
        }
        
        Debug.Log($"[Tutorial] Total sprites found for {elementName}: {results.Count}");
        return results;
    }
    
    private void OnDestroy()
    {
        RemoveAllHighlights();
        
        // Stop all flashing
        foreach (var flash in activeFlashes.Values)
        {
            if (flash != null)
            {
                StopCoroutine(flash);
            }
        }
        activeFlashes.Clear();
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

/// <summary>
/// Simple component to add pulsing animation to highlight overlays
/// </summary>
public class TutorialHighlightPulse : MonoBehaviour
{
    private float pulseSpeed = 2f;
    private float pulseIntensity = 0.1f;
    private Vector3 baseScale;
    
    public void Initialize(float speed, float intensity)
    {
        pulseSpeed = speed;
        pulseIntensity = intensity;
        baseScale = transform.localScale;
    }
    
    private void Update()
    {
        // Pulse animation
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
        transform.localScale = baseScale * pulse;
    }
}

