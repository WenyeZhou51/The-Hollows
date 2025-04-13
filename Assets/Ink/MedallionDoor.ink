// File: MedallionDoor.ink
// This is the Ink dialogue file for the Medallion Door

// Variables to track medallion state
VAR hasMedallionLeft = false
VAR hasMedallionRight = false
VAR hasBothMedallions = false
VAR canUnlock = false

-> start

=== start ===
{hasBothMedallions:
    -> unlock_door
    - else:
    -> check_medallions
}

=== check_medallions ===
{hasMedallionLeft && not hasMedallionRight:
    You place the left medallion into its slot, but nothing happens. Another slot remains empty.
    -> END
}
{hasMedallionRight && not hasMedallionLeft:
    You place the right medallion into its slot, but nothing happens. Another slot remains empty.
    -> END
}
{not hasMedallionLeft && not hasMedallionRight:
    The great door does not budge. Two circular slots lie on the wall.
    -> END
}

=== unlock_door ===
The giant door creaks open... # DOOR_UNLOCKED
-> END 