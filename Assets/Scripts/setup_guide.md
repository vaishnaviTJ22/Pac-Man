# Ghost Setup Guide

This guide explains how to set up markers for the Ghost House and Scatter Corners to ensure the AI behaves correctly.

## 1. Ghost House Markers

### Ghost House Center
- **What it is**: The exact middle of the ghost house area.
- **Use**: This is where ghosts return to after being eaten and where they "bob" up and down while waiting to be released.
- **Setup**: Place an empty GameObject in the center of the ghost house floor. Assign it to the `Ghost House Center` field in the `GhostManager`.

### House Exit
- **What it is**: A position just outside the ghost house entrance (usually one tile forward from the door).
- **Use**: When a ghost is released, it first moves to this point to safely enter the maze. This prevents ghosts from getting stuck in the walls of the house during exit.
- **Setup**: Place an empty GameObject about 1-2 units in front of the ghost house door. Assign it to the `Ghost House Exit` field in the `GhostManager`.

---

## 2. Scatter Corners

### What are they?
In classic Pac-Man, ghosts occasionally stop chasing you and retreat to their respective corners. This is called **Scatter Mode**.

- **Blinky Corner**: Top-Right
- **Pinky Corner**: Top-Left
- **Inky Corner**: Bottom-Right
- **Clyde Corner**: Bottom-Left

### Setup:
1. Create 4 empty GameObjects in the scene.
2. Place each one at the very edge of the maze in its designated corner.
3. Assign these to the `Blinky Corner`, `Pinky Corner`, etc., fields in the `GhostManager`.
4. **Tip**: Make sure these points are reachable (not inside a wall) so the ghost can get as close as possible.

---

## 3. Troubleshooting: Floor Passthrough
If your ghosts are falling through the floor:
- **Rigidbody Settings**: Ensure the ghost's Rigidbody has **Use Gravity** unchecked.
- **Constraints**: Set **Freeze Position Y** to checked in the Rigidbody constraints.
- **Height**: The `GhostController` script now enforces a fixed Y height to prevent clipping.
