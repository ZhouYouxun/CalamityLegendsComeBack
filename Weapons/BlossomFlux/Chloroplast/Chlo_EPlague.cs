using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast
{
    // 散播叶绿体：边飞边喷出瘟疫毒雾，把区域慢慢污染起来。
    internal sealed class Chlo_EPlague : BlossomFluxChloroplastPreset
    {
        public static Chlo_EPlague Instance { get; } = new();

        private Chlo_EPlague()
        {
        }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            projectile.penetrate = 2;
            projectile.timeLeft = 180;
            projectile.localNPCHitCooldown = 18;
        }

        public override void AI(Projectile projectile)
        {
            Lighting.AddLight(projectile.Center, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague).ToVector3() * 0.34f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.GreenTorch,
                    Main.rand.NextVector2Circular(0.45f, 0.45f),
                    100,
                    ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague),
                    Main.rand.NextFloat(0.8f, 1.15f));
                dust.noGravity = true;
            }

            if ((int)projectile.ai[1] % 24 == 0 && projectile.owner == Main.myPlayer)
            {
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.18f, Pitch = -0.35f }, projectile.Center);
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextVector2Circular(1.1f, 1.1f) + new Vector2(0f, -0.25f),
                    ModContent.ProjectileType<BFArrow_EPlagueGas>(),
                    (int)(projectile.damage * 0.22f),
                    0f,
                    projectile.owner,
                    Main.rand.Next(3),
                    Main.rand.NextFloat(0.85f, 1.1f));
            }
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 180);
            target.AddBuff(BuffID.Venom, 120);
            ChloroplastCommon.SimpleBurst(projectile, DustID.GreenTorch, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague), 10, 1f, 3.6f, 0.8f, 1.15f);
            SoundEngine.PlaySound(SoundID.NPCHit18 with { Volume = 0.28f, Pitch = -0.2f }, target.Center);
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            ChloroplastCommon.SimpleBurst(projectile, DustID.GreenTorch, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague), 12, 1f, 3.8f);
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
            ChloroplastCommon.DrawGlow(projectile, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague), 1.04f, 0.28f);
            return true;
        }
    }
}
