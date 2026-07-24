using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.DStage3
{
    // The left-click charge payoff: a fast, straight volcanic core with no homing or ballistic drift.
    public class VesuviusThermalCore : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/Magic/VolatileStarcore";

        private int Stage => (int)MathHelper.Clamp(Projectile.ai[0], 2f, 3f);
        private bool Cataclysmic => Stage >= 3;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.localAI[2] = Projectile.velocity.ToRotation();
                Projectile.scale = Cataclysmic ? 1.55f : 1.18f;
                Projectile.Resize(Cataclysmic ? 38 : 28, Cataclysmic ? 38 : 28);
                Projectile.netUpdate = true;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 2)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            // No gravity, no drag, no homing — the point is that this one is a straight, uninterrupted line
            Projectile.rotation += Cataclysmic ? 0.42f : 0.3f;
            float lightPower = Cataclysmic ? 1.25f : 0.85f;
            Lighting.AddLight(Projectile.Center, lightPower, 0.42f * lightPower, 0.1f);

            SpawnCoreTrail();
        }

        private void SpawnCoreTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitY);

            if (Main.rand.NextBool(2))
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.InfernoFork,
                    backward.RotatedByRandom(0.3f) * Main.rand.NextFloat(1.2f, 3.2f),
                    60,
                    Main.rand.NextBool(3) ? VesuviusProjectileVisuals.HotWhite : VesuviusProjectileVisuals.LavaGold,
                    Main.rand.NextFloat(1f, 1.7f));
                ember.noGravity = true;
            }

            if (Main.rand.NextBool(4))
            {
                Particle streak = new LineParticle(
                    Projectile.Center,
                    backward * Main.rand.NextFloat(3f, 7f),
                    false,
                    16,
                    Main.rand.NextFloat(0.4f, 0.75f),
                    Main.rand.NextBool() ? VesuviusProjectileVisuals.LavaOrange : Color.White);
                GeneralParticleHandler.SpawnParticle(streak);
            }

            if (Main.rand.NextBool(Cataclysmic ? 3 : 5))
                VesuviusProjectileVisuals.SpawnMoltenBloom(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), Main.rand.NextFloat(14f, Cataclysmic ? 34f : 24f), 0.48f);

            if (Cataclysmic && Main.rand.NextBool(3))
            {
                Particle shellSmoke = new SmallSmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    backward * Main.rand.NextFloat(1.2f, 3.4f),
                    VesuviusProjectileVisuals.RavagerSmoke,
                    Color.Black,
                    Main.rand.NextFloat(0.7f, 1.15f),
                    Main.rand.Next(105, 145),
                    Main.rand.NextFloat(-0.04f, 0.04f));
                GeneralParticleHandler.SpawnParticle(shellSmoke);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.localAI[1] = 1f;
            target.AddBuff(BuffID.OnFire3, 240);
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = Cataclysmic ? 0.85f : 0.62f, Pitch = Cataclysmic ? -0.5f : -0.34f }, Projectile.Center);

            if (Projectile.owner == Main.myPlayer)
            {
                bool directHit = Projectile.localAI[1] > 0f;
                float blastMultiplier = directHit
                    ? (Cataclysmic ? 1.15f : 1.05f)
                    : (Cataclysmic ? 0.58f : 0.42f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<VesuviusThermalCoreBlast>(),
                    Math.Max(1, (int)(Projectile.damage * blastMultiplier)),
                    Projectile.knockBack * 1.4f,
                    Projectile.owner,
                    Stage,
                    directHit ? 1f : 0f,
                    Projectile.localAI[2]);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Color coreColor = Color.Lerp(VesuviusProjectileVisuals.LavaOrange, VesuviusProjectileVisuals.HotWhite, 0.4f);
            Color additiveCore = new Color(coreColor.R, coreColor.G, coreColor.B, 0);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                float t = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(
                    bloom,
                    oldCenter - Main.screenPosition,
                    null,
                    additiveCore * (t * 0.5f),
                    0f,
                    bloom.Size() * 0.5f,
                    Projectile.scale * 0.55f * t,
                    SpriteEffects.None);
            }

            // Two-stage bloom exactly like Calamity's VolatileStarcore: a wide coloured halo and
            // a tight white hotspot underneath the sprite.
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, additiveCore * 0.7f, 0f, bloom.Size() * 0.5f, Projectile.scale * 1.3f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, VesuviusProjectileVisuals.AdditiveColor(Color.White) * 0.7f, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.62f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            // Starcore body opaque, as Calamity draws it — additive A=0 here dissolved the core
            // animation into the bloom and left nothing solid to read.
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White,
                Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    // Mixed-effect violent detonation: layered rings, sparks, rock shrapnel and fire dust, staged over a few frames
    public class VesuviusThermalCoreBlast : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Stage => (int)MathHelper.Clamp(Projectile.ai[0], 2f, 3f);
        private bool Cataclysmic => Stage >= 3;
        private bool DirectHit => Projectile.ai[1] > 0f;
        private Vector2 ImpactDirection => Projectile.ai[2].ToRotationVector2();
        private float ExplosionRadius => Cataclysmic ? 300f : 200f;
        private int currentFrame;
        private bool damageFrame;
        private float burstAngle;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 240;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 24;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            damageFrame = currentFrame == 10;

            if (currentFrame == 0)
            {
                burstAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.3f }, Projectile.Center);

                GeneralParticleHandler.SpawnParticle(new ImpactParticle(Projectile.Center, 0.1f, 12, Cataclysmic ? 0.5f : 0.38f, VesuviusProjectileVisuals.HotWhite));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, VesuviusProjectileVisuals.HotWhite, "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, 0f, 0.06f, Cataclysmic ? 0.25f : 0.19f, 13));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, Color.Lerp(VesuviusProjectileVisuals.RavagerSmoke, VesuviusProjectileVisuals.LavaOrange, 0.42f), new Vector2(1f, 0.38f), ImpactDirection.ToRotation(), 0.18f, Cataclysmic ? 1.5f : 1.08f, 18));

                for (int i = 0; i < 6; i++)
                {
                    Vector2 vel = (MathHelper.TwoPi / 6f * i + burstAngle).ToRotationVector2() * 6f;
                    Dust spark = Dust.NewDustPerfect(Projectile.Center, i % 2 == 0 ? DustID.Stone : DustID.InfernoFork, vel, 80, Color.Lerp(Color.DarkGray, VesuviusProjectileVisuals.LavaGold, 0.35f), 1.15f);
                    spark.noGravity = true;
                }
            }

            if (currentFrame == 4)
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, VesuviusProjectileVisuals.LavaOrange, new Vector2(1f, 0.28f), ImpactDirection.ToRotation(), 0.14f, Cataclysmic ? 2.05f : 1.45f, 16));

            if (currentFrame == 10)
                TriggerCoreRupture();

            currentFrame++;
        }

        private void TriggerCoreRupture()
        {
            float distanceFactor = Utils.GetLerpValue(1800f, 240f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            float shakePower = Cataclysmic ? 15f : 8.5f;
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shakePower * distanceFactor);

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.45f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.7f, Pitch = -0.2f }, Projectile.Center);

            float eruptionScale = Cataclysmic ? 1f : 0.74f;
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                VesuviusProjectileVisuals.HotWhite,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                burstAngle,
                0.05f,
                0.26f * eruptionScale,
                18));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                VesuviusProjectileVisuals.LavaOrange,
                "CalamityMod/Particles/FlameExplosion",
                Vector2.One,
                -burstAngle,
                0.06f,
                0.4f * eruptionScale,
                22));
            GeneralParticleHandler.SpawnParticle(new ImpactParticle(Projectile.Center, 0.08f, 14, 0.52f * eruptionScale, VesuviusProjectileVisuals.HotWhite));

            // The two flattened rings are the earthquake read: a dark pressure front followed
            // by a smaller molten fault ring. They stay low instead of becoming a solar halo.
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(VesuviusProjectileVisuals.RavagerSmoke, VesuviusProjectileVisuals.LavaOrange, 0.38f),
                new Vector2(1f, 0.34f),
                ImpactDirection.ToRotation(),
                0.22f,
                (Cataclysmic ? 3.25f : 2.25f),
                28));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                VesuviusProjectileVisuals.LavaGold,
                new Vector2(1f, 0.22f),
                ImpactDirection.ToRotation(),
                0.14f,
                (Cataclysmic ? 2.45f : 1.72f),
                20));

            int ejectaCount = Cataclysmic ? 8 : 6;
            for (int i = 0; i < ejectaCount; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi / ejectaCount * i + burstAngle).ToRotationVector2() * Main.rand.NextFloat(5f, 8f);
                GeneralParticleHandler.SpawnParticle(new PointParticle(Projectile.Center, velocity - Vector2.UnitY * Main.rand.NextFloat(0.4f, 2f), true, Main.rand.Next(15, 23), Main.rand.NextFloat(0.46f, 0.72f), i % 3 == 0 ? VesuviusProjectileVisuals.HotWhite : VesuviusProjectileVisuals.LavaGold, true));
            }

            // Rock shrapnel — a different material mixed into the fire for visual variety
            for (int i = 0; i < (Cataclysmic ? 18 : 12); i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3.5f, 9f) - Vector2.UnitY * Main.rand.NextFloat(0f, 2.5f);
                Dust stone = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(2) ? DustID.Stone : DustID.Obsidian, vel, 100, Color.Lerp(Color.DarkGray, VesuviusProjectileVisuals.LavaOrange, 0.2f), Main.rand.NextFloat(1f, 1.9f));
                stone.noGravity = i % 2 == 0;
            }

            // A small amount of hot dust lights the rock ejecta; it does not form a second blast.
            for (int i = 0; i < (Cataclysmic ? 18 : 12); i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(7f, 5f) - Vector2.UnitY * Main.rand.NextFloat(0f, 2f);
                Dust fire = Dust.NewDustPerfect(Projectile.Center, DustID.InfernoFork, vel, 40, default, Main.rand.NextFloat(0.85f, 1.45f));
                fire.noGravity = true;
                fire.color = Color.Lerp(Color.White, Main.rand.NextBool(3) ? VesuviusProjectileVisuals.LavaGold : VesuviusProjectileVisuals.LavaOrange, 0.7f);
            }

            // The smoke rises; it does not occupy the whole circular hitbox.
            for (int i = 0; i < (Cataclysmic ? 8 : 5); i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2.5f, 1.2f) - Vector2.UnitY * Main.rand.NextFloat(1.2f, 3.8f);
                Particle smoke = new HeavySmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(22f, 12f), vel, VesuviusProjectileVisuals.RavagerSmoke, Main.rand.Next(30, 46), Main.rand.NextFloat(0.8f, 1.35f) * eruptionScale, 0.58f, Main.rand.NextFloat(-0.05f, 0.05f), false, required: true);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            VesuviusProjectileVisuals.SpawnMoltenBloom(Projectile.Center, Cataclysmic ? 70f : 48f, DirectHit ? 0.78f : 0.55f);

            if (Cataclysmic)
                SpawnCrustFractures();
        }

        private void SpawnCrustFractures()
        {
            Vector2 forward = ImpactDirection.SafeNormalize(Vector2.UnitX);
            for (int branch = -1; branch <= 1; branch++)
            {
                Vector2 branchDirection = forward.RotatedBy(branch * 0.34f);
                for (int step = 1; step <= 6; step++)
                {
                    Vector2 position = Projectile.Center + branchDirection * (step * 25f) + branchDirection.RotatedBy(MathHelper.PiOver2) * (float)Math.Sin(step * 1.7f + branch) * 8f;
                    Color color = step % 3 == 0 ? VesuviusProjectileVisuals.HotWhite : VesuviusProjectileVisuals.LavaGold;
                    GeneralParticleHandler.SpawnParticle(new LineParticle(position, branchDirection * 0.2f, false, 12 + step, 0.28f + (6 - step) * 0.018f, color));

                    Dust rock = Dust.NewDustPerfect(position, step % 2 == 0 ? DustID.Obsidian : DustID.Stone, -Vector2.UnitY.RotatedByRandom(0.38f) * Main.rand.NextFloat(1.6f, 5.2f), 100, Color.Lerp(Color.DarkGray, color, 0.18f), Main.rand.NextFloat(0.8f, 1.35f));
                    rock.noGravity = step % 3 == 0;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 360);
            target.AddBuff(BuffID.Daybreak, 240);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, ExplosionRadius, targetHitbox);

        public override bool? CanDamage() => damageFrame ? null : false;

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
