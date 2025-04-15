// Define death count as an external variable to be set by the code
VAR deathCount = 0

// Main dialogue based on death count
{
    - deathCount == 0:
        Tentatively you step into the dungeon. Your party members close behind you.
        
        Trecking through burning mountains and deserts of glass, you arrive here.
        
        Outside, the skies crumble and the gulls cry. The whole work tilts to the Obelisk's side. 
        
        The Obelisk lies in this basement. The heart of corruption. 
        
        Clense it and the world is saved, fail and the world is forfeit. 
        
        The fire crackles in the entrance room.
    - deathCount == 1:
        Is it over?
        
        You cannot move. Every inch of your body is hurting.
        
        Has all your preparations come to this?
        
        A warmth washes over you, with sound of crackling flames.
        
        You open them, and discover you have eyes.
        
        You are back. Were it just a dream? You hands are shaking.
        
        Though...you're still alive. That's gotta be a good thing.
    - deathCount == 2:
        The second time, but just as frightening.
        
        You open your eyes to see the flames.
        
        It it's not a fluke. You cannot die.
        
        It didn't matter you couldn't fall the obelisk, that you died horribly, that your friends died horribly. 
        
        It didn't matter you are underprepared, that you couldn't understand anything.
        
        You can try and try again, to fall the obelisk, to save the world.
        
        You smile so hard your face starts to hurt.
        
        It's magic.
    - deathCount == 3:
        You are getting used to it here. The dungeons walls are familiar and comforting. The monsters no longer scare you.
        
        Piece by piece, you are understanding this place.
    - deathCount == 4:
        Aaand back. Like nothing ever happened. Time to go again.
    - deathCount == 5:
        Victory takes practice. Practice and improvement and understanding.
        
        It's just a matter of time.
    - deathCount == 6:
        You're fine. Safely back here staring at the fire. You can go again.
    - deathCount == 7:
        You know the dungeon like the back of your hand. The dim-lit walls, the shuffling enemies, and stagnent air.
        
        and you, existing harmoniously within it.
        
        You take a deep breath, and continue.
    - else:
        What did you die from? It's all blending together. A sequence of images.
        
        No matter. All that matters is this attempt. The world depends on it.
} 