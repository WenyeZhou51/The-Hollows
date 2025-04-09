// File: LockedDoor.ink
// This is the Ink dialogue file for the Locked Door

// Variables to track door state
VAR hasColdKey = false
VAR canUnlock = false

-> start

=== start ===
{hasColdKey:
    -> unlock_door
    - else:
    -> locked_door
}

=== locked_door ===
The door is locked. The iron bars feels cold to the touch.
-> END

=== unlock_door ===
You unlock the door with the Cold Key. # DOOR_UNLOCKED
-> END 