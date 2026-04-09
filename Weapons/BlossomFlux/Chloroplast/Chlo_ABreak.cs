using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast
{
    // 突破叶绿体：强调穿刺与连续换目标追猎。
    internal sealed class Chlo_ABreak : BlossomFluxChloroplastPreset
    {
        public static Chlo_ABreak Instance { get; } = new();

        private Chlo_ABreak()
        {
        }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            projectile.penetrate = 5;
            projectile.timeLeft = 96;
            projectile.localNPCHitCooldown = 6;
            projectile.velocity *= 1.2f;
        }

        public override void AI(Projectile projectile)
        {
            Lighting.AddLight(projectile.Center, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak).ToVector3() * 0.36f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.Grass,
                    -projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    100,
                    ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak),
                    Main.rand.NextFloat(0.8f, 1.1f));
                dust.noGravity = true;
            }

            if (projectile.ai[1] < 10f)
                return;

            NPC target = projectile.Center.ClosestNPCAt(720f);
            if (target is null)
                return;

            Vector2 desiredVelocity = (target.Center - projectile.Center).SafeNormalize(projectile.velocity.SafeNormalize(Vector2.UnitY)) * 18f;
            projectile.velocity = (projectile.velocity * 14f + desiredVelocity) / 15f;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            ChloroplastCommon.SimpleBurst(projectile, DustID.GrassBlades, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak), 8, 1.1f, 3.8f);
            SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.32f, Pitch = -0.1f }, target.Center);

            NPC nextTarget = projectile.Center.ClosestNPCAt(840f);
            if (nextTarget != null && nextTarget.whoAmI != target.whoAmI)
            {
                float speed = System.Math.Max(projectile.velocity.Length(), 16f);
                projectile.velocity = (nextTarget.Center - projectile.Center).SafeNormalize(Vector2.UnitY) * speed;
                projectile.netUpdate = true;
            }
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            ChloroplastCommon.SimpleBurst(projectile, DustID.Grass, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak), 14, 1.2f, 4.6f);
        }

        public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
        {
            return true;
        }

        public override bool? CanDamage(Projectile projectile)
        {
            return null;
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            ChloroplastCommon.DrawGlow(projectile, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak), 1.05f, 0.28f);
            return true;
        }
    }
}
