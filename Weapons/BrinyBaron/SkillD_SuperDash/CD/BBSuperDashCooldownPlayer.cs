using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.BrinyBaron;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal class BBSuperDashCooldownPlayer : ModPlayer
    {
        public const int NoBossCooldown = 30 * 60;
        public const int BossCooldown = 40 * 60;

        public int CooldownTimer { get; private set; }
        public int CooldownDuration { get; private set; } = NoBossCooldown;

        private bool cooldownActive;

        public bool IsCoolingDown => cooldownActive && CooldownTimer < CooldownDuration;
        public bool CanUseSuperDash => !IsCoolingDown;
        public float CooldownCompletion => !IsCoolingDown || CooldownDuration <= 0 ? 1f : CooldownTimer / (float)CooldownDuration;
        public int RemainingFrames => IsCoolingDown ? CooldownDuration - CooldownTimer : 0;

        public override void Initialize()
        {
            ResetState();
        }

        public override void UpdateDead()
        {
            ResetState();
        }

        public override void PostUpdate()
        {
            if (!IsCoolingDown)
                return;

            CooldownTimer++;
            if (CooldownTimer >= CooldownDuration)
            {
                CooldownTimer = CooldownDuration;
                cooldownActive = false;
                PlayCooldownReadyFeedback();
            }
        }

        public void StartCooldown()
        {
            RestartCooldown(AnyBossAlive() ? BossCooldown : NoBossCooldown);
        }

        public void ClearCooldown()
        {
            CooldownTimer = CooldownDuration;
            cooldownActive = false;
        }

        private void RestartCooldown(int duration)
        {
            CooldownDuration = duration;
            CooldownTimer = 0;
            cooldownActive = true;
        }

        private void ResetState()
        {
            CooldownDuration = NoBossCooldown;
            CooldownTimer = CooldownDuration;
            cooldownActive = false;
        }

        private void PlayCooldownReadyFeedback()
        {
            if (Main.myPlayer != Player.whoAmI)
                return;

            SoundEngine.PlaySound(SoundID.Item29 with
            {
                Volume = 1f,
                Pitch = 0.22f
            }, Player.Center);

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 10f) * Main.rand.NextFloat(2.4f, 5.4f);
                Dust glow = Dust.NewDustPerfect(
                    Player.Center,
                    Main.rand.NextBool(3) ? DustID.GoldCoin : DustID.YellowTorch,
                    velocity,
                    100,
                    new Color(255, 226, 115),
                    Main.rand.NextFloat(1.05f, 1.45f));
                glow.noGravity = true;
            }
        }

        private static bool AnyBossAlive()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.boss && !npc.friendly && npc.lifeMax > 5)
                    return true;
            }

            return false;
        }
    }
}
