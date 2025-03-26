// NPC Dialogue Sample
// This is a sample Ink script for NPC dialogue

VAR hasInteractedBefore = false

-> main

=== main ===
{hasInteractedBefore:
    // This dialogue path is shown when the player has talked to this NPC before
    Welcome back, traveler! Need something else?
    * [Ask about the area again.]
        As I said before, be careful as you venture deeper.
        The creatures here can be quite dangerous.
        -> more_info
    * [Any new information?]
        Well, I've heard rumors of strange lights deeper in the caverns.
        Some say it's treasure... others think it's something worse.
        -> END
    * [Just checking in.]
        Glad to see you're still in one piece. Good luck out there!
        -> END
    - -> END
- else:
    // First time dialogue - original conversation
    Hello there, traveler! I'm the guardian of this entrance.
    What brings you to The Hollows?
    * [I'm exploring the area.]
        Ah, an adventurer! Be careful as you venture deeper.
        The creatures here can be quite dangerous.
        -> more_info
    * [I'm looking for treasure.]
        Treasure, you say? Well, there are rumors of ancient artifacts hidden within...
        But many have entered seeking riches, and few have returned.
        -> more_info
    * [Just passing through.]
        Nobody just "passes through" The Hollows, stranger.
        Everyone who enters has a purpose... or finds one.
        -> more_info
}

=== more_info ===
Would you like to know more about this place?
* [Yes, tell me about The Hollows.]
    The Hollows is an ancient labyrinth, created centuries ago by a powerful mage.
    It's said that the deeper you go, the more the walls shift and change.
    Some believe the place itself is alive, watching, waiting...
    -> advice
* [No, I'll find out for myself.]
    Brave and foolish. I like that combination.
    -> advice

=== advice ===
Before you go, take this advice: # SET_FLAG:ReceivedAdvice
Trust nothing that moves in the shadows.
And if you hear whispers... run.
* [Thank you for the warning.]
    May fortune favor you, brave one.
* [I can handle myself.]
    We shall see. They all say that at first.

-> END 