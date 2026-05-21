using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFHardMode_MeowCreature : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Ranged/MeowCreature";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float BounceCount => ref Projectile.localAI[1];
        private bool Homing => BounceCount >= 3f || Timer > 95f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 36;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = 190 * Projectile.MaxUpdates;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Homing)
            {
                Projectile.tileCollide = false;
                CalamityUtils.HomeInOnNPC(Projectile, ignoreTiles: false, 520f, 16f, 18f);
            }
            else
            {
                Projectile.velocity.Y += 0.16f;
                if (Projectile.velocity.Length() > 18f)
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 18f;
            }

            Lighting.AddLight(Projectile.Center, PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 142, 66)).ToVector3() * 0.35f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X * 0.86f;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y * 0.86f;

            BounceCount++;
            Projectile.netUpdate = true;
            if (BounceCount >= 3f)
                Projectile.timeLeft = Math.Max(Projectile.timeLeft, 100);

            SoundEngine.PlaySound(SoundID.Item57 with { Volume = 0.36f, Pitch = 0.22f }, Projectile.Center);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundStyle sound = Main.rand.NextBool() ? SoundID.Item58 : SoundID.Item57;
            SoundEngine.PlaySound(sound with { Volume = 0.45f }, Projectile.Center);

            if (Main.dedServ)
                return;

            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 142, 66));
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(16f, 16f);
                Color colorFire = Color.Lerp(theme, Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.8f), 0.35f);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(Projectile.Center, velocity, colorFire, Color.Black, Main.rand.NextFloat(0.6f, 1.6f), 220 - Main.rand.Next(60), 0.1f));
            }
        }

        private float WidthFunction(float completionRatio, Vector2 vertexPos) => (1f - completionRatio) * Projectile.scale * 9f;

        private Color ColorFunction(float completionRatio, Vector2 vertexPos)
        {
            float hue = 0.5f + 0.5f * completionRatio * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Color trailColor = Main.hslToRgb(hue, 1f, 0.8f);
            return trailColor * Projectile.Opacity;
        }

        public override void PostDraw(Color lightColor)
        {
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new PrimitiveSettings(WidthFunction, ColorFunction, (_, _) => Projectile.Size * 0.5f), 30);
            Texture2D value = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(value, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, value.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
        }
    }
}
