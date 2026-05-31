\# Five Elements — Unity Script Reference



\## File List



| File | Location | Purpose |

|---|---|---|

| `ElementType.cs` | Scripts/Elements/ | Enums, ElementData, InteractionResult |

| `ElementRegistry.cs` | Scripts/Elements/ | ScriptableObject holding all ElementData |

| `ElementManager.cs` | Scripts/Elements/ | Singleton — tracks absorbed elements \& active element |

| `ElementInteraction.cs` | Scripts/Elements/ | Generating/Overcoming cycles, combo logic |

| `AbilityBase.cs` | Scripts/Abilities/ | Abstract base for all 5 abilities |

| `ElementAbilities.cs` | Scripts/Abilities/ | Water, Wood, Fire, Earth, Metal abilities |

| `PlayerController.cs` | Scripts/Player/ | Movement, jump, energy, damage, ability dispatch |

| `PuzzleObject.cs` | Scripts/Puzzle/ | Base puzzle class + ElementalDoor, PressurePlate, WaterBasin |

| `PuzzleRoom.cs` | Scripts/Puzzle/ | Tracks a set of PuzzleObjects; fires solved event |

| `ElementShrine.cs` | Scripts/World/ | In-world shrine player interacts with to absorb elements |

| `LevelProgressionManager.cs` | Scripts/Managers/ | Scene loading, checkpoints, respawn, save/load |

| `GameManager.cs` | Scripts/Managers/ | Top-level state machine (MainMenu/Playing/Paused/GameOver) |

| `AudioManager.cs` | Scripts/Managers/ | Music crossfade + SFX pool + volume persistence |

| `CameraController.cs` | Scripts/Camera/ | Smooth follow + look-ahead + bounds + zoom |

| `CameraShaker.cs` | Scripts/Camera/ | Screen shake (called by EarthAbility) |

| `ElementVignetteController.cs` | Scripts/Camera/ | Tints screen vignette to active element colour |

| `EnemyBase.cs` | Scripts/Enemy/ | Patrol/Chase/Attack FSM, health, stun, burn |

| `ElementalEnemy.cs` | Scripts/Enemy/ | Elemental weakness/resistance, freeze, entangle |

| `GuardianBoss.cs` | Scripts/Enemy/ | Multi-phase elemental boss |

| `HUD.cs` | Scripts/UI/ | ElementHUD, HealthBar, EnergyBar, CooldownWheel, ComboFeedbackUI |

| `ObjectPool.cs` | Scripts/Utility/ | Generic object pool for projectiles \& VFX |



\---



\## Scene Setup (Minimal Playable)



\### 1 — Persistent Managers (one scene, DontDestroyOnLoad)

Create an empty GameObject called `\_Managers` and attach:

\- `GameManager`

\- `ElementManager` (assign `ElementRegistry` ScriptableObject)

\- `LevelProgressionManager`

\- `AudioManager` (add two AudioSource children for music A/B)

\- `ObjectPool` (pre-warm pool definitions)



\### 2 — Player GameObject

\- `Rigidbody2D` (Gravity Scale 3, Freeze Rotation Z)

\- `CapsuleCollider2D`

\- `SpriteRenderer`

\- `Animator`

\- `PlayerController`

\- One of each ability: `WaterAbility`, `WoodAbility`, `FireAbility`, `EarthAbility`, `MetalAbility`

\- `AudioSource` (for ability sounds)

\- Child GameObject `GroundCheck` positioned at feet — assign to PlayerController



\### 3 — Camera

\- Add `CameraController` and `CameraShaker` to Main Camera

\- Create a UI Canvas → stretch an `Image` over it for vignette → assign to `ElementVignetteController` (also on Main Camera)



\### 4 — ElementRegistry ScriptableObject

\- `Assets → Create → FiveElements → Element Registry`

\- Fill in one `ElementData` entry per element (icon, colors, sounds, VFX prefabs)



\### 5 — HUD Canvas

\- `ElementHUD` — 5 slot Images in a horizontal group

\- `HealthBar` — two Images (fill + ghost fill, both Filled type)

\- `EnergyBar` — one Image (Filled type)

\- `CooldownWheel` — one Image (Filled type, Radial 360)

\- `ComboFeedbackUI` — CanvasGroup with two TMP text fields



\### 6 — Puzzle Room

\- Create empty GameObject `PuzzleRoom\_01`, attach `PuzzleRoom`

\- Add child GameObjects with `ElementalDoor`, `PressurePlate`, or `WaterBasin`

\- Assign `requiredRoom` in `ElementShrine` to gate the element unlock



\---



\## Input Summary



| Action | Key |

|---|---|

| Move | A / D or Arrow Keys |

| Jump | Space |

| Use Ability (primary) | Left Mouse / Fire1 |

| Use Ability (secondary) | Right Mouse / Fire2 |

| Cycle element forward | E / Scroll Up |

| Cycle element backward | Q / Scroll Down |

| Select element directly | 1 – 5 |

| Interact (shrine etc.) | F |

| Pause | Escape |



\---



\## Extending the System



\### Add a new ability

1\. Create a class inheriting `AbilityBase`

2\. Override `Execute(PlayerController player)`

3\. Attach to the Player GameObject

4\. Set `elementType` in the inspector



\### Add a new puzzle object

1\. Create a class inheriting `PuzzleObject`

2\. Override `OnActivate(ElementType element)` and optionally `HandleCombo()`

3\. Set `requiredElements` in the inspector to define which elements trigger it



\### Add a new enemy type

1\. Create a class inheriting `EnemyBase` or `ElementalEnemy`

2\. Override `HandleAttack()` for custom attack patterns

3\. Set `enemyElement` to define its elemental weakness

