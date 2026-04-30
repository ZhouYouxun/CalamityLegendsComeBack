using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Projectiles.DraedonsArsenal;
using CalamityMod.Projectiles.Ranged;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.C_Warships
{
    public class YC_WarshipBattleship : YC_WarshipBase
    {
        private static readonly Vector2[] RelativeOffsets =
        {
            new(-110f, -96f),
            new(110f, -96f)
        };

        private static readonly float[] AngleOffsetsDegrees =
        {
            -2.5f,
            2.5f
        };

        private bool timerInitialized;
        private ref float AttackTimer => ref Projectile.localAI[0];

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCRight/YC_Right_Battleship";
        protected override Color AccentColor => new(255, 145, 105);
        protected override float PositionLerp => 0.22f;
        protected override float ScaleBase => 1f;

        protected override Vector2 GetLocalOffset() => RelativeOffsets[Utils.Clamp(SlotIndex, 0, RelativeOffsets.Length - 1)];
        protected override float GetAngleOffsetDegrees() => AngleOffsetsDegrees[Utils.Clamp(SlotIndex, 0, AngleOffsetsDegrees.Length - 1)];

        protected override void UpdateAttack(YC_WarshipHoldout holdout, Projectile holdoutProjectile)
        {
            if (!timerInitialized)
            {
                AttackTimer = SlotIndex == 0 ? 16f : 54f;
                timerInitialized = true;
            }

            if (AttackTimer > 0f)
            {
                AttackTimer--;
                return;
            }

            if (Projectile.owner != Main.myPlayer)
                return;

            int shotType = SlotIndex == 0
                ? ModContent.ProjectileType<TeslaCannonShot>()
                : ModContent.ProjectileType<KarasawaShot>();

            int shot = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + CurrentForwardDirection * 24f,
                CurrentForwardDirection * 2.65f,
                shotType,
                (int)(Projectile.damage * (SlotIndex == 0 ? 2.05f : 2.35f)),
                Projectile.knockBack + 1.2f,
                Projectile.owner);

            if (shot >= 0 && shot < Main.maxProjectiles)
            {
                Main.projectile[shot].tileCollide = false;
                Main.projectile[shot].DamageType = DamageClass.Magic;
            }

            EmitMuzzleBurst(CurrentForwardDirection, AccentColor, 5.8f, 10);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.3f, Pitch = -0.32f + SlotIndex * 0.08f }, Projectile.Center);
            AttackTimer = 76f;
        }
    }
}
