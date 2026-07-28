using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    // Sea-blue variant of Hyperdeath Rift Scepter's delayed falling beam.
    internal sealed class BrinyBaron_UltimateAzureRiftBeam : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => BB_Balance.UltimateAzureRiftBeamLifetime - Projectile.timeLeft;
        private float BeamAngle => Projectile.ai[0];
        private Vector2 BeamStart => Projectile.Center + BeamAngle.ToRotationVector2() * BB_Balance.UltimateAzureRiftBeamLength;
        private Vector2 BeamDirection => BeamStart.DirectionTo(Projectile.Center);
        private bool CanDamageBeam => Age >= BB_Balance.UltimateAzureRiftBeamWindupFrames;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BB_Balance.UltimateAzureRiftBeamLifetime;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => CanDamageBeam ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!CanDamageBeam)
                return false;

            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(), BeamStart,
                Projectile.Center + BeamDirection * BB_Balance.UltimateAzureRiftBeamLength,
                42f, ref collisionPoint);
        }

        public override void AI()
        {
            if (Age == BB_Balance.UltimateAzureRiftBeamWindupFrames)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack")
                {
                    Volume = 0.34f,
                    Pitch = 0.18f,
                    MaxInstances = -1
                }, Projectile.Center);
            }

            Lighting.AddLight(Projectile.Center, 0.10f, 0.56f, 0.92f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float windup = Utils.GetLerpValue(0f, BB_Balance.UltimateAzureRiftBeamWindupFrames, Age, true);
            float fade = Utils.GetLerpValue(BB_Balance.UltimateAzureRiftBeamLifetime, BB_Balance.UltimateAzureRiftBeamLifetime - 5f, Age, true);
            float opacity = windup * fade;
            if (opacity <= 0f)
                return false;

            Texture2D glowBeam = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineThick").Value;
            Texture2D coreBeam = ModContent.Request<Texture2D>("CalamityMod/Particles/LineThick").Value;
            Vector2 drawPosition = BeamStart - Main.screenPosition;
            float rotation = BeamDirection.ToRotation() + MathHelper.PiOver2;
            float beamScaleY = BB_Balance.UltimateAzureRiftBeamLength / 975f;

            Main.EntitySpriteDraw(glowBeam, drawPosition, null, new Color(25, 154, 255, 0) * (0.75f * opacity),
                rotation, new Vector2(glowBeam.Width * 0.5f, glowBeam.Height), new Vector2(0.16f + 0.12f * windup, beamScaleY), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(coreBeam, drawPosition, null, Color.White * (0.82f * opacity),
                rotation, new Vector2(coreBeam.Width * 0.5f, coreBeam.Height), new Vector2(0.045f + 0.045f * windup, beamScaleY), SpriteEffects.None, 0);
            return false;
        }
    }
}
