# Blutspendespiel – Blood Donation Game

A Pac-Man-inspired 2D arcade game built with Unity, themed around blood donation.
The player navigates a maze, collecting blood tiles while avoiding ghost enemies.
Collecting a golden blood tile grants temporary invincibility ("golden mode") in
which ghosts can be defeated for a bonus life.

---

## Features

- Pac-Man-style tile-based movement with keyboard and touch/swipe input
- Four ghost AI personalities (Chaser, Ambusher, Random, Patroller) with scatter phases
- Difficulty scaling: ghost speed, aggression, and lookahead increase per level
- Golden-blood power-up with timed invincibility and visual flash warning
- Persistent scoreboard via a Node.js REST backend (WebGL-compatible)
- Camera shake and red screen vignette on player hit
- Pause / resume, global mute, and letterbox camera support
- Multi-level support with additive scene loading

---

## Controls

| Input | Action |
|-------|--------|
| W or Arrow Up | Move up |
| S or Arrow Down | Move down |
| A or Arrow Left | Move left |
| D or Arrow Right | Move right |
| Swipe (touch) | Move in swipe direction |

---

## Project Structure

```
Assets/
  general scripts/
    GameManager.cs          Entry point; exposes scoreboard helper
    LevelManager.cs         Singleton: levels, lives, game flow, score
    ScoreboardClient.cs     HTTP client for the score server
  Sprites/Scripts/
    Movement.cs             Player movement (PacManMovement2D)
    GhostMovement.cs        Ghost AI with 4 personalities
    BloodCollector.cs       Collects blood tiles, updates score bar
    golden_blood_collector.cs  Golden blood / invincibility logic
    pausemanager.cs         Pause / resume (PauseManager)
    ToggleText.cs           Info panel toggle
    scoreboard_button.cs    Scoreboard UI button
    togglemute.cs           Global audio mute toggle (MuteButton)
    LetterboxCamera.cs      Orthographic camera letterboxing
  PlayerInputs.cs           Auto-generated Input System bindings
  Scenes/
    SampleScene.unity       Main scene
```

---

## Setup

1. Open the project in **Unity 2022.3** (LTS) or later.
2. Open **Scenes/SampleScene.unity**.
3. *(Optional)* Start the Node.js score server on port 3000.
   Without the server the game runs normally; scores simply are not persisted.
4. Press **Play**.

---

## Score Server

The game communicates with a REST endpoint on port **3000**:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/score` | Submit a score entry `{ id, name, score }` |
| GET | `/scoreboard` | Retrieve all score entries as a JSON array |

In a **WebGL build** the server URL is derived from `Application.absoluteURL`
(same host, port 3000). In the **Editor** it defaults to `http://localhost:3000`.

---

## Adding Levels

1. **Duplicate an existing level scene** in the Project window (right-click → Duplicate).
   This keeps all the correct tilemaps, tags, and structure in place.
2. **Paint the new layout** — open the duplicated scene, select the Grid in the Hierarchy,
   then use the **Tile Palette** (Window → 2D → Tile Palette) to paint tiles on the tilemaps.
   - The wall tilemap name must contain `"wall"`.
   - The blood tilemap name must contain `"blood"` (but not `"golden"`).
   - The golden blood tilemap name must contain `"golden"`.
3. **Adjust spawn points** — move the `PlayerSpawn` object and any `EnemySpawn` objects to
   the desired positions for the new layout.
4. **Add to Build Settings** — open File → Build Settings and drag the new scene into the list.
5. **Register the level** — add the scene name to `LevelManager.levelScenes[]` in the Inspector.

---

## Building for Mobile & Desktop

When creating a WebGL build, make sure to select the **custom project build template**
in the Player Settings — this template includes the correct headers and configuration
for the game to run on both mobile browsers and desktop browsers.

Go to **Edit → Project Settings → Player → Resolution and Presentation** and confirm
the custom template is selected before building.
