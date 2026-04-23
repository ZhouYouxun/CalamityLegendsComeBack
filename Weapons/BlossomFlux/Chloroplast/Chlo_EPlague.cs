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
            projectile.localAI[0] = 30f;
            projectile.localAI[1] = Main.rand.Next(2, 11);
        }

        public override void AI(Projectile projectile)
        {
            Lighting.AddLight(projectile.Center, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague).ToVector3() * 0.34f);
            ChloroplastCommon.EmitTrail(projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, 1.02f);

            if (projectile.owner == Main.myPlayer && projectile.ai[1] >= projectile.localAI[0])
            {
                int releaseInterval = (int)projectile.localAI[1];
                if (releaseInterval <= 0)
                    releaseInterval = 2;

                if (((int)projectile.ai[1] - (int)projectile.localAI[0]) % releaseInterval == 0)
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
                    projectile.localAI[1] = Main.rand.Next(2, 11);
                }
            }
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 180);
            target.AddBuff(BuffID.Venom, 120);
            ChloroplastCommon.EmitBurst(projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, 12, 1f, 3.6f, 0.8f, 1.15f);
            SoundEngine.PlaySound(SoundID.NPCHit18 with { Volume = 0.28f, Pitch = -0.2f }, target.Center);
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            ChloroplastCommon.EmitBurst(projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, 12, 1f, 3.8f);
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
            ChloroplastCommon.DrawPresetProjectile(projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, lightColor, 1.04f);
            return false;
        }
    }
}
