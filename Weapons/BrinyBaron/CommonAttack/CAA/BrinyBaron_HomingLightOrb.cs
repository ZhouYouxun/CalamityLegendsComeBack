using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BrinyBaron_HomingLightOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const float HomingRange = 920f;
        private const float MaxSpeed = 16.5f;
        private int timer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 95;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override void AI()
        {
            timer++;
            Projectile.rotation += Projectile.velocity.X * 0.012f + 0.04f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.34f, 0.55f));
            HomeTowardTarget();
            SpawnLightTrail();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Burst();
        }

        public override void OnKill(int timeLeft)
        {
            Burst();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color theme = Color.Lerp(new Color(90, 205, 255), Color.White, 0.2f);
            float pulse = 1f + (float)Math.Sin(timer * 0.24f + Projectile.identity * 0.4f) * 0.08f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = Color.Lerp(theme, Color.Transparent, completion) * (0.46f * (1f - completion));
                Main.EntitySpriteDraw(bloom, trailPosition, null, trailColor * 0.34f, Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * MathHelper.Lerp(0.08f, 0.22f, 1f - completion), SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, drawPosition, null, new Color(60, 185, 255, 0) * 0.42f, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.24f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, Color.White with { A = 0 } * 0.2f, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.11f, SpriteEffects.None);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void HomeTowardTarget()
        {
            NPC target = FindNearestTarget(HomingRange);
            if (target == null)
            {
                Projectile.velocity *= 0.992f;
                return;
            }

            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
            Vector2 currentDirection = Projectile.velocity.SafeNormalize(desiredDirection);
            float loosen = Utils.GetLerpValue(0f, 22f, timer, true);
            float closeLoosen = Utils.GetLerpValue(280f, 52f, Projectile.Distance(target.Center), true);
            float turnRate = MathHelper.Lerp(0.18f, 0.62f, MathHelper.Max(loosen, closeLoosen));
            float targetSpeed = MathHelper.Lerp(9.5f, MaxSpeed, loosen);

            Vector2 steeredDirection = currentDirection.ToRotation().AngleTowards(desiredDirection.ToRotation(), turnRate).ToRotationVector2();
            float speed = MathHelper.Lerp(Projectile.velocity.Length(), targetSpeed, 0.22f + MathHelper.Max(loosen, closeLoosen) * 0.28f);
            Projectile.velocity = steeredDirection * speed;
        }

        private void SpawnLightTrail()
        {
            if (Main.dedServ)
                return;

            Color theme = Color.Lerp(new Color(80, 185, 255), Color.White, Main.rand.NextFloat(0.06f, 0.32f));

            if (timer % 2 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GenericBubbleParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    -Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0.2f, 0.72f) + Main.rand.NextVector2Circular(0.28f, 0.28f),
                    Main.rand.NextFloat(0.48f, 0.86f) * Projectile.scale,
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    Main.rand.Next(24, 40)));
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    -Projectile.velocity * Main.rand.NextFloat(0.03f, 0.08f),
                    0,
                    theme,
                    Main.rand.NextFloat(0.55f, 0.86f));
                dust.noGravity = true;
            }
        }

        private void Burst()
        {
            if (Main.dedServ)
                return;

            Color theme = new Color(90, 205, 255);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, theme * 0.72f, Vector2.One, Projectile.rotation, 0.02f, 0.26f, 18));

            for (int i = 0; i < 5; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GenericBubbleParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.8f, 2.4f),
                    Main.rand.NextFloat(0.52f, 0.96f),
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    Main.rand.Next(24, 42)));
            }

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 4.6f),
                    100,
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.1f, 0.6f)),
                    Main.rand.NextFloat(0.62f, 1.04f));
                dust.noGravity = true;
            }
        }

        private NPC FindNearestTarget(float maxDistance)
        {
            NPC closestTarget = null;
            float closestDistance = maxDistance;

            foreach (NPC npc in Main.npc)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestTarget = npc;
            }

            return closestTarget;
        }
    }
}
