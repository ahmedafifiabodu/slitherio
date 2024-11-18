# Project Demo

## Overview

Project Demo is a multiplayer game developed using Unity and Netcode for GameObjects. The gameplay is inspired by the popular game Slither.io, where players control a character that grows in length by consuming other players or items in the game world.

## Features

- **Multiplayer Gameplay**: Engage in real-time multiplayer matches with other players.
- **Player Growth**: Increase your character's length by consuming other players or items.
- **Collision Detection**: Determine the winner in player collisions based on length.
- **Smooth Movement**: Control your character with smooth and responsive movement mechanics.

## Technologies Used

- **Unity**: The game engine used for development.
- **Netcode for GameObjects**: A networking library for Unity to handle multiplayer functionality.
- **Input System**: Unity's new input system for handling player inputs.

## Getting Started

### Prerequisites

- Unity 2021.3 or later
- .NET Framework 4.7.1

### Installation

1. Clone the repository:

```
    git clone https://github.com/yourusername/project-demo.git
```

2. Open the project in Unity.

### Running the Game

1. Open the `Assets/Project Demo/Scenes/MainScene.unity` scene.
2. Press the Play button in the Unity Editor to start the game.

## Project Structure

- `Assets/Project Demo/Scripts/Player/PlayerController.cs`: Main script for player control and networking.
- `Assets/Project Demo/Scripts/Player/PlayerLength.cs`: Script for managing player length.
- `Assets/Project Demo/Scripts/Services/ServiceLocator.cs`: Service locator pattern implementation for managing game services.
- `Assets/Project Demo/Scripts/Input/InputManager.cs`: Manages player input using the new input system.

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request for any improvements or bug fixes.

## License

This project is licensed under the MIT License. See the `LICENSE` file for more details.

## Acknowledgements

- Unity Technologies for the game engine and tools.
- Netcode for GameObjects for providing the networking framework.