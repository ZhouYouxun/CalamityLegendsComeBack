using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class SoulofFlightEffect : DefaultEffect
    {
        public override int EffectID => 11;
        public override int AmmoType => ItemID.SoulofFlight;

        // ===== 天蓝色 =====
        public override Color ThemeColor => new Color(120, 200, 255);
        public override Color StartColor => new Color(180, 230, 255);
        public override Color EndColor => new Color(60, 140, 200);

        public override float SquishyLightParticleFactor => 1.1f;
        public override float ExplosionPulseFactor => 1.1f;
        public override bool EnableDefaultSlowdown => false;

        private int spawnTimer;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // ===== 穿透次数 =====
            projectile.penetrate = 6;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            spawnTimer++;

            // ===== 每3帧往下发射 =====
            if (spawnTimer % 3 == 0)
            {
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    new Vector2(0f, 6f),
                    ModContent.ProjectileType<NewSHPS>(),
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner,
                    2 // preset2
                );
            }

            // ===== 重力逻辑（你原本的）=====
            float gravity = 0.18f;
            float maxFallSpeed = 16f;

            projectile.velocity.Y += gravity;

            if (projectile.velocity.Y > maxFallSpeed)
                projectile.velocity.Y = maxFallSpeed;

            projectile.rotation = projectile.velocity.ToRotation();

            projectile.velocity *= 1.020408f;

            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 1.8f);
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            // ===== 伤害降低 =====
            modifiers.FinalDamage *= 0.1f;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone) { }
        public override void OnKill(Projectile projectile, Player owner, int timeLeft) { }
    }
}