using System.Linq;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive
{
    // 肉山后解锁的常态天降弹幕，主视觉是绿色 primitive trail，本体只做一颗很小的核心。
    internal sealed class BFPassiveRain : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity *= 1.024f;

            float maxSpeed = 26f;
            if (Projectile.velocity.Length() > maxSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * maxSpeed;

            Lighting.AddLight(Projectile.Center, new Color(90, 255, 150).ToVector3() * 0.42f);

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextBool(2) ? DustID.GemEmerald : DustID.TerraBlade,
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    100,
                    Color.Lerp(new Color(90, 255, 150), Color.White, 0.25f),
                    Main.rand.NextFloat(0.8f, 1.15f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.26f, Pitch = 0.22f }, target.Center);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GemEmerald,
                    Main.rand.NextVector2Circular(2.4f, 2.4f),
                    100,
                    new Color(90, 255, 150),
                    Main.rand.NextFloat(0.9f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D coreTexture = TextureAssets.Projectile[ProjectileID.SeedlerNut].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color drawColor = Color.Lerp(new Color(80, 255, 150), Color.White, 0.18f) * 0.85f;

            Main.EntitySpriteDraw(
                coreTexture,
                drawPosition,
                null,
                drawColor,
                Projectile.rotation,
                coreTexture.Size() * 0.5f,
                0.36f,
                SpriteEffects.None,
                0f);

            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (Projectile.oldPos[0] == Vector2.Zero)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));

            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(
                    OuterWidthFunction,
                    OuterColorFunction,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                Projectile.oldPos.Length * 2);

            Vector2[] coreTrail = Projectile.oldPos.Take(8).ToArray();
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                coreTrail,
                new PrimitiveSettings(
                    CoreWidthFunction,
                    CoreColorFunction,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                coreTrail.Length * 2);
        }

        private float OuterWidthFunction(float completionRatio, Vector2 _)
        {
            return MathHelper.Lerp(20f, 6f, completionRatio) * Projectile.scale;
        }

        private Color OuterColorFunction(float completionRatio, Vector2 _)
        {
            Color startColor = Color.Lerp(new Color(70, 255, 140), Color.White, 0.12f);
            Color endColor = new Color(18, 125, 70);
            return Color.Lerp(startColor, endColor, completionRatio) * (1f - completionRatio) * 0.95f;
        }

        private float CoreWidthFunction(float completionRatio, Vector2 _)
        {
            return MathHelper.Lerp(9f, 2f, completionRatio) * Projectile.scale;
        }

        private Color CoreColorFunction(float completionRatio, Vector2 _)
        {
            Color startColor = Color.Lerp(Color.White, new Color(170, 255, 205), 0.55f);
            Color endColor = new Color(70, 255, 150);
            return Color.Lerp(startColor, endColor, completionRatio) * (1f - completionRatio) * 0.9f;
        }
    }
}
