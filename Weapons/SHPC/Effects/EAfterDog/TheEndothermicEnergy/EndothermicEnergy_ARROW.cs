using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheEndothermicEnergy
{
    internal class EndothermicEnergy_ARROW : ModProjectile, ILocalizedModType
    {
        private class EndothermicCopyState
        {
            public bool PendingShadowRelease;
            public int MarkedTargetIndex = -1;
        }

        private readonly System.Collections.Generic.Dictionary<int, EndothermicCopyState> projectileStates = new();
        private const int ExtraUpdateCount = 3;
        private const int VisibleLifetimeFrames = 108;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private EndothermicCopyState GetState()
        {
            if (!projectileStates.TryGetValue(Projectile.whoAmI, out EndothermicCopyState state))
            {
                state = new EndothermicCopyState();
                projectileStates[Projectile.whoAmI] = state;
            }

            return state;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 28;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = ExtraUpdateCount;
            Projectile.timeLeft = VisibleLifetimeFrames * (Projectile.extraUpdates + 1);
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            EndothermicCopyState state = GetState();
            state.PendingShadowRelease = false;
            state.MarkedTargetIndex = -1;
        }

        public override void AI()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Color(170, 220, 255).ToVector3() * 0.36f);

            EmitFlightTrail(forward);
        }

        private void EmitFlightTrail(Vector2 forward)
        {
            if (Main.dedServ)
                return;

            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float t = (float)Main.GameUpdateCount * 0.28f + Projectile.identity * 0.31f + Projectile.numUpdates * 0.19f;
            float ribbonOffset = (float)System.Math.Sin(t * 1.8f) * Main.rand.NextFloat(1.2f, 3.8f);
            Vector2 basePosition = Projectile.Center - forward * Main.rand.NextFloat(2f, 7f) + right * ribbonOffset;

            if (Main.rand.NextBool(2))
            {
                SquishyLightParticle particle = new(
                    basePosition,
                    -forward * Main.rand.NextFloat(0.35f, 0.9f) + right * Main.rand.NextFloat(-0.12f, 0.12f),
                    Main.rand.NextFloat(0.28f, 0.42f),
                    Color.Lerp(new Color(220, 240, 255), Color.White, Main.rand.NextFloat(0.18f, 0.55f)) * 0.5f,
                    Main.rand.Next(9, 14)
                );
                GeneralParticleHandler.SpawnParticle(particle);
            }

            if (Main.rand.NextBool(3))
            {
                float side = Main.rand.NextBool() ? -1f : 1f;
                GlowSparkParticle spark = new GlowSparkParticle(
                    basePosition + right * side * Main.rand.NextFloat(1f, 3f),
                    (-forward + right * side * Main.rand.NextFloat(0.12f, 0.28f)).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1.2f, 2.4f),
                    false,
                    Main.rand.Next(5, 7),
                    Main.rand.NextFloat(0.008f, 0.013f),
                    Color.Lerp(new Color(190, 230, 255), Color.White, Main.rand.NextFloat(0.22f, 0.62f)) * 0.62f,
                    new Vector2(1.45f, 0.72f),
                    true,
                    false,
                    1.04f
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    basePosition + right * Main.rand.NextFloat(-2.8f, 2.8f),
                    Main.rand.NextBool(2) ? DustID.IceTorch : DustID.GemDiamond,
                    -forward * Main.rand.NextFloat(0.55f, 1.55f) + right * Main.rand.NextFloat(-0.18f, 0.18f),
                    0,
                    Color.Lerp(new Color(120, 170, 255), Color.White, Main.rand.NextFloat(0.28f, 0.72f)),
                    Main.rand.NextFloat(0.45f, 0.68f)
                );
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(4))
            {
                Particle mist = new MediumMistParticle(
                    basePosition + right * Main.rand.NextFloat(-3f, 3f),
                    -forward * Main.rand.NextFloat(0.12f, 0.45f) + right * Main.rand.NextFloat(-0.08f, 0.08f),
                    Color.White * 0.42f,
                    Color.Transparent,
                    Main.rand.NextFloat(0.18f, 0.3f),
                    Main.rand.NextFloat(50f, 74f)
                );
                GeneralParticleHandler.SpawnParticle(mist);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Vector2 previous = Projectile.Center;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 current = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 delta = previous - current;
                float length = delta.Length();
                if (length <= 1f)
                {
                    previous = current;
                    continue;
                }

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(new Color(70, 125, 255, 0), new Color(220, 250, 255, 0), completion);

                Main.EntitySpriteDraw(
                    line,
                    previous - Main.screenPosition - delta * 0.5f,
                    null,
                    trailColor * (0.1f + completion * 0.42f),
                    delta.ToRotation() + MathHelper.PiOver2,
                    line.Size() * 0.5f,
                    new Vector2(MathHelper.Lerp(0.06f, 0.18f, completion), length / line.Height),
                    SpriteEffects.None,
                    0f);

                previous = current;
            }

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float rotation = forward.ToRotation() + MathHelper.PiOver2;

            Main.EntitySpriteDraw(
                line,
                drawPos - forward * 10f,
                null,
                new Color(90, 220, 255, 180),
                rotation,
                line.Size() * 0.5f,
                new Vector2(0.11f, 0.48f),
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                line,
                drawPos + forward * 8f,
                null,
                new Color(235, 255, 255, 210),
                rotation,
                line.Size() * 0.5f,
                new Vector2(0.026f, 0.28f),
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                bloom,
                drawPos + forward * 12f,
                null,
                new Color(245, 255, 255, 180),
                rotation,
                bloom.Size() * 0.5f,
                new Vector2(0.045f, 0.018f),
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                bloom,
                drawPos,
                null,
                new Color(120, 230, 255, 120),
                rotation,
                bloom.Size() * 0.5f,
                new Vector2(0.12f, 0.07f),
                SpriteEffects.None,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            EndothermicCopyState state = GetState();

            target.AddBuff(BuffID.Frostburn, 300);
            target.AddBuff(BuffID.Chilled, 180);
            state.PendingShadowRelease = true;
            state.MarkedTargetIndex = target.whoAmI;

            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            EndothermicCopyState state = GetState();

            if (Projectile.owner != Main.myPlayer)
            {
                projectileStates.Remove(Projectile.whoAmI);
                return;
            }

            if (state.PendingShadowRelease && Main.npc.IndexInRange(state.MarkedTargetIndex))
            {
                NPC target = Main.npc[state.MarkedTargetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        target.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<EndothermicEnergy_LN2>(),
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner);

                    for (int arm = 0; arm < 6; arm++)
                    {
                        float armAngle = MathHelper.TwoPi * arm / 6f;
                        Vector2 spawnOffset = armAngle.ToRotationVector2() * 260f;

                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            target.Center + spawnOffset,
                            Vector2.Zero,
                            ModContent.ProjectileType<EndothermicEnergy_Shadow>(),
                            (int)(Projectile.damage * 0.36f),
                            Projectile.knockBack,
                            Projectile.owner,
                            0f,
                            target.whoAmI,
                            armAngle);
                    }
                }
            }

            projectileStates.Remove(Projectile.whoAmI);
        }
    }
}
