using UnityEngine;

[CreateAssetMenu(fileName = "ColdKey", menuName = "Items/Cold Key")]
public class ColdKey : BaseItem
{
    private void OnEnable()
    {
        // Ensure this is always a key item
        Type = ItemData.ItemType.KeyItem;
        RequiresTarget = false;
        
        // Set default properties if they aren't already set
        if (string.IsNullOrEmpty(Name))
        {
            Name = "Cold Key";
            Description = "A frigid key that seems to emanate cold. You won it from a mysterious figure in a game of Ravenbond. It might open something important.";
        }
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        // Key items typically aren't used directly, but we could add logic 
        // to display a message about the key's purpose if needed
        Debug.Log("The Cold Key cannot be used as an item. It's for unlocking something special.");
    }
} 