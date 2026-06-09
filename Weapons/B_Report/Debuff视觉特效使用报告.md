# Visual Effects Report for Heat, Cold, and Sickness Debuffs in Calamity Mod

This report documents the specific visual properties, emission rates, sizes, colors, and motion dynamics of Heat (Burn), Cold (Frostbite/Freeze), and Sickness (Poison/Disease) debuffs when applied to enemies (NPCs) in Calamity Mod. 

*Note: In accordance with requirements, damage formulas and stat changes are excluded. The focus is purely on visual representation (dust, custom particles, lighting, and NPC sprite tinting).*

---

## Technical Overview of Spawning Systems

Calamity Mod implements visual debuff effects inside its global NPC framework, overriding the `DrawEffects` lifecycle hook. Visuals are typically spawned every frame with a specific probability (tuned using `Main.rand.NextBool()` or `Main.rand.Next()`).
Modded visuals are divided into three types:
1.  **Vanilla Dusts**: Spawned via `Dust.NewDustDirect` or `Dust.NewDustPerfect` using standard `DustID` integers.
2.  **Custom Particles**: Spawned via `GeneralParticleHandler.SpawnParticle(Particle particle)` using specialized physics objects (e.g., `CritSpark`, `SparkParticle`, `DirectionalPulseRing`, `GenericBloom`, `SnowflakeSparkle`, `MediumMistParticle`, `VelChangingSpark`, `VoidSparkParticle`).
3.  **Composite Particle Sets**: Objects inheriting from `BaseParticleSet` (like `FireParticleSet`) which manage their own relative particle physics, offsets, and rendering anchors.

---

## 1. Heat (Burn) Debuffs

### Banishing Fire & Holy Flames
*   **Debuff Category**: Heat (Burn)
*   **Visual Elements**:
    *   **Custom Particle**: `CritSpark` (bright yellow-orange solar sparks)
    *   **Dust**: Vanilla `DustID.GemTopaz` (yellow topaz glow dust)
*   **Release Frequency & Probability**:
    *   `CritSpark`: 1/4 (25%) chance per frame.
    *   `DustID.GemTopaz` dust: 1/4 (25%) chance per frame.
*   **Size (Scale) Tuning**:
    *   `CritSpark`: Base scale of `0.8f` (with thickness `2f` and speed `1.9f`, lasting `15` frames).
    *   `DustID.GemTopaz`: Random scale between `[0.7f, 1.2f]`.
*   **Velocity & Motion**:
    *   `CritSpark`: Base vertical velocity is `(0f, -5f)` (25% chance) or `(0f, -9f)` (75% chance), rotated randomly by up to 25 degrees, and multiplied by a random speed factor `[0.1f, 1.9f]`.
    *   `DustID.GemTopaz`: Added to the NPC's current velocity with a random upward drift: `npc.velocity + new Vector2(0f, random[-5f, -1f])`. Gravity is disabled (`noGravity = true`), and opacity is set to semi-transparent (`alpha = 235`).
*   **Positioning**:
    *   `CritSpark`: Spawns at a random location within the NPC's bounding box.
    *   `DustID.GemTopaz`: Spawns in a slightly padded bounding box around the NPC (`npc.position - 2px`, width + 4, height + 4).
*   **Lighting & Tinting**: Adds gold/yellow illumination around the NPC's position with RGB intensity `(0.25f, 0.25f, 0.1f)`.

### Brimstone Flames
*   **Debuff Category**: Heat (Burn)
*   **Visual Elements**:
    *   **Main Dust**: 1/3 (33.3%) chance of `DustID.RuneWizard` (114 - reddish-magenta fire dust) and 2/3 (66.7%) chance of custom `BrimstoneFlame` dust.
    *   **Secondary Dust**: 1/2 chance of `DustID.CopperCoin` (90 - bronze-red sparkles) and 1/2 chance of custom `BrimstoneFlame` dust.
*   **Release Frequency & Probability**: Main trigger has a 1/3 (33.3%) chance per frame. If triggered, it spawns **1** main dust and **3** secondary dusts.
*   **Size (Scale) Tuning**:
    *   Main dust scale is fixed at `1.6f`.
    *   Secondary dust scale is fixed at `1.4f`.
*   **Velocity & Motion**:
    *   Main dust: Moving upward at `(0, random[-5f, -3f])` plus the NPC's current velocity. Gravity is disabled (`noGravity = true`).
    *   Secondary dust: Spreads out and upward at `(random[-4f, 4f], random[-3f, -1f])` plus the NPC's velocity. Gravity is disabled (`noGravity = true`).
*   **Positioning**:
    *   Main dust: Spawns randomly anywhere in the NPC's bounding box.
    *   Secondary dust: Spawns horizontally aligned with the center of the NPC: `npc.position + new Vector2(random[-10f, 10f], npc.height / 2)`.
*   **Lighting & Tinting**: Adds a subtle deep red/magenta light at the NPC's position with RGB values `(0.05f, 0.01f, 0.01f)`.

### Demonic Flames
*   **Debuff Category**: Heat (Burn)
*   **Visual Elements**:
    *   **Dust**: Custom `LightDust` (glowing purple-magenta)
    *   **Custom Particle**: `VelChangingSpark` (velocity-shifting bloom spark)
*   **Release Frequency & Probability**: Spawns with 1/3 (33.3%) chance per frame. Each trigger spawns **1** dust and **2** particles.
*   **Size (Scale) Tuning**:
    *   `LightDust`: Random scale between `[0.8f, 1.2f]`.
    *   `VelChangingSpark`: Dynamic scale factor linked to NPC size: `[0.1f, 0.25f] * MathHelper.Lerp(Math.Max(npc.height, npc.width) / 120, 0.5f, 0.7f)`.
*   **Velocity & Motion**:
    *   `LightDust`: Vertical drift of `(0, random[-8f, -4f])`, rotated randomly up to 0.3 radians, plus the NPC's velocity. Gravity is disabled (`noGravity = true`) and light emission is suppressed (`noLightEmittence = true`).
    *   `VelChangingSpark`: Initial velocity is set to `sparkVel + npc.velocity`, where `sparkVel` is `(random[-width/6, width/6], random[-height/20, -height/17])`. The particle accelerates in the opposite horizontal direction and doubles its vertical speed over a lifetime of `[13, 20]` frames: acceleration vector is `new Vector2(-sparkVel.X * 0.5f, sparkVel.Y * 2) * 3.5f`.
*   **Positioning**:
    *   `LightDust`: Spawns randomly in the NPC bounding box.
    *   `VelChangingSpark`: Spawns offset from the NPC's center: `npc.Center + new Vector2(random[-10f, 10f], npc.height / 2) + sparkVel * 0.5f`.
*   **Color & Lighting**:
    *   Both elements are colored randomly between `Color.MediumOrchid` and `Color.BlueViolet` (with particle alpha set to `0.75f`).
    *   Emits orchid-colored light at `npc.Center` using `Color.MediumOrchid.ToVector3()`.

### Dragonfire
*   **Debuff Category**: Heat (Burn)
*   **Visual Elements**:
    *   **Custom Particle**: `SparkParticle` (orange-red fire spark)
    *   **Smoke Particle**: `SmallSmokeParticle` (thick gray/black smoke clouds)
*   **Release Frequency & Probability**:
    *   `SparkParticle`: Spawns **1** particle **every frame** (100% chance).
    *   `SmallSmokeParticle`: Spawns with 1/3 (33.3%) chance per frame.
*   **Size (Scale) Tuning**:
    *   `SparkParticle`: Random scale between `[0.4f, 0.5f]`.
    *   `SmallSmokeParticle`: Random scale between `[0.2f, 1.2f]`.
*   **Velocity & Motion**:
    *   `SparkParticle`: Base velocity is `(0f, -2f)` (25% chance) or `(0f, -8f)` (75% chance), rotated randomly by up to 10 degrees (33.3% chance) or up to 35 degrees (66.7% chance), multiplied by `[0.1f, 1.9f]`. A horizontal damping factor is applied to counteract the NPC's horizontal velocity: `(sparkVel.X - npc.velocity.X * 0.3f, sparkVel.Y)`. Lifetime is fixed at `10` frames.
    *   `SmallSmokeParticle`: Base velocity is `(0f, -3f)` (50% chance) or `(0f, -14f)` (50% chance), rotated randomly by up to 25 degrees, and multiplied by `[0.1f, 1.9f]`.
*   **Positioning**: Both spawn at a random position inside the NPC's bounding box.
*   **Color & Lighting**:
    *   Sparks are randomly colored `Color.OrangeRed` or `Color.Orange`.
    *   Smoke uses a primary `Color.DimGray` with secondary colors randomly chosen between `Color.Black` and `Color.DimGray`.
    *   Emits a purple-pink light at `npc.position` with RGB values `(0.1f, 0f, 0.135f)`.

### God Slayer Inferno
*   **Debuff Category**: Heat (Burn)
*   **Visual Elements**:
    *   **Custom Particle**: `SparkParticle` (cosmic aqua/magenta spark)
*   **Release Frequency & Probability**: Spawns with 1/2 (50%) chance per frame.
*   **Size (Scale) Tuning**: Random scale between `[0.2f, 0.5f]`.
*   **Velocity & Motion**: Velocity is purely vertical: `(0, random[-5f, 5f])`. Lifetime is randomized between `[11, 13]` frames.
*   **Positioning**: Spawns at a random position inside the NPC's bounding box.
*   **Color & Lighting**:
    *   Color has a 1/7 (14.3%) chance of being `Color.Aqua` (cyan) and a 6/7 (85.7%) chance of being `Color.Fuchsia` (bright magenta/purple).
    *   Emits purple-magenta light at `npc.position` with RGB values `(0.1f, 0f, 0.135f)`.

### Vulnerability Hex & True Vulnerability Hex
*   **Debuff Category**: Heat (Burn)
*   **Visual Elements**:
    *   **Fire Overlay Set**: `FireParticleSet` (a composite flame overlay at the base of the NPC)
    *   **Custom Particle**: `VoidSparkParticle` (dark magenta/black sparkles - *True Vulnerability Hex only*)
*   **Release Frequency & Probability**:
    *   `FireParticleSet` (Both Hexes): Runs continuously. Spawns individual fire particles **every frame** (ParticleSpawnRate = 1).
    *   `VoidSparkParticle` (True Hex Only): Spawns with 1/2 (50%) chance per frame.
*   **Size (Scale) Tuning**:
    *   `FireParticleSet` sizes are calculated dynamically using the NPC's dimensions:
        *   **Horizontal Span (Compactness)**: `npc.width * 0.6f`, clamped to a minimum of `10f`.
        *   **Flame Height & Intensity (Power)**: `npc.height / 100f`, clamped to a maximum of `2.75f`.
    *   `VoidSparkParticle`: Scale is randomized between `[0.02f, 0.05f]`.
*   **Velocity & Motion**:
    *   `FireParticleSet`: Generates individual `FireParticle` units that drift upwards from the bottom of the NPC with a lifetime of `50` ticks.
    *   `VoidSparkParticle`: Extremely fast upward velocity: `-(0, -1)` rotated randomly by up to 18 degrees (`Pi / 10f`), multiplied by `[10f, 18f]`. Lifetime is fixed at `20` frames.
*   **Positioning**:
    *   `FireParticleSet`: Anchored at the bottom center of the NPC sprite: `npc.Bottom - Vector2.UnitY * (12f - npc.gfxOffY)`.
    *   `VoidSparkParticle`: Spawns at random coordinates within the NPC bounding box.
*   **Color & Tinting**:
    *   `FireParticleSet`: Uses a dark red to bright red fire gradient: `Color.Red * 1.25f` primary to `Color.Red` secondary.
    *   `VoidSparkParticle`: Color is randomly chosen between solid `Color.Black` and a reddish-magenta blend: `Color.Lerp(Color.Red, Color.Magenta, 0.35f)`.

---

## 2. Cold (Frostbite) Debuffs

### Glacial State
*   **Debuff Category**: Cold (Freeze)
*   **Visual Elements**: None (no dusts or particles are emitted).
*   **Color Tinting**: Overrides the NPC's sprite rendering draw color entirely to solid **Cyan** (`Color.Cyan`).

### Nightwither
*   **Debuff Category**: Cold (Frostbite)
*   **Visual Elements**:
    *   **Custom Particle**: `CritSpark` (lunar turquoise spark)
    *   **Dust**: Vanilla `DustID.Vortex` (300) (cyan-teal swirling dust) and `DustID.TerraBlade` (323) (light blue trail dust)
*   **Release Frequency & Probability**:
    *   `CritSpark`: Spawns with 1/3 (33.3%) chance per frame.
    *   `Dust`: Spawns **2** dusts **every frame** (100% chance).
*   **Size (Scale) Tuning**:
    *   `CritSpark`: Base scale of `0.8f` (with thickness `2f`, speed `1.9f`, lasting `15` frames).
    *   `Dust`: Scale is fixed at a small `0.5f`.
*   **Velocity & Motion**:
    *   `CritSpark`: Base vertical velocity is `(0f, -5f)` (25% chance) or `(0f, -9f)` (75% chance), rotated randomly by up to 25 degrees, and multiplied by a random speed factor `[0.1f, 1.9f]`.
    *   `Dust`: Added to the NPC's velocity with a rapid upward drift: `npc.velocity + new Vector2(0f, random[-11f, -2f])`. Gravity is disabled (`noGravity = true`), and opacity is set to semi-transparent (`alpha = 235`).
*   **Positioning**:
    *   `CritSpark`: Spawns at a random coordinate inside the NPC bounding box.
    *   `Dust`: Spawns inside a slightly padded bounding box around the NPC (`npc.position - 2px`, width + 4, height + 4).
*   **Color**:
    *   `CritSpark`: Primary color is randomly `Color.Cyan` or `Color.Turquoise`; secondary bloom color is `Color.PaleTurquoise`.
    *   `Dust`: 1/4 (25%) chance of `DustID.Vortex` (300) and 3/4 (75%) chance of `DustID.TerraBlade` (323).

### Voidfrost
*   **Debuff Category**: Cold (Frostbite)
*   **Visual Elements**:
    *   **Custom Particle**: `SnowflakeSparkle` (snowflake-shaped cyan/blue sparkles)
    *   **Mist Particle**: `MediumMistParticle` (pale cyan misty fog clouds)
    *   **Dust**: Vanilla `DustID.Ice` (20) (blue/white ice flakes) or 113 (frost sparkle dust)
*   **Release Frequency & Probability**:
    *   `SnowflakeSparkle`: Spawns with 1/5 (20%) chance per frame.
    *   `MediumMistParticle`: Spawns with 1/40 (2.5%) chance per frame.
    *   `Dust`: Spawns **1** dust **every frame** (100% chance).
*   **Size (Scale) Tuning**:
    *   `SnowflakeSparkle`: Base scale of `0.8f`.
    *   `MediumMistParticle`: Random scale between `[0.5f, 1.5f]`.
    *   `Dust`: Random scale between `[0.3f, 1.0f]`.
*   **Velocity & Motion**:
    *   `SnowflakeSparkle` & `MediumMistParticle`: Base vertical velocity is `(0f, -5f)` (25% chance) or `(0f, -9f)` (75% chance), rotated randomly by up to 25 degrees, and multiplied by `[0.1f, 1.9f]`.
    *   `Dust`: Added to the NPC's velocity with an upward drift: `npc.velocity + new Vector2(0f, random[-11f, -2f])`. Gravity is disabled (`noGravity = true`), and opacity is set to highly opaque (`alpha = 10`).
*   **Positioning**: All elements spawn at random spots inside the NPC's bounding box.
*   **Color**:
    *   `SnowflakeSparkle`: Primary is randomly `Color.Cyan` or `Color.DarkBlue`; secondary is `Color.DodgerBlue`.
    *   `MediumMistParticle`: Primary is pale cyan `new Color(172, 238, 255)`; secondary is muted gray-blue `new Color(145, 170, 188)`.
    *   `Dust`: 1/4 (25%) chance of `DustID.Ice` (20) and 3/4 (75%) chance of 113.

---

## 3. Sickness (Poison / Sickness) Debuffs

### Sulphuric Poisoning
*   **Debuff Category**: Sickness (Poison)
*   **Visual Elements**:
    *   **Dust**: Vanilla `DustID.JungleTorch` (yellowish-green toxic fire dust)
    *   **Custom Particle**: `DirectionalPulseRing` (expanding green/olive shockwave rings)
*   **Release Frequency & Probability**: Main trigger has a 1/2 (50%) chance per frame. When active, it spawns **1** dust and has a 50% chance (overall 25% chance per frame) to spawn **1** pulse ring.
*   **Size (Scale) Tuning**: Both elements scale up dynamically based on the target NPC's size:
    *   `JungleTorch` dust: Scale is calculated as `1.2f + (0.000003f * npc.width * npc.height)`.
    *   `DirectionalPulseRing`: Scale rate is calculated as `0.12f + (0.0000007f * npc.width * npc.height)`.
*   **Velocity & Motion**:
    *   `JungleTorch` dust: Heavily damped horizontal speed and slow upward pull: `(npc.velocity.X * 0.225f, npc.velocity.Y * 0.3f - 1f)`. Gravity is disabled (`noGravity = true`). There is a 1/4 (25%) chance for gravity to be enabled (`noGravity = false`), which multiplies the dust's scale by `0.4f`.
    *   `DirectionalPulseRing`: Drift velocity is `(random[-1f, 1f], random[-4.5f, -6f])` (upward). Lifetime is fixed at `45` frames.
*   **Positioning**:
    *   `JungleTorch` dust: Spawns in slightly padded NPC bounding box (`npc.position - 2px`, width + 4, height + 4).
    *   `DirectionalPulseRing`: Spawns at a random location within the NPC bounding box.
*   **Color**:
    *   `JungleTorch` dust is yellowish-green.
    *   `DirectionalPulseRing` is randomly colored `Color.OliveDrab` (dark olive green) or `Color.GreenYellow` (bright yellow-green).

### Plague
*   **Debuff Category**: Sickness (Poison)
*   **Visual Elements**:
    *   **Custom Particle**: `DirectionalPulseRing` (expanding radioactive green rings)
    *   **Dust**: Vanilla `DustID.ToxicSludge` (89) (acid green acid dust) and `DustID.SporeColony` (220) (green spores)
*   **Release Frequency & Probability**: Spawns with 1/3 (33.3%) chance per frame. Each trigger spawns **1** pulse ring and **4** dusts.
*   **Size (Scale) Tuning**:
    *   `DirectionalPulseRing`: Scale rate scales up with NPC size: `random[0.07f, 0.18f] + (0.0000007f * npc.width * npc.height)`.
    *   `ToxicSludge` dust: Random scale between `[0.3f, 0.4f]`.
    *   `SporeColony` dust: Random scale between `[1.0f, 1.2f]`.
*   **Velocity & Motion**:
    *   `DirectionalPulseRing`: Stationary (`Vector2.Zero`). Lifetime is fixed at `15` frames.
    *   `Dust`: Spawns with standard Terraria random dispersal velocities.
*   **Positioning**: Spawns at random coordinates inside the NPC bounding box.
*   **Color & Lighting**:
    *   `DirectionalPulseRing`: 1/3 chance of `Color.LimeGreen` and 2/3 chance of `Color.Green`.
    *   `Dust`: 1/30 (3.3%) chance of `DustID.SporeColony` (220) and 29/30 (96.7%) chance of `DustID.ToxicSludge` (89).
    *   Emits radioactive green light at `npc.position` with RGB values `(0.07f, 0.15f, 0.01f)`.

### Astral Infection
*   **Debuff Category**: Sickness (Poison)
*   **Visual Elements**:
    *   **Custom Particle**: `DirectionalPulseRing` (starry turquoise/coral ring)
    *   **Custom Particle**: `GenericBloom` (starry turquoise/coral glow)
*   **Release Frequency & Probability**: Spawns with 1/5 (20%) chance per frame. Each trigger spawns **1** ring and **1** bloom particle.
*   **Size (Scale) Tuning**: Both particles scale up with the NPC's dimensions:
    *   `DirectionalPulseRing`: Scale rate is `0.1f + (0.0000007f * npc.width * npc.height)`.
    *   `GenericBloom`: Scale is `0.065f + (0.0000007f * npc.width * npc.height)`.
*   **Velocity & Motion**: Both particles are stationary (`Vector2.Zero`). `DirectionalPulseRing` lasts `20` frames; `GenericBloom` lasts `8` frames.
*   **Positioning**: Spawns at two independent random locations within the NPC's bounding box.
*   **Color**: Both particles are randomly colored either `Color.DarkTurquoise` (astral teal-blue) or `Color.Coral` (astral pink-orange).

### Brain Rot
*   **Debuff Category**: Sickness (Poison)
*   **Visual Elements**:
    *   **Dust**: `DustID.SpookyWood` (184) (ghostly purple-cyan spirit dust) and `DustID.Copper` (18) (reddish copper dust)
*   **Release Frequency & Probability**: Spawns with 1/2 (50%) chance per frame. Each trigger spawns **1** dust.
*   **Size (Scale) Tuning**:
    *   `DustID.Copper` (18) dust scale is fixed at `0.6f`.
    *   `DustID.SpookyWood` (184) dust scale is fixed at `1.2f`.
*   **Velocity & Motion**: Stationary (`Vector2.Zero`). Gravity is disabled (`noGravity = true`).
*   **Positioning**: Spawns at a random position inside the NPC bounding box.
*   **Color & Transparency**: Color matches the dust types (purple-cyan or orange-red). Transparency `alpha` is randomized per dust between `35` and `90` (semi-transparent).

### Burning Blood
*   **Debuff Category**: Sickness (Poison)
*   **Visual Elements**:
    *   **Dust**: Vanilla `DustID.BloodGlow` (296) (glowing dark red blood dust) and `DustID.Blood` (5) (red blood dust)
*   **Release Frequency & Probability**: Spawns with 1/3 (33.3%) chance per frame. Each trigger spawns **1** blood dust.
*   **Size (Scale) Tuning**: Scale is fixed at `1.25f`.
*   **Velocity & Motion**: Added to the NPC's velocity and sprayed slightly upwards: `(npc.velocity.X * 0.52f, npc.velocity.Y * 0.52f - 0.5f)`. Gravity is disabled (`noGravity = true`), and opacity is set to semi-transparent (`alpha = 100`).
*   **Positioning**: Spawns inside a slightly padded bounding box around the NPC (`npc.position - 2px`, width + 4, height + 4).
*   **Color & Lighting**:
    *   Color is blood red: 1/8 (12.5%) chance of glowing blood (296) and 7/8 (87.5%) chance of standard blood (5).
    *   Emits a faint deep red light at `npc.Center` with RGB values `(0.08f, 0f, 0f)`.

### Absorber Affliction
*   **Debuff Category**: Sickness (Poison)
*   **Visual Elements**:
    *   **Custom Particle**: `CustomSpark` (sparkling sea-green particle)
    *   **Dust**: Custom `LightDust` (glowing green/sea-green dust)
*   **Release Frequency & Probability**:
    *   `CustomSpark`: Spawns with 1/3 (33.3%) chance per frame.
    *   `LightDust`: Spawns **1** dust **every frame** (100% chance).
*   **Size (Scale) Tuning**:
    *   `CustomSpark`: Scale is randomized between `[1.5f, 2.0f]`. Lifetime is randomized between `[16, 26]` frames.
    *   `LightDust`: Scale is randomized between `[0.8f, 1.8f]`.
*   **Velocity & Motion**:
    *   `CustomSpark`: Purely vertical speed: `(0, random[-4f, 4f])`.
    *   `LightDust`: Added to the NPC's velocity with a very strong upward pull: `(npc.velocity.X * 0.4f, (npc.velocity.Y * 0.4f - 1.8f) * 2.5f)`. Gravity is disabled (`noGravity = true`).
*   **Positioning**:
    *   `CustomSpark`: Spawns at a random spot inside the NPC bounding box.
    *   `LightDust`: Spawns inside a slightly padded bounding box around the NPC (`npc.position - 2px`, width + 4, height + 4).
*   **Color & Tinting**:
    *   Sprite Color Tinting: Overrides the NPC's sprite rendering draw color to solid **DarkSeaGreen** (`Color.DarkSeaGreen`).
    *   `CustomSpark`: Color is randomly lerped between `Color.DarkSeaGreen` and `Color.MediumSeaGreen`.
    *   `LightDust`: Color has a 1/3 (33.3%) chance of being `Color.PaleGreen` (light green) and a 2/3 (66.7%) chance of being `Color.DarkSeaGreen` (sea green).

### Sage Poison & Whispering Death
*   **Debuff Category**: Sickness (Poison)
*   **Visual Elements**: None (no custom dusts, particles, or lighting are spawned). They display their icons in the modded debuff display list under the boss health bar.
