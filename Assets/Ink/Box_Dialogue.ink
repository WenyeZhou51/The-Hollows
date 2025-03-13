// Box Dialogue Sample
// This is a sample Ink script for interacting with a box

-> main

=== main ===
You examine the mysterious box...
* [Open it carefully]
    You slowly lift the lid, peering inside with caution.
    -> found_item
* [Shake it first]
    You shake the box. Something liquid sloshes around inside.
    -> found_item
* [Kick it open]
    You kick the box open! The lid flies off.
    Fortunately, nothing breaks inside.
    -> found_item

=== found_item ===
Inside, you find a bottle of <b>Fruit Juice</b>! # GIVE_ITEM:FruitJuice
* [Take it]
    You put the Fruit Juice in your inventory.
    This will restore some health when consumed.
* [Leave it]
    You decide to leave the Fruit Juice for now.
    You can always come back for it later.

-> END 