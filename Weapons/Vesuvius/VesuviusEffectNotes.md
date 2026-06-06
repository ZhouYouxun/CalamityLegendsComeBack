# Vesuvius effect notes

## 主力素材

- `AsteroidMolten` / `AsteroidMolten2` / `AsteroidMolten3`
- `AsteroidMoltenGlow` / `AsteroidMoltenGlow2` / `AsteroidMoltenGlow3`
- `DustID.CopperCoin` style molten asteroid dust

## 允许的粒子

- `SparkParticle`
- `PointParticle`
- `CritSpark`
- `GlowOrbParticle`
- `ImpactParticle`
- `SquishyLightParticle`

## 允许的烟雾

- `HeavySmokeParticle`
- `SmallSmokeParticle`

## 使用约束

- `RancorLavaMetaball` 只能用于命中、落点、残留岩浆、喷发核心等位置反馈。
- `RancorLavaMetaball` 不作为飞行尾迹或普通飞行过程特效。
- `SmallSmokeParticle + Dust` 是 `Cinder` 追踪灰烬的全部视觉构成。
