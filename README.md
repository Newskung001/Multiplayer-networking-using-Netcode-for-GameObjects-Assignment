# Unity Player Input & Netcode Learning Project

A beginner-friendly Unity project demonstrating player input handling with Unity's Input System and multiplayer networking using Netcode for GameObjects.

## Project Description

This educational project teaches the fundamentals of implementing player movement and networking in Unity. It covers how to set up player input using the **Invoke Unity Events** approach, configure networked game objects, and create responsive movement mechanics that work in both single-player and multiplayer environments.

## 📺 Demo Video

Watch a demonstration of this project in action. Note that videos may or may not reflect all features the game currently has:
- [YouTube Demo Video v1.8.0 - Match Management, Player Health & Rematch System](https://youtu.be/CNuqDFzaBOo)
- [YouTube Demo Video v1.7.0 - Bomb System & Advanced Gameplay Rules](https://youtu.be/S_bNccx_Vls)
- [YouTube Demo Video v1.6.0 - Item Usage, Respawn & Warnings](https://youtu.be/ayDatHFu2ls)
- [YouTube Demo Video v1.5.0 - Character Selection & Team Colors](https://youtu.be/q5bilQQiywA) 
- [YouTube Demo Video v1.4.0 - Spawn Points & Prefab Improvements](https://youtu.be/SjlMnAtwjQw)
- [YouTube Demo Video v1.3.1 - Player Limits & Duplicate Name Check](https://youtu.be/h2C8v7wJeqk)
- [YouTube Demo Video v1.3.0 - Connection Approval Mode](https://youtu.be/lIrLQUzu-OM)
- [YouTube Demo Video](https://youtu.be/rHt5OeKh73o)

## Key Features

- **Player Input with Invoke Unity Events** - Uses Unity's new Input System with event-based input handling for clean, decoupled code
- **NetworkObject/NetworkTransform Setup** - Proper configuration for synchronizing player position and rotation across the network
- **WASD Movement** - Smooth character movement with rotation towards movement direction
- **Jump Mechanics** - Physics-based jumping with ground detection using sphere checks
- **Connection Approval Mode** - Configurable approval mode with Manual Approve and Always Approve options
- **Player Limits** - Configurable maximum player limit to prevent server overload
- **Duplicate Name Prevention** - Server-side validation to prevent duplicate usernames
- **Spawn Points & Randomization** - Configure spawn locations in the scene and optionally randomize which client gets which point
- **Player State Synchronization** - `PlayerStateSync` script included to mirror custom player state across the network
- **Prefab Enhancements** - Player prefab now has a status renderer and the name text prefab uses a mesh renderer for better visuals
- **Character Selection UI** - Players can choose from multiple avatars before joining the game
- **Team Color & Naming UI Improvements** - Team index is now applied to player prefab visuals and `PlayerName` updates when team colors change; host-specific state handling added to `PlayerStateSync`
- **Item Usage & Respawn** - Players can use items via RPC calls, with usage count UI updates, reset functionality, and a warning UI when uses are depleted.
- **Match Management & Round Flow** - Server-side `MatchManager` handles waiting for players, starting the round, movement locking, and determining winners/draws.
- **Player Health System** - Networked `PlayerHealth` tracks HP and alive status; integrated with the bomb system for server-side damage application.
- **Rematch Voting & Timer** - Post-match UI allows players to vote for a rematch with a 30-second countdown; automatically handles player disconnects by updating the vote requirements.
- **Networked Bomb System** - Players can place bombs that countdown and explode, spawning an `ExplosionEffect` on all clients with a 3D sound effect.
- **Bomb Placement Cooldown** - Server-side cooldown prevents bomb spamming; rejected clients are notified via `ClientRpc`.
- **Active Bomb Limit** - Configurable cap on simultaneous live bombs per player, tracked server-side with `NetworkObjectReference`.
- **`SpawnWithOwnership()` Comparison** - Inspector toggle to switch between `Spawn()` (server-owned) and `SpawnWithOwnership()` (player-owned) to explore ownership behaviour differences.
- **Networked Bomb Requester ID** - `NetworkVariable<ulong>` on `Bomb` exposes the placing player's ID to all clients.
- **Collision-Triggered Bombs** - Optional bomb mode that detonates on contact with a networked object instead of a timer, with configurable arm delay.

## Prerequisites

- **Unity 2022.3+** (URP - Universal Render Pipeline)
- **Netcode for GameObjects** package (via Package Manager)
- **Input System** package (via Package Manager)

## Setup Instructions

1. Clone or download this project
2. Open the project in Unity 2022.3 or later
3. Open the `SampleScene` from `Assets/Scenes/`
4. Ensure NetworkManager is configured in the scene
5. Press **Play** in the Unity Editor
6. Use the UI buttons to start as Host, Client, or Server

## Controls

| Action | Key |
|--------|-----|
| Move | WASD or Arrow Keys |
| Jump | Space |
| Place Bomb | Configured via Input Action Asset (`PlaceBomb`) |

## Project Structure

```
Assets/
├── Scripts/
│   ├── MatchManager.cs          # Centralized match flow, player count, and rematch logic
│   ├── PlayerHealth.cs          # Networked HP management and damage application
│   ├── MatchResultUI.cs         # Game Over UI, rematch voting status, and exit logic
│   ├── MainPlayerScript.cs      # Player movement, jump, and input handling (with movement locking)
│   ├── MainGameManagerScript.cs # Network connection management
│   ├── PlayerBombSpawner.cs     # Input → ServerRpc bomb spawn with cooldown & limit
│   ├── Bomb.cs                  # Server countdown/collision detonation & NetworkVariable requester ID
│   ├── ExplosionEffect.cs       # Timed NetworkObject despawn + 3D explosion audio
│   ├── PlayerStateSync.cs       # Player name, team color, and status synchronization
│   ├── PlayerRpcDemo.cs         # Item use RPCs and usage count NetworkVariable
│   ├── ConnectionManager.cs     # Connection approval, player limits, username validation
│   └── CharacterSelectUI.cs     # Pre-join character/avatar selection UI
├── Prefabs/
│   ├── Player.prefab            # Networked player prefab with NetworkObject & NetworkTransform
│   ├── Bomb.prefab              # Networked bomb with Bomb script and collider
│   └── Explosion.prefab         # Networked explosion effect with ExplosionEffect script
├── Audio/
│   └── explosion.wav            # Explosion sound effect (see Credits)
└── Scenes/
    └── SampleScene.unity        # Main game scene
```

## Learning Resources

This project is part of a Unity learning curriculum focusing on:
- Unity Input System fundamentals
- Netcode for GameObjects basics
- Network behaviour and ownership
- Physics-based character controllers

## Credits

- **Explosion Sound** — Sound Effect by [David Dumais](https://pixabay.com/users/daviddumaisaudio-41768500/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=190266) from [Pixabay](https://pixabay.com/sound-effects//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=190266)

## Changelog

For detailed version history and updates, see [CHANGELOG.md](./CHANGELOG.md).
