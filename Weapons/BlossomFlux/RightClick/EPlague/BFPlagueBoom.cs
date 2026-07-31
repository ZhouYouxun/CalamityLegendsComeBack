using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    // 疫爆：疫球引爆时叠放的三层巨型腐蚀冲击波。
    // 借用灾厄的柏林噪声 + ForceField 着色器骨架（和狞桀爆裂同一套），配色换成叶流瘟疫的疫绿→腐黑。
    internal class BFPlagueBoom : BaseMassiveExplosionProjectile
    {
        // 疫球引爆闪光的亮疫绿，比形态主色更亮，保证在噪声上不会糊成一团。
        private static readonly Color PlagueFlare = new(216, 255, 104);

        // 腐败褪色，冲击波末期收进这个颜色，读起来像毒素沉降而不是火焰熄灭。
        private static readonly Color PlagueRot = new(12, 40, 14);

        public override int Lifetime => 66;

        // 狞桀爆裂把震屏关掉了，这里留一点点，和你们瘟疫箭扎中时的 SetScreenshake 呼应。
        public override bool UsesScreenshake => true;

        public override float GetScreenshakePower(float pulseCompletionRatio) =>
            CalamityUtils.Convert01To010(pulseCompletionRatio) * 2.4f;

        // 前段偏亮疫绿，后段迅速沉进腐黑，衰减比灾厄默认更快一点，避免三层叠起来糊屏。
        public override Color GetCurrentExplosionColor(float pulseCompletionRatio) =>
            Color.Lerp(PlagueFlare, PlagueRot, MathHelper.Clamp(pulseCompletionRatio * 1.9f, 0f, 1f));

        public override float Fadeout(float completion) => (1f - (float)System.Math.Sqrt(completion)) * 0.62f;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override void PostAI()
        {
            Lighting.AddLight(Projectile.Center, 0.10f, 0.30f, 0.06f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 冲击波只上「疫萎」这一档，「疫亡」留给疫球本体直击。
            target.GetGlobalNPC<BFPlaguePollutionNPC>().ApplyWither(target, Projectile.owner, fromRightClick: true);
            target.AddBuff(BuffID.Venom, 240);
        }
    }
}
