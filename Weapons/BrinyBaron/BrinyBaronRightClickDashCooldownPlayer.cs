using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    internal class BrinyBaronRightClickDashCooldownPlayer : ModPlayer
    {
        public const int DashCooldown = 3 * 60;

        public int CooldownTimer { get; private set; }

        public bool IsCoolingDown => CooldownTimer > 0;
        public bool CanUseDash => CooldownTimer <= 0;
        public float CooldownCompletion => 1f - CooldownTimer / (float)DashCooldown;

        public override void Initialize()
        {
            CooldownTimer = 0;
        }

        public override void UpdateDead()
        {
            CooldownTimer = 0;
        }

        public override void PostUpdate()
        {
            if (CooldownTimer > 0)
                CooldownTimer--;
        }

        public void StartCooldown()
        {
            CooldownTimer = DashCooldown;
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
