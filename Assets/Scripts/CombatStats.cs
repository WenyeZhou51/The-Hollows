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
        
        // First try to find the icon in the scene
        GameObject sceneIcon = GameObject.Find("icon");
        
        if (sceneIcon != null)
        {
            // Use the icon from the scene
            Debug.Log($"[Human Shield] Using icon from scene for guarded status");
            
            // Create a new icon instance
            GameObject icon = Instantiate(sceneIcon);
            icon.name = "GuardedIcon";
            icon.transform.SetParent(ally.transform);
            icon.transform.localPosition = new Vector3(0, -0.7f, 0); // Position further below the character for visibility
            icon.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f); // Make it larger
            
            // Ensure the icon is visible
            SpriteRenderer iconRenderer = icon.GetComponent<SpriteRenderer>();
            if (iconRenderer != null)
            {
                iconRenderer.sortingOrder = 20; // Higher sorting order to ensure visibility
                Debug.Log($"[Human Shield] Set icon sorting order to 20");
            }
            
            icon.SetActive(true);
            
            // Store the reference
            ally.GuardedIcon = icon;
            Debug.Log($"[Human Shield] Icon created from scene object and attached to {ally.name}");
            return;
        }
        
        // If scene icon not found, create a new icon
        GameObject icon2 = new GameObject("GuardedIcon");
        icon2.transform.SetParent(ally.transform);
        icon2.transform.localPosition = new Vector3(0, -0.7f, 0); // Position further below the character
        
        // Add a sprite renderer
        SpriteRenderer renderer = icon2.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 20; // Higher sorting order to ensure visibility
        
        // Try to load the sprite from the Icons folder
        Sprite guardedSprite = Resources.Load<Sprite>("Guarded Icon");
        
        // If not found in Resources, try direct path
        if (guardedSprite == null)
        {
            #if UNITY_EDITOR
            guardedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Icons/Guarded Icon.png");
            Debug.Log($"[Human Shield] Trying to load sprite from direct path: {guardedSprite != null}");
            #endif
        }
        
        renderer.sprite = guardedSprite;
        
        if (renderer.sprite != null)
        {
            Debug.Log($"[Human Shield] Using Guarded Icon sprite from Assets/Icons");
            renderer.color = Color.white; // Use original color
        }
        else
        {
            Debug.LogWarning("[Human Shield] Guarded Icon sprite not found. Creating a placeholder.");
            
            // Create a visible placeholder
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.SetParent(icon2.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); // Larger sphere
            
            // Add a material with a shield color
            Renderer visualRenderer = visual.GetComponent<Renderer>();
            visualRenderer.material.color = new Color(0, 0.5f, 1f); // Bright blue shield color
            // Can't set sortingOrder on a regular Renderer, only on SpriteRenderer
            
            // Make sure the GameObject with the sphere has a high sorting layer
            visual.layer = LayerMask.NameToLayer("UI") != -1 ? LayerMask.NameToLayer("UI") : 5; // Use UI layer if available
            
            // Remove the collider
            Destroy(visual.GetComponent<Collider>());
            
            Debug.Log("[Human Shield] Created blue sphere placeholder for guarded icon");
        }
        
        // Store the reference
        ally.GuardedIcon = icon2;
        Debug.Log($"[Human Shield] Guarded icon created and attached to {ally.name}");
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
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlight ? highlightColor : originalColor;
            Debug.Log($"[Character Highlight] {name} spriteRenderer color set to {(highlight ? "highlight" : "original")}");
        }
        else
        {
            Debug.Log($"[Character Highlight] {name} has no spriteRenderer");
        }
        
        if (characterImage != null)
        {
            characterImage.color = highlight ? highlightColor : (isEnemy ? Color.red : Color.white);
            Debug.Log($"[Character Highlight] {name} characterImage color set to {(highlight ? "highlight" : isEnemy ? "red" : "white")}");
        }
        else
        {
            Debug.Log($"[Character Highlight] {name} has no characterImage");
        }
        
        // If neither renderer is available, create a temporary visual indicator
        if (spriteRenderer == null && characterImage == null)
        {
            Debug.LogWarning($"[Character Highlight] {name} has no visual components to highlight!");
            
            // Check if we already have a highlight indicator
            Transform existingIndicator = transform.Find("HighlightIndicator");
            
            if (highlight)
            {
                // Only create if it doesn't exist and we're highlighting
                if (existingIndicator == null)
                {
                    GameObject indicator = new GameObject("HighlightIndicator");
                    indicator.transform.SetParent(transform);
                    indicator.transform.localPosition = Vector3.zero;
                    
                    // Create a visible highlight
                    SpriteRenderer indicatorRenderer = indicator.AddComponent<SpriteRenderer>();
                    indicatorRenderer.sprite = Resources.Load<Sprite>("HighlightSprite");
                    
                    if (indicatorRenderer.sprite == null)
                    {
                        // Create a primitive if no sprite is available
                        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        visual.transform.SetParent(indicator.transform);
                        visual.transform.localPosition = Vector3.zero;
                        visual.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                        
                        // Add a material with a highlight color
                        Renderer visualRenderer = visual.GetComponent<Renderer>();
                        visualRenderer.material.color = highlightColor;
                        
                        // Remove the collider
                        Destroy(visual.GetComponent<Collider>());
                        
                        Debug.Log($"[Character Highlight] Created fallback highlight indicator for {name}");
                    }
                    else
                    {
                        indicatorRenderer.color = highlightColor;
                        indicatorRenderer.sortingOrder = 15;
                        Debug.Log($"[Character Highlight] Created sprite highlight indicator for {name}");
                    }
                }
                else
                {
                    existingIndicator.gameObject.SetActive(true);
                    Debug.Log($"[Character Highlight] Activated existing highlight indicator for {name}");
                }
            }
            else if (existingIndicator != null)
            {
                // Hide the indicator when not highlighted
                existingIndicator.gameObject.SetActive(false);
                Debug.Log($"[Character Highlight] Deactivated highlight indicator for {name}");
            }
        }
    }

    public void ResetAction()
    {
        currentAction = 0f;
    }
} 