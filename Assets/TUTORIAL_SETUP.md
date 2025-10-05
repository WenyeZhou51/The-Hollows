# Battle Tutorial Setup - Quick Guide

## ✅ Files Created

**Scripts** (Assets/Scripts/):
- `TutorialHighlighter.cs` - Manages UI highlighting
- `TutorialDialogueHandler.cs` - Processes Ink highlight tags
- `TutorialBattleDialogueTrigger.cs` - Triggers tutorial dialogue in battle

**Ink Dialogue** (Assets/Ink/):
- `battle_tutorial_intro.ink` - Tutorial dialogue script

**Editor Tool** (Assets/Editor/):
- `TutorialHighlightTester.cs` - Test highlights in Play Mode

---

## 🚀 Setup Steps

### 1. Compile Ink File
1. In Unity, find `Assets/Ink/battle_tutorial_intro.ink`
2. It should auto-compile to `battle_tutorial_intro.json`
3. If not: Right-click → "Recompile Ink"

### 2. Open Battle_Tutorial Scene
Open your Battle_Tutorial scene in Unity

### 3. Create TutorialManager GameObject
In Hierarchy:
- Right-click → Create Empty
- Name it: **TutorialManager**
- Place at root level (same as Canvas, Camera, etc.)

### 4. Add Components
Select TutorialManager, then in Inspector:

**Component 1:**
- Add Component → `TutorialBattleDialogueTrigger`
- **Intro Dialogue JSON**: Drag `battle_tutorial_intro.json` from Assets/Ink/
- ✅ Check "Play Intro Dialogue"

**Component 2:**
- Add Component → `TutorialHighlighter`
- Leave settings at default

### 5. Save & Test
- Save scene (Ctrl+S)
- Enter Play Mode
- Wait 1 second → Tutorial starts
- Press Z to advance dialogue
- Watch UI elements highlight!

---

## 📍 Where to Place Components

```
Battle_Tutorial Scene Hierarchy:
├── Canvas
├── EventSystem
├── Main Camera
└── TutorialManager  ← CREATE THIS
    ├── TutorialBattleDialogueTrigger (Component)
    └── TutorialHighlighter (Component)
```

---

## 🎯 What Happens

1. **After 1 second**: Tutorial dialogue starts, combat pauses
2. **Ranger speaks**: "You still remember the basics..."
3. **Fighter explains bars**: HP, Mind, Action bars each highlight
4. **Ranger shows enemy**: Enemy HP and Action bars highlight
5. **Bard shows actions**: Attack, Guard, Skills, Items buttons highlight
6. **Magician confirms**: "Got it, let's get them!" - combat resumes

---

## 🎨 Customization (Optional)

In TutorialHighlighter component:
- **Highlight Color**: Change from yellow to your preference
- **Highlight Scale**: 1.2 = 20% larger (adjust as needed)
- **Pulse Speed**: 2.0 = normal speed
- **Pulse Intensity**: 0.1 = subtle pulse

---

## 🐛 Troubleshooting

| Problem | Solution |
|---------|----------|
| No dialogue appears | Check JSON is assigned in TutorialBattleDialogueTrigger |
| No highlights | Verify TutorialHighlighter component exists |
| Ink won't compile | Right-click .ink file → "Recompile Ink" |
| Wrong elements highlighted | Check console for "Could not find UI element" warnings |

---

## 🧪 Testing Highlights

In Play Mode:
1. Select TutorialManager GameObject
2. TutorialHighlighter shows test buttons in Inspector
3. Click buttons to test individual highlights
4. Verify elements are found correctly

---

## 📝 Ink Dialogue Format

The tutorial uses proper Ink syntax:

```ink
-> main

=== main ===
portrait: character_name, Dialogue text # speaker: Name # HIGHLIGHT:element_name

<> # UNHIGHLIGHT:element_name # HIGHLIGHT:another_element

-> END
```

**Available highlight tags:**
- `# HIGHLIGHT:element_name` - Show highlight
- `# UNHIGHLIGHT:element_name` - Remove highlight
- `# CLEAR_HIGHLIGHTS` - Remove all highlights

**Recognized element names:**
- `player_health`, `player_mind`, `player_action`
- `enemy_health`, `enemy_action`
- `attack_button`, `guard_button`, `skill_button`, `item_button`

---

## ✨ That's It!

The system integrates seamlessly with your existing DialogueManager and combat system. No conflicts, no modifications to existing code - just add components and go!

**Questions?** Check `Assets/Resources/TutorialSetupGuide.md` for detailed documentation.

