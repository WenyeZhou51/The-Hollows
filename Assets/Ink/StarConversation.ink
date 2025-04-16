// Star Conversation - Dialogue about the stars on Magician's clothes
// This dialogue branches based on death count from PersistentGameManager

VAR deathCount = 0

-> main

=== main ===
{
    - deathCount < 2:
        -> stars_death_0_1
    - deathCount < 4:
        -> stars_death_2_3
    - deathCount == 4:
        -> stars_death_4
    - deathCount == 5:
        -> stars_death_5
    - deathCount == 6:
        -> stars_death_6
    - deathCount == 7:
        -> stars_death_7
    - else:
        -> stars_death_8_plus
}

// Death count 0-1 branch
=== stars_death_0_1 ===
portrait: Fighter neutral, A starry sky, this deep underground?
portrait: Bard confused, Talking of stars, why do you wear them on your clothes?
portrait: Magician neutral, Cuz they sparkle. I'm like a bloody magpie.
-> END

// Death count 2-3 branch
=== stars_death_2_3 ===
portrait: Fighter neutral, A starry sky, this deep underground?
portrait: Bard confused, Talking of stars, why do you wear them on your clothes?
portrait: Magician neutral, Cuz they're beautiful.
portrait: Magician neutral, Worlds, millions of miles away, shining their mysterious light on us.
portrait: Magician neutral, Sometimes I get disoriented in the dungeon walls, and I look to the stars. No matter how distant the world, how dark the night, their light still reaches us.
-> END

// Death count 4 branch
=== stars_death_4 ===
portrait: Fighter neutral, A starry sky, this deep underground?
portrait: Bard confused, Talking of stars, why do you wear them on your clothes?
portrait: Magician neutral, Cuz they're beautiful.
portrait: Magician neutral, Worlds, millions of miles away, shining their mysterious light on us.
portrait: Magician neutral, It's the closest thing to magic
-> END

// Death count 5 branch
=== stars_death_5 ===
portrait: Fighter neutral, A starry sky, this deep underground?
portrait: Bard confused, Talking of stars, why do you wear them on your clothes?
portrait: Magician confused, Have you heard of "Wish upon a star"?
portrait: Magician neutral, A star for every wish, hanging in the night sky.
portrait: Magician neutral, We can almost touch it.
-> END

// Death count 6 branch
=== stars_death_6 ===
portrait: Fighter neutral, A starry sky, this deep underground?
portrait: Bard confused, Talking of stars, why do you wear them on your clothes?
portrait: Magician confused, Have you heard of "Wish upon a star"?
portrait: Magician neutral, A star for every wish, hanging in the night sky.
portrait: Magician neutral, And only the universe stands between
-> END

// Death count 7 branch 
=== stars_death_7 ===
portrait: Fighter neutral, A starry sky, this deep underground?
portrait: Bard confused, Talking of stars, why do you wear them on your clothes?
portrait: Magician confused, Like afterimages.
portrait: Bard confused, Like what?
portrait: Magician confused, Light from the stars takes years to reach us. A star might have gone out, and we wouldn't even know it.
portrait: Magician neutral, These lights from the past are certainly entrancing to watch
-> END

// Death count 8+ branch
=== stars_death_8_plus ===
portrait: Fighter neutral, A starry sky, this deep underground?
portrait: Bard confused, Talking of stars, why do you wear them on your clothes?
portrait: Magician confused, Like afterimages.
portrait: Bard confused, Like what?
portrait: Magician confused, Light from the stars takes years to reach us. The stars made their move, and we watched the replay under the black sky.
portrait: Magician neutral, We watch it every night
-> END 