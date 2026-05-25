using System;
using CalamityLegendsComeBack.Accssory.TS;
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
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/AzureThunder";

        private int TargetIndex => (int)Projectile.ai[0];
        private Vector2 StoredImpactPosition => new(Projectile.ai[1], Projectile.ai[2]);
        private Vector2 impactPosition;
        private int timer;
        private bool dashing;
        private bool exploding;

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
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4;
                Projectile.scale = MathHelper.Lerp(Projectile.scale, 1.75f, 0.045f);
                SpawnChargeVisuals();

                if (timer >= 24)
                    BeginDrop();

                return;
            }

            if (dashing)
            {
                float dashCompletion = Utils.GetLerpValue(0f, 22f, timer, true);
                Projectile.velocity = Vector2.UnitY * MathHelper.Lerp(30.6f, 98.6f, dashCompletion * dashCompletion);
                Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4;
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

        private void BeginDrop()
        {
            dashing = true;
            timer = 0;
            impactPosition = ResolveImpactPosition();
            Projectile.Center = new Vector2(impactPosition.X, Projectile.Center.Y);
            Projectile.velocity = Vector2.UnitY * 30.6f;
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
                int flags = AzureThunderFlatLightning.GainChargeFlag | AzureThunderFlatLightning.StaticDischargeFlag | AzureThunderFlatLightning.BigLightningFlag;
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
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
            AzureThunderPlayer.ApplyUltimateDot(target, 240);
            Main.player[Projectile.owner].GetModPlayer<AzureThunderPlayer>().TryGainThunderChargeFromTarget(target);

            if (dashing && !exploding && Projectile.numHits >= 5)
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
