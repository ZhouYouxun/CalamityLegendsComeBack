using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle.Proj
{
    internal sealed class AMRBullseye : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public const int FadeInDuration = 10;
        public const int FadeOutDuration = 14;

        private int fadeTimer = 0;
        private bool isFadingOut = false;
        private bool triggeredThisFrame = false;

        public Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.timeLeft = 3600;
            Projectile.Opacity = 0f;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (!Main.npc.IndexInRange(targetIndex))
            {
                StartFadeOut();
            }

            NPC target = Main.npc[targetIndex];
            AMRPlayer amrPlayer = Owner.active && !Owner.dead ? Owner.GetModPlayer<AMRPlayer>() : null;

            bool shouldStayActive = target.active && target.life > 0 && !target.dontTakeDamage &&
                                    Owner.active && !Owner.dead && amrPlayer != null &&
                                    amrPlayer.IsHoldingWeapon && AMRBalance.BullseyeUnlocked;

            if (!shouldStayActive)
            {
                StartFadeOut();
            }

            if (!isFadingOut)
            {
                if (fadeTimer < FadeInDuration)
                    fadeTimer++;
            }
            else
            {
                fadeTimer--;
                if (fadeTimer <= 0)
                {
                    Projectile.Kill();
                    return;
                }
            }

            float fadeProgress = isFadingOut
                ? MathHelper.Clamp(fadeTimer / (float)FadeOutDuration, 0f, 1f)
                : MathHelper.Clamp(fadeTimer / (float)FadeInDuration, 0f, 1f);

            Projectile.Opacity = fadeProgress;
            Projectile.scale = isFadingOut
                ? MathHelper.Lerp(1.35f, 1.0f, fadeProgress)
                : MathHelper.Lerp(1.45f, 1.0f, fadeProgress);

            triggeredThisFrame = false;

            // 计算敌人碰撞箱中心到玩家碰撞箱中心的“敌人碰撞箱边缘”位置
            if (target.active)
            {
                Vector2 dirToPlayer = Owner.Center - target.Center;
                float dist = dirToPlayer.Length();
                if (dist > 0.001f)
                {
                    Vector2 v = dirToPlayer;
                    float hw = target.width * 0.5f;
                    float hh = target.height * 0.5f;
                    float absVx = Math.Abs(v.X);
                    float absVy = Math.Abs(v.Y);
                    float tx = absVx > 0.0001f ? (hw / absVx) : float.MaxValue;
                    float ty = absVy > 0.0001f ? (hh / absVy) : float.MaxValue;
                    float t = Math.Min(tx, ty);

                    Projectile.Center = target.Center + v * t;
                }
                else
                {
                    Projectile.Center = target.Center;
                }
            }

            // 准星周边的边缘微粒气场 (包含从冲刺移过来的粒子特效)
            if (Projectile.Opacity > 0.5f && Main.rand.NextBool(3) && !Main.dedServ)
            {
                Vector2 sparkVel = Main.rand.NextVector2Circular(2f, 2f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                    sparkVel,
                    false,
                    8,
                    0.35f,
                    Main.rand.NextBool() ? new Color(255, 210, 40) : new Color(30, 26, 20)));
            }
        }

        public void StartFadeOut()
        {
            if (!isFadingOut)
            {
                isFadingOut = true;
                fadeTimer = FadeOutDuration;
            }
        }

        public void TriggerVortexImpactExplosion(NPC target)
        {
            if (triggeredThisFrame)
                return;

            triggeredThisFrame = true;

            if (Main.dedServ)
                return;

            Vector2 center = Projectile.Center;

            // 播放 Item68 游戏音效
            SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.9f, Pitch = 0.12f }, center);

            // 数学美感的黄金角度螺线 (Golden Angle Spiral) 涡旋爆发
            const int armCount = 5;
            const int particlesPerArm = 6;

            for (int arm = 0; arm < armCount; arm++)
            {
                float armBaseAngle = arm * MathHelper.TwoPi / armCount;
                for (int i = 0; i < particlesPerArm; i++)
                {
                    float progress = (i + 1f) / particlesPerArm;
                    float spiralAngle = armBaseAngle + i * 0.45f;
                    float radius = 10f + progress * 52f;
                    Vector2 spiralOffset = spiralAngle.ToRotationVector2() * radius;
                    Vector2 vel = (spiralAngle + MathHelper.PiOver2 * 0.7f).ToRotationVector2() * (3.5f + progress * 5.5f);

                    Color pColor = Color.Lerp(new Color(255, 215, 50), new Color(40, 32, 16), progress * 0.5f);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        center + spiralOffset,
                        vel,
                        false,
                        14,
                        0.45f * (1f - progress * 0.3f),
                        pColor,
                        true,
                        false,
                        true));

                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        center + spiralOffset,
                        vel * 1.3f,
                        false,
                        12,
                        0.42f,
                        new Color(255, 195, 45)));
                }
            }

            // 融合从冲刺移过来的脉冲环与重烟冲击特效
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                new Color(255, 210, 50),
                new Vector2(0.85f, 0.85f),
                0f,
                0.08f,
                1.5f,
                16));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                new Color(25, 20, 15),
                new Vector2(1.15f, 1.15f),
                0f,
                0.04f,
                1.9f,
                20));

            for (int k = 0; k < 8; k++)
            {
                Vector2 smokeVel = (MathHelper.TwoPi * k / 8f).ToRotationVector2() * 3.2f;
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    center,
                    smokeVel,
                    new Color(20, 16, 12),
                    18,
                    0.52f,
                    0.8f,
                    0.03f,
                    false,
                    required: true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner || Projectile.Opacity <= 0f)
                return false;

            int targetIndex = (int)Projectile.ai[0];
            NPC target = Main.npc.IndexInRange(targetIndex) ? Main.npc[targetIndex] : null;
            if (target == null || !target.active)
                return false;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Projectile.Opacity;
            float scale = Projectile.scale;

            // 完全使用晨光灵源 (DaawnlightSpiritOrigin) 官方高精贴图绘制
            Texture2D bullseyeTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Typeless/SpiritOriginRegularBullseye").Value;
            Rectangle frame = bullseyeTexture.Frame();
            if (target.IsABoss())
            {
                bullseyeTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Typeless/SpiritOriginBossBullseye").Value;
                frame = bullseyeTexture.Frame(1, 4, 0, (int)(Main.GlobalTimeWrappedHourly * 7f) % 4);
                drawPosition.Y -= 17;
                drawPosition.X -= 1;
            }

            Vector2 origin = frame.Size() * 0.5f;

            // 1. 黑色加粗描边底层 (8 方向偏置绘制)
            Vector2[] outlineOffsets =
            {
                new(-1.5f, 0f), new(1.5f, 0f), new(0f, -1.5f), new(0f, 1.5f),
                new(-1.2f, -1.2f), new(1.2f, 1.2f), new(-1.2f, 1.2f), new(1.2f, -1.2f)
            };
            Color darkOutlineColor = new Color(15, 12, 8) * opacity;

            foreach (Vector2 offset in outlineOffsets)
            {
                Main.EntitySpriteDraw(
                    bullseyeTexture,
                    drawPosition + offset,
                    frame,
                    darkOutlineColor,
                    Projectile.rotation,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0);
            }

            // 2. 高亮战术金黄色顶层
            Color goldMainColor = new Color(255, 210, 45) * opacity;
            Main.EntitySpriteDraw(
                bullseyeTexture,
                drawPosition,
                frame,
                goldMainColor,
                Projectile.rotation,
                origin,
                scale,
                SpriteEffects.None,
                0);

            return false;
        }
    }
}
