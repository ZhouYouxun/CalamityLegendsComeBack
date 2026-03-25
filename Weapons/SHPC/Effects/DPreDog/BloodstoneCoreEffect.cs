using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class BloodstoneCoreEffect : DefaultEffect
    {
        public override int EffectID => 29;

        public override int AmmoType => ModContent.ItemType<BloodstoneCore>();

        public override Color ThemeColor => new Color(220, 40, 40);
        public override Color StartColor => new Color(255, 120, 120);
        public override Color EndColor => new Color(90, 0, 0);

        public override float SquishyLightParticleFactor => 1.85f;
        public override float ExplosionPulseFactor => 1.85f;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            BloodstoneCoreEffectData data = projectile.GetGlobalProjectile<BloodstoneCoreEffectData>();
            data.empowered = false;
            data.linkedPlayerIndex = -1;

            Player targetPlayer = FindClosestValidPlayer(projectile.Center, 45f * 16f);
            if (targetPlayer != null)
            {
                int lifeThreshold = (int)(targetPlayer.statLifeMax2 * 0.5f);

                if (targetPlayer.statLife > lifeThreshold && targetPlayer.statLife > 66)
                {
                    targetPlayer.statLife -= 66;
                    CombatText.NewText(targetPlayer.Hitbox, Color.Red, 66, true, false);

                    data.empowered = true;
                    data.linkedPlayerIndex = targetPlayer.whoAmI;

                    for (int i = 0; i < 22; i++)
                    {
                        Vector2 dir = (projectile.Center - targetPlayer.Center).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f);
                        Vector2 velocity = dir * Main.rand.NextFloat(2f, 8f);

                        Dust dust = Dust.NewDustPerfect(
                            targetPlayer.Center + Main.rand.NextVector2Circular(18f, 18f),
                            DustID.Blood,
                            velocity,
                            0,
                            Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat()),
                            Main.rand.NextFloat(1.1f, 1.8f)
                        );
                        dust.noGravity = true;
                    }

                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 velocity = (projectile.Center - targetPlayer.Center).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 4f);

                        SquishyLightParticle particle = new(
                            Vector2.Lerp(targetPlayer.Center, projectile.Center, Main.rand.NextFloat(0.1f, 0.35f)),
                            velocity,
                            Main.rand.NextFloat(0.45f, 0.9f),
                            Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat()),
                            Main.rand.Next(18, 28)
                        );

                        GeneralParticleHandler.SpawnParticle(particle);
                    }
                }
            }
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            BloodstoneCoreEffectData data = projectile.GetGlobalProjectile<BloodstoneCoreEffectData>();

            Color lightColor = data.empowered ? new Color(255, 70, 70) : new Color(170, 35, 35);
            Lighting.AddLight(projectile.Center, lightColor.ToVector3() * (data.empowered ? 0.75f : 0.45f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextBool(2) ? DustID.Blood : DustID.RedTorch,
                    -projectile.velocity * Main.rand.NextFloat(0.18f, 0.5f),
                    0,
                    Color.Lerp(ThemeColor, StartColor, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1f, data.empowered ? 1.8f : 1.35f)
                );
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                SquishyLightParticle particle = new(
                    projectile.Center,
                    -projectile.velocity.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.04f, 0.18f),
                    Main.rand.NextFloat(0.4f, data.empowered ? 0.95f : 0.7f),
                    Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat()),
                    Main.rand.Next(12, 20)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            if (data.empowered && data.linkedPlayerIndex >= 0 && data.linkedPlayerIndex < Main.maxPlayers)
            {
                Player linkedPlayer = Main.player[data.linkedPlayerIndex];
                if (linkedPlayer.active && !linkedPlayer.dead)
                {
                    CreateBloodLink(projectile.Center, linkedPlayer.Center);
                }
            }
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            BloodstoneCoreEffectData data = projectile.GetGlobalProjectile<BloodstoneCoreEffectData>();

            if (data.empowered)
                modifiers.SourceDamage *= 1.2f;
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            BloodstoneCoreEffectData data = projectile.GetGlobalProjectile<BloodstoneCoreEffectData>();

            int count = data.empowered ? 18 : 10;
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, data.empowered ? 6f : 3.5f);

                Dust dust = Dust.NewDustPerfect(
                    target.Center,
                    Main.rand.NextBool(2) ? DustID.Blood : DustID.RedTorch,
                    velocity,
                    0,
                    Color.Lerp(ThemeColor, StartColor, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1f, data.empowered ? 1.8f : 1.25f)
                );
                dust.noGravity = true;
            }
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            BloodstoneCoreEffectData data = projectile.GetGlobalProjectile<BloodstoneCoreEffectData>();

            int explosionDamage = data.empowered ? (int)(projectile.damage * 1.6f) : (int)(projectile.damage * 0.95f);

            int projIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                explosionDamage,
                projectile.knockBack,
                projectile.owner
            );

            Projectile proj = Main.projectile[projIndex];
            proj.width = data.empowered ? 250 : 75;
            proj.height = data.empowered ? 250 : 75;

            int dustCount = data.empowered ? 42 : 20;
            float speedMin = data.empowered ? 2f : 1f;
            float speedMax = data.empowered ? 8f : 4f;

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(speedMin, speedMax);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    Main.rand.NextBool(2) ? DustID.Blood : DustID.RedTorch,
                    velocity,
                    0,
                    Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.1f, data.empowered ? 2.2f : 1.5f)
                );
                dust.noGravity = true;
            }

            int smokeCount = data.empowered ? 10 : 4;
            for (int i = 0; i < smokeCount; i++)
            {
                Particle smoke = new HeavySmokeParticle(
                    projectile.Center,
                    Main.rand.NextVector2Circular(2.4f, 2.4f),
                    Color.Lerp(new Color(80, 0, 0), ThemeColor, Main.rand.NextFloat()),
                    Main.rand.Next(22, 36),
                    Main.rand.NextFloat(0.8f, data.empowered ? 1.6f : 1.15f),
                    0.35f,
                    Main.rand.NextFloat(-0.04f, 0.04f),
                    true
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            Particle blastPulse = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                data.empowered ? new Color(255, 50, 50) : new Color(180, 30, 30),
                "CalamityMod/Particles/DetailedExplosion",
                Vector2.One * (data.empowered ? 1.15f : 0.7f),
                Main.rand.NextFloat(-0.3f, 0.3f),
                0.03f,
                data.empowered ? 0.3f : 0.18f,
                data.empowered ? 28 : 18,
                false
            );
            GeneralParticleHandler.SpawnParticle(blastPulse);

            if (data.empowered)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (!player.active || player.dead)
                        continue;

                    int healAmount = 66;
                    player.statLife += healAmount;
                    if (player.statLife > player.statLifeMax2)
                        player.statLife = player.statLifeMax2;

                    player.HealEffect(healAmount);

                    CreateBloodReturn(projectile.Center, player.Center);
                }
            }
        }

        private Player FindClosestValidPlayer(Vector2 center, float maxDistance)
        {
            Player closest = null;
            float closestDistance = maxDistance;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                    continue;

                float distance = Vector2.Distance(center, player.Center);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = player;
                }
            }

            return closest;
        }

        private void CreateBloodLink(Vector2 start, Vector2 end)
        {
            Vector2 toEnd = end - start;
            float distance = toEnd.Length();
            if (distance <= 8f)
                return;

            Vector2 direction = toEnd / distance;
            int steps = (int)(distance / 12f);

            for (int i = 0; i <= steps; i++)
            {
                if (Main.rand.NextBool(3))
                    continue;

                float factor = i / (float)Math.Max(steps, 1);
                Vector2 point = Vector2.Lerp(start, end, factor) + Main.rand.NextVector2Circular(3f, 3f);

                Dust dust = Dust.NewDustPerfect(
                    point,
                    Main.rand.NextBool(2) ? DustID.Blood : DustID.RedTorch,
                    direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 1.1f),
                    0,
                    Color.Lerp(new Color(255, 90, 90), new Color(120, 0, 0), Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.85f, 1.35f)
                );
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                SquishyLightParticle particle = new(
                    Vector2.Lerp(start, end, Main.rand.NextFloat()),
                    direction * Main.rand.NextFloat(0.2f, 1f),
                    Main.rand.NextFloat(0.45f, 0.85f),
                    Color.Lerp(new Color(255, 110, 110), new Color(180, 20, 20), Main.rand.NextFloat()),
                    Main.rand.Next(10, 18)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }
        }

        private void CreateBloodReturn(Vector2 start, Vector2 end)
        {
            Vector2 toEnd = end - start;
            float distance = toEnd.Length();
            if (distance <= 8f)
                return;

            Vector2 direction = toEnd.SafeNormalize(Vector2.UnitY);
            int steps = (int)(distance / 10f);

            for (int i = 0; i <= steps; i++)
            {
                if (Main.rand.NextBool(2))
                    continue;

                float factor = i / (float)Math.Max(steps, 1);
                Vector2 point = Vector2.Lerp(start, end, factor) + Main.rand.NextVector2Circular(4f, 4f);

                Dust dust = Dust.NewDustPerfect(
                    point,
                    Main.rand.NextBool(2) ? DustID.Blood : DustID.LifeDrain,
                    direction * Main.rand.NextFloat(0.3f, 1.4f),
                    0,
                    Color.Lerp(new Color(255, 140, 140), new Color(180, 20, 20), Main.rand.NextFloat()),
                    Main.rand.NextFloat(1f, 1.5f)
                );
                dust.noGravity = true;
            }

            for (int i = 0; i < 6; i++)
            {
                SquishyLightParticle particle = new(
                    Vector2.Lerp(start, end, Main.rand.NextFloat()),
                    direction.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.4f, 2.2f),
                    Main.rand.NextFloat(0.5f, 1f),
                    Color.Lerp(new Color(255, 130, 130), new Color(200, 30, 30), Main.rand.NextFloat()),
                    Main.rand.Next(14, 24)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }
        }
    }

    public class BloodstoneCoreEffectData : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool empowered;
        public int linkedPlayerIndex = -1;
    }
}