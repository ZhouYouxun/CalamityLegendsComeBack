# Vesuvius effect notes

## Left-click visual identity

- Borrow Helium Flash's **cadence** only: inward charge, a clear ready beat, one forceful release,
  then a short pressure vent. Do not copy its fireworks, steam-cannon density, or plasma-sun body.
- The material order is **scoria shell first, molten fissures second, bloom last**. A bright circle
  without an opaque rock silhouette does not read as Vesuvius.
- Charging is compression: sparse ash and hot fissure slivers travel inward. Keep the continuously
  visible muzzle stack to one pressure halo, one tight hotspot, and one flattened gasket ring.
- Flight is one fast molten mass. Use a short heat wake plus ash; do not wrap it in a rotating ring.
- High-tier impact is two beats: a white-hot penetration flash, then a low seismic front with rock
  ejecta and rising ash. Reserve the widest effect for the earthquake ring, not for round fire bloom.
- Higher stages increase brightness, debris weight, and rupture reach. They should not multiply the
  number of unrelated particle families.

## Main references

- `AsteroidMolten`
- `AsteroidMolten2`
- `AsteroidMolten3`
- `AsteroidMoltenGlow`
- `AsteroidMoltenGlow2`
- `AsteroidMoltenGlow3`
- Asteroid-style molten `DustID.CopperCoin` dust

## Allowed particles

- `SparkParticle`
- `PointParticle`
- `CritSpark`
- `GlowOrbParticle`
- `ImpactParticle`
- `SquishyLightParticle`

## Allowed smoke

- `HeavySmokeParticle`
- `SmallSmokeParticle`

## Hard rules

- `RancorLavaMetaball` is blacklisted for the entire Vesuvius family and must not be used for flight, impact, charge, lingering lava, or eruption visuals.
- Use structured bloom, heat distortion, smoke, embers, rock fragments, and directional rupture geometry instead.
- `Cinder` visuals are limited to `SmallSmokeParticle` plus dust.
- `ObsidianShard` keeps the void/obsidian read, but its projectile body scale is locked to roughly 33 percent.

## Drawing rules

These are the mistakes that made the whole weapon look wrong. Do not reintroduce them.

- **Particle opacity scales differ per particle class. Check before you pass a number.**
  - `SmallSmokeParticle` and `MediumMistParticle` take opacity on a **0-255** scale. They
    decrement it 2-3 per frame and `Kill()` at zero. Passing `0.5f` makes the particle
    invisible *and* kills it on its first update. Use 90-165, as Calamity does.
  - `HeavySmokeParticle` and `TimedSmokeParticle` take opacity on a **0-1** scale.
- **Never draw a sprite body under `BlendState.Additive` with `A = 0`.** Additive uses
  SourceAlpha as its source factor and adds rather than occludes, so the sprite loses its
  silhouette and turns into a formless glow. Bodies use `AlphaBlend`; only glow, bloom and
  afterimage layers go additive. Calamity's own projectiles draw bodies with `Color.White`
  or `lightColor` under normal blending.
- **Additive black is invisible.** A dark object (obsidian) must be alpha-blended. Only its
  fracture light is additive, and that light has to be an actually bright colour.
- **Glowmasks draw *after* the body**, never before — otherwise the opaque body paints over
  the glow and the weapon never lights up.
- **No global intensity/scale dampeners.** There used to be `VisualIntensity = 0.55f` and
  `VisualScale = 0.72f` multiplied into nearly every effect, plus a hard clamp at 0.8. Tune
  effects individually at the call site.
- **Do not fake an outline by stamping offset copies of the whole sprite.** Several draws
  looped 8-18 rotated copies of the staff or asteroid a few pixels apart; that reads as a
  blurry smear, not a border. Use a bloom behind the sprite, or a real glowmask.
- `EnterShaderRegion` is for binding an actual shader. With none bound, use `SetBlendState`.

## Reference implementations

When in doubt, match these Calamity files rather than inventing a layering scheme:

- `CalamityMod/Projectiles/Magic/AsteroidMolten.cs` — rock body + glowmask
- `CalamityMod/Projectiles/Magic/VolatileStarcore.cs` — two-stage bloom under an opaque core
- `CalamityMod/Projectiles/Magic/HeliumFlashHoldout.cs` — charge-up layering (3 layers, not 11)
- `CalamityMod/Projectiles/Melee/VolcanicFireball.cs` — constant fire tint via `GetAlpha`
- `CalamityMod/Projectiles/Rogue/SubductionFlameburst.cs` — animated sheet, plain white
