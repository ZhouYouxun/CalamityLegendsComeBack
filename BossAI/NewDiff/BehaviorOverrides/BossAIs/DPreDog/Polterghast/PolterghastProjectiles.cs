using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Polterghast
{
    internal static class GhastFx
    {
        public static void Burst(Vector2 position, float speed, int count, int dustType = DustID.PurpleTorch)
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

    // TERROR BLADE — moon-blade waves bounce off the cage walls up to 3 times, shedding tooth-shards each bounce.
    public class TerrorBladeWaveProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/TerrorBlade";

        public override void SetDefaults()
        {
            Projectile.width = 50; Projectile.height = 50;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 240;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.2f;
            float half = Projectile.ai[1];
            Vector2 center = new(Projectile.ai[2], Projectile.ai[3]);
            if (half > 0f && Projectile.localAI[0] < Projectile.ai[0] && (Projectile.Center - center).Length() > half)
            {
                Projectile.localAI[0]++;
                Projectile.velocity = (center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Projectile.velocity.Length();
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f }, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * MathHelper.TwoPi / 3f;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, a.ToRotationVector2() * 5f, ModContent.ProjectileType<ToothShardProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
                    }
                }
            }
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.06f, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            GhastFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1f, new Color(180, 30, 60));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class ToothShardProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 12; Projectile.height = 12;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 90;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.05f, 100, default, 0.7f);
                d.fadeIn = 1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 160, 255), Projectile.rotation, new Vector2(0.5f), new Vector2(10f, 10f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // BANSHEE HOOK — chains fire out and embed, then whip back into a thick cutting line.
    public class BansheeChainProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 30;
        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 50; Projectile.height = 50;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = TelegraphTime + 30;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == TelegraphTime)
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, Pitch = -0.2f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f);
            bool armed = Projectile.localAI[0] >= TelegraphTime;
            Vector2 dir = new Vector2(Projectile.ai[0], Projectile.ai[1]).SafeNormalize(Vector2.UnitY);
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = start - dir * 400f;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float width = armed ? 50f : MathHelper.Lerp(2f, 12f, t);
            Color c = armed ? new Color(180, 30, 60) : Color.Lerp(new Color(90, 10, 30), Color.White, t);
            Main.spriteBatch.Draw(pixel, start, new Rectangle(0, 0, 1, 1), c, (end - start).ToRotation(), new Vector2(0f, 0.5f), new Vector2((end - start).Length(), width), SpriteEffects.None, 0f);
            return false;
        }
    }

    // DAEMON'S FLAME — fireballs winding around a shared central axis in a sine spiral.
    public class DaemonsFireballProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Ranged/DaemonsFlame";

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 24;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 180;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Vector2 baseDir = new(Projectile.ai[0], Projectile.ai[1]);
            Vector2 perp = new(-baseDir.Y, baseDir.X);
            float wave = MathF.Sin(Projectile.localAI[0] * 0.1f + Projectile.ai[2]) * 5f;
            Projectile.velocity = baseDir * 8f + perp * wave;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.06f, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 0.9f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // FATE'S REVEAL — sighing skull sigils sequentially fire homing wraiths that loop back after passing.
    public class FatesRevealSigilProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool? CanDamage() => false;

        public override void SetDefaults()
        {
            Projectile.width = 40; Projectile.height = 40;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 200;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.08f;
            if (Projectile.localAI[0] == Projectile.ai[0] && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.4f }, Projectile.Center);
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Vector2 dir = target.active ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) : Vector2.UnitY;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dir * 9f, ModContent.ProjectileType<WraithProj>(), Projectile.damage, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            bool armed = Projectile.localAI[0] >= Projectile.ai[0];
            float t = MathHelper.Clamp(Projectile.localAI[0] / Math.Max(Projectile.ai[0], 1f), 0f, 1f);
            Color c = armed ? new Color(220, 160, 255) : Color.Lerp(new Color(90, 30, 140), Color.White, t);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), c, Projectile.rotation, new Vector2(0.5f), new Vector2(36f, 36f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class WraithProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/FatesRevealFlame";

        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 26;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 150;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active)
            {
                bool passedBy = Vector2.Dot(Projectile.velocity, target.Center - Projectile.Center) < 0f;
                if (passedBy && Projectile.localAI[0] > 20f)
                {
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 10f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.06f);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.06f, 100, default, 0.9f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            GhastFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // GHASTLY VISAGE — a giant face glides, pauses, then dive-bombs the player's position at high speed.
    public class GhastlyVisageFaceProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Magic/GhastlyVisage";
        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 90; Projectile.height = 90;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 160;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.velocity *= 0.94f;
                if (Projectile.localAI[0] >= 60f)
                {
                    Projectile.localAI[1] = 1f;
                    Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                    Vector2 dir = target.active ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) : Vector2.UnitY;
                    Projectile.velocity = dir * 26f;
                    SoundEngine.PlaySound(SoundID.Item68, Projectile.Center);
                    GhastFx.Burst(Projectile.Center, 4f, 12);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            GhastFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1.3f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1.3f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // ETHEREAL SUBJUGATOR — 3 minions orbit the player in a triangle, firing inward.
    public class SubjugatorMiniProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Summon/EtherealSubjugator";

        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 26;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 160;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active)
            {
                float angle = Projectile.ai[1] + Projectile.localAI[0] * 0.04f;
                float radius = Projectile.ai[0] <= 0f ? 200f : Projectile.ai[0];
                Vector2 desired = target.Center + angle.ToRotationVector2() * radius;
                Projectile.Center = Vector2.Lerp(Projectile.Center, desired, 0.15f);

                if (Projectile.localAI[0] % 40 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dir * 8f, ModContent.ProjectileType<GhostFireProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
                }
            }
            Projectile.rotation += 0.1f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            GhastFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 0.9f, new Color(220, 100, 200));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 0.9f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class GhostFireProj : ModProjectile
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
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch, -Projectile.velocity * 0.05f, 100, default, 0.75f);
                d.fadeIn = 1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 130, 200), Projectile.rotation, new Vector2(0.5f), new Vector2(12f, 12f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // GHOULISH GOUGER — a drill spear impacts the cage wall, then rolls along the inner edge for 1.5 laps.
    public class GougerDrillProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/GhoulishGouger";

        public override void SetDefaults()
        {
            Projectile.width = 40; Projectile.height = 40;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 260;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.3f;
            float half = Projectile.ai[0];
            Vector2 center = new(Projectile.ai[1], Projectile.ai[2]);

            if (Projectile.localAI[1] == 0f)
            {
                if (half > 0f && (Projectile.Center - center).Length() > half)
                {
                    Projectile.localAI[1] = 1f;
                    SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f }, Projectile.Center);
                }
            }
            else
            {
                Vector2 rel = Projectile.Center - center;
                float perimeter = half * 8f;
                float frac = PerimeterFractionOf(rel, half);
                Projectile.localAI[0] += 12f;
                float laps = Projectile.localAI[0] / perimeter;
                if (laps >= 1.5f) { Projectile.Kill(); return; }
                float newFrac = (frac + Projectile.localAI[0] / perimeter) % 1f;
                Projectile.Center = center + SquarePerimeterPoint(half, newFrac);
            }
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Vector2.Zero, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        private static float PerimeterFractionOf(Vector2 rel, float half)
        {
            if (Math.Abs(rel.Y - (-half)) < 1f) return (rel.X + half) / (half * 8f);
            if (Math.Abs(rel.X - half) < 1f) return 0.25f + (rel.Y + half) / (half * 8f);
            if (Math.Abs(rel.Y - half) < 1f) return 0.5f + (half - rel.X) / (half * 8f);
            return 0.75f + (half - rel.Y) / (half * 8f);
        }

        private static Vector2 SquarePerimeterPoint(float half, float frac)
        {
            frac = ((frac % 1f) + 1f) % 1f;
            float side = frac * 4f;
            int edge = (int)side;
            float t = side - edge;
            return edge switch
            {
                0 => new Vector2(-half + t * half * 2f, -half),
                1 => new Vector2(half, -half + t * half * 2f),
                2 => new Vector2(half - t * half * 2f, half),
                _ => new Vector2(-half, half - t * half * 2f),
            };
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            GhastFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // GALILEO GLADIUS — 7 rapid teleport-stabs, each leaving a glowing slash trail.
    public class GalileoSlashProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 30;

        public override void SetDefaults()
        {
            Projectile.width = 70; Projectile.height = 70;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI() { }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / (float)Lifetime, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 160, 255) * alpha, Projectile.rotation, new Vector2(0.5f), new Vector2(80f, 14f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // CRESCENT MOON — a chained blade swings like a pendulum across the lower half of the arena.
    public class CrescentPendulumProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/CrescentMoon";

        public override void SetDefaults()
        {
            Projectile.width = 60; Projectile.height = 60;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 200;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Vector2 pivot = new(Projectile.ai[0], Projectile.ai[1]);
            float angle = MathF.Sin(Projectile.localAI[0] * 0.045f) * 1.1f + MathHelper.PiOver2;
            float radius = 380f;
            Projectile.Center = pivot + angle.ToRotationVector2() * radius;
            Projectile.rotation += 0.15f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Vector2.Zero, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            GhastFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1.1f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1.1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // HALLEY'S INFERNO — a comet flies through, endlessly shedding a spiral tail of embers.
    public class HalleysCometProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Ranged/HalleysInferno";

        public override void SetDefaults()
        {
            Projectile.width = 34; Projectile.height = 34;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 150;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.localAI[0] % 6 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float side = (Projectile.localAI[0] / 6) % 2 == 0 ? 1f : -1f;
                Vector2 perp = new Vector2(-Projectile.velocity.Y, Projectile.velocity.X).SafeNormalize(Vector2.UnitX) * side;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, perp * 5f, ModContent.ProjectileType<GhostFireProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            GhastFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1f, new Color(255, 140, 40));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // ALPHA DRACONIS — a constellation anchor periodically fires homing blue fireballs.
    public class DraconisFireballProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 18; Projectile.height = 18;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 130;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 11f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.04f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch, -Projectile.velocity * 0.06f, 100, default, 0.9f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(90, 160, 255, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(24f, 8f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(180, 220, 255), rot, new Vector2(0.5f), new Vector2(16f, 4f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // STRATUS SPHERE — 3 cloud orbs linked by arcs periodically drop lightning rain.
    public class StratusCloudProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool? CanDamage() => false;

        public override void SetDefaults()
        {
            Projectile.width = 50; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 200;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] % 35 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawn = Projectile.Center + new Vector2(Main.rand.NextFloat(-40f, 40f), 0f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, new Vector2(0f, 13f), ModContent.ProjectileType<DraconisFireballProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(60, 30, 90) * 0.7f, 0f, new Vector2(0.5f), new Vector2(100f, 50f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // SIRIUS — a bright star charges, then bursts into 12 concentric expanding laser rings.
    public class SiriusStarProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 60;
        public override bool? CanDamage() => false;

        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = TelegraphTime + 4;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.scale = MathHelper.Lerp(0.5f, 1.4f, MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f));
            if (Projectile.localAI[0] == TelegraphTime && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.8f }, Projectile.Center);
                GhastFx.Burst(Projectile.Center, 7f, 30);
                for (int i = 0; i < 12; i++)
                {
                    float a = i * MathHelper.TwoPi / 12f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, a.ToRotationVector2() * 9f, ModContent.ProjectileType<ToothShardProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 240, 255) * MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0.2f, 1f), 0f, new Vector2(0.5f), new Vector2(30f, 30f) * Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    // WARLOK'S MOON FIST — a giant fist slams from above, splashing shockwaves left and right.
    public class MoonFistProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 90; Projectile.height = 90;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 130;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[1] == 0f && Projectile.localAI[0] >= Projectile.ai[0])
            {
                Projectile.localAI[1] = 1f;
                SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.3f }, Projectile.Center);
                GhastFx.Burst(Projectile.Center, 6f, 24);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    foreach (float dir in new float[] { -1f, 1f })
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(dir * 13f, 0f), ModContent.ProjectileType<GhostFireProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            bool armed = Projectile.localAI[1] >= 1f;
            float t = MathHelper.Clamp(Projectile.localAI[0] / Math.Max(Projectile.ai[0], 1f), 0f, 1f);
            Color c = armed ? new Color(160, 60, 220) : Color.Lerp(new Color(60, 20, 90), Color.White, t);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), c, 0f, new Vector2(0.5f), new Vector2(80f, 80f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // VEGA — a rotating net of 6 diagonal star-lines sweeps the arena.
    public class VegaLightNetProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2200; Projectile.height = 2200;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 200;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI() { Projectile.rotation += 0.012f; }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = Projectile.rotation + i * MathHelper.TwoPi / 6f;
                Vector2 dir = a.ToRotationVector2();
                Vector2 s = Projectile.Center - dir * 1100f, e = Projectile.Center + dir * 1100f;
                float collisionPoint = 0f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), s, e, 20f, ref collisionPoint))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            for (int i = 0; i < 6; i++)
            {
                float a = Projectile.rotation + i * MathHelper.TwoPi / 6f;
                Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 160, 255) * 0.7f, a, new Vector2(0.5f), new Vector2(2200f, 10f), SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
