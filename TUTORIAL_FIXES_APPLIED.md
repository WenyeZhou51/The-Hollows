# Tutorial System Fixes

## Issues Fixed

### 1. ✅ Ink File Compile Errors - FIXED

**Problem:** `TutorialIntro.ink` had incorrect syntax and couldn't be parsed by Unity's Ink compiler.

**Solution:** Updated the ink file to use proper Ink syntax:
- Added proper `-> main` flow redirect
- Used `# speaker:` tag format instead of VAR assignments
- Removed unnecessary speaker variable
- Structured it like other working ink files in the project

**Before:**
```ink
VAR speaker = ""

=== main ===
~ speaker = "The Magician"
It's sure pretty dark here
```

**After:**
```ink
// Tutorial Intro Dialogue - Party enters the dark
-> main

=== main ===
It's sure pretty dark here # speaker: The Magician
```

### 2. ✅ METAMORPHOSIS in Tutorial - FIXED

**Problem:** Enemies could use METAMORPHOSIS ability during the tutorial battle, which would be confusing for new players.

**Solution:** Modified both enemy behavior scripts to automatically disable METAMORPHOSIS when in the `Battle_Tutorial` scene:
- `AperatureBehaviorPre.cs` - Now detects tutorial scene and sets metamorphosis chance to 0
- `WeaverBehaviorPre.cs` - Now detects tutorial scene and sets metamorphosis chance to 0

**How it works:**
```csharp
// At the start of ExecuteTurn():
float effectiveMetamorphosisChance = metamorphosisChance;
if (SceneManager.GetActiveScene().name == "Battle_Tutorial")
{
    effectiveMetamorphosisChance = 0f;
    Debug.Log("METAMORPHOSIS disabled in tutorial battle");
}
```

This means:
- In normal battles: Enemies can use METAMORPHOSIS normally
- In Battle_Tutorial: Metamorphosis chance is forced to 0, enemies will only use their other attacks
- No manual configuration needed - it's automatic!

## Files Modified

1. **Assets/Ink/TutorialIntro.ink** - Fixed ink syntax
2. **Assets/Scripts/AperatureBehaviorPre.cs** - Added tutorial scene detection
3. **Assets/Scripts/WeaverBehaviorPre.cs** - Added tutorial scene detection

## Testing

### Test the Ink File Fix:
1. In Unity, check the Console for ink compilation errors
2. Look for `Assets/Ink/TutorialIntro.json` - it should compile successfully
3. If it doesn't auto-compile, right-click `TutorialIntro.ink` → "Recompile Ink"

### Test METAMORPHOSIS Disable:
1. Play through to the tutorial battle
2. Watch enemy actions - they should only use:
   - **Aperature_pre:** Blinding Lights, Wobble, Basic Attack
   - **Weaver_pre:** Tangle, Poke, Connect, Basic Attack
3. **NEVER:** Metamorphosis
4. Check Console for: `[AperatureBehaviorPre] METAMORPHOSIS disabled in tutorial battle`

### Test Normal Battles (Post-Tutorial):
1. After completing tutorial, engage in a normal battle
2. Enemies should be able to use METAMORPHOSIS normally
3. This confirms the fix only affects Battle_Tutorial scene

## Debug Info

If METAMORPHOSIS still appears in tutorial:
1. Check Console for the disable log message
2. Verify scene name is exactly "Battle_Tutorial"
3. Confirm you're using the correct enemy prefabs with the updated behavior scripts

## Summary

✅ Both issues fixed with minimal changes
✅ No linter errors
✅ Automatic detection - works without configuration
✅ Only affects Battle_Tutorial scene - normal battles unchanged

