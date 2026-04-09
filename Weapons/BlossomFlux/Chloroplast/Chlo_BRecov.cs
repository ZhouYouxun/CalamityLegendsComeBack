using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast
{
    // 复苏叶绿体：造成命中后会折返，为玩家带回治疗。
    internal sealed class Chlo_BRecov : BlossomFluxChloroplastPreset
    {
        public static Chlo_BRecov Instance { get; } = new();

        private Chlo_BRecov()
        {
        }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            projectile.penetrate = -1;
            projectile.timeLeft = 150;
            projectile.localNPCHitCooldown = -1;
            projectile.localAI[0] = 0f;
            projectile.localAI[1] = 3f;
        }

        public override void AI(Projectile projectile)
        {
            Lighting.AddLight(projectile.Center, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov).ToVector3() * 0.34f);

            if (projectile.localAI[0] == 0f)
            {
                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustPerfect(
                        projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                        DustID.GemEmerald,
                        -projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                        100,
                        ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov),
                        Main.rand.NextFloat(0.85f, 1.15f));
                    dust.noGravity = true;
                }

                if (projectile.ai[1] >= 20f || projectile.timeLeft < 85)
                    BeginReturn(projectile);

                return;
            }

            Player owner = Main.player[projectile.owner];
            if (!owner.active || owner.dead)
            {
                projectile.Kill();
                return;
            }

            projectile.friendly = false;
            projectile.tileCollide = false;

            Vector2 desiredVelocity = (owner.Center - projectile.Center).SafeNormalize(Vector2.UnitY) * 13.5f;
            projectile.velocity = Vector2.Lerp(projectile.velocity, desiredVelocity, 0.12f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.GreenTorch,
                    Main.rand.NextVector2Circular(0.45f, 0.45f),
                    100,
                    ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov),
                    Main.rand.NextFloat(0.9f, 1.2f));
                dust.noGravity = true;
            }

            if (projectile.Hitbox.Intersects(owner.Hitbox))
            {
                int healAmount = Utils.Clamp((int)projectile.localAI[1], 3, 10);
                owner.statLife += healAmount;
                if (owner.statLife > owner.statLifeMax2)
                    owner.statLife = owner.statLifeMax2;

                owner.HealEffect(healAmount, true);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f, Pitch = 0.2f }, owner.Center);
                projectile.Kill();
            }
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            projectile.localAI[1] = Utils.Clamp(projectile.localAI[1] + 2f + damageDone / 60f, 3f, 10f);
            ChloroplastCommon.SimpleBurst(projectile, DustID.GemEmerald, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov), 8, 1f, 3.4f);
            BeginReturn(projectile);
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            ChloroplastCommon.SimpleBurst(projectile, DustID.GreenTorch, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov), 12, 1f, 3.4f);
        }

        public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
        {
            BeginReturn(projectile);
            return false;
        }

        public override bool? CanDamage(Projectile projectile)
        {
            return projectile.localAI[0] == 0f ? null : false;
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            ChloroplastCommon.DrawGlow(projectile, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov), 1.04f, 0.3f);
            return true;
        }

        private static void BeginReturn(Projectile projectile)
        {
            if (projectile.localAI[0] != 0f)
                return;

            projectile.localAI[0] = 1f;
            projectile.friendly = false;
            projectile.tileCollide = false;
            projectile.timeLeft = Utils.Clamp(projectile.timeLeft, 90, 150);
            projectile.netUpdate = true;
        }
    }
}
