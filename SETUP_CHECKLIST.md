# Tutorial Transition - Quick Setup Checklist

## ✅ What I've Done

1. ✅ Created `FirstTutorialTrigger.cs` - Intercepts Startroom exit
2. ✅ Created `TutorialIntro.ink` - Pre-combat dialogue
3. ✅ Modified `SceneTransitionManager.cs` - Handles post-tutorial transition
4. ✅ Added `Battle_Tutorial` to build settings
5. ✅ Uses existing `PersistentGameManager` to track tutorial completion

## 🎯 What You Need to Do in Unity

### 1. Compile the Ink File (1 minute)

1. Open Unity
2. Go to `Assets/Ink/TutorialIntro.ink`
3. Unity should auto-compile it to `TutorialIntro.json`
4. If not: Right-click → "Recompile Ink"

### 2. Setup Overworld_Startroom Scene (3 minutes)

1. **Open Scene:**
   - Open `Assets/Scenes/Overworld_Startroom.unity`

2. **Find the Exit:**
   - Look for the GameObject that has a `TransitionArea` component going to Overworld_Entrance
   - It should be a collider at the room's exit

3. **Replace with Tutorial Trigger:**
   - Select that GameObject
   - In Inspector, **remove** the `TransitionArea` component
   - Click "Add Component" → Search for `FirstTutorialTrigger` → Add it

4. **Configure FirstTutorialTrigger:**
   - **Target Scene Name**: `Overworld_Entrance`
   - **Target Marker Id**: `bottom_entrance` *(or whatever your bottom marker is called)*
   - **Tutorial Intro Dialogue**: Drag `Assets/Ink/TutorialIntro.json` here
   - **Auto Transition**: ✓ Checked

5. **Save Scene** (Ctrl+S)

### 3. Verify Overworld_Entrance Scene (1 minute)

1. **Open Scene:**
   - Open `Assets/Scenes/Overworld_Entrance.unity`

2. **Check Bottom Spawn Point:**
   - Find the spawn marker at the bottom of the scene
   - Check its ID/name - it should match what you set in step 2.4 above
   - Common names: `bottom_entrance`, `SpawnMarker_Bottom`, etc.
   - If the marker has a different ID, go back and update FirstTutorialTrigger's **Target Marker Id**

3. **Done!** Scene should already be set up.

## 🧪 Testing (2 minutes)

### Test 1: First Time Through
1. In Unity, press F9 to open debug menu
2. Click "RESET ALL DATA" (click twice to confirm)
3. Start the game
4. Play through to Startroom exit
5. **Expected Result:**
   - Screen goes black
   - Dialogue appears: Magician → Fighter → Ranger → Bard
   - Press Z to advance
   - Battle_Tutorial loads
   - Win the combat
   - You spawn at bottom of Overworld_Entrance

### Test 2: Second Time Through
1. Walk back to Startroom
2. Exit again
3. **Expected Result:**
   - No dialogue
   - Direct transition to Overworld_Entrance
   - Tutorial doesn't repeat

## 📋 Summary

**The Flow:**
```
First Exit from Startroom:
┌─────────────────┐
│  Startroom      │
│  Player exits   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Black Screen   │
│  + Dialogue     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Battle_Tutorial │
│  (Combat)       │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Overworld_      │
│ Entrance        │
│ (bottom)        │
└─────────────────┘

Subsequent Exits:
┌─────────────────┐
│  Startroom      │
│  Player exits   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Overworld_      │
│ Entrance        │
│ (direct)        │
└─────────────────┘
```

## 🐛 Troubleshooting

**Problem: Can't find FirstTutorialTrigger component**
- Solution: Save all scripts and wait for Unity to compile

**Problem: TutorialIntro.json not found**
- Solution: Check `Assets/Ink/` folder, right-click TutorialIntro.ink → Recompile Ink

**Problem: Tutorial repeats every time**
- Solution: Check console for `[TRANSITION DEBUG] Marked tutorial as completed`
- Make sure you're winning the tutorial combat (not losing)

**Problem: Player spawns in wrong location**
- Solution: Check the marker ID in Overworld_Entrance matches FirstTutorialTrigger's Target Marker Id

## 📝 Notes

- Tutorial completion is saved permanently (survives game restarts)
- To reset tutorial for testing: Press F9 → RESET ALL DATA
- All dialogue uses the speaker name format (shows "The Magician:", etc.)
- Losing the tutorial combat will NOT mark it as completed (player can retry)

