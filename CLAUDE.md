# CLAUDE.md — Project Context for Claude Code

## Project Overview
This is a first-person tower defence game built in Unity (HDRP). The player physically exists in the game world, builds and maintains turrets with a hammer, defends health buds from enemies, and progresses through 4 zones.

## Unity Version & Render Pipeline
- Unity (latest stable)
- HDRP (High Definition Render Pipeline)
- Input System: Unity's new Input System (PlayerInputActions.inputactions)

## Project Structure
```
Assets/
├── Scripts/
|   ├── Enemy/            — Enemy spawning, pathing, health, drops
|   ├── Game Progression/
|   ├── Hammer/
|   ├── Map/              — Map generation, grid, zones
|   |   └── Managers/
|   ├── NPC/              — NPC base, witch, blacksmith, engineer
│   |   └── Skill Tree/
|   ├── Player/           — Player controller, health, inventory
|   ├── Towers/           — Turret system, addons, projectiles
|   ├── TurretPartItems/  — Blueprint unlocks, turret part unlocks
|   ├── UI/               — All UI scripts
|   ├── Wave/             — Wave manager, wave definitions
├── Prefabs/
├── ScriptableObjects/
└── InputSystem/
```

## Core Architecture

### Map Generation
- `MasterManager` — orchestrates all generation, save/load
- `ZoneGridManager` — per-zone grid (Top, Bottom, Left, Right sides)
- `TargetManager` — central target area (house location)
- `Grid` — grid data structure with `Tile` and `TileType` enum
- `TileType` enum: Buildable, Path, Obstacle, Target, Wall, House, Branch, OccupiedObstacle, Spawner
- `Side` enum: Top, Bottom, Left, Right
- Map saves/loads via JSON using `FullMapData` and `ZoneGridData`
- `TileScale = 10f` (MasterManager.TileScale) — all world positions multiply by this

### Enemy System
- `EnemyDefinition` (ScriptableObject) — stats, prefab variants, drop table, behaviour
- `EnemyPathFollower` — movement along Catmull-Rom spline path, states
- `EnemyHealth` — HP, damage, death, drop spawning
- `EnemyState` enum: Idle, Moving, ReturningToPath, SeekingBud, AttackingBud, SeekingPlayer, AttackingPlayer, Dead
- `EnemyBehaviour` enum: TargetBuds, TargetPlayer
- `EnemyRegistry` — singleton, tracks all alive enemies for turret targeting
- `EnemyPath` — world-space waypoint list built by `EnemyPathBuilder`
- Enemies use Rigidbody for physics separation

### Wave System
- `WaveManager` — drives spawning, weight system, mini boss, charge rewards
- `WaveDefinition` (ScriptableObject) — enemy pool, weight limit, release delay, mini boss
- `ZoneDefinition` (ScriptableObject) — 10 waves per zone, spawner unlock schedule
- `WaveSnapshot` — saves bud state and player HP at wave start for restart
- Weight system: alive enemies have weight, spawning pauses when limit exceeded
- Mini boss drops half charge, two half charges = obstacle remover
- `SpawnerManager` — instantiates EnemySpawner objects
- `EnemySpawner` — physical spawner, Activate/Deactivate API
- `BranchObstacleManager` — manages branch path blockers
- `BranchObstacle` — physical blocker, removed with obstacle remover

### Player System
- `PlayerController` — first person movement, CharacterController
  - Static flags: `LookLocked`, `InputLocked`
  - Public fields: `meleeDamage`, `hammerRange`, `swingDuration`, `altAttackDamage`, `altAttackRange`
- `PlayerHealth` — HP tied to health buds, zone-based max HP
  - `ResetForZone(Side zone, int totalBudCount)`
  - `OnBudDestroyed(Side zone)`
  - `hpPerBud = 100f`, calculates `maxHP = totalBudCount/4 * hpPerBud`
- `PlayerInventory` — singleton, Dictionary<ItemDefinition, int>, gold int
  - `AddItem`, `RemoveItem`, `GetCount`, `HasItem`
  - `AddGold`, `SpendGold`, `HasGold`
  - `BlueprintProgress blueprintProgress`

### Health Bud System
- `HealthBud` — individual bud, HP, destroyed state, zone assignment
  - `public Side zone` — assigned during MapVisualizer.VisualiseTarget
- `HealthBudManager` — tracks all buds, active zone filter
  - `SetActiveZone(Side zone)` — only counts/detects buds for active zone
  - `OnAllBudsDestroyed` event → triggers game over
  - `ClaimAttackPosition` / `ReleaseAttackPosition` — enemy attack coordination
- `HealthBarUI` — slider UI, green→red color gradient

### Tower System
- `TurretDefinition` (ScriptableObject) — base stats, cylinder count, rotation speed
- `TurretBase` — state machine (TurretBuildState), HP, cylinders
  - `TurretBuildState`: Empty, UnderConstruction, Built, Damaged, Destroyed
- `TurretBuildMinigame` — phase-based build system with nail hit points
  - `BuildPhase` — phase object, nail points list, optional prefab override
- `TurretCylinder` — rotates toward target, queries EnemyRegistry
  - Uses `TargetingPriority` enum: Closest, FirstInLine, HighestHP, LowestHP
- `TurretJoint` — addon attachment point, highlight states (JointHighlightState)
- `TurretAddon` — weapon module, fire logic, upgrade tiers
- `TurretProjectile` — physical projectile with target leading (Catmull-Rom intercept)
- `AddonDefinition` (ScriptableObject) — range, damage, fire rate, projectile speed
- `AddonUpgradeTier` — per-tier stat multipliers
- `TowerManager` — placement, phase gating, tracks occupied tiles
- `BuildableTile` — component on buildable tile prefabs, highlight, occupied state
- `BuildableTileDetector` — raycast from camera, build mode (B key)
- `AddonCarrySystem` — player carries addons, hold point in front of camera
- `AddonInteractionDetector` — raycast for joint/addon interaction (E key)
- `HitPoint` — nail component, IHammerHittable, drive animation
- `IHammerHittable` interface — OnHammerHit(float hammerStrength)
- `IInteractable` interface — OnInteract(), InteractionPrompt string

### Hammer System
- Left mouse button — swing attack
- Right mouse button — alt attack (hitscan, high damage for testing)
- Hammer swing uses peak-window raycast (not OverlapSphere)
- `HammerUpgradeManager` — singleton, manages 6 stats
- `HammerUpgradeStat` enum: Damage, Reach, SwingSpeed, TurretRepair, Knockback, AbilityCooldown
- `HammerUpgradeDefinition` (ScriptableObject) — per-stat upgrade data

### Skill Tree System (Blacksmith)
- `SkillTreeWorld` — singleton, manages 3D skill tree in hidden area (0, 0, -1800)
  - Renders via dedicated orthographic camera → RenderTexture → RawImage
  - A/D rotation, scroll zoom
- `SkillTreeBranch` — Bezier spline branch, LineRenderer, 5 milestone nodes
  - Control points: P0 (trunk), P1, P2, P3 (tip)
  - Milestones at t = 0.2, 0.4, 0.6, 0.8, 1.0
  - Animated grow via coroutine
- `MilestoneNode` — clickable upgrade node, hover highlight
- `ZoneSkillTreeDefinition` (ScriptableObject) — per-zone tree visual config

### NPC System
- `NPCBase` — MonoBehaviour, waypoint-based movement
  - `outpostWaypoints`, `houseWaypoints` (List<Transform>)
  - `WalkToHouse()`, `WalkToOutpost()`
- `NPCState` enum: Idle, Walking, InHouse
- `WitchNPC : NPCBase, IInteractable` — opens WitchShopUI
- `BlacksmithNPC : NPCBase, IInteractable` — opens BlacksmithShopUI
- NPCs walk to house on wave start, return to outpost on wave end

### Shop System
- `ShopUIBase` — base class, show/hide, cursor lock, ESC to close
- `WitchShopUI : ShopUIBase` — sell items for gold, tab system
- `BlacksmithShopUI : ShopUIBase` — skill tree display, gold display
- `ItemEntryUI` — one row per item type, 1x/5x/10x sell buttons
- All shops use direct references (no singleton except BlacksmithShopUI)

### Item System
- `ItemDefinition` (ScriptableObject) — name, rarity, icon, prefab, goldValue, pickupRadius, lifetimeDuration, autoPickupFee
- `ItemRarity` enum: Common, Uncommon, Rare, Legendary
- `WorldDrop` — physics-based pickup, auto-pickup on proximity, lifetime timer with fee
- `DropTable` / `DropEntry` — per-enemy drop configuration
- `PlayerInventory` handles all item storage

### Game Progression
- `GameProgressionManager` — singleton, zone completion, transitions
- `ZoneDoor` — animated door (single mesh, rotation), ZoneDoorState enum
- `ZoneDoorState` enum: Locked, Closed, Open, Complete
- `BedInteractable : IInteractable` — starts wave OR triggers zone transition
- `GameOverUI : ShopUIBase` — shown when all buds destroyed, restart/quit

## Key Patterns & Conventions

### Singleton Pattern
Used for: `PlayerInventory`, `PlayerHealth`, `EnemyRegistry`, `HammerUpgradeManager`, `SkillTreeWorld`, `GameProgressionManager`, `BlacksmithShopUI`, `WaveManager` (via direct reference)

### ScriptableObject Pattern
All definitions use ScriptableObjects:
- `EnemyDefinition`, `WaveDefinition`, `ZoneDefinition`
- `AddonDefinition`, `TurretDefinition`, `HammerUpgradeDefinition`
- `ItemDefinition`, `ZoneSkillTreeDefinition`
- MenuName format: `"Category/Name Definition"`

### Event Pattern
- C# `event Action` for loose coupling
- `OnDeath`, `OnBudDestroyed`, `OnAllBudsDestroyed`, `OnMinigameComplete`
- Always unsubscribe in handler or OnDestroy

### Layer System
Important layers:
- `BuildableTile` — buildable tile prefabs
- `Hittable` — enemies, hit points (hammer detection)
- `Interactable` — NPCs, interactable objects
- `SkillTree` — skill tree world objects
- `Player` — player GameObject
- `Pickup` — world drop items

### TileScale
ALL world position calculations: `position = (gridPos + 0.5f) * MasterManager.TileScale`
Y position for spawners/enemies: `y = 1f`

## Common Issues & Solutions

### Input System
- Movement uses `ReadValue<Vector2>()` in Update (not events) — fixes diagonal movement
- `PlayerController.LookLocked` and `PlayerController.InputLocked` — static flags
- All shops set both flags on Show(), clear on Hide()

### Physics
- Enemies use Rigidbody with `Continuous Dynamic` collision detection
- `rb.MovePosition()` for movement (not transform.position)
- OverlapSphere for hammer hits (Hittable layer)
- Raycast from camera center for precise interactions

### UI
- Multiple Canvas setup — each major UI on its own Canvas
- All shops inherit ShopUIBase
- Cursor unlocked when any shop open
- HDRP emission: use `_EmissiveColor` and `HDMaterial.ValidateMaterial`

### Save/Load
- Full map saved as JSON via `JsonUtility`
- `ZoneGridData` contains: tiles, obstacles, spawners, paths, branch merge tiles
- Always regenerate and resave after adding new persistent data fields

## Work In Progress / TODO
- Engineer NPC and shop (blueprint unlock system)
- Multiple turret addons (catapult, mortar)
- Repair hammer minigame
- Lever — targeting priority switching on turret
- Player death — eagle grab, teleport to house, HP regen
- Trap system (barbed wire, poison sludge)
- Zone materials — physical collectibles (E key pickup)
- Zone 2-4 content
- Wall destruction for final round
- GameProgressionManager full implementation
- Models and animations (all current visuals are primitives)
- Audio and VFX
- New Game+

## Alpha Test Scope
- Zone 1 only (5 waves)
- Wave 5 = final boss
- Bed interaction after final boss → end screen
- All core systems functional with primitive visuals
