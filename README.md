# CE-Final-Project

# Dungeons Together
**by Eoin Ocathasaigh & Cian Dicker Huges**

**Dungeons together** is a Dungeons & Dragons (D&D) companion app for playing tabletop RPG with your friends online, built in Unity. Our Game hopes to bring the core systems of the game to life, such as gameplay/"Campaign" management to help Dungeon Masters (DM), a streamlined character creator for players to create their own new personas, and enhanced interaction with the core mechanics of combat through real time turn based combat accessible to players and controlled by the DM.

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
* Unity
* Netcode for GameObjects (Networking)
* Unity Relay (Low-latency networking)
* Hugging Face - Pre Built/Trained LLM's and Data sets

## Project Structure
* **Assets/Scripts/** - Core game logic and systems
* **Assets/Scenes/** - Game scenes
* **Assets/Prefabs/** - Reusable game objects
* **Assets/Characters/** - Character assets and management
* **Assets/Campaigns/** - Campaign data and session management
* **Assets/Sprites/** - Game sprites and UI assets
* **Assets/Settings/** - Game configuration files

## Getting Started
1. Open the project in Unity
2. Load a scene from `Assets/Scenes/`
3. Build and run to play with friends

## Platforms to access the full game
1. Navigate to xyz
2. When we properly publish the game we add the details here

## Development
This project is currently being built with the **Agile** Software Development method in which we engage in the following practices:
1. **Scrum** - we organise regular sprints in which we focus our efforts on a particular feature/set of features to acomplish in the iterative cycles of sprints. We frequently update one another in our daily & weekly meetings on developments, noteable changes or errors encountered. This helps us ensure we understand the current state of things and the path ahead
2. **CI/CD** - An important step in our development process in which we regularly commit/merge our code changes/developments to this shared repository to help inform the others processes or development
3. **Test-Driven Development & User Stories** - Eoin is currently managing and tracking this section in which we design tests prior to any code/assets being created. We give ourselves an understanding of the processes, flow of user interaction and absolutely necessary features and then go on to ensure these cases pass or the acceptance criteria of the user stories is met before we proceed or consider a certain feature complete

This form of development is incredibly useful for us as both myself (Eoin) and Cian are long time fans of D&D so we understand the features, design considerations, feasability of features and absolute requirements of this project not only from a developers perspective but also a customers one. In a way, we are the users/customers who are informing the development process and test cases.
