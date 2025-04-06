# Comics Display System

A simple system for displaying sequential comic panels in Unity.

## Features

- Show comic panels in sequence with fade-in/out transitions
- Multiple transition directions (TOP, BOTTOM, LEFT, RIGHT)
- Trigger comic sequences via collider triggers or manually with F5 key
- Compatible with the existing dialogue system
- Freezes player movement during comic sequences
- Customizable transition speeds and animations

## Setup Instructions

### 1. Basic Setup

1. Create a Canvas in your scene (UI > Canvas)
2. Add the `ComicsDisplayController` script to the Canvas or a child GameObject
3. Add UI Image elements as children of the Canvas for each comic panel
   - Use the provided `ComicPanel` prefab as a template
   - Set each panel to inactive initially (uncheck the checkbox in the Inspector)
   - Assign your comic art to each panel's Image component

### 2. Configure Panels in ComicsDisplayController

1. Select your GameObject with the `ComicsDisplayController` script
2. In the Inspector, use the "Comic Panels" list to add your panels:
   - Click "+" to add a new panel entry
   - Drag the panel GameObject to the "Panel Object" field
   - Select the desired transition direction for each panel

### 3. Setting Up Trigger Areas

1. Create an empty GameObject in your scene
2. Add a Collider2D component (Box Collider 2D is recommended)
3. Make sure "Is Trigger" is checked
4. Add the `ComicsTrigger` script to this GameObject
5. Configure the trigger settings:
   - Set the player layer mask
   - Configure "Play Once" if it should only trigger once
   - Add the comic panels in sequence from your Canvas

## Usage

### Manual Testing

- Press F5 during gameplay to manually trigger the comic sequence
- Press Z to advance to the next panel
- When all panels have been shown, gameplay will resume

### Trigger Areas

- Place trigger GameObjects in your scene
- Configure which panels should be shown
- Players will automatically see the comic sequence when entering the trigger area

### Code Integration

You can also trigger sequences from other scripts:

```csharp
// Find the controller
ComicsDisplayController controller = ComicsDisplayController.Instance;

// Start the sequence
if (controller != null)
{
    controller.StartComicSequence();
}
```

## Customization

- Adjust fade duration and slide distance in the ComicsDisplayController Inspector
- Change key bindings for triggering and advancing panels
- Modify `ComicsDisplayController.cs` animations for custom transitions

## Notes

- Comic panels should be added to a canvas and positioned as desired
- All panels are made inactive at start and shown only when needed
- The panels will fade in with a sliding animation based on transition direction
- Player movement is disabled during the sequence
- Time.timeScale is set to 0 during the sequence to pause the game 