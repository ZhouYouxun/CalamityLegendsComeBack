using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.ResponsibilityPhone.Ultimate
{
    /// <summary>
    /// The regular Wulfrum Drone fires the vanilla Saucer Laser. This friendly version keeps that
    /// projectile's original sprite and movement style, then adds the squad's low-damage homing.
    /// </summary>
    internal sealed class ResponsibilityDroneLaser : ModProjectile
    {
        private int Age => 120 - Projectile.timeLeft;
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.SaucerLaser}";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.SaucerLaser);
            AIType = ProjectileID.SaucerLaser;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            if (Age >= 4)
            {
                NPC target = GetTarget();
                if (target != null && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, target.position, target.width, target.height))
                {
                    Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center) * Math.Max(12f, Projectile.velocity.Length());
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.19f);
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        private NPC GetTarget()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (Main.npc.IndexInRange(targetIndex) && Main.npc[targetIndex].CanBeChasedBy())
                return Main.npc[targetIndex];
            return ResponsibilityPhoneGlobalNPC.FindPriorityTarget(Projectile.owner, Projectile.Center, 500f);
        }
    }

    /// <summary>
    /// Wulfrum Amplifiers already reuse the vanilla Saucer Missile. The friendly Hovercraft salvo
    /// uses that same missile sprite instead of drawing a custom geometric projectile.
    /// </summary>
    internal sealed class ResponsibilityHovercraftMissile : ModProjectile
    {
        private int Age => 150 - Projectile.timeLeft;
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.SaucerMissile}";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            // Projectile 448 (Saucer Missile) is a vertical strip containing exactly three frames.
            Main.projFrames[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.SaucerMissile);
            AIType = -1;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            if (Age < 24)
            {
                Projectile.velocity.Y -= 0.035f;
            }
            else
            {
                NPC target = GetTarget();
                if (target != null)
                {
                    float speed = MathHelper.Clamp(Projectile.velocity.Length() + 0.08f, 8f, 13.5f);
                    Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center) * speed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.072f);
                }
                else
                {
                    Projectile.velocity.Y += 0.09f;
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Main.rand.NextBool(2))
            {
                Vector2 exhaustPosition = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 8f;
                Dust exhaust = Dust.NewDustPerfect(exhaustPosition, Main.rand.NextBool(4) ? DustID.Electric : DustID.Smoke,
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.1f), 120, default, Main.rand.NextFloat(0.7f, 1.05f));
                exhaust.noGravity = true;
            }
        }

        private NPC GetTarget()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (Main.npc.IndexInRange(targetIndex) && Main.npc[targetIndex].CanBeChasedBy())
                return Main.npc[targetIndex];
            return ResponsibilityPhoneGlobalNPC.FindPriorityTarget(Projectile.owner, Projectile.Center, 700f);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_Death(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<ResponsibilityMissileExplosion>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner);
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.48f, Pitch = 0.18f }, Projectile.Center);
            if (Main.dedServ)
                return;

            for (int i = 0; i < 18; i++)
            {
                int dustType = i % 4 == 0 ? DustID.Electric : DustID.Smoke;
                Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, Main.rand.NextVector2Circular(4.5f, 4.5f),
                    100, default, Main.rand.NextFloat(0.75f, 1.25f));
                dust.noGravity = true;
            }
        }
    }

    /// <summary>
    /// Damage-only blast hitbox. The visible explosion is produced by the missile's native smoke,
    /// electric dust, and explosion sound; this projectile intentionally draws nothing.
    /// </summary>
    internal sealed class ResponsibilityMissileExplosion : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 180;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 12;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
