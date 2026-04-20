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
                ChloroplastCommon.EmitTrail(projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 1.08f);

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

            ChloroplastCommon.EmitTrail(projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 0.92f);

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
            ChloroplastCommon.EmitBurst(projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 10, 1f, 3.4f);
            BeginReturn(projectile);
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            ChloroplastCommon.EmitBurst(projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 12, 1f, 3.4f);
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
            ChloroplastCommon.DrawPresetProjectile(projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, lightColor, 1.04f);
            return false;
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
            ChloroplastCommon.EmitBurst(projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 8, 0.8f, 2.6f, 0.8f, 1.1f);
        }
    }
}
