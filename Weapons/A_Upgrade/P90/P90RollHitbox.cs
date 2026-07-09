using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.P90
{
    internal sealed class P90RollHitbox : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.P90";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Player Owner => Main.player[Projectile.owner];
        private NewLegendP90Player P90Player => Owner.GetModPlayer<NewLegendP90Player>();

        public override void SetDefaults()
        {
            Projectile.width = 62;
            Projectile.height = 62;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = NewLegendP90.RollFrames + 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => P90Player.IsRolling ? null : false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || !P90Player.IsRolling)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Owner.Center;
            Projectile.velocity = Vector2.UnitX * P90Player.RollDirection;
            Projectile.timeLeft = 2;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Collision.CheckAABBvAABBCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Owner.Center - new Vector2(34f), new Vector2(68f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 direction = Vector2.UnitX * P90Player.RollDirection;
            target.velocity += direction * (7f + 7f * target.knockBackResist) - Vector2.UnitY * (2f + 3f * target.knockBackResist);
            SpawnImpact(target.Center, direction);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.62f, Pitch = 0.28f }, target.Center);
        }

        private static void SpawnImpact(Vector2 position, Vector2 direction)
        {
            for (int i = 0; i < 24; i++)
            {
                float angle = MathHelper.TwoPi * i / 24f;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(2.2f, 7.4f) + direction * 2.2f;
                Dust dust = Dust.NewDustPerfect(
                    position + velocity.SafeNormalize(Vector2.UnitY) * 8f,
                    i % 2 == 0 ? DustID.GoldFlame : DustID.FireworkFountain_Red,
                    velocity,
                    90,
                    i % 2 == 0 ? Color.Gold : new Color(255, 70, 70),
                    Main.rand.NextFloat(0.9f, 1.45f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 8; i++)
            {
                Dust smoke = Dust.NewDustPerfect(
                    position + Main.rand.NextVector2Circular(14f, 14f),
                    DustID.Smoke,
                    -direction.RotatedByRandom(0.8f) * Main.rand.NextFloat(1.0f, 3.4f),
                    145,
                    Color.Gray,
                    Main.rand.NextFloat(0.7f, 1.15f));
                smoke.noGravity = true;
            }
        }
    }
}
