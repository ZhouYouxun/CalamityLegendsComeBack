using System;
using System.Collections.Generic;
using System.Linq;
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
    // 速凝掩体：玩家阻挡墙壁。
    // 限制上限最多同时存在 2 个（产生第 3 个时第 1 个自动摧毁）。
    // 视觉效果：干净清爽的能量屏障双层描边线框（参考 CalamitasClone ArenaWall），去除杂乱矩阵与碎石纹理。
    public class AegisWallProjectile : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public static readonly int WallHalfWidth = BalanceAegisBlade.WallWidthTiles * 16 / 2;
        public static readonly int WallHalfHeight = BalanceAegisBlade.WallHeightTiles * 16 / 2;

        private ref float Phase => ref Projectile.ai[1];
        private ref float RiseTimer => ref Projectile.localAI[0];
        private ref float SolidTimer => ref Projectile.localAI[1];
        private bool solidifyEffectFired;

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

        private void LimitWallCount()
        {
            int wallType = Projectile.type;
            int myOwner = Projectile.owner;
            List<Projectile> activeWalls = new();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == wallType && p.owner == myOwner && p.whoAmI != Projectile.whoAmI)
                {
                    activeWalls.Add(p);
                }
            }
            // 当加上自己后超过 2 个，自动销毁最老的一个掩体
            while (activeWalls.Count >= 2)
            {
                Projectile oldest = activeWalls.OrderBy(p => p.timeLeft).First();
                oldest.Kill();
                activeWalls.Remove(oldest);
            }
        }

        public override void AI()
        {
            if (Phase < 1f)
            {
                RiseTimer++;
                if (RiseTimer == 1f)
                {
                    LimitWallCount();
                    SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.46f, Pitch = -0.34f, MaxInstances = 4 }, Projectile.Center);
                }
                else if (RiseTimer % 5f == 0f)
                {
                    SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.46f, Pitch = -0.34f, MaxInstances = 4 }, Projectile.Center);
                }

                AegisVisuals.Light(Projectile.Center, 0.55f);
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

        private void EmitRisingDirt()
        {
            float bottomY = Projectile.Center.Y + WallHalfHeight;
            Vector2 position = new(Projectile.Center.X + Main.rand.NextFloat(-WallHalfWidth, WallHalfWidth), bottomY);
            Vector2 velocity = new(Main.rand.NextFloat(-1.8f, 1.8f), -Main.rand.NextFloat(2.5f, 7.5f));

            Dust dust = Dust.NewDustPerfect(position, DustID.Dirt, velocity * 0.65f, 0, new Color(74, 48, 30),
                Main.rand.NextFloat(0.9f, 1.35f));
            dust.noGravity = Main.rand.NextBool(4);

            if (Main.rand.NextBool(2))
            {
                Dust ember = Dust.NewDustPerfect(position, AegisVisuals.ProfanedFireDust,
                    velocity * 0.4f, 0, Color.White, Main.rand.NextFloat(0.8f, 1.4f));
                ember.noGravity = true;
            }
        }

        private void EmitSolidDirt()
        {
            float height = Main.rand.NextFloat(-WallHalfHeight, WallHalfHeight);
            float side = Main.rand.NextBool() ? -WallHalfWidth : WallHalfWidth;
            Vector2 position = Projectile.Center + new Vector2(side, height);

            AegisVisuals.EmberDrip(position, 3f, 6f, 0.7f);
        }

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

            AegisVisuals.Screenshake(Projectile.Center, 1.6f, 600f);
        }

        private void EmitCollapseBurst()
        {
            if (Main.dedServ)
                return;

            AegisVisuals.HolyDetonation(Projectile.Center, 1.1f);
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

            DrawEnergyBarrierWall(center, fade, riseProgress);
            return false;
        }

        /// <summary>
        /// 模仿 CalamitasClone ArenaWall 的正宗动态扩散描边扫描线：
        /// 柔和衬底 + CalamitasClone 同款黄色动态多重扩散扫描线 (Expanding Border Scanlines) + 多级描边 + 角点辉光
        /// </summary>
        private void DrawEnergyBarrierWall(Vector2 center, float fade, float riseProgress)
        {
            Texture2D magicPixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
            Texture2D bloom = AegisVisuals.Tex(AegisVisuals.TexBloom);
            float time = Main.GlobalTimeWrappedHourly;

            float currentHeight = WallHalfHeight * 2f * riseProgress;
            float topY = center.Y + WallHalfHeight - currentHeight;
            float bottomY = center.Y + WallHalfHeight;
            float leftX = center.X - WallHalfWidth;
            float rightX = center.X + WallHalfWidth;

            Vector2 tl = new(leftX, topY);
            Vector2 tr = new(rightX, topY);
            Vector2 bl = new(leftX, bottomY);
            Vector2 br = new(rightX, bottomY);

            Color coreColor = AegisVisuals.Add(AegisVisuals.Core, fade * 0.95f);
            Color goldColor = AegisVisuals.Add(AegisVisuals.Gold, fade * 0.85f);
            Color flameColor = AegisVisuals.Add(AegisVisuals.Flame, fade * 0.5f);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            // 1. 屏障柔底
            Rectangle fillRect = new((int)leftX, (int)topY, (int)(rightX - leftX), (int)(bottomY - topY));
            Main.spriteBatch.Draw(magicPixel, fillRect, flameColor * 0.16f);

            // 2. CalamitasClone 同款动态黄金波幅扫描线 (Inner Border Clones / Expanding Scanlines)
            // 机制：根据 GlobalTimeWrappedHourly 动态计算多重矩形框向外/向内偏移与透明度衰减
            float amount = 5f;
            float totalDistance = 56f;
            float timePhase = (time * 1.2f) % 1f;

            for (float i = timePhase; i < amount; i += 1f)
            {
                float progress = i / amount; // 0 ~ 1
                float offset = totalDistance * progress;
                float alpha = (1f - progress) * fade * 0.7f;

                Color scanColor = AegisVisuals.Add(AegisVisuals.Gold, alpha);

                Vector2 stl = new(leftX - offset, topY - offset);
                Vector2 str = new(rightX + offset, topY - offset);
                Vector2 sbl = new(leftX - offset, bottomY + offset);
                Vector2 sbr = new(rightX + offset, bottomY + offset);

                float lineThick = MathHelper.Lerp(3f, 1f, progress);

                Main.spriteBatch.DrawLineBetter(stl, str, scanColor, lineThick);
                Main.spriteBatch.DrawLineBetter(stl, sbl, scanColor, lineThick);
                Main.spriteBatch.DrawLineBetter(str, sbr, scanColor, lineThick);
                Main.spriteBatch.DrawLineBetter(sbr, sbl, scanColor, lineThick);
            }

            // 同款向内收缩辅助扫描线 (Contracting Scanlines)
            for (float i = timePhase; i < 3f; i += 1f)
            {
                float progress = i / 3f;
                float innerOffset = MathHelper.Lerp(0f, 24f, progress);
                if (leftX + innerOffset < rightX - innerOffset && topY + innerOffset < bottomY - innerOffset)
                {
                    float alpha = (1f - progress) * fade * 0.5f;
                    Color innerScanColor = AegisVisuals.Add(AegisVisuals.Core, alpha);

                    Vector2 itl = new(leftX + innerOffset, topY + innerOffset);
                    Vector2 itr = new(rightX - innerOffset, topY + innerOffset);
                    Vector2 ibl = new(leftX + innerOffset, bottomY - innerOffset);
                    Vector2 ibr = new(rightX - innerOffset, bottomY - innerOffset);

                    Main.spriteBatch.DrawLineBetter(itl, itr, innerScanColor, 1.5f);
                    Main.spriteBatch.DrawLineBetter(itl, ibl, innerScanColor, 1.5f);
                    Main.spriteBatch.DrawLineBetter(itr, ibr, innerScanColor, 1.5f);
                    Main.spriteBatch.DrawLineBetter(ibr, ibl, innerScanColor, 1.5f);
                }
            }

            // 3. 干净的脉动线框（模仿 CalamitasClone 边界框的主描边与能量边框）
            float pulse = 0.85f + 0.15f * MathF.Sin(time * 6f);

            // ① 外层外焰描边 (Glow)
            Main.spriteBatch.DrawLineBetter(tl, tr, flameColor * 0.45f, 10f * pulse);
            Main.spriteBatch.DrawLineBetter(tl, bl, flameColor * 0.45f, 10f * pulse);
            Main.spriteBatch.DrawLineBetter(tr, br, flameColor * 0.45f, 10f * pulse);
            Main.spriteBatch.DrawLineBetter(br, bl, flameColor * 0.45f, 10f * pulse);

            // ② 主屏障圣金描边 (4px)
            Main.spriteBatch.DrawLineBetter(tl, tr, goldColor * 0.9f, 4f);
            Main.spriteBatch.DrawLineBetter(tl, bl, goldColor * 0.9f, 4f);
            Main.spriteBatch.DrawLineBetter(tr, br, goldColor * 0.9f, 4f);
            Main.spriteBatch.DrawLineBetter(br, bl, goldColor * 0.9f, 4f);

            // ③ 内芯白金线 (1.6px)
            Main.spriteBatch.DrawLineBetter(tl, tr, coreColor, 1.6f);
            Main.spriteBatch.DrawLineBetter(tl, bl, coreColor, 1.6f);
            Main.spriteBatch.DrawLineBetter(tr, br, coreColor, 1.6f);
            Main.spriteBatch.DrawLineBetter(br, bl, coreColor, 1.6f);

            // ④ 四角辉光点缀
            Main.EntitySpriteDraw(bloom, tl, null, coreColor * 0.75f, 0f, bloom.Size() * 0.5f, AegisVisuals.RadiusScale(bloom, 14f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, tr, null, coreColor * 0.75f, 0f, bloom.Size() * 0.5f, AegisVisuals.RadiusScale(bloom, 14f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, bl, null, coreColor * 0.75f, 0f, bloom.Size() * 0.5f, AegisVisuals.RadiusScale(bloom, 14f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, br, null, coreColor * 0.75f, 0f, bloom.Size() * 0.5f, AegisVisuals.RadiusScale(bloom, 14f), SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
