using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.PlaguebringerGoliath
{
    // Shared plague-green burst, mirroring Cryogen's EmitFrostBurst standard (fadeIn bloom + upward-biased scatter).
    internal static class PlagueFx
    {
        public static void Burst(Vector2 position, float speed, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Dust d = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(40f, 40f), DustID.Venom);
                d.velocity = Main.rand.NextVector2Circular(speed, speed) - Vector2.UnitY * 1.4f;
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
    // VIRULENCE — slow toxic wave that splits into weak-tracking micro-waves after traveling a set distance.
    // =====================================================================================================================
    public class VirulentWaveProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/VirulentWave";

        public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6)
            {
                Projectile.frame = (Projectile.frame + 1) % 4;
                Projectile.frameCounter = 0;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Projectile.ai[0] == 0f && Projectile.localAI[0] >= 20f)
            {
                Projectile.ai[0] = 1f;
                Projectile.Kill();
                return;
            }

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, -Projectile.velocity * 0.1f, 100, default, 1.1f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.5f, Pitch = 0.3f }, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 6; i++)
            {
                Vector2 vel = Projectile.velocity.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-40f, 40f))) * Main.rand.NextFloat(0.8f, 1.1f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<VirulentShardProj>(), Projectile.damage, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(1, 4, 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            PlagueFx.DrawBackglow(Main.spriteBatch, tex, pos, frame, Projectile.rotation, origin, Projectile.scale, new Color(90, 220, 60));
            Main.spriteBatch.Draw(tex, pos, frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class VirulentShardProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 140;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active && !target.dead)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 14f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.02f); // weak tracking, per design
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, -Projectile.velocity * 0.1f, 100, default, 0.9f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle source = new Rectangle(0, 0, 1, 1);
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, source, new Color(90, 220, 60, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(22f, 8f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, source, new Color(150, 255, 110), rot, new Vector2(0.5f), new Vector2(14f, 4f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // MALEVOLENCE — arrows rise, hover at the ceiling, then execute a staggered horizontal volley at the player's Y.
    // Telegraph IS the rise-and-hover — arrows are visible and stationary well before the deadly horizontal pass.
    // =====================================================================================================================
    public class PlagueArrowProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/PlagueArrow";

        // ai[0]: horizontal fire direction (+-1), ai[1]: per-arrow stagger delay (frames after lock)
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 400;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (Projectile.localAI[0] < 30f)
            {
                // Rising phase
                Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, 0f, 0.08f);
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
            else if (Projectile.localAI[0] < 30f + Projectile.ai[1])
            {
                // Hover and wait for this arrow's turn in the executioner volley
                Projectile.velocity *= 0.9f;
            }
            else if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                Projectile.velocity = new Vector2(Projectile.ai[0] * 15f, 0f);
                Projectile.rotation = Projectile.velocity.ToRotation();
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.35f, Pitch = 0.3f }, Projectile.Center);
            }

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, -Projectile.velocity * 0.08f, 100, default, 0.85f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            bool armed = Projectile.localAI[1] >= 1f;
            // Bright pre-fire flash on the last few hover frames so the volley beat is readable
            float flashT = MathHelper.Clamp((Projectile.localAI[0] - (24f + Projectile.ai[1])) / 6f, 0f, 1f);
            Color tint = armed ? Color.White : Color.Lerp(Color.White, Color.Yellow, flashT * 0.7f);
            PlagueFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(90, 220, 60));
            Main.spriteBatch.Draw(tex, pos, null, tint, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // PLAGUE STAFF — three fang sigils converge on a shared point after a visible windup, then shatter outward.
    // =====================================================================================================================
    public class PlagueFangProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/PlagueFang";

        // ai[0]: converge target X, ai[1]: converge target Y (packed via localAI instead, see below)
        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 100;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => Projectile.localAI[0] >= 45f ? null : (bool?)false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Vector2 convergePoint = new Vector2(Projectile.ai[0], Projectile.ai[1]);

            if (Projectile.localAI[0] < 45f)
            {
                // Windup: spins in place, scaling up — the telegraph
                Projectile.rotation += 0.12f;
                Projectile.scale = MathHelper.Lerp(0.6f, 1.1f, Projectile.localAI[0] / 45f);
                if (Main.rand.NextBool(2))
                {
                    Vector2 around = Projectile.Center + Main.rand.NextVector2Circular(30f, 30f);
                    Dust d = Dust.NewDustPerfect(around, DustID.Venom, (Projectile.Center - around) * 0.05f, 100, default, 1f);
                    d.fadeIn = 1.1f;
                    d.noGravity = true;
                }
            }
            else if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                Projectile.velocity = (convergePoint - Projectile.Center).SafeNormalize(Vector2.UnitY) * 15f;
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f }, Projectile.Center);
            }
            else
            {
                Projectile.rotation += 0.15f;
                if ((Projectile.Center - convergePoint).LengthSquared() < 900f)
                    Projectile.Kill();
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.localAI[1] < 1f)
                return; // died mid-windup (e.g. attack cut short) — no shatter
            SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.6f }, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 4; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / 4f + Main.rand.NextFloat(MathHelper.TwoPi / 4f)).ToRotationVector2() * 7f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<VirulentShardProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            PlagueFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, Projectile.scale, new Color(140, 90, 220));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // FUEL CELL BUNDLE — tossed flask arcs down, shatters into a lingering bubbling acid pool.
    // =====================================================================================================================
    public class FuelCellFlaskProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Summon/FuelCellBundle";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.35f;
            Projectile.rotation += 0.15f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, -Projectile.velocity * 0.1f, 100, default, 0.9f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft) => Splash();

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Splash();
            Projectile.Kill();
            return false;
        }

        private void Splash()
        {
            SoundEngine.PlaySound(SoundID.Item54 with { Volume = 0.6f, Pitch = 0.2f }, Projectile.Center);
            PlagueFx.Burst(Projectile.Center, 4f, 14);
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<AcidPoolProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 0.9f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class AcidPoolProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 180; // 3 seconds

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Main.rand.NextBool(4))
            {
                Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-55f, 55f), 6f);
                Dust d = Dust.NewDustPerfect(pos, DustID.Venom, new Vector2(0f, Main.rand.NextFloat(-3f, -1f)), 100, default, Main.rand.NextFloat(1f, 1.4f));
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 24f, 0f, 1f) * MathHelper.Clamp((Lifetime - Projectile.timeLeft) / 12f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(90, 220, 60, 0) * 0.5f * alpha, 0f, new Vector2(0.5f), new Vector2(120f, 16f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(150, 255, 110) * 0.6f * alpha, 0f, new Vector2(0.5f), new Vector2(112f, 8f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // THE SYRINGE — thrown javelin that embeds on impact and shatters into glass shards.
    // =====================================================================================================================
    public class TheSyringeProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/TheSyringe";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 40;
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
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, -Projectile.velocity * 0.1f, 100, default, 0.9f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
            if (Projectile.timeLeft <= 20)
                Projectile.Kill(); // "embeds" in the arena wall after its flight window
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Shatter, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * Main.rand.NextFloat(4f, 8f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<VirulentShardProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            PlagueFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(90, 220, 60));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // THE HIVE — slow-drifting nuke; long airborne telegraph before its radial burst.
    // =====================================================================================================================
    public class HiveNukeProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/HiveNuke";
        private const int FuseTime = 120;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = FuseTime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.03f;
            Projectile.scale = 1f + 0.1f * MathF.Sin(Projectile.timeLeft * 0.15f) * MathHelper.Clamp((FuseTime - Projectile.timeLeft) / 30f, 0f, 1f);
            if (Main.rand.NextBool(2))
            {
                Vector2 around = Projectile.Center + Main.rand.NextVector2Circular(20f, 20f);
                Dust d = Dust.NewDustPerfect(around, DustID.Venom, (Projectile.Center - around) * 0.06f, 100, default, 1.1f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.9f }, Projectile.Center);
            Player p = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (p.active)
                p.Calamity().GeneralScreenShakePower = 8f;
            PlagueFx.Burst(Projectile.Center, 6f, 30);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 16; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * Main.rand.NextFloat(6f, 9f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<VirulentShardProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            PlagueFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, Projectile.scale, new Color(90, 220, 60));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // PESTILENT DEFILER — sine-wave bullet, oscillating in the perpendicular axis as it travels.
    // =====================================================================================================================
    public class SicknessRoundProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/SicknessRound";

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Vector2 baseDir = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            Vector2 perp = new Vector2(-baseDir.Y, baseDir.X);
            float wave = MathF.Sin(Projectile.localAI[0] * 0.18f) * 3.2f;
            Projectile.velocity = baseDir * 13f + perp * wave;
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, -Projectile.velocity * 0.08f, 100, default, 0.8f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            PlagueFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(90, 220, 60));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // MALACHITE — daggers hold in a fan, then lock onto the player's position at that instant and dash in, staggered.
    // =====================================================================================================================
    public class MalachiteDaggerProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Rogue/MalachiteProj";

        // ai[0]: this dagger's lock delay (frames before it fires)
        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 200;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.rotation += 0.1f;
                if (Projectile.localAI[0] >= Projectile.ai[0])
                {
                    Projectile.localAI[1] = 1f;
                    Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                    Vector2 dir = target.active ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) : Vector2.UnitY;
                    Projectile.velocity = dir * 20f;
                    SoundEngine.PlaySound(SoundID.Item18 with { Volume = 0.4f, Pitch = 0.3f }, Projectile.Center);
                }
            }
            else
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, -Projectile.velocity * 0.1f, 100, default, 1f);
                    d.fadeIn = 1.1f;
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            bool armed = Projectile.localAI[1] >= 1f;
            float readyT = MathHelper.Clamp((Projectile.localAI[0] - (Projectile.ai[0] - 8f)) / 8f, 0f, 1f);
            Color tint = armed ? Color.White : Color.Lerp(Color.White, new Color(200, 255, 190), readyT);
            PlagueFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(90, 220, 100));
            Main.spriteBatch.Draw(tex, pos, null, tint, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // BLIGHT SPEWER — arc-sweeping flame stream; each puff drifts and falls like slow embers.
    // =====================================================================================================================
    public class BlightFlameProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/BlightFlames";

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.96f;
            Projectile.velocity.Y += 0.05f;
            Projectile.rotation += 0.05f;
            Projectile.alpha = (int)MathHelper.Lerp(0f, 255f, (float)(90 - Projectile.timeLeft) / 90f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, -Projectile.velocity * 0.05f, 100, default, 1f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            float op = (255 - Projectile.alpha) / 255f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(140, 255, 100) * op, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // PANDEMIC — twin yoyos co-orbiting the player, shrinking radius over their lifetime.
    // =====================================================================================================================
    public class PandemicYoyoProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/Yoyos/PandemicYoyo";
        private const float StartRadius = 160f;
        private const float EndRadius = 80f;
        private const int ShrinkTime = 180;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = ShrinkTime + 20;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.12f;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active && !target.dead)
            {
                float radius = MathHelper.Lerp(StartRadius, EndRadius, MathHelper.Clamp(Projectile.localAI[0] / ShrinkTime, 0f, 1f));
                float angle = Projectile.ai[1] + Projectile.localAI[0] * 0.02f;
                Vector2 desired = target.Center + angle.ToRotationVector2() * radius;
                Projectile.Center = Vector2.Lerp(Projectile.Center, desired, 0.08f);
            }

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, Vector2.Zero, 100, default, 0.9f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            PlagueFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1.1f, new Color(90, 220, 60));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1.1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // PLAGUE TAINTED SMG — straight bullet with a slight arc-in acceleration (never pure constant velocity).
    // =====================================================================================================================
    public class PlagueTaintedBulletProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/PlagueTaintedProjectile";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Projectile.velocity.Length() < 20f)
                Projectile.velocity *= 1.02f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, -Projectile.velocity * 0.06f, 100, default, 0.75f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
