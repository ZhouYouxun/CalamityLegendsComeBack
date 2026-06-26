using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // Homing air-torpedo spawned post-burst (stages 3-4) or per-shot (stage 5).
    // In stage 5 the 4th shot spawns SeasSearingMissile instead of this.
    internal sealed class SkyfinTorpedo : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/SkyfinBombers";

        private const float HomingRange  = 850f;
        private const float TurnFactor   = 0.12f;
        private const float MaxSpeed     = 18f;
        private const float MinSpeed     = 8f;

        private static readonly int TrailLength = 12;

        private float Speed => Math.Clamp(Projectile.velocity.Length(), MinSpeed, MaxSpeed);

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
            Projectile.timeLeft       = 360;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = -1;
            Projectile.ArmorPenetration     = 12;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            NPC target = FindTarget();
            if (target != null)
            {
                Vector2 toTarget  = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = Vector2.Lerp(
                    Projectile.velocity.SafeNormalize(Vector2.UnitY),
                    toTarget,
                    TurnFactor).SafeNormalize(Vector2.UnitY) * (Speed + 0.2f);
            }
            else
            {
                Projectile.velocity += Vector2.UnitY * 0.1f;
            }

            Lighting.AddLight(Projectile.Center, SeasSearingPalette.BiohazardLime.ToVector3() * 0.32f);

            if (!Main.dedServ && Main.GameUpdateCount % 2 == 0)
            {
                Dust trail = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * 0.6f,
                    DustID.GemEmerald,
                    Projectile.velocity * -Main.rand.NextFloat(0.2f, 0.5f),
                    110, Color.Lerp(SeasSearingPalette.ToxicGreen, SeasSearingPalette.BiohazardLime, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.5f, 0.9f));
                trail.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SeasSearingPollutionNPC pollution = target.GetGlobalNPC<SeasSearingPollutionNPC>();
            pollution.ApplyPollution(target, Projectile.owner, 8, 14 * 60);
            target.AddBuff(BuffID.Venom, 300);

            if (Main.myPlayer == Projectile.owner)
            {
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
            Vector2   origin = tex.Size() * 0.5f;

            for (int i = 1; i < TrailLength; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float t    = 1f - (float)i / TrailLength;
                Color tc   = Color.Lerp(SeasSearingPalette.ToxicGreen, SeasSearingPalette.BiohazardLime, t) * (t * t * 0.55f);
                tc.A       = 0;
                Main.EntitySpriteDraw(tex,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    null, tc, Projectile.oldRot[i],
                    origin, Projectile.scale * MathHelper.Lerp(0.3f, 0.85f, t), SpriteEffects.None, 0);
            }

            Color main = SeasSearingPalette.BiohazardLime;
            main.A     = 0;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition,
                null, main, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 10, 2.5f, 16f);
            SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 2f, 12f, 10, SeasSearingPalette.ToxicGreen);
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
