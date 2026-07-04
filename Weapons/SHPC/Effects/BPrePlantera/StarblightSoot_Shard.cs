using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal sealed class StarblightSootShard : ModProjectile, ILocalizedModType
    {
        private const string GlowBladeTexture = "CalamityLegendsComeBack/Texture/Shared/GlowBlade";

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/Ranged/StarmageddonStar2";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 33;
            Projectile.height = 33;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 140;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.scale = Main.rand.NextFloat(0.72f, 1.05f) * 1.5f;
            Projectile.alpha = 0;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            SoundEngine.PlaySound(SoundID.Item9, Projectile.Center);
        }

        public override void AI()
        {
            Timer++;
            Projectile.velocity *= 0.99f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + Timer * 0.03f;
            Lighting.AddLight(Projectile.Center, new Color(255, 120, 76).ToVector3() * 0.38f);
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            if (Main.dedServ)
                return;

            SpawnOrbitSparks();
            SpawnFlightParticles();

            if (Main.rand.NextBool(2))
            {
                Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + backward * Main.rand.NextFloat(3f, 12f),
                    Utils.SelectRandom(Main.rand, ModContent.DustType<AstralOrange>(), ModContent.DustType<AstralBlue>()),
                    backward.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.6f, 2.2f),
                    0,
                    default,
                    Main.rand.NextFloat(0.82f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 240);
            SoundEngine.PlaySound(SoundID.Item9, target.Center);

            if (!Main.dedServ)
                SpawnImpactParticles(target.Center, false, 1.0f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                int explosionIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<NewLegendSHPE>(),
                    (int)(Projectile.damage * 0.4),
                    Projectile.knockBack,
                    Projectile.owner);

                if (Main.projectile.IndexInRange(explosionIndex))
                {
                    Projectile explosion = Main.projectile[explosionIndex];
                    int explosionSize = 224;
                    explosion.width = explosionSize;
                    explosion.height = explosionSize;
                    explosion.Center = Projectile.Center;
                    explosion.netUpdate = true;
                }
            }

            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item9, Projectile.Center);

            for (int i = 0; i < 11; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Utils.SelectRandom(Main.rand, ModContent.DustType<AstralOrange>(), ModContent.DustType<AstralBlue>()),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 7.6f),
                    0,
                    default,
                    Main.rand.NextFloat(0.9f, 1.45f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Utils.SelectRandom(Main.rand, ModContent.DustType<AstralOrange>(), ModContent.DustType<AstralBlue>()),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(9.5f, 15.5f),
                    0,
                    default,
                    Main.rand.NextFloat(0.8f, 1.3f));
                dust.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 136, 72) * 0.6f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(-0.2f, 0.2f),
                0.05f,
                0.52f,
                16));

            SpawnImpactParticles(Projectile.Center, true, 0.6f);

            for (int i = 0; i < 7; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 velocity = direction * Main.rand.NextFloat(7.5f, 13.5f);
                Color color = Main.rand.NextBool() ? Color.Orange : Color.LightBlue;
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center + direction * Main.rand.NextFloat(4f, 12f),
                    velocity,
                    false,
                    Main.rand.Next(35, 55),
                    Main.rand.NextFloat(0.6f, 1.0f),
                    color));
            }

            float pulseFactor = Projectile.scale;
            Color pulseColor = new Color(255, 136, 72);
            float startSize = 0.07f * pulseFactor * 0.25f;
            float endSize = 0.33f * pulseFactor * 0.25f;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero, pulseColor,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One, Main.rand.NextFloat(-10f, 10f), startSize, endSize, 30));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero, pulseColor,
                "CalamityMod/Particles/BloomRing",
                Vector2.One, Main.rand.NextFloat(-10f, 10f), startSize, endSize, 30));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero, pulseColor,
                "CalamityMod/Particles/PlasmaExplosion",
                Vector2.One, Main.rand.NextFloat(-10f, 10f), startSize, endSize, 30));
        }

        private void SpawnOrbitSparks()
        {
            // �û��������뵯Ļ��ת����һ��
            float spinPhase = Projectile.rotation;
            float orbitRadius = 9f;

            Color sparkColor =
                Color.Lerp(
                    new Color(255, 130, 68),
                    new Color(92, 205, 255),
                    0.35f) * 0.46f;

            // ����һ֡��Ļλ��
            Vector2 positionCompensation = Projectile.velocity;

            for (int i = 0; i < 4; i++)
            {
                float angle = spinPhase + MathHelper.PiOver2 * i;

                Vector2 radialDirection = angle.ToRotationVector2();

                // ��ǰ���ɣ������Ӿ�������ں���
                Vector2 sparkCenter =
                    Projectile.Center +
                    radialDirection * orbitRadius +
                    positionCompensation;

                Vector2 finalVelocity =
                    radialDirection.RotatedBy(MathHelper.PiOver2) * 1.05f;

                float extraRot =
                    radialDirection.ToRotation() -
                    finalVelocity.ToRotation();

                GeneralParticleHandler.SpawnParticle(
                    new CustomSpark(
                        sparkCenter,
                        finalVelocity,
                        GlowBladeTexture,
                        false,
                        2,
                        0.08f,
                        sparkColor,
                        new Vector2(0.28f, 0.58f),
                        glowCenter: true,
                        shrinkSpeed: 1.2f,
                        glowCenterScale: 0.62f,
                        glowOpacity: 0.38f,
                        extraRotation: extraRot));
            }
        }

        private void SpawnFlightParticles()
        {
            Color trailColor = Main.rand.NextBool()
                ? new Color(255, 148, 82)
                : new Color(102, 214, 255);

            for (int i = 0; i < 2; i++)
            {
                int dustType = Utils.SelectRandom(
                    Main.rand,
                    DustID.OrangeTorch,
                    DustID.BlueTorch,
                    DustID.WhiteTorch,
                    DustID.CrimsonTorch);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                    dustType,
                    -Projectile.velocity * Main.rand.NextFloat(0.015f, 0.06f),
                    100,
                    trailColor,
                    Main.rand.NextFloat(0.72f, 1.18f));
                dust.noGravity = true;
                dust.fadeIn = 0.3f;
            }

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    -Projectile.velocity * Main.rand.NextFloat(0.02f, 0.08f),
                    false,
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.42f, 0.78f),
                    trailColor));
            }

            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Vector2.Zero,
                    trailColor,
                    Vector2.One,
                    0f,
                    0.08f,
                    0f,
                    16));
            }
        }

        private static void SpawnImpactParticles(Vector2 center, bool stronger, float density = 1.0f)
        {
            int dustCount  = Math.Max(1, (int)Math.Round((stronger ? 28 : 18) * density));
            int sparkCount = Math.Max(1, (int)Math.Round((stronger ? 18 : 11) * density));
            int rayCount   = Math.Max(1, (int)Math.Round((stronger ? 4  : 3)  * density));

            for (int i = 0; i < dustCount; i++)
            {
                int dustType = Utils.SelectRandom(
                    Main.rand,
                    DustID.OrangeTorch,
                    DustID.BlueTorch,
                    DustID.WhiteTorch,
                    DustID.CrimsonTorch);
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, stronger ? 9f : 6.5f);
                Dust dust = Dust.NewDustPerfect(center, dustType, velocity, 80, default, Main.rand.NextFloat(1.05f, stronger ? 1.85f : 1.55f));
                dust.noGravity = true;
                dust.fadeIn = 0.7f;
            }

            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / sparkCount).ToRotationVector2();
                Vector2 velocity = direction.RotatedBy(0.44f) * Main.rand.NextFloat(1.6f, stronger ? 5.2f : 3.8f);
                Color color = Main.rand.NextBool() ? Color.Orange : Color.LightBlue;
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    center + direction * Main.rand.NextFloat(2f, 10f),
                    velocity,
                    false,
                    Main.rand.Next(28, 44),
                    Main.rand.NextFloat(0.72f, 1.25f) * density,
                    color));
            }

            for (int side = 0; side < rayCount; side++)
            {
                Vector2 direction = (MathHelper.TwoPi * side / rayCount).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    center,
                    direction * Main.rand.NextFloat(10f, 16f),
                    false,
                    Main.rand.Next(20, 32),
                    Main.rand.NextFloat(0.62f, 0.92f) * density,
                    side % 2 == 0 ? Color.Orange : Color.LightBlue));
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                (stronger ? new Color(255, 148, 82) : new Color(102, 214, 255)) * density,
                Vector2.One,
                0f,
                stronger ? 0.3f : 0.22f,
                0f,
                stronger ? 28 : 22));

            if (Main.rand.NextFloat() < density)
                CLCBLightingBoltsSystem.Spawn_AstralSoulLightsA(center);
            if (Main.rand.NextFloat() < density)
                CLCBLightingBoltsSystem.Spawn_CelestialBurst(center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Rectangle frame = texture.Frame(verticalFrames: Main.projFrames[Type], frameY: Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Color mainColor = new(255, 136, 72, 0);
            Color blueColor = new(92, 210, 255, 0);
            Vector2 center = Projectile.Center - Main.screenPosition;

            // 1. Draw Bloom background (Additive)
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            
            float pulse = 1f + (float)Math.Sin(Timer * 0.22f) * 0.08f;
            // Enhanced main orange bloom
            Main.EntitySpriteDraw(bloom, center, null, mainColor * 0.7f, Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * 0.38f * pulse, SpriteEffects.None);
            // Dynamic blue bloom for depth
            Main.EntitySpriteDraw(bloom, center, null, blueColor * 0.45f, -Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * 0.26f * pulse, SpriteEffects.None);

            // 2. Draw Trails (Additive)
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldPosition = Projectile.oldPos[i];
                if (oldPosition == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = oldPosition + Projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = Color.Lerp(mainColor, blueColor, 0.35f + completion * 0.35f) * (completion * 0.55f);
                Main.EntitySpriteDraw(texture, drawPosition, frame, trailColor, Projectile.rotation, origin, Projectile.scale * completion * 0.58f, SpriteEffects.None);
            }

            // 3. Draw outline (Additive)
            Color outlineColor = Color.Lerp(mainColor, blueColor, 0.42f) * 0.75f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 2.6f * Projectile.scale;
                Main.EntitySpriteDraw(texture, center + offset, frame, outlineColor, Projectile.rotation, origin, Projectile.scale * 1.02f, SpriteEffects.None);
            }

            // 4. Draw main body (AlphaBlend for solid crispness and high brightness)
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            
            // Draw main star (solid)
            Main.EntitySpriteDraw(texture, center, frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            
            // Draw inner star (aligned rotation now!)
            Main.EntitySpriteDraw(texture, center, frame, Color.White * 0.65f, Projectile.rotation, origin, Projectile.scale * 0.62f, SpriteEffects.None);

            return false;
        }
    }
}
