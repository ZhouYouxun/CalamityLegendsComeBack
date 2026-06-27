using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    public class M61Bullet : ModProjectile, ILocalizedModType
    {
        private static readonly Color HelixGoldBright = new(255, 238, 110);
        private static readonly Color HelixGoldDeep = new(255, 168, 32);

        public new string LocalizationCategory => "Projectiles.MK14EBR";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/MK14EBR/M61Bullet";

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = Projectile.direction;

            Projectile.localAI[0]++;
            if (Projectile.localAI[0] <= 2f)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
            float phase = Projectile.localAI[0] * 0.15f;

            SpawnHelixStrand(direction, perpendicular, phase);
            SpawnHelixStrand(direction, perpendicular, phase + MathHelper.Pi);
        }

        private void SpawnHelixStrand(Vector2 direction, Vector2 perpendicular, float phase)
        {
            float sinVal = (float)Math.Sin(phase);
            float depth = Math.Abs(sinVal);

            Dust strand = Dust.NewDustPerfect(
                Projectile.Center + perpendicular * sinVal * 7f,
                DustID.Torch,
                -direction * Main.rand.NextFloat(0.15f, 0.55f),
                0,
                Color.Lerp(HelixGoldDeep, HelixGoldBright, depth),
                Main.rand.NextFloat(0.42f, 0.72f) * (0.35f + depth * 0.65f));
            strand.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300);
        }
    }
}
