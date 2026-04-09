using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill
{
    public class YC_EX_LaserCruiser : YC_EX_WarshipBase
    {
        private bool laserSpawned;

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/EXSkill/YC_EX_LaserCruiser";

        protected override float FormationRadius => 136f;
        protected override int FormationCount => YC_EX_VIP.LaserCruiserTotal;
        protected override float FormationAngleOffsetRadians => -MathHelper.PiOver4;
        protected override float TargetRange => 1840f;
        protected override float ScaleBase => 0.98f;
        protected override float ScaleAmplitude => 0.035f;
        protected override float LightStrength => 0.52f;
        protected override Color AccentColor => new(255, 205, 118);

        protected override void OnStateChanged(YC_EX_VIP.EXVipState newState)
        {
            if (newState != YC_EX_VIP.EXVipState.Firing)
                laserSpawned = false;
        }

        protected override void HandleFiringState(YC_EX_VIP vip, int timer)
        {
            if (laserSpawned || Projectile.owner != Main.myPlayer)
                return;

            laserSpawned = true;
            YC_CBeam.SpawnBeam(
                Projectile.GetSource_FromThis(),
                Projectile.Center + CurrentForwardDirection * 24f,
                CurrentForwardDirection,
                (int)(Projectile.damage * 1.55f),
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI,
                YC_CBeam.BeamAnchorKind.ExDrone,
                1760f,
                24f,
                YC_EX_VIP.LaserFireTime,
                false,
                false,
                AccentColor,
                Color.White,
                24f,
                0f,
                -1,
                10);

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.34f, Pitch = -0.14f + SlotIndex * 0.04f }, Projectile.Center);
        }
    }
}
