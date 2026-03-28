using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ashes
{
    internal class AshesofAnnEffect : DefaultEffect
    {
        public override int EffectID => 39;

        public override int AmmoType => ModContent.ItemType<AshesofAnnihilation>();

        public override Color ThemeColor => new Color(120, 0, 0);
        public override Color StartColor => new Color(200, 20, 20);
        public override Color EndColor => new Color(10, 0, 0);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;

        public override void OnSpawn(Projectile projectile, Player owner)
        {

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