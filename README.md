# Unity Player Input & Netcode Learning Project

A beginner-friendly Unity project demonstrating player input handling with Unity's Input System and multiplayer networking using Netcode for GameObjects.

## Project Description

This educational project teaches the fundamentals of implementing player movement and networking in Unity. It covers how to set up player input using the **Invoke Unity Events** approach, configure networked game objects, and create responsive movement mechanics that work in both single-player and multiplayer environments.

## 📺 Demo Video

Watch a demonstration of this project in action: [YouTube Demo Video](https://youtu.be/rHt5OeKh73o)

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

## Changelog

## [v1.2.0] - 2026-02-25

### Added
- **ConnectionManager.cs**: New script implementing comprehensive network connection management with connection approval, error handling, and state management for multiplayer sessions
- **UI Connection System**: Complete player connection flow including lobby screen, connection status indicators, and error messaging
- **Network Configuration**: Dedicated server and client configuration management system
- **Connection Approval Logic**: Custom connection validation with unique player identifiers and session management
- **Error Handling Framework**: Comprehensive network error detection and user feedback system

### Changed
- **Player Controller Integration**: Enhanced existing physics-based character controllers with network synchronization capabilities
- **Network Session Management**: Improved player spawn coordination and session cleanup procedures
- **Connection State Tracking**: Enhanced real-time connection status monitoring and display updates
- **Lobby System Architecture**: Refactored connection flow from basic join/leave to managed lobby experience

### Fixed
- **Connection Stability Issues**: Resolved intermittent disconnection problems during high-latency conditions
- **Player Synchronization**: Fixed desync issues when multiple players join simultaneously
- **State Management**: Corrected connection state persistence across scene transitions
- **Error Recovery**: Implemented automatic retry mechanisms for temporary connection failures

### Security
- **Connection Validation**: Added server-side player authentication and validation checks
- **Session Integrity**: Implemented unique session token generation and verification
- **Rate Limiting**: Added connection attempt limiting to prevent abuse

## [v1.1.0] - 2026-02-24

### Added
- **Physics-based Character Controllers**: Implementation of realistic movement with collision detection
- **Network Synchronization**: Real-time player position and state replication across clients
- **Dedicated Server Support**: Headless server deployment capabilities
- **Connection Status Monitoring**: Real-time connection quality and latency tracking

### Changed
- **Architecture**: Migrated from basic peer-to-peer to client-server networking model
- **Performance**: Optimized network message frequency and data compression
- **Scalability**: Improved support for larger player counts

## [v1.0.0] - 2026-02-23

### Initial Release
- **Core Networking Foundation**: Basic Netcode for GameObjects implementation
- **Player Prefab Setup**: Network-ready player objects with movement controls
- **Scene Management**: Multi-scene networking support
- **Basic Multiplayer Functionality**: Player spawning and basic interaction
