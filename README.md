# Dungeons Together
**by Eoin Ocathasaigh & Cian Dicker Hughes**

**Dungeons Together** is a Dungeons & Dragons (D&D) companion app for playing tabletop RPG with your friends online, built in Unity. Our game brings the core systems of the game to life, including gameplay/"Campaign" management to help Dungeon Masters (DMs), a streamlined character creator for players to create their own personas, and enhanced interaction with the core mechanics of combat through real-time turn-based combat accessible to players and controlled by the DM.

## Play the Game
Play the full game on itch.io: **[Dungeons Together](https://e-ocasey.itch.io/dungeons-together)**

## Features
* Real-time multiplayer gameplay with synchronized maps and tokens
* Dice rolling system
* Character management and persistence
* Campaign session management (save and resume)
* Intuitive UI for players and Dungeon Masters
* Tile-based map interaction

## Objectives
* Enable real-time multiplayer gameplay with synchronized maps, tokens, and dice rolls
* Provide persistent session management, allowing players to save and resume campaigns
* Design a modular, extensible architecture for future AI/LLM integration (e.g., NPC dialogue, rule assistance)
* Ensure a reliable and low-latency networking system using Unity Relay and Netcode for GameObjects
* Develop an intuitive and responsive UI for both players and Dungeon Masters

## Tech Stack
* Unity 6 (C#)
* Netcode for GameObjects v2.7.0 (Networking)
* Unity Services Multiplayer v1.2.0
    * Unity Relay (Low-latency networking)
    * Unity Transport (UTP)
* Hugging Face - Pre Built/Trained LLM's and Data sets

## Project Structure
* **Assets/Scripts/** — Core game logic and systems
* **Assets/Scenes/** — Game scenes (Start Play at: `BootStrap.unity`)
* **Assets/Prefabs/** — Reusable game objects
* **Assets/Characters/** — Character assets and management
* **Assets/Campaigns/** — Campaign data and session management
* **Assets/Enemies/** — Enemy assets and prefabs
* **Assets/Resources/** — Runtime-loaded assets
* **Assets/Animator/** — Animation controllers
* **Assets/Game Audio/** — Music and sound effects
* **Assets/AI Datasets/** & **Assets/AI Files/** — LLM datasets and AI integration files
* **Assets/UI Toolkit/**, **Assets/Our UI/**, **Assets/Fantasy Wooden GUI Free/**, **Assets/Simple Fantasy GUI/** — UI assets and layouts
* **Assets/Cainos/**, **Assets/LowlyPoly/**, **Assets/Tree_Textures/** — Third-party art packs
* **Assets/Settings/** — Game configuration files
* **Assets/UserData/** — Persistent user

## Getting Started
### Prerequisites
* Unity 6 (Unity Hub recommended)
* A Unity account with a linked Unity Services project (required for Relay/Multiplayer)
* Git

### Running the project
1. Clone the repository and open the project folder in Unity Hub
2. Open `Assets/Scenes/BootStrap.unity` as the starting scene
3. Press **Play** in the editor, or build the project for your target platform

### Multiplayer (Host & Join)
* The **Dungeon Master** hosts a session from the main menu, which generates a Unity Relay join code
* **Players** enter the join code from the main menu to connect to the DM's session
* All map state, tokens, dice rolls and combat are synchronized via Netcode for GameObjects

## Development
This project is currently being built with the **Agile** Software Development method in which we engage in the following practices:
1. **Scrum** - we organise regular sprints in which we focus our efforts on a particular feature/set of features to accomplish in the iterative cycles of sprints. We frequently update one another in our daily & weekly meetings on developments, notable changes or errors encountered. This helps us ensure we understand the current state of things and the path ahead
2. **CI/CD** - An important step in our development process in which we regularly commit/merge our code changes/developments to this shared repository to help inform the others processes or development
3. **Test-Driven Development & User Stories** - Eoin is currently managing and tracking this section in which we design tests prior to any code/assets being created. We give ourselves an understanding of the processes, flow of user interaction and absolutely necessary features and then go on to ensure these cases pass or the acceptance criteria of the user stories is met before we proceed or consider a certain feature complete

This form of development is incredibly useful for us as both Eoin and Cian are long-time fans of D&D, so we understand the features, design considerations, feasibility of features and absolute requirements of this project — not only from a developer's perspective but also a customer's one. In a way, we are the users/customers who are informing the development process and test cases.

## Credits & Attribution

### UI Elements
* **Simple Fantasy GUI** — Nayrissa — [Unity Asset Store](https://assetstore.unity.com/packages/2d/gui/simple-fantasy-gui-99451)
* **Pixel Art Icon Pack** — Cainos — [Unity Asset Store](https://assetstore.unity.com/packages/2d/gui/icons/pixel-art-icon-pack-rpg-158343)
* **Fantasy Wooden GUI** — Black Hammer — [Unity Asset Store](https://assetstore.unity.com/packages/2d/gui/fantasy-wooden-gui-free-103811)

### Wallpapers (pre-generated)
* **CampaignsScreen** — [WallpapersDen](https://wallpapersden.com/cover-of-dungeons-and-dragons-wallpaper/)
* **DMScreenWallpaper** — [WallpapersDen](https://wallpapersden.com/cover-of-dungeons-and-dragons-wallpaper/)
* **WaitingRoomBG** — [WallpaperCave](https://wallpapercave.com/w/wp5902013)
* **MainMenu** — [Wallpaper House](https://wallpaper-house.com/wallpaper-id-176055.php)

### Sprites
* **Enemy Icons** — Roll20 website
* **Mahoraga** — Kota_2kx

### Textures
* **Map Textures** — [Freepik](https://www.freepik.com/)
* **Void Texture** — Minecraft

All other code, scripts and original art are © Eoin Ocathasaigh & Cian Dicker Hughes.