using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill
{
    internal sealed class BFUltimateFieldPlayer : ModPlayer
    {
        private const int RefreshFrames = 2;
        private const int RecoveryPersistFrames = 8 * 60;
        private const int RecoveryStackBuildFrames = 45;
        private const int MaxRecoveryStacks = 5;

        private int reconFieldTimer;
        private int recoveryFieldTimer;
        private int recoveryPersistTimer;
        private int recoveryBuildTimer;

        public int RecoveryStacks { get; private set; }

        public override void UpdateDead()
        {
            reconFieldTimer = 0;
            recoveryFieldTimer = 0;
            recoveryPersistTimer = 0;
            recoveryBuildTimer = 0;
            RecoveryStacks = 0;
        }

        public override void PostUpdate()
        {
            if (reconFieldTimer > 0)
                reconFieldTimer--;

            if (recoveryFieldTimer > 0)
            {
                recoveryFieldTimer--;
                recoveryPersistTimer = System.Math.Max(recoveryPersistTimer, RecoveryPersistFrames);

                recoveryBuildTimer++;
                if (recoveryBuildTimer >= RecoveryStackBuildFrames)
                {
                    recoveryBuildTimer = 0;
                    if (RecoveryStacks < MaxRecoveryStacks)
                    {
                        RecoveryStacks++;
                        if (Player.whoAmI == Main.myPlayer)
                            SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.35f, Pitch = 0.16f + RecoveryStacks * 0.02f }, Player.Center);
                    }
                }
            }
            else
            {
                recoveryBuildTimer = 0;
            }

            if (recoveryPersistTimer > 0)
            {
                recoveryPersistTimer--;
            }
            else
            {
                RecoveryStacks = 0;
            }
        }

        public override bool FreeDodge(Player.HurtInfo info)
        {
            if (reconFieldTimer > 0 &&
                info.DamageSource.SourceProjectileType >= 0 &&
                Main.rand.NextFloat() < 0.30f)
            {
                Player.immune = true;
                Player.immuneNoBlink = true;
                Player.immuneTime = System.Math.Max(Player.immuneTime, 20);
                if (Player.whoAmI == Main.myPlayer)
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.48f, Pitch = 0.28f }, Player.Center);
                return true;
            }

            return base.FreeDodge(info);
        }

        public override void PostHurt(Player.HurtInfo info)
        {
            if (recoveryPersistTimer <= 0 || RecoveryStacks <= 0)
                return;

            int healAmount = (int)(Player.statLifeMax2 * 0.2f * RecoveryStacks);
            int oldLife = Player.statLife;
            Player.statLife = System.Math.Min(Player.statLifeMax2, Player.statLife + healAmount);
            int actualHeal = Player.statLife - oldLife;

            RecoveryStacks = 0;
            recoveryPersistTimer = 0;
            recoveryBuildTimer = 0;

            if (actualHeal > 0)
                Player.HealEffect(actualHeal, true);

            if (Player.whoAmI == Main.myPlayer)
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.45f, Pitch = 0.14f }, Player.Center);
        }

        public void RefreshFromField(BlossomFluxChloroplastPresetType preset)
        {
            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    recoveryFieldTimer = RefreshFrames;
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    reconFieldTimer = RefreshFrames;
                    break;
            }
        }
    }
}
