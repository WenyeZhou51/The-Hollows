// CakeWish Dialogue - Different responses based on death count
VAR deathCount = 0

A cake with a candle on it.
Make a wish?
* Yes
    -> make_wish
* No
    -> END

=== make_wish ===
{
    - deathCount >= 0 && deathCount <= 1:
        -> death_0_1
    - deathCount >= 2 && deathCount <= 3:
        -> death_2_3
    - deathCount >= 4:
        -> death_4_plus
}

=== death_0_1 ===
The candle burns brightly with the strenghth of your wish
-> wish_options

=== death_2_3 ===
The candle burns brightly with the strenghth of your wish

It is melting
-> wish_options

=== death_4_plus ===
The candle burns brightly with the strenghth of your wish

It is melting

You see yourself in it
-> wish_options

=== wish_options ===
What do you wish for?
* [wish for time to flow backwards]
    Backwards and backwards, undoing every mistake, until the world, like puzzles falling into place, deliver your victory.
    -> END
* [wish for time to stop]
    Eyes open, eyes closed, you march on and give no pause. Ironic, isn't it, that you have all of eternity to fall the obelisk, but not a single moment to breathe.
    -> END
* [wish for time to flow forwards]
    Restarting and rewinding. Your determined eyes and the pitched Obelisk make two mirrors that form an endless hall. Shatter the obelisk, and time will finally flow past its fateful edge.
    -> END 