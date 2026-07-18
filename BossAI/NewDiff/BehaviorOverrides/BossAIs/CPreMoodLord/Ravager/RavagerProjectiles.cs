using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.Ravager
{
    // Shared blood-red burst, matching the established Fx standard (fadeIn bloom + upward-biased scatter).
    internal static class RavagerFx
    {
        public static void Burst(Vector2 position, float speed, int count, int dustType = DustID.Blood)
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
    // ULTIMUS CLEAVER — ground cracks open from the impact point, chain-erupting rock spires left-to-right.
    // =====================================================================================================================
    public class SpikecragSpireProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Summon/SpikecragSpike";
        private const int TelegraphTime = 24;

        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 90;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TelegraphTime + 30;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == TelegraphTime)
            {
                SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
                RavagerFx.Burst(Projectile.Center, 4f, 10);
            }
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-16f, 16f), 30f), DustID.Blood, new Vector2(0f, -1f), 100, default, 0.9f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f);
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = new(tex.Width * 0.5f, tex.Height);
            Vector2 pos = Projectile.Center - Main.screenPosition;

            if (t < 1f)
            {
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 60, 60) * (0.3f + 0.3f * t), 0f, new Vector2(0.5f), new Vector2(30f, 6f), SpriteEffects.None, 0f);
            }
            else
            {
                RavagerFx.DrawBackglow(Main.spriteBatch, tex, pos, null, 0f, origin, 1f, new Color(200, 40, 40));
                Main.spriteBatch.Draw(tex, pos, null, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
            }
            return false;
        }
    }

    // =====================================================================================================================
    // REALM RAVAGER — a horizontal rift telegraphs, then blows open into a full-height energy net (variant A);
    // or a slow wall of rift-fire presses in from one screen edge (variant B).
    // =====================================================================================================================
    public class RealmRiftProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 30;

        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 2400;
            Projectile.height = 60;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TelegraphTime + 40;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == TelegraphTime)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = -0.1f }, Projectile.Center);
                RavagerFx.Burst(Projectile.Center, 5f, 20);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f);
            bool armed = Projectile.localAI[0] >= TelegraphTime;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float height = armed ? 220f : MathHelper.Lerp(2f, 16f, t);
            Color core = armed ? new Color(255, 80, 80) : Color.Lerp(new Color(160, 30, 30), Color.White, t);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), core * 0.4f, 0f, new Vector2(0.5f), new Vector2(2400f, height * 2.2f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), core, 0f, new Vector2(0.5f), new Vector2(2400f, height), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class RiftWallProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 2000;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.Center += new Vector2(Projectile.ai[0], 0f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(0f, Main.rand.NextFloat(-400f, 400f)), DustID.Blood, new Vector2(-Projectile.ai[0] * 0.3f, 0f), 100, default, 1.1f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(200, 40, 40, 0) * 0.5f, 0f, new Vector2(0.5f), new Vector2(90f, 2000f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 100, 100) * 0.7f, 0f, new Vector2(0.5f), new Vector2(40f, 2000f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // HEMATEMESIS — arcing blood blast; splits into radial gravity droplets on impact.
    // =====================================================================================================================
    public class HematemesisBloodProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.3f;
            Projectile.rotation += 0.1f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, -Projectile.velocity * 0.1f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft) => Splatter();
        public override bool OnTileCollide(Vector2 oldVelocity) { Splatter(); Projectile.Kill(); return false; }

        private void Splatter()
        {
            SoundEngine.PlaySound(SoundID.Item54 with { Volume = 0.6f, Pitch = -0.1f }, Projectile.Center);
            RavagerFx.Burst(Projectile.Center, 4f, 10);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            int count = (int)(Projectile.ai[0] <= 0f ? 6 : Projectile.ai[0]);
            for (int i = 0; i < count; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / count).ToRotationVector2() * Main.rand.NextFloat(3f, 6f) - Vector2.UnitY * 3f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<BloodDropletProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            RavagerFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(200, 30, 30));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class BloodDropletProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 100;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.25f;
            Projectile.rotation += 0.15f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, Vector2.Zero, 100, default, 0.8f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(200, 30, 30), Projectile.rotation, new Vector2(0.5f), new Vector2(12f, 12f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // CRANIUM SMASHER — flail launches out, pauses at range, then whips back scattering bone shards.
    // =====================================================================================================================
    public class CraniumFlailProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/CraniumSmasher";

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 44;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 160;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.2f;
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.velocity *= 0.94f;
                if (Projectile.velocity.Length() < 1f && Projectile.localAI[0] > 20f)
                {
                    Projectile.localAI[1] = 1f;
                    Projectile.velocity = Vector2.Zero;
                }
            }
            else if (Projectile.localAI[1] == 1f)
            {
                if (Projectile.localAI[0] >= Projectile.ai[0])
                {
                    Projectile.localAI[1] = 2f;
                    SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.6f }, Projectile.Center);
                    RavagerFx.Burst(Projectile.Center, 6f, 16);
                }
            }
            else
            {
                Vector2 owner = new(Projectile.ai[1], Projectile.ai[2]);
                Vector2 dir = (owner - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 22f, 0.15f);
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Bone, -Projectile.velocity * 0.1f, 100, default, 0.9f);
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
            RavagerFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(200, 40, 40));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // VESUVIUS — volcanic eruption; lava bombs arc up then drift down leaving hot vertical light-trails.
    // =====================================================================================================================
    public class VesuviusEmberProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/AsteroidMolten";

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.12f;
            if (Projectile.velocity.Y > 3f)
                Projectile.velocity.Y = 3f;
            Projectile.rotation += 0.08f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, new Vector2(0f, -1f), 100, default, 1.1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            RavagerFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(255, 140, 40));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 200, 80, 0) * 0.35f, MathHelper.PiOver2, new Vector2(0f, 0.5f), new Vector2(60f, 6f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // CORPUS AVERTOR — twin crescents thrust from either side, snap 90 degrees near center, diverge up/down.
    // =====================================================================================================================
    public class CorpusAvertorProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/CorpusAvertor";

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.15f;
            if (Projectile.localAI[1] == 0f && Projectile.localAI[0] >= Projectile.ai[0])
            {
                Projectile.localAI[1] = 1f;
                float turnDir = Projectile.ai[1] >= 0f ? 1f : -1f;
                Projectile.velocity = new Vector2(0f, turnDir * Math.Abs(Projectile.velocity.X));
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.4f }, Projectile.Center);
            }
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, -Projectile.velocity * 0.08f, 100, default, 0.9f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            RavagerFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(200, 40, 40));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // MUTILATOR — cross-slash blood wave that splits into 3 tracking blood spikes on wall impact.
    // =====================================================================================================================
    public class MutilatorWaveProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, -Projectile.velocity * 0.08f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
            if (Projectile.timeLeft <= 20 && Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 vel = Projectile.velocity.RotatedBy((i - 1) * 0.35f).SafeNormalize(Vector2.UnitX) * 10f;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<BloodDropletProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(200, 30, 30, 0) * 0.4f, Projectile.rotation, new Vector2(0.5f), new Vector2(60f, 34f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 80, 80) * 0.7f, Projectile.rotation, new Vector2(0.5f), new Vector2(50f, 16f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // LACERATOR — spiraling saw-yoyo trailing a cutting energy thread that lingers.
    // =====================================================================================================================
    public class LaceratorYoyoProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/LaceratorSaw";
        private const float StartRadius = 60f;
        private const float EndRadius = 220f;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 200;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.25f;
            NPC owner = Main.npc[(int)Projectile.ai[0]];
            if (owner.active)
            {
                float radius = MathHelper.Lerp(StartRadius, EndRadius, MathHelper.Clamp(Projectile.localAI[0] / 160f, 0f, 1f));
                float angle = Projectile.ai[1] + Projectile.localAI[0] * 0.05f;
                Vector2 desired = owner.Center + angle.ToRotationVector2() * radius;
                Projectile.Center = Vector2.Lerp(Projectile.Center, desired, 0.15f);
            }

            if (Main.rand.NextBool(2) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int idx = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<LaceratorThreadProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            RavagerFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(200, 40, 40));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class LaceratorThreadProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 180; // 3 seconds

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
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 80, 80) * 0.6f * alpha, 0f, new Vector2(0.5f), new Vector2(8f, 8f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // CLARET CANNON — rapid blood-bolt spray that reflects off the arena edge into thin lasers.
    // =====================================================================================================================
    public class ClaretBoltProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Typeless/ClaretCannonProj";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 100;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            float half = Projectile.ai[0];
            if (half > 0f && Projectile.localAI[0] == 0f && Math.Abs(Projectile.Center.X - Projectile.ai[1]) > half)
            {
                Projectile.localAI[0] = 1f;
                Projectile.velocity.X *= -1f;
                Projectile.velocity *= 1.4f;
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.3f }, Projectile.Center);
            }
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, -Projectile.velocity * 0.06f, 100, default, 0.8f);
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

    // =====================================================================================================================
    // ARTERIAL ASSAULT — 8 vertical blood pillars sweep horizontally like a moving fence.
    // =====================================================================================================================
    public class ArterialColumnProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 26;

        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 1600;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 200;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] >= TelegraphTime)
                Projectile.Center += new Vector2(Projectile.ai[0], 0f);
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(0f, Main.rand.NextFloat(-700f, 700f)), DustID.Blood, Vector2.Zero, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f);
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            bool armed = Projectile.localAI[0] >= TelegraphTime;
            float width = armed ? 60f : MathHelper.Lerp(3f, 12f, t);
            Color core = armed ? new Color(255, 70, 70) : Color.Lerp(new Color(150, 30, 30), Color.White, t);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), core * 0.4f, 0f, new Vector2(0.5f), new Vector2(width * 2f, 1600f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), core, 0f, new Vector2(0.5f), new Vector2(width, 1600f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // BLOOD BOILER — a long boiling-blood-mist breath that inflicts a stacking slow debuff.
    // =====================================================================================================================
    public class BloodBoilerFlameProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.Venom, 240);

        public override void AI()
        {
            Projectile.velocity *= 0.97f;
            Projectile.alpha = (int)MathHelper.Lerp(0f, 255f, (60 - Projectile.timeLeft) / 60f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, -Projectile.velocity * 0.05f, 100, default, 1f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            float op = (255 - Projectile.alpha) / 255f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(220, 40, 40) * op, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // SANGUINE FLARE — a cross-shaped nuclear blast erupting over the player's head.
    // =====================================================================================================================
    public class SanguineFlareProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/SanguineFlareProj";
        private const int TelegraphTime = 40;

        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TelegraphTime + 20;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.scale = MathHelper.Lerp(0.5f, 1.3f, MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f));
            if (Projectile.localAI[0] == TelegraphTime)
            {
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.8f }, Projectile.Center);
                Player p = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (p.active) p.Calamity().GeneralScreenShakePower = 8f;
                RavagerFx.Burst(Projectile.Center, 6f, 24);
            }
            if (Main.rand.NextBool(2))
            {
                Vector2 around = Projectile.Center + Main.rand.NextVector2Circular(20f, 20f);
                Dust d = Dust.NewDustPerfect(around, DustID.Blood, (Projectile.Center - around) * 0.05f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            RavagerFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, Projectile.scale, new Color(220, 30, 30));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);

            if (Projectile.localAI[0] >= TelegraphTime)
            {
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 60, 60) * 0.6f, 0f, new Vector2(0.5f), new Vector2(700f, 20f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 60, 60) * 0.6f, MathHelper.PiOver2, new Vector2(0.5f), new Vector2(700f, 20f), SpriteEffects.None, 0f);
            }
            return false;
        }
    }

    // =====================================================================================================================
    // VISCERA — organ-crystal prisms that leech HP from the player back to the boss.
    // =====================================================================================================================
    public class VisceraSpireProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            NPC owner = Main.npc[(int)Projectile.ai[0]];
            if (owner.active)
            {
                owner.life = Math.Min(owner.lifeMax, owner.life + (int)(owner.lifeMax * 0.005f));
                owner.HealEffect((int)(owner.lifeMax * 0.005f));
            }
        }

        public override void AI()
        {
            Projectile.rotation += 0.1f;
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, -Projectile.velocity * 0.05f, 100, default, 0.9f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            RavagerFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(200, 30, 30));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // DRAGONBLOOD DISGORGER — a fountain of dragon-blood spreads into a burning pool.
    // =====================================================================================================================
    public class DragonbloodLavaProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.3f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, -Projectile.velocity * 0.1f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft) => Pool();
        public override bool OnTileCollide(Vector2 oldVelocity) { Pool(); Projectile.Kill(); return false; }

        private void Pool()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<DragonbloodPoolProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(200, 30, 30), Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class DragonbloodPoolProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 300; // 5 seconds

        public override void SetDefaults()
        {
            Projectile.width = 140;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.OnFire3, 120);

        public override void AI()
        {
            if (Main.rand.NextBool(3))
            {
                Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-65f, 65f), 6f);
                Dust d = Dust.NewDustPerfect(pos, DustID.Torch, new Vector2(0f, Main.rand.NextFloat(-2f, -0.5f)), 100, default, Main.rand.NextFloat(1f, 1.3f));
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f) * MathHelper.Clamp((Lifetime - Projectile.timeLeft) / 14f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(200, 30, 30, 0) * 0.5f * alpha, 0f, new Vector2(0.5f), new Vector2(140f, 20f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 120, 40) * 0.6f * alpha, 0f, new Vector2(0.5f), new Vector2(130f, 10f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // BLOODSOAKED CRASHER — a ground-shaking hammer slam sends wall-hugging shockwaves both directions.
    // =====================================================================================================================
    public class BloodsoakedWaveProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity *= 1.01f;
            Projectile.rotation += Math.Sign(Projectile.velocity.X) * 0.1f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, -Projectile.velocity * 0.06f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            RavagerFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1.2f, new Color(200, 30, 30));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1.2f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
