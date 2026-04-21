using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal class BFArrow_BRecovTransfer : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float TargetPlayerIndex => ref Projectile.ai[0];
        private ref float StoredHealAmount => ref Projectile.ai[1];
        private ref float Phase => ref Projectile.localAI[0];
        private ref float Timer => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.alpha = 255;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Timer++;
            Projectile.Opacity = Utils.GetLerpValue(0f, 5f, Timer, true) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, new Color(110, 255, 150).ToVector3() * 0.55f * Projectile.Opacity);

            if (!BFArrowCommon.InBounds(TargetPlayerIndex, Main.maxPlayers))
            {
                Projectile.Kill();
                return;
            }

            Player target = Main.player[(int)TargetPlayerIndex];
            if (!target.active || target.dead)
            {
                Projectile.Kill();
                return;
            }

            if (Phase == 0f)
            {
                Vector2 liftVelocity = new Vector2(0f, -18f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, liftVelocity, 0.32f);

                if (Timer == 1f)
                    SpawnTransferBurst(Projectile.Center, 0.95f, false);

                if (Timer >= 8f)
                {
                    Phase = 1f;
                    Timer = 0f;
                    Projectile.netUpdate = true;
                }
            }
            else if (Phase == 1f)
            {
                Vector2 hoverPoint = target.Top + new Vector2(0f, -42f);
                Vector2 desiredVelocity = (hoverPoint - Projectile.Center).SafeNormalize(Vector2.UnitY) * 24f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.28f);

                if (Vector2.Distance(Projectile.Center, hoverPoint) <= 22f)
                {
                    Projectile.Center = hoverPoint;
                    Phase = 2f;
                    Timer = 0f;
                    Projectile.netUpdate = true;
                    SpawnTransferBurst(hoverPoint, 1.1f, true);
                    SoundEngine.PlaySound(SoundID.Item109 with { Volume = 0.42f, Pitch = 0.4f }, hoverPoint);
                }
            }
            else
            {
                Vector2 destination = target.Top + new Vector2(0f, 10f);
                Vector2 desiredVelocity = (destination - Projectile.Center).SafeNormalize(Vector2.UnitY) * 34f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.44f);

                if (Projectile.Hitbox.Intersects(target.Hitbox) || Vector2.Distance(Projectile.Center, destination) <= 16f)
                {
                    HealTarget(target);
                    return;
                }
            }

            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation();

            EmitTransferTrail();
        }

        public override void OnKill(int timeLeft)
        {
            SpawnTransferBurst(Projectile.Center, 0.9f, Phase >= 2f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D lineTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            Color mainColor = new Color(96, 255, 150, 0) * Projectile.Opacity;
            Color accentColor = new Color(220, 255, 235, 0) * Projectile.Opacity;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(
                    lineTexture,
                    drawPosition,
                    null,
                    mainColor * (0.3f * completion),
                    Projectile.rotation + MathHelper.PiOver2,
                    lineTexture.Size() * 0.5f,
                    new Vector2(0.025f, 0.06f + 0.045f * completion),
                    SpriteEffects.None,
                    0f);
            }

            Main.EntitySpriteDraw(bloomTexture, Projectile.Center - Main.screenPosition, null, mainColor * 0.8f, 0f, bloomTexture.Size() * 0.5f, 0.09f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloomTexture, Projectile.Center - Main.screenPosition, null, accentColor * 0.55f, 0f, bloomTexture.Size() * 0.5f, 0.045f, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void HealTarget(Player target)
        {
            int healAmount = Utils.Clamp((int)StoredHealAmount, 3, 12);
            target.statLife += healAmount;
            if (target.statLife > target.statLifeMax2)
                target.statLife = target.statLifeMax2;

            target.HealEffect(healAmount, true);
            SpawnTransferBurst(target.Center, 1.25f, true);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.62f, Pitch = 0.34f }, target.Center);
            Projectile.Kill();
        }

        private void EmitTransferTrail()
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);

            if (Main.rand.NextBool(2))
            {
                GlowOrbParticle orb = new(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    -Projectile.velocity * 0.035f + Main.rand.NextVector2Circular(0.25f, 0.25f),
                    false,
                    10,
                    Main.rand.NextFloat(0.2f, 0.34f),
                    Color.Lerp(mainColor, Color.White, 0.28f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if ((int)Timer % 2 == 0)
            {
                GenericSparkle sparkle = new(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextVector2Circular(0.2f, 0.2f),
                    Color.White,
                    Color.Lerp(mainColor, Color.White, 0.32f),
                    0.8f,
                    7,
                    0f,
                    1.15f);
                GeneralParticleHandler.SpawnParticle(sparkle);
            }
        }

        private void SpawnTransferBurst(Vector2 center, float intensity, bool downward)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);
            Vector2 direction = downward ? Vector2.UnitY : -Vector2.UnitY;

            DirectionalPulseRing pulse = new(
                center,
                direction * 0.25f,
                Color.Lerp(mainColor, Color.White, 0.2f),
                new Vector2(0.8f, 1.5f),
                direction.ToRotation(),
                0.16f * intensity,
                0.038f,
                14);
            GeneralParticleHandler.SpawnParticle(pulse);

            BloomLineVFX beam = new(center - direction * 20f, direction * 40f, 1f * intensity, Color.Lerp(mainColor, Color.White, 0.3f), 12);
            GeneralParticleHandler.SpawnParticle(beam);

            StrongBloom bloom = new(center, Vector2.Zero, Color.Lerp(mainColor, Color.White, 0.3f), 0.8f * intensity, 14);
            GeneralParticleHandler.SpawnParticle(bloom);
        }
    }
}
