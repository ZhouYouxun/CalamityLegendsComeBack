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
        private static readonly int[] LastPrimarySpawnFrameByOwner = new int[Main.maxPlayers];
        private const int PrimarySpawnLockoutFrames = 30;

        public override int EffectID => 34;

        public override int AmmoType => ModContent.ItemType<EndothermicEnergy>();
        private const float ArrowLaunchSpeed = 35f;

        public override Color ThemeColor => new Color(120, 170, 255);
        public override Color StartColor => new Color(220, 240, 255);
        public override Color EndColor => new Color(30, 60, 130);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override bool EnableDefaultSlowdown => false;
        public override bool PlayDefaultLeftClickFireSound => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            EndothermicEnergy_GP gp = projectile.GetGlobalProjectile<EndothermicEnergy_GP>();
            gp.firstFrame = true;
            gp.releaseArrow = false;
            gp.hasReleasedArrow = false;

            int ownerIndex = owner.whoAmI;
            int currentFrame = (int)Main.GameUpdateCount;
            if (ownerIndex >= 0 && ownerIndex < LastPrimarySpawnFrameByOwner.Length &&
                (LastPrimarySpawnFrameByOwner[ownerIndex] <= 0 ||
                currentFrame - LastPrimarySpawnFrameByOwner[ownerIndex] > PrimarySpawnLockoutFrames))
            {
                LastPrimarySpawnFrameByOwner[ownerIndex] = currentFrame;
                gp.releaseArrow = true;
            }
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
            if (projectile.owner != Main.myPlayer)
                return;

            EndothermicEnergy_GP gp = projectile.GetGlobalProjectile<EndothermicEnergy_GP>();
            if (!gp.releaseArrow || gp.hasReleasedArrow)
                return;

            gp.hasReleasedArrow = true;

            Vector2 spawnVelocity = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction) * ArrowLaunchSpeed;
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                spawnVelocity,
                ModContent.ProjectileType<EndothermicEnergy_ARROW>(),
                projectile.damage,
                projectile.knockBack,
                owner.whoAmI
            );
        }
    }

    internal class EndothermicEnergy_GP : GlobalProjectile
    {
        public string LocalizationCategory => "Projectiles.SHPC";
        public override bool InstancePerEntity => true;

        public bool firstFrame;
        public bool releaseArrow;
        public bool hasReleasedArrow;
    }
}
