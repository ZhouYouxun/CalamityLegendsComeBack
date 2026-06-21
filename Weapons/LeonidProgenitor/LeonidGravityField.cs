using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidGravityField : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        public Player Owner => Main.player[Projectile.owner];
        public int PrimaryEffectID => (int)Projectile.ai[0];
        public int SecondaryEffectID => (int)Projectile.ai[1];

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

        public override bool? CanDamage() => timer >= 180; // only deal field damage when meteors start falling

        public override void AI()
        {
            timer++;

            // Visual effects inside the gravity field (floating purple dusts)
            if (Main.rand.NextBool(3))
            {
                Vector2 spawnOffset = new Vector2(Main.rand.NextFloat(-550f, 550f), Main.rand.NextFloat(-400f, 200f));
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + spawnOffset,
                    DustID.TintableDustLighted,
                    new Vector2(0f, Main.rand.NextFloat(1.5f, 3.5f)),
                    100,
                    new Color(150, 90, 255),
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
                    PrimaryEffectID,
                    SecondaryEffectID,
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

            float progress = Math.Min(timer / 60f, 1f); // fade in over 1 second
            if (Projectile.timeLeft < 60)
            {
                progress = Projectile.timeLeft / 60f; // fade out over last second
            }

            Color fieldColor = new Color(130, 80, 255) * 0.15f * progress;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // Draw central gravity core
            Main.spriteBatch.Draw(bloom, drawPos, null, fieldColor, 0f, bloom.Size() * 0.5f, 5f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(bloom, drawPos, null, Color.White * 0.05f * progress, 0f, bloom.Size() * 0.5f, 2f, SpriteEffects.None, 0f);

            // Draw accretion disk rings
            Main.spriteBatch.Draw(ring, drawPos, null, fieldColor * 2.2f, Main.GlobalTimeWrappedHourly * 1.2f, ring.Size() * 0.5f, 7.5f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, drawPos, null, fieldColor * 1.4f, -Main.GlobalTimeWrappedHourly * 0.7f, ring.Size() * 0.5f, 10f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, drawPos, null, fieldColor * 0.8f, Main.GlobalTimeWrappedHourly * 0.4f, ring.Size() * 0.5f, 13f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
