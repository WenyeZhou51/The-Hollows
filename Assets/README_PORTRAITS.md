# Portrait System For Dialogue

## Important: Making Portraits Work in Builds

The dialogue portrait system uses Unity's `Resources.Load()` to load portrait images in the built game. For this to work correctly, **all portrait images must be in the Resources folder**.

### Current Issue

Your portraits are currently located in:
- `Assets/Sprites/Portraits/` - Used in the editor
- `Assets/Resources/Portraits/` - Used in builds

The problem is that not all portraits from `Sprites/Portraits` are duplicated in `Resources/Portraits`. In the built game, Unity can ONLY load images from the Resources folder using `Resources.Load()`.

### Solution Options

#### Option 1: Move ALL portrait images to Resources folder (Recommended)

1. Make sure all your portrait images exist in `Assets/Resources/Portraits/`
2. Copy any missing portraits from `Assets/Sprites/Portraits/` to `Assets/Resources/Portraits/`
3. Keep the naming convention consistent (e.g., `magician_neutral_1.png` uses underscores)

#### Option 2: Rename your portrait references in ink files

For each portrait used in your ink dialogue files (like `test portrait.ink`), make sure the name matches exactly what's in the Resources folder. For example:

```
// Instead of this:
portrait: Magician confused, ...

// Use this:
portrait: magician_confused, ...
```

Make sure your portrait filenames in Resources follow a consistent naming pattern.

### Filename Conventions

The updated portrait loading system tries several formats when looking for portraits:

1. Exact match: `Portraits/Magician confused`
2. Lowercase: `Portraits/magician confused`
3. Underscores instead of spaces: `Portraits/magician_confused`
4. Direct ID (without Portraits/ prefix): `Magician confused`

For best results, use a consistent naming pattern like lowercase with underscores (e.g., `magician_neutral_1`, `hood_neutral_1`).

### Testing Portraits in Builds

After implementing one of the solutions above, create a test build and verify all portraits load correctly in the game.

If you continue having issues, check the console logs for specific error messages about which portraits can't be found. 