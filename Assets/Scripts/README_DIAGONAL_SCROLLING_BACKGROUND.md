# Diagonal Scrolling Background Implementation

## Overview
This implementation provides a smooth, infinite diagonal scrolling background for the Battle Weaver scene using the "Battle bg.png" sprite. The system creates a seamless tiling effect that scrolls diagonally across the screen.

## Components

### 1. InfiniteScrollBackground.cs
**Main script that handles the scrolling logic**

**Features:**
- Diagonal scrolling with configurable direction and speed
- Infinite tiling with seamless wrapping
- Performance optimized with configurable grid size
- Runtime controls for direction, speed, and enable/disable

**Key Settings:**
- `scrollDirection`: Vector2(1, 0.5) - Diagonal direction (right and up)
- `scrollSpeed`: 2.0 - Speed of scrolling
- `normalizeDirection`: true - Maintains consistent speed regardless of direction
- `gridWidth/Height`: 3x3 - Number of background tiles
- `bufferMultiplier`: 1.5 - Extra buffer to prevent gaps

### 2. BackgroundScrollTester.cs
**Testing and debugging script**

**Features:**
- Runtime controls for testing
- Debug information display
- Keyboard shortcuts for testing

**Controls:**
- `Space` - Toggle scrolling on/off
- `+/-` - Increase/decrease speed
- `D` - Change direction

### 3. BackgroundScrollOptimizer.cs
**Performance monitoring and optimization**

**Features:**
- FPS monitoring
- Automatic optimization based on performance
- Performance statistics display

## Setup

### Scene Configuration
The Battle_Weaver scene already contains:
1. **Background GameObject** - Main container with InfiniteScrollBackground script
2. **BackgroundElement1 Prefab** - Individual background tile using "Battle bg.png"
3. **Proper positioning** - Background is positioned behind all other elements

### Prefab Configuration
- **Background.prefab**: Contains the InfiniteScrollBackground script with proper settings
- **BackgroundElement1.prefab**: Contains the Battle bg sprite with proper scaling

## Usage

### Basic Usage
The scrolling background starts automatically when the scene loads. No additional setup required.

### Runtime Controls
```csharp
// Get reference to the scrolling background
InfiniteScrollBackground scrollBG = FindObjectOfType<InfiniteScrollBackground>();

// Control scrolling
scrollBG.SetScrollingEnabled(false); // Pause scrolling
scrollBG.SetScrollSpeed(3.0f); // Change speed
scrollBG.SetScrollDirection(new Vector2(-1, 0.5f)); // Change direction
```

### Testing
Add the BackgroundScrollTester script to any GameObject in the scene for runtime testing:
- Press Space to toggle scrolling
- Use +/- to adjust speed
- Press D to change direction

## Performance Considerations

### Optimization Features
1. **Efficient Grid System**: Only renders necessary tiles
2. **Smart Repositioning**: Tiles are repositioned only when needed
3. **Configurable Grid Size**: Adjust grid size based on performance needs
4. **Buffer Management**: Configurable buffer to prevent gaps

### Performance Monitoring
The BackgroundScrollOptimizer script provides:
- Real-time FPS monitoring
- Automatic optimization when performance drops
- Performance statistics display

### Recommended Settings
- **High Performance**: gridWidth=3, gridHeight=3, scrollSpeed=1-2
- **Medium Performance**: gridWidth=4, gridHeight=4, scrollSpeed=2-3
- **Low Performance**: gridWidth=2, gridHeight=2, scrollSpeed=0.5-1

## Troubleshooting

### Common Issues

1. **Background not scrolling**
   - Check if InfiniteScrollBackground script is attached
   - Verify backgroundPrefab is assigned
   - Ensure enableScrolling is true

2. **Gaps in background**
   - Increase bufferMultiplier
   - Check element dimensions
   - Verify grid size is appropriate

3. **Performance issues**
   - Reduce grid size
   - Lower scroll speed
   - Use BackgroundScrollOptimizer for monitoring

### Debug Information
Enable debug logging in InfiniteScrollBackground to see:
- Element dimensions
- Grid creation
- Initialization status

## Customization

### Changing Scroll Direction
```csharp
// Horizontal scrolling
scrollBG.SetScrollDirection(new Vector2(1, 0));

// Vertical scrolling  
scrollBG.SetScrollDirection(new Vector2(0, 1));

// Diagonal scrolling (current)
scrollBG.SetScrollDirection(new Vector2(1, 0.5));
```

### Changing Background Sprite
1. Replace the sprite in BackgroundElement1.prefab
2. Ensure the new sprite has proper dimensions
3. Adjust scaling if needed

### Adjusting Grid Size
- Increase grid size for larger backgrounds
- Decrease grid size for better performance
- Ensure grid covers the entire visible area with buffer

## Technical Details

### How It Works
1. **Grid Creation**: Creates a 3x3 grid of background tiles
2. **Scrolling**: Moves all tiles in the specified direction
3. **Wrapping**: Repositions tiles that move off-screen to the opposite side
4. **Seamless Loop**: Creates infinite scrolling effect

### Coordinate System
- Uses world coordinates for positioning
- Camera-relative bounds calculation
- Automatic tile repositioning based on camera position

### Memory Management
- Tiles are reused rather than destroyed/created
- Efficient repositioning algorithm
- Minimal memory allocation during runtime

## Future Enhancements

### Possible Improvements
1. **Parallax Layers**: Multiple scrolling layers at different speeds
2. **Dynamic Direction**: Direction changes based on game events
3. **Speed Variations**: Speed changes for dramatic effect
4. **Color Tinting**: Runtime color changes for atmosphere
5. **Particle Integration**: Add particle effects to scrolling background

### Integration Ideas
- Combat phase changes affecting scroll direction
- Player input affecting scroll speed
- Environmental effects synchronized with scrolling
- Audio synchronization with scroll rhythm

## Conclusion

The diagonal scrolling background system provides a smooth, performant solution for the Battle Weaver scene. The implementation is flexible, well-optimized, and includes comprehensive testing and monitoring tools. The system is ready for production use and can be easily customized for different needs.
