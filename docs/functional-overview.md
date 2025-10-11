# Functional Overview

This document provides a catalog of the current gameplay, economy, UI, and tooling features in the project. Each section lists the owning scripts/assets and outlines how to extend or configure the system.

## Core Framework
- **Game bootstrap** (`Scripts/Core/BootLoader.cs`, `Scripts/Core/SceneLoader.cs`): bootstraps persistent managers, loads the hub/battle scenes, and routes high-level flow. Configure scene names on the `BootLoader` component in the hub scene to change the startup path.
- **Game state** (`Scripts/Core/GameManager.cs`): holds the active `PlayerProfile`, fires profile change events, and keeps a cached copy across scene loads. Use `GameManager.CurrentProfile` or subscribe to `GameManager.OnProfileChanged` for systems that need to react to profile swaps.
- **Pooling** (`Scripts/Core/Pool.cs`): generic pooling utility for bullets/enemies/FX. Instantiate a `Pool` component, assign a prefab, and call `Spawn()`/`Return()` to reduce allocation churn.

## Player Systems
- **Profile & persistence** (`Scripts/Player/PlayerProfile.cs`): serializable save data with inventory stacks, gold, XP, talents, and equipped items. Use `PlayerProfile.Save` after mutating stats or inventory to persist changes.
- **Class library** (`Scripts/Player/CharacterClass.cs`, `Scripts/Player/CharacterClassRegistry.cs`): ScriptableObject definitions for each class and a registry to fetch them by ID. Extend by creating new class assets and registering them in the registry.
- **Avatar customization** (`Scripts/Player/PlayerAvatarSettings.cs`, `Scripts/Player/PlayerAvatarBuilder.cs`): configures mesh/material variations per class. Invoke `PlayerAvatarBuilder.Apply` to rebuild visuals when the player changes loadout.
- **Stats & derived combat values** (`Scripts/Player/PlayerStatService.cs`, `Scripts/Player/PlayerStats.cs`): aggregates STR/DEX/INT from class, equipment, and talents to compute attack cadence, HP, movement, and bonus hooks for items. Subscribe to `PlayerStatService.OnStatsChanged` to update dependent systems.
- **Control & health** (`Scripts/Player/PlayerController.cs`, `Scripts/Player/PlayerHealth.cs`, `Scripts/Player/PlayerAnimationDriver.cs`): manages movement, ability usage, death, and animation events. Adjust input responsiveness in `PlayerController` and death handling in `PlayerHealth`.
- **Camera** (`Scripts/Player/CameraFollow.cs`): smooth follow camera with centering, collision handling, and inspector-tunable offsets/lag limits. Use the Reset function in the inspector to auto-bind the player target.

## Combat & Abilities
- **Attack execution** (`Scripts/Combat/SimpleAttack.cs`): drives attack timing, hit detection, and projectile spawning using player stats for cadence and damage scaling. Extend by adding new ability behaviours or altering damage formulas.
- **Health system** (`Scripts/Combat/Health.cs`): shared component for entities with hit points, exposing events for damage and death. Hook into `Health.OnDeath` to trigger loot drops or UI updates.

## Enemy & AI Systems
- **Enemy behaviours** (`Scripts/Enemies/*.cs`): base class plus runner/tank/shooter variants with unique movement and attack logic. Tune per-enemy parameters in their respective ScriptableObject `EnemyDefinition` assets.
- **Wave orchestration** (`Scripts/AI/WaveSpawner.cs`, `Scripts/AI/WaveStageSet.cs`, `Scripts/AI/WaveTable.cs`): builds stage plans from `WaveStageSet` assets, schedules normal waves, support groups, and bosses with overflow controls. Assign the desired `WaveStageSet` on `WaveSpawner` to configure stage progression.
- **Loot drops** (`Scripts/AI/EnemyLoot.cs`): resolves loot tables when enemies die, pulling from drop tables and economy modifiers.

## Economy & Loot
- **Balance sheet** (`Scripts/Economy/EconomyBalance.cs`, asset `Resources/Balance/EconomyBalance.asset`): ScriptableObject holding vendor markups, salvage values, rarity multipliers, and currency formatting. Adjust values in the asset or through the `EconomyBalanceWindow` editor.
- **Drop tables** (`Scripts/Economy/DropTable.cs`, assets in `Resources/Balance/*DropTable.asset`): weighted loot entries with rarity tiers and minimum guarantees. Edit via `DropTableEditorWindow` or duplicate the assets for new enemy groups.
- **Vendor logic** (`Scripts/Economy/Vendor.cs`, `Scripts/UI/VendorUI.cs`, `Scripts/UI/VendorPanelBoot.cs`): builds shop inventories from drop tables and enforces pricing rules. Use `Vendor.GenerateInventory` when refreshing shops and subscribe to inventory events for UI updates.
- **Currency helpers** (`Scripts/Economy/Currency.cs`): defines denominations and formatting utilities for gold display.

## Inventory & Equipment
- **Item catalog** (`Scripts/Item/ItemDefinition.cs`, `Scripts/Item/ItemDB.cs`, assets under `Resources/Items`): defines stack limits, equipment slots, stat bonuses, and rarity. Add items by creating new `ItemDefinition` assets and registering them in `ItemDB`.
- **Inventory data service** (`Scripts/Item/InventoryService.cs`): event-driven API for adding/removing items, handling stack merges, equipment slots, and capacity changes. Use `InventoryService.TryAdd/TryRemove` to mutate stacks and listen to `OnSlotChanged` for partial UI refreshes.
- **Stacking algorithms** (`Scripts/Item/InventoryAlgorithms.cs`): pure utility implementing stack merging, metadata-aware matching, and capacity enforcement. Covered by edit-mode tests for regression safety.
- **UI bindings** (`Scripts/UI/InventoryUI.cs`, `Scripts/UI/InventoryUIV2.cs`): renders slot grids, previews, and equips; responds to inventory events to update slots efficiently.

## Talents & Progression
- **Talent definitions** (`Scripts/Talents/TalentDefinitions.cs`, assets under `Resources/Talents/Expanded`): описывает дерево из 100 узлов, их требования и бонусы. Подробности о генерации из CSV — в `docs/talent-generation.md`.
- **Talent service** (`Scripts/Talents/TalentService.cs`): evaluates prerequisites, grants/refunds talent points, and applies stat modifiers via the stat service.
- **Progress tracking** (`Scripts/Progress/ProgressService.cs`, `Scripts/UI/ProgressUI.cs`): manages level XP thresholds and updates the UI when players advance.

## User Interface
- **Battle HUD** (`Scripts/UI/BattleHUD.cs`): displays health, cooldowns, and wave status during combat; hooks into `WaveSpawner` and player stats.
- **Hub UI** (`Scripts/UI/HubUI.cs`): orchestrates vendor, inventory, and talent panels in the hub scene.
- **Character creation** (`Scripts/UI/CharacterCreationUI.cs`): handles new profile setup, class selection, and seed generation.
- **Damage numbers** (`Scripts/UI/DamageNumbers.cs`): spawns floating numbers on hits with pooling to minimize GC.
- **Talents UI** (`Scripts/UI/TalentsUI.cs`): renders the talent tree, responds to talent service events, and shows requirements.

## World Generation
- **Map generation** (`Scripts/World/MapGeneratorSimple.cs`): builds simple tile layouts for combat arenas. Extend by swapping in new generator implementations for additional level variety.

## Utilities & Shared Infrastructure
- **JSON persistence** (`Scripts/Util/JsonUtil.cs`): wrapper around `PlayerPrefs` for serializing arbitrary objects.
- **Wave & economy documentation** (`docs/wave-stages.md`, `docs/balance-tools.md`): provides designer-facing guides for configuring stage sets and balance data.

## Editor Tooling
- **Economy editor** (`Editor/EconomyBalanceWindow.cs`): custom window for adjusting `EconomyBalance` without opening the raw asset.
- **Drop table editor** (`Editor/DropTableEditorWindow.cs`): inspector utility to tweak drop weights, guarantees, and rarity thresholds with CSV import/export support.

## Assets & Configuration
- **Wave presets** (`Resources/Waves/SampleStageSet.asset`): sample stage layout with six-wave base and boss support definitions.
- **Enemy definitions** (`Resources/Enemies/*.asset`): configure HP, damage, loot tables, and stat scaling per enemy archetype.
- **Balance data** (`Resources/Balance/*.asset`): default drop tables and economy multipliers consumed at runtime.

## Automated Tests
- **Inventory stacking** (`Tests/EditMode/InventoryAlgorithmsTests.cs`): verifies metadata-aware stacking, capacity growth, and removal flows.
- **Profile migrations** (`Tests/EditMode/PlayerProfileTests.cs`): ensures new profile fields migrate correctly from legacy saves.

## How to Extend Safely
1. **Add new data assets**: duplicate existing ScriptableObjects (items, drop tables, stage sets) and reference them in the appropriate services (`ItemDB`, `WaveSpawner`, `EconomyBalance`).
2. **Update gameplay code**: leverage service APIs (`InventoryService`, `PlayerStatService`, `TalentService`) to avoid direct profile manipulation and preserve save compatibility.
3. **Document designer workflows**: update `docs/balance-tools.md` or `docs/wave-stages.md` when introducing new data pipelines or editor tooling.
4. **Protect changes with tests**: add edit-mode tests under `Tests/EditMode` mirroring the patterns in the existing inventory/profile suites.

---

# Roadmap

## Stabilization
- Expand automated tests to cover wave spawning plans, enemy loot resolution, and economy price calculations to prevent regressions when tuning data.
- Introduce validation scripts that scan `Resources/Balance` and `Resources/Enemies` for missing references or inconsistent rarity tiers before builds.

## Gameplay & Content
- Build additional `WaveStageSet` assets for later stages, including unique boss support compositions and environment modifiers.
- Flesh out more `ItemDefinition` entries with differentiated stat bonuses to take advantage of the STR/DEX/INT scaling service.
- Implement talent respec costs and UI feedback so players can reallocate points without editing saves.

## UX & Tooling
- Add in-game previews for drop table odds and economy multipliers within the hub UI to aid live balance reviews.
- Extend the inventory UI with drag-and-drop and context menus for quicker item management.
- Provide CSV export/import for `EconomyBalance` to align with external balancing spreadsheets.

## Technical Enhancements
- Profile performance hot spots (wave spawning, pooling) and consider jobified/enumerable patterns for large battles.
- Add runtime diagnostics (e.g., in-editor overlays) to visualize camera collision bounds and stat contributions per source.
- Implement cloud-save hooks or platform persistence wrappers to move beyond `PlayerPrefs` for production builds.
