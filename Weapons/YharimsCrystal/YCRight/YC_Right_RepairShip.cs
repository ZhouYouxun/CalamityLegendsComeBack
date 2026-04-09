using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight
{
    public class YC_Right_RepairShip : YC_RightWarshipBase
    {
        private bool shieldsSpawned;

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCRight/YC_Right_RepairShip";
        protected override Color AccentColor => new(110, 235, 255);

        protected override float PositionLerp => 0.22f;
        protected override float ScaleBase => 0.94f;
        protected override float ScaleAmplitude => 0.03f;
        protected override float LightStrength => 0.55f;

        protected override Vector2 GetLocalOffset() => new(0f, -92f);
        protected override float GetAngleOffsetDegrees() => 0f;

        protected override void UpdateAttack(YC_RightHoldOut holdout, Projectile holdoutProjectile)
        {
            if (shieldsSpawned || Projectile.owner != Main.myPlayer)
                return;

            shieldsSpawned = true;
            for (int i = 0; i < YC_Right_RepairShield.ShieldCount; i++)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<YC_Right_RepairShield>(),
                    0,
                    0f,
                    Projectile.owner,
                    i,
                    ParentHoldoutIndex);
            }

            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.16f, Pitch = 0.18f }, Projectile.Center);
        }
    }
}
