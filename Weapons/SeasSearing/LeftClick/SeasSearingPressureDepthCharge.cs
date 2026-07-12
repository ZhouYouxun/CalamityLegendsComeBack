using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // A delayed pressure charge: it slows, locks onto a nearby target, then collapses into a pollution blast.
    internal sealed class SeasSearingPressureDepthCharge : ModProjectile, ILocalizedModType
    {
        private const int ArmFrames = 22;
        private const int DetonateFrames = 54;
        private bool detonated;

        private float DriftDirection => Projectile.ai[0];
        private float Age => Projectile.localAI[0];

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 80;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 20;
        }

        public override bool? CanDamage() => detonated ? null : false;

        public override void AI()
        {
            if (detonated)
            {
                Projectile.velocity = Vector2.Zero;
                return;
            }

            Projectile.localAI[0]++;
            NPC target = FindTarget();
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (Age < ArmFrames)
                Projectile.velocity *= 0.92f;
            else if (target != null)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(direction) * Math.Max(9f, Projectile.velocity.Length() + 0.18f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.075f);
                direction = Projectile.velocity.SafeNormalize(direction);
            }

            float drift = (float)Math.Sin(Age * 0.27f + DriftDirection * 2.8f) * 0.16f;
            Projectile.velocity += direction.RotatedBy(MathHelper.PiOver2) * drift;
            Lighting.AddLight(Projectile.Center, Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.BiohazardLime, Age / DetonateFrames).ToVector3() * 0.45f);

            if (!Main.dedServ && (int)Age % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                    Main.rand.NextBool() ? DustID.GemEmerald : DustID.Water,
                    -direction * Main.rand.NextFloat(0.4f, 1.8f), 120,
                    Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.BiohazardLime, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.55f, 0.9f));
                dust.noGravity = true;
            }

            if (Age >= DetonateFrames || (Age >= ArmFrames && target != null && Vector2.DistanceSquared(Projectile.Center, target.Center) <= 56f * 56f))
                Detonate();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 10, 14 * 60);
            target.AddBuff(BuffID.Venom, 300);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 300);
        }

        private void Detonate()
        {
            if (detonated)
                return;

            detonated = true;
            Vector2 center = Projectile.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.width = Projectile.height = 150;
            Projectile.Center = center;
            Projectile.timeLeft = 3;
            Projectile.netUpdate = true;

            SeasSearingVisualUtility.SpawnAbyssDust(center, 24, 5f, 30f, 1.15f);
            SeasSearingVisualUtility.SpawnPressureRing(center, 3.5f, 20f, 18, SeasSearingPalette.BiohazardLime);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.58f, Pitch = -0.42f }, center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Color core = Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.BiohazardLime, MathHelper.Clamp(Age / DetonateFrames, 0f, 1f));
            core.A = 0;

            if (detonated)
            {
                float completion = 1f - Projectile.timeLeft / 3f;
                float opacity = 1f - completion;
                Main.EntitySpriteDraw(bloom, center, null, core * (opacity * 0.9f), 0f, bloom.Size() * 0.5f, 1.35f + completion * 1.1f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(ring, center, null, core * opacity, Main.GlobalTimeWrappedHourly * 2.2f, ring.Size() * 0.5f, 0.72f + completion * 1.8f, SpriteEffects.None, 0);
                return false;
            }

            float pulse = 0.72f + 0.12f * (float)Math.Sin(Age * 0.35f);
            Main.EntitySpriteDraw(bloom, center, null, core * 0.72f, 0f, bloom.Size() * 0.5f, 0.19f * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring, center, null, core * 0.56f, -Main.GlobalTimeWrappedHourly * 2.4f, ring.Size() * 0.5f, 0.21f * pulse, SpriteEffects.None, 0);
            return false;
        }

        private NPC FindTarget()
        {
            NPC best = null;
            float bestDistance = 680f * 680f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = npc;
                }
            }
            return best;
        }
    }
}
