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
    // 无形土墙（由 AegisBladeThrown 落地后生成）。
    // 玩家碰撞由 AegisBladePlayer.PreUpdateMovement 负责。
    // Center 对应墙体的中心（垂直方向）。
    public class AegisWallProjectile : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // 每帧刷新，由 AegisBladePlayer 写入（检测 boss 存在则用短时间）
        private int wallLifeLeft;

        // 墙的像素尺寸
        public static readonly int WallHalfWidth  = BalanceAegisBlade.WallWidthTiles * 16 / 2;   // 16px
        public static readonly int WallHalfHeight = BalanceAegisBlade.WallHeightTiles * 16 / 2;  // 128px

        private static readonly Color WallGoldA = new(220, 170, 50, 40);
        private static readonly Color WallGoldB = new(255, 230, 120, 80);

        private bool initialized = false;

        public override void SetDefaults()
        {
            Projectile.width  = WallHalfWidth * 2;
            Projectile.height = WallHalfHeight * 2;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BalanceAegisBlade.WallDuration;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            initialized = true;
            // 给落地时间的音效
            SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.6f, Pitch = -0.4f }, Projectile.Center);
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            // 根据是否有 BOSS 决定时长
            bool bossAlive = false;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.boss) { bossAlive = true; break; }
            }
            // 初次设置时长
            if (initialized)
            {
                wallLifeLeft = bossAlive ? BalanceAegisBlade.WallDurationBoss : BalanceAegisBlade.WallDuration;
                Projectile.timeLeft = wallLifeLeft;
                initialized = false;
            }

            // 使用 bossAlive 动态时长：若 boss 消失时墙还在，不额外延长
            if (bossAlive && Projectile.timeLeft > BalanceAegisBlade.WallDurationBoss)
                Projectile.timeLeft = BalanceAegisBlade.WallDurationBoss;

            // 粒子沿墙体散发
            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                float randY = Main.rand.NextFloat(-WallHalfHeight, WallHalfHeight);
                int side = Main.rand.NextBool() ? -WallHalfWidth : WallHalfWidth;
                Vector2 particlePos = Projectile.Center + new Vector2(side, randY);
                Vector2 vel = new Vector2(side > 0 ? 1f : -1f, -0.5f) * Main.rand.NextFloat(0.4f, 1.2f);
                Dust dust = Dust.NewDustPerfect(particlePos, DustID.GoldFlame, vel, 100, WallGoldB, 0.7f);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float fade = System.MathF.Min(1f, Projectile.timeLeft / 60f);

            Vector2 center = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // 绘制竖向光柱（由若干拉伸的 bloom 叠加）
            int steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float t = (float)i / (steps - 1);
                Vector2 pos = center + new Vector2(0f, MathHelper.Lerp(-WallHalfHeight, WallHalfHeight, t));
                float widthScale = 0.25f + 0.05f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 3f + i);
                float heightScale = (1f / steps) * 2.2f;
                Color col = Color.Lerp(WallGoldA, WallGoldB, t) with { A = 0 };
                Main.EntitySpriteDraw(bloom, pos, null, col * fade * 0.8f,
                    0f, bloom.Size() * 0.5f,
                    new Vector2(widthScale, heightScale) * (WallHalfHeight / 40f),
                    SpriteEffects.None, 0);
            }

            // 顶端光晕
            Vector2 top = center - new Vector2(0f, WallHalfHeight);
            Main.EntitySpriteDraw(bloom, top, null, WallGoldB with { A = 0 } * fade * 0.6f,
                0f, bloom.Size() * 0.5f, 0.5f, SpriteEffects.None, 0);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
