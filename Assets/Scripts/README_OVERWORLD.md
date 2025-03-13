# Overworld Scene Setup Instructions

This README provides instructions on how to set up the overworld scene with player movement, interaction, and camera following.

## Setup Steps

1. **Create an Interactable Layer**:
   - Go to Edit > Project Settings > Tags and Layers
   - Add a new layer called "Interactable" (e.g., layer 8)

2. **Add the OverworldSetupMenu Component**:
   - Create an empty GameObject in your scene and name it "SceneSetup"
   - Add the `OverworldSetupMenu` component to it
   - Assign the following references in the Inspector:
     - Player Object: Your player GameObject (the blue square)
     - Box Object: Your box GameObject (the orange square)
     - NPC Object: Your NPC GameObject (the black triangle)
     - Main Camera: Your main camera
     - Interactable Layer: Set this to the "Interactable" layer you created

3. **Use the Setup Buttons**:
   - Click the "Setup Dialogue System" button to create the dialogue UI
   - Click the "Setup Player" button to add movement and interaction components
   - Click the "Setup Box" button to make the box interactable
   - Click the "Setup NPC" button to make the NPC interactable
   - Click the "Setup Camera Follow" button to make the camera follow the player

## Controls

- **Arrow Keys**: Move the player
- **Z Key**: Interact with objects/NPCs and close dialogue boxes

## Components Overview

- **PlayerController**: Handles player movement and interaction
- **CameraFollow**: Makes the camera follow the player
- **DialogueManager**: Manages dialogue display
- **InteractableBox**: Makes boxes interactable and shows item message
- **InteractableNPC**: Makes NPCs interactable and shows dialogue

## Customization

- You can adjust movement speed, interaction radius, and other parameters in the Inspector
- To change the item name for boxes, select the box and modify the "Item Name" field in the InteractableBox component
- To change the NPC name, select the NPC and modify the "NPC Name" field in the InteractableNPC component 