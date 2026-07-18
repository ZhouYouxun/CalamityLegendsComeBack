using System;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage2.AquaticScourge;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.OldDuke
{
    // Several weapons (SlitheringEels, SkyfinBombers, SpentFuelContainer, SulphurousGrabber) are shared
    // with Aquatic Scourge per the design docs, so their projectiles/held-weapons are reused directly from
    // CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage2.AquaticScourge rather than duplicated here.

    // MUTATED TRUFFLE — a minion burrows in, surfaces, then charges the player.
    public class BurrowerMinionProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool? CanDamage() => Projectile.localAI[0] >= 60f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 220;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            int pIdx = (int)Projectile.ai[0];

            if (Projectile.localAI[0] < 60f)
            {
                Projectile.velocity *= 0.9f;
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Grass, Vector2.Zero, 100, default, 1.3f);
                    d.noGravity = true;
                }
                return;
            }

            if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                SoundEngine.PlaySound(SoundID.NPCDeath14 with { Pitch = -0.3f }, Projectile.Center);
                ScourgeFx.Burst(Projectile.Center, 5f, 10, DustID.ToxicBubble);
            }

            if (pIdx >= 0 && pIdx < Main.maxPlayers && Main.player[pIdx].active)
            {
                Vector2 dir = (Main.player[pIdx].Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 10f, 0.05f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
    }

    // CADAVEROUS CARRION — circles overhead, then dive-bombs the player.
    public class CarrionDiveProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 28; Projectile.height = 28;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 220;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            int pIdx = (int)Projectile.ai[0];
            if (pIdx < 0 || pIdx >= Main.maxPlayers || !Main.player[pIdx].active) return;
            Player target = Main.player[pIdx];

            if (Projectile.localAI[0] < 80f)
            {
                float angle = Projectile.ai[1] + Projectile.localAI[0] * 0.05f;
                Vector2 orbit = target.Center + new Vector2(0f, -260f) + angle.ToRotationVector2() * 200f;
                Projectile.velocity = (orbit - Projectile.Center) * 0.1f;
            }
            else if (Projectile.localAI[1] == 0f)
            {
                // ai[1] doubles as the "dived" flag once the orbit phase ends (angle no longer read after this point)
                Projectile.localAI[1] = 1f;
                Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 16f;
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.6f }, Projectile.Center);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Bone, Vector2.Zero, 100, default, 1f);
                d.noGravity = true;
            }
        }
    }

    // TOXICANT TWISTER — a slow homing vortex disc that drags the player toward it.
    public class TwisterDiskProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 60; Projectile.height = 60;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 260;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.3f;
            int pIdx = (int)Projectile.ai[0];
            if (pIdx >= 0 && pIdx < Main.maxPlayers && Main.player[pIdx].active)
            {
                Player target = Main.player[pIdx];
                Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 5f, 0.02f);

                float dist = Vector2.Distance(target.Center, Projectile.Center);
                if (dist < 260f)
                    target.velocity += (Projectile.Center - target.Center).SafeNormalize(Vector2.Zero) * 0.15f;
            }
            if (Main.rand.NextBool(2))
            {
                Vector2 dustPos = Projectile.Center + Main.rand.NextVector2CircularEdge(30f, 30f).RotatedBy(Projectile.rotation);
                Dust d = Dust.NewDustPerfect(dustPos, DustID.ToxicBubble, Vector2.Zero, 100, default, 1.2f);
                d.noGravity = true;
            }
        }
    }

    // SULPHURIC ACID CANNON — a slow heavy orb that bursts into a lingering acid cloud.
    public class AcidOrbProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 140;
            Projectile.tileCollide = true; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.12f;
            Projectile.rotation += 0.15f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.ToxicBubble, Vector2.Zero, 100, default, 1.4f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.2f }, Projectile.Center);
            ScourgeFx.Burst(Projectile.Center, 5f, 14, DustID.ToxicBubble);
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ToxicCloudHazardProj>(), Projectile.damage, 0f, Main.myPlayer, 130f, 120f);
        }
    }

    // Expanding damaging ring — GAMMA HEART's radiation pulse and PHOSPHORESCENT GAUNTLET's punch shockwave.
    // ai[0] = max radius, ai[1] = growth time in frames.
    public class ExpandingRingProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private int hurtCooldown = 0;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = (int)(Projectile.ai[1] > 0f ? Projectile.ai[1] : 30f) + 4;
            }
            Projectile.localAI[1]++;
            if (hurtCooldown > 0) hurtCooldown--;
        }

        private float GrowthTime => Projectile.ai[1] > 0f ? Projectile.ai[1] : 30f;
        private float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 220f;
        private float Radius => MaxRadius * MathHelper.Clamp(Projectile.localAI[1] / GrowthTime, 0f, 1f);

        public override bool CanHitPlayer(Player target)
        {
            if (hurtCooldown > 0) return false;
            float dist = Vector2.Distance(target.Center, Projectile.Center);
            if (Math.Abs(dist - Radius) > 45f) return false;
            hurtCooldown = 20;
            return true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            float r = Radius;
            const int segs = 28;
            for (int i = 0; i < segs; i++)
            {
                float a = MathHelper.TwoPi * i / segs;
                Vector2 pos = center + a.ToRotationVector2() * r;
                Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 255, 110, 0) * 0.6f, a, new Vector2(0.5f), new Vector2(30f, 10f), SpriteEffects.None, 0f);
            }
            return false;
        }
    }

    // ACIDIC EXHAUST — the dash burns a straight trail; shortly after, it ignites into a line of fire.
    // ai[0]/ai[1] = direction, ai[2] = trail length.
    public class ExhaustTrailProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 45;
        public override bool? CanDamage() => false;

        public override void SetDefaults()
        {
            Projectile.width = 8; Projectile.height = 8;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = TelegraphTime + 4;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == TelegraphTime && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item45, Projectile.Center);
                Vector2 dir = new(Projectile.ai[0], Projectile.ai[1]);
                float length = Projectile.ai[2] > 0f ? Projectile.ai[2] : 500f;
                const int count = 6;
                for (int i = 0; i < count; i++)
                {
                    Vector2 pos = Projectile.Center + dir * (length * i / (count - 1));
                    int idx = Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, Vector2.Zero, ModContent.ProjectileType<ExhaustFireProj>(), Projectile.damage, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = i * 4f;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f);
            Vector2 dir = new(Projectile.ai[0], Projectile.ai[1]);
            float length = Projectile.ai[2] > 0f ? Projectile.ai[2] : 500f;
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = start + dir * length * t;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, start, new Rectangle(0, 0, 1, 1), new Color(255, 140, 60) * 0.6f, dir.ToRotation(), new Vector2(0f, 0.5f), new Vector2((end - start).Length(), 6f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class ExhaustFireProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 70; Projectile.height = 70;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 140;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] >= Projectile.ai[0] && Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                SoundEngine.PlaySound(SoundID.Item45 with { Pitch = 0.2f }, Projectile.Center);
                ScourgeFx.Burst(Projectile.Center, 5f, 12, DustID.Torch);
            }
            if (Projectile.localAI[1] >= 1f && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f), DustID.Torch, new Vector2(0f, -2f), 100, default, 1.3f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.localAI[1] < 1f) return false;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float p = MathHelper.Clamp(Projectile.timeLeft / 110f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 120, 40, 0) * 0.5f * p, 0f, new Vector2(0.5f), new Vector2(70f, 90f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 220, 140) * p, 0f, new Vector2(0.5f), new Vector2(30f, 90f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
