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
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // Yellow-ish highlight
    private Color originalColor;
    public string characterName;
    public List<SkillData> skills = new List<SkillData>();
    
    // Properties for the Human Shield skill
    public bool IsGuarded { get; private set; } = false;
    public CombatStats Guardian { get; private set; } = null;
    public CombatStats ProtectedAlly { get; private set; } = null;
    public GameObject GuardedIcon { get; private set; } = null;

    // Add these fields for the blinking highlight effect
    private bool isHighlighted = false;
    private float blinkTimer = 0f;
    private float blinkInterval = 0.3f; // Changed to 0.3 seconds as requested
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
                    spriteRenderer.color = isEnemy ? originalColor : Color.white;
                }
                if (characterImage != null)
                {
                    characterImage.color = isEnemy ? Color.red : Color.white;
                }
            }
        }
    }

    private void Start()
    {
        if (isEnemy)
        {
            maxHealth = 20f;
            maxAction = 100f;
            if (characterImage != null)
            {
                characterImage.color = Color.red; // Make enemies red-tinted
            }
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            // Enemies don't use sanity
            
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
        
        currentHealth = maxHealth;
        currentSanity = maxSanity;
        currentAction = 0f;

        // Debug log to check character name
        Debug.Log($"Character initialized: {characterName}, isEnemy: {isEnemy}");

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
                    // Alternate between yellow and white (not disappearing)
                    spriteRenderer.color = isBlinkOn ? highlightColor : Color.white;
                }
                
                if (characterImage != null)
                {
                    // Alternate between yellow and appropriate default color
                    characterImage.color = isBlinkOn ? highlightColor : (isEnemy ? Color.red : Color.white);
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
        
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // Create damage popup
        Vector3 popupPosition = transform.position + Vector3.up * 0.5f; // Adjust the Y offset as needed
        DamagePopup.Create(popupPosition, damage, !isEnemy);
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
            Debug.Log($"[Human Shield] {name} stopped guarding {ProtectedAlly.name}");
            ProtectedAlly.IsGuarded = false;
            ProtectedAlly.Guardian = null;
            
            // Remove the guarded icon
            if (ProtectedAlly.GuardedIcon != null)
            {
                Destroy(ProtectedAlly.GuardedIcon);
                ProtectedAlly.GuardedIcon = null;
            }
            
            ProtectedAlly = null;
        }
    }
    
    // Create a visual indicator for guarded status
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
            icon.transform.localPosition = new Vector3(0, -1.2f, 0); // Position below the character
            //icon.transform.localScale = new Vector3(1f, 1f, 1f); // Use original scale or adjust as needed
            
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
            fallbackIcon.transform.localPosition = new Vector3(0, -0.7f, 0); // Position below the character
            
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

    public void HealHealth(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
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
                // Always use white for non-highlighted sprites to ensure visibility
                spriteRenderer.color = isEnemy ? originalColor : Color.white;
                Debug.Log($"[Character Highlight] {name} spriteRenderer color reset to visible default");
            }
            
            if (characterImage != null)
            {
                characterImage.color = isEnemy ? Color.red : Color.white;
                Debug.Log($"[Character Highlight] {name} characterImage color reset to default");
            }
        }
    }

    public void ResetAction()
    {
        currentAction = 0f;
    }
} 