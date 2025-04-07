# Exit Comic Display Setup Guide

This guide explains how to set up the exit comic display feature for the Overworld_entrance scene.

## Overview

When the player chooses to leave the dungeon via the exit dialogue, a sequence of comic panels will be displayed before the game exits. This feature leverages:

1. The existing **Ink Dialogue System** 
2. The existing **ComicsDisplayController** in the scene
3. A custom tag in the Ink script to trigger the comics

No additional components or scripts are required - we're using the existing systems.

## Setup Steps

### 1. Configure Comic Panels

The scene already has comic panels set up under the "Comics Canvas" object. Ensure these panels are configured as desired:

1. Open the Overworld_entrance scene
2. Find the "Comics Canvas" GameObject in the hierarchy
3. Select the ComicsDisplayController component
4. Configure the comic panels in the inspector:
   - Make sure the panels are in the desired order
   - Set appropriate transition directions for each panel
   - Verify that each panel has proper visual content
   - Ensure all panels are initially inactive

### 2. Verify Ink File Modification

The OverworldExit.ink file has been modified to include a special tag that triggers the comic display:

1. The choice "Leave the dungeon" now points to a new knot called "exit_with_comics"
2. This knot contains the tag "#SHOW_EXIT_COMICS" which is detected by the InkDialogueHandler
3. Verify this tag exists in the OverworldExit.ink file

### 3. Testing the Feature

To test the comic display:

1. Enter Play mode
2. Navigate to the exit area in the Overworld_entrance scene
3. Interact with the exit to trigger the dialogue
4. Choose "Yes" and then "Leave the dungeon"
5. The comic sequence should play automatically
6. After the sequence completes, the application will quit

## How It Works

1. When the player selects "Leave the dungeon" in the exit dialogue, the Ink story advances to a knot with the "#SHOW_EXIT_COMICS" tag
2. The InkDialogueHandler's ProcessTags() method detects this tag
3. The method finds the ComicsDisplayController instance in the scene
4. After a brief delay (to allow dialogue to close), it calls StartComicSequence()
5. The comic panels display in sequence as configured in the controller
6. After a delay to allow viewing the comics, Application.Quit() is called

## Troubleshooting

If the comic display doesn't work:

1. Check the console for error messages
2. Verify that the ComicsDisplayController exists in the scene and has properly configured panels
3. Make sure the OverworldExit.ink file has the "#SHOW_EXIT_COMICS" tag
4. Ensure the compiled JSON is up to date with the .ink file

## Notes

- This implementation uses existing systems rather than adding new components
- The tag-based approach is clean and maintainable
- Future expansions could include different comic sequences based on game state 