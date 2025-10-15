// Fortune Teller Dialogue
// Tracks conversation count across all runs
// Player can only interact once per run

VAR conversationCount = 0

-> main

=== main ===
// Branch based on conversation count
{conversationCount == 0:
    -> conversation_0
}
{conversationCount == 1:
    -> conversation_1
}
{conversationCount == 2:
    -> conversation_2
}
{conversationCount == 3:
    -> conversation_3
}
{conversationCount >= 4:
    -> conversation_4_plus
}

=== conversation_0 ===
portrait: hood_neutral_1, You want to have your fortunes read?
* [Yes]
    portrait: hood_neutral_1, Your fortunes are etched on the lid of your eye
    portrait: hood_neutral_1, Close them and you will see it
    -> END
* [No]
    portrait: hood_neutral_1, Hehehe
    portrait: hood_neutral_1, Good choice. Good choice.
    -> END

=== conversation_1 ===
portrait: hood_neutral_1, You want to have your fortunes read?
* [Yes]
    portrait: hood_neutral_1, Walking in there night after night in the dark.
    portrait: hood_neutral_1, You say you do not know it?
    portrait: hood_neutral_1, Close your eyes and you will see it
    -> END
* [No]
    portrait: hood_neutral_1, Hehehe
    portrait: hood_neutral_1, You don't need me to tell you, do you?
    -> END

=== conversation_2 ===
portrait: hood_neutral_1, You want to have your fortunes read?
* [Yes]
    portrait: hood_neutral_1, Our magical thinker wants a fortune read
    portrait: hood_neutral_1, Why don't you read your own fortune and make it come true?
    portrait: hood_neutral_1, Are you capable of such a feat?
    -> END
* [No]
    portrait: hood_neutral_1, You know.
    portrait: hood_neutral_1, You've seen the ending before you began the story
    -> END

=== conversation_3 ===
portrait: hood_neutral_1, You want to have your fortunes read?
* [Wait, can you tell me what's the deal with the candle in the bottom room?]
    portrait: hood_neutral_1, A child at a birthday party tried making a wish yet forgot to blow out the candle
    portrait: hood_neutral_1, They stayed there, making the wish. For minutes, hours, weeks.
    portrait: hood_neutral_1, By the time they opened their eyes, the candle was as tall as the room
    portrait: hood_neutral_1, And no one in the world could blow it out
    -> END

=== conversation_4_plus ===
portrait: hood_neutral_1, Do you know the secret of being a good fortune teller
* [What is it?]
    portrait: hood_neutral_1, To know that things that happen never stop happening.
    -> END

