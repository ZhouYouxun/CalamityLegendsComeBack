using System;
using CalamityLegendsComeBack.Weapons.AegisBlade.Visuals;
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
    // A fast-growing compact dirt barricade. It damages during the rise, then becomes a player-only solid wall.
    //
    // 视觉重做说明：旧版用 TextureAssets.MagicPixel 画矩形描边 + 扫描线 + 侧边刻度，
    // 是一套"科技矩阵"语言，和这把武器的亵渎圣火/圣岩主题完全不搭。
    // 现在改为「亵渎圣岩壁垒」：分段堆叠的岩体贴图 + 段与段之间的炽金裂缝 +
    // 顶部符文封印 + 底部炉光，玩法/碰撞/时长完全不变。
    public class AegisWallProjectile : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public static readonly int WallHalfWidth = BalanceAegisBlade.WallWidthTiles * 16 / 2;
        public static readonly int WallHalfHeight = BalanceAegisBlade.WallHeightTiles * 16 / 2;

        private const int StoneSegments = 7;      // 岩体分几段堆叠

        private ref float Phase => ref Projectile.ai[1];
        private ref float RiseTimer => ref Projectile.localAI[0];
        private ref float SolidTimer => ref Projectile.localAI[1];
        private bool solidifyEffectFired;

        // 岩体配色：暗褐岩石 + 炽金裂缝，属于统一调色盘的"暗部/底层"延伸
        private static readonly Color StoneDark = new(74, 48, 30);
        private static readonly Color StoneLit = new(138, 96, 54);

        public override void SetDefaults()
        {
            Projectile.width = WallHalfWidth * 2;
            Projectile.height = WallHalfHeight * 2;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BalanceAegisBlade.WallRiseTime + BalanceAegisBlade.WallDuration;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override bool ShouldUpdatePosition() => Phase < 1f;

        public override void AI()
        {
            if (Phase < 1f)
            {
                RiseTimer++;
                AegisVisuals.Light(Projectile.Center, 0.55f);
                if (RiseTimer == 1f || RiseTimer % 5f == 0f)
                    SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.46f, Pitch = -0.34f, MaxInstances = 4 }, Projectile.Center);
                if (!Main.dedServ && Main.rand.NextBool(2))
                    EmitRisingDirt();

                if (RiseTimer < BalanceAegisBlade.WallRiseTime)
                    return;

                Phase = 1f;
                Projectile.velocity = Vector2.Zero;
                Projectile.friendly = false;
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.72f, Pitch = -0.35f }, Projectile.Center);
                EmitSolidifyBurst();
                return;
            }

            SolidTimer++;
            AegisVisuals.Light(Projectile.Center, 0.32f);
            if (SolidTimer >= GetSolidDuration())
            {
                Projectile.Kill();
                return;
            }

            // 阻挡普通敌对小怪（非Boss敌人），BOSS与玩家不受影响
            Rectangle wallBox = new(
                (int)(Projectile.Center.X - WallHalfWidth),
                (int)(Projectile.Center.Y - WallHalfHeight),
                WallHalfWidth * 2,
                WallHalfHeight * 2);

            // 玩家工具一击清理机制：当玩家使用镐子/斧头/锤子挥击触碰土墙时，土墙瞬间摧毁
            Player owner = Main.player[Projectile.owner];
            if (owner.active && !owner.dead && owner.itemAnimation > 0)
            {
                Item heldItem = owner.HeldItem;
                if (heldItem != null && (heldItem.pick > 0 || heldItem.axe > 0 || heldItem.hammer > 0))
                {
                    Rectangle toolHitbox = new(
                        (int)(owner.itemLocation.X - 24),
                        (int)(owner.itemLocation.Y - 24),
                        48 + heldItem.width,
                        48 + heldItem.height);

                    if (toolHitbox.Intersects(wallBox))
                    {
                        SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.9f, Pitch = -0.2f }, Projectile.Center);
                        EmitCollapseBurst();
                        Projectile.Kill();
                        return;
                    }
                }
            }

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || npc.friendly || npc.boss || npc.dontTakeDamage || npc.damage <= 0)
                    continue;

                if (npc.Hitbox.Intersects(wallBox))
                {
                    if (npc.Center.X < Projectile.Center.X)
                    {
                        npc.position.X = wallBox.Left - npc.width;
                        if (npc.velocity.X > 0f) npc.velocity.X = 0f;
                    }
                    else
                    {
                        npc.position.X = wallBox.Right;
                        if (npc.velocity.X < 0f) npc.velocity.X = 0f;
                    }

                    // 被挡住的瞬间在接触点擦出火星，让"撞墙"这件事有反馈
                    if (!Main.dedServ && Main.rand.NextBool(3))
                    {
                        float contactX = npc.Center.X < Projectile.Center.X ? wallBox.Left : wallBox.Right;
                        Vector2 contact = new(contactX, MathHelper.Clamp(npc.Center.Y, wallBox.Top, wallBox.Bottom));
                        Vector2 push = new(npc.Center.X < Projectile.Center.X ? -1f : 1f, 0f);
                        AegisVisuals.EmberJet(contact, push, 2, 0.5f, 0.9f);
                    }
                }
            }

            if (!Main.dedServ && Main.rand.NextBool(4))
                EmitSolidDirt();
        }

        private static int GetSolidDuration()
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.boss)
                    return BalanceAegisBlade.WallDurationBoss;
            }
            return BalanceAegisBlade.WallDuration;
        }

        /// <summary>升起时从底部喷出的岩屑与圣火尘。</summary>
        private void EmitRisingDirt()
        {
            float bottomY = Projectile.Center.Y + WallHalfHeight;
            Vector2 position = new(Projectile.Center.X + Main.rand.NextFloat(-WallHalfWidth, WallHalfWidth), bottomY);
            Vector2 velocity = new(Main.rand.NextFloat(-1.8f, 1.8f), -Main.rand.NextFloat(2.5f, 7.5f));

            Dust dust = Dust.NewDustPerfect(position, DustID.Dirt, velocity * 0.65f, 0, StoneDark,
                Main.rand.NextFloat(0.9f, 1.35f));
            dust.noGravity = Main.rand.NextBool(4);

            GeneralParticleHandler.SpawnParticle(new StoneDebrisParticle(position, velocity * 0.75f,
                Color.Lerp(StoneDark, StoneLit, Main.rand.NextFloat()), Main.rand.NextFloat(0.7f, 1.15f),
                Main.rand.Next(26, 42), Main.rand.NextFloat(0.4f, 1.3f)));

            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(position, velocity,
                Color.Lerp(StoneDark, Color.DarkSlateGray, Main.rand.NextFloat(0.3f, 0.8f)), Color.Transparent,
                Main.rand.NextFloat(0.36f, 0.68f), Main.rand.Next(18, 28), Main.rand.NextFloat(-0.05f, 0.05f)));

            // 从裂缝里被挤出来的圣火
            if (Main.rand.NextBool(2))
            {
                Dust ember = Dust.NewDustPerfect(position, AegisVisuals.ProfanedFireDust,
                    velocity * 0.4f, 0, Color.White, Main.rand.NextFloat(0.8f, 1.4f));
                ember.noGravity = true;
            }
        }

        /// <summary>已成形期间沿墙面渗出的余烬。</summary>
        private void EmitSolidDirt()
        {
            float height = Main.rand.NextFloat(-WallHalfHeight, WallHalfHeight);
            float side = Main.rand.NextBool() ? -WallHalfWidth : WallHalfWidth;
            Vector2 position = Projectile.Center + new Vector2(side, height);

            Dust dust = Dust.NewDustPerfect(position, DustID.Dirt,
                new Vector2(Math.Sign(side) * Main.rand.NextFloat(0.3f, 1.2f), -Main.rand.NextFloat(0.1f, 0.9f)),
                0, StoneDark, Main.rand.NextFloat(0.65f, 1.05f));
            dust.noGravity = Main.rand.NextBool(5);

            AegisVisuals.EmberDrip(position, 3f, 6f, 0.7f);
        }

        /// <summary>凝固：符文封印咬合，整堵墙的裂缝同时亮一次。</summary>
        private void EmitSolidifyBurst()
        {
            if (solidifyEffectFired || Main.dedServ)
                return;

            solidifyEffectFired = true;
            Vector2 top = Projectile.Center - Vector2.UnitY * WallHalfHeight;

            AegisVisuals.HolyDetonation(top, 0.85f, false);
            AegisVisuals.CoronaRing(top, 10, 0.7f);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero,
                AegisVisuals.Add(AegisVisuals.Gold, 0.9f), new Vector2(0.5f, 1.9f), 0f, 0.06f, 0.9f, 20));

            for (int i = 0; i < 8; i++)
            {
                Vector2 position = Projectile.Center + new Vector2(Main.rand.NextFloat(-WallHalfWidth, WallHalfWidth),
                    Main.rand.NextFloat(-WallHalfHeight, WallHalfHeight));
                GeneralParticleHandler.SpawnParticle(new StrongBloom(position, Vector2.Zero,
                    AegisVisuals.Add(Main.rand.NextBool(3) ? AegisVisuals.Core : AegisVisuals.Gold, 1f),
                    Main.rand.NextFloat(0.16f, 0.3f), Main.rand.Next(9, 15)));
            }

            AegisVisuals.Screenshake(Projectile.Center, 1.6f, 600f);
        }

        /// <summary>被镐/斧/锤敲碎：岩体真的碎开成块，而不是简单闪一下。</summary>
        private void EmitCollapseBurst()
        {
            if (Main.dedServ)
                return;

            AegisVisuals.HolyDetonation(Projectile.Center, 1.1f);

            for (int i = 0; i < 18; i++)
            {
                Vector2 position = Projectile.Center + new Vector2(Main.rand.NextFloat(-WallHalfWidth, WallHalfWidth),
                    Main.rand.NextFloat(-WallHalfHeight, WallHalfHeight));
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f) -
                                   Vector2.UnitY * 1.5f;

                GeneralParticleHandler.SpawnParticle(new StoneDebrisParticle(position, velocity,
                    Color.Lerp(StoneDark, StoneLit, Main.rand.NextFloat()), Main.rand.NextFloat(0.8f, 1.3f),
                    Main.rand.Next(30, 50), Main.rand.NextFloat(0.5f, 1.6f)));

                Dust dust = Dust.NewDustPerfect(position, DustID.Dirt, velocity * 0.8f, 0, StoneDark,
                    Main.rand.NextFloat(0.9f, 1.4f));
                dust.noGravity = false;
            }
        }

        public override bool? CanDamage() => Phase < 1f ? null : false;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Vector2 center = Projectile.Center - Main.screenPosition;
            float solidDuration = GetSolidDuration();
            float fade = Phase < 1f
                ? MathHelper.Clamp(RiseTimer / BalanceAegisBlade.WallRiseTime, 0f, 1f)
                : MathHelper.Clamp((solidDuration - SolidTimer) / 60f, 0f, 1f);
            float riseProgress = Phase < 1f
                ? MathHelper.Clamp(RiseTimer / BalanceAegisBlade.WallRiseTime, 0f, 1f)
                : 1f;

            if (fade <= 0.01f)
                return false;

            DrawStoneBody(center, fade, riseProgress, lightColor);
            DrawGlowingSeams(center, fade, riseProgress);
            return false;
        }

        /// <summary>岩体：自下而上分段堆叠的碎岩贴图，每段有固定的旋转/翻转，看起来是"垒"起来的。</summary>
        private void DrawStoneBody(Vector2 center, float fade, float riseProgress, Color lightColor)
        {
            Texture2D[] stones =
            {
                AegisVisuals.Tex(AegisVisuals.TexRockB),
                AegisVisuals.Tex(AegisVisuals.TexRockA),
                AegisVisuals.Tex(AegisVisuals.TexRockC),
            };

            float segmentHeight = WallHalfHeight * 2f / StoneSegments;
            int seed = Projectile.identity;

            for (int i = 0; i < StoneSegments; i++)
            {
                // completion 0 = 顶端，1 = 底端。升起时从底往顶逐段出现。
                float completion = i / (float)(StoneSegments - 1);
                if (completion < 1f - riseProgress)
                    continue;

                Texture2D stone = stones[(i + seed) % stones.Length];
                Vector2 position = center + new Vector2(
                    ((i + seed) % 3 - 1) * 2.4f,                        // 每段轻微左右错位，避免笔直呆板
                    MathHelper.Lerp(-WallHalfHeight, WallHalfHeight, completion));

                float rotation = ((i * 37 + seed * 13) % 360) * MathHelper.TwoPi / 360f;
                SpriteEffects flip = ((i + seed) % 2 == 0) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                // 顶亮底暗，让墙有上下光照层次
                Color stoneColor = Color.Lerp(StoneLit, StoneDark, completion * 0.75f);
                stoneColor = Color.Lerp(stoneColor, lightColor.MultiplyRGB(stoneColor), 0.45f);

                Main.EntitySpriteDraw(stone, position, null, stoneColor * fade, rotation,
                    stone.Size() * 0.5f,
                    new Vector2(AegisVisuals.RadiusScale(stone, WallHalfWidth * 1.22f),
                                AegisVisuals.RadiusScale(stone, segmentHeight * 0.78f)),
                    flip, 0);
            }
        }

        /// <summary>炽金裂缝、顶部符文封印与底部炉光 —— 这堵墙是被圣火烧结起来的。</summary>
        private void DrawGlowingSeams(Vector2 center, float fade, float riseProgress)
        {
            Texture2D cracks = AegisVisuals.Tex(AegisVisuals.TexScorch);
            Texture2D bloom = AegisVisuals.Tex(AegisVisuals.TexBloom);
            float time = Main.GlobalTimeWrappedHourly;
            float segmentHeight = WallHalfHeight * 2f / StoneSegments;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            // ① 段与段之间的炽金接缝：脉动，像还没冷却的熔接口
            for (int i = 0; i < StoneSegments - 1; i++)
            {
                float completion = (i + 0.5f) / (StoneSegments - 1f);
                if (completion < 1f - riseProgress)
                    continue;

                Vector2 seamPosition = center + new Vector2(0f,
                    MathHelper.Lerp(-WallHalfHeight, WallHalfHeight, completion));
                float pulse = 0.55f + 0.45f * MathF.Sin(time * 3.4f + i * 1.15f);

                Main.EntitySpriteDraw(cracks, seamPosition, null,
                    AegisVisuals.Add(AegisVisuals.Flame, 0.34f * fade * pulse),
                    (i % 2 == 0 ? 1f : -1f) * 0.35f, cracks.Size() * 0.5f,
                    new Vector2(AegisVisuals.RadiusScale(cracks, WallHalfWidth * 1.15f),
                                AegisVisuals.RadiusScale(cracks, segmentHeight * 0.34f)),
                    SpriteEffects.None, 0);

                Main.EntitySpriteDraw(bloom, seamPosition, null,
                    AegisVisuals.Add(AegisVisuals.Ember, 0.26f * fade * pulse),
                    0f, bloom.Size() * 0.5f,
                    new Vector2(AegisVisuals.RadiusScale(bloom, WallHalfWidth * 0.95f),
                                AegisVisuals.RadiusScale(bloom, segmentHeight * 0.22f)),
                    SpriteEffects.None, 0);
            }

            // ② 侧缘的余烬轮廓：让墙从背景里"切"出来
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 edgePosition = center + new Vector2(side * WallHalfWidth * 0.92f,
                    MathHelper.Lerp(WallHalfHeight, -WallHalfHeight, riseProgress * 0.5f));
                Main.EntitySpriteDraw(bloom, edgePosition, null,
                    AegisVisuals.Add(AegisVisuals.Ember, 0.22f * fade),
                    0f, bloom.Size() * 0.5f,
                    new Vector2(AegisVisuals.RadiusScale(bloom, 5f),
                                AegisVisuals.RadiusScale(bloom, WallHalfHeight * riseProgress)),
                    SpriteEffects.None, 0);
            }

            // ③ 底部炉光：墙是从这里被顶上来的
            Vector2 basePosition = center + Vector2.UnitY * WallHalfHeight;
            float basePulse = 0.75f + 0.25f * MathF.Sin(time * 5f);
            Main.EntitySpriteDraw(bloom, basePosition, null,
                AegisVisuals.Add(AegisVisuals.Flame, 0.5f * fade * basePulse),
                0f, bloom.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(bloom, WallHalfWidth * 1.5f),
                            AegisVisuals.RadiusScale(bloom, 14f)),
                SpriteEffects.None, 0);

            // ④ 顶部符文封印：升到七成后才盖章，作为"这堵墙成形了"的标记
            if (riseProgress >= 0.7f)
            {
                Vector2 topPosition = center - Vector2.UnitY * WallHalfHeight;
                float sealStrength = Utils.GetLerpValue(0.7f, 1f, riseProgress, true) * fade;
                AegisVisuals.DrawRuneSigil(topPosition, WallHalfWidth * 1.35f,
                    time * 0.9f, sealStrength * 0.85f, new Vector2(1f, 0.5f), 1f);
                Main.EntitySpriteDraw(bloom, topPosition, null,
                    AegisVisuals.Add(AegisVisuals.Core, 0.45f * sealStrength),
                    0f, bloom.Size() * 0.5f,
                    new Vector2(AegisVisuals.RadiusScale(bloom, WallHalfWidth * 0.8f),
                                AegisVisuals.RadiusScale(bloom, 10f)),
                    SpriteEffects.None, 0);
            }

            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
