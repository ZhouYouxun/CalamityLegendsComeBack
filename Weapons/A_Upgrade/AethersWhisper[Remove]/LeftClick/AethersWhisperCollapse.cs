using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Shared;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.LeftClick
{
    /// <summary>
    /// 微光坍缩：左键晶核在命中点触发的一圈坍缩（文档第 3.3 节，无伤害视觉 + 可选 AoE）。
    /// 直击命中时 damage &gt; 0：一圈 30% 直击伤害、排除直击目标；撞墙时 damage == 0：纯视觉。
    /// 视觉只有三层：空心环轮廓 → 白色核心内缩成点 → 2–4 片短晶屑。
    /// 环用灾厄现成的 HollowCircleHardEdge（即 PulseRing 用的冲击环图），核用 BloomCircle。
    /// 不用火焰爆炸 / 圆形烟雾 / 血肉碎片 / 雷链 / 大范围尘埃。
    /// </summary>
    internal sealed class AethersWhisperCollapse : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 24;

        public new string LocalizationCategory => "Projectiles.AethersWhisper";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float Radius => Projectile.ai[0];
        private int ExcludedTarget => (int)Projectile.ai[1];
        private int Age => Lifetime - Projectile.timeLeft;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // 每个 NPC 只被这圈坍缩结算一次
        }

        // 只有携带伤害（直击派生）且成形的一小段窗口才造成伤害；撞墙的纯视觉恒不伤害。
        public override bool? CanDamage() => Projectile.damage > 0 && Age >= 1 && Age <= 8;

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            float r = Radius * MathHelper.Clamp(Age / 4f, 0.4f, 1f);
            Vector2 center = Projectile.Center;
            hitbox = new Rectangle((int)(center.X - r), (int)(center.Y - r), (int)(r * 2f), (int)(r * 2f));
        }

        // 排除直击目标，防止单体双吃。
        public override bool? CanHitNPC(NPC target) => target.whoAmI == ExcludedTarget ? false : null;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Main.dedServ)
                return;

            // 命中/撞墙坍缩的军械库同频爆点：白核强闪 + 拉宽脉冲环 + 硬光方块崩解 + 电弧尘。
            GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero,
                AethersWhisperVisuals.ToWhite(AethersWhisperVisuals.ShimmerCyan, 0.5f), Radius / 260f + 0.15f, 12));
            GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, Vector2.Zero,
                AethersWhisperVisuals.PulseRingAltTex, false, 16, Radius / 900f, AethersWhisperVisuals.ShimmerCyan,
                new Vector2(1f, 1f), true, false, shrinkSpeed: -0.15f));

            // 硬光方块碎屑：晶核被拆解成规律晶片（军械库 GlowSquareFading）。
            int shards = 4 + (int)(Radius / 32f);
            for (int i = 0; i < shards; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / shards).ToRotationVector2() * Main.rand.NextFloat(3f, 7f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, vel,
                    AethersWhisperVisuals.GlowSquareTex, false, Main.rand.Next(14, 22), Main.rand.NextFloat(0.08f, 0.13f),
                    AethersWhisperVisuals.Lerp(Main.rand.NextFloat()), new Vector2(1f, 1f), true, false, spin: Main.rand.NextFloat(-0.25f, 0.25f)));
            }
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? AethersWhisperVisuals.ElectricDust : AethersWhisperVisuals.HardLightDust,
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f), 40, AethersWhisperVisuals.Lerp(Main.rand.NextFloat()), Main.rand.NextFloat(0.9f, 1.4f));
                d.noGravity = true;
                d.fadeIn = 0.4f;
            }
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, AethersWhisperVisuals.AetherPurple.ToVector3() * 0.6f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float progress = Age / (float)Lifetime;
            SpriteBatch sb = Main.spriteBatch;

            float open = AethersWhisperVisuals.ShockwaveExpand(Age / 5f);
            float fade = AethersWhisperVisuals.BurstFade(progress);

            // 全部走加色批次 + A=0：HollowCircleHardEdge/BloomCircle 都是黑底图，
            // 若在默认批次以非零 alpha 画会露出黑方块（本项目反复出现的坑）。
            AethersWhisperVisuals.BeginAdditive(sb);

            // 第一层：大小不等且不同心的三重深紫空心环轮廓。
            // 扩张走 4 次 PolyOut 冲击波曲线（前 30% 冲到位）、透明度走 cos 爆发淡出——
            // 用线性会变成匀速膨胀的肥皂泡，没有坍缩的力量感（第 0 篇曲线词汇表）。
            Texture2D ring = AethersWhisperVisuals.HollowRing.Value;
            Vector2 ringOrigin = ring.Size() * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                float r = Radius * open * (0.6f + i * 0.24f);
                Vector2 wobble = new Vector2(i - 1, (i % 2) - 0.5f) * 3f;
                float scale = r * 2f / ring.Width;
                sb.Draw(ring, pos + wobble, null, AethersWhisperVisuals.AetherPurple with { A = 0 } * (fade * 0.85f),
                    Projectile.identity * 0.3f + i, ringOrigin, scale, SpriteEffects.None, 0f);
            }

            // 第二层：白色核心向内缩成点 + 冷青外晕。
            Texture2D bloom = AethersWhisperVisuals.BloomCircle.Value;
            float coreScale = Radius / bloom.Width * MathHelper.Lerp(1.4f, 0.05f, progress);
            sb.Draw(bloom, pos, null, AethersWhisperVisuals.PearlWhite with { A = 0 } * fade,
                0f, bloom.Size() * 0.5f, coreScale, SpriteEffects.None, 0f);
            sb.Draw(bloom, pos, null, AethersWhisperVisuals.ShimmerCyan with { A = 0 } * (fade * 0.6f),
                0f, bloom.Size() * 0.5f, coreScale * 1.6f, SpriteEffects.None, 0f);

            AethersWhisperVisuals.EndAdditive(sb);

            return false;
        }
    }
}
