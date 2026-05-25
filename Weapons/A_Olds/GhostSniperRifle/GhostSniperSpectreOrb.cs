using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.GhostSniperRifle
{
    public class GhostSniperSpectreOrb : ModProjectile, ILocalizedModType
    {
        private static readonly Color HealBlue = new(178, 250, 255);
        private static readonly Color DamageBlue = new(118, 220, 255);

        public new string LocalizationCategory => "Projectiles.GhostSniperRifle";
        public override string Texture => "Terraria/Images/Projectile_642";

        private int timer;
        private int retargetTimer;
        private int targetPlayerIndex = -1;
        private int targetNPCIndex = -1;

        private bool HealingMode => targetPlayerIndex >= 0;

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 360;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => HealingMode ? false : null;

        public override bool? CanHitNPC(NPC target)
        {
            if (HealingMode)
                return false;

            if (targetNPCIndex >= 0)
                return target.whoAmI == targetNPCIndex ? null : false;

            return null;
        }

        public override void AI()
        {
            timer++;
            retargetTimer++;

            if (retargetTimer >= 12 || !TargetIsValid())
            {
                AcquireTargets();
                retargetTimer = 0;
            }

            Projectile.friendly = !HealingMode;
            Projectile.rotation += 0.14f * (Projectile.velocity.X >= 0f ? 1f : -1f);
            Lighting.AddLight(Projectile.Center, (HealingMode ? HealBlue : DamageBlue).ToVector3() * 0.7f);

            if (HealingMode)
                HealingAI();
            else
                DamageAI();

            SpawnTrail();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnBurst(Projectile.Center, DamageBlue);
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.45f, Pitch = 0.3f }, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            SpawnBurst(Projectile.Center, HealingMode ? HealBlue : DamageBlue);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color theme = HealingMode ? HealBlue : DamageBlue;
            float pulse = 0.86f + (float)System.Math.Sin(timer * 0.18f) * 0.12f;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, theme with { A = 0 } * 0.36f, 0f, bloom.Size() * 0.5f, 0.34f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, theme with { A = 0 } * 0.82f, Projectile.rotation, texture.Size() * 0.5f, 0.42f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White with { A = 0 } * 0.55f, -Projectile.rotation, texture.Size() * 0.5f, 0.22f * pulse, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }

        private void HealingAI()
        {
            Player target = Main.player[targetPlayerIndex];
            Vector2 predictedCenter = target.Center + target.velocity * 8f;
            Vector2 toTarget = predictedCenter - Projectile.Center;
            float distance = toTarget.Length();
            Vector2 desired = toTarget.SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY));
            float speed = MathHelper.Lerp(8f, 24f, Utils.GetLerpValue(520f, 80f, distance, true));
            float turnRate = MathHelper.Lerp(0.075f, 0.3f, Utils.GetLerpValue(500f, 90f, distance, true));

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired * speed, turnRate);
            Projectile.velocity = Projectile.velocity.SafeNormalize(desired) * MathHelper.Clamp(Projectile.velocity.Length(), 4f, 28f);

            if (distance < 30f || Projectile.Hitbox.Intersects(target.Hitbox))
            {
                HealPlayer(target, 12);
                Projectile.Kill();
            }
        }

        private void DamageAI()
        {
            NPC target = targetNPCIndex >= 0 && Main.npc.IndexInRange(targetNPCIndex) ? Main.npc[targetNPCIndex] : null;
            if (target != null && target.active && target.CanBeChasedBy(Projectile))
            {
                Vector2 predictedCenter = target.Center + target.velocity * 12f;
                Vector2 desired = (predictedCenter - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY));
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired * 18f, 0.12f);
                Projectile.velocity = Projectile.velocity.SafeNormalize(desired) * MathHelper.Clamp(Projectile.velocity.Length(), 6f, 20f);
            }
            else
            {
                Projectile.velocity *= 0.99f;
            }
        }

        private void AcquireTargets()
        {
            targetPlayerIndex = FindInjuredPlayer();
            if (targetPlayerIndex >= 0)
            {
                targetNPCIndex = -1;
                Projectile.netUpdate = true;
                return;
            }

            targetNPCIndex = FindEnemy();
            Projectile.netUpdate = true;
        }

        private bool TargetIsValid()
        {
            if (HealingMode)
                return IsValidHealingTarget(targetPlayerIndex);

            if (targetNPCIndex < 0 || !Main.npc.IndexInRange(targetNPCIndex))
                return false;

            NPC target = Main.npc[targetNPCIndex];
            return target.active && target.CanBeChasedBy(Projectile);
        }

        private static int FindInjuredPlayer()
        {
            int best = -1;
            float bestRatio = 1f;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (!IsValidHealingTarget(i))
                    continue;

                Player player = Main.player[i];
                float ratio = player.statLife / (float)player.statLifeMax2;
                if (ratio >= bestRatio)
                    continue;

                best = i;
                bestRatio = ratio;
            }

            return best;
        }

        private int FindEnemy()
        {
            NPC best = null;
            float bestScore = 1450f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float score = Projectile.Distance(npc.Center);
                if (npc.boss)
                    score *= 0.72f;

                if (score >= bestScore)
                    continue;

                best = npc;
                bestScore = score;
            }

            return best?.whoAmI ?? -1;
        }

        private static bool IsValidHealingTarget(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
                return false;

            Player player = Main.player[playerIndex];
            return player.active && !player.dead && player.statLife < player.statLifeMax2;
        }

        private static void HealPlayer(Player player, int healAmount)
        {
            player.statLife = System.Math.Min(player.statLifeMax2, player.statLife + healAmount);
            player.HealEffect(healAmount, true);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.55f, Pitch = 0.25f }, player.Center);
        }

        private void SpawnTrail()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Color theme = HealingMode ? HealBlue : DamageBlue;

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - forward * Main.rand.NextFloat(4f, 18f) + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextBool(3) ? DustID.SpectreStaff : DustID.GemDiamond,
                    -forward * Main.rand.NextFloat(0.4f, 1.6f),
                    100,
                    Main.rand.NextBool(3) ? Color.White : theme,
                    Main.rand.NextFloat(0.55f, 0.95f));
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    -forward * Main.rand.NextFloat(0.2f, 0.9f),
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.2f, 0.34f),
                    theme,
                    true,
                    false,
                    true));
            }
        }

        private static void SpawnBurst(Vector2 center, Color theme)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool(3) ? DustID.SpectreStaff : DustID.GemDiamond,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 4.4f),
                    80,
                    Main.rand.NextBool(3) ? Color.White : theme,
                    Main.rand.NextFloat(0.7f, 1.2f));
                dust.noGravity = true;
            }
        }
    }
}
