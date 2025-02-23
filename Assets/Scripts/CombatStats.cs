using UnityEngine;
using UnityEngine.UI;

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
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // Create damage popup
        Vector3 popupPosition = transform.position + Vector3.up * 0.5f; // Adjust the Y offset as needed
        DamagePopup.Create(popupPosition, damage, !isEnemy);
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
        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlight ? highlightColor : originalColor;
        }
        if (characterImage != null)
        {
            characterImage.color = highlight ? highlightColor : (isEnemy ? Color.red : Color.white);
        }
    }
} 