using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.AstrumAureus
{
    // Shared gold/violet celestial burst, mirroring the Cryogen/Plague Fx standard (fadeIn bloom + upward-biased scatter).
    internal static class AureusFx
    {
        public static void Burst(Vector2 position, float speed, int count, int dustType = DustID.GoldFlame)
        {
            for (int i = 0; i < count; i++)
            {
                Dust d = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(40f, 40f), dustType);
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
    // NEBULASH — a lash-line telegraph along a fixed direction; 0.5s later it chain-explodes base-to-tip.
    // ai[0]/ai[1]: unit direction. ai[2]: reach in pixels. Variant (2 lashes forming an X) is just 2 spawns from the AI.
    // =====================================================================================================================
    public class NebulashLashProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 30;
        private const int SegmentCount = 8;

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TelegraphTime + 4;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        private Vector2 Dir => new(Projectile.ai[0], Projectile.ai[1]);
        private float Reach => Projectile.ai[2] <= 0f ? 800f : Projectile.ai[2];

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == TelegraphTime)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = -0.1f }, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < SegmentCount; i++)
                    {
                        Vector2 segPos = Projectile.Center + Dir * (Reach * (i + 1) / SegmentCount);
                        int idx = Projectile.NewProjectile(Projectile.GetSource_FromThis(), segPos, Vector2.Zero, ModContent.ProjectileType<NebulashExplosionProj>(), Projectile.damage, 0f, Main.myPlayer);
                        if (idx >= 0 && idx < Main.maxProjectiles)
                            Main.projectile[idx].ai[0] = i * 3f;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f);
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = start + Dir * Reach * t;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle src = new(0, 0, 1, 1);
            float rot = Dir.ToRotation();
            float len = Reach * t;
            float flicker = 0.5f + 0.5f * MathF.Sin((float)Main.GameUpdateCount * 0.6f);
            Main.spriteBatch.Draw(pixel, start, src, new Color(160, 60, 220, 0) * 0.5f, rot, new Vector2(0f, 0.5f), new Vector2(len, 10f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, start, src, new Color(230, 200, 255) * (0.5f + flicker * 0.4f), rot, new Vector2(0f, 0.5f), new Vector2(len, 3f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class NebulashExplosionProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

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
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] < Projectile.ai[0])
                return;
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.4f, Pitch = 0.2f }, Projectile.Center);
                AureusFx.Burst(Projectile.Center, 5f, 10, DustID.PurpleTorch);
            }
            if (Projectile.localAI[0] >= Projectile.ai[0] + 8f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.localAI[1] < 1f)
                return false;
            float p = (Projectile.localAI[0] - Projectile.ai[0]) / 8f;
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            float scale = MathHelper.Lerp(0.6f, 1.6f, p);
            float alpha = 1f - p;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(200, 120, 255) * alpha, 0f, origin, scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // AURORA BLAZER — blue bolts are slow, long-lived space-fillers; pink bolts are fast, direct strikes.
    // ai[0]: 0 = blue (slow), 1 = pink (fast).
    // =====================================================================================================================
    public class AuroraBoltProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/RancorFog";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = Projectile.ai[0] == 0f ? 300 : 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            bool blue = Projectile.ai[0] == 0f;
            if (Main.rand.NextBool(blue ? 4 : 2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, blue ? DustID.Electric : DustID.PinkTorch, -Projectile.velocity * 0.06f, 100, default, blue ? 1.3f : 0.9f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            bool blue = Projectile.ai[0] == 0f;
            Color glow = blue ? new Color(90, 160, 255) : new Color(255, 90, 200);
            float scale = blue ? 1.3f : 0.9f;
            AureusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, scale, glow);
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // ALULA AUSTRALIS — a homing feather that locks its target point at launch and pierces straight through it.
    // =====================================================================================================================
    public class AlulaFeatherProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/AerSigilFeather";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
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
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.08f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            AureusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(230, 200, 60));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // BOREALIS BOMBER — falls onto a target reticle, then bursts into two horizontal sweep lines at impact height.
    // =====================================================================================================================
    public class BorealisBombProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Summon/AureusBomber";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 200;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.1f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.1f, 100, default, 1.1f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
            if (Projectile.ai[0] > 0f && Projectile.Center.Y >= Projectile.ai[0])
            {
                Projectile.Center = new Vector2(Projectile.Center.X, Projectile.ai[0]);
                Detonate();
                Projectile.Kill();
            }
        }

        private void Detonate()
        {
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.7f }, Projectile.Center);
            AureusFx.Burst(Projectile.Center, 5f, 16);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            foreach (float dir in new float[] { -1f, 1f })
            {
                int idx = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(dir * 15f, 0f), ModContent.ProjectileType<BorealisSweepLineProj>(), Projectile.damage, 0f, Main.myPlayer);
                if (idx >= 0 && idx < Main.maxProjectiles)
                    Main.projectile[idx].ai[0] = dir;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            AureusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(230, 200, 60));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class BorealisSweepLineProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 90;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 40;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(0f, Main.rand.NextFloat(-40f, 40f)), DustID.GoldFlame, new Vector2(Projectile.ai[0] * -2f, 0f), 100, default, 1f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 10f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(230, 200, 60, 0) * 0.5f * alpha, 0f, new Vector2(0.5f), new Vector2(700f, 90f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 240, 180) * 0.7f * alpha, 0f, new Vector2(0.5f), new Vector2(700f, 30f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // AURORADICAL THROW — weak-tracking outbound, then a strong-tracking return once it reaches the arena edge.
    // =====================================================================================================================
    public class AuroradicalBoomerangProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Rogue/AuroradicalStar";

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 260;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.2f;
            Projectile.localAI[0]++;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

            if (Projectile.localAI[1] == 0f)
            {
                // Outbound leg: weak tracking
                if (target.active && Projectile.localAI[0] > 20f)
                {
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity) * Projectile.velocity.Length();
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.01f);
                }
                if (Projectile.localAI[0] >= 70f)
                {
                    Projectile.localAI[1] = 1f;
                    SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.5f }, Projectile.Center);
                }
            }
            else
            {
                // Return leg: strong tracking
                if (target.active)
                {
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 20f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.09f);
                }
            }

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.08f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Color glow = Projectile.localAI[1] >= 1f ? new Color(255, 120, 200) : new Color(230, 200, 60);
            AureusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, glow);
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // ASTRAL SCYTHE — two shadow scythes cross the screen diagonally and collide at the midpoint in a dust shockwave.
    // =====================================================================================================================
    public class AstralScytheProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Enemy/MantisRing";

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.25f;
            Vector2 target = new(Projectile.ai[0], Projectile.ai[1]);
            if ((Projectile.Center - target).LengthSquared() < 900f)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.6f }, Projectile.Center);
                AureusFx.Burst(Projectile.Center, 6f, 20, DustID.PurpleTorch);
                Projectile.Kill();
            }
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.06f, 100, default, 1.1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            AureusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // TITAN ARM — a ground reticle telegraphs, then a giant fist erupts upward through that column.
    // =====================================================================================================================
    public class TitanFistProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/TitanArm";
        private const int TelegraphTime = 40;

        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 140;
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
            if (Projectile.localAI[0] < TelegraphTime)
            {
                Projectile.Center = new Vector2(Projectile.ai[0], Projectile.ai[1] + 60f);
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, new Vector2(0f, -2f), 100, default, 0.9f);
                    d.fadeIn = 1f;
                    d.noGravity = true;
                }
            }
            else
            {
                if (Projectile.localAI[0] == TelegraphTime)
                {
                    SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.8f, Pitch = -0.2f }, Projectile.Center);
                    Player p = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                    if (p.active) p.Calamity().GeneralScreenShakePower = 7f;
                    AureusFx.Burst(Projectile.Center, 6f, 18);
                }
                Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, -22f, 0.3f);
                Projectile.Center = new Vector2(Projectile.ai[0], Projectile.Center.Y);
                Projectile.Center += new Vector2(0f, Projectile.velocity.Y);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = new(tex.Width * 0.5f, tex.Height);
            Vector2 pos = Projectile.Center - Main.screenPosition;
            bool armed = Projectile.localAI[0] >= TelegraphTime;

            if (!armed)
            {
                float t = Projectile.localAI[0] / TelegraphTime;
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Main.spriteBatch.Draw(pixel, pos + new Vector2(0f, 60f), new Rectangle(0, 0, 1, 1), new Color(255, 60, 60) * (0.3f + 0.3f * t), 0f, new Vector2(0.5f), new Vector2(70f, 6f), SpriteEffects.None, 0f);
            }

            AureusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, 0f, origin, 1.1f, new Color(230, 200, 60));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, 0f, origin, 1.1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // STELLAR CANNON — 45-frame bright charge line, then a wide beam that leaves lingering plasma-fire pools.
    // =====================================================================================================================
    public class StellarBeamProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int ChargeTime = 45;
        private const int FireTime = 20;

        public override bool? CanDamage() => Projectile.localAI[0] >= ChargeTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = ChargeTime + FireTime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        private Vector2 Dir => new Vector2(Projectile.ai[0], Projectile.ai[1]).SafeNormalize(Vector2.UnitY);

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == ChargeTime)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.8f }, Projectile.Center);
                Player p = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (p.active) p.Calamity().GeneralScreenShakePower = 8f;
            }
            if (Projectile.localAI[0] >= ChargeTime && Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(3))
            {
                Vector2 pos = Projectile.Center + Dir * Main.rand.NextFloat(60f, 1200f);
                int idx = Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, Vector2.Zero, ModContent.ProjectileType<StellarPlasmaPoolProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / ChargeTime, 0f, 1f);
            Vector2 start = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle src = new(0, 0, 1, 1);
            float rot = Dir.ToRotation();
            bool firing = Projectile.localAI[0] >= ChargeTime;
            float width = firing ? 120f : MathHelper.Lerp(2f, 14f, t);
            Color core = firing ? new Color(255, 240, 200) : Color.Lerp(new Color(230, 200, 60), Color.White, t);
            Main.spriteBatch.Draw(pixel, start, src, core * 0.35f, rot, new Vector2(0f, 0.5f), new Vector2(1400f, width * 2.2f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, start, src, core, rot, new Vector2(0f, 0.5f), new Vector2(1400f, width), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class StellarPlasmaPoolProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 180;

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Main.rand.NextBool(3))
            {
                Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-35f, 35f), 4f);
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, new Vector2(0f, Main.rand.NextFloat(-2f, -0.5f)), 100, default, Main.rand.NextFloat(1f, 1.3f));
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 24f, 0f, 1f) * MathHelper.Clamp((Lifetime - Projectile.timeLeft) / 12f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(230, 200, 60, 0) * 0.5f * alpha, 0f, new Vector2(0.5f), new Vector2(70f, 16f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 240, 180) * 0.6f * alpha, 0f, new Vector2(0.5f), new Vector2(60f, 8f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // STELLAR KNIFE — rises, hovers, then dives along the player's velocity direction at the moment it triggers.
    // =====================================================================================================================
    public class StellarKnifeHoverProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/StellarKnife";

        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] < 50f)
            {
                Projectile.velocity *= 0.92f;
                Projectile.rotation += 0.05f;
            }
            else if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Vector2 dir = target.active && target.velocity.LengthSquared() > 1f ? target.velocity.SafeNormalize(Vector2.UnitY) : Vector2.UnitY;
                Projectile.velocity = dir * 14f;
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.4f }, Projectile.Center);
            }
            else
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.06f, 100, default, 0.85f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            AureusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(230, 200, 60));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // ASTRALACHNEA STAFF — a web anchor that blocks a lane; briefly roots the player on contact, never chases.
    // =====================================================================================================================
    public class AstralWebProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/AstralachneaFang";
        private const int Lifetime = 240;

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.scale = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f) * MathHelper.Clamp((Lifetime - Projectile.timeLeft) / 15f, 0f, 1f) + 0.001f;
            if (Main.rand.NextBool(6))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), DustID.PurpleTorch, Vector2.Zero, 150, default, 0.7f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Webbed, 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(tex, pos, null, new Color(200, 160, 255) * Projectile.scale, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // ABANDONED SLIME — a small core bounces three times, puffing low star-dust with each landing.
    // =====================================================================================================================
    public class AbandonedSlimeCoreProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Summon/AstrageldonSummon";

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 260;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.5f;
            Projectile.rotation += Projectile.velocity.X * 0.02f;

            float floorY = Projectile.ai[1];
            if (floorY > 0f && Projectile.velocity.Y > 0f && Projectile.Center.Y >= floorY)
            {
                Projectile.Center = new Vector2(Projectile.Center.X, floorY);
                Projectile.ai[0]++;
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.35f }, Projectile.Center);
                AureusFx.Burst(Projectile.Center, 3.5f, 10, DustID.GoldFlame);
                if (Projectile.ai[0] >= 3f)
                {
                    Projectile.Kill();
                    return;
                }
                Projectile.velocity.Y = -13f;
            }

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.1f, 100, default, 0.9f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            AureusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(230, 200, 60));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // HIVE POD — thrown pod cracks open mid-flight into a small swarm of weak-tracking star-bees.
    // =====================================================================================================================
    public class HivePodProj : ModProjectile
    {
        public override string Texture => "CalamityMod/NPCs/Astral/Astraglomerate";

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 70;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.15f;
            Projectile.rotation += 0.1f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.1f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.5f }, Projectile.Center);
            AureusFx.Burst(Projectile.Center, 4f, 12, DustID.PurpleTorch);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            for (int i = 0; i < 5; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / 5f + Main.rand.NextFloat(0.3f)).ToRotationVector2() * Main.rand.NextFloat(3f, 5f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<HivelingProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
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

    public class HivelingProj : ModProjectile
    {
        public override string Texture => "CalamityMod/NPCs/Astral/Glomerling";

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active && !target.dead)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 7f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.025f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.08f, 100, default, 0.7f);
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
