using System;
using CalamityLegendsComeBack.Weapons.AegisBlade.Visuals;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    /// <summary>
    /// 下插的庇护之刃。坠地后钉在地上，从插入点升起庇护土墙，随后自身烧尽消散。
    /// 视觉：坠落拖火 → 插地焦痕 + 扩张符文封印 → 剑身逐渐烧成余烬。
    /// </summary>
    public class AegisBladeThrown : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/AegisBlade";

        private bool embedded;
        private bool wallSpawned;
        private int embedTimer;
        private int fallTimer;
        private float impactRotation;

        private float EmbeddedFade => embedded ? Utils.GetLerpValue(40f, 0f, embedTimer, true) : 1f;

        /// <summary>0 → 1：插地后封印圈向外扩张的进度。</summary>
        private float SealExpansion => embedded ? Utils.GetLerpValue(0f, 16f, embedTimer, true) : 0f;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.ignoreWater = true;
            Projectile.scale = 1.35f;
        }

        public override void AI()
        {
            if (!embedded)
            {
                fallTimer++;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
                Projectile.velocity.Y = MathF.Min(Projectile.velocity.Y + 0.34f, 22f);
                AegisVisuals.Light(Projectile.Center, 0.95f);

                if (!Main.dedServ)
                    EmitFallFlame();
                return;
            }

            embedTimer++;
            float fade = EmbeddedFade;
            AegisVisuals.Light(Projectile.Center, 0.5f + fade * 0.9f);
            if (!Main.dedServ)
                EmitEmbeddedDissolve(fade);

            if (!wallSpawned && embedTimer == 1)
            {
                SpawnWalls();
                wallSpawned = true;
            }

            if (embedTimer > 40)
                Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            embedded = true;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60;
            Projectile.rotation = SnapEmbeddedRotation(oldVelocity);
            impactRotation = Main.rand.NextFloat(MathHelper.TwoPi);
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 1f, Pitch = -0.38f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact with { Volume = 0.65f, Pitch = 0.15f }, Projectile.Center);
            EmitImpact(Projectile.Center, oldVelocity.SafeNormalize(Vector2.UnitY));
            return false;
        }

        private static float SnapEmbeddedRotation(Vector2 impactVelocity)
        {
            bool horizontal = Math.Abs(impactVelocity.X) >= Math.Abs(impactVelocity.Y);
            float axisRotation = horizontal
                ? (impactVelocity.X >= 0f ? 0f : MathHelper.Pi)
                : (impactVelocity.Y >= 0f ? MathHelper.PiOver2 : -MathHelper.PiOver2);
            return axisRotation + MathHelper.PiOver4;
        }

        /// <summary>坠落拖火：圣火尾迹 + 侧向被气流撕开的火屑。</summary>
        private void EmitFallFlame()
        {
            AegisVisuals.FlightTrail(Projectile.Center, Projectile.velocity, 1.25f, fallTimer, 3);

            if (fallTimer % 2 == 0)
            {
                Vector2 backwards = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
                Vector2 side = backwards.RotatedBy(MathHelper.PiOver2);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + backwards * 12f,
                    backwards * Main.rand.NextFloat(1.5f, 4f) + side * Main.rand.NextFloatDirection() * 1.8f,
                    false, Main.rand.Next(8, 14), Main.rand.NextFloat(0.07f, 0.13f),
                    AegisVisuals.Add(AegisVisuals.Gold, 0.85f), new Vector2(2.2f, 0.45f), true, false, 1f));
            }
        }

        /// <summary>插地：向下压出定向冲击，往两侧扫出火焰与碎石。</summary>
        private void EmitImpact(Vector2 position, Vector2 direction)
        {
            if (Main.dedServ)
                return;

            AegisVisuals.HolyDetonation(position, 1.75f);
            AegisVisuals.DirectionalImpact(position, direction, 1.2f);
            AegisVisuals.Screenshake(position, 2.8f, 950f);

            // 沿地面向两侧扫开的火舌
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 sweep = new Vector2(i, -0.3f).SafeNormalize(Vector2.UnitX);
                AegisVisuals.EmberJet(position, sweep, 7, 1.15f, 0.3f);
                GeneralParticleHandler.SpawnParticle(new CustomPulse(position, Vector2.Zero,
                    AegisVisuals.Add(AegisVisuals.Gold, 0.85f), AegisVisuals.TexBlastCone,
                    new Vector2(3.4f, 1.15f), sweep.ToRotation(), 0.8f, 0f, 22));
            }

            // 被砸起来的碎石
            for (int i = 0; i < 12; i++)
            {
                Dust debris = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(16f, 6f),
                    DustID.Dirt, new Vector2(Main.rand.NextFloat(-5.5f, 5.5f), -Main.rand.NextFloat(2f, 8.5f)),
                    0, Color.Lerp(AegisVisuals.Charred, AegisVisuals.Ember, Main.rand.NextFloat(0.15f, 0.75f)),
                    Main.rand.NextFloat(0.9f, 1.5f));
                debris.noGravity = false;
            }
        }

        /// <summary>钉在地上的期间：剑身持续冒火屑，火从封印圈里往上舔。</summary>
        private void EmitEmbeddedDissolve(float fade)
        {
            if (embedTimer % 3 == 0)
            {
                Vector2 outward = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 position = Projectile.Center + outward * Main.rand.NextFloat(4f, 22f);

                Dust ember = Dust.NewDustPerfect(position, AegisVisuals.ProfanedFireDust,
                    outward * Main.rand.NextFloat(0.25f, 1.1f) - Vector2.UnitY * 0.8f,
                    0, Color.White, Main.rand.NextFloat(0.8f, 1.4f) * fade);
                ember.noGravity = true;

                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(position,
                    -Vector2.UnitY * Main.rand.NextFloat(0.8f, 2.2f) + outward * 0.4f,
                    false, Main.rand.Next(16, 26), Main.rand.NextFloat(0.12f, 0.24f) * fade,
                    AegisVisuals.RandomFlameColor(), true, false, true));
            }

            // 烧尽的最后阶段：剑身开始向上飘散圣灰
            if (fade < 0.55f && Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 20f),
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(0.8f, 2.4f)),
                    Color.Lerp(AegisVisuals.Charred, Color.DarkSlateGray, Main.rand.NextFloat(0.3f, 0.9f)),
                    Color.Transparent, Main.rand.NextFloat(0.3f, 0.6f), Main.rand.Next(24, 40),
                    Main.rand.NextFloat(-0.04f, 0.04f)));
            }
        }

        private void SpawnWalls()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            int wallType = ModContent.ProjectileType<AegisWallProjectile>();
            float riseSpeed = AegisWallProjectile.WallHalfHeight / (float)BalanceAegisBlade.WallRiseTime;
            int wallDamage = Math.Max(1, (int)(Projectile.damage * 0.8f));

            // 速凝掩体最多同时存在 2 个，超出时先清除剩余时间最少（最老）的
            int wallCount = 0;
            int oldestIdx = -1;
            int minTimeLeft = int.MaxValue;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.type != wallType || p.owner != Projectile.owner) continue;
                wallCount++;
                if (p.timeLeft < minTimeLeft) { minTimeLeft = p.timeLeft; oldestIdx = i; }
            }
            if (wallCount >= 2 && oldestIdx >= 0)
                Main.projectile[oldestIdx].Kill();

            Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                new Vector2(Projectile.Center.X, Projectile.Center.Y), new Vector2(0f, -riseSpeed),
                wallType, wallDamage, 4f, Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D swordTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D jet = AegisVisuals.Tex(AegisVisuals.TexJet);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = new(0f, swordTexture.Height);
            float fade = EmbeddedFade;
            float glowStrength = embedded ? fade : 0.72f;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            if (embedded)
            {
                // ① 地面焦痕：插地那一下烧出来的印子
                AegisVisuals.DrawScorchDecal(drawPosition + Vector2.UnitY * 10f, impactRotation,
                    MathHelper.Lerp(24f, 76f, SealExpansion), fade * 0.95f, new Vector2(1.35f, 0.55f));

                // ② 扩张的符文封印：土墙就是从这个印记里升起来的
                AegisVisuals.DrawRuneSigil(drawPosition + Vector2.UnitY * 10f,
                    MathHelper.Lerp(20f, 88f, SealExpansion),
                    Main.GlobalTimeWrappedHourly * 1.6f, fade * 0.8f,
                    new Vector2(1f, 0.38f), 1f);
            }

            // ③ 剑身火脊：一条沿刃口的火焰，越接近烧尽越弱
            Vector2 bladeDirection = (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
            Vector2 spineCenter = drawPosition + bladeDirection * 26f * Projectile.scale;
            Main.EntitySpriteDraw(jet, spineCenter, null,
                AegisVisuals.Add(AegisVisuals.Flame, 0.42f * glowStrength),
                Projectile.rotation - MathHelper.PiOver4 + MathHelper.PiOver2, jet.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(jet, 11f), AegisVisuals.RadiusScale(jet, 30f)),
                SpriteEffects.None, 0);
            Main.EntitySpriteDraw(jet, spineCenter, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.3f * glowStrength),
                Projectile.rotation - MathHelper.PiOver4 + MathHelper.PiOver2, jet.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(jet, 4.5f), AegisVisuals.RadiusScale(jet, 25f)),
                SpriteEffects.None, 0);

            // ④ 亵渎背光
            AegisVisuals.ProfanedBackglow(swordTexture, drawPosition, null, Projectile.rotation, origin,
                new Vector2(Projectile.scale), glowStrength, 3.6f, 6);

            // ⑤ 烧尽：剑身被余烬一层层剥离，副本随进度向外散开
            if (embedded)
            {
                float dissolveProgress = 1f - fade;
                int copies = 9;
                for (int i = 0; i < copies; i++)
                {
                    float angle = MathHelper.TwoPi * i / copies + dissolveProgress * 1.7f;
                    Vector2 offset = angle.ToRotationVector2() * MathHelper.Lerp(2f, 26f, dissolveProgress);
                    Color copyColor = AegisVisuals.Add(
                        AegisVisuals.Gradient(0.15f + 0.75f * (i / (float)copies)), 0.16f * fade);
                    Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null, copyColor,
                        Projectile.rotation, origin, Projectile.scale * (1f + dissolveProgress * 0.08f),
                        SpriteEffects.None, 0);
                }
            }

            Main.spriteBatch.ExitShaderRegion();

            Color bodyColor = embedded
                ? Color.Lerp(lightColor, AegisVisuals.Ember, 1f - fade) * MathHelper.Lerp(0.35f, 1f, fade)
                : lightColor;
            Main.EntitySpriteDraw(swordTexture, drawPosition, null, bodyColor,
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
