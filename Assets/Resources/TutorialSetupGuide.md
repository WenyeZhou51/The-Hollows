# Battle Tutorial Setup Guide

This guide explains how to set up the tutorial dialogue system with UI highlighting for `Battle_Tutorial` scene.

## Files Created

1. **TutorialHighlighter.cs** - Manages UI element highlighting
2. **TutorialDialogueHandler.cs** - Extended InkDialogueHandler that processes highlight tags
3. **TutorialBattleDialogueTrigger.cs** - Battle dialogue trigger using tutorial handlers
4. **battle_tutorial_intro.ink** - Tutorial dialogue script

## Setup Instructions

### Step 1: Compile the Ink File

1. Open `Assets/Ink/battle_tutorial_intro.ink` in Unity
2. Unity should automatically compile it to JSON (battle_tutorial_intro.json)
3. If not, right-click the .ink file and select "Recompile Ink"
4. The compiled JSON will be in the same folder: `Assets/Ink/battle_tutorial_intro.json`

### Step 2: Create a Highlight Sprite (Optional)

1. In Unity, create a new Sprite:
   - Right-click in Project → Create → Sprites → Circle (or use any existing circle/frame sprite)
   - Name it "HighlightSprite"
   - Place it in `Assets/Sprites/UI/` folder
2. Set the sprite color to white in the texture settings

**OR** you can skip this - the system will create a simple colored overlay if no sprite is assigned.

### Step 3: Setup Battle_Tutorial Scene

1. Open the `Battle_Tutorial` scene
2. Find your CombatManager GameObject (or create an empty GameObject if needed)
3. Add the `TutorialBattleDialogueTrigger` component:
   - Add Component → Scripts → Tutorial Battle Dialogue Trigger
4. In the Inspector, configure the component:
   - **Intro Dialogue JSON**: Drag `battle_tutorial_intro.json` here
   - **Play Intro Dialogue**: Check this box
   - **Play Victory Dialogue**: Uncheck (unless you have victory dialogue)
5. Add the `TutorialHighlighter` component to the same GameObject (or a new one):
   - Add Component → Scripts → Tutorial Highlighter
6. Configure TutorialHighlighter (optional):
   - **Highlight Sprite**: Drag your highlight sprite here (optional)
   - **Highlight Color**: Yellow with 50% alpha (default)
   - **Highlight Scale**: 1.2 (makes highlight 20% larger than target)
   - **Pulse Speed**: 2.0
   - **Pulse Intensity**: 0.1

### Step 4: Ensure CombatManager Has Required Method

The tutorial system needs CombatManager to have a `SetCombatActive(bool)` method to pause/resume combat.

If this method doesn't exist, add it to `CombatManager.cs`:

```csharp
public void SetCombatActive(bool active)
{
    isCombatActive = active;
    Debug.Log($"Combat active set to: {active}");
}
```

### Step 5: Test the Tutorial

1. Play the Battle_Tutorial scene
2. After 1 second, the tutorial dialogue should start
3. As you advance through dialogue, UI elements should highlight:
   - Player HP bar
   - Player Mind bar
   - Player Action bar
   - Enemy HP bar
   - Enemy Action bar
   - Attack button
   - Guard button
   - Skills button
   - Items button

## Customization

### Adding More Highlight Tags

In your Ink dialogue, use these tags:

```ink
"Some dialogue text" # HIGHLIGHT:element_name
"More dialogue" # UNHIGHLIGHT:element_name
"Clear all" # CLEAR_HIGHLIGHTS
```

### Supported Element Names

The system recognizes these element names by default:
- `player_health`, `player_hp`, `health_bar` - Player health bar
- `player_mind`, `player_sanity`, `mind_bar`, `sanity_bar` - Player mind bar
- `player_action`, `action_bar` - Player action bar
- `enemy_health`, `enemy_hp` - Enemy health bar
- `enemy_action` - Enemy action bar
- `attack_button` - Attack button in action menu
- `guard_button` - Guard button in action menu
- `skill_button`, `skills_button` - Skills button
- `item_button`, `items_button` - Items button

You can also use exact GameObject names from the scene hierarchy.

### Creating Additional Tutorial Dialogues

To create more tutorial dialogues:

1. Create a new .ink file in `Assets/Resources/Ink/`
2. Use the highlight tags as needed
3. Assign the compiled JSON to TutorialBattleDialogueTrigger's mid-battle or victory dialogue slots

## Troubleshooting

**Highlights not appearing:**
- Check that TutorialHighlighter component exists in the scene
- Check the console for warnings about missing UI elements
- Verify element names match the supported names or actual GameObject names

**Dialogue not pausing combat:**
- Ensure CombatManager has the `SetCombatActive(bool)` method
- Check that combatManager reference is found in TutorialBattleDialogueTrigger

**Highlights staying after dialogue:**
- The system should auto-clear when dialogue ends
- If not, manually call `TutorialHighlighter.Instance.RemoveAllHighlights()`

**Ink file not compiling:**
- Make sure the Ink Unity integration plugin is installed
- Right-click the .ink file and select "Recompile Ink"

## Portrait Setup

The tutorial dialogue uses portraits. Make sure you have these portraits in your `Assets/Sprites/Portraits/` or `Assets/Resources/Portraits/` folder:
- ranger_neutral
- fighter_neutral
- bard_neutral
- magician_neutral

If portraits are missing, the dialogue will still work but won't show character portraits.

