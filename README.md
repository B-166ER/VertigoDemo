# Vertigo6

Wheel-of-fortune style loot game built in Unity 6 (URP). Spin the wheel, collect rewards, survive bomb slots, and progress through bronze / silver / gold zone tiers.

## Tech stack

- Unity 6000.4.6f1
- Universal Render Pipeline (URP)
- TextMeshPro
- New Input System (UI pointer events)
- ScriptableObjects for item data

## Project layout

```
Assets/
├── Animations/     UI card animator controllers
├── Graphics/       Sprites, atlases, materials
├── Items/          ItemDataSO assets (one per reward type)
├── Prefabs/        UI list elements, flying icon pool
├── Scenes/         SampleScene.unity — main playable scene
├── Scripts/
│   ├── Core/       Game loop, events, save state
│   ├── Wheel/      Spin physics, slots, wheel visuals
│   ├── Items/      Item definitions and lookup
│   ├── Inventory/  Reward processing, piles, bombs
│   ├── Rewards/    Wheel loot tables, reward list UI
│   ├── UI/         Cards, buttons, zone progress bar
│   ├── Rendering/  Custom UI shader helpers
│   └── Debug/      Editor-only wheel fill tools
├── Settings/       URP and volume profiles
└── Shaders/        UI pixel-blend wheel shader
```

## Architecture (short)

- **Singleton managers** wire the loop: `GameManager` → `RewardManager` → `WheelManager` → `InventoryManager`.
- **EventRefrenceManager** is the event bus (spin complete, zone change, card shown, etc.).
- **ItemDataSO** holds icon, amounts, single-use / conversion rules.
- **Inventory** uses polymorphic item classes (`IntentoryItemPile`, consumables, bomb, converted reward).

## How to run

1. Open the project in Unity 6000.4.6f1 (or compatible 6.x).
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press Play — drag or tap spin to rotate the wheel.

## Save data

Zone id and inventory counts persist via `PlayerPrefs` between sessions.
