using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.AStage0;
using CalamityMod;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.CStage2
{
    public class VesuviusVolcanicBomb : VesuviusMoltenAsteroid
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f && Projectile.ai[1] <= 0f)
                Projectile.ai[1] = 1.2f;

            base.AI();
            Projectile.velocity.Y += 0.08f;
            Projectile.velocity *= 0.992f;
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 oldCenter = Projectile.Center;

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    oldCenter,
                    Vector2.Zero,
                    ModContent.ProjectileType<VesuviusLingeringLava>(),
                    Math.Max(1, (int)(Projectile.damage * 0.42f)),
                    0f,
                    Projectile.owner,
                    82f * Projectile.scale);
            }

            base.OnKill(timeLeft);

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.78f, Pitch = -0.18f }, oldCenter);
                for (int i = 0; i < 8; i++)
                {
                    Particle smoke = new HeavySmokeParticle(
                        oldCenter + Main.rand.NextVector2Circular(20f, 20f),
                        Main.rand.NextVector2Circular(5f, 5f) - Vector2.UnitY * Main.rand.NextFloat(2f, 5f),
                        Color.Lerp(Color.Gray, Color.OrangeRed, 0.22f),
                        Main.rand.Next(28, 46),
                        Main.rand.NextFloat(0.8f, 1.6f),
                        0.85f,
                        Main.rand.NextFloat(-0.05f, 0.05f),
                        true,
                        required: true);
                    GeneralParticleHandler.SpawnParticle(smoke);
                }
            }
        }
    }

    public class VesuviusLingeringLava : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 88;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                float size = Projectile.ai[0] <= 0f ? 80f : Projectile.ai[0];
                Projectile.Resize((int)size, (int)(size * 0.65f));
                Projectile.localAI[0] = 1f;
            }

            if (!Main.dedServ)
            {
                if (Projectile.timeLeft % 3 == 0)
                {
                    RancorLavaMetaball.SpawnParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.42f, Projectile.height * 0.35f),
                        Main.rand.NextFloat(18f, 38f));
                }

                if (Main.rand.NextBool(3))
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.45f, Projectile.height * 0.28f),
                        DustID.Torch,
                        -Vector2.UnitY * Main.rand.NextFloat(0.6f, 2f),
                        100,
                        new Color(255, 112, 32),
                        Main.rand.NextFloat(0.8f, 1.3f));
                    dust.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float fade = Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true);
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(255, 70, 20, 0) * 0.18f * fade,
                0f,
                bloom.Size() * 0.5f,
                new Vector2(Projectile.width / (float)bloom.Width, Projectile.height / (float)bloom.Height) * 1.35f,
                SpriteEffects.None);

            return false;
        }
    }
}
