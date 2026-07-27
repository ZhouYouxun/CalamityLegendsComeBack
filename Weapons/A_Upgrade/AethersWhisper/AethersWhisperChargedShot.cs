using System;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper
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

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;                 // 只命中首个敌人
            Projectile.tileCollide = true;            // 撞墙即坍缩
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = AethersWhisperBalance.ChargedShotLifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.extraUpdates = 1;              // 更平滑的高速飞行
        }

        public override void AI()
        {
            // 满蓄直击的额外护甲穿透。
            if (IsFull)
                Projectile.ArmorPenetration = AethersWhisperBalance.FullChargeArmorPen;

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, AethersWhisperVisuals.AetherPurple.ToVector3() * (0.5f + Charge * 0.5f));

            if (Main.rand.NextBool(2) && !Main.dedServ)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(HitWidth * 0.4f, HitWidth * 0.4f),
                    DustID.PurpleTorch, -Projectile.velocity * 0.05f, 80, AethersWhisperVisuals.AetherPurple, Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;
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
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                    Main.rand.NextVector2Circular(3f, 3f), 60, AethersWhisperVisuals.AetherPurple, Main.rand.NextFloat(0.8f, 1.2f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            float visualWidth = AethersWhisperBalance.ChargedShotVisualWidth(Charge);
            Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            AethersWhisperVisuals.BeginAdditive(sb);

            // 深紫外壳（沿飞行方向拉长的重炮晶核）。
            Vector2 tail = Projectile.Center - aim * visualWidth * 1.6f;
            AethersWhisperVisuals.DrawBeamSegment(sb, tail, Projectile.Center + aim * visualWidth * 0.4f,
                AethersWhisperVisuals.AetherPurple with { A = 0 }, visualWidth);
            // 珠白中心线（更细、更亮）。
            AethersWhisperVisuals.DrawBeamSegment(sb, tail, Projectile.Center + aim * visualWidth * 0.4f,
                AethersWhisperVisuals.PearlWhite with { A = 0 }, visualWidth * 0.32f);

            // 核心 bloom。
            Texture2D bloom = AethersWhisperVisuals.BloomCircle.Value;
            sb.Draw(bloom, Projectile.Center - Main.screenPosition, null, AethersWhisperVisuals.ShimmerCyan with { A = 0 },
                0f, bloom.Size() * 0.5f, visualWidth / bloom.Width * 1.4f, SpriteEffects.None, 0f);

            AethersWhisperVisuals.EndAdditive(sb);
            return false;
        }
    }
}
