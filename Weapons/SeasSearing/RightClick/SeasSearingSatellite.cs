using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // Small homing orb spawned in a ring by SeasSearingPressureBolt or as a split from VentShot.
    internal sealed class SeasSearingSatellite : ModProjectile, ILocalizedModType
    {
        private float orbitPhase;
        private bool  hasTarget;
        private int   targetWho = -1;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width          = 10;
            Projectile.height         = 10;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
            Projectile.penetrate      = -1;
            Projectile.timeLeft       = 150;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 18;
        }

        public override void AI()
        {
            orbitPhase += 0.12f;

            if (Main.myPlayer == Projectile.owner)
            {
                if (!hasTarget || targetWho < 0 || !Main.npc.IndexInRange(targetWho) || !Main.npc[targetWho].CanBeChasedBy())
                    FindTarget();
            }

            if (hasTarget && Main.npc.IndexInRange(targetWho) && Main.npc[targetWho].CanBeChasedBy())
            {
                NPC     target   = Main.npc[targetWho];
                Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Vector2 perp     = new(-toTarget.Y, toTarget.X) * (float)Math.Sin(orbitPhase) * 3.5f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, toTarget * 10f + perp, 0.14f);
            }
            else
            {
                Projectile.velocity *= 0.97f;
            }

            if (Projectile.velocity.LengthSquared() > 144f)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 12f;

            float age = 1f - Projectile.timeLeft / 150f;
            Lighting.AddLight(Projectile.Center, SeasSearingPalette.PressureBlue.ToVector3() * (0.25f - age * 0.1f));

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center, DustID.GemDiamond,
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    110,
                    Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.RadioactiveCyan, age),
                    Main.rand.NextFloat(0.5f, 0.85f));
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 2, 6 * 60, fromSpread: true);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 120);
        }

        public override void OnKill(int timeLeft) =>
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 8, 2.8f, 5f, 0.8f);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom   = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float     opacity = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f);
            Color c  = (SeasSearingPalette.PressureBlue   with { A = 0 }) * opacity;
            Color c2 = (SeasSearingPalette.RadioactiveCyan with { A = 0 }) * opacity * 0.55f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float   t   = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, pos, null, c2 * (t * 0.6f), 0f, bloom.Size() * 0.5f, 0.07f + t * 0.06f, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, c,  0f, bloom.Size() * 0.5f, 0.14f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, c2, 0f, bloom.Size() * 0.5f, 0.08f, SpriteEffects.None, 0);
            return false;
        }

        private void FindTarget()
        {
            float bestDist = 400f * 400f;
            int   best     = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.CanBeChasedBy()) continue;
                float d = Vector2.DistanceSquared(Projectile.Center, n.Center);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            targetWho = best;
            hasTarget  = best >= 0;
        }
    }
}
