using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    public class LivingShard_Healing : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int timer;
        private int retargetTimer;
        private int targetPlayerIndex = -1;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 420;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => Projectile.ai[0] == 0f ? null : false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            AcquireTargetPlayer();

            if (Projectile.velocity.Length() > 0.1f)
            {
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * (Projectile.velocity.Length() * 0.5f);
                return;
            }

            Player owner = Main.player[Projectile.owner];
            Vector2 direction = owner.active && !owner.dead
                ? (owner.Center - Projectile.Center).SafeNormalize(Vector2.UnitY)
                : -Vector2.UnitY;
            Projectile.velocity = direction * 4.5f;
        }

        public override void AI()
        {
            timer++;
            retargetTimer++;

            if (Projectile.ai[0] == 0f && timer > 18)
            {
                Projectile.ai[0] = 1f;
                Projectile.friendly = false;
                Projectile.netUpdate = true;
            }

            if (!IsValidTarget(targetPlayerIndex) || retargetTimer >= 12)
                AcquireTargetPlayer();

            Player target = Main.player[targetPlayerIndex];
            Vector2 predictedCenter = target.Center + target.velocity * 8f;
            Vector2 toTarget = predictedCenter - Projectile.Center;
            float distance = toTarget.Length();
            Vector2 desiredDirection = toTarget.SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY));
            float windup = Utils.GetLerpValue(0f, 40f, timer, true);
            float desiredSpeed = MathHelper.Lerp(9f, 26f, Utils.GetLerpValue(720f, 80f, distance, true));
            desiredSpeed *= MathHelper.Lerp(0.55f, 1f, windup);
            float turnRate = MathHelper.Lerp(0.08f, 0.34f, Utils.GetLerpValue(520f, 90f, distance, true));

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredDirection * desiredSpeed, turnRate);
            if (Projectile.velocity.Length() > 30f)
                Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * 30f;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Color(115, 255, 150).ToVector3() * 0.75f);

            if (distance < 28f || Projectile.Hitbox.Intersects(target.Hitbox))
                Projectile.Kill();

            SpawnFlightEffects();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.ai[0] = 1f;
            Projectile.friendly = false;
            Projectile.velocity = (Main.player[Projectile.owner].Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY)) * 16f;
            Projectile.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = bloom.Size() * 0.5f;
            float pulse = 0.82f + (float)System.Math.Sin(timer * 0.18f) * 0.12f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                new Color(90, 255, 130) * 0.36f,
                0f,
                origin,
                0.42f * pulse,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                Color.White * 0.16f,
                0f,
                origin,
                0.18f * pulse,
                SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            int healAmount = 12;
            Player owner = Main.player[Projectile.owner];
            Player healTarget = owner;

            if (targetPlayerIndex >= 0 && targetPlayerIndex < Main.maxPlayers)
            {
                Player candidate = Main.player[targetPlayerIndex];
                if (candidate.active && !candidate.dead)
                    healTarget = candidate;
            }

            healTarget.statLife = System.Math.Min(healTarget.statLifeMax2, healTarget.statLife + healAmount);
            healTarget.HealEffect(healAmount);

            Vector2 center = Projectile.Center;

            for (int i = 0; i < 6; i++)
            {
                Vector2 dir = (MathHelper.TwoPi * i / 6f).ToRotationVector2();
                SquishyLightParticle particle = new(
                    center,
                    dir * Main.rand.NextFloat(1.4f, 3.2f),
                    0.65f,
                    Color.Lerp(Color.LimeGreen, Color.White, 0.3f),
                    14
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            for (int i = 0; i < 12; i++)
            {
                float t = MathHelper.TwoPi * i / 12f;
                Vector2 ellipse = new Vector2((float)System.Math.Cos(t) * 42f, (float)System.Math.Sin(t) * 18f);
                Dust dust = Dust.NewDustPerfect(
                    center + ellipse,
                    Main.rand.NextBool() ? 107 : 110,
                    ellipse.SafeNormalize(Vector2.UnitY) * 1.6f,
                    120,
                    Main.rand.NextBool() ? Color.LightGreen : Color.LimeGreen,
                    Main.rand.NextFloat(0.75f, 1.15f)
                );
                dust.noGravity = true;
            }
        }

        private void SpawnFlightEffects()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);

            if (Projectile.numUpdates == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 spawnPosition = Projectile.Center - forward * Main.rand.NextFloat(4f, 14f) + side * Main.rand.NextFloat(-7f, 7f);
                    Vector2 velocity = -forward.RotatedByRandom(0.42f) * Main.rand.NextFloat(0.7f, 2.8f) + side * Main.rand.NextFloat(-0.35f, 0.35f);

                    SquishyLightParticle particle = new(
                        spawnPosition,
                        velocity,
                        Main.rand.NextFloat(0.75f, 1.25f),
                        Color.Lerp(new Color(130, 255, 145), Color.White, Main.rand.NextFloat(0.08f, 0.32f)),
                        Main.rand.Next(16, 26),
                        1f,
                        Main.rand.NextFloat(1.1f, 1.65f));
                    GeneralParticleHandler.SpawnParticle(particle);
                }

                for (int i = 0; i < 2; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                        Main.rand.NextBool() ? 107 : DustID.GreenTorch,
                        -forward * Main.rand.NextFloat(0.5f, 2.4f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                        80,
                        Main.rand.NextBool() ? Color.LightGreen : new Color(80, 255, 130),
                        Main.rand.NextFloat(1.0f, 1.65f));
                    dust.noGravity = true;
                    dust.fadeIn = Main.rand.NextFloat(0.3f, 0.7f);
                }
            }
            else if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - forward * Main.rand.NextFloat(2f, 8f),
                    DustID.GreenTorch,
                    -forward * Main.rand.NextFloat(0.25f, 1.1f),
                    100,
                    new Color(100, 255, 130),
                    Main.rand.NextFloat(0.75f, 1.1f));
                dust.noGravity = true;
            }
        }

        private bool IsValidTarget(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
                return false;

            Player player = Main.player[playerIndex];
            return player.active && !player.dead;
        }

        private void AcquireTargetPlayer()
        {
            retargetTimer = 0;
            float bestScore = float.MaxValue;
            targetPlayerIndex = -1;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                    continue;

                float ratio = (float)player.statLife / player.statLifeMax2;
                float distanceFactor = Projectile.Distance(player.Center) / 2400f;
                float score = ratio + distanceFactor * 0.18f;

                if (score < bestScore)
                {
                    bestScore = score;
                    targetPlayerIndex = i;
                }
            }

            if (targetPlayerIndex == -1)
                targetPlayerIndex = Projectile.owner;
        }
    }
}
