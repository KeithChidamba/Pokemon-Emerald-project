<div align="center">

# Pokémon Emerald — Unity Engine

**A from-scratch reconstruction of the Pokémon Emerald battle, overworld, and save systems, built in Unity.**

[![Unity](https://img.shields.io/badge/Unity-2022.3.62f1-black?logo=unity)](https://unity.com/)
[![Language](https://img.shields.io/badge/language-C%23-239120)](#)
[![Platform](https://img.shields.io/badge/platform-WebGL-orange)](#)
[![Status](https://img.shields.io/badge/status-in%20development-yellow)](#)

[**▶ Play in Browser**](https://keithchidamba.github.io/Pokemon-Emerald-Build/) · [Build Repo](https://github.com/KeithChidamba/Pokemon-Emerald-Build)

</div>

---

## Overview

This project is a personal challenge: rebuild the core systems of **Pokémon Emerald** from the ground up inside the Unity Engine — not just the look of it, but the underlying architecture. Battle mechanics, move logic, abilities, status effects, the overworld, NPC interaction, inventory, and persistent save data are all implemented independently in C#, then validated through a custom in-engine test suite.

It's an ongoing project (**569+ commits**) rather than a finished product, and it's actively used as a sandbox for exploring proper software architecture patterns — dependency injection, service-oriented design, and automated testing — in a domain (turn-based RPG battle logic) that's notoriously fiddly to get right.

## Features

### ⚔️ Battle System
- Full turn-based battle engine covering damage calculation, type effectiveness, stat changes, status effects, weather, and field conditions
- Individually implemented move logic for special-case moves (multi-hit, semi-invulnerable, recoil, drain, protection, barrier, and unique-logic moves)
- Ability interactions (e.g. Static, Levitate, Guts, Shed Skin, Arena Trap, Pickup) modeled as discrete, testable systems
- Held item effects, including consumables and stat-modifying items
- Support for both single and double battles

### 🗺️ Overworld & World Systems
- Tile-based overworld movement and interaction, built on Unity's Tilemap system
- NPC behavior and dialogue/interaction handling
- Story objective tracking for progressing through the game's narrative beats
- Item pickups, Pokémon Centers, PokéMarts, and PC storage

### 🎒 Pokémon & Item Management
- Pokémon data modeling (stats, moves, evolution, move-learning)
- Full inventory and item-usage system (evolution items, level-up items, power-point items)
- PC box storage for managing a full Pokémon collection

### 💾 Persistent Save System
- JSON-based serialization for player, party, and world state
- Directory-structured save data (separated by category: Player, Items, Overworld, Party Pokémon, PC Storage)

### 🧪 Custom Automated Testing Framework
Rather than relying only on manual playtesting, the project includes a hand-built testing framework that runs inside the engine:
- **64 test files**, the majority (54) targeting battle-specific mechanics — abilities, status effects, held items, and individual move logic
- Custom `UnitTestHandler` and `TestCaseHandler` classes for defining and running test cases with pass/fail conditions
- Reusable `TestTemplates` and a `TestActionSequencer` to reduce boilerplate across battle test scenarios
- Integration tests validating multi-system interactions, not just isolated units

### 🌐 WebGL Build & Browser Persistence
The project compiles to a playable WebGL build with a save system engineered specifically for the constraints of running in a browser:
- Custom **JavaScript plugins (`.jslib`)** bridge Unity's C# runtime and the browser environment
- Save data is mounted to an **in-memory IndexedDB file system (IDBFS)** via Emscripten's `FS` API, giving the game a persistent, structured "file system" inside the browser sandbox
- Players can **download their entire save directory as a `.zip`** (via JSZip) and **re-upload it** later to restore progress — enabling save backups and transfer between browsers/devices

## Architecture

The codebase leans on a lightweight dependency-injection setup rather than relying on Unity singletons everywhere:

| Component | Responsibility |
|---|---|
| `GameInstaller` | Bootstraps and registers all game services on startup |
| `ServiceContainer` | Central registry resolving dependencies between systems |
| `InstanceFactory` | Handles object construction for services and runtime instances |
| Input State Services | Context-specific input handlers (Battle, Party, PC Storage, Bag, Settings, Typing, PokéMart, Details) swapped based on the active game state |

This keeps individual systems (battle, save data, UI, input) decoupled and independently testable — which is what makes the custom test suite above practical to maintain.

Game content (Pokémon, moves, items) is largely data-driven, using **32 ScriptableObjects** to separate content definitions from logic. Asynchronous sequencing (animations, battle turn order, UI transitions) is handled through **92 coroutine-driven scripts**.

## Tech Stack

| Category | Details |
|---|---|
| Engine | Unity 2022.3.62f1 (LTS) |
| Language | C# |
| Rendering | Unity 2D feature set, Tilemap |
| UI | Unity UGUI, TextMesh Pro |
| Navigation | Unity AI Navigation |
| Testing | Custom in-house test harness |
| Web Interop | Hand-written JavaScript (`.jslib`), Emscripten FS / IDBFS, JSZip |
| Deployment | WebGL, hosted via GitHub Pages |

## Project Structure

```
Assets/
├── Scripts/
│   ├── Battle/            # Turn-based battle engine (46 scripts)
│   ├── Pokemon/           # Pokémon data, stats, move-learning (10 scripts)
│   ├── Items/             # Inventory & item-usage logic (20 scripts)
│   ├── NPC/                # NPC behavior & interaction (5 scripts)
│   ├── Story Objectives/   # Narrative/quest progression (17 scripts)
│   ├── overworld/          # Tile-based world movement (24 scripts)
│   ├── System/             # DI container, input services, save handling (25 scripts)
│   ├── Ui/                 # UI logic (26 scripts)
│   ├── Testing/            # Custom test framework (64 scripts)
│   └── player_scripts/     # Player controller & state (5 scripts)
├── Plugins/                 # WebGL JavaScript interop (.jslib)
├── Save_data/ & Temp_Save_data/   # Runtime save directory structure
├── Resources/, Tiles/, Palettes/  # Art & world-building assets
└── WebGLTemplate/            # Custom WebGL build template
```

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/KeithChidamba/Pokemon-Emerald-project.git
   ```
2. Open the project in **Unity 2022.3.62f1** (or later 2022 LTS) via Unity Hub.
3. Open the main scene from `Assets/Scenes` and hit Play.

To try it without installing Unity, use the [live WebGL build](https://keithchidamba.github.io/Pokemon-Emerald-Build/) instead — save data persists in-browser and can be downloaded/restored via the in-game backup option.

## Roadmap

- [ ] Expand automated test coverage
- [ ] Additional overworld areas and story content
- [ ] Further battle mechanic polish (double-battle edge cases, weather interactions)

## Author

**Keith Kudakwashe Chidamba**
Software Engineering student · [LinkedIn](https://www.linkedin.com/in/keith-chidamba-611ba8222/) · [GitHub](https://github.com/KeithChidamba)

---

<sub>This is a fan-made, non-commercial educational project. Pokémon is a trademark of Nintendo/Game Freak/Creatures Inc. This project is not affiliated with or endorsed by any of the above.</sub>
