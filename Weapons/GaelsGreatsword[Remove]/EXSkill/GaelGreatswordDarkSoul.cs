using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.GaelsGreatsword
{
    internal sealed class GaelGreatswordDarkSoul : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/GaelSkull";

        // 血怨紫（暗部点缀）+ 硫火红（主色），与至尊灾厄同源。
        private static readonly Color SoulPurple = GaelGreatswordVisuals.CrimsonViolet;
        private static readonly Color BloodRed = GaelGreatswordVisuals.BrimstoneRed;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            Main.projFrames[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            // 骷髅面朝飞行方向：与灾厄原版 GaelSkull 一致，向左飞时用反向角配合水平翻转，
            // 避免累积自旋 + 镜像组合在变向瞬间产生贴图跳变。
            if (Projectile.velocity.X < 0f)
            {
                Projectile.spriteDirection = -1;
                Projectile.rotation = (-Projectile.velocity).ToRotation();
            }
            else
            {
                Projectile.spriteDirection = 1;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            Lighting.AddLight(Projectile.Center, 0.22f, 0.04f, 0.32f);

            if (Projectile.ai[1] > 12f)
            {
                NPC target = GetStoredTarget();
                if (target != null)
                    HomeToward(target);
                else
                    CalamityUtils.HomeInOnNPC(Projectile, true, 900f, 15f, 26f);
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 0)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];

            if (Projectile.scale > 1.2f)
            {
                Projectile.velocity *= 1.012f;
                Projectile.alpha += 2;
                if (Projectile.alpha >= 245)
                    Projectile.Kill();
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool(4) ? DustID.Blood : (int)CalamityDusts.Brimstone,
                    -Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.3f, 1.8f), 120,
                    Main.rand.NextBool(4) ? BloodRed : SoulPurple, Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }

            EmitBloodRing();
            EmitSoulTrail();
        }

        private void EmitBloodRing()
        {
            // 灾厄原版 GaelSkull 的标志性血环：每 20 帧沿骷髅轮廓生成一圈
            // 血月雨尘埃并向速度后方收束，像被骷髅拖行的血雾罩。
            if (Projectile.ai[1] % 20 != 0f)
                return;

            for (int i = 0; i < 14; i++)
            {
                Vector2 ringOffset = Vector2.UnitX * -Projectile.width * 0.5f;
                ringOffset += -Vector2.UnitY.RotatedBy(i * MathHelper.TwoPi / 14f) * new Vector2(8f, 16f) * Projectile.scale;
                ringOffset = ringOffset.RotatedBy(Projectile.rotation);

                Dust ring = Dust.NewDustPerfect(Projectile.Center + ringOffset, DustID.Rain_BloodMoon,
                    Vector2.Zero, 0, new Color(188, 126, 154), 1.5f);
                ring.noGravity = true;
                // 原版公式：尘埃朝"中心 - 速度×3"的滞后点收束，速度 1.25。
                ring.velocity = ((Projectile.Center - Projectile.velocity * 3f) - ring.position).SafeNormalize(Vector2.Zero) * 1.25f;
            }
        }

        private void EmitSoulTrail()
        {
            // 锁定目标后骷髅尾部曳出暗紫灵珠，提示它正在死咬着谁。
            if (Main.dedServ || GetStoredTarget() == null || !Main.rand.NextBool(4))
                return;

            Vector2 tail = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 14f;
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(tail + Main.rand.NextVector2Circular(5f, 5f),
                -Projectile.velocity * 0.06f + Main.rand.NextVector2Circular(0.5f, 0.5f), false,
                Main.rand.Next(14, 22), Main.rand.NextFloat(0.1f, 0.18f), Main.rand.NextBool(3) ? BloodRed : SoulPurple));
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.55f, MathHelper.Clamp(Projectile.numHits / 3f, 0f, 1f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Main.player[Projectile.owner].GetModPlayer<GaelGreatswordPlayer>().AddDarkEmbers(3 + GaelGreatswordProgression.GetStage() / 2);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? DustID.Blood : (int)CalamityDusts.Brimstone,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 4.2f), 100,
                    Main.rand.NextBool() ? BloodRed : SoulPurple, Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color drawColor = Color.Lerp(SoulPurple, BloodRed, 0.35f + MathF.Sin(Projectile.ai[1] * 0.08f) * 0.2f);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                // TrailingMode 2 会记录 oldRot/oldSpriteDirection，残影用各自历史帧的朝向，
                // 转向时尾迹才能贴合真实轨迹而不是整条一起扭头。
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float afterimageRotation = Projectile.oldRot[i];
                SpriteEffects afterimageEffects = Projectile.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Main.EntitySpriteDraw(texture, drawPosition, frame, drawColor with { A = 0 } * completion * 0.35f,
                    afterimageRotation, origin, Projectile.scale * (0.75f + completion * 0.35f), afterimageEffects);
            }

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, drawColor with { A = 0 } * 0.42f,
                0f, bloom.Size() * 0.5f, Projectile.scale * 0.72f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White,
                Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }

        private NPC GetStoredTarget()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
                return null;

            NPC target = Main.npc[targetIndex];
            return target.CanBeChasedBy(Projectile) ? target : null;
        }

        private void HomeToward(NPC target)
        {
            Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center) * 16f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.075f);
        }
    }
}
