# Vesuvius effect notes

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

- `RancorLavaMetaball` is only for hit impact, landing, lingering lava, eruption cores, or other location feedback.
- `RancorLavaMetaball` must not be used as normal projectile flight trail.
- `Cinder` visuals are limited to `SmallSmokeParticle` plus dust.
- `ObsidianShard` keeps the void/obsidian read, but its projectile body scale is locked to roughly 33 percent.
