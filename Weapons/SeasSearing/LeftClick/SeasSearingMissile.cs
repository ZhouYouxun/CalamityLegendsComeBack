using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // Stage 5 only: replaces the 4th SkyfinTorpedo with a more spectacular missile.
    // 2× torpedo damage, tighter tracking, 200×200 explosion.
    internal sealed class SeasSearingMissile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_134";

        private const float HomingRange = 950f;
        private const float TurnFactor  = 0.20f;
        private const float MaxSpeed    = 22f;
        private const float MinSpeed    = 10f;

        private static readonly int TrailLength = 16;

        private float Speed => Math.Clamp(Projectile.velocity.Length(), MinSpeed, MaxSpeed);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type]     = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width          = 24;
            Projectile.height         = 24;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = 1;
            Projectile.timeLeft       = 480;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = -1;
            Projectile.ArmorPenetration     = 22;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            NPC target = FindTarget();
            if (target != null)
            {
                Vector2 toTarget   = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = Vector2.Lerp(
                    Projectile.velocity.SafeNormalize(Vector2.UnitY),
                    toTarget,
                    TurnFactor).SafeNormalize(Vector2.UnitY) * (Speed + 0.35f);
            }
            else
            {
                Projectile.velocity += Vector2.UnitY * 0.06f;
            }

            Color thrustColor = Color.Lerp(SeasSearingPalette.WarningOrange, SeasSearingPalette.BiohazardLime,
                MathF.Sin(Main.GameUpdateCount * 0.22f) * 0.5f + 0.5f);
            Lighting.AddLight(Projectile.Center, thrustColor.ToVector3() * 0.55f);

            if (!Main.dedServ)
            {
                // Thruster flame
                for (int i = 0; i < 3; i++)
                {
                    Vector2 backPos = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * (8f + i * 5f);
                    Dust flame = Dust.NewDustPerfect(backPos + Main.rand.NextVector2Circular(3f, 3f),
                        DustID.Torch,
                        -Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.3f) * Main.rand.NextFloat(0.8f, 2.5f),
                        120,
                        Color.Lerp(SeasSearingPalette.WarningOrange, SeasSearingPalette.BiohazardLime, Main.rand.NextFloat()),
                        Main.rand.NextFloat(0.6f, 1.2f));
                    flame.noGravity = true;
                }

                // Toxic smoke
                if (Main.GameUpdateCount % 3 == 0)
                {
                    Dust smoke = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                        DustID.GemEmerald,
                        Main.rand.NextVector2Circular(0.8f, 0.8f),
                        125, SeasSearingPalette.ToxicGreen,
                        Main.rand.NextFloat(0.45f, 0.75f));
                    smoke.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SeasSearingPollutionNPC pollution = target.GetGlobalNPC<SeasSearingPollutionNPC>();
            pollution.ApplyPollution(target, Projectile.owner, 15, 16 * 60);
            target.AddBuff(BuffID.Venom, 480);

            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_OnHit(target),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<SeasSearingMissileExplosion>(),
                    Math.Max(1, Projectile.damage), 5f, Projectile.owner);
            }

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.85f, Pitch = -0.1f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex    = ModContent.Request<Texture2D>(Texture).Value;
            Vector2   origin = tex.Size() * 0.5f;

            for (int i = 1; i < TrailLength; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float t  = 1f - (float)i / TrailLength;
                Color tc = Color.Lerp(SeasSearingPalette.WarningOrange, SeasSearingPalette.BiohazardLime, t) * (t * t * 0.65f);
                tc.A     = 0;
                Main.EntitySpriteDraw(tex,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    null, tc, Projectile.oldRot[i],
                    origin, Projectile.scale * MathHelper.Lerp(0.35f, 0.95f, t), SpriteEffects.None, 0);
            }

            Color main = SeasSearingPalette.BiohazardLime;
            main.A     = 0;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition,
                null, main, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.5f, Pitch = 0.15f }, Projectile.Center);
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 12, 3f, 20f);
        }

        private NPC FindTarget()
        {
            NPC   best     = null;
            float bestDist = HomingRange * HomingRange;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy()) continue;
                float dist = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (dist < bestDist) { bestDist = dist; best = npc; }
            }
            return best;
        }
    }
}
