using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheEndothermicEnergy
{
    internal class EndothermicEnergyEffect : DefaultEffect
    {
        public override int EffectID => 34;

        public override int AmmoType => ModContent.ItemType<EndothermicEnergy>();

        public override Color ThemeColor => new Color(120, 170, 255);
        public override Color StartColor => new Color(220, 240, 255);
        public override Color EndColor => new Color(30, 60, 130);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.GetGlobalProjectile<EndothermicEnergy_GP>().firstFrame = true;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            EndothermicEnergy_GP gp = projectile.GetGlobalProjectile<EndothermicEnergy_GP>();
            if (gp.firstFrame)
            {
                gp.firstFrame = false;
                projectile.Kill();
            }
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            return true;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 spawnVelocity = projectile.velocity.SafeNormalize(Vector2.UnitX) * projectile.velocity.Length();
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                spawnVelocity,
                ModContent.ProjectileType<EndothermicEnergy_Copy>(),
                projectile.damage,
                projectile.knockBack,
                owner.whoAmI
            );
        }
    }

    internal class EndothermicEnergy_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}
