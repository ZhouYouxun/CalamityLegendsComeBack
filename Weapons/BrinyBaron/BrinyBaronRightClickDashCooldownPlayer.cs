using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    internal class BrinyBaronRightClickDashCooldownPlayer : ModPlayer
    {
        public int CooldownTimer { get; private set; }
        public int CooldownDuration { get; private set; }

        public bool IsCoolingDown => CooldownTimer > 0;
        public bool CanUseDash => CooldownTimer <= 0;
        public float CooldownCompletion => 1f - CooldownTimer / (float)System.Math.Max(1, CooldownDuration);

        public override void Initialize()
        {
            CooldownTimer = 0;
            CooldownDuration = BB_Balance.DefaultRightClickCooldown;
        }

        public override void UpdateDead()
        {
            CooldownTimer = 0;
            CooldownDuration = BB_Balance.DefaultRightClickCooldown;
        }

        public override void PostUpdate()
        {
            if (CooldownTimer > 0)
                CooldownTimer--;
        }

        public void StartCooldown()
        {
            StartCooldown(BB_Balance.DefaultRightClickCooldown);
        }

        public void StartCooldown(int frames)
        {
            CooldownDuration = System.Math.Max(1, frames);
            CooldownTimer = CooldownDuration;
        }

        public void ClearCooldown()
        {
            CooldownTimer = 0;
        }

        public void ReduceCooldownTo(int frames)
        {
            if (CooldownTimer > frames)
                CooldownTimer = frames;
        }
    }
}
