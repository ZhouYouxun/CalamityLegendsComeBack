using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.CeaselessVoid
{
    internal static class VoidFx
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

    // Shared 24-way disintegration blast orb, also the type the 6 Void Amplifiers deflect when it passes
    // through their orbit ring (see CeaselessVoidAI.UpdateOrbiterDeflection).
    public class VoidOrbProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16; Projectile.height = 16;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 120;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.06f, 100, default, 0.9f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(24f, 8f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 160, 255), rot, new Vector2(0.5f), new Vector2(16f, 4f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class VoidSplitLaserProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 14; Projectile.height = 14;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 100;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 13f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.05f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(200, 120, 255), rot, new Vector2(0.5f), new Vector2(18f, 5f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // MIRROR BLADE — a giant blade flies straight, bounces off the arena boundary and re-angles behind the player.
    public class MirrorBladeSwordProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/MirrorBlade";

        public override void SetDefaults()
        {
            Projectile.width = 60; Projectile.height = 60;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 200;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.2f;
            float half = Projectile.ai[1];
            if (half > 0f && Projectile.localAI[0] < Projectile.ai[0] && (Projectile.Center - new Vector2(Projectile.ai[2], Projectile.ai[3])).Length() > half)
            {
                Projectile.localAI[0]++;
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Vector2 dir = target.active ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) : -Projectile.velocity.SafeNormalize(Vector2.UnitY);
                Projectile.velocity = dir * Projectile.velocity.Length();
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f }, Projectile.Center);
                VoidFx.Burst(Projectile.Center, 4f, 10);
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
            VoidFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // VOID CONCENTRATION — mini singularities absorb nearby bullets, then core-detonate.
    public class VoidAbsorbHoleProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int AbsorbTime = 90;

        public override void SetDefaults()
        {
            Projectile.width = 50; Projectile.height = 50;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = AbsorbTime + 4;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.1f;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active && (target.Center - Projectile.Center).Length() < 300f)
                target.velocity += (Projectile.Center - target.Center).SafeNormalize(Vector2.Zero) * 0.12f;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.friendly && !p.hostile && (p.Center - Projectile.Center).Length() < 70f)
                {
                    p.Kill();
                    Projectile.localAI[1]++;
                }
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 around = Projectile.Center + Main.rand.NextVector2Circular(30f, 30f);
                Dust d = Dust.NewDustPerfect(around, DustID.PurpleTorch, (Projectile.Center - around) * 0.05f, 100, default, 1.1f);
                d.fadeIn = 1.2f; d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62, Projectile.Center);
            VoidFx.Burst(Projectile.Center, 6f, 20);
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathHelper.TwoPi / 8f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, a.ToRotationVector2() * 7f, ModContent.ProjectileType<VoidOrbProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float scale = MathHelper.Lerp(0.5f, 1.2f, MathHelper.Clamp(Projectile.localAI[0] / AbsorbTime, 0f, 1f));
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220, 0) * 0.5f, Projectile.rotation, new Vector2(0.5f), new Vector2(50f, 50f) * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(40, 10, 60), Projectile.rotation, new Vector2(0.5f), new Vector2(30f, 30f) * scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    // DARK SPARK — a void core bursts into a cross-shaped rotating laser.
    public class VoidDarkSparkCoreProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Magic/DarkSpark";
        private const int TelegraphTime = 40;
        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 24;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = TelegraphTime + 60;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity *= 0.95f;
            Projectile.rotation += Projectile.localAI[0] >= TelegraphTime ? 0.08f : 0.03f;
            if (Projectile.localAI[0] == TelegraphTime)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.3f }, Projectile.Center);
                VoidFx.Burst(Projectile.Center, 5f, 14);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            bool armed = Projectile.localAI[0] >= TelegraphTime;
            VoidFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            if (armed)
            {
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220) * 0.6f, Projectile.rotation, new Vector2(0.5f), new Vector2(700f, 14f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220) * 0.6f, Projectile.rotation + MathHelper.PiOver2, new Vector2(0.5f), new Vector2(700f, 14f), SpriteEffects.None, 0f);
            }
            return false;
        }
    }

    // EVENT HORIZON — a ring of star-dust smoothly contracts toward the boss's center.
    public class VoidShrinkRingProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 150;

        public override void SetDefaults()
        {
            Projectile.width = 700; Projectile.height = 700;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI() { Projectile.rotation += 0.02f; }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = MathHelper.Lerp(350f, 20f, MathHelper.Clamp((Lifetime - Projectile.timeLeft) / (float)Lifetime, 0f, 1f));
            float dist = Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2());
            return Math.Abs(dist - radius) < 24f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float radius = MathHelper.Lerp(350f, 20f, MathHelper.Clamp((Lifetime - Projectile.timeLeft) / (float)Lifetime, 0f, 1f));
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            for (int i = 0; i < 28; i++)
            {
                float a = MathHelper.TwoPi * i / 28f + Projectile.rotation;
                Vector2 p = pos + a.ToRotationVector2() * radius;
                Main.spriteBatch.Draw(pixel, p, new Rectangle(0, 0, 1, 1), new Color(190, 120, 255), 0f, new Vector2(0.5f), new Vector2(9f, 9f), SpriteEffects.None, 0f);
            }
            return false;
        }
    }

    // MISTLESTORM — sine-wave shadow leaf blade.
    public class MistlestormLeafProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Magic/Mistlestorm";

        public override void SetDefaults()
        {
            Projectile.width = 22; Projectile.height = 22;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 160;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Vector2 baseDir = new(Projectile.ai[0], Projectile.ai[1]);
            Vector2 perp = new(-baseDir.Y, baseDir.X);
            float wave = MathF.Sin(Projectile.localAI[0] * 0.15f + Projectile.ai[2]) * 3.6f;
            Projectile.velocity = baseDir * 11f + perp * wave;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.06f, 100, default, 0.85f);
                d.fadeIn = 1f; d.noGravity = true;
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

    // ONTOLOGICAL DESPOILER — sine-wave bullet spray, bobs up and down as it travels.
    public class OntologicalBulletProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 14; Projectile.height = 14;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 130;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity.Y += MathF.Sin(Projectile.localAI[0] * 0.1f) * 0.3f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.06f, 100, default, 0.75f);
                d.fadeIn = 1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(200, 120, 255), rot, new Vector2(0.5f), new Vector2(16f, 6f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // SEALED SINGULARITY — a dense central black hole pulls the player and fires two vertical void pillars.
    public class SealedSingularityCoreProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 40;
        public override bool? CanDamage() => false;

        public override void SetDefaults()
        {
            Projectile.width = 60; Projectile.height = 60;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = TelegraphTime + 60;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.06f;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active && (target.Center - Projectile.Center).Length() < 500f)
                target.velocity += (Projectile.Center - target.Center).SafeNormalize(Vector2.Zero) * 0.2f;

            if (Projectile.localAI[0] == TelegraphTime && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f }, Projectile.Center);
                foreach (float dir in new float[] { -1f, 1f })
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(0f, dir), ModContent.ProjectileType<SingularityPillarProj>(), Projectile.damage, 0f, Main.myPlayer, dir);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220, 0) * 0.5f, Projectile.rotation, new Vector2(0.5f), new Vector2(60f, 60f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(20, 5, 30), Projectile.rotation, new Vector2(0.5f), new Vector2(40f, 40f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class SingularityPillarProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 100; Projectile.height = 2000;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 60;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220, 0) * 0.5f * alpha, 0f, new Vector2(0.5f), new Vector2(200f, 2000f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 160, 255) * alpha, 0f, new Vector2(0.5f), new Vector2(90f, 2000f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // TACTICIAN'S TRUMP — 4 tarot cards align, then fire vertical light-grid columns.
    public class TacticianCardLaserProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/PhantasmalFuryProj";

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 24;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 140;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
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
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 0.9f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // ETERNITY — a slow-rotating, wide purple beam that cuts across half the arena.
    public class EternityBeamProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2600; Projectile.height = 100;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 130;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.ai[0] + MathHelper.ToRadians(120f) * MathHelper.Clamp(Projectile.localAI[0] / 120f, 0f, 1f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 start = Projectile.Center - dir * 1300f;
            Vector2 end = Projectile.Center + dir * 1300f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 50f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220, 0) * 0.4f, Projectile.rotation, new Vector2(0.5f), new Vector2(2600f, 220f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 160, 255), Projectile.rotation, new Vector2(0.5f), new Vector2(2600f, 100f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // PHANTASMAL FURY — a homing blue phantom that leaves a damaging spark trail.
    public class PhantasmalPhantomProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/PhantasmalFuryProj";

        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 26;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 220;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active)
            {
                float wobble = MathF.Sin(Projectile.localAI[0] * 0.1f) * 0.3f;
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedBy(wobble) * 8f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.03f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch, -Projectile.velocity * 0.05f, 100, default, 0.9f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            VoidFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 0.8f, new Color(80, 160, 255));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 0.8f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // REALITY RUPTURE — twin rifts converge and collapse into a screen-wide shockwave.
    public class VoidRiftProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 80; Projectile.height = 500;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 130;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.Center += new Vector2(Projectile.ai[0], 0f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(0f, Main.rand.NextFloat(-240f, 240f)), DustID.PurpleTorch, Vector2.Zero, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
            if (Projectile.timeLeft <= 4 && Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.3f }, Projectile.Center);
                VoidFx.Burst(Projectile.Center, 8f, 30);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220, 0) * 0.5f, 0f, new Vector2(0.5f), new Vector2(80f, 500f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(210, 140, 255) * 0.7f, 0f, new Vector2(0.5f), new Vector2(40f, 500f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
