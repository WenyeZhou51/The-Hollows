# Status Effect Cancellation System

This document explains how the status effect cancellation system works in The Hollows.

## Overview

The game implements a status effect cancellation system where certain opposing status effects will cancel each other out when applied to the same character. This applies to both player characters and enemies.

## Cancellation Pairs

The following status effects cancel each other out:

- **STRENGTH** and **WEAKNESS** - These affect attack multipliers
- **TOUGH** and **VULNERABLE** - These affect defense multipliers
- **AGILE** and **SLOWED** - These affect action speed

## How It Works

1. When a status effect is applied to a character, the system checks if the opposing status effect is already present.
2. If the opposing effect is found, both status effects are removed and the appropriate multiplier is reset to its default value (1.0 for attack/defense, base speed for action speed).
3. Visual indicators of both status effects are removed from the character.
4. Debug logs track when status effects cancel each other out.

## Implementation Details

The cancellation logic is implemented in the `StatusManager.cs` file:

- `ApplyStatus` - Checks for opposing status effects before applying a new one
- `GetOpposingStatus` - Maps each status effect to its opposing pair
- `ResetAppropriateMultiplier` - Ensures multipliers are properly reset when status effects cancel out

## Testing

A test script (`StatusCancellationTest.cs`) is available to verify the functionality. The test script:

1. Applies a status effect to a test character
2. Verifies the effect is applied correctly
3. Applies the opposing status effect
4. Verifies both effects are canceled and multipliers are reset

## Design Considerations

This system creates more strategic depth in combat:
- Characters can remove negative status effects by applying their opposing positive effects
- Enemies can counter player buffs by applying debuffs
- Players need to consider the order and timing of status effect application

## Usage Example

```csharp
// Example: If a character has WEAKNESS (attack down) and you apply STRENGTH (attack up):
statusManager.ApplyStatus(character, StatusType.Weakness); // Character now has reduced attack
statusManager.ApplyStatus(character, StatusType.Strength); // Both effects cancel out, attack returns to normal
``` 