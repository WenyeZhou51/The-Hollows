# Item Mismatch Analysis - "Unknown item" Issue

## Item Comparison: ItemManager vs LootTable

### Items in ItemManager (with descriptions):
1. ✅ "Fruit Juice" - "Heals 30 HP to all party members"
2. ✅ "Super Espress-O" - "Restores 50 SP and increases ally's action generation by 50% for 3 turns"
3. ✅ "Shiny Bead" - "Deals 20 damage to target enemy"
4. ✅ "Panacea" - "Heal target party member for 100HP and 100SP, remove all negative status effects"
5. ✅ "Tower Shield" - "Gives TOUGH to ally for 3 turns"
6. ✅ "Pocket Sand" - "WEAKENS all target enemies"
7. ✅ "Otherworldly Tome" - "Gives STRENGTH to all party members for 3 turns"
8. ✅ "Unstable Catalyst" - "Deals 40 damage to all enemies"
9. ✅ "Ramen" - "Heals ally for 15 HP"

### Items in LootTable (can drop):
1. ✅ "Fruit Juice" - "Heals 30 HP"
2. ✅ "Shiny Bead" - "Deals 20 damage to target enemy"
3. ✅ "Super Espress-O" - "Restores 50 SP and increases ally speed by 50%"
4. ✅ "Panacea" - "Heal target party member for 100HP and 100SP, remove all negative status effects"
5. ✅ "Tower Shield" - "Gives TOUGH to ally for 3 turns"
6. ✅ "Pocket Sand" - "WEAKENS all target enemies"
7. ✅ "Otherworldly Tome" - "Gives STRENGTH to all party members for 3 turns"
8. ✅ "Unstable Catalyst" - "Deals 40 damage to all enemies"
9. ✅ "Ramen" - "Heals ally for 15 HP"

### Special Items (created manually):
- ❌ **"Cold Key"** - KeyItem, created in multiple places but **NOT in ItemManager!**

## ⚠️ CRITICAL FINDING: Missing ItemManager Initialization

### Root Cause of "Unknown item"

**ItemManager requires a GameObject in the scene to initialize!**

Looking at `ItemManager.cs`:
```csharp
private void Awake()
{
    if (_instance != null && _instance != this)
    {
        Destroy(gameObject);
        return;
    }
    
    _instance = this;
    DontDestroyOnLoad(gameObject);
    
    // Initialize default items
    InitializeDefaultItems();  // ← This must run!
}
```

**If there's no ItemManager GameObject in your starting scene, the instance is NULL!**

This means:
- `ItemManager.Instance` returns `null`
- `GetItemData()` is never called
- Fallback triggers: "Unknown item"

## Solutions

### Solution 1: Create ItemManager GameObject (RECOMMENDED)
1. In your start scene (Start_Menu or Overworld_Startroom)
2. Create empty GameObject named "ItemManager"
3. Add `ItemManager` component to it
4. ItemManager will DontDestroyOnLoad and persist

### Solution 2: Add EnsureExists Pattern
Add to `ItemManager.cs`:

```csharp
public static ItemManager EnsureExists()
{
    if (_instance == null)
    {
        GameObject go = new GameObject("ItemManager");
        _instance = go.AddComponent<ItemManager>();
        DontDestroyOnLoad(go);
        Debug.Log("ItemManager created programmatically");
    }
    return _instance;
}
```

Then update PlayerInventory.cs line 68:
```csharp
// Ensure ItemManager exists before using it
ItemManager.EnsureExists();

if (ItemManager.Instance != null)
{
    // ... rest of code
}
```

### Solution 3: Add Cold Key to ItemManager

Add to `ItemManager.InitializeDefaultItems()` after line 109:

```csharp
// Cold Key (KeyItem)
ItemData coldKey = new ItemData(
    "Cold Key", 
    "A frigid key that seems to emanate cold. You won it from a mysterious figure in a game of Ravenbond.", 
    1, 
    false, 
    ItemData.ItemType.KeyItem);
itemTemplates.Add(coldKey.name, coldKey);
```

## Verification Steps

### Check if ItemManager exists:
1. Open Unity Console
2. Look for log: "Initialized X hardcoded items in ItemManager"
3. If missing → ItemManager never initialized!

### Check what items fail:
Look for warnings:
- "ItemData not found: [ITEM NAME]" → Item not in ItemManager
- "ItemManager doesn't have data for [ITEM NAME]" → Name mismatch or missing item

### Test in Play Mode:
```csharp
// Add temporary debug in Start scene
Debug.Log($"ItemManager exists: {ItemManager.Instance != null}");
if (ItemManager.Instance != null)
{
    var testItem = ItemManager.Instance.GetItemData("Fruit Juice");
    Debug.Log($"Can get Fruit Juice: {testItem != null}");
}
```

## Most Likely Cause

**You don't have an ItemManager GameObject in your scenes!**

The ItemManager component needs to be attached to a GameObject to run its `Awake()` method and initialize the item templates dictionary.

Without it:
- `ItemManager.Instance == null`
- All items show "Unknown item"
- Scrolling appears to work but items look identical

---

## Action Required

Choose ONE solution:
1. **Easiest:** Add ItemManager GameObject to Start_Menu scene
2. **Robust:** Implement EnsureExists() pattern
3. **Complete:** Both above + add Cold Key to ItemManager

Then test entering combat and checking item descriptions!

