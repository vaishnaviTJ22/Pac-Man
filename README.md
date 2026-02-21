# Pac-Man 3D Project

A modern 3D take on the classic Pac-Man gameplay implemented in Unity.

## 🛠 Project Information
- **Unity Version**: 6000.0.64f1
- **Platform**: PC / WebGL

## 🎮 Controls
| Key | Action |
| --- | --- |
| **W / Up Arrow** | Move Forward / Up |
| **S / Down Arrow** | Move Backward / Down |
| **A / Left Arrow** | Move Left |
| **D / Right Arrow** | Move Right |

## 🕹 Gameplay Mechanics
- **Objective**: Navigate the maze, eat all the dots, and avoid the ghosts to progress to the next level.
- **Ghosts**: Four unique ghosts (Blinky, Pinky, Inky, Clyde) with different AI targeting behaviors.
- **Energizers**: Collecting an energizer turns ghosts blue (Frightened Mode), allowing you to eat them for bonus points.
- **Level Scaling**: The game difficulty increases with each level, scaling player/enemy speeds and adjusting power-up intervals.

### Special Power-Ups
- **🔴 Booster Bottle (Red)**: Grants a temporary speed boost and allows Pac-Man to pass through walls (Ghost Mode).
- **🟢 Health Orb (Green)**: Grants temporary invincibility, preventing death when caught by ghosts.

## 📊 Features
- **Leaderboard**: Local high-score tracking with player names.
- **Dynamic HUD**: Real-time display of Player Name, Score, and Lives.
- **Maze Generation**: Runtime maze generation based on ScriptableObject data.

## ⚠️ Known Limitations
- **Ghost AI**: Movement is predominantly target-based; ghosts do not currently implement complex pathfinding (like A*) beyond their simple grid decisions.
- **Grid Alignment**: Movement requires strict alignment with the maze grid; stopping midway or clipping depends on the `MazeGenerator` setup.
- **Level Persistence**: While names persist via `PlayerPrefs`, the current game state (active level/score) is lost upon closing the application unless reaching the leaderboard.
