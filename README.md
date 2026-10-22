<div align="center">

# Pokémon Emerald: Unity Engine

**A from-scratch reconstruction of Pokémon Emerald's battle, overworld, story, and save systems, built in Unity.**

[![Unity](https://img.shields.io/badge/Unity-2022.3.62f1-black?logo=unity)](https://unity.com/)
[![Language](https://img.shields.io/badge/language-C%23-239120)](#)
[![Platform](https://img.shields.io/badge/platform-WebGL-orange)](#)
[![Commits](https://img.shields.io/badge/commits-600%2B-blue)](#)
[![Status](https://img.shields.io/badge/status-in%20development-yellow)](#upcoming-content)

[**▶ Play in Browser**](https://keithchidamba.github.io/Pokemon-Emerald-Build/) · [Build Repo](https://github.com/KeithChidamba/Pokemon-Emerald-Build)

</div>

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Testing Framework](#testing-framework)
- [WebGL Build & Browser Persistence](#webgl-build--browser-persistence)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Upcoming Content](#upcoming-content)
- [Author](#author)

## Overview

This project is a personal challenge: rebuild the core systems of **Pokémon Emerald** from the ground up inside the Unity Engine. The aim is to reproduce the underlying architecture, not just the look of the game. Battle mechanics, move logic, abilities, status effects, items, the overworld, NPCs, story progression, sound, and persistent save data are all implemented independently in C#, then validated by a custom in-engine test suite.

Development began in December 2024 and has reached **600+ commits and roughly 277 C# scripts**. The project doubles as a sandbox for practising proper software architecture (dependency injection, data-driven design, automated testing) in a domain where turn-based battle logic is notoriously fiddly to get right.

## Features

### ⚔️ Battle System
- Full turn-based battle engine covering damage calculation, type effectiveness, stat changes, status effects, weather, and field conditions
- Single and double battles against wild Pokémon and trainers
- Individually implemented logic for special-case moves (multi-hit, semi-invulnerable, recoil, drain, protection, barriers, and unique-logic moves such as Bide, Pursuit, Whirlwind, and Silver Wind)
- Ability interactions (e.g. Static, Levitate, Guts, Shed Skin, Arena Trap, Pickup) modeled as discrete, testable systems
- Held item effects, including consumables and move-locking items such as Choice Band
- EXP gain and EXP Share, bonus EXP, and an in-battle evolution flow with a dedicated evolution screen

### 📖 Story Objective System
A data-driven progression system that scripts the game's narrative beats:
- Abstract `StoryObjective` ScriptableObjects with **13 objective types**, including wild and trainer battles, NPC interaction and movement, item pickups, gift Pokémon, market and storage UI usage, and berry interactions
- Objectives are **grouped into sections**, with their own save handling so progress persists between sessions
- **Player position boundaries** restrict movement while an objective is active
- **42 objective assets** drive the content, including a complete tutorial sequence

### 🗺️ Overworld, Areas & NPCs
- Tile-based overworld movement and interaction on Unity's Tilemap system
- **Area system** (`AreaManager`, `AreaData`) with tile-based area switching, per-area NPC loading, and biome data. Currently defined areas: Open Garden, PokéMart, PokéCenter, Route 101, Route 102, and Battle Zone
- NPCs with scripted movement, player auto-detection, and a follow mechanic for story sequences
- Wild encounter tables, fishing encounter areas, berry trees with planting and growth, and overworld item pickups
- PokéMarts, Pokémon Center healing (with animations), and PC storage

### 🎒 Pokémon & Item Management
- Pokémon data modeling: stats, natures, abilities, EV yields, learnsets, and evolution (level, item, and friendship-based)
- A modular **info module** system for item and move data, so each item or move type carries only the behaviour it needs
- Broad item support: Poké Balls, potions, revives, status healers, X-items, berries, herbs, vitamins, Rare Candy, evolution stones, Ether and PP modifiers, TMs/HMs, held items, and fishing rods

### 🔊 Sound System
- Central `SoundManager` handling music, jingles, UI sounds, and sound effects across separate layers
- Music ducking, fade control, and pitch variance per sound, backed by 60+ audio files
- Editor tooling to auto-populate sound entries from a folder

### 💾 Persistent Save System
- JSON-based serialization for player, party, story progress, and world state
- Directory-structured save data, separated by category (Player, Items, Overworld, Party Pokémon, PC Storage)

## Architecture

The codebase uses a lightweight dependency-injection setup rather than relying on Unity singletons:

| Component | Responsibility |
|---|---|
| `GameInstaller` | Bootstraps and registers all game services on startup |
| `ServiceContainer` | Central registry resolving dependencies between systems |
| `InstanceFactory` | Handles object construction for services and runtime instances |
| `IInjectable` | Contract for systems that receive their dependencies from the container |
| Input State Services | Context-specific input handlers (Battle, Party, PC Storage, Bag, Settings, Typing, PokéMart, Details) swapped based on the active game state |

This keeps individual systems (battle, save data, UI, input, areas, story objectives) decoupled and independently testable, which is what makes the test suite below practical to maintain.

Game content (Pokémon, moves, items, areas, story objectives) is largely data-driven through **ScriptableObjects**, separating content definitions from logic. Asynchronous sequencing (animations, battle turn order, UI transitions) is handled through **119 coroutine-driven scripts**.

## Testing Framework

Rather than relying on manual playtesting alone, the project includes a hand-built testing framework that runs inside the engine. It has grown to **93 test files** across three tiers:

| Tier | Files | Scope |
|---|---|---|
| **Integration** | 55 | Abilities, special moves (semi-invulnerability, Pursuit, Silver Wind, and other unique-logic moves), held items, and battle systems |
| **End-to-End** | 15 | Full item and progression flows: potions, revives, berries, herbs, X-items, Poké Balls, EXP gain and share, in-battle evolution, move lock |
| **General** | 12 | Overworld item behaviour: evolution stones, Rare Candy, vitamins, TMs/HMs, Ether, held-item give/take |

Supporting infrastructure:
- `TestRegistry` defines which tests run and in what order (about 72 tests registered)
- `TestCaseHandler`, `UnitTestHandler`, and `TestActionSequencer` manage test cases, pass/fail conditions, and scripted action sequences
- `TestingEnvironmentHandler` sets up a controlled game state for each test
- A custom **editor tool** (`Tools > Test Class Generator`) scaffolds new Integration and End-to-End test classes from reusable templates, cutting boilerplate when adding coverage

## WebGL Build & Browser Persistence

The project compiles to a playable WebGL build, with a save system engineered for the constraints of running in a browser:

- Custom **JavaScript plugins (`.jslib`)** bridge Unity's C# runtime and the browser environment
- Save data is mounted to an **in-memory IndexedDB file system (IDBFS)** via Emscripten's `FS` API, giving the game a structured "file system" inside the browser sandbox
- Players can **download their entire save directory as a `.zip`** (via JSZip) and **re-upload it** later to restore progress, enabling save backups and transfer between browsers or devices
- A scripted `BuildScript` editor tool produces the WebGL build, which is published from the separate [Build repo](https://github.com/KeithChidamba/Pokemon-Emerald-Build) via GitHub Pages

## Tech Stack

| Category | Details |
|---|---|
| Engine | Unity 2022.3.62f1 (LTS) |
| Language | C# |
| Testing | Unity Test Framework + custom in-house test harness |
| Web Interop | Hand-written JavaScript (`.jslib`), Emscripten FS / IDBFS, JSZip |
| Editor Tooling | Custom Unity editor windows (test generator, asset lookup, sound auto-populate, build script) |
| Deployment | WebGL, hosted via GitHub Pages |

## Project Structure

```
Assets/
├── Scripts/
│   ├── Battle/             # Turn-based battle engine (45 scripts)
│   ├── Pokemon/            # Pokémon data, stats, evolution, learnsets (10 scripts)
│   ├── Items/              # Item handling and info modules (16 scripts)
│   ├── NPC/                # NPC logic, movement, and creation data (7 scripts)
│   ├── Story Objectives/   # Narrative and quest progression (17 scripts)
│   ├── overworld/          # Areas, encounters, interactions, berry trees (24 scripts)
│   ├── System/             # DI container, input services, saving, sound, info modules (28 scripts)
│   ├── Ui/                 # UI logic (26 scripts)
│   ├── Testing/            # Custom test framework and test suites (93 scripts)
│   └── Player/             # Player movement and data (4 scripts)
├── Editor/                 # Custom editor tooling
├── Plugins/                # WebGL JavaScript interop (.jslib)
├── Scenes/                 # Main.unity
├── Save_data/ & Temp_Save_data/   # Runtime save directory structure
├── Resources/, Tiles/, Palettes/  # Game data, art, and world-building assets
└── WebGLTemplate/          # Custom WebGL build template
```

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/KeithChidamba/Pokemon-Emerald-project.git
   ```
2. Open the project in **Unity 2022.3.62f1** (or a later 2022 LTS) via Unity Hub.
3. Open `Assets/Scenes/Main.unity` and press Play.

To try it without installing Unity, use the [live WebGL build](https://keithchidamba.github.io/Pokemon-Emerald-Build/). Save data persists in the browser and can be downloaded and restored through the in-game backup option.

## Upcoming Content

> ### 🚧 Coming Soon: The Battle Zone
> A new **Battle Zone** area is currently in development, building on the existing battle engine and story objective system. More story content will follow as the area comes together.
>
> Development is active, so expect frequent updates and changes while this content is built.

## Author

**Keith Kudakwashe Chidamba**
Software Engineering student · [LinkedIn](https://www.linkedin.com/in/keith-chidamba-611ba8222/) · [GitHub](https://github.com/KeithChidamba)

---

<sub>This is a fan-made, non-commercial educational project. Pokémon is a trademark of Nintendo/Game Freak/Creatures Inc. This project is not affiliated with or endorsed by any of the above.</sub>
