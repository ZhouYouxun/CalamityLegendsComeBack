using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill
{
    public class YC_EX_Drone : YC_EX_WarshipBase
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        private ref float AttackTimer => ref Projectile.localAI[0];

        public int ColorIndex => (int)Projectile.ai[1];

        public static readonly Color[] RainbowPalette =
        {
            new(255, 96, 96),
            new(255, 154, 78),
            new(255, 220, 94),
            new(116, 235, 126),
            new(108, 196, 255),
            new(120, 138, 255),
            new(208, 118, 255)
        };

        public static Color GetDroneColor(int index) => RainbowPalette[Utils.Clamp(index, 0, RainbowPalette.Length - 1)];

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/EXSkill/YC_EX_Drone";

        protected override float FormationRadius => 86f;
        protected override int FormationCount => YC_EX_VIP.DroneTotal;
        protected override float TargetRange => 1700f;
        protected override float ScaleBase => 0.94f;
        protected override float ScaleAmplitude => 0.045f;
        protected override Color AccentColor => GetDroneColor(ColorIndex);

        protected override void OnStateChanged(YC_EX_VIP.EXVipState newState)
        {
            if (newState != YC_EX_VIP.EXVipState.Firing)
                AttackTimer = 0f;
        }

        protected override void HandleFiringState(YC_EX_VIP vip, int timer)
        {
            if (AttackTimer > 0f)
                AttackTimer--;

            if (Projectile.owner != Main.myPlayer || AttackTimer > 0f)
                return;

            YC_CBeam.SpawnBeam(
                Projectile.GetSource_FromThis(),
                Projectile.Center + CurrentForwardDirection * 18f,
                CurrentForwardDirection,
                (int)(Projectile.damage * 1.12f),
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI,
                YC_CBeam.BeamAnchorKind.ExDrone,
                1720f,
                16f,
                16,
                false,
                false,
                AccentColor,
                Color.White,
                18f,
                0f,
                -1,
                12,
                2);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + CurrentForwardDirection * 22f,
                CurrentForwardDirection * 8.4f,
                ModContent.ProjectileType<YC_WarshipPulse>(),
                (int)(Projectile.damage * 0.75f),
                Projectile.knockBack * 0.45f,
                Projectile.owner,
                1.18f,
                1f);

            EmitMuzzleBurst(CurrentForwardDirection, 6, 4f);
            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.14f, Pitch = -0.18f + ColorIndex * 0.03f }, Projectile.Center);
            AttackTimer = 16f;
        }
    }
}
