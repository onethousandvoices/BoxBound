# BoxBound

A Unity 6 2D movement prototype built around a rectangular platform where the player can walk across all four faces, jump outward from the current surface and transition between edges automatically without falling off.

## Overview

BoxBound is a compact gameplay prototype focused on surface-relative movement. The scene contains one central rectangular platform, and the player is always attached to one of its faces. Movement is driven by the New Input System, runtime bootstrap creates gameplay prefabs from `Resources`, and player tuning comes from a `ScriptableObject` config loaded at startup.

The character walks along the current face, automatically transfers to the next face when reaching an edge, keeps control during jumps and uses the active face normal as the jump direction. Desktop input uses `A`, `D` and `Space`, while mobile uses runtime-created on-screen buttons under the existing `MainCanvas`.

## Key Features
  * Four-face traversal system with automatic edge transfer
  * Surface-relative jump logic with air control
  * New Input System setup for desktop and mobile
  * Runtime prefab creation from `Resources`
  * `ScriptableObject` player config for speed, jump height and gravity
  * Lightweight DI-style bootstrap container
  * Mobile controls instantiated under the existing `MainCanvas`

## Tech Stack

Technology Role
Unity 6 Engine
URP 2D rendering setup
Unity Input System Cross-platform input
ScriptableObject Runtime player tuning
Resources Runtime prefab and config loading

## Architecture
    SampleScene
        |
        v
    GameBootstrap
        |
        +-- GameContainer
        +-- InputController
        +-- PlayerModel
        +-- PlatformView
        +-- PlayerView

Runtime flow:

  1. `GameBootstrap` loads config and prefabs from `Resources`
  2. `PlatformView` defines the rectangular surface geometry
  3. `InputController` reads `Move` and `Jump` from the Input System asset
  4. `PlayerModel` exposes speed, jump height and gravity from `PlayerConfig`
  5. `PlayerView` handles face movement, jump simulation and edge transfers

## Running

  1. Open `Assets/Scenes/SampleScene.unity`
  2. Enter Play Mode in Unity 6
  3. Use `A` and `D` to move, `Space` to jump
