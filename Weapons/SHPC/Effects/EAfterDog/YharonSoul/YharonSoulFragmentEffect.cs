using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items.Materials;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.YharonSoul
{
    public class YharonSoulFragmentEffect : DefaultEffect
    {
        public override int EffectID => 37;

        public override int AmmoType => ModContent.ItemType<YharonSoulFragment>();

        public override Color ThemeColor => new Color(255, 140, 40);
        public override Color StartColor => new Color(255, 200, 80);
        public override Color EndColor => new Color(120, 30, 0);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            var gp = projectile.GetGlobalProjectile<YharonSoulFragment_GP>();

            // 标记第一帧
            gp.firstFrame = true;
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            var gp = projectile.GetGlobalProjectile<YharonSoulFragment_GP>();

            // 第一帧直接自杀（确保状态已同步）
            if (gp.firstFrame)
            {
                gp.firstFrame = false;
                projectile.Kill();
                return;
            }
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX);

            // 发射火焰（参数完全保持不变）
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                direction * 12f,
                ModContent.ProjectileType<YharonSoulFragment_Flame>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner,
                projectile.ai[0], // ✅ 继承EffectID（关键）
                projectile.ai[1],
                projectile.ai[2]
            );
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 300);
        }
    }

    // ================= 独立实例数据 =================
    public class YharonSoulFragment_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}