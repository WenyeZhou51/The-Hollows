// File: Ravenbond Dialogue.ink
// This is an Ink dialogue file for the Ravenbond card game

// Variables to track game state
VAR player_health = 100
VAR player_max_sanity = 100
VAR has_glass_key = false
-> start
=== start ===
A tall man sits in the chair, a deck of cards in his hand.  
The cards are laden with golden borders. Patterns and sigils line their back.  
"Care to play a game of Ravenbond?" # speaker: Mysterious Figure

* [Yes] -> deal_hand
* [No] -> decline_game

=== decline_game ===
"..." # speaker: Mysterious Figure
-> END



=== deal_hand ===
You are dealt a hand of cards. The hooded figure silently plays a 7 of Wands and knocks on the wooden table.

* [Play "4 of wands" silently] -> step1_correct
* [Play "8 of swords" silently] -> failure
* [Knock on the table] -> failure
* ["What are the rules again?"] -> failure

=== step1_correct ===
The hooded figure silently plays a 4 of Swords, and waits for you.

* [Play "3 of Wands" silently] -> failure
* [Play "4 of Cups" silently] -> step2_correct
* [Play "4 of Cups" and knock on the table] -> failure
* [Accuse him of cheating] -> failure

=== step2_correct ===
The hooded figure silently plays a 5 of Cups, and knocks on the table.

* [Play "7 of Swords" silently] -> failure
* [Play "5 of Wands" and knock on the table] -> step3_correct
* [Play "5 of Wands" silently] -> failure
* [Say "Ravenbond"] -> failure

=== step3_correct ===
The hooded figure silently plays an 8 of Wands.

* [Play "2 of Wands" and stay silent] -> failure
* [Play "8 of Swords" and knock on the table] -> failure
* [Play "3 of Wands" and knock on the table] -> step4_correct
* [Say "You're so behind you might as well concede now"] -> failure

=== step4_correct ===
The hooded figure plays a special black card with an evening star on it and says "The Star". His voice echoes in the room. # speaker: Mysterious Figure

* [Play any number card] -> failure
* [Play "The hanged man" and knock on the table] -> failure
* [Play "The hanged man" and say "The hanged man"] -> step5_correct
* [Say "Thank you"] -> failure

=== step5_correct ===
The figure considers, then plays a card with a crumbling tower, a smile on his face. You have no more special cards in hand.

* [Draw two cards and say "Thank you"] -> step6_correct
* [Play "9 of wands" and stay silent] -> failure
* [Say "Ravenbond"] -> failure
* [Discard two cards and knock on the table] -> failure

=== step6_correct ===
Your play continues. You have a single card left.

* [Say "good game" and nod] -> failure
* [Stand on your chair and bow to him elegantly] -> failure
* [Say "I can coach you if you pay me"] -> failure
* [Say "Ravenbond"] -> win

=== win ===
The tall figure nods and acknowledges your skill. He puts down a glass key on the table before vanishing into a swirl of shadow tendrils.  
~ has_glass_key = true
-> END

=== failure ===
The tall figure shakes their head, and tendrils spread from the cards in your hand, grasping at your heart and soul.  
You lose 10% max HP and max Sanity.  
~ player_health = player_health * 0.9
~ player_max_sanity = player_max_sanity * 0.9
-> END

// External functions that can be called from Unity via story.EvaluateFunction
=== function give_glass_key() ===
~ has_glass_key = true
~ return has_glass_key

=== function reduce_health_and_sanity() ===
~ player_health = player_health * 0.9
~ player_max_sanity = player_max_sanity * 0.9
~ return true
