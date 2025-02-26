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
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 1f, 0.6f); // Default highlight color
    private Color defaultColor; // Store the default panel color

    public void Initialize()
    {
        // Get all sliders (should be in order: health, sanity, action)
        sliders = characterPanel.GetComponentsInChildren<Slider>();
        nameText = characterPanel.GetComponentInChildren<TextMeshProUGUI>();
        
        Debug.Log($"Panel {characterPanel.name}: Found nameText? {nameText != null}");
        
        panelImage = characterPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            defaultColor = panelImage.color;
        }
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

        // Update panel highlight
        if (panelImage != null)
        {
            panelImage.color = isActive ? highlightColor : defaultColor;
        }
    }
} 