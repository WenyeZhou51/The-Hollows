# Dialogue System Instructions

## Overview
The dialogue system has been updated to use a prefab-based approach instead of generating UI elements at runtime. This makes it easier to customize the appearance of the dialogue UI and ensures consistency across scenes.

## Setup Instructions

### 1. Prefab Setup
1. The main prefab is `DialogueCanvas` located in the `Assets/Prefabs` folder.
2. A copy of this prefab should also be placed in the `Assets/Resources` folder for runtime loading.
3. The `DialogueButton` prefab is used for dialogue choices and must be assigned in the inspector.

#### Setting up the DialogueCanvas Prefab
1. Create an empty GameObject in your scene.
2. Add the `DialogueCanvasSetup` component to it.
3. Click the "Setup Canvas Components" button in the inspector.
4. Customize the appearance of the canvas as desired.
5. Click the "Create/Update Prefab" button to create the prefab in both the Prefabs and Resources folders.
6. The prefab should have:
   - A Canvas component with CanvasScaler and GraphicRaycaster
   - A DialoguePanel with an Image component and a DialogueText (TextMeshProUGUI) child
   - A DialogueButtonContainer with a VerticalLayoutGroup component for displaying choices

#### Setting up the DialogueButton Prefab
1. Create an empty GameObject in your scene.
2. Add the `DialogueButtonSetup` component to it.
3. Click the "Setup Button Components" button in the inspector.
4. Customize the appearance of the button as desired.
5. Click the "Create/Update Prefab" button to create the prefab in both the Prefabs and Resources folders.
6. The prefab should have:
   - A Button component
   - An Image component
   - A child with a TextMeshProUGUI component

### 2. Scene Setup
There are two ways to set up the dialogue system in your scene:

#### Option 1: Manual Setup in Scene
1. Create an empty GameObject in your scene.
2. Add the `DialogueManager` component to it.
3. Assign the `DialogueCanvas` prefab to the `Dialogue Canvas Prefab` field in the Inspector.
4. Assign the `DialogueButton` prefab to the `Choice Button Prefab` field in the Inspector.

#### Option 2: Create from Code
1. Call `DialogueManager.CreateInstance()` from your code to create a DialogueManager if one doesn't exist.
2. The DialogueManager will automatically try to load the prefabs from the Resources folder if they're not assigned.
3. Alternatively, you can call `DialogueManager.Instance.SetDialogueCanvasPrefab(yourPrefab)` to set the canvas prefab.

### 3. Using Ink for Dialogue
1. Create an Ink story file (.ink) and compile it to JSON.
2. Add the `InkDialogueHandler` component to any GameObject that should trigger dialogue.
3. Assign the compiled Ink JSON file to the `Ink JSON` field in the Inspector.
4. To trigger dialogue, call `DialogueManager.Instance.StartInkDialogue(inkHandler)` where `inkHandler` is a reference to the InkDialogueHandler component.

### 4. Simple Text Dialogue
For simple text dialogue without Ink:
1. Call `DialogueManager.Instance.ShowDialogue("Your dialogue text here")` to display a message.
2. Call `DialogueManager.Instance.CloseDialogue()` to close the dialogue.

## Keyboard Controls
The dialogue system now supports keyboard navigation:
- Press `Z` (or the key set in the `Interact Key` field) to continue dialogue or select a choice.
- Use the arrow keys (Up/Down) or W/S to navigate between choices.
- The currently selected choice will be highlighted.

## Customizing the Dialogue UI
1. You can modify the `DialogueCanvas` prefab to change the appearance of the dialogue UI.
2. The prefab contains:
   - `DialoguePanel`: The main panel that contains the dialogue text.
   - `DialogueText`: The TextMeshPro component that displays the dialogue text.
   - `DialogueButtonContainer`: The container that holds the dialogue choice buttons.
3. You can also modify the `DialogueButton` prefab to change the appearance of the choice buttons.
4. The DialogueManager has color settings for normal and highlighted button states.

## Troubleshooting
- If the dialogue UI doesn't appear, check the console for error messages.
- Make sure the `DialogueCanvas` prefab is correctly set up with all the required components.
- Ensure the `DialogueButton` prefab has a Button component and a TextMeshProUGUI component for the text.
- If loading from Resources fails, make sure there's a copy of the `DialogueCanvas` prefab in the `Assets/Resources` folder.
- If choices don't appear, make sure the `Choice Button Prefab` is assigned in the DialogueManager inspector.
- The most common error is forgetting to assign the `Choice Button Prefab` - this must be explicitly assigned!
- Make sure the DialogueButtonContainer in the DialogueCanvas prefab has a VerticalLayoutGroup component for proper button layout. 