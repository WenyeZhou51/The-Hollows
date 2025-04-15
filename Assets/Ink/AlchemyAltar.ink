=== altar_book ===
A book lies on the strange altar.

You flip through the book. It seems to be about alchemy.

"Human souls hold immense magical capacity."

"The most powerful magic comes from the alchemy of souls...yielding powerful artifacts with great healing, offensive, or utility potential."

"A human soul consists of three parts: The Aperture, forming one's ability to interact with their environment, The Spindle, forming one's ability to talk and connect with others, and The Locus, forming one's ability to think and create an internal model of the world within their mind."

"Below lists a transmutation ritual for each part. You can perform them on the altar."

~ temp part = ""
+ [Transmute the Aperture] 
    ~ part = "Aperture"
    -> choose_transmutation(part)
+ [Transmute the Spindle] 
    ~ part = "Spindle"
    -> choose_transmutation(part)
+ [Transmute the Locus] 
    ~ part = "Locus"
    -> choose_transmutation(part)
+ [Close the book] -> end

=== choose_transmutation(part) ===
What to transmute the {part} into?
~ temp result = ""
+ [Panacea] 
    ~ result = "Panacea"
    -> transmutation_complete(part, result)
+ [Combustive catalyst] 
    ~ result = "Combustive catalyst"
    -> transmutation_complete(part, result)
+ [Void medallion] 
    ~ result = "Void medallion"
    -> transmutation_complete(part, result)
+ [Cancel] -> altar_book

=== transmutation_complete(part, result) ===
The altar begins to pulse with an eerie light as you prepare to transmute the {part}.

{part == "Aperture" && result == "Panacea":
    The {part} transforms into a {result}, a powerful healing artifact that can restore life force.
}
{part == "Aperture" && result == "Combustive catalyst":
    The {part} transforms into a {result}, an explosive component that amplifies destructive magic.
}
{part == "Aperture" && result == "Void medallion":
    The {part} transforms into a {result}, a strange artifact that seems to absorb energy around it.
}

{part == "Spindle" && result == "Panacea":
    The {part} transforms into a {result}, a restorative elixir that mends both body and spirit.
}
{part == "Spindle" && result == "Combustive catalyst":
    The {part} transforms into a {result}, a fiery component that enhances offensive spells.
}
{part == "Spindle" && result == "Void medallion":
    The {part} transforms into a {result}, a dark artifact that creates a shield against magical forces.
}

{part == "Locus" && result == "Panacea":
    The {part} transforms into a {result}, a miraculous cure that can heal even the most grievous wounds.
}
{part == "Locus" && result == "Combustive catalyst":
    The {part} transforms into a {result}, a powerful reagent that can break through magical barriers.
}
{part == "Locus" && result == "Void medallion":
    The {part} transforms into a {result}, a mysterious object that can temporarily suspend the laws of reality.
}

The transmutation is complete. You feel a strange sensation, as if something fundamental has been altered.

+ [Return to the book] -> altar_book
+ [Leave the altar] -> end

=== end ===
You step away from the altar, the knowledge of soul alchemy lingering in your mind.
-> END 