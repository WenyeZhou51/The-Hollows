// Battle Tutorial Introduction Dialogue
// This teaches the player the basics of combat

-> main

=== main ===
portrait: ranger_neutral, Calm down everyone # speaker: Ranger

portrait: ranger_neutral, You still remember the basics of combat, right? # speaker: Ranger

portrait: fighter_neutral, This is our HP bar # speaker: Fighter # HIGHLIGHT:player_health

portrait: fighter_neutral, We collapse if we run out of HP! # speaker: Fighter

portrait: fighter_neutral, This is our Mind bar. Mind is spent to use skills. # speaker: Fighter # UNHIGHLIGHT:player_health # HIGHLIGHT:player_mind

portrait: fighter_neutral, This is our action bar. Once a member's action bar fills up, they are ready to take an action! # speaker: Fighter # UNHIGHLIGHT:player_mind # HIGHLIGHT:player_action

portrait: ranger_neutral, The enemy also has HP bars... # speaker: Ranger # UNHIGHLIGHT:player_action # FLASH:enemy_health

portrait: ranger_neutral, ...and Action bars. # speaker: Ranger # STOPFLASH:enemy_health # FLASH:enemy_action

portrait: bard_happy1, You can do basic attacks or Guard, these don't consume any resources. # speaker: Bard # STOPFLASH:enemy_action # HIGHLIGHT:attack_button

portrait: bard_happy1, You can also Guard to reduce incoming damage. # speaker: Bard # UNHIGHLIGHT:attack_button # HIGHLIGHT:guard_button

portrait: bard_happy1, You can use skills, which sometimes consume mind. # speaker: Bard # UNHIGHLIGHT:guard_button # HIGHLIGHT:skill_button

portrait: bard_happy1, You can also use items you've collected along the way. # speaker: Bard # UNHIGHLIGHT:skill_button # HIGHLIGHT:item_button

portrait: magician_neutral_1, Got it, let's get them! # speaker: Magician # CLEAR_HIGHLIGHTS

-> END

