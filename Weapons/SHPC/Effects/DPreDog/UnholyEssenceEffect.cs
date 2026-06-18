using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class UnholyEssenceEffect : DefaultEffect
    {
        public override int EffectID => 26;

        public override int AmmoType => ModContent.ItemType<UnholyEssence>();

        public override Color ThemeColor => new Color(170, 255, 150);
        public override Color StartColor => new Color(220, 255, 180);
        public override Color EndColor => new Color(80, 170, 80);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;
        public override bool EnableDefaultSlowdown => false;
        public override bool PlayDefaultLeftClickFireSound => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.GetGlobalProjectile<UnholyEssence_GP>().firstFrame = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 2;
            projectile.tileCollide = false;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            UnholyEssence_GP gp = projectile.GetGlobalProjectile<UnholyEssence_GP>();
            if (!gp.firstFrame)
                return;

            gp.firstFrame = false;
            projectile.Kill();
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArsenalOffCooldown"), projectile.Center);
            if (projectile.owner != Main.myPlayer)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(owner.direction == 0 ? Vector2.UnitX : new Vector2(owner.direction, 0f));
            if (forward == Vector2.Zero)
                forward = Vector2.UnitX;

            Vector2 spawnVelocity = forward * 24f;
            int waveIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                spawnVelocity,
                ModContent.ProjectileType<UnholyEssence_Wave>(),
                (int)(projectile.damage * 1.2f),
                projectile.knockBack,
                projectile.owner);

            if (Main.projectile.IndexInRange(waveIndex))
                Main.projectile[waveIndex].rotation = spawnVelocity.ToRotation();
        }
    }

    public class UnholyEssence_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}
