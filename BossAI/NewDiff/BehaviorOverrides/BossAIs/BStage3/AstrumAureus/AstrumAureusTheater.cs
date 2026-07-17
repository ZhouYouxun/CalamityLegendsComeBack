using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.AstrumAureus
{
    // =====================================================================================================================
    // STAR MOTE — 白金星舰的"陪跑弹" / theater bullet。
    //
    // 与 Cryogen 的 FrostMote 同一套设计原理（东方式填屏），只换成本 Boss 的星域配色：
    // P1 金 (230,200,60)，P2 紫 (160,60,220)。成环喷出、大部分注定飞过玩家、伤害很低。
    //
    // 可读性铁律：刻意画得比威胁弹暗、细、软。本 Boss 的威胁弹走 AureusFx.DrawBackglow 的
    // 12× 偏移爆亮描边；陪跑弹【绝不】用它——只有一层柔 bloom + 一条细流线，没有白色高亮核心。
    // 玩家的眼睛会自动把它当背景星尘忽略，把注意力留给真正致命的东西。
    //
    // ai[0] = 色相变体（0 = P1 金，1 = P2 紫）。
    // =====================================================================================================================
    public class AureusStarMote : ModProjectile
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

        // 出膛缓冲——陪跑弹也讲基本公平
        public override bool? CanDamage() => Age > 8 ? null : (bool?)false;

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            // 极轻侧摆，让弹流"活"一点，但绝不改变大方向（不追踪）
            Projectile.velocity = Projectile.velocity.RotatedBy(
                MathF.Sin((Age + Projectile.identity * 5f) * 0.08f) * 0.006f);

            Color light = Violet ? new Color(110, 45, 160) : new Color(180, 150, 45);
            Lighting.AddLight(Projectile.Center, light.ToVector3() * 0.18f);

            // 拖尾星尘：刻意稀疏（低配友好），末段淡出时不喷
            if (!Main.dedServ && Age % 7 == 0 && Projectile.timeLeft > 24)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, Violet ? DustID.PurpleTorch : DustID.GoldFlame,
                    -Projectile.velocity * 0.1f, 150, default, 0.7f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 压暗、压细：陪跑弹不能抢威胁弹的视觉优先级
            Color tint = Violet ? new Color(165, 90, 235) : new Color(240, 210, 90);
            float fade = MathHelper.Clamp(Age / 8f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / 22f, 0f, 1f);
            tint *= 0.55f * fade;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i - 1] == Vector2.Zero)
                    continue;
                float pct = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 a = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 b = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f;
                Main.spriteBatch.DrawLineBetter(a, b, tint * (pct * 0.5f), pct * 2.4f);
            }

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, tint * 0.6f, 0f,
                bloom.Size() * 0.5f, 0.09f, SpriteEffects.None, 0f);
            Main.spriteBatch.DrawLineBetter(
                Projectile.Center - forward * 7f, Projectile.Center + forward * 5f, tint, 1.6f);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, Violet ? DustID.PurpleTorch : DustID.GoldFlame,
                    Main.rand.NextVector2Circular(1.5f, 1.5f), 150, default, 0.6f);
                d.noGravity = true;
            }
        }
    }
}
