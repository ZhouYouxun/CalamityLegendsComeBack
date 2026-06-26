using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // Delayed eruptive column at cursor position (AbyssalRupture state).
    // ai[0] = delay frames before becoming active.
    internal sealed class SeasSearingAbyssalGeyser : ModProjectile, ILocalizedModType
    {
        private const int ActiveFrames = 28;
        private bool activated;
        private int  activeTimer;

        private int DelayFrames => (int)Projectile.ai[0];

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width          = 28;
            Projectile.height         = 220;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
            Projectile.penetrate      = -1;
            Projectile.timeLeft       = 200;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = -1;
        }

        public override bool? CanDamage() => activated && activeTimer >= 2;

        public override void AI()
        {
            if (!activated)
            {
                float delayThresh = Projectile.ai[0];
                float progress    = delayThresh > 0f ? Projectile.localAI[1] / delayThresh : 1f;
                Projectile.localAI[1]++;
                Projectile.friendly = false;

                if (!Main.dedServ && Main.GameUpdateCount % 6 == 0)
                {
                    SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 1.5f, 10f, 12,
                        Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.WarningOrange, progress));
                }

                if (Projectile.localAI[1] >= Projectile.ai[0])
                {
                    activated = true;
                    Projectile.friendly = true;
                    SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 8f, 14f, 28, SeasSearingPalette.WarningOrange);
                    SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 45, 9f, 16f, 1.5f);
                    SeasSearingVisualUtility.ShakeAt(Projectile.Center, 5.5f, 1400f);
                    SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.76f, Pitch = -0.42f }, Projectile.Center);
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.58f, Pitch = -0.58f }, Projectile.Center);
                }
                return;
            }

            activeTimer++;
            if (activeTimer > ActiveFrames)
            {
                Projectile.Kill();
                return;
            }

            float t = activeTimer / (float)ActiveFrames;
            Lighting.AddLight(Projectile.Center, Color.Lerp(SeasSearingPalette.WarningOrange, SeasSearingPalette.RadioactiveCyan, t).ToVector3() * 0.65f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                float   rx  = Main.rand.NextFloat(-12f, 12f);
                float   ry  = Main.rand.NextFloat(-100f, 10f);
                Vector2 pos = Projectile.Center + new Vector2(rx, ry);
                Vector2 vel = new(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-8f, -3f));
                Color   col = Main.rand.NextBool(3)
                    ? SeasSearingPalette.WarningOrange
                    : Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.ToxicGreen, Main.rand.NextFloat());
                Dust d = Dust.NewDustPerfect(pos, Main.rand.NextBool(2) ? DustID.Water : DustID.GemEmerald,
                    vel, 110, col, Main.rand.NextFloat(0.65f, 1.25f));
                d.noGravity = true;
            }

            if (activeTimer == ActiveFrames / 2 && Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<SeasSearingFalloutCloud>(),
                    Math.Max(1, Projectile.damage / 4), 0f, Projectile.owner, 20f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 12);
            target.AddBuff(BuffID.Venom, 360);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 420);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!activated) return false;

            Texture2D bloom  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring   = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            float t          = activeTimer / (float)ActiveFrames;
            float fade       = (float)Math.Sin(t * MathHelper.Pi);
            Vector2 center   = Projectile.Center - Main.screenPosition;

            Color orange = (SeasSearingPalette.WarningOrange   with { A = 0 }) * fade;
            Color cyan   = (SeasSearingPalette.RadioactiveCyan with { A = 0 }) * fade;

            Main.EntitySpriteDraw(bloom, center, null, orange * 0.7f, 0f, bloom.Size() * 0.5f,
                new Vector2(1.8f * (0.6f + t * 0.8f), 0.22f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, center, null, cyan * 0.55f, 0f, bloom.Size() * 0.5f,
                new Vector2(0.18f, 3.8f * fade), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring, center, null, orange * 0.8f,
                Main.GlobalTimeWrappedHourly * 4f, ring.Size() * 0.5f, 0.22f + t * 0.55f, SpriteEffects.None, 0);

            return false;
        }
    }
}
