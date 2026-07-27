using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper
{
    /// <summary>
    /// 微光坍缩：左键晶核在命中点触发的一圈坍缩（文档第 3.3 节，无伤害视觉 + 可选 AoE）。
    /// 直击命中时 damage &gt; 0：一圈 30% 直击伤害、排除直击目标；撞墙时 damage == 0：纯视觉。
    /// 视觉只有三层：面向镜头的六边形轮廓 → 白色核心内缩成点 → 2–4 片短晶屑。
    /// 不用火焰爆炸 / 圆形烟雾 / 血肉碎片 / 雷链 / 大范围尘埃。
    /// （占位资产：六边形轮廓暂用 HollowCircleHardEdge，正式请换 Assets/AetherCollapseGlyph.png。）
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
            // 第三层：2–4 片短晶屑（0.2 秒内消失）。
            int shards = Main.rand.Next(2, 5);
            for (int i = 0; i < shards; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 5f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, vel, 40, AethersWhisperVisuals.AetherPurple, Main.rand.NextFloat(0.9f, 1.4f));
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

            // 第一层：面向镜头、大小不等且不同心的三重六边形轮廓（深紫，AlphaBlend）。
            Texture2D ring = AethersWhisperVisuals.HollowRing.Value;
            Vector2 ringOrigin = ring.Size() * 0.5f;
            float open = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(Age / 5f, 0f, 1f));
            float fade = 1f - progress;
            for (int i = 0; i < 3; i++)
            {
                float r = Radius * open * (0.6f + i * 0.24f);
                Vector2 wobble = new Vector2(i - 1, (i % 2) - 0.5f) * 3f;
                float scale = r * 2f / ring.Width;
                sb.Draw(ring, pos + wobble, null, AethersWhisperVisuals.AetherPurple * (fade * 0.7f),
                    Projectile.identity * 0.3f + i, ringOrigin, scale, SpriteEffects.None, 0f);
            }

            // 第二层：白色核心向内缩成点（加色）。
            AethersWhisperVisuals.BeginAdditive(sb);
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
