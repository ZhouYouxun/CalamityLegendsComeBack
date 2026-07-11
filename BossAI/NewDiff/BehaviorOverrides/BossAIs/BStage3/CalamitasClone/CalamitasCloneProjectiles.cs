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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.CalamitasClone
{
    // Shared brimstone-red burst, matching Cryogen's EmitFrostBurst / Plaguebringer's PlagueFx standard.
    internal static class BrimstoneFx
    {
        public static void Burst(Vector2 position, float speed, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Dust d = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(40f, 40f), DustID.Torch);
                d.velocity = Main.rand.NextVector2Circular(speed, speed) - Vector2.UnitY * 1.2f;
                d.scale = Main.rand.NextFloat(1.2f, 1.7f);
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
    // OBLIVION — thrown yoyo that stalls beside the player, then sweeps a full 360-degree orbit around them.
    // The swept arc leaves a ring of brimstone flame that erupts upward 1.5s later.
    // =====================================================================================================================
    public class OblivionYoyoProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/Yoyos/OblivionYoyo";
        private const int StallTime = 26;
        private const int SweepTime = 70;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = StallTime + SweepTime + 10;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.14f;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (!target.active)
                return;

            if (Projectile.localAI[0] < StallTime)
            {
                Projectile.velocity *= 0.85f;
                if (Projectile.ai[0] == 0f)
                {
                    Projectile.ai[0] = 1f;
                    Projectile.ai[1] = target.Center.X;
                    Projectile.ai[2] = target.Center.Y;
                }
            }
            else
            {
                // Sweep a full circle centered on the player's position AT STALL END (fixed pivot, not chased)
                Vector2 pivot = new Vector2(Projectile.ai[1], Projectile.ai[2]);
                float sweepT = (Projectile.localAI[0] - StallTime) / (float)SweepTime;
                float radius = Vector2.Distance(Projectile.Center, pivot);
                if (radius < 10f)
                    radius = 210f;
                float angle = (Projectile.Center - pivot).ToRotation() + 0.11f;
                Vector2 desired = pivot + angle.ToRotationVector2() * radius;
                Projectile.velocity = (desired - Projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.localAI[0] % 6 == 0)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<OblivionFireRingProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
                }
            }

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.08f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            BrimstoneFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(220, 60, 60));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // A single marker left along the yoyo's swept arc; telegraphs for 1.5s (90 frames) before erupting.
    public class OblivionFireRingProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 90;

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 60;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TelegraphTime + 30;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime && Projectile.localAI[0] < TelegraphTime + 20 ? null : (bool?)false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Main.rand.NextFloat() < Projectile.localAI[0] / (float)TelegraphTime * 0.5f)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(10f, 30f)), DustID.Torch, new Vector2(0f, -1f), 100, default, 0.9f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
            if (Projectile.localAI[0] == TelegraphTime)
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.35f, Pitch = 0.2f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float p = MathHelper.Clamp(Projectile.localAI[0] / (float)TelegraphTime, 0f, 1f);
            bool erupting = Projectile.localAI[0] >= TelegraphTime;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float height = erupting ? 90f : MathHelper.Lerp(10f, 60f, p);
            Color col = erupting ? new Color(255, 140, 60) : Color.Lerp(new Color(220, 60, 60), Color.Orange, p);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), col * 0.55f, MathHelper.PiOver2, new Vector2(0.5f), new Vector2(height, 16f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), Color.White * 0.5f, MathHelper.PiOver2, new Vector2(0.5f), new Vector2(height * 0.8f, 6f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // ANIMOSITY — laser-locked sniper shot; if it hits the arena wall instead of the player, it bursts into an acid fog.
    // =====================================================================================================================
    public class AnimosityBulletProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/AnimosityBullet";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
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
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.05f, 100, default, 1f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Only bursts into fog if it missed the player and simply expired/hit the boundary reflector
            if (timeLeft > 4)
                return;
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.5f }, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dir, ModContent.ProjectileType<AcidFogWallProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 60, 60, 0) * 0.5f, rot, new Vector2(0.5f), new Vector2(50f, 8f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), Color.White, rot, new Vector2(0.5f), new Vector2(24f, 4f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class AcidFogWallProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 180; // 3 seconds

        public override void SetDefaults()
        {
            Projectile.width = 400;
            Projectile.height = 400;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => Projectile.localAI[0] >= 15f ? null : (bool?)false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Main.rand.NextBool(2))
            {
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(180f, 180f);
                Dust d = Dust.NewDustPerfect(pos, DustID.Venom, Vector2.Zero, 100, default, Main.rand.NextFloat(1f, 1.6f));
                d.fadeIn = 1.4f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f) * MathHelper.Clamp(Projectile.localAI[0] / 15f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(90, 200, 60, 0) * 0.35f * alpha, 0f, new Vector2(0.5f), new Vector2(400f, 400f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // LASHES OF CHAOS — brimstone hellfireball that, after its flight, blooms into a gravity vortex.
    // =====================================================================================================================
    public class BrimstoneHellfireballProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Boss/BrimstoneHellfireball";
        private const int FlightTime = 45;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = FlightTime + 5;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.1f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.1f, 100, default, 1.2f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = -0.1f }, Projectile.Center);
            BrimstoneFx.Burst(Projectile.Center, 4f, 16);
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<HellfireVortexProj>(), Projectile.damage, 0f, Main.myPlayer);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            BrimstoneFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(220, 40, 40));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class HellfireVortexProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 150; // 2.5 seconds

        public override void SetDefaults()
        {
            Projectile.width = 110;
            Projectile.height = 110;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.06f;
            Projectile.localAI[0]++;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active && !target.dead && Vector2.Distance(target.Center, Projectile.Center) < 260f)
            {
                Vector2 pull = (Projectile.Center - target.Center).SafeNormalize(Vector2.Zero) * 0.45f;
                target.velocity += pull;
            }

            if (Main.rand.NextBool(2))
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + ang.ToRotationVector2() * Main.rand.NextFloat(50f);
                Dust d = Dust.NewDustPerfect(pos, DustID.Torch, (Projectile.Center - pos) * 0.05f, 100, default, 1.1f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 24f, 0f, 1f);
            float pulse = 0.85f + 0.15f * MathF.Sin(Projectile.localAI[0] * 0.2f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 40, 40, 0) * 0.35f * alpha * pulse, Projectile.rotation, new Vector2(0.5f), new Vector2(110f, 110f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 140, 60) * 0.5f * alpha, -Projectile.rotation, new Vector2(0.5f), new Vector2(60f, 60f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // BRIMSTONE LASER — amplified shot from a Calamitamini eye, fired in a 3-way fan (per design doc's shield amplification).
    // =====================================================================================================================
    public class MiniAmplifiedLaserProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.velocity.Length() < 20f)
                Projectile.velocity *= 1.025f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.06f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 60, 60, 0) * 0.45f, rot, new Vector2(0.5f), new Vector2(40f, 9f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), Color.White, rot, new Vector2(0.5f), new Vector2(20f, 4f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // CRUSHSAW CRASHER — sawblade that flies to the arena wall then rolls flush along the inside edge for 1.5 laps.
    // =====================================================================================================================
    public class CrushaxProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Rogue/Crushax";

        // Only the arena center (X, Y) is passed via ai0/ai1 — NewProjectile's Vector2-position overload tops
        // out at 3 extra floats (ai0-ai2) before overload resolution starts matching the float-X/Y variant
        // instead and produces confusing type-mismatch errors. Arena size is hardcoded since Crushsaw Crasher
        // only ever fires during Phase 3 (the 900px box); hug direction is fixed clockwise for simplicity.
        private const float ArenaSize = 900f;

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 260;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        // ai[0..1]: arena center. localAI[0]: perimeter distance traveled while hugging; localAI[1]: 0=flight, 1=hugging
        public override void AI()
        {
            Projectile.rotation += 0.28f;
            Vector2 center = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            float half = ArenaSize * 0.5f;
            float perimeter = half * 8f;

            if (Projectile.localAI[1] == 0f)
            {
                bool nearWall = Math.Abs(Projectile.Center.X - center.X) >= half - 20f || Math.Abs(Projectile.Center.Y - center.Y) >= half - 20f;
                if (nearWall)
                {
                    Projectile.localAI[1] = 1f;
                    // Seed the hug start from the ACTUAL impact position (clamped onto the square), so the
                    // switch from flight to hugging never visually snaps to a corner.
                    Vector2 clamped = new Vector2(
                        MathHelper.Clamp(Projectile.Center.X, center.X - half, center.X + half),
                        MathHelper.Clamp(Projectile.Center.Y, center.Y - half, center.Y + half));
                    Projectile.localAI[0] = PerimeterFractionOf(center, half, clamped) * perimeter;
                    SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, Pitch = -0.2f }, Projectile.Center);
                }
            }
            else
            {
                Projectile.localAI[0] += 14f;
                float laps = Projectile.localAI[0] / perimeter;
                if (laps >= 1.5f)
                {
                    Projectile.Kill();
                    return;
                }
                float frac = (Projectile.localAI[0] / perimeter) % 1f;
                Projectile.Center = SquarePerimeterPoint(center, half, frac);
            }

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Vector2.Zero, 100, default, 1.1f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        // Inverse of SquarePerimeterPoint: given a point already ON the square's edge, returns its 0-1 fraction.
        private static float PerimeterFractionOf(Vector2 center, float half, Vector2 point)
        {
            Vector2 rel = point - center;
            // Determine which edge: top(0), right(1), bottom(2), left(3), matching SquarePerimeterPoint's winding
            if (Math.Abs(rel.Y + half) < 1f) return (rel.X + half) / (2f * half) * 0.25f;
            if (Math.Abs(rel.X - half) < 1f) return 0.25f + (rel.Y + half) / (2f * half) * 0.25f;
            if (Math.Abs(rel.Y - half) < 1f) return 0.5f + (half - rel.X) / (2f * half) * 0.25f;
            return 0.75f + (half - rel.Y) / (2f * half) * 0.25f;
        }

        private static Vector2 SquarePerimeterPoint(Vector2 center, float half, float t)
        {
            float side = t * 4f;
            int edge = (int)side;
            float edgeT = side - edge;
            Vector2 tl = center + new Vector2(-half, -half);
            Vector2 tr = center + new Vector2(half, -half);
            Vector2 br = center + new Vector2(half, half);
            Vector2 bl = center + new Vector2(-half, half);
            return edge switch
            {
                0 => Vector2.Lerp(tl, tr, edgeT),
                1 => Vector2.Lerp(tr, br, edgeT),
                2 => Vector2.Lerp(br, bl, edgeT),
                _ => Vector2.Lerp(bl, tl, edgeT),
            };
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            BrimstoneFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1.1f, new Color(220, 60, 60));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1.1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // HAVOC'S BREATH — sweeping flame puff; on death near the arena wall, ignites that stretch of boundary.
    // =====================================================================================================================
    public class BrimstoneFireFriendlyProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/FireProj";

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 70;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.97f;
            Projectile.rotation += 0.08f;
            Projectile.alpha = (int)MathHelper.Lerp(0f, 200f, (70f - Projectile.timeLeft) / 70f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.05f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            float op = (255 - Projectile.alpha) / 255f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(255, 140, 60) * op, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // DESPERATION OVERLOAD — slow-rotating cross laser (4-way), and falling star bursts.
    // =====================================================================================================================
    public class RotatingBrimstoneLaserProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public const float SpinSpeed = 0.012f;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 1400;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3600;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => Projectile.localAI[0] >= 20f ? null : (bool?)false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += SpinSpeed;
            if (Main.rand.NextBool(3))
            {
                Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(-680f, 680f);
                Dust d = Dust.NewDustPerfect(pos, DustID.Torch, Vector2.Zero, 100, default, 0.9f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.localAI[0] < 20f)
                return false;
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 start = Projectile.Center - dir * 700f;
            Vector2 end = Projectile.Center + dir * 700f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 20f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            bool active = Projectile.localAI[0] >= 20f;
            float opacity = active ? 1f : MathHelper.Clamp(Projectile.localAI[0] / 20f, 0f, 1f);
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 start = Projectile.Center - dir * 700f;
            Vector2 end = Projectile.Center + dir * 700f;
            IUMWWeaponBossVisuals.DrawLine(Main.spriteBatch, start, end, new Color(220, 40, 40, 0) * (active ? 0.7f : 0.4f) * opacity, active ? 16f : 4f);
            IUMWWeaponBossVisuals.DrawLine(Main.spriteBatch, start, end, Color.White * (active ? 0.5f : 0.3f) * opacity, active ? 5f : 2f);
            return false;
        }
    }

    public class HellfireStarExplosionProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 40;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TelegraphTime + 20;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity.Y += 0.3f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.1f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
            if (Projectile.localAI[0] >= TelegraphTime)
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.6f }, Projectile.Center);
            BrimstoneFx.Burst(Projectile.Center, 5f, 20);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 6f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<MiniAmplifiedLaserProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float p = Projectile.localAI[0] / (float)TelegraphTime;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 140, 60, 0) * 0.5f, Projectile.rotation, new Vector2(0.5f), new Vector2(14f + p * 6f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), Color.White * (0.5f + p * 0.5f), Projectile.rotation, new Vector2(0.5f), new Vector2(6f + p * 4f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
