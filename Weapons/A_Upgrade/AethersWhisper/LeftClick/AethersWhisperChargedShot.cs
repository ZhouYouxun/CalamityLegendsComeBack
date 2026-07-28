using System;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Shared;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.LeftClick
{
    /// <summary>
    /// 左键的微光坍缩炮晶核（文档第 3.2 节）。
    /// 可见的压缩晶核：不追踪、无重力、无砖块穿透，只命中首个敌人或首块实体砖。
    /// 直击首个敌人吃完整伤害；随后在命中点触发一圈坍缩 AoE（30% 直击、排除直击目标）。
    /// 满蓄直击额外 40 护穿并施加更久的暗影焰；撞墙只产生等半径的无伤害坍缩视觉。
    /// </summary>
    internal sealed class AethersWhisperChargedShot : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AethersWhisper";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        /// <summary>蓄力进度 0..1（生成时写入）。</summary>
        private float Charge => Projectile.ai[0];
        private bool IsFull => Projectile.ai[1] >= 0.5f;

        private float HitWidth => AethersWhisperBalance.ChargedShotHitWidth(Charge);
        private float CollapseRadius => AethersWhisperBalance.ChargedShotCollapseRadius(Charge);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20; // 更长的高速残影
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;                 // 只命中首个敌人
            Projectile.tileCollide = true;            // 撞墙即坍缩
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            // extraUpdates=9 → 每帧更新 10 次；timeLeft 按子步计，×10 保持约 70 帧寿命。
            Projectile.extraUpdates = AethersWhisperBalance.ChargedShotExtraUpdates;
            Projectile.timeLeft = AethersWhisperBalance.ChargedShotLifetime * (AethersWhisperBalance.ChargedShotExtraUpdates + 1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            // 满蓄直击的额外护甲穿透。
            if (IsFull)
                Projectile.ArmorPenetration = AethersWhisperBalance.FullChargeArmorPen;

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, AethersWhisperVisuals.Lerp(Charge).ToVector3() * (0.7f + Charge * 0.7f));

            // 高速：特效只在每帧最后一个子步生成一次，避免 ×10 刷爆粒子。
            if (Main.dedServ || !Projectile.FinalExtraUpdate())
                return;

            int time = (int)Projectile.localAI[0]++;
            Vector2 fwd = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 perp = fwd.RotatedBy(MathHelper.PiOver2);
            float scaleP = 0.8f + Charge;

            // ① 特斯拉同款「旋转能量螺旋」——但用粒子而非纯尘：两条相位相反的臂 × 三层半径，绕弹体自转。
            float spin = time * 0.42f;
            for (int layer = 0; layer < 3; layer++)
            {
                float radius = (7f + layer * 6f) * scaleP;
                for (int arm = 0; arm < 2; arm++)
                {
                    Vector2 off = perp.RotatedBy(spin + arm * MathHelper.Pi) * radius;
                    bool square = layer == 2;
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center + off, off * 0.04f,
                        square ? AethersWhisperVisuals.GlowSquareTex : "CalamityMod/Particles/BloomCircle",
                        false, Main.rand.Next(10, 16), (square ? 0.06f : 0.09f) * scaleP,
                        AethersWhisperVisuals.Lerp(layer / 2f), square ? new Vector2(1f, 1f) : new Vector2(0.7f, 1.2f),
                        true, !square, glowCenterScale: 0.6f, shrinkSpeed: 0.15f));
                }
            }

            // ② 主光核残影 + 高斯压扁尘拖尾
            GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, Projectile.velocity * 0.12f,
                "CalamityMod/Particles/BloomCircle", false, Main.rand.Next(16, 24), (0.18f + Charge * 0.16f),
                AethersWhisperVisuals.ToWhite(AethersWhisperVisuals.Lerp(Charge), 0.35f), new Vector2(1f, 2f), true, true,
                glowCenterScale: 0.7f, shrinkSpeed: 0.05f));
            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), AethersWhisperVisuals.SquashDust,
                    -fwd.RotatedByRandom(0.25f) * Main.rand.NextFloat(4f, 12f), 0,
                    AethersWhisperVisuals.Lerp(Main.rand.NextFloat()), Main.rand.NextFloat(1.2f, 1.8f) * scaleP);
                d.noGravity = true;
                d.fadeIn = -0.4f;
            }

            // ③ 偶发硬光方块（VelChangingSpark）从弹体后方甩出，强化「压缩微光被拉出」的硬光质感
            if (time % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new VelChangingSpark(Projectile.Center, -Projectile.velocity * 0.12f, -Projectile.velocity * 0.04f,
                    AethersWhisperVisuals.GlowSquareTex, 30, 0.13f + Charge * 0.09f, AethersWhisperVisuals.ToWhite(AethersWhisperVisuals.AetherPurple, 0.3f),
                    new Vector2(1f, 0.9f), lerpRate: 0.1f));
            }
        }

        // 以“上一帧位置 → 当前位置”的线段做首碰判定，宽度随蓄力，避免高速漏判。
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 start = Projectile.Center - Projectile.velocity;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                start, Projectile.Center, HitWidth, ref collisionPoint);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // 满蓄的额外护穿已由 Projectile.ArmorPenetration 提供，这里无需重复。
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Shadowflame>(),
                IsFull ? AethersWhisperBalance.FullChargeShadowflameTicks : AethersWhisperBalance.NormalShadowflameTicks);

            // 周边坍缩：30% 直击伤害，排除直击目标（防止单体双吃）。
            SpawnCollapse(target.Center, Math.Max(1, (int)(damageDone * AethersWhisperBalance.CollapseDamageRatio)), target.whoAmI);
            SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.5f, Pitch = -0.3f }, target.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 撞墙只产生等半径的无伤害视觉坍缩（旧版八向散弹已由右键晶片继承）。
            SpawnCollapse(Projectile.Center, 0, -1);
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f, Pitch = -0.4f }, Projectile.Center);
            Projectile.Kill();
            return false;
        }

        private void SpawnCollapse(Vector2 center, int collapseDamage, int excludedTargetId)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<AethersWhisperCollapse>(),
                collapseDamage,
                0f,
                Projectile.owner,
                CollapseRadius,
                excludedTargetId);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, AethersWhisperVisuals.SquashDust,
                    Main.rand.NextVector2Circular(3f, 3f), 60, AethersWhisperVisuals.Lerp(Main.rand.NextFloat()), Main.rand.NextFloat(0.9f, 1.4f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            float visualWidth = AethersWhisperBalance.ChargedShotVisualWidth(Charge);
            Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            Texture2D bloom = AethersWhisperVisuals.BloomCircle.Value;
            AethersWhisperVisuals.BeginAdditive(sb);

            // 飞行后残影：沿历史位置留一串深紫残影，用 1−t³ 锐利淡出（尾部断崖消失，避免拖泥带水）。
            Vector2 boxCenter = Projectile.Size * 0.5f;
            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float t = i / (float)Projectile.oldPos.Length;
                float a = AethersWhisperVisuals.SharpFade(t) * 0.45f;
                Vector2 p = Projectile.oldPos[i] + boxCenter - Main.screenPosition;
                sb.Draw(bloom, p, null, AethersWhisperVisuals.AetherPurple with { A = 0 } * a,
                    0f, bloom.Size() * 0.5f, visualWidth / bloom.Width * (1f - t) * 0.9f, SpriteEffects.None, 0f);
            }

            // 深紫外壳（沿飞行方向拉长的重炮晶核）。
            Vector2 tail = Projectile.Center - aim * visualWidth * 1.6f;
            AethersWhisperVisuals.DrawBeamSegment(sb, tail, Projectile.Center + aim * visualWidth * 0.4f,
                AethersWhisperVisuals.AetherPurple with { A = 0 }, visualWidth);
            // 珠白中心线（更细、更亮）——双绘制的窄核心。
            AethersWhisperVisuals.DrawBeamSegment(sb, tail, Projectile.Center + aim * visualWidth * 0.4f,
                AethersWhisperVisuals.PearlWhite with { A = 0 }, visualWidth * 0.32f);

            // 压缩微光核心：军械库同款 7 层生长辉光球（青→白渐变），拉成沿飞行方向的椭球。
            AethersWhisperVisuals.DrawEnergyOrb(sb, Projectile.Center, visualWidth * 1.7f,
                AethersWhisperVisuals.Lerp(Charge), 0.9f, new Vector2(1.5f, 1f).RotatedBy(0f));
            // 一枚硬光方块内芯（军械库硬光质感）随飞行方向自旋。
            Texture2D square = ModContent.Request<Texture2D>(AethersWhisperVisuals.GlowSquareTex).Value;
            sb.Draw(square, Projectile.Center - Main.screenPosition, null, AethersWhisperVisuals.PearlWhite with { A = 0 } * 0.8f,
                Projectile.rotation + Main.GlobalTimeWrappedHourly * 3f, square.Size() * 0.5f,
                visualWidth / square.Width * 0.5f, SpriteEffects.None, 0f);

            AethersWhisperVisuals.EndAdditive(sb);
            return false;
        }
    }
}
