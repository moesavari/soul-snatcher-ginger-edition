# SoulSnatcher: Ginger Edition 🔥🧡

A 2D top-down soul-harvesting action-defense game where a cursed ginger defends villages by day and drains souls by night.

---

## 🔥 Core Features

* Ginger-based **soul magic system** tied to hair growth stages
* **Reputation + fear AI**: villagers react to your morality
* **Hair meter = magic meter** (visual & mechanical)
* **Nighttime zombie sieges** with escalating waves
* **Dynamic AudioCue system**: event-driven stingers, music, spawn/death sounds
* Choose your victims, grow your power, embrace the curse
* **Inventory ↔ Equipment System**:
  - Drag/drop or right-click to equip/unequip
  - Equipment Sheet mirrors inventory stats
  - Tooltips + context menus on both bag and equipped slots

---

## 🛠 Road So Far

* Initial player characters (male/female, hair growth stages)
* Implemented **Soul System** (villager siphoning fuels magic, reputation shifts)
* Neutral combat (melee + ranged bow)
* Integrated **dynamic AudioManager + AudioCues** (spawn/death, night start, music)
* Villagers & zombies spawn with colliders + health systems
* **WaveManager + NightPreset**: multi-type, multi-wave night system (event-driven on NightStarted)
* Zombies fixed with **SpawnZFixer** (correct Z placement)
* HUD binds dynamically to player (health, souls, hair stage, wave text)
* **DeathRelay** system for alive count tracking (pool-safe)
* Projectiles fire toward mouse with corrected art + alignment
* **Inventory & Equipment integrated**:
  - Items equip from bag → slots update
  - Slots interactive (hover tooltip, context menu, right-click unequip)
  - No duplication on unequip
* **Debug improvements**:
  - Context-rich DebugManager logging
  - Equipment debug inspector & runtime overlay

* Git repo structured for Unity 6000+ workflow

---

## 🔮 Next Steps

* Animate player, villagers, zombies
* Flesh out villager roles (blacksmith, trader, healer, guard)
* Expand **reputation/fear interactions** with villager AI
* Procedural night raid escalation & difficulty curve
* Soul-locked dungeon event (triggered at reputation extremes)
* Per-wave HUD updates + audio stingers
* Polish music transitions and looping ambience

---

*Built with Unity (6000.2.0f1)*
