using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    // 瘟疫形态右键：蓄力期间在准星前方挂一颗孢子球，松手把它甩出去引爆。
    // 球的生命周期完全由这里托管，取消蓄力或换形态都会让它自己散掉。
    internal sealed partial class NewLegendBlossomFluxHoldOut
    {
        private int plagueOrbIndex = -1;

        private bool PlagueChargePoseActive => rightChargeActive && CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_EPlague;

        // 按索引取回自己那颗球，顺带做完整性校验，避免拿到被顶掉的槽位。
        private BFPlagueSporeBomb GetPlagueSporeOrb()
        {
            if (!BFArrowCommon.InBounds(plagueOrbIndex, Main.maxProjectiles))
                return null;

            Projectile orb = Main.projectile[plagueOrbIndex];
            if (!orb.active || orb.owner != Projectile.owner || orb.type != ModContent.ProjectileType<BFPlagueSporeBomb>())
            {
                plagueOrbIndex = -1;
                return null;
            }

            return orb.ModProjectile as BFPlagueSporeBomb;
        }

        private void SpawnPlagueSporeOrb()
        {
            if (!PlagueChargePoseActive || Projectile.owner != Main.myPlayer)
                return;

            // 场上只留一颗，重复按右键不该叠球。
            if (GetPlagueSporeOrb() is not null)
                return;

            plagueOrbIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                AimDirection,
                ModContent.ProjectileType<BFPlagueSporeBomb>(),
                1,
                0f,
                Projectile.owner);
        }

        // 每帧把蓄力进度喂给球；这同时也是球的续命信号。
        private void UpdatePlagueSporeOrb()
        {
            if (!PlagueChargePoseActive)
            {
                CancelPlagueSporeOrb();
                return;
            }

            // 锚在持握弹幕正中心，球跟着武器走。
            GetPlagueSporeOrb()?.PushCharge(ChargeCompletion, Projectile.Center, AimDirection);
        }

        // 蓄满松手：甩球。球不在（被打断、多人不同步）就退回原来的单发瘟疫箭。
        private void ReleasePlagueSporeOrb(float chargeCompletion)
        {
            BFPlagueSporeBomb orb = GetPlagueSporeOrb();
            if (orb is null)
            {
                FireSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_EPlague>(), 18.6f, 0.98f);
                return;
            }

            int damage = (int)(GetCurrentRightClickDamage() * RightClickBaseDamageMultiplier
                * MathHelper.Lerp(0.8f, 1.35f, chargeCompletion) * 0.98f);
            float knockback = Projectile.knockBack * MathHelper.Lerp(0.85f, 1.15f, chargeCompletion);

            orb.Launch(AimDirection, damage, knockback);
            plagueOrbIndex = -1;

            SpawnSHPCLeftMuzzleParticles(GunTipPosition, AimDirection * 14f, BlossomFluxChloroplastPresetType.Chlo_EPlague, 1.35f);
        }

        // 中断蓄力：只散掉还在悬停的球，已经甩出去的不管。
        private void CancelPlagueSporeOrb()
        {
            BFPlagueSporeBomb orb = GetPlagueSporeOrb();
            if (orb is null)
                return;

            if (orb.TryFizzle())
                plagueOrbIndex = -1;
        }
    }
}
