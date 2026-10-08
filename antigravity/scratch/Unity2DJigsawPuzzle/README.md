# 2D Jigsaw Puzzle Game in Unity (C#)

A complete, feature-rich 2D Jigsaw Puzzle game created for Unity LTS (2D Built-in Render Pipeline), TextMeshPro, and Unity UI Canvas. Built with zero paid asset dependencies.

## Key Features

1. **Procedural Bezier Jigsaw Slicing**: Slices any Sprite into configurable rows and columns (e.g., 3x3, 4x4, 5x5). Uses cubic curves to generate interlocking tabs and blanks with 100% seam matching and padded transparent textures.
2. **True-Shape Polygon Colliders**: Each piece gets an automatically computed `PolygonCollider2D` that exactly matches its tabs and blanks for pixel-perfect drag-and-drop.
3. **Touch and Mouse Input**: Seamlessly supports desktop mouse drag & drop and Android / iOS touch input with cursor grab offset to prevent jumpiness.
4. **Light-Blue Slot Silhouettes**: Displays `#8FD3EC` silhouettes for empty slots on the board that vanish when the correct piece snaps into place.
5. **Magnetic Snap & Scale Pop**: Dropping a piece within the configurable snap tolerance snaps it into place, plays an audio click, and triggers a juicy scale pop tween.
6. **One-Click Automated Solver**: Green "SOLVE" button smoothly animates all remaining pieces into place with staggered cubic easing, then triggers victory.
7. **Complete Scene Progression**: Main Menu, Level Select (dynamic grid), and Game scenes with timer, pieces counter, restart, and victory popup.
8. **Built-in Procedural Audio**: Generates snappy click SFX and celebratory win jingles procedurally via C# if no audio files are assigned.
9. **One-Click Automated Editor Setup**: `Tools > Jigsaw > 1. Quick Setup Everything (One Click)` builds the entire project, sample farm art, levels, and scenes in seconds.

## Directory Structure

```text
Assets/
├── Audio/            (Custom audio files; optional procedural fallback included)
├── Levels/           (LevelData ScriptableObjects)
├── Prefabs/          (UI and puzzle prefabs)
├── Scenes/           (MainMenu, LevelSelect, Game)
├── Scripts/
│   ├── Core/        (GameManager, LevelManager, AudioManager)
│   ├── Data/        (LevelData ScriptableObject)
│   ├── Editor/      (PuzzleSetupTools automated builder)
│   ├── Puzzle/      (PuzzleBoard, PuzzlePiece, JigsawMaskGenerator, PieceScatter)
│   └── UI/          (HUDController, WinPanel, MainMenuUI, LevelSelectUI)
└── Sprites/          (Source puzzle images)
```

## Quick Start

1. Open this project folder in Unity (Unity 2022 LTS or newer).
2. When prompted, click **Import TMP Essentials** for TextMeshPro.
3. In the top Unity menu, click:  
   **Tools > Jigsaw > 1. Quick Setup Everything (One Click)**
4. Open `Assets/Scenes/Game.unity` and click **Play**!
