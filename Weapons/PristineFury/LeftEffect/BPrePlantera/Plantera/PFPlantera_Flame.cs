using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFPlantera_PseudoLaser : ModProjectile, ILocalizedModType
    {
        private const float BeamLength = 920f;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.timeLeft = 7;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center;
            Vector2 end = start + direction * BeamLength;
            Vector2 edge = end - start;
            float fade = Utils.GetLerpValue(0f, 3f, Projectile.timeLeft, true);

            Main.EntitySpriteDraw(
                pixel,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                new Color(100, 255, 110, 0) * 0.18f * fade,
                edge.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(edge.Length(), 3f),
                SpriteEffects.None,
                0);
            return false;
        }
    }

    internal sealed class PFPlantera_Flame : ModProjectile, ILocalizedModType
    {
        private static readonly Color LightningGreen = new(72, 255, 92);
        private ref float Timer => ref Projectile.localAI[0];

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 56;
            Projectile.extraUpdates = 5;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            Timer++;
            Projectile.velocity = Projectile.velocity.RotatedByRandom(0.003f);
            ApplySubtleTracking();
            Lighting.AddLight(Projectile.Center, LightningGreen.ToVector3() * 0.42f);

            if (!Main.dedServ && Timer % 2f == 0f)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center,
                    Projectile.velocity * 0.08f,
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    16,
                    Main.rand.NextFloat(0.16f, 0.24f),
                    Color.Lerp(LightningGreen, Color.White, Main.rand.NextFloat(0.12f, 0.45f)),
                    new Vector2(0.8f, 1f),
                    shrinkSpeed: 0.35f));
            }

            if (Timer == 11f && Projectile.ai[0] < 4f && Projectile.owner == Main.myPlayer)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    float branchAngle = MathHelper.Lerp(0.26f, 0.1f, Projectile.ai[0] / 4f);
                    float spiralBias = (Projectile.identity % 2 == 0 ? 1f : -1f) * 0.025f;
                    Vector2 velocity = Projectile.velocity.RotatedBy(i * branchAngle + spiralBias) * 0.92f;
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        velocity,
                        Type,
                        Math.Max(1, (int)(Projectile.damage * 0.78f)),
                        Projectile.knockBack * 0.7f,
                        Projectile.owner,
                        Projectile.ai[0] + 1f);
                }
            }
        }

        private void ApplySubtleTracking()
        {
            if (Timer < 5f)
                return;

            NPC target = FindNearestTarget(780f);
            if (target is null)
                return;

            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentDirection);
            float turn = MathHelper.Lerp(MathHelper.ToRadians(1.5f), MathHelper.ToRadians(5f), Utils.GetLerpValue(5f, 28f, Timer, true));
            Vector2 newDirection = currentDirection.ToRotation().AngleTowards(desiredDirection.ToRotation(), turn).ToRotationVector2();
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, newDirection * Projectile.velocity.Length(), 0.085f);
        }

        private NPC FindNearestTarget(float range)
        {
            NPC closest = null;
            float bestDistance = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityMod.CalamityUtils.CircularHitboxCollision(Projectile.Center, 18f, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(BuffID.OnFire3, 180);

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
