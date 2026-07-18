using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal sealed class SHPCNecroplasmBeam : ModProjectile, ILocalizedModType
    {
        private const float BeamLength = 1280f;
        private const float BeamWidth = 12f;
        private static readonly Color OuterColor = new(69, 54, 184);
        private static readonly Color CoreColor = new(170, 255, 255);

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float BeamRotation => Projectile.ai[0];
        private Vector2 Direction => BeamRotation.ToRotationVector2();

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 12;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                Projectile.Center + Direction * BeamLength, BeamWidth, ref collisionPoint);
        }

        public override void AI()
        {
            Projectile.rotation = BeamRotation;
            Lighting.AddLight(Projectile.Center + Direction * 42f, CoreColor.ToVector3() * 0.48f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
            float opacity = Utils.GetLerpValue(0f, 3f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 4f, 12 - Projectile.timeLeft, true);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = new(0f, 0.5f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(pixel, drawPosition, null, OuterColor * (0.60f * opacity), BeamRotation, origin,
                new Vector2(BeamLength, BeamWidth * 0.75f), SpriteEffects.None);
            Main.EntitySpriteDraw(pixel, drawPosition, null, CoreColor * (0.88f * opacity), BeamRotation, origin,
                new Vector2(BeamLength, BeamWidth * 0.24f), SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
