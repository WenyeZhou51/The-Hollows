using UnityEngine;
using UnityEngine.EventSystems;

public class HoverDescriptionHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CombatUI combatUI;
    private MenuSelector menuSelector;
    private SkillButtonData skillData;
    private ItemButtonData itemData;
    
    private void Awake()
    {
        // Find the CombatUI and MenuSelector components
        combatUI = FindObjectOfType<CombatUI>();
        menuSelector = FindObjectOfType<MenuSelector>();
        
        // Get the skill or item data from this button
        skillData = GetComponent<SkillButtonData>();
        itemData = GetComponent<ItemButtonData>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only show description when we're in skill/item menu, not during other actions
        if (combatUI == null || menuSelector == null) return;
        
        // Check if we're currently in a state where descriptions should be shown
        if (menuSelector.IsSelectingTarget()) return; // Don't show during target selection
        
        // Only show descriptions when we're actually in the skill/item menus
        if (!menuSelector.IsInSkillOrItemMenu()) return;
        
        // Don't override keyboard navigation descriptions immediately after keyboard input
        if (Time.time - lastKeyboardInputTime < 0.2f) return;
        
        // Show description based on what type of button this is
        if (skillData != null && skillData.skill != null)
        {
            combatUI.UpdateSkillDescription(skillData.skill);
            Debug.Log($"[Hover] Showing description for skill: {skillData.skill.name}");
        }
        else if (itemData != null && itemData.item != null)
        {
            combatUI.UpdateItemDescription(itemData.item);
            Debug.Log($"[Hover] Showing description for item: {itemData.item.name}");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Only clear description if we're not in target selection mode
        if (combatUI == null || menuSelector == null) return;
        
        // Don't clear during target selection as that might interfere with keyboard navigation
        if (menuSelector.IsSelectingTarget()) return;
        
        // Only clear descriptions when we're actually in the skill/item menus
        if (!menuSelector.IsInSkillOrItemMenu()) return;
        
        // Don't clear if the user recently used keyboard navigation (let keyboard selection take precedence)
        // This prevents mouse hover from interfering with keyboard-driven descriptions
        if (Time.time - lastKeyboardInputTime < 0.5f) return;
        
        // Clear the description when mouse leaves the button
        combatUI.ClearDescription();
        Debug.Log("[Hover] Cleared description on mouse exit");
    }
    
    // Track when keyboard input was last used to prevent mouse interference
    private static float lastKeyboardInputTime = 0f;
    
    private void Update()
    {
        // Track keyboard input to prioritize keyboard navigation over mouse hover
        if (Input.anyKeyDown && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || 
                                Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
                                Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z) ||
                                Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X)))
        {
            lastKeyboardInputTime = Time.time;
        }
    }
}
