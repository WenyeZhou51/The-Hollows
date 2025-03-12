using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class CharacterUIElements
{
    public GameObject characterPanel; // The whole panel prefab
    private Slider[] sliders; // Will hold health, sanity, action bars
    private TextMeshProUGUI nameText;
    private Image panelImage; // Add reference to panel background image
    
    // Use pure white for active character's turn
    [SerializeField] private Color activeColor = Color.white;
    
    // Use the same grey as action buttons when not active (matching the action button color)
    [SerializeField] private Color inactiveColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    public void Initialize()
    {
        // Get all sliders (should be in order: health, sanity, action)
        sliders = characterPanel.GetComponentsInChildren<Slider>();
        nameText = characterPanel.GetComponentInChildren<TextMeshProUGUI>();
        
        Debug.Log($"Panel {characterPanel.name}: Found nameText? {nameText != null}");
        
        panelImage = characterPanel.GetComponent<Image>();
    }

    public void UpdateUI(CombatStats stats, bool isActive)
    {
        if (stats == null) return;

        // Update sliders
        if (sliders != null && sliders.Length >= 3)
        {
            sliders[0].value = stats.currentHealth / stats.maxHealth;
            sliders[1].value = stats.currentSanity / stats.maxSanity;
            sliders[2].value = stats.currentAction / stats.maxAction;
        }

        // Update name
        if (nameText != null)
        {
            nameText.text = stats.characterName;
        }

        // Update panel color based on active state
        if (panelImage != null)
        {
            panelImage.color = isActive ? activeColor : inactiveColor;
        }
    }
} 