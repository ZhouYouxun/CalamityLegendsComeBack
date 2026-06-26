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
    public class LeonidRightClickHoldout : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        public Player Owner => Main.player[Projectile.owner];
        public int PrimaryEffectID => (int)Projectile.ai[0];
        public int SecondaryEffectID => (int)Projectile.ai[1];

        // Charge progress stored in localAI[0] (0 to 100)
        public float ChargeProgress
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = MathHelper.Clamp(value, 0f, 100f);
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<LeonidProgenitor>())
            {
                Projectile.Kill();
                return;
            }

            // Keep holdout alive as long as right click is pressed
            if (Owner.controlUseTile)
            {
                Projectile.timeLeft = 2;
            }
            else
            {
                // Release and fire!
                LaunchMeteor();
                Projectile.Kill();
                return;
            }

            // Align owner and set held projection
            Owner.ChangeDir(Math.Sign(Main.MouseWorld.X - Owner.Center.X));
            Owner.itemRotation = (Main.MouseWorld - Owner.Center).ToRotation();
            if (Owner.direction == -1)
                Owner.itemRotation += MathHelper.Pi;

            Owner.itemTime = Owner.itemAnimation = 5;
            Owner.heldProj = Projectile.whoAmI;

            // Positioning in front of the player
            Vector2 aimDir = (Main.MouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);
            Projectile.Center = Owner.MountedCenter + aimDir * 28f;

            // Slow down player speed slightly
            Owner.velocity.X *= 0.9f;
            if (Owner.velocity.Y * Owner.gravDir < 0f)
                Owner.velocity.Y *= 0.9f;

            // Increment charge progress naturally
            ChargeProgress += 1.4f;

            // Search and recycle active reflective comets
            int reflectiveType = ModContent.ProjectileType<LeonidReflectiveMeteor>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == reflectiveType && p.ModProjectile is LeonidReflectiveMeteor reflective)
                {
                    if (p.owner == Projectile.owner && Vector2.Distance(Projectile.Center, p.Center) < 1200f)
                    {
                        // Put it into returning state
                        reflective.IsReturning = true;

                        // Merge if it gets very close
                        if (Vector2.Distance(Projectile.Center, p.Center) < 36f)
                        {
                            // Add 4% progress instantly
                            ChargeProgress += 4f;

                            // Spawn merge sparks
                            for (int j = 0; j < 6; j++)
                            {
                                Dust d = Dust.NewDustPerfect(
                                    p.Center,
                                    DustID.TintableDustLighted,
                                    Main.rand.NextVector2Circular(2f, 2f),
                                    100,
                                    Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.MoonWhite, 0.35f),
                                    Main.rand.NextFloat(0.7f, 1f));
                                d.noGravity = true;
                            }

                            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.35f, Pitch = 0.5f }, Projectile.Center);
                            p.Kill();
                        }
                    }
                }
            }

            // Charge fully sound and visual trigger
            if (ChargeProgress >= 100f && Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f; // ready flag
                SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.85f, Pitch = 0.1f }, Projectile.Center);
                for (int i = 0; i < 20; i++)
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.TintableDustLighted,
                        Main.rand.NextVector2Circular(4f, 4f),
                        100,
                        LeonidVisualUtils.GetReadyGold(Projectile.whoAmI * 0.11f),
                        Main.rand.NextFloat(1f, 1.4f));
                    d.noGravity = true;
                }
            }
        }

        private void LaunchMeteor()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 launchVel = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX * Owner.direction) * 20f;
            float prog = ChargeProgress / 100f;
            
            // Damage scales from 60% to 150% based on charge progress
            int damage = (int)(Projectile.damage * (0.6f + 0.9f * prog));
            
            int largeCometType = ModContent.ProjectileType<LeonidCometLarge>();
            int p = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                launchVel,
                largeCometType,
                damage,
                Projectile.knockBack * (0.8f + 0.4f * prog),
                Projectile.owner,
                PrimaryEffectID,
                SecondaryEffectID,
                prog); // pass progress as ai[2] for scale

            if (p.WithinBounds(Main.maxProjectiles))
            {
                Main.projectile[p].netUpdate = true;
            }

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.9f, Pitch = -0.15f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float prog = ChargeProgress / 100f;
            float size = MathHelper.Lerp(0.35f, 1.25f, prog);

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D sparkle = ModContent.Request<Texture2D>("CalamityMod/Particles/Sparkle").Value;
            Color coreColor = Color.Lerp(LeonidVisualUtils.GetCelestialColor(prog, Projectile.whoAmI * 0.09f), LeonidVisualUtils.GetReadyGold(), prog * 0.24f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 aimDir = (Main.MouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);

            // The charge core is a pure light effect and must not render BloomCircle's black base.
            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            LeonidVisualUtils.DrawGlowBlade(Projectile.Center - aimDir * 4f, aimDir, coreColor, 0.42f + prog * 0.22f, size * 0.12f, size * 0.03f);
            Main.EntitySpriteDraw(bloom, drawPos, null, coreColor * 0.4f, 0f, bloom.Size() * 0.5f, size * 0.65f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, drawPos, null, Color.White * 0.3f, 0f, bloom.Size() * 0.5f, size * 0.35f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ring, drawPos, null, coreColor * (0.38f + prog * 0.24f), Main.GlobalTimeWrappedHourly * 2.8f, ring.Size() * 0.5f, size * 0.34f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(sparkle, drawPos, null, Color.White * (0.24f + prog * 0.24f), -Main.GlobalTimeWrappedHourly * 3.4f, sparkle.Size() * 0.5f, size * 0.42f, SpriteEffects.None, 0f);
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();

            // Orbiting stars spiraling in
            int orbitCount = 4;
            for (int i = 0; i < orbitCount; i++)
            {
                float angleOffset = i * MathHelper.TwoPi / orbitCount;
                float time = Main.GlobalTimeWrappedHourly * 6.5f + angleOffset;
                // Spiral radius oscillates and decreases as they converge
                float radius = MathHelper.Lerp(48f, 10f, (float)Math.Sin(time * 0.6f) * 0.5f + 0.5f);
                Vector2 orbitOffset = time.ToRotationVector2() * radius;
                
                Main.EntitySpriteDraw(sparkle, drawPos + orbitOffset, null, Color.Lerp(coreColor, Color.White, 0.32f) * 0.72f, -time, sparkle.Size() * 0.5f, size * 0.24f, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
