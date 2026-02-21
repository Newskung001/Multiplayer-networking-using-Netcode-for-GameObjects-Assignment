# Unity Player Input & Netcode Learning Project

A beginner-friendly Unity project demonstrating player input handling with Unity's Input System and multiplayer networking using Netcode for GameObjects.

## Project Description

This educational project teaches the fundamentals of implementing player movement and networking in Unity. It covers how to set up player input using the **Invoke Unity Events** approach, configure networked game objects, and create responsive movement mechanics that work in both single-player and multiplayer environments.

## Key Features

- **Player Input with Invoke Unity Events** - Uses Unity's new Input System with event-based input handling for clean, decoupled code
- **NetworkObject/NetworkTransform Setup** - Proper configuration for synchronizing player position and rotation across the network
- **WASD Movement** - Smooth character movement with rotation towards movement direction
- **Jump Mechanics** - Physics-based jumping with ground detection using sphere checks

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

## Project Structure

```
Assets/
├── Scripts/
│   ├── MainPlayerScript.cs    # Player movement, jump, and input handling
│   └── MainGameManagerScript.cs # Network connection management
├── Prefabs/
│   └── Player.prefab          # Networked player prefab with NetworkObject & NetworkTransform
└── Scenes/
    └── SampleScene.unity      # Main game scene
```

## Learning Resources

This project is part of a Unity learning curriculum focusing on:
- Unity Input System fundamentals
- Netcode for GameObjects basics
- Network behaviour and ownership
- Physics-based character controllers
