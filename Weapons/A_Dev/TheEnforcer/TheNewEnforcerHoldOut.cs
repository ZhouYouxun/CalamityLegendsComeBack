using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.TheEnforcer
{
    internal class TheNewEnforcerHoldOut : BaseSwordHoldoutProjectile, ILocalizedModType
    {
        private const float FlameDamageFactor = 0.42f;
        private const float SlashDamageFactor = 0.34f;

        private bool firedFlames;

        public new string LocalizationCategory => "Projectiles.TheEnforcer";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/TheEnforcer/TheNewEnforcer";
        public override Item BaseItem => ModContent.GetModItem(ModContent.ItemType<TheNewEnforcer>()).Item;
        public override int swingWidth => 240;
        public override int AfterImageLength => 8;
        public override float lineCollisionLength => 226f;
        public override bool drawSwordTrail => true;
        public override int trailLength => 28;
        public override float trailOffset => 38f;
        public override bool useMeleeSpeed => true;
        public override bool useMeleeSize => true;
        public override int StartupTime { get; set; }
        public override int CooldownTime { get; set; }
        public override int OffsetDistance { get; set; } = 72;
        public override Color AfterImageColor => new(130, 95, 255);

        public override Color[] trailColors => new Color[3]
        {
            new(95, 55, 190),
            new(9, 5, 28),
            new(45, 230, 255)
        };

        public override SoundStyle? UseSound => SoundID.Item71 with
        {
            Volume = 0.82f,
            Pitch = -0.12f,
            PitchVariance = 0.08f
        };

        public override void Defaults()
        {
            Projectile.extraUpdates = 3;
            Projectile.noEnchantmentVisuals = true;
            Projectile.scale = 1.35f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void Spawn()
        {
            StartupTime = 1;
            CooldownTime = 1;
            swingTime = System.Math.Max(1, swingTime - StartupTime - CooldownTime);
            OffsetDistance = 72;
            RotateInStartup = 0.28f;
            RotateInCooldown = 0.2f;
            firedFlames = false;
        }

        public override float SwingFunction()
        {
            if (inStartup)
                return MathHelper.ToRadians(MathHelper.SmoothStep(-144f, -120f, 1f - MathF.Pow(1f - StartupCompletion, 2f)));

            if (inCooldown)
                return MathHelper.ToRadians(MathHelper.SmoothStep(120f, 144f, MathF.Pow(CooldownCompletion, 2f)));

            float easedSwing = 0.5f - MathF.Cos(SwingCompletion * MathHelper.Pi) * 0.5f;
            return MathHelper.ToRadians(MathHelper.Lerp(-120f, 120f, easedSwing));
        }

        public override void AdditionalAI()
        {
            Player owner = Main.player[Projectile.owner];

            if (inStartup)
                Projectile.scale = baseScale * MathHelper.Lerp(0.82f, 1.12f, 1f - MathF.Pow(1f - StartupCompletion, 2f));
            else if (inCooldown)
                Projectile.scale = baseScale * MathHelper.Lerp(1.12f, 0.86f, MathF.Pow(CooldownCompletion, 2f));
            else if (inSwing)
            {
                float bell = MathF.Sin(SwingCompletion * MathHelper.Pi);
                Projectile.scale = baseScale * MathHelper.Lerp(1.02f, 1.36f, bell);
                OffsetDistance = (int)MathHelper.Lerp(64f, 84f, bell);

                if (!firedFlames && SwingCompletion >= 0.34f)
                {
                    FireEssenceVolley(owner);
                    ApplyScreenShake(4.2f);
                    firedFlames = true;
                }

                SpawnSwingVFX(owner, bell);
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.18f, 0.74f) * Projectile.Opacity);
        }

        private void FireEssenceVolley(Player owner)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 bladeDirection = GetBladeDirection(owner);
            Vector2 bladeTip = GetBladeTip(owner);
            Vector2 bladeBase = Vector2.Lerp(owner.MountedCenter, Projectile.Center, 0.45f);
            Vector2 normal = bladeDirection.RotatedBy(MathHelper.PiOver2);
            NPC target = FindBestTarget(bladeTip, bladeDirection, 2800f);
            const int flameCount = 7;

            for (int i = 0; i < flameCount; i++)
            {
                float centeredIndex = i - (flameCount - 1) * 0.5f;
                float side = centeredIndex == 0f ? Main.rand.NextBool().ToDirectionInt() : Math.Sign(centeredIndex);
                float bladeCompletion = MathHelper.Clamp(0.48f + i * 0.085f + Main.rand.NextFloat(-0.05f, 0.05f), 0.24f, 1.05f);
                float sideOffset = centeredIndex * Main.rand.NextFloat(12f, 20f);
                Vector2 spawnPosition = Vector2.Lerp(bladeBase, bladeTip, bladeCompletion);
                spawnPosition += normal * sideOffset;
                spawnPosition -= bladeDirection * Main.rand.NextFloat(6f, 34f);
                spawnPosition += Main.rand.NextVector2Circular(9f, 9f);

                float openingSpread = centeredIndex * 0.115f + Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 openingDirection = bladeDirection.RotatedBy(openingSpread);
                Vector2 tangentDirection = normal * side;
                openingDirection = Vector2.Lerp(openingDirection, tangentDirection, 0.18f + Math.Abs(centeredIndex) * 0.035f).SafeNormalize(bladeDirection);

                if (target is not null)
                {
                    float openingSpeed = 12.5f + i * 0.75f;
                    Vector2 predictedTarget = PredictTargetPosition(target, spawnPosition, openingSpeed);
                    Vector2 targetDirection = (predictedTarget - spawnPosition).SafeNormalize(bladeDirection);
                    float targetInfluence = MathHelper.Clamp(0.36f + 0.04f * i, 0.34f, 0.62f);
                    openingDirection = Vector2.Lerp(openingDirection, targetDirection.RotatedBy(openingSpread * 0.35f), targetInfluence).SafeNormalize(targetDirection);
                }

                Vector2 flameVelocity = openingDirection * Main.rand.NextFloat(11.5f, 16.5f);
                int profile = Main.rand.Next(4);
                float flameProfile = i + profile * 10f + Main.rand.NextFloat(0.01f, 0.99f);

                int flame = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    flameVelocity,
                    ModContent.ProjectileType<NewEssenceFlame2>(),
                    System.Math.Max(1, (int)(Projectile.damage * FlameDamageFactor)),
                    Projectile.knockBack * 0.42f,
                    Projectile.owner,
                    target?.whoAmI ?? -1f,
                    flameProfile);

                if (Main.projectile.IndexInRange(flame))
                {
                    Main.projectile[flame].scale = Main.rand.NextFloat(0.92f, 1.16f);
                    Main.projectile[flame].netUpdate = true;
                }

                SpawnFlameReleaseVFX(spawnPosition, flameVelocity, i);
            }

            SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.55f, Pitch = 0.16f }, bladeTip);
        }

        private void SpawnSwingVFX(Player owner, float intensity)
        {
            if (Main.dedServ || timer % Projectile.MaxUpdates != 0)
                return;

            Vector2 bladeDirection = GetBladeDirection(owner);
            Vector2 bladeTip = GetBladeTip(owner);
            Vector2 normal = bladeDirection.RotatedBy(MathHelper.PiOver2);
            Color violet = new(125, 70, 255);
            Color cyan = new(80, 225, 255);

            for (int i = 0; i < 2; i++)
            {
                Vector2 position = Vector2.Lerp(owner.MountedCenter, bladeTip, Main.rand.NextFloat(0.3f, 1f)) + normal * Main.rand.NextFloat(-18f, 18f);
                Vector2 velocity = -bladeDirection.RotatedByRandom(0.34f) * Main.rand.NextFloat(1.8f, 4.8f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    position,
                    velocity,
                    false,
                    Main.rand.Next(12, 19),
                    Main.rand.NextFloat(0.5f, 0.95f) * Projectile.scale,
                    Main.rand.NextBool(3) ? cyan : violet));
            }

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    bladeTip + Main.rand.NextVector2Circular(10f, 10f),
                    -bladeDirection * Main.rand.NextFloat(1.2f, 3.4f),
                    "CalamityMod/Particles/VerticalSmearLarge",
                    false,
                    Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.035f, 0.06f) * Projectile.scale,
                    Color.Lerp(violet, cyan, Main.rand.NextFloat(0.18f, 0.55f)) * (0.58f + intensity * 0.36f),
                    new Vector2(0.85f, 1.55f),
                    true));
            }

            Dust dust = Dust.NewDustPerfect(
                bladeTip + Main.rand.NextVector2Circular(18f, 18f),
                Main.rand.NextBool() ? DustID.Shadowflame : DustID.BlueTorch,
                -bladeDirection.RotatedByRandom(0.45f) * Main.rand.NextFloat(1.5f, 5f),
                100,
                Color.Lerp(violet, cyan, Main.rand.NextFloat()),
                Main.rand.NextFloat(0.9f, 1.35f));
            dust.noGravity = true;
        }

        private void SpawnExecutionSlashes(NPC target)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            const int slashCount = 5;
            for (int i = 0; i < slashCount; i++)
            {
                float angleOffset = MathHelper.TwoPi * i / slashCount + Main.rand.NextFloat(-0.26f, 0.26f);
                Vector2 direction = angleOffset.ToRotationVector2();
                Vector2 spawnPosition = target.Center - direction * Main.rand.NextFloat(24f, 62f) + Main.rand.NextVector2Circular(18f, 18f);

                Projectile.NewProjectile(
                    Projectile.GetSource_OnHit(target),
                    spawnPosition,
                    direction * 0.01f,
                    ModContent.ProjectileType<TheNewEnforcerSlash>(),
                    System.Math.Max(1, (int)(Projectile.damage * SlashDamageFactor)),
                    Projectile.knockBack * 0.28f,
                    Projectile.owner,
                    Main.rand.NextFloat(1.35f, 2.2f),
                    angleOffset + MathHelper.PiOver2);
            }

            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.48f, Pitch = 0.22f }, target.Center);
        }

        private Vector2 GetBladeDirection(Player owner) =>
            (Projectile.Center - owner.MountedCenter).SafeNormalize((-angle).SafeNormalize(Vector2.UnitX * owner.direction));

        private Vector2 GetBladeTip(Player owner) =>
            Projectile.Center + GetBladeDirection(owner) * lineCollisionLength * 0.48f * Projectile.scale;

        private static NPC FindBestTarget(Vector2 origin, Vector2 aimDirection, float range)
        {
            NPC bestTarget = null;
            float bestScore = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                Vector2 toTarget = npc.Center - origin;
                float distance = toTarget.Length();
                if (distance > range)
                    continue;

                float anglePenalty = 1f - MathHelper.Clamp(Vector2.Dot(aimDirection, toTarget.SafeNormalize(aimDirection)), -1f, 1f);
                float score = distance + anglePenalty * 340f;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private static Vector2 PredictTargetPosition(NPC target, Vector2 origin, float speed)
        {
            float distance = Vector2.Distance(origin, target.Center);
            float travelTime = MathHelper.Clamp(distance / Math.Max(speed, 1f), 8f, 42f);
            return target.Center + target.velocity * travelTime * 0.55f;
        }

        private static void SpawnFlameReleaseVFX(Vector2 position, Vector2 velocity, int index)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            Color violet = new(126, 58, 255);
            Color cyan = new(72, 230, 255);
            Color color = Color.Lerp(violet, cyan, index / 6f);

            Dust dust = Dust.NewDustPerfect(
                position,
                Main.rand.NextBool() ? DustID.Shadowflame : DustID.BlueTorch,
                -direction * Main.rand.NextFloat(0.6f, 1.8f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                100,
                color,
                Main.rand.NextFloat(0.8f, 1.25f));
            dust.noGravity = true;

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    position - direction * 4f,
                    -direction * Main.rand.NextFloat(1.2f, 3f),
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.48f, 0.78f),
                    color));
            }
        }

        private void ApplyScreenShake(float power)
        {
            float distanceFactor = Utils.GetLerpValue(1400f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = System.Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
            SpawnExecutionSlashes(target);
        }

        public override void PostDraw(Color lightColor)
        {
            if (Main.dedServ)
                return;

            Player owner = Main.player[Projectile.owner];
            Vector2 bladeDirection = GetBladeDirection(owner);
            Vector2 drawPosition = GetBladeTip(owner) - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Asset<Texture2D> bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Asset<Texture2D> streak = ModContent.Request<Texture2D>("CalamityMod/Particles/FadeStreak");
            float pulse = 0.72f + 0.18f * MathF.Sin(Main.GlobalTimeWrappedHourly * 12f + Projectile.identity);
            Color color = Color.Lerp(new Color(115, 65, 255), new Color(60, 230, 255), 0.25f);
            color.A = 0;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                bloom.Value,
                drawPosition,
                null,
                color * 0.42f,
                0f,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.34f * pulse,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                streak.Value,
                drawPosition,
                null,
                Color.White with { A = 0 } * 0.34f,
                bladeDirection.ToRotation(),
                new Vector2(streak.Width() * 0.5f, streak.Height() * 0.5f),
                new Vector2(0.74f, 0.18f) * Projectile.scale,
                SpriteEffects.None,
                0f);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }
    }
}
