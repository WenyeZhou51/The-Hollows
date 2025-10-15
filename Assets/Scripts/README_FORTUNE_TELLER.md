# Fortune Teller NPC Implementation

## Overview
The Fortune Teller is a unique NPC that tracks conversation history across all runs and can only be interacted with once per run before disappearing.

## Files Created
1. `Assets/Ink/FortuneNeller.ink` - Ink dialogue file with 5 different conversation branches
2. `Assets/Scripts/FortuneNeller.cs` - C# script implementing the Fortune Teller behavior

## Features

### Persistent Conversation Tracking
- **Conversation Count**: Tracks the total number of times the player has spoken to the Fortune Teller across ALL runs
- This count persists forever (until the game data is manually reset)
- Each conversation reveals different dialogue based on the count

### Per-Run Interaction Limit
- The Fortune Teller can only be interacted with **once per run**
- After one conversation, the Fortune Teller disappears (becomes invisible and non-interactable)
- When the player dies and starts a new run, the Fortune Teller reappears and can be interacted with again
- The conversation count increments after each interaction, so returning in a new run shows new dialogue

## Dialogue Branches

### Conversation 0 (First Interaction)
- Ft: "You want to have your fortunes read?"
  - **Yes**: "Your fortunes are etched on the lid of your eye / Close them and you will see it"
  - **No**: "Hehehe / Good choice. Good choice."

### Conversation 1 (Second Interaction)
- Ft: "You want to have your fortunes read?"
  - **Yes**: "Walking in there night after night in the dark. / You say you do not know it? / Close your eyes and you will see it"
  - **No**: "Hehehe / You don't need me to tell you, do you?"

### Conversation 2 (Third Interaction)
- Ft: "You want to have your fortunes read?"
  - **Yes**: "Our magical thinker wants a fortune read / Why don't you read your own fortune and make it come true? / Are you capable of such a feat?"
  - **No**: "You know. / You've seen the ending before you began the story"

### Conversation 3 (Fourth Interaction)
- Ft: "You want to have your fortunes read?"
  - **Player**: "Wait, can you tell me what's the deal with the candle in the bottom room?"
  - Ft: "A child at a birthday party tried making a wish yet forgot to blow out the candle / They stayed there, making the wish. For minutes, hours, weeks. / By the time they opened their eyes, the candle was as tall as the room / And no one in the world could blow it out"

### Conversation 4+ (Fifth and Beyond)
- Ft: "Do you know the secret of being a good fortune teller"
  - **Player**: "What is it?"
  - Ft: "To know that things that happen never stop happening."

## Setup Instructions

### In Unity Editor

1. **Create the Fortune Teller GameObject**:
   - In your Junction Up scene, create a new GameObject
   - Name it "Fortune Teller" or similar
   - Add a `SpriteRenderer` component and assign the Fortune Teller sprite
   - Add a `BoxCollider2D` component (or the script will add it automatically)

2. **Add the FortuneNeller Script**:
   - Select the Fortune Teller GameObject
   - Click "Add Component" and search for "FortuneNeller"
   - The script will automatically add an `InkDialogueHandler` component if needed

3. **Assign the Ink File**:
   - In the FortuneNeller component's Inspector
   - Find the "Ink File" field
   - Drag and drop `Assets/Ink/FortuneNeller.json` into this field
   - (Note: Make sure the .ink file has been compiled to .json first)

4. **Configure Settings** (Optional):
   - **NPC Name**: Default is "Fortune Teller" (can be changed for debugging)
   - **NPC ID**: Default is "FortuneNeller" (should remain unique)

5. **Test the Setup**:
   - Play the game
   - Approach the Fortune Teller and press the interact key (E or Space)
   - The dialogue should appear
   - After finishing the dialogue, the Fortune Teller should disappear
   - Die or restart the run, and the Fortune Teller should reappear with new dialogue

## Technical Details

### Persistence System
The Fortune Teller uses the `PersistentGameManager` to store two pieces of data:

1. **Conversation Count** (Key: `"FortuneNeller_ConversationCount"`)
   - Incremented each time dialogue ends
   - Never resets automatically (persists across all runs)
   - Used to determine which dialogue branch to show

2. **Interacted This Run** (Key: `"FortuneNeller_InteractedThisRun"`)
   - Set to `true` when player interacts with the Fortune Teller
   - Reset to `false` when player dies (via `PersistentGameManager.IncrementDeaths()`)
   - Used to prevent multiple interactions per run and hide the NPC

### Integration with Death System
When the player dies:
1. `PersistentGameManager.IncrementDeaths()` is called
2. This method now also calls `FortuneNeller.ResetRunState()`
3. The "Interacted This Run" flag is reset to `false`
4. When the scene loads again, the Fortune Teller checks this flag and reappears if it's `false`

### Disappearing Behavior
After dialogue ends:
1. The `HandleDialogueStateChanged` method is triggered
2. The conversation count is incremented and saved
3. `MakeInvisible()` is called which:
   - Disables the `SpriteRenderer` (makes the NPC invisible)
   - Disables the `Collider2D` (prevents interaction)

## Debugging

### Debug Logs
The Fortune Teller script includes extensive debug logging with the tag `[Fortune Teller]`. Look for these logs to diagnose issues:
- State loading/saving
- Interaction attempts
- Dialogue initialization
- Conversation count changes

### Manual Testing
- **To test different conversation branches**: Use the PersistentGameManager debug UI (F9 key) to view the conversation count
- **To reset all data**: Call `PersistentGameManager.Instance.ResetAllData()` from the debug menu or console
- **To increment deaths manually**: Press F1 key (this will also reset the Fortune Teller's per-run state)

### Common Issues

**Fortune Teller doesn't disappear after dialogue**:
- Check that the dialogue actually ended (not just closed mid-conversation)
- Verify `DialogueManager.OnDialogueStateChanged` event is being triggered
- Check console for any errors during dialogue

**Fortune Teller doesn't reappear after death**:
- Verify `IncrementDeaths()` is being called on player death
- Check that `FortuneNeller.ResetRunState()` is executing
- Look for the "Reset run state" debug log

**Wrong dialogue branch showing**:
- Check the conversation count value in PersistentGameManager
- Verify the Ink file is properly compiled to JSON
- Ensure the `conversationCount` variable is being set in the Ink story

**Fortune Teller not interactable**:
- Ensure the GameObject has a `Collider2D` component
- Check that the collider is not disabled
- Verify the GameObject is on a layer that can be interacted with
- Make sure the player's interaction system is working with other NPCs

## Extending the System

### Adding More Dialogue Branches
To add more conversation branches:

1. **Edit the Ink File** (`FortuneNeller.ink`):
   ```ink
   === conversation_5 ===
   New dialogue here...
   -> END
   ```

2. **Update the Main Routing**:
   ```ink
   === main ===
   {conversationCount == 5:
       -> conversation_5
   }
   ```

3. **Recompile** the Ink file to JSON

### Modifying the Per-Run Behavior
If you want to allow multiple interactions per run:
- Remove or comment out the `hasInteractedThisRun` check in the `Interact()` method
- Remove the `MakeInvisible()` call from `HandleDialogueStateChanged()`

### Resetting Conversation Count
To reset just the conversation count without resetting all game data:
```csharp
PersistentGameManager.Instance.SetCustomDataValue("FortuneNeller_ConversationCount", 0);
```

## Notes
- The Fortune Teller sprite and positioning should be set up manually in the Unity scene
- The script automatically adds required components (`InkDialogueHandler`, `BoxCollider2D`)
- The system is designed to work seamlessly with the existing dialogue system
- No modifications to other game systems are required (except the one-line addition to `IncrementDeaths()`)

