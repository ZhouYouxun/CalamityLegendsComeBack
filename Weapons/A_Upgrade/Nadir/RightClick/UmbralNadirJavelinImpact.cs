using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityMod.Enums;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.RightClick
{
    /// <summary>
    /// 前两支标枪命中时展开的黑白双相坍缩印记。
    /// 无伤害，只负责把“命中”读成先收缩、再旋开的太极深渊。
    /// </summary>
    public class UmbralNadirJavelinImpact : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float DirectionAngle => ref Projectile.ai[0];
        private ref float Time => ref Projectile.localAI[0];
        private static readonly Color VoidWhite = new(220, 247, 255);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 30;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Time++;
            if (Time == 1f)
                SpawnInitialBurst();
        }

        private void SpawnInitialBurst()
        {
            Vector2 center = Projectile.Center;
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, Color.Black,
                "CalamityMod/Particles/SmallBloom", Vector2.One, DirectionAngle, 0.08f, 0.72f, 28, false));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, Color.Black,
                Vector2.One, DirectionAngle, 0.12f, 0.6f, 24, false));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, VoidWhite with { A = 0 },
                Vector2.One, -DirectionAngle, 0.14f, 0.76f, 22), false, GeneralDrawLayer.AfterEverything);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, VoidWhite with { A = 0 },
                "CalamityMod/Particles/BloomRing", Vector2.One, DirectionAngle, 0f, 0.9f, 20), false, GeneralDrawLayer.AfterEverything);

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 5.5f);
                Color color = i % 2 == 0 ? Color.Black : VoidWhite with { A = 0 };
                GeneralParticleHandler.SpawnParticle(new GenericBloom(center, velocity, color,
                    Main.rand.NextFloat(0.11f, 0.23f), Main.rand.Next(10, 17), true, false),
                    i % 2 != 0, GeneralDrawLayer.AfterEverything);
            }
            UmbralNadirVisuals.ScreenShake(center, 1.8f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = MathHelper.Clamp(Time / 30f, 0f, 1f);
            float opacity = MathF.Sin(progress * MathHelper.Pi);
            float spin = DirectionAngle + Time * 0.24f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Texture2D circularSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearSmokey").Value;
            Texture2D halfSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmearSwipe").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;

            Main.EntitySpriteDraw(circularSmear, drawPos, null, Color.Black * (0.64f * opacity), -spin * 0.55f,
                circularSmear.Size() * 0.5f, new Vector2(0.7f + progress * 0.52f, 0.62f + progress * 0.38f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(halfSmear, drawPos, null, VoidWhite with { A = 0 } * (0.46f * opacity), spin,
                halfSmear.Size() * 0.5f, new Vector2(0.75f + progress * 0.65f, 0.6f + progress * 0.45f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(halfSmear, drawPos, null, Color.Black * (0.56f * opacity), spin + MathHelper.Pi,
                halfSmear.Size() * 0.5f, new Vector2(0.75f + progress * 0.65f, 0.6f + progress * 0.45f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring, drawPos, null, VoidWhite with { A = 0 } * (0.38f * opacity), -spin * 0.2f,
                ring.Size() * 0.5f, 0.46f + progress * 0.66f, SpriteEffects.None, 0);

            Vector2 orbit = spin.ToRotationVector2() * (8f + progress * 18f);
            Main.EntitySpriteDraw(bloom, drawPos + orbit, null, VoidWhite with { A = 0 } * (0.62f * opacity), 0f,
                bloom.Size() * 0.5f, 0.14f + progress * 0.16f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPos - orbit, null, Color.Black * (0.78f * opacity), 0f,
                bloom.Size() * 0.5f, 0.2f + progress * 0.2f, SpriteEffects.None, 0);
            return false;
        }
    }
}
