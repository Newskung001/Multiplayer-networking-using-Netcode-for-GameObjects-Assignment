# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [v1.9.0] - 2026-04-29

### Added
- **Online Session Management**: Implemented online session management using Unity Services for robust multiplayer connectivity.
- **Lobby UI**: Added a `LobbyClient` button and placeholder UI elements to the `SampleScene` for lobby interaction.

### Fixed
- **Session Stability**: Improved session management and stability to ensure reliable network connections.
- **UI Layout**: Adjusted anchored positions for UI elements in `SampleScene` for better alignment across resolutions.

### Git Commits
- Commits: [ab1d228..5b7e9b1](https://github.com/Newskung001/Multiplayer-networking-using-Netcode-for-GameObjects-Assignment/compare/ab1d228...5b7e9b1f2b6c4890bd7c3367c365bc9cf9eea34b) (`ab1d228`, `b37f969`, `9da503b`, `2a71b4c`, `5b7e9b1`)
- Author: Newskung001
- Date: 2026-04-29

---

## [v1.8.0] - 2026-04-21

### Added
- **Match Management System**: Centralized match flow in `MatchManager.cs`. The server now waits for a required number of players (default 2) before starting the round.
- **Player Health & Survival**: Implemented `PlayerHealth` with networked `CurrentHP` and `IsAlive` variables. Damage from bombs is calculated server-side and synchronized across all clients.
- **Movement Locking**: Players are automatically locked from moving before a match starts, after being defeated, or when the match concludes.
- **Rematch Voting System**: A post-match voting mechanic that requires all connected players to agree before a new round begins. Supports dynamic player count updates if someone leaves during the voting phase.
- **Match Timer**: A 30-second countdown during the Game Over state; the match automatically resets if the timer expires.
- **Game Over UI**: A dedicated `MatchResultUI` panel that displays the winner, rematch status ("X/Y voted"), and the countdown timer.
- **Exit Logic**: Integrated an "Exit" button that safely shuts down the NetworkManager and reloads the scene for a clean return to the login screen.

### Fixed
- **UI Initialization**: Implemented a coroutine-based wait in `MatchResultUI` to ensure the `MatchManager` is fully networked before syncing state, preventing race conditions on Clients.
- **UI Layering**: Added `SetAsLastSibling()` logic to ensure the Game Over panel always renders on top of dynamic player name tags.

### Git Commits
- Commits: [5c4b935..81a69b1](https://github.com/Newskung001/Multiplayer-networking-using-Netcode-for-GameObjects-Assignment/compare/5c4b935b766ec0e110ed6916730c5dfc540e041a...81a69b1d62598d1d8a9a6579c4edf677dc638ec1) (`5c4b935`, `da61709`, `290b200`, `5d8cd3b`, `87947f1`, `81a69b1`)
- Author: Newskung001
- Date: 2026-04-21

## [v1.7.0] - 2026-03-29

### Added
- **Bomb System**: Introduced a fully networked bomb mechanic with `PlayerBombSpawner`, `Bomb`, and `ExplosionEffect` scripts. Players can place bombs using the new `PlaceBomb` input action; the server handles spawn, countdown, explosion spawn, and despawn.
- **Explosion Prefab & Effect**: An `ExplosionEffect` NetworkObject is spawned at detonation and auto-despawns after its lifetime expires.
- **Explosion Sound**: `ExplosionEffect` now plays a 3D audio clip via `AudioSource.PlayClipAtPoint` on all clients when the explosion spawns. Sound credit: *Explosion Sound* by [David Dumais](https://pixabay.com/users/daviddumaisaudio-41768500/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=190266) from [Pixabay](https://pixabay.com/sound-effects//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=190266).

### Optional Extensions (Advanced Gameplay Rules)
- **Bomb Placement Cooldown** *(Feature 1)*: Server-side per-player cooldown (`bombCooldown`, default 2s) prevents bomb spamming. Rejected requests notify the owner client via `ClientRpc` with the remaining cooldown time.
- **Active Bomb Limit** *(Feature 2)*: Configurable maximum simultaneous bombs per player (`maxActiveBombs`, default 3). The server tracks live bomb references and rejects placements when the limit is reached.
- **`SpawnWithOwnership()` Comparison** *(Feature 3)*: Added `useSpawnWithOwnership` toggle on `PlayerBombSpawner`. When enabled, bombs use `SpawnWithOwnership(senderClientId)` so `OwnerClientId` matches the requesting player; a log line shows `OwnerMatchesRequester` for direct comparison with the default `Spawn()` (server-owned) behaviour.
- **Networked Bomb Requester ID** *(Feature 5)*: Added `NetworkVariable<ulong> NetworkedRequesterId` on `Bomb` so all clients know which player placed each bomb. `OnNetworkSpawn` logs are now emitted on every client, not just the server.
- **Collision-Triggered Detonation** *(Feature 6)*: `Bomb` supports a `triggerByCollision` mode that skips the timer and detonates on `OnTriggerEnter` with any other `NetworkObject`. An `armDelay` (default 0.5s) prevents instant self-detonation on placement.

### Changed
- **`Bomb.cs`**: Extracted shared explosion logic into a `Detonate()` helper method used by both timer and collision paths. `OnNetworkSpawn` logs are now visible to all clients.
- **`ExplosionEffect.cs`**: Removed `IsServer` guard from `OnNetworkSpawn` for the audio path so all clients play the sound independently.

### Git Commits
- Commits: [8550f5c..ece0915](https://github.com/Newskung001/Multiplayer-networking-using-Netcode-for-GameObjects-Assignment/compare/8550f5c30994ff31dba13f33bc326f13e90f7e75...ece0915963f5b630a5310051daf8698614b8c42d) (`f4c54c1`, `8ba4de4`, `dfbbae1`, `ece0915`)
- Author: Newskung001
- Date: 2026-03-29

---

## [v1.6.0] - 2026-03-17

### Added
- **Player Respawn & RPC**: Added respawn flow and RPC functionality for player actions and item interactions.
- **Item Usage System**: Implemented item use + reset mechanics in `PlayerRpcDemo` with UI updates for remaining uses.
- **Warning UI for Item Usage**: Added a "no uses remaining" warning UI and flashing effect when an item is depleted.

### Changed
- **Dependencies & Settings**: Updated project settings and Unity package dependencies for compatibility.

### Git Commits
- Commits: [cd7798d..a15e30b](https://github.com/Newskung001/Multiplayer-networking-using-Netcode-for-GameObjects-Assignment/compare/a15e30b...cd7798d) (`cd7798d`, `4c07b7f`, `2798630`, `0ca98e8`, `71ce495`, `a15e30b`)
- Author: Newskung001
- Dates: 2026-03-17

## [v1.5.0] - 2026-03-11

### Added
- **Character Selection UI**: New UI screen allowing players to pick their character avatars prior to joining.
- **Team Color Support**: Player prefab can now display team colors and updates `PlayerName` UI dynamically when team changes. Host state is distinguished in `PlayerStateSync`.

### Changed
- **Player Prefabs**: Added `worldOffset` and `TeamIndex` fields; enhanced `PlayerStateSync` to handle host-specific logic.

### Git Commits
- Commits: [787c4bb..30cc81f](https://github.com/Newskung001/Multiplayer-networking-using-Netcode-for-GameObjects-Assignment/compare/30cc81f...787c4bb) (`787c4bb`, `b0da738`, `30cc81f`)
- Author: Newskung001
- Dates: 2026-03-11

## [v1.4.0] - 2026-03-10

### Added
- **Spawn Points**: Configurable spawn locations with optional random selection for clients
- **Player State Sync**: `PlayerStateSync` script added to synchronize additional player data across the network
- **Prefab Enhancements**: Player prefab now includes a `statusRenderer`; the PlayerName text prefab gained a `MeshRenderer` component

### Fixed
- **Prefab Hash & Approval**: Compute and assign prefab hash before connection approval checks, with added debug logging; corrected scene hash in `SampleScene` to avoid mismatches

### Changed
- **Dependencies**: Updated Unity package dependencies to the latest compatible versions

### Documentation
- **Changelog Links**: Added Git commit references to changelog entries for easier navigation

### Git Commits
- Commits: [9f4ed43..68e6d43](https://github.com/Newskung001/Multiplayer-networking-using-Netcode-for-GameObjects-Assignment/compare/68e6d43...9f4ed43) (`9f4ed43`, `a965ec5`, `e229b4c`, `c0e0079`, `eca9786`, `68e6d43`)
- Author: Newskung001
- Dates: 2026-03-09 through 2026-03-10

---

## [v1.3.1] - 2026-02-28

### Added
- **Player Limits**: New configurable `maxPlayers` setting in ConnectionManager to limit simultaneous connections
- **Input Validation**: maxPlayers field validation in Start() method - invalid values (≤ 0) default to 6
- **Server Full Handling**: Connection rejection with "Server Full (Maximum players reached)" message when limit reached
- **Duplicate Name Prevention**: Enhanced server-side validation to reject connections with existing usernames

### Changed
- **ApprovalCheck Refactoring**: Restructured validation flow with player count check before approval mode check
- **Documentation**: Added XML documentation for the `_connectedNames` field explaining its dual purpose

### Git Commit
- Commit: [`d600ec0`](https://github.com/Newskung001/Multiplayer-networking-using-Netcode-for-GameObjects-Assignment/commit/d600ec008679a1dad1b1cf5ec9f2211023479717)
- Author: Newskung001
- Date: 2026-02-28

---

## [v1.3.0] - 2026-02-27

### Added
- **ApprovalMode Enum**: New enum with `AlwaysApprove` and `ManualApprove` options for connection approval control
- **Connection Settings**: Configurable approval mode via Inspector with descriptive tooltip
- **Console Command**: `set-approve` command to toggle manual approval state at runtime
- **Host Auto-Approval**: Host connection is now automatically approved (Netcode cannot reject itself)
- **Connection Approval Enforcement**: Connection approval is now force-enabled in code on Start

### Changed
- **ApprovalCheck Refactoring**: Restructured approval logic with clearer step-by-step validation flow
- **SetupApprovedResponse Method**: Extracted response setup into dedicated helper method for code reuse
- **Debug Logging**: Enhanced logging with "incoming Name =" prefix for clearer debugging
- **Connection Data Processing**: Improved payload handling and error messaging

### Security
- **Manual Approval Mode**: Servers can now require manual approval via console before accepting connections
- **Auto Approval Mode**: Optional always-approve mode for development/testing scenarios

### Git Commit
- Commit: [`adacdea`](https://github.com/Newskung001/Multiplayer-networking-using-Netcode-for-GameObjects-Assignment/commit/adacdea)
- Author: Newskung001
- Date: 2026-02-27

---

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

### Git Commit
- Commit: [`67ab28a`](https://github.com/Newskung001/Multiplayer-networking-using-Netcode-for-GameObjects-Assignment/commit/67ab28a)
- Author: Newskung001
- Date: 2026-02-23

---

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

### Git Commit
- Commit: [`0800691`](https://github.com/Newskung001/Multiplayer-networking-using-Netcode-for-GameObjects-Assignment/commit/0800691)
- Author: Newskung001
- Date: 2026-02-21

---

## [v1.0.0] - 2026-02-23

### Initial Release
- **Core Networking Foundation**: Basic Netcode for GameObjects implementation
- **Player Prefab Setup**: Network-ready player objects with movement controls
- **Scene Management**: Multi-scene networking support
- **Basic Multiplayer Functionality**: Player spawning and basic interaction

### Git Commit
- Commit: [`6673b48`](https://github.com/Newskung001/Multiplayer-networking-using-Netcode-for-GameObjects-Assignment/commit/6673b48)
- Author: Newskung001
- Date: 2026-02-20
