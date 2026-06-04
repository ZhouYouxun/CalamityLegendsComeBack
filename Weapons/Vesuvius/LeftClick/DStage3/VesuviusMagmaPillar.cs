using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.DStage3
{
    public class VesuviusMagmaPillar : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 22;
            Projectile.extraUpdates = 5;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.95f, 0.24f, 0.06f);

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft % 2 == 0)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<VesuviusMagmaResidual>(),
                    Math.Max(1, (int)(Projectile.damage * 0.34f)),
                    0f,
                    Projectile.owner,
                    Projectile.rotation);
            }

            if (!Main.dedServ)
                VesuviusProjectileVisuals.SpawnPillarTrail(Projectile, 1f + Projectile.ai[1] * 0.08f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                if (oldCenter == Vector2.Zero)
                    continue;

                float opacity = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                float progress = i / (float)Projectile.oldPos.Length;
                float width = MathHelper.Lerp(54f, 16f, progress);
                Vector2 drawPos = oldCenter - Main.screenPosition - normal * width * 0.5f;

                Main.EntitySpriteDraw(
                    bloom,
                    oldCenter - Main.screenPosition,
                    null,
                    VesuviusProjectileVisuals.LavaOrange with { A = 0 } * 0.16f * opacity,
                    Projectile.rotation,
                    bloom.Size() * 0.5f,
                    new Vector2(width / bloom.Width * 1.2f, 0.28f),
                    SpriteEffects.None);

                Main.EntitySpriteDraw(
                    pixel,
                    drawPos,
                    new Rectangle(0, 0, 1, 1),
                    VesuviusProjectileVisuals.RavagerSmoke with { A = 0 } * 0.2f * opacity,
                    Projectile.rotation,
                    new Vector2(0f, 0.5f),
                    new Vector2(84f, width * 1.45f),
                    SpriteEffects.None);

                Main.EntitySpriteDraw(
                    pixel,
                    drawPos,
                    new Rectangle(0, 0, 1, 1),
                    VesuviusProjectileVisuals.LavaOrange with { A = 0 } * 0.54f * opacity,
                    Projectile.rotation,
                    new Vector2(0f, 0.5f),
                    new Vector2(72f, width),
                    SpriteEffects.None);

                Main.EntitySpriteDraw(
                    pixel,
                    oldCenter - Main.screenPosition - normal * width * 0.16f,
                    new Rectangle(0, 0, 1, 1),
                    Color.White with { A = 0 } * 0.46f * opacity,
                    Projectile.rotation,
                    new Vector2(0f, 0.5f),
                    new Vector2(58f, width * 0.32f),
                    SpriteEffects.None);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }

    public class VesuviusMagmaResidual : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.rotation = Projectile.ai[0];
            if (!Main.dedServ)
                VesuviusProjectileVisuals.SpawnPillarResidual(Projectile.Center, Projectile.rotation, 0.82f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 120);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smoke = ModContent.Request<Texture2D>("CalamityMod/Particles/HighResFoggyCircleHardEdge").Value;
            float fade = Projectile.timeLeft / 18f;
            Main.EntitySpriteDraw(
                smoke,
                Projectile.Center - Main.screenPosition - Vector2.UnitY * 5f,
                null,
                Color.Lerp(Color.Black, VesuviusProjectileVisuals.RavagerSmoke, 0.5f) * 0.18f * fade,
                Projectile.rotation,
                smoke.Size() * 0.5f,
                new Vector2(0.55f, 0.24f) * (1f + (1f - fade) * 0.55f),
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                VesuviusProjectileVisuals.LavaOrange with { A = 0 } * 0.16f * fade,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                new Vector2(0.42f, 0.18f) * (1f + (1f - fade) * 0.4f),
                SpriteEffects.None);
            return false;
        }
    }
}
