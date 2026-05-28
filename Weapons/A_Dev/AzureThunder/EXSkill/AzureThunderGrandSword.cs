using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    internal sealed class AzureThunderGrandSword : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/AzureThunder";

        private int TargetIndex => (int)Projectile.ai[0];
        private Vector2 StoredImpactPosition => new(Projectile.ai[1], Projectile.ai[2]);
        private Vector2 impactPosition;
        private int timer;
        private bool dashing;
        private bool exploding;
        private const int DropAnticipationFrames = 16;
        private const float InitialDropSpeed = 92f;
        private const float DropAcceleration = 12.5f;
        private const float MaxDropSpeed = 176f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 128;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 210;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.scale = 1.65f;
        }

        public override bool? CanDamage() => dashing || exploding;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (exploding)
                return CalamityUtils.CircularHitboxCollision(Projectile.Center, 170f * Projectile.scale, targetHitbox);

            float collisionPoint = float.NaN;
            Vector2 bladeDirection = Vector2.UnitY;
            Vector2 start = Projectile.Center - bladeDirection * 42f * Projectile.scale;
            Vector2 end = Projectile.Center + bladeDirection * 145f * Projectile.scale;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 42f * Projectile.scale, ref collisionPoint);
        }

        public override void AI()
        {
            timer++;

            if (impactPosition == Vector2.Zero)
                impactPosition = ResolveImpactPosition();

            if (!dashing && !exploding)
            {
                impactPosition = ResolveImpactPosition();
                Vector2 hoverPosition = impactPosition - Vector2.UnitY * 780f;
                Projectile.Center = Vector2.Lerp(Projectile.Center, hoverPosition, 0.28f);
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4;
                Projectile.scale = MathHelper.Lerp(Projectile.scale, 1.75f, 0.045f);
                SpawnChargeVisuals();

                if (timer >= DropAnticipationFrames)
                    BeginDrop();

                return;
            }

            if (dashing)
            {
                // Heavy drop tuning: start fast, then gain 12.5 px/frame so it reads as a sudden execution stroke.
                impactPosition = ResolveImpactPosition();
                float lateralCorrection = (impactPosition.X - Projectile.Center.X) * 0.08f;
                float trailSway = (float)Math.Sin(timer * 0.55f + Projectile.identity) * 2.2f;
                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, lateralCorrection + trailSway, 0.25f);
                Projectile.velocity.Y = Math.Min(MaxDropSpeed, Projectile.velocity.Y + DropAcceleration);
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
                SpawnFallingVisuals();

                if (Projectile.Distance(impactPosition) < 46f)
                    BeginExplosion(impactPosition);
                else if (timer >= 34)
                    BeginExplosion(Projectile.Center);

                return;
            }

            if (exploding)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.scale = MathHelper.Lerp(Projectile.scale, 2.35f, 0.18f);
                Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 0f, 0.16f);
                SpawnExplosionVisuals();
                if (timer > 18)
                    Projectile.Kill();
            }
        }

        private Vector2 ResolveImpactPosition()
        {
            if (TargetIndex >= 0 && Main.npc.IndexInRange(TargetIndex))
            {
                NPC target = Main.npc[TargetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                    return target.Center;
            }

            return StoredImpactPosition == Vector2.Zero ? Projectile.Center + Vector2.UnitY * 560f : StoredImpactPosition;
        }

        private void BeginDrop()
        {
            dashing = true;
            timer = 0;
            impactPosition = ResolveImpactPosition();
            Projectile.Center = new Vector2(impactPosition.X, Projectile.Center.Y);
            Projectile.velocity = Vector2.UnitY * InitialDropSpeed;
            Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4;
            Projectile.friendly = true;
            AzureThunderSounds.PlayHeavyDrop(Projectile.Center);
        }

        private void BeginExplosion(Vector2 explosionCenter)
        {
            dashing = false;
            exploding = true;
            timer = 0;
            Projectile.Center = explosionCenter;
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = true;
            Projectile.localNPCHitCooldown = 5;

            if (Main.myPlayer == Projectile.owner)
            {
                int flags = AzureThunderFlatLightning.StaticDischargeFlag | AzureThunderFlatLightning.BigLightningFlag;
                for (int i = 0; i < 10; i++)
                {
                    Vector2 direction = (MathHelper.TwoPi * i / 10f).ToRotationVector2();
                    Vector2 spawnPosition = Projectile.Center - direction * Main.rand.NextFloat(90f, 180f);
                    AzureThunderPlayer.SpawnFlatLightning(
                        Projectile.GetSource_FromThis(),
                        spawnPosition,
                        Projectile.Center - spawnPosition,
                        Math.Max(1, (int)(Projectile.damage * 0.28f)),
                        Projectile.knockBack,
                        Projectile.owner,
                        i % 3 == 0 ? 1.35f : 0.95f,
                        flags);
                }
            }

            AzureThunderSounds.PlayHeavyImpact(Projectile.Center);
        }

        private void SpawnChargeVisuals()
        {
            if (!Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(52f, 52f),
                DustID.FireworksRGB,
                -Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1f, 4f) + Main.rand.NextVector2Circular(1.5f, 1.5f),
                0,
                Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                Main.rand.NextFloat(0.9f, 1.4f));
            dust.noGravity = true;
        }

        private void SpawnFallingVisuals()
        {
            Vector2 upwardTrail = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * Main.rand.NextFloat(0.3f, 0.75f) + Main.rand.NextVector2Circular(42f, 42f),
                    DustID.FireworksRGB,
                    -Projectile.velocity * Main.rand.NextFloat(0.02f, 0.08f),
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.Yellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(1f, 1.6f));
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(38f, 38f),
                    upwardTrail * Main.rand.NextFloat(4f, 9f) + Main.rand.NextVector2Circular(1.2f, 1.2f),
                    false,
                    Main.rand.Next(14, 21),
                    Main.rand.NextFloat(0.045f, 0.075f),
                    Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    new Vector2(2.2f, 0.46f),
                    true,
                    true,
                    0.9f));
            }

            GeneralParticleHandler.SpawnParticle(new LineParticle(
                Projectile.Center - Projectile.velocity * Main.rand.NextFloat(0.12f, 0.35f),
                upwardTrail * Main.rand.NextFloat(2.5f, 5.5f),
                false,
                Main.rand.Next(12, 18),
                Main.rand.NextFloat(0.55f, 0.9f),
                Main.rand.NextBool(3) ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure));
        }

        private void SpawnExplosionVisuals()
        {
            if (!Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(120f, 120f),
                DustID.FireworksRGB,
                Main.rand.NextVector2Circular(7f, 7f),
                0,
                Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                Main.rand.NextFloat(1.1f, 1.8f));
            dust.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 240);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
            AzureThunderPlayer.ApplyUltimateDot(target, 240);

            if (dashing && !exploding && Projectile.numHits >= 5)
                BeginExplosion(target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                if (oldCenter == Projectile.Size * 0.5f)
                    continue;

                float opacity = (1f - i / (float)Projectile.oldPos.Length) * (dashing ? 0.34f : 0.12f);
                Color trailColor = Color.Lerp(AzureThunderColors.Azure, AzureThunderColors.PaleYellow, i / (float)Projectile.oldPos.Length) with { A = 0 };
                Main.EntitySpriteDraw(texture, oldCenter - Main.screenPosition, null, trailColor * opacity, Projectile.rotation, origin, Projectile.scale * (1f - i * 0.018f), SpriteEffects.None);
            }

            HoldoutOutlineHelper.DrawSolidOutline(
                texture,
                drawPosition,
                Projectile.rotation,
                origin,
                Vector2.One * Projectile.scale,
                SpriteEffects.None,
                AzureThunderColors.Yellow,
                exploding ? 12f : 7f,
                exploding ? 0.28f : 0.36f,
                Main.GlobalTimeWrappedHourly + Projectile.identity * 0.15f,
                exploding ? 26 : 18);

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
