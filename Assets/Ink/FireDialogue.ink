// Fire Dialogue - Different responses based on death count
// This dialogue branches based on death count from PersistentGameManager

VAR deathCount = 0

-> main

=== main ===
{
    - deathCount == 0:
        -> fire_death_0
    - deathCount == 1:
        -> fire_death_1
    - deathCount == 2:
        -> fire_death_2
    - deathCount >= 3 && deathCount <= 4:
        -> fire_death_3_4
    - deathCount >= 5 && deathCount <= 6:
        -> fire_death_5_6
    - else:
        -> fire_death_7_plus
}

// Death count 0 branch
=== fire_death_0 ===
It's a burning fire. It's light bouncing in the dim dungeon. 
You think about having some roasted marshmallows here after you're done saving the world.
-> END

// Death count 1 branch
=== fire_death_1 ===
The fire is warm. It seems to be cheering you on!
-> END

// Death count 2 branch
=== fire_death_2 ===
The fire is warm. You wish you could rest beside it a bit longer, but the clock is ticking, and the obelisk stands.
-> END

// Death count 3-4 branch
=== fire_death_3_4 ===
The fire burns dimmer than you remembered. 
Perhaps it's a visual thing. Maybe your eyes just adapted to the darkness.
-> END

// Death count 5-6 branch
=== fire_death_5_6 ===
The fire is foreign to you. Its light hurts your eyes.
-> END

// Death count 7+ branch
=== fire_death_7_plus ===
You shrink away from the flames.
-> END 