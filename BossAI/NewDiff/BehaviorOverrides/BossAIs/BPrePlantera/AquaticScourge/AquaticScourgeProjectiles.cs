using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage2.AquaticScourge
{
    internal static class ScourgeFx
    {
        public static void Burst(Vector2 position, float speed, int count, int dustType = DustID.Water)
        {
            for (int i = 0; i < count; i++)
            {
                Dust d = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(30f, 30f), dustType);
                d.velocity = Main.rand.NextVector2Circular(speed, speed) - Vector2.UnitY * 1.2f;
                d.scale = Main.rand.NextFloat(1.1f, 1.5f);
                d.fadeIn = 1.3f;
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

    // SUBMARINE SHOCKER — homing electro-torpedo, arcs of current trailing behind, dives on expiry.
    public class ShockAnchorProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 24;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 150;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] < 45f)
            {
                int pIdx = (int)Projectile.ai[0];
                if (pIdx >= 0 && pIdx < Main.maxPlayers && Main.player[pIdx].active)
                {
                    Vector2 dir = (Main.player[pIdx].Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY));
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * Projectile.velocity.Length(), 0.06f);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * 0.1f, 60, new Color(120, 220, 255), 1.1f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.7f }, Projectile.Center);
            ScourgeFx.Burst(Projectile.Center, 5f, 12, DustID.Electric);
        }
    }

    // BARINAUTICAL — harpoon-blade flies out, reverses, tears back through on the return.
    public class HarpoonBoomerangProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 140;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.3f;
            if (Projectile.localAI[0] == 50f)
            {
                int ownerWhoAmI = (int)Projectile.ai[2];
                if (ownerWhoAmI >= 0 && ownerWhoAmI < Main.maxNPCs && Main.npc[ownerWhoAmI].active)
                    Projectile.velocity = (Main.npc[ownerWhoAmI].Center - Projectile.Center).SafeNormalize(-Projectile.velocity) * Projectile.velocity.Length() * 1.15f;
            }
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Water, Vector2.Zero, 80, default, 1.2f);
                d.noGravity = true;
            }
        }
    }

    // DOWNPOUR — a raincloud hovers and periodically drops water torrents straight down.
    public class RainCloudProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool? CanDamage() => false;

        public override void SetDefaults()
        {
            Projectile.width = 80; Projectile.height = 40;
            Projectile.hostile = false; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 200;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] % 22f == 0f && Projectile.localAI[0] < 180f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int idx = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(0f, 9f), ModContent.ProjectileType<WaterBoltDropProj>(), Projectile.damage, 0f, Main.myPlayer);
                if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].timeLeft = 90;
            }
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(50f, 20f), DustID.Water, new Vector2(0f, 1f), 100, default, 1.4f);
                d.noGravity = true;
            }
        }
    }

    public class WaterBoltDropProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 16; Projectile.height = 16;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 90;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.15f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Water, Vector2.Zero, 60, default, 1.1f);
            d.noGravity = true;
        }
    }

    // DEEPSEA STAFF — anglerfish swims in, then lunges with a burst of speed.
    public class AnglerfishProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 26;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 220;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            int pIdx = (int)Projectile.ai[0];
            if (pIdx >= 0 && pIdx < Main.maxPlayers && Main.player[pIdx].active)
            {
                Player target = Main.player[pIdx];
                if (Projectile.localAI[0] < 130f)
                {
                    Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 7f, 0.05f);
                }
                else if (Projectile.localAI[1] == 0f)
                {
                    Projectile.localAI[1] = 1f;
                    Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 15f;
                    SoundEngine.PlaySound(SoundID.NPCDeath14 with { Volume = 0.5f, Pitch = 0.6f }, Projectile.Center);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity, DustID.Water, Vector2.Zero, 100, new Color(100, 200, 180), 1f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft) => ScourgeFx.Burst(Projectile.Center, 4f, 8);
    }

    // Shared extend -> hold -> retract tendril, used by both ScourgeoftheSeas (barbed whip) and
    // SulphurousGrabber (clamping claw). ai[0]/ai[1] = fixed world direction, ai[2] = owner NPC whoAmI.
    public class BarbedTendrilProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int ExtendTime = 20;
        private const int HoldTime = 14;
        private const int RetractTime = 26;
        public float MaxLength => Projectile.ai[3] > 0f ? Projectile.ai[3] : 420f;

        public override bool? CanDamage() => Projectile.localAI[0] > 2f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 20; Projectile.height = 20;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = ExtendTime + HoldTime + RetractTime + 5;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        private Vector2 Dir => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        private Vector2 Origin
        {
            get
            {
                int owner = (int)Projectile.ai[2];
                if (owner >= 0 && owner < Main.maxNPCs && Main.npc[owner].active)
                    return Main.npc[owner].Center;
                return Projectile.Center;
            }
        }

        public float CurrentLength
        {
            get
            {
                float t = Projectile.localAI[0];
                if (t <= ExtendTime)
                    return MathHelper.Lerp(0f, MaxLength, 1f - MathF.Pow(1f - t / ExtendTime, 2f));
                if (t <= ExtendTime + HoldTime)
                    return MaxLength;
                float p = (t - ExtendTime - HoldTime) / RetractTime;
                return MathHelper.Lerp(MaxLength, 0f, p);
            }
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.Center = Origin + Dir * CurrentLength;
            Projectile.width = Projectile.height = 24;
            if (Projectile.localAI[0] == 1f)
                SoundEngine.PlaySound(SoundID.Item94 with { Pitch = -0.3f }, Origin);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Origin + Dir * CurrentLength * Main.rand.NextFloat(), DustID.ToxicBubble, Vector2.Zero, 60, default, 1.2f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 start = Origin - Main.screenPosition;
            Vector2 end = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float len = (end - start).Length();
            if (len < 2f) return false;
            Main.spriteBatch.Draw(pixel, start, new Rectangle(0, 0, 1, 1), new Color(120, 200, 90, 0) * 0.5f, Dir.ToRotation(), new Vector2(0f, 0.5f), new Vector2(len, 20f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, start, new Rectangle(0, 0, 1, 1), new Color(180, 255, 140), Dir.ToRotation(), new Vector2(0f, 0.5f), new Vector2(len, 8f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // FLAK TOXICANNON — arcing shell bursts into a shrapnel ring.
    public class FlakShellProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 22; Projectile.height = 22;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 70;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.25f;
            Projectile.rotation += 0.2f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.ToxicBubble, Vector2.Zero, 60, default, 1.1f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62, Projectile.Center);
            ScourgeFx.Burst(Projectile.Center, 6f, 14, DustID.ToxicBubble);
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 6f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<ToxicShardProj>(), Projectile.damage, 0f, Main.myPlayer);
            }
        }
    }

    public class ToxicShardProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 14; Projectile.height = 14;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 50;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.98f;
            Projectile.rotation += 0.3f;
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.ToxicBubble, Vector2.Zero, 60, default, 0.9f);
            d.noGravity = true;
        }
    }

    // SLITHERING EELS — homing bolts that weave side to side.
    public class EelBoltProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 18; Projectile.height = 18;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 160;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            int pIdx = (int)Projectile.ai[0];
            if (pIdx >= 0 && pIdx < Main.maxPlayers && Main.player[pIdx].active && Projectile.localAI[0] > 15f)
            {
                Vector2 dir = (Main.player[pIdx].Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 9f, 0.045f);
            }
            Vector2 wiggle = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * MathF.Sin(Projectile.localAI[0] * 0.25f) * 1.5f;
            Projectile.velocity += wiggle * 0.05f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.ToxicBubble, Vector2.Zero, 60, new Color(150, 255, 120), 1f);
            d.noGravity = true;
        }
    }

    // CAUSTIC CROAKER — a temporary hopping sentry that spits acid globs.
    public class CroakerSentryProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool? CanDamage() => false;
        public override void SetDefaults()
        {
            Projectile.width = 34; Projectile.height = 30;
            Projectile.hostile = false; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 300;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity.Y += 0.2f;
            if (Projectile.velocity.Y > 0f && Projectile.localAI[1] == 0f)
            {
                int pIdx = (int)Projectile.ai[0];
                if (Projectile.localAI[0] % 45f == 0f && pIdx >= 0 && pIdx < Main.maxPlayers && Main.player[pIdx].active && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 dir = (Main.player[pIdx].Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                    Projectile.velocity = new Vector2(dir.X * 6f, -6f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, (Main.player[pIdx].Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 7f, ModContent.ProjectileType<AcidGlobProj>(), Projectile.damage, 0f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.4f }, Projectile.Center);
                }
            }
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.ToxicBubble, Vector2.Zero, 80, default, 1.1f);
            d.noGravity = true;
        }
    }

    public class AcidGlobProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 16; Projectile.height = 16;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 100;
            Projectile.tileCollide = true; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.2f;
            Projectile.rotation += 0.25f;
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.ToxicBubble, Vector2.Zero, 60, default, 1f);
            d.noGravity = true;
        }

        public override void OnKill(int timeLeft) => ScourgeFx.Burst(Projectile.Center, 3f, 6, DustID.ToxicBubble);
    }

    // SKYFIN BOMBERS — fly a line over the target, dropping small toxic bomblets.
    public class BomberFishProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool? CanDamage() => false;
        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 20;
            Projectile.hostile = false; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 120;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.localAI[0] % 12f == 0f && Projectile.localAI[0] < 100f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(0f, 4f), ModContent.ProjectileType<ToxicBombProj>(), Projectile.damage, 0f, Main.myPlayer, 220f, 60f);
            }
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.ToxicBubble, -Projectile.velocity * 0.1f, 60, default, 1.1f);
            d.noGravity = true;
        }
    }

    // Falling bomb -> lingering toxic gas cloud. ai[0] = cloud radius, ai[1] = cloud duration.
    public class ToxicBombProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 14; Projectile.height = 14;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 70;
            Projectile.tileCollide = true; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.25f;
            Projectile.rotation += 0.3f;
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.ToxicBubble, Vector2.Zero, 60, default, 1f);
            d.noGravity = true;
        }

        public override void OnKill(int timeLeft)
        {
            ScourgeFx.Burst(Projectile.Center, 4f, 10, DustID.ToxicBubble);
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float radius = Projectile.ai[0] > 0f ? Projectile.ai[0] : 90f;
            float duration = Projectile.ai[1] > 0f ? Projectile.ai[1] : 60f;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ToxicCloudHazardProj>(), Projectile.damage, 0f, Main.myPlayer, radius, duration);
        }
    }

    // SPENT FUEL CONTAINER — a heavy barrel lobs in, then bursts into a larger, longer-lived gas cloud.
    public class FuelBarrelProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 26;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 110;
            Projectile.tileCollide = true; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.3f;
            Projectile.rotation += 0.35f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.ToxicBubble, Vector2.Zero, 60, default, 1.3f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.3f }, Projectile.Center);
            ScourgeFx.Burst(Projectile.Center, 6f, 18, DustID.ToxicBubble);
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ToxicCloudHazardProj>(), Projectile.damage, 0f, Main.myPlayer, 160f, 150f);
        }
    }

    // Lingering hazard patch — ai[0] = radius, ai[1] = duration.
    public class ToxicCloudHazardProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private int hurtCooldown = 0;
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 200;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = (int)(Projectile.ai[1] > 0f ? Projectile.ai[1] : 100f);
            }
            float radius = Projectile.ai[0] > 0f ? Projectile.ai[0] : 90f;
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(radius, radius), DustID.ToxicBubble, Vector2.Zero, 100, default, 1.2f);
                d.noGravity = true;
            }
            if (hurtCooldown > 0) hurtCooldown--;
        }

        public override bool CanHitPlayer(Player target)
        {
            float radius = Projectile.ai[0] > 0f ? Projectile.ai[0] : 90f;
            if (hurtCooldown <= 0 && Vector2.Distance(target.Center, Projectile.Center) <= radius)
            {
                hurtCooldown = 30;
                return true;
            }
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = Projectile.ai[0] > 0f ? Projectile.ai[0] : 90f;
            return Vector2.Distance(targetHitbox.Center.ToVector2(), Projectile.Center) <= radius;
        }
    }

    // SULPHUR TIDE WALL — a diagonal current sweeps across the arena with two navigable gaps.
    // ai[0] = orientation sign (+1 = "\" normal, -1 = "/" normal), ai[1] used as an internal sweep clock.
    public class SulphurTideWallProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 50;
        private const int ActiveTime = 200;
        private const float SweepRange = 1000f;
        private const float GapWidth = 100f;
        private int hurtCooldown = 0;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = TelegraphTime + ActiveTime + 5;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        private Vector2 Normal => Projectile.ai[0] > 0f ? new Vector2(1f, 1f).SafeNormalize(Vector2.UnitX) : new Vector2(1f, -1f).SafeNormalize(Vector2.UnitX);
        private Vector2 Tangent => new Vector2(-Normal.Y, Normal.X);

        public override void AI()
        {
            Projectile.ai[1]++;
            if (hurtCooldown > 0) hurtCooldown--;
            if (Main.rand.NextBool(TelegraphActive ? 2 : 6))
            {
                Vector2 dust = Projectile.Center + Tangent * Main.rand.NextFloat(-SweepRange, SweepRange);
                Dust d = Dust.NewDustPerfect(dust, DustID.ToxicBubble, Vector2.Zero, 80, default, TelegraphActive ? 1.4f : 0.8f);
                d.noGravity = true;
            }
        }

        private bool TelegraphActive => Projectile.ai[1] <= TelegraphTime;

        private float SweepOffset
        {
            get
            {
                float p = MathHelper.Clamp((Projectile.ai[1] - TelegraphTime) / ActiveTime, 0f, 1f);
                return MathHelper.Lerp(-SweepRange, SweepRange, p);
            }
        }

        private float GapCenter1 => MathF.Sin(Main.GameUpdateCount * 0.011f) * (SweepRange * 0.65f);
        private float GapCenter2 => -GapCenter1;

        public override bool? CanDamage() => TelegraphActive ? false : (bool?)null;

        public override bool CanHitPlayer(Player target)
        {
            if (TelegraphActive || hurtCooldown > 0) return false;
            Vector2 linePoint = Projectile.Center + Normal * SweepOffset;
            float distToLine = Math.Abs(Vector2.Dot(target.Center - linePoint, Normal));
            if (distToLine > 55f) return false;
            float along = Vector2.Dot(target.Center - Projectile.Center, Tangent);
            if (Math.Abs(along - GapCenter1) < GapWidth) return false;
            if (Math.Abs(along - GapCenter2) < GapWidth) return false;
            hurtCooldown = 30;
            return true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => false;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.wingTime = 0f;
            target.rocketDelay2 = 20;
            if (ModContent.TryFind("CalamityMod", "SulphuricPoisoning", out ModBuff b))
                target.AddBuff(b.Type, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 center = TelegraphActive ? Projectile.Center : Projectile.Center + Normal * SweepOffset;
            Vector2 screenCenter = center - Main.screenPosition;
            float alpha = TelegraphActive ? MathHelper.Clamp(Projectile.ai[1] / TelegraphTime, 0f, 1f) * 0.5f : 0.85f;
            Color color = TelegraphActive ? new Color(255, 220, 120, 0) * alpha : new Color(140, 255, 110, 0) * alpha;

            float gap1 = GapCenter1, gap2 = GapCenter2;
            float halfLen = SweepRange;
            DrawSegment(pixel, screenCenter, -halfLen, gap1 - GapWidth, color);
            DrawSegment(pixel, screenCenter, gap1 + GapWidth, gap2 - GapWidth, color);
            DrawSegment(pixel, screenCenter, gap2 + GapWidth, halfLen, color);
            return false;
        }

        private void DrawSegment(Texture2D pixel, Vector2 screenCenter, float from, float to, Color color)
        {
            if (to <= from) return;
            Vector2 start = screenCenter + Tangent * from;
            float len = to - from;
            Main.spriteBatch.Draw(pixel, start, new Rectangle(0, 0, 1, 1), color, Tangent.ToRotation(), new Vector2(0f, 0.5f), new Vector2(len, 90f), SpriteEffects.None, 0f);
        }
    }
}
