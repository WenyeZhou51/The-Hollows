// File: Ravenbond Dialogue.ink
// This is an Ink dialogue file for the Sphinx riddle challenge

// Variables to track game state
VAR player_health = 100
VAR player_max_sanity = 100
VAR has_cold_key = false
VAR hasInteractedBefore = false
-> start

=== start ===
portrait: Sphinx portrait, You wish for the key, and I wish to feast
portrait: Sphinx portrait, Then riddle my answer and answer me these:
-> riddle1

=== riddle1 ===
portrait: Sphinx portrait, Needle
-> riddle1_choices

=== riddle1_choices ===
* [What has a head and a tail, can flip but has no legs?] -> failure
* [What has an ear but cannot hear?] -> failure
* [What has an eye but cannot see?] -> riddle2

=== riddle2 ===
portrait: Sphinx portrait, Name
-> riddle2_choices

=== riddle2_choices ===
* [What belongs to you, but is more used by others?] -> riddle3
* [What goes up but never comes down?] -> failure
* [What is the difference between a raven and a writing desk?] -> riddle2_alternative

=== riddle2_alternative ===
portrait: Sphinx portrait, That's incorrect
portrait: Sphinx portrait, Although...I rather like the sound of that
portrait: Sphinx portrait, I'll overlook your mistake just this once
-> riddle3

=== riddle3 ===
portrait: Sphinx portrait, Tomorrow
-> riddle3_choices

=== riddle3_choices ===
* [What has legs but doesn't walk?] -> failure
* [What is always coming but never arrives?] -> win
* [What breaks when you say its name?] -> failure

=== win ===
The stranger spits out a metal key. It feels cold to the touch. # GIVE_COLD_KEY
~ has_cold_key = true
-> END

=== failure ===
portrait: Sphinx portrait, That makes no sense.
portrait: Magician neutral, Your questions make no sense!
All party members lose 20 HP. # RAVENBOND_FAILURE
~ player_health = player_health - 20
~ player_max_sanity = player_max_sanity - 20
-> END

// External functions that can be called from Unity via story.EvaluateFunction
=== function give_cold_key() ===
~ has_cold_key = true
~ return has_cold_key

=== function reduce_health_and_sanity() ===
~ player_health = player_health - 20
~ player_max_sanity = player_max_sanity - 20
~ return true
