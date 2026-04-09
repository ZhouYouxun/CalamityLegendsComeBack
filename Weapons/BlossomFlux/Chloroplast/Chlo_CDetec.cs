using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast
{
    // 侦查叶绿体：提供短标记，让整队更容易集火被扫描到的目标。
    internal sealed class Chlo_CDetec : BlossomFluxChloroplastPreset
    {
        public static Chlo_CDetec Instance { get; } = new();

        private Chlo_CDetec()
        {
        }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            projectile.penetrate = 1;
            projectile.timeLeft = 120;
            projectile.localNPCHitCooldown = 10;
            projectile.velocity *= 1.05f;
        }

        public override void AI(Projectile projectile)
        {
            Lighting.AddLight(projectile.Center, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec).ToVector3() * 0.34f);

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                    DustID.RedTorch,
                    Main.rand.NextVector2Circular(0.45f, 0.45f),
                    100,
                    ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec),
                    Main.rand.NextFloat(0.8f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<BFArrow_CDetecNPC>().ApplyDamageAmpMark(projectile.owner, 30);

            if (projectile.owner == Main.myPlayer)
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.32f, Pitch = 0.3f }, target.Center);

            ChloroplastCommon.SimpleBurst(projectile, DustID.RedTorch, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec), 10, 1f, 3.8f, 0.8f, 1.15f);
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            ChloroplastCommon.SimpleBurst(projectile, DustID.RedTorch, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec), 10, 1.1f, 3.4f, 0.8f, 1.05f);
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
            ChloroplastCommon.DrawGlow(projectile, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec), 1.03f, 0.28f);
            return true;
        }
    }
}
