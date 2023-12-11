# Sack a Vole

Sack-a-Vole is like whack-a-mole, but with a twist! Holes spawn in the room and players need to keep an eye on their space to pluck the voles as they leap out of their hole. If they're successful, the vole goes in the sack! The player with the most voles at the end of the round wins!

A multiplayer, shared AR game that utilizes the [Lightship ARDK](https://lightship.dev/docs/ardk/) and Unity's Netcode for GameObjects to demonstrates a number of features below.

## How to Play

### Setup a lobby

-   Print out the included target image, and place it in the space you want to play.
-   Start the app as a Host by clicking the `Host Game` button.
-   You'll be prompted to scan for a target image, make sure you don't move the image once it has been scanned (If the image gets offset, you can always rescan the image to reset the position). A blue overlay will appear over the image when it has been scanned correctly.
-   The game will now prompt you to `Scan the floor`, this will generate a mesh surface to play on.
-   Once enough of the room has been scanned, you'll see a button to `Start Sacking`.
-   If any other players want to join, they will also need to scan the target image, and scan the room.
-   As soon as any player presses the start button, the game will count down and begin.

### Gameplay

-   Voles will begin spawning into your room
-   Tap on a vole to 'sack' it, and see your points go up at the bottom of the screen.
-   At the end of the 30 second round, whoever has the highest number of sacked voles wins.

## Features

### Meshing

When joining the lobby each player must scan the room to generate a mesh.
If the player is the host, this mesh is used as both the NavMesh for moving Voles around the scene, as well as a mesh to occlude any geometry that is behind a real-world object.
If the player is a client, this mesh is only used to occlude other geometry.

### NavMesh / NavAgents

All voles are setup as NavAgents on the host. Periodically, voles are given a new randomized target position on the NavMesh as their destination. Their position is synced across the network with the host keeping authority over their desination and current position.

### Shared AR

This project utilizes Unity's Netcode for GameObjects to sync multiple players.
Each player that joins is given a player avatar (represented by a sack), and authority over the avatar's position.
The avatar will follow the player's phone position as they move around the scene.
Voles are controlled by the host and use Shared Space Origin to keep everything in sync with the real world.

### Image based colocalization

A target image is included in the project that is used to colocalize multiple players in AR space, and within the real world.
As part of joining a lobby, players will need to scan for this image and set a shared origin.

### Pin to join rooms

When a host creates a new game, a pin is generated randomly. Other players can use this number to join the specific lobby/room.

## Build and Run

### Add your Lightship API key

In order to run the project you'll need to add your LightShip API key.

-   Sign up at [the lightship dev website] (https://lightship.dev/),
-   Create a new project
-   Copy the generated API key
-   In unity, select the menu `Lightship -> Settings` and paste your key into the `API Key` field
-

### Sign and build for iOS

You'll need to add a signing identity if you want to build for iOS.
This can be done in Unity's project settings, or within Xcode.

-   Build the game from unity to an Xcode project
-   Build the game in Xcode to a test device
