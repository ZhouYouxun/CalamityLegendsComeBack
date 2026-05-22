using System;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
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
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/AT4";

        private int TargetIndex => (int)Projectile.ai[0];
        private Vector2 StoredImpactPosition => new(Projectile.ai[1], Projectile.ai[2]);
        private Vector2 impactPosition;
        private Vector2 lockedDashDirection;
        private int timer;
        private bool dashing;
        private bool exploding;

        public override void SetDefaults()
        {
            Projectile.width = 150;
            Projectile.height = 150;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 210;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.scale = 1.35f;
        }

        public override bool? CanDamage() => dashing || exploding;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (exploding)
                return CalamityUtils.CircularHitboxCollision(Projectile.Center, 170f * Projectile.scale, targetHitbox);

            float collisionPoint = float.NaN;
            Vector2 bladeDirection = dashing
                ? Projectile.velocity.SafeNormalize(Projectile.rotation.ToRotationVector2())
                : Projectile.rotation.ToRotationVector2();
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
                Projectile.velocity.Y += 0.48f;
                Projectile.velocity.X *= 0.992f;
                Projectile.rotation += MathHelper.TwoPi * 2f / 34f * Math.Sign(Projectile.velocity.X == 0f ? Main.player[Projectile.owner].direction : Projectile.velocity.X);
                Projectile.scale = MathHelper.Lerp(Projectile.scale, 1.75f, 0.045f);
                SpawnChargeVisuals();

                if (timer >= 34)
                    BeginLockedDash();

                return;
            }

            if (dashing)
            {
                float dashCompletion = Utils.GetLerpValue(0f, 24f, timer, true);
                float dashSpeed = MathHelper.Lerp(18f, 52f, dashCompletion * dashCompletion);
                Projectile.velocity = lockedDashDirection * dashSpeed;
                Projectile.rotation = Projectile.rotation.AngleLerp(lockedDashDirection.ToRotation() + MathHelper.PiOver4, 0.3f);
                SpawnFallingVisuals();

                if (Projectile.Distance(impactPosition) < 46f)
                    BeginExplosion(impactPosition);
                else if (timer >= 54)
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

        private void BeginLockedDash()
        {
            dashing = true;
            timer = 0;
            impactPosition = ResolveImpactPosition();
            lockedDashDirection = (impactPosition - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY));
            Projectile.velocity = lockedDashDirection * 18f;
            Projectile.rotation = lockedDashDirection.ToRotation() + MathHelper.PiOver4;
            Projectile.friendly = true;
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.82f, Pitch = -0.15f }, Projectile.Center);
        }

        private void BeginExplosion(Vector2 explosionCenter)
        {
            dashing = false;
            exploding = true;
            timer = 0;
            Projectile.Center = explosionCenter;
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = true;
            Projectile.localNPCHitCooldown = 8;

            if (Main.myPlayer == Projectile.owner)
            {
                NPC target = AzureThunderPlayer.FindNearestTarget(Projectile.Center, 700f);
                AzureThunderPlayer.SpawnVerticalLightning(
                    Projectile.GetSource_FromThis(),
                    target?.Center ?? Projectile.Center,
                    target,
                    Math.Max(1, (int)(Projectile.damage * 0.55f)),
                    Projectile.knockBack,
                    Projectile.owner,
                    gainCharge: true,
                    applyStaticDischarge: true,
                    big: true);
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.72f, Pitch = 0.08f }, Projectile.Center);
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
            AzureThunderPlayer.ApplyUltimateDot(target, 240);
            Main.player[Projectile.owner].GetModPlayer<AzureThunderPlayer>().TryGainThunderChargeFromTarget(target);

            if (dashing && !exploding)
                BeginExplosion(target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

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
