using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidGravityField : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        public Player Owner => Main.player[Projectile.owner];

        private int timer;

        public override void SetDefaults()
        {
            Projectile.width = 1200;
            Projectile.height = 800;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360; // 6 seconds
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Owner.GetModPlayer<LeonidConstellationPlayer>().IsUnlocked(LeonidStar.Denebola))
                Projectile.timeLeft = 480;
        }

        public override bool? CanDamage() => timer >= 180; // only deal field damage when meteors start falling

        public override void AI()
        {
            timer++;

            // 向星光系统报点：场存在期间，所有星光也会被这股引力一起往下拽。
            LeonidStarlight.ReportGravityWell(Projectile.Center, 1f);

            if (timer == 1)
            {
                // 开场：两圈反向张开的星辉，把引力场的边界一次性画出来。
                LeonidStarlight.Ring(Projectile.Center, 18, 150f, LeonidVisualUtils.GetReadyGold(),
                    LeonidStarlightShape.Shard, speed: 5f, scale: 1.2f, hoverTime: 40, lifetime: 220, lanceSpeed: 22f);
                LeonidStarlight.Ring(Projectile.Center, 14, 300f, LeonidVisualUtils.StratusBlue,
                    LeonidStarlightShape.Mote, speed: 2.5f, scale: 1f, hoverTime: 55, lifetime: 240,
                    lanceSpeed: 20f, angleOffset: MathHelper.Pi / 14f);
                LeonidStarlight.Spawn(Projectile.Center, Vector2.Zero, LeonidVisualUtils.StarGold,
                    LeonidStarlightShape.Halo, 2.4f, 16, 110, 10f, linksToSiblings: false);
            }

            // 流星暴期间同步下星雨。星光落进场里会被引力加速，
            // 悬停时又彼此连成星网——整片天空是活的。
            if (timer <= 210 && timer % 7 == 0)
            {
                LeonidStarlight.Rain(Projectile.Center, 540f, 640f, 3,
                    shape: Main.rand.NextBool(4) ? LeonidStarlightShape.Shard : LeonidStarlightShape.Mote,
                    scale: 0.9f, hoverTime: 30, lifetime: 200);
            }
            else if (timer % 11 == 0)
            {
                LeonidStarlight.Rain(Projectile.Center, 540f, 560f, 1, scale: 0.75f, hoverTime: 38, lifetime: 190);
            }

            // Visual effects inside the gravity field (floating stratus motes)
            if (Main.rand.NextBool(3))
            {
                Vector2 spawnOffset = new Vector2(Main.rand.NextFloat(-550f, 550f), Main.rand.NextFloat(-400f, 200f));
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + spawnOffset,
                    Main.rand.NextBool(4) ? DustID.Electric : DustID.TintableDustLighted,
                    new Vector2(0f, Main.rand.NextFloat(1.5f, 3.5f)),
                    100,
                    Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.MoonViolet, Main.rand.NextFloat(0.45f)),
                    Main.rand.NextFloat(0.6f, 1f));
                d.noGravity = true;
            }

            // Apply "群星重力" (Gravity of Stars) debuff to enemies inside/above the field
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.active && !npc.friendly && !npc.dontTakeDamage)
                {
                    float dx = Math.Abs(npc.Center.X - Projectile.Center.X);
                    if (dx < 600f)
                    {
                        float dy = npc.Center.Y - Projectile.Center.Y;
                        // Targets up to 900 pixels above the field's center, down to 300 pixels below
                        if (dy > -900f && dy < 300f)
                        {
                            npc.AddBuff(ModContent.BuffType<LeonidGravityDebuff>(), 10);
                        }
                    }
                }
            }

            // Apply downward force to all other active projectiles
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && i != Projectile.whoAmI && p.type != ModContent.ProjectileType<LeonidCometSmall>() && p.type != ModContent.ProjectileType<LeonidCometLarge>())
                {
                    float dx = Math.Abs(p.Center.X - Projectile.Center.X);
                    float dy = p.Center.Y - Projectile.Center.Y;
                    if (dx < 600f && dy > -900f && dy < 300f)
                    {
                        p.velocity.Y += 0.35f;
                    }
                }
            }

            // Continuously spawn comets in the sky during the first 3 seconds (180 frames)
            if (timer <= 180 && timer % 6 == 0 && Main.myPlayer == Projectile.owner)
            {
                // Random position in the sky above the field
                float rx = Main.rand.Next(-500, 500);
                float ry = -650f + Main.rand.Next(-60, 60);
                Vector2 spawnPos = Projectile.Center + new Vector2(rx, ry);

                int p = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    Vector2.Zero,
                    ModContent.ProjectileType<LeonidCometSmall>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    0f,
                    0f,
                    (float)LeonidCometSmall.GravityFieldFlag);

                if (p.WithinBounds(Main.maxProjectiles))
                {
                    Projectile meteor = Main.projectile[p];
                    int meteorIndex = timer / 6;
                    // Sequentially launch comets after 180 ticks (3 seconds)
                    float delay = (180 - timer) + meteorIndex * 4;

                    meteor.localAI[1] = delay;

                    // Large/Small variation
                    if (Main.rand.NextBool(3)) // 33% chance to be large
                    {
                        meteor.scale = 1.8f;
                        meteor.width = 44;
                        meteor.height = 44;
                        meteor.damage = (int)(meteor.damage * 1.5f);
                    }
                    
                    meteor.netUpdate = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D sparkle = ModContent.Request<Texture2D>("CalamityMod/Particles/Sparkle").Value;

            float progress = Math.Min(timer / 60f, 1f); // fade in over 1 second
            if (Projectile.timeLeft < 60)
            {
                progress = Projectile.timeLeft / 60f; // fade out over last second
            }

            Color fieldColor = LeonidVisualUtils.GetCelestialColor(0.8f, Projectile.whoAmI * 0.07f) * 0.15f * progress;
            Color gold = LeonidVisualUtils.GetReadyGold(Projectile.whoAmI * 0.11f) * progress;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            Main.EntitySpriteDraw(bloom, drawPos, null, fieldColor, 0f, bloom.Size() * 0.5f, 5f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, drawPos, null, Color.White * 0.05f * progress, 0f, bloom.Size() * 0.5f, 2f, SpriteEffects.None, 0f);

            Main.EntitySpriteDraw(ring, drawPos, null, fieldColor * 2.2f, Main.GlobalTimeWrappedHourly * 1.2f, ring.Size() * 0.5f, 7.5f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ring, drawPos, null, fieldColor * 1.4f, -Main.GlobalTimeWrappedHourly * 0.7f, ring.Size() * 0.5f, 10f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ring, drawPos, null, fieldColor * 0.8f, Main.GlobalTimeWrappedHourly * 0.4f, ring.Size() * 0.5f, 13f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ring, drawPos, null, gold * 0.2f, -Main.GlobalTimeWrappedHourly * 1.7f, ring.Size() * 0.5f, 4.2f, SpriteEffects.None, 0f);

            for (int i = 0; i < 8; i++)
            {
                float angle = Main.GlobalTimeWrappedHourly * 0.9f + i * MathHelper.TwoPi / 8f;
                Vector2 point = drawPos + angle.ToRotationVector2() * (170f + 24f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f + i));
                Main.EntitySpriteDraw(sparkle, point, null, Color.Lerp(fieldColor, gold, i % 2) * 0.52f, -angle, sparkle.Size() * 0.5f, 0.38f, SpriteEffects.None, 0f);
            }
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();

            // 三层准星环反向缓慢自转，把"这里是一片被锁定的天空"讲清楚。
            for (int i = 0; i < 3; i++)
            {
                float spin = Main.GlobalTimeWrappedHourly * (0.35f + i * 0.22f) * (i % 2 == 0 ? 1f : -1f);
                LeonidStarlight.DrawReticle(
                    Projectile.Center,
                    i == 1 ? gold : Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.MoonViolet, i * 0.4f),
                    0.1f * progress,
                    0.9f + i * 0.65f,
                    spin);
            }

            LeonidStarlight.DrawSunburst(Projectile.Center, gold, 0.16f * progress, 0.34f, Main.GlobalTimeWrappedHourly * 0.22f);

            // 外缘游走的星芒，标出场的水平边界。
            for (int i = 0; i < 10; i++)
            {
                float angle = -Main.GlobalTimeWrappedHourly * 0.55f + i * MathHelper.TwoPi / 10f;
                float radius = 250f + 40f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.6f + i * 0.9f);
                LeonidStarlight.DrawFlare(
                    Projectile.Center + angle.ToRotationVector2() * radius,
                    i % 2 == 0 ? LeonidVisualUtils.MoonWhite : gold,
                    0.34f * progress,
                    0.075f,
                    angle * 1.5f);
            }

            return false;
        }
    }
}
