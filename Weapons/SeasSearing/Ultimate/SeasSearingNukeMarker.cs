using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingNukeMarker : ModProjectile, ILocalizedModType
    {
        private const int AlarmFrames = 300;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width      = 16;
            Projectile.height     = 16;
            Projectile.penetrate  = -1;
            Projectile.timeLeft   = AlarmFrames;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            float completion = 1f - Projectile.timeLeft / (float)AlarmFrames;
            Lighting.AddLight(Projectile.Center, Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.WarningOrange, completion).ToVector3() * 0.42f);

            if (Projectile.timeLeft % 60 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.65f, Pitch = MathHelper.Lerp(-0.55f, 0.18f, completion) }, Projectile.Center);
                SeasSearingVisualUtility.ShakeAt(Projectile.Center, 1.4f + completion * 2.2f, 2200f);
            }

            if (Projectile.timeLeft % 9 == 0)
                SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 2f + completion * 3f, 24f + completion * 120f, 20,
                    Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.WarningOrange, completion));
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer != Projectile.owner) return;

            Vector2 spawnPos = Projectile.Center + new Vector2(Main.rand.NextFloat(-160f, 160f), -1450f);
            Vector2 velocity = (Projectile.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 24f;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(), spawnPos, velocity,
                ModContent.ProjectileType<SeasSearingThermonuclearWarhead>(),
                Projectile.damage, 14f, Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D ring  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float completion = 1f - Projectile.timeLeft / (float)AlarmFrames;
            float pulse      = 0.82f + 0.18f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f);
            Vector2 center   = Projectile.Center - Main.screenPosition;
            Color warning    = (Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.WarningOrange, completion) with { A = 0 }) * (0.65f + completion * 0.35f);

            Main.EntitySpriteDraw(bloom, center, null, warning * 0.38f, 0f, bloom.Size() * 0.5f, 0.22f + completion * 0.38f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring,  center, null, warning * 0.86f, Main.GlobalTimeWrappedHourly * 1.8f,  ring.Size() * 0.5f, (0.2f + completion * 1.15f) * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring,  center, null, (Color.White with { A = 0 }) * 0.34f, -Main.GlobalTimeWrappedHourly * 2.4f, ring.Size() * 0.5f, 0.12f + completion * 0.32f, SpriteEffects.None, 0);
            return false;
        }
    }
}
