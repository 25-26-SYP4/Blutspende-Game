# Blutspendespiel – Technical Documentation

---

## Architecture Overview

The game uses a **singleton LevelManager** as the central hub.
All gameplay events (player hit, level complete, game over) flow through it.
Scoreboard communication is handled by a separate **ScoreboardClient** component
so network logic stays decoupled from game logic.

```
┌──────────────────────────────────────────────────────┐
│                     LevelManager                     │
│  • level loading/unloading (additive scenes)         │
│  • lives, game-over / level-complete sequences       │
│  • ghost spawning at EnemySpawn markers              │
│  • score persistence (PlayerPrefs + REST server)     │
└────────┬─────────────┬─────────────┬────────────────-┘
         │             │             │
   PacManMovement2D  GhostMovement  BloodCollector
   (player input)   (ghost AI)     (tile collection)
         │
   golden_blood_collector
   (invincibility power-up)
```

Player data (ID, name, score, level progress) is saved to **PlayerPrefs** after
every significant event so progress survives session restarts.

---

## Script Reference

### general scripts/GameManager.cs — `GameManager`

| | |
|---|---|
| Base class | `MonoBehaviour` |
| Purpose | Thin coordinator; holds a `ScoreboardClient` reference and exposes a debug helper. |

**Fields**

| Field | Type | Description |
|-------|------|-------------|
| `scoreboardClient` | `ScoreboardClient` | Resolved via `GetComponent` on Start |

**Methods**

| Method | Access | Description |
|--------|--------|-------------|
| `ShowScoreboard()` | private | Prints all scoreboard entries to the Unity console |

> **Note:** Score updates are sent by `LevelManager.SendScoreUpdate()`, not by this class.

---

### general scripts/ScoreboardClient.cs — `ScoreboardClient`

| | |
|---|---|
| Base class | `MonoBehaviour` |
| Purpose | HTTP client — posts scores to and fetches them from the Node.js server. |

**Data classes**

| Class | Fields | Description |
|-------|--------|-------------|
| `ScoreEntry` | `id`, `name`, `score` | Single scoreboard row |
| `ScoreResponse` | `success`, `message`, `data` | Server response envelope |

**Methods**

| Method | Signature | Description |
|--------|-----------|-------------|
| `AddScore` | `(string id, string name, int score, Action<bool,string> callback)` | POST a score entry; callback receives success flag and message |
| `GetScoreboard` | `(Action<List<ScoreEntry>> callback)` | GET all entries; callback receives the parsed list |
| `GetServerUrl` | private | Lazy-initialises the server URL; uses `Application.absoluteURL` in WebGL builds |

**URL resolution**

```
WebGL build  →  <scheme>://<host>:3000   (derived from Application.absoluteURL)
Editor / PC  →  http://localhost:3000
```

---

### general scripts/LevelManager.cs — `LevelManager`

| | |
|---|---|
| Base class | `MonoBehaviour` |
| Pattern | Singleton (`LevelManager.Instance`) |
| Purpose | Central game manager: levels, lives, UI states, score, camera effects. |

**Inspector fields (Headers)**

*Level Configuration*

| Field | Type | Description |
|-------|------|-------------|
| `levelScenes` | `string[]` | Scene names loaded additively in order |
| `currentLevelIndex` | `int` | Index into `levelScenes`; persisted in PlayerPrefs |

*UI References*

| Field | Type | Description |
|-------|------|-------------|
| `mainMenuCanvas` | `GameObject` | Main-menu root object |
| `mainMenuSceneName` | `string` | Scene name for the menu (informational) |
| `maxScoreText` | `TextMeshProUGUI` | Displays the accumulated global score in the menu |
| `worldGameUI` | `GameObject` | HUD shown during gameplay |
| `nextLevelText` | `TMP_Text` | "Level: N" label |
| `startGameButton` | `Button` | Enabled only when a player name is entered |
| `nameInput` | `TMP_InputField` | Player name input |
| `NextLevelScene` | `GameObject` | "Next Level" overlay |
| `GameCompleteScene` | `GameObject` | "Game Complete" overlay |
| `GameOverScene` | `GameObject` | "Game Over" overlay |

*Life Settings*

| Field | Type | Description |
|-------|------|-------------|
| `lives` | `int` | Current life count (starts at `maxLives = 3`) |
| `heartIcons` | `GameObject[]` | Heart UI icons; shown/hidden to reflect `lives` |

*Game References*

| Field | Type | Description |
|-------|------|-------------|
| `ghostPrefab` | `GameObject` | Instantiated at each `EnemySpawn` tag |
| `player` | `PacManMovement2D` | Player movement component |
| `bloodCollector` | `BloodCollector` | Receives `InitializeLevel()` on each load |
| `goldenBloodCollector` | `golden_blood_collector` | Deactivated on level/game reset |

*Audio*

| Field | Type | Description |
|-------|------|-------------|
| `levelCompleteSound` | `AudioClip` | Played on level complete |
| `gameOverSound` | `AudioClip` | Played on game over |
| `hitSound` | `AudioClip` | Played when the player is hit (normal) |
| `goldenHitSound` | `AudioClip` | Played when the player defeats a ghost in golden mode |
| `*Volume` | `float [0-1]` | Volume for each clip |

*Hit Feedback*

| Field | Type | Description |
|-------|------|-------------|
| `redVignetteImage` | `Image` | Full-screen red overlay; fades in/out on hit |
| `gameCamera` | `Camera` | Camera that shakes on hit |
| `shakeStrength` | `float` | Peak shake offset (default 0.15) |
| `shakeDuration` | `float` | Duration in seconds (default 0.4) |
| `vignetteDuration` | `float` | Duration of the red flash (default 0.6) |

**Public methods**

| Method | Description |
|--------|-------------|
| `StartGame()` | Validates name, hides menu, loads the current level |
| `LoadLevel(int)` | Starts additive scene load coroutine |
| `OnLevelComplete()` | Increments level, sends score, shows next-level or game-complete screen |
| `OnPlayerHit(GameObject)` | Deducts life (or defeats ghost in golden mode), triggers feedback effects |
| `UpdateLifeUI()` | Syncs heart icons to current `lives` value |
| `ShowMainMenu()` | Resets UI to the main-menu state |
| `NewPlayer()` | Generates a new GUID player ID; resets score and level; clears name |
| `SavePlayerName(string)` | Persists the name to PlayerPrefs |
| `OnNameInputChanged(string)` | Validates the start button |

**Key private coroutines**

| Coroutine | Description |
|-----------|-------------|
| `LoadLevelCoroutine` | Unloads previous scene, loads new scene additively, spawns ghosts, assigns tilemap refs |
| `GameOverSequence` | Plays sound, shows overlay, resets state, returns to menu |
| `GameCompleteSequence` | Shows complete overlay, resets everything, returns to menu |
| `LevelCompleteRoutine` | Short pause then returns to menu for the next level |
| `ReturnToMenuRoutine` | Unloads level scene and restores menu state |
| `RedVignetteEffect` | Fades vignette in then out over `vignetteDuration` |
| `CameraShake` | Random offset diminishing over `shakeDuration` |

---

### Sprites/Scripts/Movement.cs — `PacManMovement2D`

| | |
|---|---|
| Base class | `MonoBehaviour` |
| Requires | `BoxCollider2D` |
| Purpose | Tile-snapping player movement supporting keyboard and touch swipe. |

**Inspector fields**

| Field | Type | Description |
|-------|------|-------------|
| `tilemap` | `Tilemap` | Wall tilemap used for collision checks (set by LevelManager) |
| `speed` | `float` | Movement speed; above ~3 corner navigation becomes unreliable |
| `centerTolerance` | `float` | Distance threshold for "at cell centre" (default 0.1) |
| `wallMask` | `LayerMask` | Physics layers considered walls |

**Movement model**

1. `HandleInput` stores the desired next direction (`nextDir`).
2. In `Update`, if `nextDir` is the reverse of `currentDir`, the U-turn is immediate.
3. At a cell centre, if `nextDir` is free, the player snaps to the centre and turns.
4. If the forward path is blocked at a cell centre, movement stops.
5. `FixedUpdate` advances `transform.position` by `currentDir * speed * fixedDeltaTime`.

Enemy contact (collision or trigger) delegates to `LevelManager.OnPlayerHit`.

---

### Sprites/Scripts/GhostMovement.cs — `GhostMovement`

| | |
|---|---|
| Base class | `MonoBehaviour` |
| Purpose | Tile-based ghost AI; chooses direction at intersections and cell centres. |

**Personality enum**

| Value | Behaviour |
|-------|-----------|
| `Chaser` | Always moves toward the player's current position |
| `Ambusher` | Targets a position ahead of the player's movement direction |
| `Random` | Randomly targets the player or picks a random free direction (bias increases per level) |
| `Patroller` | Patrols the four corners; chases the player when within `patrolChaseRange` |

**Scatter mode**

Every `scatterInterval` seconds the ghost enters scatter mode for `scatterDuration`
seconds, during which it moves randomly regardless of personality.

**Level scaling** (`ApplyLevelScaling`)

| Variable | Base | Max | Growth |
|----------|------|-----|--------|
| `randomBias` | 0.4 | 0.85 | +0.03 / level |
| `patrolChaseRange` | 6 | 14 | +0.53 / level |
| `ambushLookahead` | 4 | 8 | +0.27 / level |

**Golden mode reaction**

When `golden_blood_collector.invincible` is `true`, all ghosts call `FleeFromPlayer`
(move away from the player) instead of their normal behaviour.

**Inspector fields**

| Field | Type | Description |
|-------|------|-------------|
| `tilemap` | `Tilemap` | Wall tilemap (set by LevelManager) |
| `player` | `Transform` | Player transform (set by LevelManager) |
| `speed` | `float` | Ghost movement speed |
| `wallMask` | `LayerMask` | Wall physics layer |
| `personality` | `GhostPersonality` | Assigned randomly on spawn |
| `scatterInterval` | `float` | Seconds between scatter phases |
| `scatterDuration` | `float` | Duration of each scatter phase |

---

### Sprites/Scripts/BloodCollector.cs — `BloodCollector`

| | |
|---|---|
| Base class | `MonoBehaviour` |
| Purpose | Detects when the player walks over a blood tile, increments the score, updates the score bar, and signals level completion. |

**Static state**

| Field | Type | Description |
|-------|------|-------------|
| `globalScore` | `static int` | Accumulated score across all levels; reset on game over or game complete |

**Inspector fields**

| Field | Type | Description |
|-------|------|-------------|
| `bloodTilemap` | `Tilemap` | Tilemap containing collectible blood tiles |
| `scoreText` | `TextMeshProUGUI` | Score display label |
| `scorebarBorder` | `SpriteRenderer` | Sliced sprite for the score bar border |
| `scorebarFill` | `SpriteRenderer` | Sliced sprite for the score bar fill |
| `widthPerPoint` | `float` | How much the fill bar grows per collected tile |
| `borderCornerPadding` | `float` | Extra width for the border sprite corners |
| `collectSound` | `AudioClip` | Played on each tile collection |
| `collectVolume` | `float` | Volume of collect sound |

**Key methods**

| Method | Description |
|--------|-------------|
| `InitializeLevel()` | Counts all tiles, resets counters, sizes the score bar border |
| `ResetGlobalScore()` | static; resets `globalScore` to 0 and refreshes the UI |

---

### Sprites/Scripts/golden_blood_collector.cs — `golden_blood_collector`

| | |
|---|---|
| Base class | `MonoBehaviour` |
| Purpose | Handles golden-blood tile collection and the timed invincibility power-up. |

**Inspector fields**

| Field | Type | Description |
|-------|------|-------------|
| `bloodTilemap` | `Tilemap` | Golden blood tilemap (set by LevelManager) |
| `bloodLayer` | `LayerMask` | Layer of the golden blood collider |
| `invincible` | `bool` | Public flag read by GhostMovement and LevelManager |
| `INVINCIBLE_DURATION` | `float` | Invincibility duration in seconds (default 8) |
| `characterSpriteRenderer` | `SpriteRenderer` | Player sprite renderer |
| `redSprite` / `goldenSprite` | `Sprite` | Normal and golden player sprites |
| `goldenActivateSound` | `AudioClip` | Played on power-up activation |

**Flash warning**

When `invincibleTimer < 3f` the player sprite flashes (alternates between full and
40 % alpha at 0.15 s intervals) to warn that invincibility is about to expire.

---

### Sprites/Scripts/pausemanager.cs — `PauseManager`

| | |
|---|---|
| Base class | `MonoBehaviour` |
| Purpose | Pause / resume via `Time.timeScale`; shows/hides an optional overlay. |

**Inspector fields**

| Field | Type | Description |
|-------|------|-------------|
| `pauseOverlay` | `GameObject` | Panel shown while paused (optional) |
| `pauseButtonText` | `TMP_Text` | Button label: `▶` when paused, `II` when running (optional) |

**Methods** — wire `TogglePause()` to a UI button's OnClick event.

---

### Sprites/Scripts/togglemute.cs — `MuteButton`

| | |
|---|---|
| Base class | `MonoBehaviour` |
| Purpose | Toggles `AudioListener.volume` between 0 and 1; persists state in PlayerPrefs (`"Muted"`). |

**Inspector fields**

| Field | Type | Description |
|-------|------|-------------|
| `muteSprite` / `unmuteSprite` | `Sprite` | Button sprites for each state |
| `buttonImage` | `Image` | UI Image component to swap sprites on |

---

### Sprites/Scripts/ToggleText.cs — `ToggleText`

| | |
|---|---|
| Base class | `MonoBehaviour` |
| Purpose | Shows/hides an info text panel and hides the scoreboard panels when the info button is clicked. |

**Inspector fields**

| Field | Type | Description |
|-------|------|-------------|
| `textfield` | `GameObject` | Info text panel |
| `backgroundfield` | `GameObject` | Info background panel |
| `scorestuff` / `scoreStuff2` | `GameObject` | Scoreboard panels to hide on info open |

---

### Sprites/Scripts/scoreboard_button.cs — `scoreboard_button`

| | |
|---|---|
| Base class | `MonoBehaviour` |
| Purpose | Fetches the scoreboard on button click and renders the top-3 entries with the current player highlighted. |

**Inspector fields**

| Field | Type | Description |
|-------|------|-------------|
| `textfield` | `GameObject` | Scoreboard text panel |
| `backgroundfield` | `GameObject` | Scoreboard background panel |
| `infostuff` / `infostuff2` | `GameObject` | Info panels to hide on scoreboard open |
| `fontSize` | `float` | TMP font size (default 36) |
| `highlightColor` | `string` | Hex colour for the current player's row (default `#FFD700`) |

**Display logic**

- Top 3 entries are always shown.
- If the current player is outside the top 3, a `"....."` separator and their own
  entry are appended below.
- Player names longer than 9 characters are truncated with `".."`.

---

### Sprites/Scripts/LetterboxCamera.cs — `LetterboxCamera`

| | |
|---|---|
| Base class | `MonoBehaviour` |
| Purpose | Adjusts `Camera.main.orthographicSize` on Start so the target sprite fills the screen without distortion (letterbox/pillarbox). |

**Inspector fields**

| Field | Type | Description |
|-------|------|-------------|
| `targetBounds` | `SpriteRenderer` | The background/maze sprite whose dimensions define the target aspect ratio |

---

## Game Flow

```
App Start
  └─ LevelManager.Start()
       ├─ Restore PlayerPrefs (player ID, name, score, level index)
       └─ ShowMainMenu()

Main Menu
  └─ Player enters name → StartGame()
       ├─ Hide menu, show HUD
       └─ LoadLevel(currentLevelIndex)
            └─ LoadLevelCoroutine
                 ├─ Unload previous level scene (if any)
                 ├─ Load new level scene additively
                 ├─ SpawnGhostsAtMarkers()
                 └─ AssignLevelReferences()

Gameplay
  ├─ BloodCollector collects last tile
  │    └─ OnLevelComplete()
  │         ├─ currentLevelIndex++, save, SendScoreUpdate()
  │         ├─ [more levels]  → NextLevelScene + LevelCompleteRoutine → ReturnToMenuRoutine
  │         └─ [all done]     → GameCompleteSequence → reset → ShowMainMenu()
  │
  └─ GhostMovement hits player
       └─ OnPlayerHit(ghost)
            ├─ [golden mode]  → Destroy ghost, +1 life, CheckLevelCompletion()
            ├─ [lives > 0]    → lives--, camera shake, vignette, CheckLevelCompletion()
            └─ [lives == 0]   → GameOverSequence → reset → ShowMainMenu()
```

---

## PlayerPrefs Keys

| Key | Type | Description |
|-----|------|-------------|
| `PlayerId` | string | GUID; created once per player session |
| `PlayerName` | string | Display name |
| `GlobalScore` | int | Accumulated score |
| `CurrentLevelIndex` | int | Index into `LevelManager.levelScenes` |
| `Muted` | int (0/1) | Audio mute state |

---

## Naming Notes

Several classes and files do not follow C# PascalCase conventions
(`golden_blood_collector`, `scoreboard_button`, `pausemanager.cs`, `togglemute.cs`).
The class logic is unaffected; renaming would require updating all Inspector
references and is deferred to a future refactor.
