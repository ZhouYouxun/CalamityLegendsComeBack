using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles;
using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.C_PostPlantera
{
    public class DERule_PestilentDefiler : DEBulletRule
    {
        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.PestilentDefiler>();

        public override void SetDefaults(Projectile projectile)
        {
            projectile.friendly = false;
            projectile.hide = true;
            projectile.timeLeft = 2;
            projectile.tileCollide = false;
        }

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            if (Main.myPlayer == projectile.owner)
            {
                Vector2 destination = owner.Calamity().mouseWorld;
                Vector2 fromOwner = destination - owner.MountedCenter;
                if (fromOwner.Length() > 760f)
                    destination = owner.MountedCenter + fromOwner.SafeNormalize(Vector2.UnitX * owner.direction) * 760f;

                Projectile.NewProjectile(
                    projectile.GetSource_FromAI(),
                    destination,
                    Vector2.Zero,
                    ModContent.ProjectileType<DEBullet_PestilentCloud>(),
                    Math.Max(1, (int)(projectile.damage * 0.34f)),
                    projectile.knockBack,
                    projectile.owner,
                    128f);
            }

            projectile.Kill();
        }

        public override bool PreDraw(Projectile projectile, Player owner, ref Color lightColor) => false;

        public override string TooltipEffectEN => "Creates a lingering plague contamination field at the aim point; no separate bullet";
        public override string TooltipEffectZH => "在瞄准点生成一片疫病污染区域，不再发射单独子弹";
    }
}
