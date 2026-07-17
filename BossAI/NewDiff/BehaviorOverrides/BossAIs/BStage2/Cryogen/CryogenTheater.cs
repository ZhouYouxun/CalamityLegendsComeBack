using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage2.Cryogen
{
    // =====================================================================================================================
    // FROST MOTE — the "陪跑弹" / theater bullet.
    //
    // 设计意图（东方式填屏原理）：这枚弹幕存在的意义不是"打中玩家"，而是"让屏幕看起来要命"。
    // 它成环喷出、大部分注定飞过玩家身边（限制距离、界定空间），伤害很低。
    //
    // 可读性铁律：它被【故意】画得比威胁弹暗、细、软——没有白色高亮核心、没有威胁弹那种 8×描边爆亮。
    // 这样玩家的眼睛会自动把它当"背景暴雪"忽略，而把注意力留给真正会致命的威胁弹。缺了这层分层，
    // 加弹就是灾难；有了这层分层，加弹才是"乱得合理"。
    //
    // ai[0] = 色相变体（0 = P1 青冰，1 = P2 紫）。
    // 性能：AI 近乎空转、绘制仅 1 个 bloom + 1 条流线 + 短暗拖尾，低配机友好。
    // =====================================================================================================================
    public class CryogenFrostMote : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 120;
        private int Age => Lifetime - Projectile.timeLeft;
        private bool Violet => Projectile.ai[0] == 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 7;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            for (int i = 0; i < Projectile.oldPos.Length; i++)
                Projectile.oldPos[i] = Projectile.position;
        }

        // 短暂"展开"缓冲后才能命中——陪跑弹也讲基本的公平，刚出膛不打人
        public override bool? CanDamage() => Age > 8 ? null : (bool?)false;

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            // 极轻的正弦侧摆——让弹流"活"一点，但绝不改变大方向（不追踪）
            float sway = MathF.Sin((Age + Projectile.identity * 5f) * 0.08f) * 0.006f;
            Projectile.velocity = Projectile.velocity.RotatedBy(sway);

            Color light = Violet ? new Color(120, 70, 190) : new Color(70, 150, 210);
            Lighting.AddLight(Projectile.Center, light.ToVector3() * 0.18f);

            // 拖尾冰尘：刻意稀疏（低配友好），且只在中段生命喷，末段淡出时不喷
            if (!Main.dedServ && Age % 7 == 0 && Projectile.timeLeft > 24)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, Violet ? DustID.Vortex : DustID.Ice,
                    -Projectile.velocity * 0.1f, 150, Violet ? Color.MediumPurple : Color.Cyan, 0.7f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 刻意压暗、压细：这是陪跑弹，不能抢威胁弹的视觉优先级
            Color tint = Violet ? new Color(150, 95, 225) : new Color(110, 195, 245);
            float fade = MathHelper.Clamp(Age / 8f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / 22f, 0f, 1f);
            tint *= 0.55f * fade; // 半透明——比威胁弹暗一档

            // 短暗拖尾
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i - 1] == Vector2.Zero)
                    continue;
                float pct = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 a = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 b = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.spriteBatch.DrawLineBetter(a + Main.screenPosition, b + Main.screenPosition, tint * (pct * 0.5f), pct * 2.4f);
            }

            // 软 bloom（低亮度）+ 细核心流线——注意：无白色高亮核心，这是与威胁弹的关键区分
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 screen = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(bloom, screen, null, tint * 0.6f, 0f, bloom.Size() * 0.5f, 0.09f, SpriteEffects.None, 0f);
            Main.spriteBatch.DrawLineBetter(
                Projectile.Center - forward * 7f, Projectile.Center + forward * 5f, tint, 1.6f);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;
            // 极小的消散——不喧宾夺主
            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, Violet ? DustID.Vortex : DustID.Ice,
                    Main.rand.NextVector2Circular(1.5f, 1.5f), 150, Violet ? Color.MediumPurple : Color.Cyan, 0.6f);
                d.noGravity = true;
            }
        }
    }
}
