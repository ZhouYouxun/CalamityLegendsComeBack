using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    // A fast-growing radiant barricade. It damages during the rise, then becomes a player-only solid wall.
    public class AegisWallProjectile : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public static readonly int WallHalfWidth = BalanceAegisBlade.WallWidthTiles * 16 / 2;
        public static readonly int WallHalfHeight = BalanceAegisBlade.WallHeightTiles * 16 / 2;

        private ref float Side => ref Projectile.ai[0];
        private ref float Phase => ref Projectile.ai[1];
        private ref float RiseTimer => ref Projectile.localAI[0];
        private ref float SolidTimer => ref Projectile.localAI[1];
        private bool solidifyEffectFired;

        private static readonly Color OuterGold = new(220, 138, 38);
        private static readonly Color CoreGold = new(255, 208, 84);
        private static readonly Color CoreWhite = new(255, 246, 194);

        public override void SetDefaults()
        {
            Projectile.width = WallHalfWidth * 2;
            Projectile.height = WallHalfHeight * 2;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BalanceAegisBlade.WallRiseTime + BalanceAegisBlade.WallDuration;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override bool ShouldUpdatePosition() => Phase < 1f;

        public override void AI()
        {
            if (Phase < 1f)
            {
                RiseTimer++;
                Lighting.AddLight(Projectile.Center, CoreGold.ToVector3() * 0.4f);
                if (!Main.dedServ && Main.rand.NextBool(2))
                    EmitRisingFlame();

                if (RiseTimer < BalanceAegisBlade.WallRiseTime)
                    return;

                Phase = 1f;
                Projectile.velocity = Vector2.Zero;
                Projectile.friendly = false;
                SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.72f, Pitch = -0.5f }, Projectile.Center);
                EmitSolidifyBurst();
                return;
            }

            SolidTimer++;
            Lighting.AddLight(Projectile.Center, CoreGold.ToVector3() * 0.2f);
            if (SolidTimer >= GetSolidDuration())
            {
                Projectile.Kill();
                return;
            }

            if (!Main.dedServ && Main.rand.NextBool(4))
                EmitSolidFlame();
        }

        private static int GetSolidDuration()
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.boss)
                    return BalanceAegisBlade.WallDurationBoss;
            }
            return BalanceAegisBlade.WallDuration;
        }

        private void EmitRisingFlame()
        {
            float bottomY = Projectile.Center.Y + WallHalfHeight;
            Vector2 position = new(Projectile.Center.X + Side * Main.rand.NextFloat(0f, WallHalfWidth), bottomY);
            Vector2 velocity = new(Side * Main.rand.NextFloat(0.4f, 2.5f), -Main.rand.NextFloat(2.5f, 7.5f));
            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(position, velocity,
                Color.Lerp(OuterGold, CoreGold, Main.rand.NextFloat()), Color.Transparent,
                Main.rand.NextFloat(0.36f, 0.68f), Main.rand.Next(18, 28), Main.rand.NextFloat(-0.05f, 0.05f)));
        }

        private void EmitSolidFlame()
        {
            float height = Main.rand.NextFloat(-WallHalfHeight, WallHalfHeight);
            float side = Main.rand.NextBool() ? -WallHalfWidth : WallHalfWidth;
            Vector2 position = Projectile.Center + new Vector2(side, height);
            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(position,
                new Vector2(Math.Sign(side) * Main.rand.NextFloat(0.4f, 1.5f), -Main.rand.NextFloat(0.2f, 1.8f)),
                Color.Lerp(OuterGold, CoreWhite, Main.rand.NextFloat(0.15f, 0.65f)), Color.Transparent,
                Main.rand.NextFloat(0.25f, 0.5f), Main.rand.Next(16, 26), Main.rand.NextFloat(-0.04f, 0.04f)));
        }

        private void EmitSolidifyBurst()
        {
            if (solidifyEffectFired || Main.dedServ)
                return;

            solidifyEffectFired = true;
            Vector2 top = Projectile.Center - Vector2.UnitY * WallHalfHeight;
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(top, Vector2.Zero,
                CoreWhite, new Vector2(0.8f, 1.15f), 0f, 0f, 0.55f, 18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero,
                CoreGold, new Vector2(1.35f, 0.65f), 0f, 0.08f, 1.35f, 20));
            for (int i = 0; i < 8; i++)
            {
                Vector2 position = Projectile.Center + new Vector2(Main.rand.NextFloat(-WallHalfWidth, WallHalfWidth),
                    Main.rand.NextFloat(-WallHalfHeight, WallHalfHeight));
                GeneralParticleHandler.SpawnParticle(new StrongBloom(position, Vector2.Zero,
                    Main.rand.NextBool(3) ? CoreWhite : CoreGold, Main.rand.NextFloat(0.16f, 0.3f), Main.rand.Next(9, 15)));
            }
        }

        public override bool? CanDamage() => Phase < 1f ? null : false;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            float solidDuration = GetSolidDuration();
            float fade = Phase < 1f
                ? MathHelper.Clamp(RiseTimer / BalanceAegisBlade.WallRiseTime, 0f, 1f)
                : MathHelper.Clamp((solidDuration - SolidTimer) / 60f, 0f, 1f);
            float riseProgress = Phase < 1f
                ? MathHelper.Clamp(RiseTimer / BalanceAegisBlade.WallRiseTime, 0f, 1f)
                : 1f;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            const int segmentCount = 14;
            for (int i = 0; i < segmentCount; i++)
            {
                float completion = i / (float)(segmentCount - 1);
                if (completion < 1f - riseProgress)
                    continue;

                Vector2 segmentPosition = center + new Vector2(0f, MathHelper.Lerp(-WallHalfHeight, WallHalfHeight, completion));
                float width = 0.13f + 0.025f * MathF.Sin(Main.GlobalTimeWrappedHourly * 4f + i * 0.8f);
                float height = WallHalfHeight / (float)segmentCount / bloom.Height * 2.8f;
                Color color = Color.Lerp(CoreWhite, OuterGold, completion) with { A = 0 };
                Main.EntitySpriteDraw(bloom, segmentPosition, null, color * fade * 0.76f,
                    0f, bloom.Size() * 0.5f, new Vector2(width, height), SpriteEffects.None);

                if (i % 2 == 0)
                {
                    float sideOffset = WallHalfWidth * 0.72f;
                    Main.EntitySpriteDraw(bloom, segmentPosition + Vector2.UnitX * sideOffset, null,
                        CoreGold with { A = 0 } * fade * 0.24f, 0f, bloom.Size() * 0.5f,
                        new Vector2(0.06f, height * 0.9f), SpriteEffects.None);
                    Main.EntitySpriteDraw(bloom, segmentPosition - Vector2.UnitX * sideOffset, null,
                        CoreGold with { A = 0 } * fade * 0.24f, 0f, bloom.Size() * 0.5f,
                        new Vector2(0.06f, height * 0.9f), SpriteEffects.None);
                }
            }

            Vector2 basePosition = center + Vector2.UnitY * WallHalfHeight;
            Vector2 topPosition = center - Vector2.UnitY * WallHalfHeight;
            float ringPulse = 0.8f + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Main.EntitySpriteDraw(ring, basePosition, null, CoreGold with { A = 0 } * fade * 0.55f,
                0f, ring.Size() * 0.5f, new Vector2(0.42f, 0.17f) * ringPulse, SpriteEffects.None);
            if (riseProgress >= 0.7f)
            {
                Main.EntitySpriteDraw(bloom, topPosition, null, CoreWhite with { A = 0 } * fade * 0.65f,
                    0f, bloom.Size() * 0.5f, 0.48f, SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
