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

- `RancorLavaMetaball` is blacklisted for the entire Vesuvius family and must not be used for flight, impact, charge, lingering lava, or eruption visuals.
- Use structured bloom, heat distortion, smoke, embers, rock fragments, and directional rupture geometry instead.
- `Cinder` visuals are limited to `SmallSmokeParticle` plus dust.
- `ObsidianShard` keeps the void/obsidian read, but its projectile body scale is locked to roughly 33 percent.
