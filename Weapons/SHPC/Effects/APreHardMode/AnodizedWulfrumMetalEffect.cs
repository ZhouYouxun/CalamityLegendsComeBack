using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    public class AnodizedWulfrumMetalEffect : DefaultEffect
    {
        public override int EffectID => 45;

        public override int AmmoType => ModContent.Find<ModItem>("CalamityMod/AnodizedWulfrumMetal").Type;

        public override bool EnableDefaultSlowdown => false;
        public override bool SuppressDefaultOnKillEffects => true;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.localAI[0] = 1f;
            projectile.penetrate = -1;
            projectile.timeLeft = 2;
            projectile.tileCollide = false;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            if (projectile.localAI[0] <= 0f)
                return;

            projectile.localAI[0] = 0f;
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            int blunderbussProjectileType = ModContent.Find<ModItem>("CalamityMod/WulfrumBlunderbuss").Item.shoot;
            Vector2 direction = projectile.velocity.SafeNormalize(new Vector2(owner.direction, 0f));
            float speed = projectile.velocity.Length();

            // Nine pellets over a total 5-degree cone.
            const int pelletCount = 9;
            float totalSpread = MathHelper.ToRadians(5f);
            for (int i = 0; i < pelletCount; i++)
            {
                float angle = MathHelper.Lerp(-totalSpread / 2f, totalSpread / 2f, i / (float)(pelletCount - 1));
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    direction.RotatedBy(angle) * speed,
                    blunderbussProjectileType,
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner);
            }
        }
    }
}
