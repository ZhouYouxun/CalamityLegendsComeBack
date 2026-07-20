using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // A dropped torpedo: it sinks, aligns on the nearest hostile, then makes one committed run.
    internal sealed class SkyfinTorpedo : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/SkyfinBombers";

        private const int DropFrames = 18;
        private const int HoverFrames = 26;
        private const float HomingRange = 1200f;
        private const float DashSpeed = 31f;
        private static readonly int TrailLength = 12;

        private float LaunchCurve => Projectile.ai[0];
        private float Age => Projectile.localAI[0];
        private bool IsDashing => Projectile.localAI[1] > 0f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type]     = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width          = 20;
            Projectile.height         = 20;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = 1;
            Projectile.timeLeft       = 420;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = -1;
            Projectile.ArmorPenetration     = 12;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (!IsDashing)
            {
                NPC target = FindTarget();
                Vector2 downward = Vector2.UnitY.RotatedBy(LaunchCurve * 0.08f);

                // A real deployment first drops below the player instead of flying directly at the aim cursor.
                if (Age <= DropFrames)
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, downward * 10f, 0.11f);
                else
                    Projectile.velocity *= 0.84f;

                // The torpedo immediately faces the closest target while still physically dropping downward.
                if (target != null)
                {
                    Vector2 aim = (target.Center - Projectile.Center).SafeNormalize(downward);
                    SetVisualRotation(aim);

                    if (Age >= DropFrames + HoverFrames)
                    {
                        Projectile.velocity = aim * DashSpeed;
                        Projectile.localAI[1] = 1f;
                        Projectile.netUpdate = true;
                        SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.52f, Pitch = -0.32f }, Projectile.Center);
                        SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 1.65f, 11f, 10, SeasSearingPalette.BiohazardLime);
                    }
                }
                else
                {
                    SetVisualRotation(Projectile.velocity.SafeNormalize(downward));
                }
            }
            else
            {
                // Deliberately no homing here: after the torpedo run begins it cannot correct course.
                SetVisualRotation(Projectile.velocity.SafeNormalize(Vector2.UnitX));
            }

            Lighting.AddLight(Projectile.Center, SeasSearingPalette.BiohazardLime.ToVector3() * 0.42f);

            if (!Main.dedServ && Main.GameUpdateCount % (IsDashing ? 1 : 3) == 0)
            {
                Dust trail = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * 0.6f,
                    DustID.GemEmerald,
                    Projectile.velocity * -Main.rand.NextFloat(0.2f, 0.5f),
                    110, Color.Lerp(SeasSearingPalette.ToxicGreen, SeasSearingPalette.BiohazardLime, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.5f, 0.9f));
                trail.noGravity = true;
            }

            if (!Main.dedServ && IsDashing && Main.GameUpdateCount % 14 == 0)
                SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 1.15f, 9f, 8, SeasSearingPalette.BiohazardLime);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SeasSearingPollutionNPC pollution = target.GetGlobalNPC<SeasSearingPollutionNPC>();
            pollution.ApplyPollution(target, Projectile.owner, 8, 14 * 60);
            target.AddBuff(BuffID.Venom, 300);

            if (Main.myPlayer == Projectile.owner)
            {
                SeasSearingPollutionJuice.SpawnRadial(
                    Projectile.GetSource_OnHit(target),
                    Projectile.Center,
                    Main.rand.Next(5, 11),
                    5.5f,
                    10.5f,
                    Math.Max(1, (int)(Projectile.damage * 0.28f)),
                    Projectile.knockBack * 0.2f,
                    Projectile.owner);

                Projectile.NewProjectile(
                    Projectile.GetSource_OnHit(target),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<SkyfinTorpedoExplosion>(),
                    Math.Max(1, Projectile.damage), 3.5f, Projectile.owner);
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.65f, Pitch = 0.1f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex    = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2   origin = tex.Size() * 0.5f;
            Vector2   bloomOrigin = bloom.Size() * 0.5f;
            float visualRotation = Projectile.rotation;

            for (int i = 1; i < TrailLength; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float t    = 1f - (float)i / TrailLength;
                Color tc   = Color.Lerp(SeasSearingPalette.ToxicGreen, SeasSearingPalette.BiohazardLime, t) * (t * t * 0.55f);
                tc.A       = 0;
                Main.EntitySpriteDraw(bloom,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    null, tc * 0.65f, Projectile.oldRot[i],
                    bloomOrigin, 0.055f * t, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(tex,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    null, tc, Projectile.oldRot[i],
                    origin, Projectile.scale * MathHelper.Lerp(0.3f, 0.85f, t), SpriteEffects.None, 0);
            }

            Color main = SeasSearingPalette.BiohazardLime;
            main.A     = 0;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition,
                null, main * 0.58f, visualRotation, bloomOrigin, 0.09f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition,
                null, main, visualRotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 10, 2.5f, 16f);
            SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 2f, 12f, 10, SeasSearingPalette.ToxicGreen);
        }

        // This custom torpedo draw uses the SkyfinBombers texture inverted relative to SkyfinNuke.
        private void SetVisualRotation(Vector2 direction)
        {
            Projectile.spriteDirection = Projectile.direction = (direction.X > 0f).ToDirectionInt();
            Projectile.rotation = direction.ToRotation() + (Projectile.spriteDirection == 1 ? MathHelper.Pi : 0f);
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
