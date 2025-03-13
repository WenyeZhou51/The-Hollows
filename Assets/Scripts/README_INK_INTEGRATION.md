# Ink Integration for The Hollows

This README provides instructions on how to set up and use Ink files with NPCs and interactable objects in The Hollows.

## Setup Instructions

1. **Create Ink Files**:
   - Create your Ink files in the `Assets/Ink` directory
   - Use the provided samples (`NPC_Dialogue.ink` and `Box_Dialogue.ink`) as templates
   - Compile your Ink files to JSON using the Inky editor or Unity's Ink integration

2. **Assign Ink Files to Objects**:
   - Select an NPC or Box in your scene
   - In the Inspector, find the `InteractableNPC` or `InteractableBox` component
   - Assign the compiled JSON file to the `Ink File` field

3. **Ink File Structure**:
   - Start with a main knot: `=== main ===`
   - Use choices with `*` syntax
   - Use tags with `#` for special commands (e.g., `# GIVE_ITEM:HealthPotion`)
   - End your story with `-> END`

## Special Tags

The system supports the following special tags:

- `GIVE_ITEM:ItemName` - Gives the player an item (e.g., `# GIVE_ITEM:FruitJuice`)
- `SET_FLAG:FlagName` - Sets a game flag (e.g., `# SET_FLAG:ReceivedAdvice`)

## Example Ink File

```ink
// NPC Dialogue Sample
-> main

=== main ===
Hello there, traveler!
* [Greet back]
    Nice to meet you too!
    -> END
* [Ask about the area]
    What can you tell me about this place?
    -> area_info

=== area_info ===
This is The Hollows, a dangerous place.
Be careful as you explore.
-> END
```

## Customizing the Dialogue UI

The dialogue UI is created automatically by the `AutoSceneSetup` script. If you want to customize it:

1. Find the `DialogueCanvas` GameObject in your scene
2. Modify the `DialoguePanel` and `ChoicesPanel` as needed
3. Adjust the `DialogueManager` component settings:
   - `Typing Speed` - Controls how fast text appears
   - `Use Typewriter Effect` - Toggles the typewriter effect

## Troubleshooting

- If dialogue doesn't appear, check the console for errors
- Make sure your Ink file is compiled to JSON
- Verify that the Ink file is assigned to the object
- Check that the object has the correct Interactable component

## Advanced Usage

For more advanced Ink features, refer to the [Ink documentation](https://github.com/inkle/ink/blob/master/Documentation/WritingWithInk.md). 