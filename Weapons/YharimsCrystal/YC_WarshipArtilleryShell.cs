using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal
{
    public class YC_WarshipArtilleryShell : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCRight/YC_Right_HeavyBolt";

        private ref float TrackingStrength => ref Projectile.ai[0];
        private ref float SupportMarker => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 110;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Timer++;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            NPC target = YC_LeftSquadronHelper.FindPriorityTarget(owner, Projectile.Center, 1750f, forward, 75f, false);

            if (target != null)
            {
                Vector2 desiredDirection = (target.Center + target.velocity * 8f - Projectile.Center).SafeNormalize(forward);
                Projectile.velocity = RotateTowards(forward, desiredDirection, MathHelper.Max(0.01f, TrackingStrength)) *
                    MathHelper.Clamp(Projectile.velocity.Length() * 1.01f, 13f, 22f);
            }
            else
            {
                Projectile.velocity *= 1.004f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Color(255, 145, 100).ToVector3() * 0.5f);

            if (Main.dedServ || !Main.rand.NextBool(3))
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center - direction * 7f,
                Main.rand.NextBool(3) ? DustID.Smoke : DustID.Torch,
                -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.55f, 0.55f),
                0,
                Color.Lerp(new Color(255, 150, 95), new Color(255, 230, 190), Main.rand.NextFloat(0.15f, 0.55f)),
                Main.rand.NextFloat(0.8f, 1.2f));
            dust.noGravity = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.Resize(88, 88);
                Projectile.penetrate = -1;
                Projectile.maxPenetrate = -1;
                Projectile.usesLocalNPCImmunity = true;
                Projectile.localNPCHitCooldown = -1;
                Projectile.Damage();
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.34f, Pitch = -0.05f }, Projectile.Center);

            if (Main.dedServ)
                return;

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 5.8f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(4) ? DustID.Smoke : DustID.Torch,
                    velocity,
                    0,
                    Color.Lerp(new Color(255, 150, 95), new Color(255, 230, 180), Main.rand.NextFloat(0.2f, 0.6f)),
                    Main.rand.NextFloat(0.95f, 1.35f));
                dust.noGravity = true;
            }

            DirectionalPulseRing pulse = new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 185, 120) * 0.8f,
                new Vector2(1f, 1.9f),
                0f,
                0.1f,
                0.04f,
                16);
            GeneralParticleHandler.SpawnParticle(pulse);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition,
                    null,
                    new Color(255, 155, 95, 0) * (0.12f + completion * 0.2f),
                    Projectile.rotation,
                    origin,
                    Projectile.scale * (0.9f + completion * 0.16f),
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }

        private static Vector2 RotateTowards(Vector2 currentDirection, Vector2 desiredDirection, float maxTurnRadians)
        {
            float currentAngle = currentDirection.ToRotation();
            float desiredAngle = desiredDirection.ToRotation();
            float delta = MathHelper.WrapAngle(desiredAngle - currentAngle);
            delta = MathHelper.Clamp(delta, -maxTurnRadians, maxTurnRadians);
            return (currentAngle + delta).ToRotationVector2();
        }
    }
}
