using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class CombatStats : MonoBehaviour
{
    [SerializeField] private Image characterImage; // Reference to UI Image
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    // Reference the fill bars directly
    [SerializeField] private SpriteRenderer healthFill;
    [SerializeField] private SpriteRenderer actionFill;
    
    public float maxHealth = 100f;
    public float currentHealth;
    public float maxSanity = 100f;
    public float currentSanity;
    public float maxAction = 100f;
    public float currentAction;
    public float actionSpeed = 20f; // Action points gained per second
    public float baseActionSpeed;
    public float attackMultiplier = 1.0f;
    public float defenseMultiplier = 1.0f;
    public bool isEnemy;
    
    // Reference to enemy behavior script for enemies
    [Header("Enemy Behavior")]
    [Tooltip("The behavior script that controls this enemy's actions")]
    public EnemyBehavior enemyBehavior;
    
    [Header("Visual Settings")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // Yellow-ish highlight
    [SerializeField] private float blinkInterval = 0.3f; // How fast the highlight blinks
    [SerializeField] private Color enemyDefaultColor = Color.red; // Default color for enemies
    [SerializeField] private Color allyDefaultColor = Color.white; // Default color for allies
    
    private Color originalColor;
    public string characterName;
    public List<SkillData> skills = new List<SkillData>();
    public List<ItemData> items = new List<ItemData>();
    
    // Properties for the Human Shield skill
    [Header("Human Shield Settings")]
    [Tooltip("Position offset for the guarded icon")]
    [SerializeField] private Vector3 guardedIconOffset = new Vector3(0, -1.2f, 0);
    
    public bool IsGuarded { get; private set; } = false;
    public CombatStats Guardian { get; private set; } = null;
    public CombatStats ProtectedAlly { get; private set; } = null;
    public GameObject GuardedIcon { get; private set; } = null;

    // Properties for Guard action
    [Header("Guard Action Settings")]
    [Tooltip("Damage reduction multiplier when in guard mode (0.5 = 50% damage reduction)")]
    [SerializeField] private float guardDamageReductionMultiplier = 0.5f;
    [Tooltip("Position offset for the guard icon")]
    [SerializeField] private Vector3 guardIconOffset = new Vector3(0, 1.2f, 0);
    public bool IsGuarding { get; private set; } = false;
    public float GuardDamageReductionMultiplier => guardDamageReductionMultiplier;
    public GameObject GuardIcon { get; private set; } = null;

    // Properties for the Piercing Shot skill
    [Header("Defense Reduction Settings")]
    [SerializeField] private int defenseReductionDefaultDuration = 2; // Default duration in turns
    [SerializeField] private float defenseReductionMultiplier = 1.5f; // Default multiplier (1.5 = 50% more damage)
    [Tooltip("Position offset for the defense reduction icon")]
    [SerializeField] private Vector3 defenseReductionIconOffset = new Vector3(0, -1.2f, 0);
    
    public bool HasReducedDefense { get; private set; } = false;
    public int DefenseReductionTurnsLeft { get; private set; } = 0;
    public float DefenseReductionMultiplier { get; private set; } = 1.0f; // 1.0 = normal damage, 1.5 = 50% more damage
    public GameObject DefenseReductionIcon { get; private set; } = null;

    // Properties for Speed Boost effect
    private float speedBoostMultiplier = 1f;
    private int speedBoostTurnsRemaining = 0;
    
    // Add these fields for the blinking highlight effect
    private bool isHighlighted = false;
    private float blinkTimer = 0f;
    private bool isBlinkOn = false;
    private Color defaultColor = Color.white; // Default color for non-enemy characters
    
    // Track if this character is the active character (currently acting)
    private bool isActiveCharacter = false; // Private backing field
    public bool IsActiveCharacter 
    { 
        get { return isActiveCharacter; }
        set 
        { 
            isActiveCharacter = value;
            // When set as active character, immediately update appearance
            if (value)
            {
                // Set to solid yellow without blinking
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = highlightColor;
                }
                if (characterImage != null)
                {
                    characterImage.color = highlightColor;
                }
            }
            else if (!isHighlighted) // Only reset if not being targeted
            {
                // Reset to default colors
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = isEnemy ? originalColor : allyDefaultColor;
                }
                if (characterImage != null)
                {
                    characterImage.color = isEnemy ? enemyDefaultColor : allyDefaultColor;
                }
            }
            
            // If this character's turn is ending, reduce defense reduction duration
            if (!value && HasReducedDefense && DefenseReductionTurnsLeft > 0)
            {
                DefenseReductionTurnsLeft--;
                Debug.Log($"[Piercing Shot] {name}'s defense reduction turns left: {DefenseReductionTurnsLeft}");
                
                // If no turns left, remove the defense reduction
                if (DefenseReductionTurnsLeft <= 0)
                {
                    RemoveDefenseReduction();
                }
            }
        }
    }

    private void Start()
    {
        Debug.Log($"[COMBAT DEBUG] START() called for {name} - current health: {currentHealth}, maxHealth: {maxHealth}, isEnemy: {isEnemy}");
        
        if (isEnemy)
        {
            // Apply 20% variance to enemy action speed at start of battle
            // Store original action speed before applying variance
            baseActionSpeed = actionSpeed;
            
            // Random variance between 80-120% (0.8-1.2)
            float variance = Random.Range(0.8f, 1.2f);
            actionSpeed = Mathf.FloorToInt(baseActionSpeed * variance);
            
            Debug.Log($"[COMBAT DEBUG] Enemy {name} action speed adjusted with variance: {baseActionSpeed} -> {actionSpeed}");
            
            // Set up enemy-specific properties without overriding inspector values
            if (characterImage != null)
            {
                characterImage.color = enemyDefaultColor; // Use configurable enemy color
            }
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            // Enemies don't use sanity, but we don't need to modify maxSanity
            
            // Debug the enemy behavior
            if (enemyBehavior != null)
            {
                Debug.Log($"[COMBAT DEBUG] Enemy {name} has behavior: {enemyBehavior.GetType().Name}");
            }
            else
            {
                Debug.LogWarning($"[COMBAT DEBUG] Enemy {name} has NO BEHAVIOR attached!");
            }
            
            // No need to instantiate, just verify components
            if (healthFill == null || actionFill == null)
            {
                Debug.LogError("Health or Action fill bar not assigned on " + gameObject.name);
            }
        }
        else
        {
            // For non-enemy characters, store their original colors
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            if (characterImage != null)
            {
                defaultColor = characterImage.color;
            }
        }
        
        // Check if current values are already initialized (non-zero)
        // This could mean they were set by CombatManager.InitializeCharacterStats
        bool healthInitialized = currentHealth > 0;
        bool sanityInitialized = currentSanity > 0;
        
        // Only initialize values that haven't been set
        if (!healthInitialized) {
            currentHealth = maxHealth;
            Debug.Log($"CombatStats.Start: Setting default health for {characterName}: {currentHealth}/{maxHealth}");
        } else {
            Debug.Log($"CombatStats.Start: Health already initialized for {characterName}: {currentHealth}/{maxHealth}");
        }
        
        if (!sanityInitialized) {
            currentSanity = maxSanity;
            Debug.Log($"CombatStats.Start: Setting default sanity for {characterName}: {currentSanity}/{maxSanity}");
        } else {
            Debug.Log($"CombatStats.Start: Sanity already initialized for {characterName}: {currentSanity}/{maxSanity}");
        }
        
        currentAction = 0f; // Always start with empty action bar

        // Debug log to check character name
        Debug.Log($"[COMBAT DEBUG] Character fully initialized: {characterName}, isEnemy: {isEnemy}, currentHealth: {currentHealth}, maxHealth: {maxHealth}, IsDead: {IsDead()}");

        // Add skills to the first player character only (to avoid duplicates)
        if (!isEnemy && characterName == "The Magician")
        {
            Debug.Log($"Adding skills for The Magician character");
            
            // Clear existing skills to prevent duplicates
            skills.Clear();
            
            skills.Add(new SkillData(
                "Before Your Eyes",
                "Reset target's action gauge to 0",
                15f,
                true
            ));
            
            // Add the new Fiend Fire skill
            skills.Add(new SkillData(
                "Fiend Fire",
                "Deal 10 damage to a target 1-5 times randomly",
                0f,  // Costs 0 sanity
                true // Requires a target
            ));
            
            // Debug log to verify skills were added
            Debug.Log($"The Magician now has {skills.Count} skills: {string.Join(", ", skills.Select(s => s.name))}");
        }
        else if (!isEnemy && characterName == "The Fighter")
        {
            Debug.Log($"Adding skills for The Fighter character");
            
            // Clear existing skills to prevent duplicates
            skills.Clear();
            
            // Add the Slam! skill
            skills.Add(new SkillData(
                "Slam!",
                "Deal 10 damage to all enemies",
                0f,  // Costs 0 sanity
                false // Does not require a target (hits all enemies)
            ));
            
            // Add the Human Shield! skill
            skills.Add(new SkillData(
                "Human Shield!",
                "Protect an ally by taking all damage they would receive until your next turn",
                0f,  // Costs 0 sanity
                true  // Requires a target (the ally to protect)
            ));
            
            // Debug log to verify skills were added
            Debug.Log($"The Fighter now has {skills.Count} skills: {string.Join(", ", skills.Select(s => s.name))}");
        }
        else if (!isEnemy && characterName == "The Bard")
        {
            Debug.Log($"Adding skills for The Bard character");
            
            // Clear existing skills to prevent duplicates
            skills.Clear();
            
            // Add the Healing Words skill with updated healing values
            skills.Add(new SkillData(
                "Healing Words",
                "Heal an ally for 50 HP and 30 sanity. Costs 10 sanity to cast.",
                10f,  // Costs 10 sanity
                true  // Requires a target (the ally to heal)
            ));
            
            // Debug log to verify skills were added
            Debug.Log($"The Bard now has {skills.Count} skills: {string.Join(", ", skills.Select(s => s.name))}");
        }
        else if (!isEnemy && characterName == "The Ranger")
        {
            Debug.Log($"Adding skills for The Ranger character");
            
            // Clear existing skills to prevent duplicates
            skills.Clear();
            
            // Add the Piercing Shot skill
            skills.Add(new SkillData(
                "Piercing Shot",
                "Deal 10 damage and reduce target's defense by 50% for 2 turns.",
                0f,  // Costs 0 sanity
                true  // Requires a target (the enemy to hit)
            ));
            
            // Debug log to verify skills were added
            Debug.Log($"The Ranger now has {skills.Count} skills: {string.Join(", ", skills.Select(s => s.name))}");
        }

        // Initialize current values
        currentHealth = maxHealth;
        currentSanity = maxSanity;
        currentAction = 0f;
        
        // Store base action speed for status effects
        baseActionSpeed = actionSpeed;
        
        // Set the original color
        if (isEnemy)
        {
            defaultColor = enemyDefaultColor;
        }
        else
        {
            defaultColor = allyDefaultColor;
        }
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Initialize highlighting to off
        isHighlighted = false;
    }

    private void Update()
    {
        if (healthFill != null)
        {
            float healthPercent = currentHealth / maxHealth;
            // Change the local position based on the fill amount to keep left-aligned
            healthFill.transform.localPosition = new Vector3(-0.5f + (healthPercent * 0.5f), 0, 0);
            healthFill.transform.localScale = new Vector3(healthPercent, 1, 1);
        }
        
        if (actionFill != null)
        {
            float actionPercent = currentAction / maxAction;
            // Change the local position based on the fill amount to keep left-aligned
            actionFill.transform.localPosition = new Vector3(-0.5f + (actionPercent * 0.5f), 0, 0);
            actionFill.transform.localScale = new Vector3(actionPercent, 1, 1);
        }
        
        // Handle blinking highlight effect - for any highlighted character, including active character
        if (isHighlighted)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                isBlinkOn = !isBlinkOn;
                
                if (IsActiveCharacter)
                {
                    // For active characters, blink between highlight color and a slightly different highlight
                    Color alternateHighlight = new Color(
                        highlightColor.r * 0.8f, 
                        highlightColor.g * 0.8f, 
                        highlightColor.b * 0.8f, 
                        highlightColor.a
                    );
                    
                    // Apply the blink effect
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = isBlinkOn ? highlightColor : alternateHighlight;
                    }
                    
                    if (characterImage != null)
                    {
                        characterImage.color = isBlinkOn ? highlightColor : alternateHighlight;
                    }
                    
                    Debug.Log($"[Character Highlight] Active character {name} blink state: {isBlinkOn}");
                }
                else
                {
                    // For non-active characters, use the original behavior
                    // Apply the appropriate color based on blink state
                    if (spriteRenderer != null)
                    {
                        // Alternate between highlight color and appropriate default color
                        spriteRenderer.color = isBlinkOn ? highlightColor : (isEnemy ? originalColor : allyDefaultColor);
                    }
                    
                    if (characterImage != null)
                    {
                        // Alternate between highlight color and appropriate default color
                        characterImage.color = isBlinkOn ? highlightColor : (isEnemy ? enemyDefaultColor : allyDefaultColor);
                    }
                }
            }
        }
    }

    public bool IsDead()
    {
        return currentHealth <= 0 || (!isEnemy && currentSanity <= 0);
    }

    public void TakeDamage(float amount)
    {
        // Apply defense multiplier from status effects
        float adjustedAmount = amount * defenseMultiplier;
        
        // Check if guarded by another character via status system
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null && statusManager.HasStatus(this, StatusType.Guarded))
        {
            CombatStats guardian = statusManager.GetGuardian(this);
            if (guardian != null && !guardian.IsDead())
            {
                // Redirect damage to guardian
                guardian.TakeDamage(adjustedAmount);
                Debug.Log($"{name} is guarded by {guardian.name}. Redirecting {adjustedAmount} damage.");
                return;
            }
        }
        
        // Old guard/protection logic for backward compatibility
        if (IsGuarded && Guardian != null && !Guardian.IsDead())
        {
            Guardian.TakeDamage(adjustedAmount);
            Debug.Log($"{name} is guarded by {Guardian.name}. Redirecting {adjustedAmount} damage.");
            return;
        }
        
        // If in guard mode, reduce damage
        if (IsGuarding)
        {
            adjustedAmount *= GuardDamageReductionMultiplier;
            Debug.Log($"{name} is in guard stance. Reducing damage to {adjustedAmount}.");
        }
        
        // Apply the damage
        currentHealth -= adjustedAmount;
        Debug.Log($"{name} takes {adjustedAmount} damage. Current health: {currentHealth}/{maxHealth}");
        
        // Create a damage popup
        ShowDamagePopup(adjustedAmount);
        
        // Ensure health doesn't go below 0
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }
        
        // Update health bar
        UpdateHealthBar();
    }

    // Coroutine to fade out dead enemies and remove them from the scene
    private IEnumerator FadeOutAndDestroy()
    {
        Debug.Log($"[Enemy Death] {name} is fading out");
        
        // Fade duration in seconds
        float fadeDuration = 1.5f;
        float fadeTimer = 0f;
        
        // Get all renderers on this enemy
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        
        // Store original colors to properly fade them
        Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
        foreach (SpriteRenderer renderer in renderers)
        {
            originalColors[renderer] = renderer.color;
        }
        
        // Disable any status effect icons during fade
        if (DefenseReductionIcon != null)
            DefenseReductionIcon.SetActive(false);
        
        if (GuardIcon != null)
            GuardIcon.SetActive(false);
        
        if (GuardedIcon != null)
            GuardedIcon.SetActive(false);
        
        // Get UI image if available
        Image characterUIImage = null;
        if (characterImage != null)
        {
            characterUIImage = characterImage;
            originalColors[null] = characterUIImage.color; // Use null key for UI image
        }
        
        // Disable colliders to prevent interaction during fade
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
        
        // Perform fade animation with easing
        while (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            float normalizedTime = fadeTimer / fadeDuration;
            
            // Apply ease-out curve for smoother fade (start fast, end slow)
            float easedAlpha = 1f - Mathf.Pow(normalizedTime, 2);
            
            // Fade all sprite renderers
            foreach (SpriteRenderer renderer in renderers)
            {
                Color newColor = originalColors[renderer];
                newColor.a = easedAlpha;
                renderer.color = newColor;
            }
            
            // Fade UI image if available
            if (characterUIImage != null)
            {
                Color newColor = originalColors[null];
                newColor.a = easedAlpha;
                characterUIImage.color = newColor;
            }
            
            // Fade health and action bars
            if (healthFill != null)
            {
                Color healthColor = healthFill.color;
                healthColor.a = easedAlpha;
                healthFill.color = healthColor;
            }
            
            if (actionFill != null)
            {
                Color actionColor = actionFill.color;
                actionColor.a = easedAlpha;
                actionFill.color = actionColor;
            }
            
            yield return null;
        }
        
        // Hide the enemy instead of destroying it immediately
        // This allows the CombatManager to still reference it but it's visually gone
        gameObject.SetActive(false);
        
        Debug.Log($"[Enemy Death] {name} has faded out and been hidden");
    }

    // Set up guarding relationship
    public void GuardAlly(CombatStats ally)
    {
        // Check if status manager exists
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null)
        {
            // Use the new status system
            statusManager.GuardAlly(this, ally);
            
            // Set IsGuarding status
            IsGuarding = true;
            
            Debug.Log($"{name} is now guarding {ally.name} using status system.");
            return;
        }
        
        // Legacy system as fallback
        // Set this character as the guardian
        IsGuarding = true;
        ProtectedAlly = ally;
        
        // Set the ally as being guarded
        ally.IsGuarded = true;
        ally.Guardian = this;
        
        Debug.Log($"{name} is now guarding {ally.name} using legacy system.");
        
        // Show guard icon
        ShowGuardIcon();
        
        // Show guarded icon on ally
        ally.ShowGuardedIcon();
    }
    
    // Remove guarding relationship
    public void StopGuarding()
    {
        if (ProtectedAlly != null)
        {
            ProtectedAlly.IsGuarded = false;
            ProtectedAlly.Guardian = null;
            
            // Destroy the guarded icon
            if (ProtectedAlly.GuardedIcon != null)
            {
                Destroy(ProtectedAlly.GuardedIcon);
                ProtectedAlly.GuardedIcon = null;
            }
            
            Debug.Log($"[Human Shield] {name} stopped guarding {ProtectedAlly.name}");
            ProtectedAlly = null;
        }
    }
    
    // Activate guard stance
    public void ActivateGuard()
    {
        IsGuarding = true;
        Debug.Log($"[Guard] {name} entered guard stance. Damage reduced by {(1 - GuardDamageReductionMultiplier) * 100}%");
        
        // Create guard icon
        CreateGuardIcon();
    }
    
    // Deactivate guard stance
    public void DeactivateGuard()
    {
        if (IsGuarding)
        {
            IsGuarding = false;
            Debug.Log($"[Guard] {name} exited guard stance");
            
            // Destroy guard icon
            if (GuardIcon != null)
            {
                Destroy(GuardIcon);
                GuardIcon = null;
            }
        }
    }

    // Create a visual indicator for guarding status
    private void CreateGuardedIcon(CombatStats ally)
    {
        // Remove any existing icon
        if (ally.GuardedIcon != null)
        {
            Destroy(ally.GuardedIcon);
        }
        
        Debug.Log($"[Human Shield] Creating guarded icon for {ally.name}");
        
        // Load the Guarded Icon prefab from the Icons folder
        GameObject guardedIconPrefab = Resources.Load<GameObject>("Guarded Icon");
        
        // If not found in Resources, try direct path
        if (guardedIconPrefab == null)
        {
            #if UNITY_EDITOR
            guardedIconPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Icons/Guarded Icon.prefab");
            Debug.Log($"[Human Shield] Trying to load prefab from direct path: {guardedIconPrefab != null}");
            #endif
        }
        
        if (guardedIconPrefab != null)
        {
            // Create a new icon instance from the prefab
            GameObject icon = Instantiate(guardedIconPrefab);
            icon.name = "GuardedIcon";
            icon.transform.SetParent(ally.transform);
            icon.transform.localPosition = guardedIconOffset; // Use the configurable offset
            
            // Ensure the icon is visible by setting high sorting order on any renderers
            SpriteRenderer[] renderers = icon.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.sortingOrder = 20; // Higher sorting order to ensure visibility
            }
            
            icon.SetActive(true);
            
            // Store the reference
            ally.GuardedIcon = icon;
            Debug.Log($"[Human Shield] Icon prefab instantiated and attached to {ally.name}");
        }
        else
        {
            Debug.LogWarning("[Human Shield] Guarded Icon prefab not found. Creating a placeholder.");
            
            // Create a fallback icon if the prefab isn't found
            GameObject fallbackIcon = new GameObject("GuardedIcon");
            fallbackIcon.transform.SetParent(ally.transform);
            fallbackIcon.transform.localPosition = guardedIconOffset; // Use the configurable offset
            
            // Create a visible placeholder
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.SetParent(fallbackIcon.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // Add a material with a shield color
            Renderer visualRenderer = visual.GetComponent<Renderer>();
            visualRenderer.material.color = new Color(0, 0.5f, 1f); // Bright blue shield color
            
            // Make sure the GameObject has a high sorting layer
            visual.layer = LayerMask.NameToLayer("UI") != -1 ? LayerMask.NameToLayer("UI") : 5;
            
            // Remove the collider
            Destroy(visual.GetComponent<Collider>());
            
            // Store the reference
            ally.GuardedIcon = fallbackIcon;
            Debug.Log("[Human Shield] Created fallback icon for guarded status");
        }
    }

    // Create a visual indicator for guard stance
    private void CreateGuardIcon()
    {
        // Destroy any existing icon first
        if (GuardIcon != null)
        {
            Destroy(GuardIcon);
        }
        
        // Create a new icon
        GuardIcon = new GameObject($"{name}_GuardIcon");
        GuardIcon.transform.position = transform.position + guardIconOffset;
        
        // Make the icon a child of this character
        GuardIcon.transform.SetParent(transform);
        
        // Add a sprite renderer
        SpriteRenderer iconRenderer = GuardIcon.AddComponent<SpriteRenderer>();
        
        // Try to find a shield sprite in the project
        Sprite shieldSprite = Resources.Load<Sprite>("Shield");
        if (shieldSprite != null)
        {
            iconRenderer.sprite = shieldSprite;
        }
        else
        {
            // Create a simple colored square if no sprite is found
            iconRenderer.sprite = null;
            iconRenderer.color = Color.blue;
            iconRenderer.drawMode = SpriteDrawMode.Simple;
            
            // Add a simple shield shape
            GameObject shieldShape = new GameObject("ShieldShape");
            shieldShape.transform.SetParent(GuardIcon.transform);
            shieldShape.transform.localPosition = Vector3.zero;
            
            SpriteRenderer shieldRenderer = shieldShape.AddComponent<SpriteRenderer>();
            shieldRenderer.color = Color.blue;
            shieldRenderer.sortingOrder = iconRenderer.sortingOrder + 1;
            
            // Set the size of the shield
            GuardIcon.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        }
        
        // Set sorting order to be in front of the character
        iconRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 1;
    }

    public void HealHealth(float amount)
    {
        // Round down the healing amount to a whole number
        int wholeAmount = Mathf.FloorToInt(amount);
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + wholeAmount);
        
        // Create healing popup
        Vector3 popupPosition = transform.position + Vector3.up * 0.5f; // Adjust the Y offset as needed
        HealingPopup.CreateHealthPopup(popupPosition, wholeAmount, transform);
    }

    public void HealSanity(float amount)
    {
        if (!isEnemy)
        {
            // Round down the healing amount to a whole number
            int wholeAmount = Mathf.FloorToInt(amount);
            
            currentSanity = Mathf.Min(maxSanity, currentSanity + wholeAmount);
            
            // Create sanity healing popup
            Vector3 popupPosition = transform.position + Vector3.up * 0.7f; // Slightly higher than health popup
            HealingPopup.CreateSanityPopup(popupPosition, wholeAmount, transform);
        }
    }

    public void UseSanity(float amount)
    {
        if (!isEnemy)
        {
            // Round down the sanity cost to a whole number
            int wholeAmount = Mathf.FloorToInt(amount);
            currentSanity = Mathf.Max(0, currentSanity - wholeAmount);
            
            // Create mind damage popup with yellow color (isMindDamage = true)
            Vector3 popupPosition = transform.position + Vector3.up * 0.7f; // Slightly higher than health popup
            DamagePopup.Create(popupPosition, wholeAmount, false, transform, true);
        }
    }

    public void HighlightCharacter(bool highlight)
    {
        Debug.Log($"[Character Highlight] {name} highlight set to {highlight}, isEnemy: {isEnemy}, isActiveCharacter: {IsActiveCharacter}");
        
        // Store the highlight state regardless of active status
        isHighlighted = highlight;
        
        // Reset blink timer and state when highlight changes
        if (highlight)
        {
            blinkTimer = 0f;
            isBlinkOn = true;
            
            // If not active character, set initial highlight immediately
            // If active character, the Update() method will handle blinking
            if (!IsActiveCharacter)
            {
                // Set initial highlight colors
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = highlightColor;
                    Debug.Log($"[Character Highlight] {name} spriteRenderer color set to highlight");
                }
                
                if (characterImage != null)
                {
                    characterImage.color = highlightColor;
                    Debug.Log($"[Character Highlight] {name} characterImage color set to highlight");
                }
            }
            else
            {
                Debug.Log($"[Character Highlight] {name} is active character and targeted - will blink in Update");
            }
        }
        else
        {
            // When unhighlighting, if this is the active character, restore to active highlight
            if (IsActiveCharacter)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = highlightColor;
                    Debug.Log($"[Character Highlight] {name} is active character - kept highlighted");
                }
                
                if (characterImage != null)
                {
                    characterImage.color = highlightColor;
                }
            }
            else
            {
                // Reset to appropriate colors when not highlighted and not active
                if (spriteRenderer != null)
                {
                    // Use configurable colors
                    spriteRenderer.color = isEnemy ? originalColor : allyDefaultColor;
                    Debug.Log($"[Character Highlight] {name} spriteRenderer color reset to visible default");
                }
                
                if (characterImage != null)
                {
                    characterImage.color = isEnemy ? enemyDefaultColor : allyDefaultColor;
                    Debug.Log($"[Character Highlight] {name} characterImage color reset to default");
                }
            }
        }
    }

    public void ResetAction()
    {
        currentAction = 0f;
    }

    public void ApplyDefenseReduction()
    {
        Debug.LogWarning($"[DEPRECATED] {name} is using old ApplyDefenseReduction() method. Use StatusManager.ApplyStatus(character, StatusType.Vulnerable) instead!");
        
        HasReducedDefense = true;
        DefenseReductionTurnsLeft = defenseReductionDefaultDuration;
        DefenseReductionMultiplier = defenseReductionMultiplier;
        
        // Apply the same effect through the status system if available
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null)
        {
            statusManager.ApplyStatus(this, StatusType.Vulnerable, defenseReductionDefaultDuration);
            Debug.Log($"[Status Conversion] Converted legacy defense reduction to Vulnerable status for {name}");
        }
        else
        {
            Debug.LogError($"StatusManager not found! Falling back to legacy defense reduction for {name}");
            // Create defense reduction icon only if status manager isn't available
            CreateDefenseReductionIcon();
        }
    }

    public void RemoveDefenseReduction()
    {
        Debug.LogWarning($"[DEPRECATED] {name} is using old RemoveDefenseReduction() method. Use StatusManager.RemoveStatus(character, StatusType.Vulnerable) instead!");
        
        HasReducedDefense = false;
        DefenseReductionTurnsLeft = 0;
        DefenseReductionMultiplier = 1.0f;
        
        // Remove the status through the status system if available
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null)
        {
            statusManager.RemoveStatus(this, StatusType.Vulnerable);
            Debug.Log($"[Status Conversion] Removed Vulnerable status for {name}");
        }
        
        // Always remove the legacy icon
        if (DefenseReductionIcon != null)
        {
            Destroy(DefenseReductionIcon);
            DefenseReductionIcon = null;
        }
    }

    private void CreateDefenseReductionIcon()
    {
        // Remove any existing icon
        if (DefenseReductionIcon != null)
        {
            Destroy(DefenseReductionIcon);
        }
        
        Debug.Log($"[Piercing Shot] Creating defense reduction icon for {name}");
        
        // Load the Defense Reduction Icon prefab from the Icons folder
        GameObject defenseReductionIconPrefab = Resources.Load<GameObject>("Defense Reduction Icon");
        
        // If not found in Resources, try direct path
        if (defenseReductionIconPrefab == null)
        {
            #if UNITY_EDITOR
            defenseReductionIconPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Icons/Defense Reduction Icon.prefab");
            Debug.Log($"[Piercing Shot] Trying to load prefab from direct path: {defenseReductionIconPrefab != null}");
            #endif
        }
        
        if (defenseReductionIconPrefab != null)
        {
            // Create a new icon instance from the prefab
            GameObject icon = Instantiate(defenseReductionIconPrefab);
            icon.name = "DefenseReductionIcon";
            icon.transform.SetParent(transform);
            icon.transform.localPosition = defenseReductionIconOffset; // Use the configurable offset
            
            // Ensure the icon is visible by setting high sorting order on any renderers
            SpriteRenderer[] renderers = icon.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.sortingOrder = 20; // Higher sorting order to ensure visibility
            }
            
            icon.SetActive(true);
            
            // Store the reference
            DefenseReductionIcon = icon;
            Debug.Log($"[Piercing Shot] Icon prefab instantiated and attached to {name}");
        }
        else
        {
            Debug.LogWarning("[Piercing Shot] Defense Reduction Icon prefab not found. Creating a placeholder.");
            
            // Create a fallback icon if the prefab isn't found
            GameObject fallbackIcon = new GameObject("DefenseReductionIcon");
            fallbackIcon.transform.SetParent(transform);
            fallbackIcon.transform.localPosition = defenseReductionIconOffset; // Use the configurable offset
            
            // Create a visible placeholder
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.SetParent(fallbackIcon.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // Add a material with a defense reduction color
            Renderer visualRenderer = visual.GetComponent<Renderer>();
            visualRenderer.material.color = new Color(1f, 0.5f, 0f); // Orange defense reduction color
            
            // Make sure the GameObject has a high sorting layer
            visual.layer = LayerMask.NameToLayer("UI") != -1 ? LayerMask.NameToLayer("UI") : 5;
            
            // Remove the collider
            Destroy(visual.GetComponent<Collider>());
            
            // Store the reference
            DefenseReductionIcon = fallbackIcon;
            Debug.Log("[Piercing Shot] Created fallback icon for defense reduction");
        }
    }

    // Apply a speed boost effect
    public void BoostActionSpeed(float boostMultiplier, int durationInTurns)
    {
        // Store original action speed if this is the first boost
        if (speedBoostMultiplier == 1f)
        {
            baseActionSpeed = actionSpeed;
        }
        
        // Apply the boost multiplier
        speedBoostMultiplier = 1f + boostMultiplier;
        speedBoostTurnsRemaining = durationInTurns;
        
        // Update the action speed
        actionSpeed = baseActionSpeed * speedBoostMultiplier;
        
        Debug.Log($"{name}'s action speed boosted by {boostMultiplier * 100}% for {durationInTurns} turns. New speed: {actionSpeed}");
    }
    
    // Remove speed boost effect
    public void RemoveSpeedBoost()
    {
        if (speedBoostMultiplier != 1f)
        {
            speedBoostMultiplier = 1f;
            speedBoostTurnsRemaining = 0;
            actionSpeed = baseActionSpeed;
            
            Debug.Log($"{name}'s action speed returned to normal: {actionSpeed}");
        }
    }
    
    // Update speed boost duration at the end of this character's turn
    public void UpdateSpeedBoostDuration()
    {
        if (speedBoostTurnsRemaining > 0)
        {
            speedBoostTurnsRemaining--;
            
            if (speedBoostTurnsRemaining <= 0)
            {
                RemoveSpeedBoost();
            }
            else
            {
                Debug.Log($"{name}'s speed boost remaining: {speedBoostTurnsRemaining} turns");
            }
        }
    }

    // Method to calculate attack damage with status effects
    public float CalculateDamage(float baseDamage)
    {
        return baseDamage * attackMultiplier;
    }

    // Method to end turn - check for status effects ending
    public void EndTurn()
    {
        // Reset guarding status at end of turn
        if (IsGuarding)
        {
            RemoveGuardStatus();
        }
        
        // Update status effects
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null)
        {
            statusManager.UpdateStatusDurations(this);
        }
    }

    // Method to show damage popup
    private void ShowDamagePopup(float amount)
    {
        // Round down the damage to a whole number
        int wholeDamage = Mathf.FloorToInt(amount);
        
        // Create damage popup
        Vector3 popupPosition = transform.position + Vector3.up * 0.5f;
        DamagePopup.Create(popupPosition, wholeDamage, !isEnemy, transform, false);
        
        // Check if enemy is dead and needs to fade out
        if (isEnemy && currentHealth <= 0)
        {
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    // Method to update health bar visuals
    private void UpdateHealthBar()
    {
        if (healthFill != null)
        {
            float healthPercent = currentHealth / maxHealth;
            // Change the local position based on the fill amount to keep left-aligned
            healthFill.transform.localPosition = new Vector3(-0.5f + (healthPercent * 0.5f), 0, 0);
            healthFill.transform.localScale = new Vector3(healthPercent, 1, 1);
        }
    }

    // Method to show guard icon
    public void ShowGuardIcon()
    {
        CreateGuardIcon();
    }

    // Method to show guarded icon
    public void ShowGuardedIcon()
    {
        CreateGuardedIcon(this);
    }

    // Method to remove guard status
    public void RemoveGuardStatus()
    {
        // Reset guarding status
        IsGuarding = false;
        
        // Stop guarding ally (if any)
        StopGuarding();
        
        // Destroy guard icon
        if (GuardIcon != null)
        {
            Destroy(GuardIcon);
            GuardIcon = null;
        }
        
        // Log the change
        Debug.Log($"{name} guard status removed");
    }
} 