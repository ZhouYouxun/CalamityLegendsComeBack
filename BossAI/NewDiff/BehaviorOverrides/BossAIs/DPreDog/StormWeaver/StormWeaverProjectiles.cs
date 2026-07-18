using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.StormWeaver
{
    internal static class WeaverFx
    {
        public static void Burst(Vector2 position, float speed, int count, int dustType = DustID.Electric)
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

    // SKYTIDE DRAGOON — a zig-zag dash leaves crystal beacons that chain-detonate into radial water-arcs.
    public class SkytideBeaconProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 20; Projectile.height = 20;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 90;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.1f;
            if (Projectile.localAI[0] >= Projectile.ai[0] && Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                SoundEngine.PlaySound(SoundID.Item93, Projectile.Center);
                WeaverFx.Burst(Projectile.Center, 5f, 12);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int count = (int)(Projectile.ai[1] <= 0f ? 8 : Projectile.ai[1]);
                    for (int i = 0; i < count; i++)
                    {
                        float a = i * MathHelper.TwoPi / count;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, a.ToRotationVector2() * 8f, ModContent.ProjectileType<StormArcProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            bool armed = Projectile.localAI[1] >= 1f;
            if (armed) return false;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float t = MathHelper.Clamp(Projectile.localAI[0] / Math.Max(Projectile.ai[0], 1f), 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), Color.Lerp(new Color(120, 190, 255), Color.White, t), Projectile.rotation, new Vector2(0.5f), new Vector2(16f, 16f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class StormArcProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 14; Projectile.height = 14;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 90;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * 0.06f, 100, default, 0.85f);
                d.fadeIn = 1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(120, 190, 255, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(22f, 8f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 240, 255), rot, new Vector2(0.5f), new Vector2(15f, 4f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // STORM — 4 lightning nodes flash in sequence (A) or all together after a shared delay (B).
    public class WeaverStormNodeProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool? CanDamage() => Projectile.localAI[0] >= Projectile.ai[0] ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 160;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.15f;
            if (Projectile.localAI[0] == Projectile.ai[0])
            {
                SoundEngine.PlaySound(SoundID.Item93, Projectile.Center);
                WeaverFx.Burst(Projectile.Center, 4f, 10);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            bool armed = Projectile.localAI[0] >= Projectile.ai[0];
            float t = MathHelper.Clamp(Projectile.localAI[0] / Math.Max(Projectile.ai[0], 1f), 0f, 1f);
            Color c = armed ? new Color(220, 240, 255) : Color.Lerp(new Color(90, 140, 220), Color.White, t);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), c, Projectile.rotation, new Vector2(0.5f), new Vector2(26f, 26f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // VOLTERION — a slow gravity ball that pulls the player in, bursting into radial frost-sparks.
    public class VolterionSphereProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 40; Projectile.height = 40;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 220;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.05f;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active)
            {
                if ((target.Center - Projectile.Center).Length() < 500f)
                    target.velocity += (Projectile.Center - target.Center).SafeNormalize(Vector2.Zero) * 0.15f;
                if ((target.Center - Projectile.Center).Length() < 50f)
                    Projectile.Kill();
            }
            if (Main.rand.NextBool(2))
            {
                Vector2 around = Projectile.Center + Main.rand.NextVector2Circular(30f, 30f);
                Dust d = Dust.NewDustPerfect(around, DustID.Electric, (Projectile.Center - around) * 0.04f, 100, default, 1.2f);
                d.fadeIn = 1.2f; d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f }, Projectile.Center);
            WeaverFx.Burst(Projectile.Center, 6f, 20);
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int count = (int)(Projectile.ai[0] <= 0f ? 12 : Projectile.ai[0]);
            for (int i = 0; i < count; i++)
            {
                float a = i * MathHelper.TwoPi / count;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, a.ToRotationVector2() * 7f, ModContent.ProjectileType<StormArcProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(120, 190, 255, 0) * 0.5f, Projectile.rotation, new Vector2(0.5f), new Vector2(50f, 50f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 240, 255), Projectile.rotation, new Vector2(0.5f), new Vector2(30f, 30f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // AQUAS SCEPTER — a boiling steam breath, followed by grenades that cross-burst on impact.
    public class AquasSteamProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 60;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.OnFire3, 90);

        public override void AI()
        {
            Projectile.velocity *= 0.97f;
            Projectile.alpha = (int)MathHelper.Lerp(0f, 255f, (60 - Projectile.timeLeft) / 60f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Cloud, -Projectile.velocity * 0.05f, 100, default, 1.2f);
                d.fadeIn = 1.2f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float op = (255 - Projectile.alpha) / 255f;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), Color.White * 0.6f * op, Projectile.rotation, new Vector2(0.5f), new Vector2(28f, 28f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class CorinthNukeProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Ranged/CorinthPrime";

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 24;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 120;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.15f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * 0.06f, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62, Projectile.Center);
            WeaverFx.Burst(Projectile.Center, 5f, 16);
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            foreach (float rot in new float[] { 0f, MathHelper.PiOver2, MathHelper.Pi, -MathHelper.PiOver2 })
            {
                Vector2 vel = rot.ToRotationVector2() * 10f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<StormArcProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // STELLAR TORUS — a rune ring closes in around the player, sweeping inward beams.
    public class StellarTorusRingProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 500; Projectile.height = 500;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 150;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI() { Projectile.rotation += 0.03f; }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = MathHelper.Lerp(260f, 40f, MathHelper.Clamp((150f - Projectile.timeLeft) / 130f, 0f, 1f));
            float dist = Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2());
            return dist < radius + 20f && dist > radius - 20f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float radius = MathHelper.Lerp(260f, 40f, MathHelper.Clamp((150f - Projectile.timeLeft) / 130f, 0f, 1f));
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            for (int i = 0; i < 24; i++)
            {
                float a = MathHelper.TwoPi * i / 24f + Projectile.rotation;
                Vector2 p = pos + a.ToRotationVector2() * radius;
                Main.spriteBatch.Draw(pixel, p, new Rectangle(0, 0, 1, 1), new Color(160, 200, 255), 0f, new Vector2(0.5f), new Vector2(8f, 8f), SpriteEffects.None, 0f);
            }
            return false;
        }
    }

    public class TeslaConductionProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 20; Projectile.height = 20;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 150;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * 0.06f, 100, default, 0.9f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 240, 255), rot, new Vector2(0.5f), new Vector2(20f, 6f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // TWISTING THUNDER — twin double-helix electric lines.
    public class TwistingHelixProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 24;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 160;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Vector2 baseDir = new(Projectile.ai[0], Projectile.ai[1]);
            Vector2 perp = new(-baseDir.Y, baseDir.X);
            float wave = MathF.Sin(Projectile.localAI[0] * 0.15f + Projectile.ai[2]) * 4f;
            Projectile.velocity = baseDir * 10f + perp * wave;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * 0.06f, 100, default, 0.9f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 120, 200, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(24f, 8f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 200, 230), rot, new Vector2(0.5f), new Vector2(16f, 4f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class PackRocketProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16; Projectile.height = 16;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 160;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 11f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.035f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * 0.06f, 100, default, 0.8f);
                d.fadeIn = 1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(120, 190, 255), rot, new Vector2(0.5f), new Vector2(16f, 8f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // SHADOWBOLT STAFF — a dark cloud ceiling periodically strikes down bolts of shadow lightning.
    public class WeaverDarkCloudProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 150;

        public override bool? CanDamage() => false;

        public override void SetDefaults()
        {
            Projectile.width = 60; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] % 30 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item93, Projectile.Center);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(0f, 16f), ModContent.ProjectileType<ShadowboltStrikeProj>(), Projectile.damage, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(60, 30, 90) * 0.8f, 0f, new Vector2(0.5f), new Vector2(120f, 40f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class ShadowboltStrikeProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 20;
        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 1200;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = TelegraphTime + 14;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI() => Projectile.localAI[0]++;

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f);
            bool armed = Projectile.localAI[0] >= TelegraphTime;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float width = armed ? 30f : MathHelper.Lerp(2f, 8f, t);
            Color c = armed ? new Color(200, 120, 255) : Color.Lerp(new Color(90, 30, 140), Color.White, t);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), c, 0f, new Vector2(0.5f), new Vector2(width, 1200f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class SeadragonWallProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Ranged/Seadragon";

        public override void SetDefaults()
        {
            Projectile.width = 60; Projectile.height = 600;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 120;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(0f, Main.rand.NextFloat(-280f, 280f)), DustID.Water, -Projectile.velocity * 0.1f, 100, default, 1.2f);
                d.fadeIn = 1.2f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(60, 140, 220, 0) * 0.5f, 0f, new Vector2(0.5f), new Vector2(60f, 600f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(140, 210, 255) * 0.7f, 0f, new Vector2(0.5f), new Vector2(30f, 600f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // FOUR SEASONS — 4 colored star-cores fire arcing bolts, then converge into a cross-beam.
    public class SeasonStarProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/Galaxia";

        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 26;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 150;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        private static readonly Color[] Colors = { Color.LimeGreen, Color.Red, Color.Gold, Color.DeepSkyBlue };

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.12f;
            int season = (int)Projectile.ai[0];
            if (Projectile.localAI[0] == 40 + season * 8 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Vector2 vel = target.active ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * (6f + season) : Vector2.UnitY * 6f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<StormArcProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            int season = (int)Projectile.ai[0];
            Color glow = Colors[Math.Clamp(season, 0, 3)];
            WeaverFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 0.9f, glow);
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 0.9f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // REALITY RUPTURE — twin rifts converge to center, spewing void blades.
    public class WeaverRiftProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 80; Projectile.height = 400;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 130;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.Center += new Vector2(Projectile.ai[0], 0f);
            if (Main.rand.NextBool(3) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = new(-Projectile.ai[0] * 0.6f, Main.rand.NextFloat(-4f, 4f));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<StormArcProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(0f, Main.rand.NextFloat(-190f, 190f)), DustID.PurpleTorch, Vector2.Zero, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220, 0) * 0.5f, 0f, new Vector2(0.5f), new Vector2(80f, 400f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(210, 140, 255) * 0.7f, 0f, new Vector2(0.5f), new Vector2(40f, 400f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
