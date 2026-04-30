using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill
{
    public class YC_EX_LaserDrone : YC_EX_WarshipBase
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/EXSkill/YC_EX_LaserCruiser";

        protected override float FormationRadius => 156f;
        protected override int FormationCount => YC_EX_VIP.TotalWarshipCount;
        protected override float FormationAngleOffsetRadians => -MathHelper.PiOver2;
        protected override float TargetRange => 2200f;
        protected override float ScaleBase => 0.96f;
        protected override float ScaleAmplitude => 0.045f;
        protected override float LightStrength => 0.72f;
        protected override int IdleDustInterval => 7;
        protected override Color AccentColor => new(255, 224, 126);
        protected override Color OutlineColor => new(255, 245, 190);

        protected override Vector2 GetFiringAimDirection(Vector2 defaultDirection)
        {
            NPC target = YC_EXHelper.FindNearestTarget(Projectile, Owner.Center, TargetRange);
            return target != null
                ? (target.Center - Projectile.Center).SafeNormalize(defaultDirection)
                : defaultDirection;
        }

        protected override void OnStateChanged(YC_EX_VIP.EXVipState newState)
        {
            if (newState != YC_EX_VIP.EXVipState.Firing)
                KillAnchoredBeams();
        }

        protected override void HandleFiringState(YC_EX_VIP vip, int timer)
        {
            EnsurePersistentBeam(
                (int)(Projectile.damage * 1.85f),
                24f,
                2200f,
                new Color(255, 225, 115),
                Color.White,
                22f,
                6,
                2);

            if (Main.dedServ || timer % 4 != 0)
                return;

            Vector2 direction = CurrentForwardDirection.SafeNormalize((Projectile.Center - Owner.Center).SafeNormalize(Vector2.UnitY));
            GlowOrbParticle glow = new(
                Projectile.Center + direction * 18f + Main.rand.NextVector2Circular(4f, 4f),
                direction * Main.rand.NextFloat(0.35f, 1.1f),
                false,
                Main.rand.Next(8, 13),
                Main.rand.NextFloat(0.28f, 0.42f),
                Color.Lerp(AccentColor, Color.White, Main.rand.NextFloat(0.25f, 0.65f)),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(glow);

            if (timer % 20 == SlotIndex % 20)
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.08f, Pitch = 0.25f }, Projectile.Center);
        }
    }
}
