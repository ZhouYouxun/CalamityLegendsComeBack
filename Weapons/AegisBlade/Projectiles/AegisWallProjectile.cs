using System;
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
    // 从地面升起的伤害性土墙。
    // ai[0] = 方向（-1左 / +1右，仅用于粒子偏向）
    // ai[1] = 阶段（0=上升/可造成伤害，1=已竖立/仅阻挡）
    //
    // 上升阶段（WallRiseTime帧）：以初始速度向上运动，对路径中的敌人造成伤害
    // 竖立阶段：停止运动，不再伤害，但阻挡玩家横向移动（由 AegisBladePlayer 实现），逐渐淡出后消失
    public class AegisWallProjectile : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // 玩家碰撞系统读取这两个静态值
        public static readonly int WallHalfWidth  = BalanceAegisBlade.WallWidthTiles  * 16 / 2;  // 16px
        public static readonly int WallHalfHeight = BalanceAegisBlade.WallHeightTiles * 16 / 2;  // 128px

        // ai 引用
        private ref float Side  => ref Projectile.ai[0];   // -1 or +1
        private ref float Phase => ref Projectile.ai[1];   // 0=rising, 1=solid

        // 本地计时器
        private ref float RiseTimer     => ref Projectile.localAI[0];
        private ref float SolidTimer    => ref Projectile.localAI[1];

        private static readonly Color WallGoldA = new(220, 170, 50, 40);
        private static readonly Color WallGoldB = new(255, 230, 120, 80);
        private static readonly Color WallWhite = new(255, 250, 200, 0);

        public override void SetDefaults()
        {
            Projectile.width  = WallHalfWidth  * 2;
            Projectile.height = WallHalfHeight * 2;
            Projectile.friendly    = true;   // Phase 0 造成伤害
            Projectile.DamageType  = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate   = -1;
            Projectile.timeLeft    = BalanceAegisBlade.WallRiseTime + BalanceAegisBlade.WallDuration;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity   = true;
            Projectile.localNPCHitCooldown    = 12;
        }

        public override bool ShouldUpdatePosition() => Phase < 1f; // 上升阶段自动移动，竖立后静止

        public override void AI()
        {
            if (Phase < 1f)
            {
                // ── 上升阶段 ──────────────────────────────────────────────
                RiseTimer++;

                if (RiseTimer >= BalanceAegisBlade.WallRiseTime)
                {
                    Phase = 1f;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.friendly = false;
                    // 上升到位时的轰隆声
                    SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.7f, Pitch = -0.5f }, Projectile.Center);
                }

                // 上升时的土石粒子（从底部喷出）
                if (!Main.dedServ && Main.rand.NextBool(2)) EmitRisingDust();
            }
            else
            {
                // ── 竖立阶段 ──────────────────────────────────────────────
                SolidTimer++;

                // BOSS战中墙的存在时间缩短
                bool bossAlive = false;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (npc.boss) { bossAlive = true; break; }
                }
                int solidLimit = bossAlive ? BalanceAegisBlade.WallDurationBoss : BalanceAegisBlade.WallDuration;
                if (SolidTimer >= solidLimit) Projectile.Kill();

                // 金色粒子沿墙体散发
                if (!Main.dedServ && Main.rand.NextBool(5)) EmitSolidParticle();
            }
        }

        private void EmitRisingDust()
        {
            // 从底部（当前center+halfH）喷射土石粒子
            float wallBottomY = Projectile.Center.Y + WallHalfHeight;
            Vector2 pos = new Vector2(Projectile.Center.X + Side * Main.rand.NextFloat(0f, WallHalfWidth),
                                      wallBottomY);
            Vector2 vel = new Vector2(Side * Main.rand.NextFloat(1f, 4f), -Main.rand.NextFloat(3f, 9f));
            Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, vel, 0, WallGoldB, 1.1f);
            d.noGravity = true;
        }

        private void EmitSolidParticle()
        {
            float randY = Main.rand.NextFloat(-WallHalfHeight, WallHalfHeight);
            int   sideX = Main.rand.NextBool() ? -WallHalfWidth : WallHalfWidth;
            Vector2 pos = Projectile.Center + new Vector2(sideX, randY);
            Vector2 vel = new Vector2(sideX > 0 ? 0.8f : -0.8f, -0.5f) * Main.rand.NextFloat(0.5f, 1.5f);
            Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, vel, 120, WallGoldA, 0.6f);
            d.noGravity = true;
        }

        // ── 伤害判定 ──────────────────────────────────────────────────────

        public override bool? CanDamage() => Phase < 1f ? null : false;

        // ── 绘制 ──────────────────────────────────────────────────────────

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center  = Projectile.Center - Main.screenPosition;

            // 淡入淡出
            float fade;
            if (Phase < 1f)
            {
                // 上升阶段：逐渐显现
                fade = RiseTimer / BalanceAegisBlade.WallRiseTime;
            }
            else
            {
                // 竖立阶段：末期淡出
                bool bossAlive = false;
                foreach (NPC npc in Main.ActiveNPCs)
                    if (npc.boss) { bossAlive = true; break; }
                int solidLimit = bossAlive ? BalanceAegisBlade.WallDurationBoss : BalanceAegisBlade.WallDuration;
                fade = MathF.Min(1f, (solidLimit - SolidTimer) / 60f);
            }

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // 竖向光柱（由叠加的拉伸bloom构成）
            int segments = 10;
            for (int i = 0; i < segments; i++)
            {
                float t    = (float)i / (segments - 1);
                // 上升阶段：只渲染已升起的部分（从底部向上逐渐显现）
                float riseProgress = Phase < 1f ? MathF.Min(1f, (RiseTimer * 1.1f) / BalanceAegisBlade.WallRiseTime) : 1f;
                if (t < (1f - riseProgress)) continue;  // 跳过未升起的上方部分

                Vector2 pos       = center + new Vector2(0f, MathHelper.Lerp(-WallHalfHeight, WallHalfHeight, t));
                float widthScale  = 0.18f + 0.04f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.5f + i * 0.7f);
                float heightScale = 1f / segments * 2.4f;
                Color col = Color.Lerp(WallGoldA, WallGoldB, 1f - t) with { A = 0 };

                Main.EntitySpriteDraw(bloom, pos, null, col * fade * 0.9f,
                    0f, bloom.Size() * 0.5f,
                    new Vector2(widthScale, heightScale) * (WallHalfHeight / 38f),
                    SpriteEffects.None, 0);
            }

            // 顶端光晕（升起完成后）
            if (Phase >= 1f || RiseTimer >= BalanceAegisBlade.WallRiseTime * 0.7f)
            {
                Vector2 top = center - new Vector2(0f, WallHalfHeight);
                Main.EntitySpriteDraw(bloom, top, null,
                    WallWhite * fade * 0.6f,
                    0f, bloom.Size() * 0.5f, 0.55f, SpriteEffects.None, 0);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
