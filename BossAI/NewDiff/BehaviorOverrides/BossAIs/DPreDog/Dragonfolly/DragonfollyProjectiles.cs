using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Dragonfolly
{
    internal static class FollyFx
    {
        public static void Burst(Vector2 position, float speed, int count, int dustType = DustID.GoldFlame)
        {
            for (int i = 0; i < count; i++)
            {
                Dust d = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(40f, 40f), dustType);
                d.velocity = Main.rand.NextVector2Circular(speed, speed) - Vector2.UnitY * 1.2f;
                d.scale = Main.rand.NextFloat(1.2f, 1.6f);
                d.fadeIn = 1.5f;
                d.noGravity = true;
            }
        }

        public static void DrawBackglow(SpriteBatch sb, Texture2D tex, Vector2 pos, Rectangle? frame, float rotation, Vector2 origin, float scale, Color glow)
        {
            glow.A = 0;
            for (int i = 0; i < 12; i++)
            {
                Vector2 off = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 3.5f;
                sb.Draw(tex, pos + off, frame, glow, rotation, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }

    // =====================================================================================================================
    // GILDED PROBOSCIS — a fan of flame sparks bursts from the beak tip at dash's end.
    // =====================================================================================================================
    public class ProboscisFlameProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.98f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.06f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 210, 60, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(30f, 10f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 240, 160), rot, new Vector2(0.5f), new Vector2(20f, 5f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // GOLDEN EAGLE — golden feather-bolts; variant A curves outward into a wing shape, variant B stays a
    // tight direct barrage.
    // =====================================================================================================================
    public class GoldenEagleBoltProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 130;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            bool curve = Projectile.ai[0] != 0f;
            if (curve && Projectile.localAI[0] > 12f)
            {
                float side = Projectile.ai[1];
                Projectile.velocity = Projectile.velocity.RotatedBy(side * 0.025f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.06f, 100, default, 0.8f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 210, 60, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(22f, 8f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 240, 170), rot, new Vector2(0.5f), new Vector2(15f, 4f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // ROUGE SLASH — pink slash waves, growing in size and slowing in speed with each successive wave.
    // =====================================================================================================================
    public class RougeSlashProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 200;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            float scale = Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0];
            Projectile.scale = scale;
            Projectile.width = (int)(60 * scale);
            Projectile.height = (int)(60 * scale);
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f * scale, 20f * scale), DustID.PinkTorch, -Projectile.velocity * 0.05f, 100, default, scale);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            float scale = Projectile.scale;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 100, 180, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(90f * scale, 30f * scale), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 170, 220), rot, new Vector2(0.5f), new Vector2(70f * scale, 16f * scale), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // DRACONIC SWARM SIGIL — phantom drones dart diagonally, leaving lingering tesla lines behind them.
    // =====================================================================================================================
    public class DraconicDroneProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 160;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.localAI[0]++;
            if (Main.rand.NextBool(2) && Main.netMode != NetmodeID.MultiplayerClient && Projectile.localAI[0] % 4 == 0)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<TeslaTrailProj>(), Projectile.damage / 2, 0f, Main.myPlayer);

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.06f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 220, 100), Projectile.rotation, new Vector2(0.5f), new Vector2(24f, 12f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class TeslaTrailProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 150; // 2.5s

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 60, 60) * 0.6f * alpha, 0f, new Vector2(0.5f), new Vector2(8f, 8f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // THUNDERBOLT WRATH — the bolt impacts a boundary and becomes a sweeping vertical lightning waterfall.
    // =====================================================================================================================
    public class ThunderboltArrowProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 60;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * 0.06f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
            if (Projectile.timeLeft <= 40 && Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float side = Math.Sign(Projectile.velocity.X) == 0 ? 1f : Math.Sign(Projectile.velocity.X);
                    int idx = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<LightningWaterfallProj>(), Projectile.damage, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles)
                        Main.projectile[idx].ai[0] = side;
                }
                SoundEngine.PlaySound(SoundID.Item93, Projectile.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(200, 120, 255, 0) * 0.4f, Projectile.rotation, new Vector2(0.5f), new Vector2(90f, 24f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(230, 190, 255), Projectile.rotation, new Vector2(0.5f), new Vector2(70f, 10f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class LightningWaterfallProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 1600;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 130;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.Center += new Vector2(Projectile.ai[0] * 6f, 0f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(0f, Main.rand.NextFloat(-700f, 700f)), DustID.Electric, Vector2.Zero, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(200, 120, 255, 0) * 0.5f * alpha, 0f, new Vector2(0.5f), new Vector2(180f, 1600f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(230, 190, 255) * alpha, 0f, new Vector2(0.5f), new Vector2(90f, 1600f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // SONIC BOOM OVERDRIVE — expanding rings of sonic-boom pressure radiate from the boss's fixed center point.
    // =====================================================================================================================
    public class SonicBoomRingProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            bool pulsing = Projectile.ai[0] != 0f;
            Projectile.localAI[0]++;
            float speed = pulsing ? 6f + MathF.Sin(Projectile.localAI[0] * 0.2f) * 3f : 7f;
            Projectile.scale += speed / 60f;
            Projectile.width = (int)(40 * Projectile.scale);
            Projectile.height = (int)(40 * Projectile.scale);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float radius = 20f * Projectile.scale;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 15f, 0f, 1f);
            for (int i = 0; i < 40; i++)
            {
                float a = MathHelper.TwoPi * i / 40f;
                Vector2 p = pos + a.ToRotationVector2() * radius;
                Main.spriteBatch.Draw(pixel, p, new Rectangle(0, 0, 1, 1), new Color(255, 240, 160) * 0.7f * alpha, 0f, new Vector2(0.5f), new Vector2(10f, 10f), SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
