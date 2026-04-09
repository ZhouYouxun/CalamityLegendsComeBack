using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.BlossomFlux.TurretMode;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive
{
    // 手持 BlossomFlux 时负责周期性降下被动弹幕；背包右键只改这里的开关状态。
    internal sealed class BFPassivePlayer : ModPlayer
    {
        private bool holdingBlossomFlux;
        private int passiveReleaseTimer;

        public override void ResetEffects()
        {
            holdingBlossomFlux = false;
        }

        public override void UpdateDead()
        {
            holdingBlossomFlux = false;
            passiveReleaseTimer = 0;
        }

        public override void PostUpdate()
        {
            if (!holdingBlossomFlux || Player.HeldItem.type != ModContent.ItemType<NewLegendBlossomFlux>())
            {
                passiveReleaseTimer = 0;
                return;
            }

            BFRightUIPlayer rightUIPlayer = Player.GetModPlayer<BFRightUIPlayer>();
            if (!rightUIPlayer.PassiveRainUnlocked || !rightUIPlayer.PassiveRainEnabled)
            {
                passiveReleaseTimer = 0;
                return;
            }

            if (Player.whoAmI != Main.myPlayer)
                return;

            if (++passiveReleaseTimer < GetPassiveReleaseRate())
                return;

            passiveReleaseTimer = Main.rand.Next(-5, 6);
            ReleasePassiveRain();
        }

        public void SetHoldingBlossomFlux()
        {
            holdingBlossomFlux = true;
        }

        private int GetPassiveReleaseRate()
        {
            return Player.GetModPlayer<BFTurretModePlayer>().TurretModeActive ? 22 : 34;
        }

        private void ReleasePassiveRain()
        {
            int burstCount = Main.rand.Next(2, 4);
            for (int i = 0; i < burstCount; i++)
                SpawnSingleRainShard(i, burstCount);

            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.22f, Pitch = -0.35f }, Player.Center);
        }

        private void SpawnSingleRainShard(int index, int burstCount)
        {
            float horizontalOffset = MathHelper.Lerp(150f, 240f, Main.rand.NextFloat()) * Player.direction;
            float verticalOffset = Main.rand.NextFloat(-360f, -280f);
            Vector2 spawnPosition = Player.Center + new Vector2(horizontalOffset, verticalOffset);

            float spread = burstCount <= 1 ? 0f : MathHelper.Lerp(-0.14f, 0.14f, index / (float)(burstCount - 1));
            Vector2 velocity = new Vector2(Player.direction * Main.rand.NextFloat(3.5f, 5.5f), Main.rand.NextFloat(8.5f, 11.5f)).RotatedBy(spread);
            int damage = (int)(Player.GetWeaponDamage(Player.HeldItem) * (Player.GetModPlayer<BFTurretModePlayer>().TurretModeActive ? 0.78f : 0.62f));

            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<BFPassiveRain>(),
                damage,
                1.1f,
                Player.whoAmI);
        }
    }
}
