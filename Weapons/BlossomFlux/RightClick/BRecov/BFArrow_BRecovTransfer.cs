using System;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    internal class BFArrow_BRecovTransfer : ModProjectile
    {
        internal const float LeftHitSpawnMode = 0f;
        internal const float ChargedReleaseSpawnMode = 1f;

        private const int LeftScatterFrames = 10;
        private const int ChargedScatterFrames = 18;
        private const int LeftHoverFrames = 8;
        private const int ChargedHoverFrames = 10;
        private const int MaxLifetimeFrames = 150;
        private const int NoTargetFlashFrames = 8;
        private const float BaseSearchRange = 3200f;
        private const float ContactDistance = 20f;

        private bool healedTarget;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float TargetPlayerIndex => ref Projectile.ai[0];
        private ref float StoredHealAmount => ref Projectile.ai[1];
        private ref float SpawnMode => ref Projectile.ai[2];
        private ref float Timer => ref Projectile.localAI[0];
        private ref float StateTimer => ref Projectile.localAI[1];

        private bool FromChargedRelease => SpawnMode == ChargedReleaseSpawnMode;
        private int ScatterFramesInUpdates => (FromChargedRelease ? ChargedScatterFrames : LeftScatterFrames) * Projectile.MaxUpdates;
        private int HoverFramesInUpdates => (FromChargedRelease ? ChargedHoverFrames : LeftHoverFrames) * Projectile.MaxUpdates;
        private Player Owner => Main.player[Projectile.owner];

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
            Projectile.alpha = 255;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = MaxLifetimeFrames * Projectile.MaxUpdates;
        }

        public override bool? CanDamage() => false;

        internal static int Spawn(IEntitySource source, Vector2 center, Vector2 velocity, int owner, float healAmount, float spawnMode)
        {
            int targetIndex = BFArrowCommon.InBounds(owner, Main.maxPlayers)
                ? FindRecoveryTargetPlayerIndex(Main.player[owner], center, BaseSearchRange)
                : -1;

            return Projectile.NewProjectile(
                source,
                center,
                velocity,
                ModContent.ProjectileType<BFArrow_BRecovTransfer>(),
                0,
                0f,
                owner,
                targetIndex,
                healAmount,
                spawnMode);
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            StateTimer = ScatterFramesInUpdates + HoverFramesInUpdates;
            if (StoredHealAmount <= 0f)
                StoredHealAmount = 3f;

            if (Projectile.velocity.LengthSquared() < 0.01f)
            {
                Projectile.velocity = FromChargedRelease
                    ? (-Vector2.UnitY * Owner.gravDir).RotatedByRandom(0.86f) * Main.rand.NextFloat(2.4f, 5.4f)
                    : Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 5.6f);
            }

            if (FromChargedRelease)
            {
                Vector2 upward = -Vector2.UnitY * Owner.gravDir;
                Projectile.velocity += upward * Main.rand.NextFloat(1.2f, 2.8f);
                Projectile.scale = Main.rand.NextFloat(1.04f, 1.18f);
            }
            else
            {
                Projectile.velocity = Projectile.velocity.RotatedByRandom(0.55f) * Main.rand.NextFloat(0.92f, 1.18f);
                Projectile.scale = Main.rand.NextFloat(0.92f, 1.08f);
            }

            if (!HasValidHealTarget() && Projectile.owner == Main.myPlayer)
                RefreshHealTarget();

            if (!HasValidHealTarget())
                StartNoTargetFlash();
        }

        public override void AI()
        {
            Timer++;
            bool finalUpdate = Projectile.FinalExtraUpdate();
            Projectile.Opacity = Utils.GetLerpValue(0f, 8f * Projectile.MaxUpdates, Timer, true) *
                Utils.GetLerpValue(0f, 18f * Projectile.MaxUpdates, Projectile.timeLeft, true);

            Lighting.AddLight(Projectile.Center, new Color(105, 255, 145).ToVector3() * 0.58f * Projectile.Opacity);

            if (finalUpdate && Projectile.owner == Main.myPlayer && (!HasValidHealTarget() || StateTimer <= HoverFramesInUpdates))
                RefreshHealTarget();

            if (!HasValidHealTarget() && Projectile.timeLeft > NoTargetFlashFrames * Projectile.MaxUpdates)
                StartNoTargetFlash();

            if (StateTimer > HoverFramesInUpdates)
            {
                StateTimer--;
                ScatterBehavior();

                if (finalUpdate && StateTimer == HoverFramesInUpdates)
                    SpawnTransferBurst(Projectile.Center, 0.9f, false);
            }
            else if (StateTimer > 0f)
            {
                StateTimer--;
                HoverBehavior();
            }
            else if (HasValidHealTarget())
            {
                Player target = Main.player[(int)TargetPlayerIndex];
                HomeToTarget(target);

                if (Projectile.Hitbox.Intersects(target.Hitbox) || Vector2.Distance(Projectile.Center, target.Center) <= ContactDistance)
                {
                    HealTarget(target);
                    return;
                }
            }
            else
            {
                NoTargetFlashBehavior();
            }

            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation();

            EmitTransferTrail();
        }

        public override void OnKill(int timeLeft)
        {
            SpawnTransferBurst(Projectile.Center, healedTarget ? 0.55f : 0.88f, healedTarget);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D lineTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/ThinEndedLine").Value;
            Texture2D magicTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D circleTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_04").Value;
            Color mainColor = new Color(110, 255, 150, 180) * Projectile.Opacity;
            Color accentColor = new Color(220, 255, 235, 205) * Projectile.Opacity;
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;
            float pulse = 0.88f + 0.12f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8.4f + Projectile.identity * 0.47f);

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
                    mainColor * (0.26f * completion),
                    Projectile.rotation + MathHelper.PiOver2,
                    lineTexture.Size() * 0.5f,
                    new Vector2(0.022f, 0.05f + 0.04f * completion),
                    SpriteEffects.None,
                    0f);
            }

            Main.EntitySpriteDraw(
                bloomTexture,
                drawCenter,
                null,
                mainColor * 0.84f,
                0f,
                bloomTexture.Size() * 0.5f,
                0.082f,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                bloomTexture,
                drawCenter,
                null,
                accentColor * 0.52f,
                0f,
                bloomTexture.Size() * 0.5f,
                0.042f,
                SpriteEffects.None,
                0f);

            const float tinyMagicScale = 0.1f;

            Main.EntitySpriteDraw(
                magicTexture,
                drawCenter,
                null,
                mainColor * 0.24f,
                -Projectile.rotation * 0.72f,
                magicTexture.Size() * 0.5f,
                0.17f * Projectile.scale * pulse * tinyMagicScale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                circleTexture,
                drawCenter,
                null,
                accentColor * 0.18f,
                Projectile.rotation * 0.86f + MathHelper.PiOver4,
                circleTexture.Size() * 0.5f,
                0.12f * Projectile.scale * (1.08f - 0.08f * pulse) * tinyMagicScale,
                SpriteEffects.None,
                0f);

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

        internal static int FindRandomInjuredPlayerIndex(Player owner, Vector2 center, float maxDistance)
        {
            int[] candidates = new int[Main.maxPlayers];
            int candidateCount = 0;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player candidate = Main.player[i];
                if (!candidate.active || candidate.dead || candidate.statLife >= candidate.statLifeMax2)
                    continue;

                float allowedDistance = candidate.lifeMagnet ? maxDistance * 1.5f : maxDistance;
                if (Vector2.Distance(candidate.Center, center) > allowedDistance)
                    continue;

                candidates[candidateCount++] = i;
            }

            if (candidateCount <= 0)
                return -1;

            return candidates[Main.rand.Next(candidateCount)];
        }

        private static int FindRecoveryTargetPlayerIndex(Player owner, Vector2 center, float maxDistance)
        {
            int bestIndex = -1;
            float bestScore = float.MinValue;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player candidate = Main.player[i];
                if (!candidate.active || candidate.dead)
                    continue;

                float allowedDistance = candidate.lifeMagnet ? maxDistance * 1.5f : maxDistance;
                if (Vector2.Distance(candidate.Center, center) > allowedDistance)
                    continue;

                BFRecoveryEcologyPlayer ecology = candidate.GetModPlayer<BFRecoveryEcologyPlayer>();
                float healthMissing = candidate.statLifeMax2 > 0 ? 1f - candidate.statLife / (float)candidate.statLifeMax2 : 0f;
                float leafMissing = 1f - MathHelper.Clamp(ecology.LeafTimeLeft / (float)Math.Max(1, BFRecoveryLeftBalance.GetStats().LeafMaxTime), 0f, 1f);
                float ownerBias = candidate.whoAmI == owner.whoAmI ? 0.04f : 0f;
                float score = healthMissing * 2.4f + leafMissing + ownerBias;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestIndex = i;
            }

            return bestIndex;
        }

        private void RefreshHealTarget()
        {
            int newTargetIndex = FindRecoveryTargetPlayerIndex(Owner, Projectile.Center, BaseSearchRange);
            if ((int)TargetPlayerIndex == newTargetIndex)
                return;

            TargetPlayerIndex = newTargetIndex;
            Projectile.netUpdate = true;
        }

        private void StartNoTargetFlash()
        {
            Projectile.velocity = Vector2.Zero;
            StateTimer = 0f;

            int flashTimeLeft = NoTargetFlashFrames * Projectile.MaxUpdates;
            if (Projectile.timeLeft > flashTimeLeft)
                Projectile.timeLeft = flashTimeLeft;

            Projectile.netUpdate = true;

            if (Projectile.FinalExtraUpdate())
                SpawnTransferBurst(Projectile.Center, 0.9f, false);
        }

        private void NoTargetFlashBehavior()
        {
            Projectile.velocity = Vector2.Zero;
        }

        private bool HasValidHealTarget()
        {
            if (!BFArrowCommon.InBounds(TargetPlayerIndex, Main.maxPlayers))
                return false;

            Player target = Main.player[(int)TargetPlayerIndex];
            return target.active && !target.dead;
        }

        private void ScatterBehavior()
        {
            Projectile.velocity *= FromChargedRelease ? 0.972f : 0.955f;
            Projectile.velocity = Projectile.velocity.RotatedBy(MathF.Sin((Projectile.identity * 0.41f) + Timer * 0.028f) * (FromChargedRelease ? 0.011f : 0.007f));

            if (FromChargedRelease)
                Projectile.velocity += -Vector2.UnitY * Owner.gravDir * 0.026f;
        }

        private void HoverBehavior()
        {
            Projectile.velocity *= 0.88f;
            Projectile.velocity.Y += (float)Math.Sin((Projectile.identity * 0.3f) + Timer * 0.06f) * 0.015f;
            if (FromChargedRelease)
                Projectile.velocity += -Vector2.UnitY * Owner.gravDir * 0.01f;
        }

        private void HomeToTarget(Player target)
        {
            Vector2 targetCenter = target.Center + target.velocity * 4f;
            Vector2 playerVector = targetCenter - Projectile.Center;
            float targetDistance = playerVector.Length();
            float baseSpeed = target.lifeMagnet ? 10.2f : 8.8f;
            float homeSpeed = MathHelper.Lerp(baseSpeed, baseSpeed + 3.2f, Utils.GetLerpValue(260f, 28f, targetDistance, true));
            float steering = MathHelper.Lerp(0.18f, 0.42f, Utils.GetLerpValue(220f, 24f, targetDistance, true));
            Vector2 desiredVelocity = playerVector.SafeNormalize(Vector2.UnitY) * homeSpeed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, steering);

            int minimumTimeLeft = 40 * Projectile.MaxUpdates;
            if (Projectile.timeLeft < minimumTimeLeft)
                Projectile.timeLeft = minimumTimeLeft;
        }

        private void HealTarget(Player target)
        {
            int maxHealAmount = Utils.Clamp((int)MathF.Round(StoredHealAmount), 1, 999);
            int healAmount = Math.Min(maxHealAmount, Math.Max(0, target.statLifeMax2 - target.statLife));
            if (healAmount > 0)
            {
                target.statLife += healAmount;
                if (target.statLife > target.statLifeMax2)
                    target.statLife = target.statLifeMax2;

                target.HealEffect(healAmount, true);
            }

            if (!FromChargedRelease)
                target.GetModPlayer<BFRecoveryEcologyPlayer>().AddRecoveryLeaf(BFRecoveryLeftBalance.GetStats().LeafTimePerFlash);

            healedTarget = true;
            SpawnTransferBurst(target.Center, 1.15f, true);
            SoundEngine.PlaySound(BlossomFluxSounds.RightRecoveryTransfer, target.Center);
            Projectile.Kill();
        }

        private void EmitTransferTrail()
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);
            Color acidGreen = new(105, 255, 145);

            if (Main.rand.NextBool(2))
            {
                GlowOrbParticle orb = new(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    -Projectile.velocity * 0.035f + Main.rand.NextVector2Circular(0.25f, 0.25f),
                    false,
                    10,
                    Main.rand.NextFloat(0.18f, 0.3f),
                    Color.Lerp(mainColor, acidGreen, Main.rand.NextFloat(0.35f, 0.7f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if ((int)Timer % Projectile.MaxUpdates == 0)
            {
                GenericSparkle sparkle = new(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextVector2Circular(0.18f, 0.18f),
                    Color.White,
                    Color.Lerp(acidGreen, Color.White, 0.22f),
                    0.82f,
                    7,
                    0f,
                    1.12f);
                GeneralParticleHandler.SpawnParticle(sparkle);
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextBool(2) ? DustID.GreenTorch : DustID.GemEmerald,
                    -Projectile.velocity * 0.03f + Main.rand.NextVector2Circular(0.22f, 0.22f),
                    100,
                    Color.Lerp(acidGreen, Color.White, 0.18f),
                    Main.rand.NextFloat(0.85f, 1.08f));
                dust.noGravity = true;
            }
        }

        private void SpawnTransferBurst(Vector2 center, float intensity, bool downward)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);
            Color acidGreen = new(105, 255, 145);
            Vector2 direction = downward ? Vector2.UnitY : -Vector2.UnitY;

            DirectionalPulseRing pulse = new(
                center,
                direction * 0.25f,
                Color.Lerp(acidGreen, Color.White, 0.18f),
                new Vector2(0.8f, 1.5f),
                direction.ToRotation(),
                0.15f * intensity,
                0.038f,
                14);
            GeneralParticleHandler.SpawnParticle(pulse);

            BloomLineVFX beam = new(
                center - direction * 18f,
                direction * 36f,
                0.92f * intensity,
                Color.Lerp(mainColor, acidGreen, 0.52f),
                12);
            GeneralParticleHandler.SpawnParticle(beam);

            StrongBloom bloom = new(center, Vector2.Zero, Color.Lerp(acidGreen, Color.White, 0.18f), 0.76f * intensity, 14);
            GeneralParticleHandler.SpawnParticle(bloom);
        }
    }
}

