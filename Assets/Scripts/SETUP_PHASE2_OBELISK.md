# Phase 2 Obelisk Setup Guide

This guide explains how to set up the Phase 2 Obelisk with the coinflip animations for the Sunder ability.

## 1. Setting Up the Animator

### Create Animator Controller

1. In the Project window, navigate to `Assets/Animations`
2. Right-click and select `Create > Animator Controller`
3. Name it `CoinflipAnimator`

### Configure the Animator

1. Open the `CoinflipAnimator` in the Animator window
2. Create three states:
   - `Idle` (default state)
   - `CoinflipSuccess`
   - `CoinflipFail`
3. Create transitions:
   - From `Idle` to `CoinflipSuccess` with trigger parameter `PlayCoinflipSuccess`
   - From `Idle` to `CoinflipFail` with trigger parameter `PlayCoinflipFail`
   - From both `CoinflipSuccess` and `CoinflipFail` back to `Idle` when the animation finishes

## 2. Create Animations from Sprite Sheets

1. Select your coinflip success sprite sheet in the Project window
2. In the Inspector, make sure Sprite Mode is set to "Multiple"
3. Click the "Sprite Editor" button to open the Sprite Editor
4. Use the "Slice" button to automatically slice the sprite sheet
5. Click "Apply" to save the slices
6. Back in the Inspector, go to the Animation section at the bottom
7. Click "Create" in the Animations section, and save the Animation clip as "CoinflipSuccess"
8. Repeat steps 1-7 for the coinflip fail sprite sheet, naming the Animation "CoinflipFail"

## 3. Assign Animations to Animator States

1. Open the `CoinflipAnimator` again in the Animator window
2. Select the `CoinflipSuccess` state and assign the `CoinflipSuccess` animation in the Inspector
3. Select the `CoinflipFail` state and assign the `CoinflipFail` animation in the Inspector
4. Make sure both animations have appropriate settings (e.g., loop time unchecked)

## 4. Setting Up the Game Object

1. Create a new GameObject as a child of your Phase 2 Obelisk prefab named "CoinflipVisuals"
2. Add a Sprite Renderer component to it
3. Add an Animator component to it
4. Assign the `CoinflipAnimator` controller to the Animator component
5. Position the GameObject appropriately for where you want the animation to appear

## 5. Configure the Phase2ObeliskBehavior Script

1. On your Phase 2 Obelisk prefab, add the `Phase2ObeliskBehavior` script
2. In the Inspector, configure the script properties:
   - Set appropriate probability values for the two skills
   - Assign the Animator component from the CoinflipVisuals GameObject to the `coinflipAnimator` field
   - Set `coinflipSuccessTrigger` to "PlayCoinflipSuccess"
   - Set `coinflipFailTrigger` to "PlayCoinflipFail"
   - Assign the CoinflipVisuals GameObject to the `coinflipVisuals` field

## 6. Testing

1. Make sure the CoinflipVisuals GameObject is initially disabled
2. The script will enable/disable it automatically during the Sunder skill
3. Test the battle to ensure the animations play correctly

## Notes on Animation Triggers

The `Phase2ObeliskBehavior` script uses the following animation trigger parameters:

- `PlayCoinflipSuccess`: Triggered when the coinflip results in the player surviving
- `PlayCoinflipFail`: Triggered when the coinflip results in the player being killed

Make sure these exact strings match the trigger parameter names in your Animator Controller. 