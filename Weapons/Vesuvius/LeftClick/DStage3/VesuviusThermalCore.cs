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
            Lighting.AddLight(Projectile.Center, lightPower * VesuviusProjectileVisuals.VisualIntensity, 0.42f * lightPower * VesuviusProjectileVisuals.VisualIntensity, 0.1f * VesuviusProjectileVisuals.VisualIntensity);

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
                    Main.rand.NextFloat(0.55f, 0.9f),
                    0.62f,
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
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                float t = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(
                    bloom,
                    oldCenter - Main.screenPosition,
                    null,
                    additiveCore * (t * 0.5f * VesuviusProjectileVisuals.VisualIntensity),
                    0f,
                    bloom.Size() * 0.5f,
                    Projectile.scale * 0.55f * t * VesuviusProjectileVisuals.VisualScale,
                    SpriteEffects.None);
            }

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                additiveCore * 0.65f * VesuviusProjectileVisuals.VisualIntensity,
                0f,
                bloom.Size() * 0.5f,
                Projectile.scale * 1.4f * VesuviusProjectileVisuals.VisualScale,
                SpriteEffects.None);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White with { A = 0 },
                Projectile.rotation, frame.Size() * 0.5f, Projectile.scale * 1.2f, SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
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

                for (int i = 0; i < 4; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, VesuviusProjectileVisuals.LavaOrange, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 2.2f, 0.9f, 22, true));
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.White, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 1.1f, 0.4f, 22, true));
                }
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, VesuviusProjectileVisuals.LavaGold * 0.6f, "CalamityMod/Particles/BloomRing", Vector2.One, Main.rand.NextFloat(-5f, 5f), 4f, 0f, 16));

                for (int i = 0; i < 6; i++)
                {
                    Vector2 vel = (MathHelper.TwoPi / 6f * i + burstAngle).ToRotationVector2() * 6f;
                    Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, vel, 0, default, 1.4f);
                    spark.noGravity = true;
                }
            }

            if (currentFrame == 4)
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, VesuviusProjectileVisuals.LavaOrange * 0.5f, "CalamityMod/Particles/BloomRing", Vector2.One, Main.rand.NextFloat(-5f, 5f), 3.2f, 0f, 14));

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

            int pulseCount = Cataclysmic ? 7 : 5;
            for (int i = 0; i < pulseCount; i++)
            {
                Color c = Color.Lerp(VesuviusProjectileVisuals.LavaOrange, VesuviusProjectileVisuals.HotWhite, Utils.GetLerpValue(0, pulseCount, i, true));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, c, "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(-5f, 5f), 0f, ExplosionRadius * 0.00065f + 0.065f + 0.04f * i, 26 - i));
            }

            for (int i = 0; i < 3; i++)
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Lerp(VesuviusProjectileVisuals.LavaOrange, Color.OrangeRed, i / 3f), "CalamityMod/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(-5f, 5f), 0f, 0.09f + 0.05f * i * 3, 26 - i * 2));

            for (int i = 0; i < 2; i++)
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, VesuviusProjectileVisuals.LavaGold, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 1f, 4.5f, 30, true));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.White, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.5f, 1.8f, 30, true));
            }

            // Radiating spark spokes
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = (MathHelper.TwoPi / 8f * i + burstAngle).ToRotationVector2() * 9f;
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, vel, false, 10, 0.2f, VesuviusProjectileVisuals.LavaGold, new Vector2(1.6f, 0.8f), true, true, 2));
            }

            // Rock shrapnel — a different material mixed into the fire for visual variety
            for (int i = 0; i < (Cataclysmic ? 30 : 20); i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 11f);
                Dust stone = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(2) ? DustID.Stone : DustID.Obsidian, vel, 100, Color.Lerp(Color.DarkGray, VesuviusProjectileVisuals.LavaOrange, 0.2f), Main.rand.NextFloat(1f, 1.9f));
                stone.noGravity = i % 2 == 0;
            }

            // Fire dust burst
            for (int i = 0; i < (Cataclysmic ? 48 : 32); i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(9f, 9f);
                Dust fire = Dust.NewDustPerfect(Projectile.Center, DustID.InfernoFork, vel, 40, default, Main.rand.NextFloat(1f, 1.9f));
                fire.noGravity = true;
                fire.color = Color.Lerp(Color.White, Main.rand.NextBool(3) ? VesuviusProjectileVisuals.LavaGold : VesuviusProjectileVisuals.LavaOrange, 0.7f);
            }

            // Heavy smoke plume
            for (int i = 0; i < (Cataclysmic ? 16 : 10); i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4f, 4f) * Main.rand.NextFloat(0.6f, 1.6f);
                Particle smoke = new HeavySmokeParticle(Projectile.Center + vel * 3f, vel, VesuviusProjectileVisuals.RavagerSmoke, Main.rand.Next(28, 42), Main.rand.NextFloat(1.2f, 2f), 0.55f, Main.rand.NextFloat(-0.05f, 0.05f), true, required: true);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            VesuviusProjectileVisuals.SpawnMoltenBloom(Projectile.Center, Cataclysmic ? 116f : 82f, DirectHit ? 0.95f : 0.68f);

            if (Cataclysmic)
                SpawnCrustFractures();
        }

        private void SpawnCrustFractures()
        {
            Vector2 forward = ImpactDirection.SafeNormalize(Vector2.UnitX);
            for (int branch = -1; branch <= 1; branch++)
            {
                Vector2 branchDirection = forward.RotatedBy(branch * 0.34f);
                for (int step = 1; step <= 9; step++)
                {
                    Vector2 position = Projectile.Center + branchDirection * (step * 28f) + branchDirection.RotatedBy(MathHelper.PiOver2) * (float)Math.Sin(step * 1.7f + branch) * 10f;
                    Color color = step % 3 == 0 ? VesuviusProjectileVisuals.HotWhite : VesuviusProjectileVisuals.LavaGold;
                    GeneralParticleHandler.SpawnParticle(new LineParticle(position, branchDirection * Main.rand.NextFloat(2.5f, 5.5f), false, 18 + step, 0.38f + (9 - step) * 0.025f, color));

                    Dust rock = Dust.NewDustPerfect(position, step % 2 == 0 ? DustID.Obsidian : DustID.Stone, -Vector2.UnitY.RotatedByRandom(0.45f) * Main.rand.NextFloat(2f, 7f), 100, Color.Lerp(Color.DarkGray, color, 0.22f), Main.rand.NextFloat(0.9f, 1.6f));
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
