VAR character_name = "Mysterious One"

/* TestPortraits.ink - Portrait dialogue test */

-> main

=== main ===
portrait: magician_neutral_1, Hello there! I am the magician with a portrait on the left side.

Normal dialogue without any portrait. The portrait should now be hidden.

portrait: hood_neutral_1, Now I'm speaking with a hooded character portrait.

You can also use variables in portrait dialogue. For example, my name is {character_name}.

portrait: magician_neutral_1, Back to the magician portrait again! This shows how to switch between different portraits.

-> END