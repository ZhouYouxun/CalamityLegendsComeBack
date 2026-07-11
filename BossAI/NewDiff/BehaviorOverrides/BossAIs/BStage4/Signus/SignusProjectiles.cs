using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Signus
{
    internal static class SignusFx
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

    // COSMIC MINE — Twisting Mine Grid node; pull-lines connect adjacent mines, self-detonates after 5s.
    public class CosmicMineProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 300; // 5s

        public override void SetDefaults()
        {
            Projectile.width = 20; Projectile.height = 20;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.05f;
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Vector2.Zero, 100, default, 0.9f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.2f }, Projectile.Center);
            SignusFx.Burst(Projectile.Center, 5f, 14);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float flicker = 0.6f + 0.4f * MathF.Sin(Projectile.timeLeft * 0.3f);
            Color c = Color.Lerp(new Color(255, 60, 60), new Color(160, 60, 220), flicker);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), c, Projectile.rotation, new Vector2(0.5f), new Vector2(18f, 18f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // COSMIC KUNAI — knives hold in a fan, then turn 90 degrees and stab from behind.
    public class SignusKunaiProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/CosmicKunai";
        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 20; Projectile.height = 20;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 200;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.velocity *= 0.92f;
                if (Projectile.localAI[0] >= 24f)
                {
                    Projectile.localAI[1] = 1f;
                    Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                    Vector2 dir = target.active ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) : Vector2.UnitY;
                    Projectile.velocity = dir * 20f;
                    SoundEngine.PlaySound(SoundID.Item18 with { Volume = 0.4f }, Projectile.Center);
                }
            }
            Projectile.rotation = Projectile.localAI[1] >= 1f ? Projectile.velocity.ToRotation() : Projectile.rotation + 0.15f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            SignusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // COSMILAMP — a hovering lantern fires a rotating cross-beam.
    public class CosmilampLanternProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Summon/Cosmilamp";

        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 26;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 130;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI() { Projectile.rotation += 0.015f; }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 dir1 = Projectile.rotation.ToRotationVector2();
            Vector2 dir2 = (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2();
            Vector2 s1 = Projectile.Center - dir1 * 900f, e1 = Projectile.Center + dir1 * 900f;
            Vector2 s2 = Projectile.Center - dir2 * 900f, e2 = Projectile.Center + dir2 * 900f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), s1, e1, 18f, ref collisionPoint) ||
                   Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), s2, e2, 18f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220) * 0.6f, Projectile.rotation, new Vector2(0.5f), new Vector2(1800f, 12f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220) * 0.6f, Projectile.rotation + MathHelper.PiOver2, new Vector2(0.5f), new Vector2(1800f, 12f), SpriteEffects.None, 0f);
            SignusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, 0f, origin, 1f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // A generic reflect-kunai fired every time Signus decloaks during P2 (design doc's "4 reflective kunai").
    public class ReflectKunaiProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/CosmicKunai";

        public override void SetDefaults()
        {
            Projectile.width = 16; Projectile.height = 16;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 120;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.2f;
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.05f, 100, default, 0.8f);
                d.fadeIn = 1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 0.8f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // AETHER'S WHISPER — bullets bounce off the boundary, leaving a bounce-trail behind.
    public class WhisperBulletProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16; Projectile.height = 16;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 2; Projectile.timeLeft = 200;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            float half = Projectile.ai[0];
            Vector2 center = new(Projectile.ai[1], Projectile.ai[2]);
            if (half > 0f && (Projectile.Center - center).Length() > half)
            {
                Projectile.velocity = (center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Projectile.velocity.Length();
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.3f }, Projectile.Center);
            }
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.05f, 100, default, 0.85f);
                d.fadeIn = 1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(20f, 8f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 160, 255), rot, new Vector2(0.5f), new Vector2(14f, 4f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // DEATH'S ASCENSION — a scythe crescent that inflicts Decay (no life regeneration).
    public class DeathsAscensionScytheProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/DeathsAscension";

        public override void SetDefaults()
        {
            Projectile.width = 60; Projectile.height = 60;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 150;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.Bleeding, 180); // blocks life regen, matching the design doc's "Decay" (no healing)

        public override void AI()
        {
            Projectile.rotation += 0.2f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.06f, 100, default, 1.1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            SignusFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // EMPYREAN KNIVES — 8 daggers ring overhead, sequentially stabbing straight down through the player.
    public class EmpyreanKnifeProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/EmpyreanKnives";
        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 18; Projectile.height = 18;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 160;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[1] == 0f && Projectile.localAI[0] >= Projectile.ai[0])
            {
                Projectile.localAI[1] = 1f;
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Vector2 dir = target.active ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) : Vector2.UnitY;
                Projectile.velocity = dir * 16f;
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.35f }, Projectile.Center);
            }
            Projectile.rotation = Projectile.localAI[1] >= 1f ? Projectile.velocity.ToRotation() : Projectile.rotation + 0.1f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 0.9f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // KING OF CONSTELLATIONS — a draconic star-map anchors, then fires diagonal purple lightning lines.
    public class ConstellationGridProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 36;
        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 2200; Projectile.height = 2200;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = TelegraphTime + 40;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == TelegraphTime)
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f }, Projectile.Center);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 d1 = new Vector2(1f, 1f).SafeNormalize(Vector2.UnitX);
            Vector2 d2 = new Vector2(-1f, 1f).SafeNormalize(Vector2.UnitX);
            Vector2 s1 = Projectile.Center - d1 * 1100f, e1 = Projectile.Center + d1 * 1100f;
            Vector2 s2 = Projectile.Center - d2 * 1100f, e2 = Projectile.Center + d2 * 1100f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), s1, e1, 22f, ref collisionPoint) ||
                   Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), s2, e2, 22f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f);
            bool armed = Projectile.localAI[0] >= TelegraphTime;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float width = armed ? 24f : MathHelper.Lerp(2f, 8f, t);
            Color c = armed ? new Color(220, 160, 255) : Color.Lerp(new Color(100, 30, 160), Color.White, t);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), c, MathHelper.PiOver4, new Vector2(0.5f), new Vector2(2200f, width), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), c, -MathHelper.PiOver4, new Vector2(0.5f), new Vector2(2200f, width), SpriteEffects.None, 0f);
            return false;
        }
    }

    // MAGNETIC MELTDOWN — a spinning magnetic sphere absorbs nearby friendly projectiles then bursts into needles.
    public class MagneticSphereProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Magic/MagneticMeltdown";

        public override void SetDefaults()
        {
            Projectile.width = 40; Projectile.height = 40;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 100;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.2f;
            Projectile.scale = MathHelper.Lerp(0.7f, 1.3f, MathHelper.Clamp((100 - Projectile.timeLeft) / 100f, 0f, 1f));
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.friendly && !p.hostile && (p.Center - Projectile.Center).Length() < 60f)
                    p.Kill();
            }
            if (Main.rand.NextBool(2))
            {
                Vector2 around = Projectile.Center + Main.rand.NextVector2Circular(28f, 28f);
                Dust d = Dust.NewDustPerfect(around, DustID.PurpleTorch, (Projectile.Center - around) * 0.05f, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62, Projectile.Center);
            SignusFx.Burst(Projectile.Center, 6f, 20);
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            for (int i = 0; i < 16; i++)
            {
                float a = i * MathHelper.TwoPi / 16f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, a.ToRotationVector2() * 8f, ModContent.ProjectileType<ReflectKunaiProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            SignusFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, Projectile.scale, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    // NADIR — a singularity beneath the player spits rotating gear-blades upward.
    public class NadirGearProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/Nadir";

        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 130;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.25f;
            Projectile.velocity.Y -= 0.05f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.05f, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            SignusFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // THE SEVENS STRIKER — a 7-round rapid zig-zag folley.
    public class SevensStrikerBulletProj : ModProjectile
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
            Projectile.localAI[0]++;
            Vector2 baseDir = new(Projectile.ai[0], Projectile.ai[1]);
            Vector2 perp = new(-baseDir.Y, baseDir.X);
            float zigzag = MathF.Sin(Projectile.localAI[0] * 0.3f) * 2.5f;
            Projectile.velocity = baseDir * 18f + perp * zigzag;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 160, 255), rot, new Vector2(0.5f), new Vector2(16f, 5f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // VENUSIAN TRIDENT — flaming tridents impale the boundary, raining gravity fire for 2s.
    public class VenusianTridentProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Magic/VenusianTrident";

        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 80;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 100;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.06f, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
            if (Projectile.timeLeft <= 30 && Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<VenusianFireRainProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            SignusFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1f, new Color(255, 140, 40));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class VenusianFireRainProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 120; // 2s

        public override void SetDefaults()
        {
            Projectile.width = 120; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.OnFire3, 100);

        public override void AI()
        {
            if (Main.rand.NextBool(2))
            {
                Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-55f, 55f), Main.rand.NextFloat(-300f, -20f));
                Dust d = Dust.NewDustPerfect(pos, DustID.Torch, new Vector2(0f, 4f), 100, default, 1.1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 120, 40, 0) * 0.4f * alpha, 0f, new Vector2(0.5f), new Vector2(110f, 320f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // REALITY RUPTURE — a rift portal; any friendly bullet that enters is reflected out the far side, hostile.
    public class SignusRiftProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 200;

        public override bool? CanDamage() => false;

        public override void SetDefaults()
        {
            Projectile.width = 90; Projectile.height = 160;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.03f;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.friendly && !p.hostile && (p.Center - Projectile.Center).Length() < 70f)
                {
                    Vector2 outVel = p.velocity.Length() > 0.1f ? p.velocity : Vector2.UnitY * 8f;
                    p.Kill();
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, outVel, ModContent.ProjectileType<ReflectKunaiProj>(), Projectile.damage, 0f, Main.myPlayer);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f) * MathHelper.Clamp((Lifetime - Projectile.timeLeft) / 15f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220, 0) * 0.5f * alpha, Projectile.rotation, new Vector2(0.5f), new Vector2(90f, 160f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 160, 255) * alpha, Projectile.rotation, new Vector2(0.5f), new Vector2(50f, 130f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
