using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUI : MonoBehaviour
{
    public GameObject actionMenu;
    public GameObject skillMenu; // Add if you have a separate skill menu
    public GameObject itemMenu;  // Add if you have a separate item menu
    
    [System.Serializable]
    public class CharacterUIElements
    {
        public GameObject characterPanel; // The whole panel prefab
        private Slider[] sliders; // Will hold health, sanity, action bars
        private TextMeshProUGUI nameText;

        public void Initialize()
        {
            // Get all sliders (should be in order: health, sanity, action)
            sliders = characterPanel.GetComponentsInChildren<Slider>();
            nameText = characterPanel.GetComponentInChildren<TextMeshProUGUI>();
        }

        public void UpdateUI(CombatStats stats)
        {
            if (sliders == null) Initialize();

            // Update health bar (first slider)
            if (sliders.Length > 0)
                sliders[0].value = stats.currentHealth / stats.maxHealth;
            
            // Update sanity bar (second slider)
            if (sliders.Length > 1)
                sliders[1].value = stats.currentSanity / stats.maxSanity;
            
            // Update action bar (third slider)
            if (sliders.Length > 2)
                sliders[2].value = stats.currentAction / stats.maxAction;
        }
    }

    public CharacterUIElements[] characterUI;
    private CombatManager combatManager;
    private MenuSelector menuSelector;

    private void Start()
    {
        combatManager = GetComponent<CombatManager>();
        menuSelector = GetComponent<MenuSelector>();
        
        // Always show the menu
        actionMenu.SetActive(true);
        
        // Initialize all character UI elements
        foreach (var ui in characterUI)
        {
            ui.Initialize();
        }
        
        menuSelector.SetMenuItemsEnabled(false); // Start with menu disabled but visible
        UpdateUI();
    }

    public void UpdateUI()
    {
        for (int i = 0; i < combatManager.players.Count; i++)
        {
            if (i < characterUI.Length)
            {
                characterUI[i].UpdateUI(combatManager.players[i]);
            }
        }
    }

    public void ShowActionMenu(CombatStats character)
    {
        // Hide other menus
        if (skillMenu != null) skillMenu.SetActive(false);
        if (itemMenu != null) itemMenu.SetActive(false);
        
        menuSelector.SetMenuItemsEnabled(true);
        menuSelector.EnableMenu();
    }

    public void ShowSkillMenu()
    {
        if (skillMenu != null)
        {
            actionMenu.SetActive(false);
            skillMenu.SetActive(true);
        }
    }

    public void ShowItemMenu()
    {
        if (itemMenu != null)
        {
            actionMenu.SetActive(false);
            itemMenu.SetActive(true);
        }
    }

    public void BackToActionMenu()
    {
        actionMenu.SetActive(true);
        if (skillMenu != null) skillMenu.SetActive(false);
        if (itemMenu != null) itemMenu.SetActive(false);
    }

    public void HideActionMenu()
    {
        menuSelector.SetMenuItemsEnabled(false);
        menuSelector.DisableMenu();
    }

    public void OnAttackSelected()
    {
        // This is now handled by MenuSelector's target selection
    }

    public void OnTargetSelected(CombatStats target)
    {
        combatManager.ExecutePlayerAction("attack", target);
    }

    public void OnSkillSelected()
    {
        // Implement skills
    }

    public void OnGuardSelected()
    {
        // Implement guard
    }

    public void OnHealSelected()
    {
        combatManager.ExecutePlayerAction("heal");
    }
} 