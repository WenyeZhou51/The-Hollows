using UnityEngine;

public class Character : MonoBehaviour
{
    public string characterName;
    public int maxHealth = 100;
    public int maxSanity = 100;
    public int currentHealth;
    public int currentSanity;
    public bool isPlayer = true;

    private void Start()
    {
        currentHealth = maxHealth;
        currentSanity = maxSanity;
    }

    public bool IsDead()
    {
        return currentHealth <= 0 || currentSanity <= 0;
    }

    public void TakeDamage(int physicalDamage, int sanityDamage)
    {
        currentHealth -= physicalDamage;
        currentSanity -= sanityDamage;
        
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentSanity = Mathf.Clamp(currentSanity, 0, maxSanity);
    }
} 