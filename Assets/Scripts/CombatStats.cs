using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

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
    public bool isEnemy;
    
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
    [SerializeField] private float defenseReductionDefaultMultiplier = 1.5f; // Default multiplier (1.5 = 50% more damage)
    [Tooltip("Position offset for the defense reduction icon")]
    [SerializeField] private Vector3 defenseReductionIconOffset = new Vector3(0, -1.2f, 0);
    
    public bool HasReducedDefense { get; private set; } = false;
    public int DefenseReductionTurnsLeft { get; private set; } = 0;
    public float DefenseReductionMultiplier { get; private set; } = 1.0f; // 1.0 = normal damage, 1.5 = 50% more damage
    public GameObject DefenseReductionIcon { get; private set; } = null;

    // Add these fields for the blinking highlight effect
    private bool isHighlighted = false;
    private float blinkTimer = 0f;
    private bool isBlinkOn = false;
    private Color defaultColor = Color.white; // Default color for non-enemy characters
    
    // Track if this character is the active character (currently acting)
    private bool isActiveCharacter = false;
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
        if (isEnemy)
        {
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
        
        // Initialize current values based on max values from inspector
        currentHealth = maxHealth;
        currentSanity = maxSanity;
        currentAction = 0f; // Always start with empty action bar

        // Debug log to check character name
        Debug.Log($"Character initialized: {characterName}, isEnemy: {isEnemy}, maxHealth: {maxHealth}, maxAction: {maxAction}, actionSpeed: {actionSpeed}");

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
        
        // Handle blinking highlight effect - only if highlighted for targeting and not the active character
        if (isHighlighted && !isActiveCharacter)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                isBlinkOn = !isBlinkOn;
                
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

    public bool IsDead()
    {
        return currentHealth <= 0 || (!isEnemy && currentSanity <= 0);
    }

    public void TakeDamage(float damage)
    {
        // If this character is guarded, redirect damage to the guardian
        if (IsGuarded && Guardian != null && !Guardian.IsDead())
        {
            Debug.Log($"[Human Shield] {Guardian.name} takes {damage} damage instead of {name}");
            Guardian.TakeDamage(damage);
            return;
        }
        
        // Apply defense reduction multiplier if active
        float actualDamage = damage;
        if (HasReducedDefense)
        {
            actualDamage = damage * DefenseReductionMultiplier;
            Debug.Log($"[Piercing Shot] {name} takes increased damage due to reduced defense: {damage} -> {actualDamage}");
        }
        
        // Apply guard damage reduction if active
        if (IsGuarding)
        {
            actualDamage = actualDamage * GuardDamageReductionMultiplier;
            Debug.Log($"[Guard] {name} takes reduced damage due to guard stance: {damage} -> {actualDamage}");
        }
        
        currentHealth = Mathf.Max(0, currentHealth - actualDamage);
        
        // Create damage popup
        Vector3 popupPosition = transform.position + Vector3.up * 0.5f; // Adjust the Y offset as needed
        DamagePopup.Create(popupPosition, actualDamage, !isEnemy);
    }

    // Set up guarding relationship
    public void GuardAlly(CombatStats ally)
    {
        if (ally == null || ally == this || ally.isEnemy) return;
        
        // Set this character as the guardian of the ally
        ally.IsGuarded = true;
        ally.Guardian = this;
        
        // Set the ally as the protected character for this guardian
        this.ProtectedAlly = ally;
        
        Debug.Log($"[Human Shield] {name} is now guarding {ally.name}");
        
        // Create guarded icon for the ally
        CreateGuardedIcon(ally);
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
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        // Create healing popup
        Vector3 popupPosition = transform.position + Vector3.up * 0.5f; // Adjust the Y offset as needed
        HealingPopup.CreateHealthPopup(popupPosition, amount);
    }

    public void HealSanity(float amount)
    {
        if (!isEnemy)
        {
            currentSanity = Mathf.Min(maxSanity, currentSanity + amount);
            
            // Create sanity healing popup
            Vector3 popupPosition = transform.position + Vector3.up * 0.7f; // Slightly higher than health popup
            HealingPopup.CreateSanityPopup(popupPosition, amount);
        }
    }

    public void UseSanity(float amount)
    {
        if (!isEnemy)
        {
            currentSanity = Mathf.Max(0, currentSanity - amount);
        }
    }

    public void HighlightCharacter(bool highlight)
    {
        Debug.Log($"[Character Highlight] {name} highlight set to {highlight}, isEnemy: {isEnemy}");
        
        // If this is the active character, don't change its appearance through highlighting
        if (isActiveCharacter) return;
        
        // Store the highlight state
        isHighlighted = highlight;
        
        // Reset blink timer and state when highlight changes
        if (highlight)
        {
            blinkTimer = 0f;
            isBlinkOn = true;
            
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
            // Reset to appropriate colors when not highlighted
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

    public void ResetAction()
    {
        currentAction = 0f;
    }

    public void ApplyDefenseReduction()
    {
        HasReducedDefense = true;
        DefenseReductionTurnsLeft = defenseReductionDefaultDuration; // Use the inspector-configurable duration
        DefenseReductionMultiplier = defenseReductionDefaultMultiplier; // Use the inspector-configurable multiplier
        Debug.Log($"[Piercing Shot] {name} now has reduced defense. Turns left: {DefenseReductionTurnsLeft}, Multiplier: {DefenseReductionMultiplier}");
        
        // Create defense reduction icon
        CreateDefenseReductionIcon();
    }

    public void RemoveDefenseReduction()
    {
        HasReducedDefense = false;
        DefenseReductionTurnsLeft = 0;
        DefenseReductionMultiplier = 1.0f;
        Debug.Log($"[Piercing Shot] {name} defense reduction removed. Turns left: {DefenseReductionTurnsLeft}, Multiplier: {DefenseReductionMultiplier}");
        
        // Remove defense reduction icon
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
} 