// Box Dialogue Sample
// This is a sample Ink script for interacting with a box

VAR itemName = "Item"
VAR hasBeenLooted = false

-> main

=== main ===
{hasBeenLooted:
    Nothing left in this box.
    -> END
}

You found: <b>{itemName}</b>!
# GIVE_ITEM:{itemName}
-> END 