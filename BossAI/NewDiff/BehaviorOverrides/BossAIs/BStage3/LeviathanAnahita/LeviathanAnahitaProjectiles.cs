using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.WeaponAttacks;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.LeviathanAnahita
{
    // Shared deep-sea burst, mirroring the Cryogen/Plague/Aureus Fx standard (fadeIn bloom + upward-biased scatter).
    internal static class LeviathanFx
    {
        public static void Burst(Vector2 position, float speed, int count, int dustType = DustID.BlueTorch)
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
    // GREENTIDE — a tide blade telegraphs at a fixed X column, then slams straight down through the whole arena.
    // Variant A (AI picks the column at the player's current X): reads as "keep moving, the cut follows you".
    // Variant B (AI sweeps fixed columns left-to-right): reads as "outrun the traveling wave".
    // =====================================================================================================================
    public class GreentideSlamProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/GreenWater";
        private const int TelegraphTime = 32;

        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 1400;
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
            if (Projectile.localAI[0] == TelegraphTime)
                SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.5f, Pitch = -0.1f }, Projectile.Center);
            if (Projectile.localAI[0] >= TelegraphTime && Main.rand.NextBool(2))
                LeviathanFx.Burst(Projectile.Center + new Vector2(0f, Main.rand.NextFloat(-300f, 300f)), 3f, 3);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f);
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            bool armed = Projectile.localAI[0] >= TelegraphTime;
            float width = armed ? 60f : MathHelper.Lerp(3f, 14f, t);
            Color core = armed ? new Color(160, 255, 210) : Color.Lerp(new Color(60, 200, 140), Color.White, t);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), core * 0.4f, 0f, new Vector2(0.5f), new Vector2(width * 2f, 1400f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), core, 0f, new Vector2(0.5f), new Vector2(width, 1400f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // LEVIATITAN — a slow giant bubble drifts toward the player, then bursts into radial Whitewater needles.
    // =====================================================================================================================
    public class LeviatitanBubbleProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/AquaBlast";
        private const int Lifetime = 150;

        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.04f;
            Projectile.scale = MathHelper.Lerp(0.7f, 1.15f, Projectile.timeLeft / (float)Lifetime * -1f + 1f);
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active && (Projectile.Center - target.Center).Length() < 70f)
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 4);

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch, -Projectile.velocity * 0.06f, 100, default, 1.2f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item54 with { Volume = 0.7f, Pitch = -0.2f }, Projectile.Center);
            Player p = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (p.active) p.Calamity().GeneralScreenShakePower = 6f;
            LeviathanFx.Burst(Projectile.Center, 5f, 20);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            int needleCount = (int)Projectile.ai[0];
            if (needleCount <= 0) needleCount = 8;
            for (int i = 0; i < needleCount; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / needleCount).ToRotationVector2() * 16f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<WhitewaterNeedleProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            LeviathanFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, Projectile.scale, new Color(60, 160, 255));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class WhitewaterNeedleProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
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
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch, -Projectile.velocity * 0.05f, 100, default, 0.8f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(90, 200, 255, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(24f, 8f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(200, 240, 255), rot, new Vector2(0.5f), new Vector2(16f, 4f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // ANAHITA'S ARPEGGIO — crystal notes fire in melodic order along a wave-shaped bolt trajectory.
    // =====================================================================================================================
    public class AnahitaNoteProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/AnahitasArpeggioNote";

        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 220;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] < Projectile.ai[0])
            {
                Projectile.rotation += 0.08f;
                Projectile.scale = MathHelper.Lerp(0.6f, 1f, Projectile.localAI[0] / Math.Max(Projectile.ai[0], 1f));
                return;
            }
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.4f, Pitch = Projectile.ai[1] }, Projectile.Center);
            }
            Projectile.localAI[2]++;
            Vector2 baseDir = new(Projectile.ai[2], Projectile.ai[3]);
            Vector2 perp = new(-baseDir.Y, baseDir.X);
            float wave = MathF.Sin(Projectile.localAI[2] * 0.15f) * 3.4f;
            Projectile.velocity = baseDir * 11f + perp * wave;
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch, -Projectile.velocity * 0.06f, 100, default, 0.8f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            bool armed = Projectile.localAI[1] >= 1f;
            Color tint = armed ? Color.White : Color.Lerp(new Color(150, 220, 255), Color.White, Projectile.localAI[0] / Math.Max(Projectile.ai[0], 1f));
            LeviathanFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, Projectile.scale, new Color(120, 220, 255));
            Main.spriteBatch.Draw(tex, pos, null, tint, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // ATLANTIS — three tridents lock a triangle around the player; the sightlines solidify into light pillars.
    // ai[0]/ai[1]: the OTHER trident's position this beam locks onto (each of the 3 draws one edge of the triangle).
    // =====================================================================================================================
    public class AtlantisPillarProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Magic/Atlantis";
        private const int LockTime = 48;

        public override bool? CanDamage() => Projectile.localAI[0] >= LockTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LockTime + 30;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == LockTime)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.5f }, Projectile.Center);
                Player p = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (p.active) p.Calamity().GeneralScreenShakePower = 6f;
            }
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch, Vector2.Zero, 100, default, 0.9f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / LockTime, 0f, 1f);
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = new Vector2(Projectile.ai[0], Projectile.ai[1]) - Main.screenPosition;
            Vector2 delta = end - start;
            float len = delta.Length();
            float rot = delta.ToRotation();
            bool armed = Projectile.localAI[0] >= LockTime;
            float width = armed ? 80f : MathHelper.Lerp(2f, 10f, t);
            Color core = armed ? new Color(200, 240, 255) : Color.Lerp(new Color(60, 160, 255), Color.White, t);
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, start, new Rectangle(0, 0, 1, 1), core * 0.4f, rot, new Vector2(0f, 0.5f), new Vector2(len, width * 2f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, start, new Rectangle(0, 0, 1, 1), core, rot, new Vector2(0f, 0.5f), new Vector2(len, width), SpriteEffects.None, 0f);

            Texture2D tridentTex = ModContent.Request<Texture2D>(Texture).Value;
            Main.spriteBatch.Draw(tridentTex, start, null, Color.White, rot + MathHelper.PiOver4, new Vector2(0f, tridentTex.Height), 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // GASTRIC BELCHER — a floating stomach arcs acid drops that pool briefly on landing.
    // =====================================================================================================================
    public class GastricAcidDropProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Summon/GastricBelcherBubble";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 160;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.3f;
            Projectile.rotation += 0.12f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Grass, -Projectile.velocity * 0.1f, 150, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
            if (Projectile.ai[0] > 0f && Projectile.velocity.Y > 0f && Projectile.Center.Y >= Projectile.ai[0])
            {
                Projectile.Center = new Vector2(Projectile.Center.X, Projectile.ai[0]);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.4f }, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<GastricAcidPoolProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(140, 220, 90), Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class GastricAcidPoolProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 240;

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.Poisoned, 180);

        public override void AI()
        {
            if (Main.rand.NextBool(3))
            {
                Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-45f, 45f), 4f);
                Dust d = Dust.NewDustPerfect(pos, DustID.Grass, new Vector2(0f, Main.rand.NextFloat(-2f, -0.5f)), 150, default, Main.rand.NextFloat(1f, 1.3f));
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f) * MathHelper.Clamp((Lifetime - Projectile.timeLeft) / 14f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(90, 200, 60, 0) * 0.5f * alpha, 0f, new Vector2(0.5f), new Vector2(100f, 18f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(150, 255, 110) * 0.6f * alpha, 0f, new Vector2(0.5f), new Vector2(88f, 8f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // LEVIATHAN TEETH — teeth fan out, arc past the player, then curve back like a boomerang.
    // =====================================================================================================================
    public class LeviathanToothProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Rogue/LeviathanTooth";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 160;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            float curveDir = Projectile.ai[0] >= 0f ? 1f : -1f;
            if (Projectile.localAI[0] >= 45f)
                Projectile.velocity = Projectile.velocity.RotatedBy(curveDir * 0.045f);
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch, -Projectile.velocity * 0.06f, 100, default, 0.85f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            LeviathanFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(60, 160, 255));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // WHITEWATER — the 40% HP transition: two water walls converge from the screen edges leaving a moving gap.
    // ai[0]: side (-1 left, +1 right). ai[1]: arena center X (walls converge toward this X).
    // =====================================================================================================================
    public class WhitewaterWallProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 90;
        private const float GapHeight = 200f;

        public override void SetDefaults()
        {
            Projectile.width = 4000;
            Projectile.height = 2000;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        private float GapCenterY => Projectile.Center.Y + MathF.Sin(Projectile.localAI[0] * 0.05f) * 260f;

        public override bool? CanDamage() => null;

        public override void AI()
        {
            Projectile.localAI[0]++;
        }

        public override void CutTiles() { }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float gapCenter = GapCenterY;
            bool inGap = targetHitbox.Center.Y > gapCenter - GapHeight / 2f && targetHitbox.Center.Y < gapCenter + GapHeight / 2f;
            return !inGap;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / Lifetime, 0f, 1f);
            float side = Projectile.ai[0];
            float centerX = Projectile.ai[1];
            float wallX = MathHelper.Lerp(centerX + side * 1300f, centerX + side * 260f, t);
            float gapCenter = GapCenterY;
            Vector2 topPos = new Vector2(wallX, gapCenter - GapHeight / 2f - 500f) - Main.screenPosition;
            Vector2 botPos = new Vector2(wallX, gapCenter + GapHeight / 2f + 500f) - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, topPos, new Rectangle(0, 0, 1, 1), new Color(140, 220, 255) * 0.85f, 0f, new Vector2(0.5f, 1f), new Vector2(700f, 1000f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, botPos, new Rectangle(0, 0, 1, 1), new Color(140, 220, 255) * 0.85f, 0f, new Vector2(0.5f, 0f), new Vector2(700f, 1000f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // DOLPHIN JUMP — Leviathan's slam creates two towering tsunami waves sweeping outward at sea level.
    // =====================================================================================================================
    public class TsunamiWaveProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 500;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 100;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Main.rand.NextBool(2))
            {
                Vector2 pos = Projectile.Center + new Vector2(Projectile.ai[0] * 20f, Main.rand.NextFloat(-240f, 240f));
                Dust d = Dust.NewDustPerfect(pos, DustID.BlueTorch, new Vector2(Projectile.ai[0] * -3f, 0f), 100, default, 1.3f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 16f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(90, 200, 255, 0) * 0.5f * alpha, 0f, new Vector2(0.5f), new Vector2(80f, 500f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(210, 240, 255) * 0.75f * alpha, 0f, new Vector2(0.5f), new Vector2(40f, 500f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // ATLANTIS · RING NET — six tridents rotate slowly around Anahita, each firing a spinning aurora beam outward.
    // =====================================================================================================================
    public class AtlantisRingBeamProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 260;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            NPC anchor = Main.npc[(int)Projectile.ai[0]];
            if (!anchor.active) { Projectile.Kill(); return; }
            float angle = Projectile.ai[1] + Projectile.localAI[0] * 0.025f;
            Projectile.localAI[0]++;
            Projectile.Center = anchor.Center;
            Projectile.rotation = angle;

            if (Main.rand.NextBool(3))
            {
                Vector2 tip = Projectile.Center + angle.ToRotationVector2() * 900f * Main.rand.NextFloat();
                Dust d = Dust.NewDustPerfect(tip, DustID.BlueTorch, Vector2.Zero, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 tip = Projectile.Center + Projectile.rotation.ToRotationVector2() * 1200f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, tip, 20f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(140, 220, 255, 0) * 0.45f, Projectile.rotation, new Vector2(0f, 0.5f), new Vector2(1200f, 26f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 245, 255), Projectile.rotation, new Vector2(0f, 0.5f), new Vector2(1200f, 8f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
