using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.D_Endgame
{
    public class DERule_PearlGod : DEBulletRule
    {
        private static readonly Color PearlBlue = new(164, 224, 255);
        private static readonly Color PearlPink = new(255, 190, 224);
        private static readonly Color PearlGold = new(245, 224, 137);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.PearlGod>();

        public override int Penetrate => 3;
        public override float DamageMultiplier => 1.05f;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.light = 0.75f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            DEBulletUtils.OrientToVelocity(projectile);
            int colorIndex = (int)(projectile.localAI[0]++ % 3f);
            Color color = colorIndex switch
            {
                0 => PearlBlue,
                1 => PearlPink,
                _ => PearlGold
            };

            DEBulletUtils.TrailDust(projectile, DustID.GemDiamond, color, 1.05f, 0.16f);
            DEBulletUtils.GlowTrail(projectile, color, 1.1f);
            Lighting.AddLight(projectile.Center, color.ToVector3() * 0.5f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            DEBulletUtils.SpawnLifeSteal(owner, target, projectile, (int)(hit.Damage * 0.1f), 0.55f);

            if (Main.myPlayer == projectile.owner)
            {
                DEBulletUtils.SpawnAreaBurst(
                    projectile.GetSource_FromAI(),
                    target.Center,
                    Math.Max(1, (int)(hit.Damage * 0.58f)),
                    projectile.knockBack,
                    projectile.owner,
                    DEBurstStyle.Pearl,
                    96f);
            }
        }

        public override string TooltipEffectEN => "A stronger white life round; pierces 3 enemies and causes a large pearl burst on every hit";
        public override string TooltipEffectZH => "更强的白色吸血弹，可穿透3个敌人，每次命中都造成大型珍珠爆炸";
    }
}
