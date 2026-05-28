using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
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
            {
                RancorLavaMetaball.SpawnParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextFloat(20f, 36f));

                if (Main.rand.NextBool(2))
                {
                    Particle smoke = new MediumMistParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                        -Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1f, 2.2f) + Main.rand.NextVector2Circular(1.2f, 1.2f),
                        Color.OrangeRed,
                        Color.Transparent,
                        Main.rand.NextFloat(0.8f, 1.35f),
                        0.72f,
                        Main.rand.NextFloat(-0.04f, 0.04f));
                    GeneralParticleHandler.SpawnParticle(smoke);
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                if (oldCenter == Vector2.Zero)
                    continue;

                float opacity = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                float width = MathHelper.Lerp(42f, 16f, i / (float)Projectile.oldPos.Length);
                Vector2 drawPos = oldCenter - Main.screenPosition - normal * width * 0.5f;
                Main.EntitySpriteDraw(
                    pixel,
                    drawPos,
                    new Rectangle(0, 0, 1, 1),
                    new Color(255, 80, 20, 0) * 0.45f * opacity,
                    Projectile.rotation,
                    new Vector2(0f, 0.5f),
                    new Vector2(72f, width),
                    SpriteEffects.None);
            }

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
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                RancorLavaMetaball.SpawnParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(24f, 18f),
                    Main.rand.NextFloat(12f, 24f));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 120);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float fade = Projectile.timeLeft / 18f;
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(255, 90, 20, 0) * 0.12f * fade,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                new Vector2(0.42f, 0.18f) * (1f + (1f - fade) * 0.4f),
                SpriteEffects.None);
            return false;
        }
    }
}
