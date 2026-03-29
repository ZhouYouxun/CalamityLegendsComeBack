using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;


namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class BossSoulofMightEffect : DefaultEffect
    {
        public override int EffectID => 13;

        public override int AmmoType => ItemID.SoulofMight;

        public override Color ThemeColor => new Color(120, 0, 0);
        public override Color StartColor => new Color(200, 20, 20);
        public override Color EndColor => new Color(10, 0, 0);

        public override float SquishyLightParticleFactor => 1.1f;
        public override float ExplosionPulseFactor => 1.1f;
        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.Kill();
        }

        public override void AI(Projectile projectile, Player owner)
        {

        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {

        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {

        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {

        }














    }
}