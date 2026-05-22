using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
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
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
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
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float t = (float)Main.GameUpdateCount * 0.18f + Projectile.identity * 0.31f;
            bool firstSubstep = Projectile.numUpdates == 0;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Color(170, 220, 255).ToVector3() * 0.36f);

            if (!firstSubstep)
                return;

            float wave = (float)System.Math.Sin(t * 1.15f) * 4.2f;
            Vector2 coreSpawnPos = Projectile.Center - forward * Main.rand.NextFloat(3f, 7f) + right * wave;
            Vector2 coreVelocity = -forward * Main.rand.NextFloat(0.45f, 1.15f) + right * (float)System.Math.Cos(t * 1.4f) * 0.16f;

            SquishyLightParticle particle = new(
                coreSpawnPos,
                coreVelocity,
                Main.rand.NextFloat(0.44f, 0.67f),
                Color.Lerp(new Color(220, 240, 255), Color.White, Main.rand.NextFloat(0.18f, 0.55f)) * 0.7f,
                Main.rand.Next(13, 19)
            );
            GeneralParticleHandler.SpawnParticle(particle);

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                float phase = t + i * 1.13f;
                float lateral = (float)System.Math.Sin(phase * 1.35f) * 5.2f;

                Vector2 spawnPos = Projectile.Center - forward * Main.rand.NextFloat(4f, 8f) + right * lateral * side;
                Vector2 sparkVelocity =
                    (-forward + right * side * 0.42f).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1.8f, 3.9f) +
                    right * side * (float)System.Math.Cos(phase) * 0.22f;

                GlowSparkParticle spark = new GlowSparkParticle(
                    spawnPos,
                    sparkVelocity,
                    false,
                    Main.rand.Next(6, 8),
                    Main.rand.NextFloat(0.011f, 0.017f),
                    Color.Lerp(new Color(220, 240, 255), Color.White, Main.rand.NextFloat(0.15f, 0.50f)) * 0.7f,
                    new Vector2(1.8f, 1f),
                    true,
                    false,
                    1.08f
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.GameUpdateCount % 2 == 0)
            {
                Vector2 spikePosition =
                    Projectile.Center
                    - forward * Main.rand.NextFloat(2f, 8f)
                    + right * Main.rand.NextFloat(-3f, 3f);

                Vector2 spikeVelocity =
                    -forward * Main.rand.NextFloat(2.8f, 5.2f)
                    + right * Main.rand.NextFloat(-0.25f, 0.25f);

                GlowSparkParticle iceSpike = new(
                    spikePosition,
                    spikeVelocity,
                    false,
                    Main.rand.Next(7, 10),
                    Main.rand.NextFloat(0.018f, 0.026f),
                    Color.Lerp(new Color(170, 225, 255), Color.White, Main.rand.NextFloat(0.35f, 0.8f)),
                    new Vector2(2.55f, 0.72f),
                    true,
                    false,
                    1.16f
                );
                GeneralParticleHandler.SpawnParticle(iceSpike);
            }

            if (Main.GameUpdateCount % 2 == 0)
            {
                float side = Main.rand.NextBool() ? -1f : 1f;
                float phase = t * 0.92f + side * 2.1f;
                Vector2 dustPos =
                    Projectile.Center
                    - forward * Main.rand.NextFloat(4f, 10f)
                    + right * (float)System.Math.Sin(phase) * Main.rand.NextFloat(2.5f, 5.5f) * side;

                Vector2 dustVel =
                    (-forward).RotatedBy((float)System.Math.Sin(phase * 1.4f) * 0.14f) * Main.rand.NextFloat(0.9f, 2.4f) +
                    right * side * Main.rand.NextFloat(0.08f, 0.36f);

                Dust dust = Dust.NewDustPerfect(
                    dustPos,
                    Main.rand.NextBool(2) ? DustID.IceTorch : DustID.GemDiamond,
                    dustVel,
                    0,
                    Color.Lerp(new Color(120, 170, 255), Color.White, Main.rand.NextFloat(0.28f, 0.72f)),
                    Main.rand.NextFloat(0.66f, 0.9f)
                );
                dust.noGravity = true;
            }

            if (Main.GameUpdateCount % 2 == 0)
            {
                float angle = t * 0.85f;
                float radius = Main.rand.NextFloat(3f, 6f);

                Vector2 pos = Projectile.Center - forward * Main.rand.NextFloat(4f, 8f) + (Projectile.rotation + angle).ToRotationVector2() * radius;
                Vector2 vel = -forward * Main.rand.NextFloat(0.25f, 0.75f) + right * (float)System.Math.Sin(angle * 1.5f) * 0.18f;

                Particle mist = new MediumMistParticle(
                    pos,
                    vel,
                    Color.White * 0.7f,
                    Color.Transparent,
                    Main.rand.NextFloat(0.3f, 0.48f),
                    Main.rand.NextFloat(78f, 110f)
                );
                GeneralParticleHandler.SpawnParticle(mist);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i - 1] == Vector2.Zero)
                    continue;

                Vector2 current = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 previous = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 delta = previous - current;
                float length = delta.Length();
                if (length <= 1f)
                    continue;

                float completion = i / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(new Color(210, 245, 255, 170), new Color(60, 130, 255, 40), completion);

                Main.EntitySpriteDraw(
                    pixel,
                    current,
                    new Rectangle(0, 0, 1, 1),
                    trailColor * (0.65f - completion * 0.35f),
                    delta.ToRotation(),
                    new Vector2(0f, 0.5f),
                    new Vector2(length, MathHelper.Lerp(5.5f, 1.2f, completion)),
                    SpriteEffects.None,
                    0f);
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
                SpriteEffects.FlipVertically,
                0f);

            Main.EntitySpriteDraw(
                line,
                drawPos + forward * 4f,
                null,
                new Color(235, 255, 255, 210),
                rotation,
                line.Size() * 0.5f,
                new Vector2(0.035f, 0.36f),
                SpriteEffects.FlipVertically,
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
