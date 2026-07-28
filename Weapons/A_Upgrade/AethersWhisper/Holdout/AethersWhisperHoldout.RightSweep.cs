using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.RightClick;
using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Shared;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Holdout
{
    internal sealed partial class AethersWhisperHoldout
    {
        // ===== 右键：微光折返扫射（固定四连为一小节）=====

        private void RunRightSweep()
        {
            // 到达本束发射 tick 就射出（严格 4 束，无第五束）。
            for (int i = beamsFiredThisRound; i < AethersWhisperBalance.BeamsPerRound; i++)
            {
                if (roundTick != AethersWhisperBalance.BeamFireTicks[i])
                    continue;

                if (!Owner.CheckMana(AethersWhisperBalance.RightManaPerBeam, true))
                {
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.25f, Pitch = -0.5f }, Owner.Center);
                    ResetRightRound();
                    return;
                }

                FireBeam(i);
                beamsFiredThisRound++;
                break;
            }

            roundTick++;
            if (roundTick >= AethersWhisperBalance.RoundRestartTick)
            {
                roundTick = 0;
                beamsFiredThisRound = 0;
            }
        }

        private void ResetRightRound()
        {
            roundTick = 0;
            beamsFiredThisRound = 0;
        }

        private void FireBeam(int beamIndex)
        {
            Vector2 tip = GunTip;
            Vector2 aimWorld = Main.MouseWorld;
            Vector2 dir = (aimWorld - tip).SafeNormalize(AimDirection);

            int weaponDamage = Owner.GetWeaponDamage(Owner.HeldItem);
            int damage = Math.Max(1, (int)(weaponDamage * AethersWhisperBalance.BeamDamageMult));

            if (Main.myPlayer == Projectile.owner)
            {
                int beam = Projectile.NewProjectile(Projectile.GetSource_FromThis(), tip, dir * AethersWhisperBalance.BeamSpeed,
                    ModContent.ProjectileType<AethersWhisperRefractionBeam>(), damage, AethersWhisperBalance.KnockBack,
                    Projectile.owner, aimWorld.X, aimWorld.Y);
                if (Main.projectile.IndexInRange(beam))
                {
                    Main.projectile[beam].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                    Main.projectile[beam].netUpdate = true;
                }
            }

            rightFlashTimer = 6;
            starPhaseKick = (starPhaseKick + MathHelper.Pi / 6f) % MathHelper.TwoPi;
            recoilOffset = Math.Max(recoilOffset, 6f);
            Owner.velocity -= dir * 0.5f;

            // 四束音高逐渐提高 0.03。
            SoundEngine.PlaySound(SoundID.Item91 with { Volume = 0.42f, Pitch = 0.15f + beamIndex * 0.03f, MaxInstances = 4 }, tip);

            SpawnBeamMuzzle(tip, dir, beamIndex);
        }

        // 每束的枪口爆发：拉宽脉冲环 + 前向 CustomSpark 光条 + 电弧尘（快、密、脆）。
        private void SpawnBeamMuzzle(Vector2 muzzle, Vector2 dir, int beamIndex)
        {
            if (Main.dedServ) return;
            float tint = beamIndex / 3f;
            Color c = AethersWhisperVisuals.Lerp(0.25f + tint * 0.4f);

            GeneralParticleHandler.SpawnParticle(new CustomSpark(muzzle, dir * 8f,
                AethersWhisperVisuals.PulseRingAltTex, false, 12, 0.04f, c, new Vector2(2f, 0.6f), true, false, shrinkSpeed: 0.25f));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(muzzle, dir * 0.5f,
                AethersWhisperVisuals.ToWhite(c, 0.2f), new Vector2(0.22f, 1.1f), dir.ToRotation(), 0.04f, 0.5f, 10));

            for (int i = 0; i < 4; i++)
            {
                Vector2 vel = dir.RotatedByRandom(0.28f) * Main.rand.NextFloat(4f, 9f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(muzzle, vel,
                    "CalamityMod/Particles/BloomCircle", false, Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.07f, 0.11f), AethersWhisperVisuals.ToWhite(c, Main.rand.NextFloat(0.3f)),
                    new Vector2(0.5f, 1.6f), true, true, glowCenterScale: 0.6f, shrinkSpeed: 0.2f));
            }
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustPerfect(muzzle, Main.rand.NextBool() ? AethersWhisperVisuals.ElectricDust : AethersWhisperVisuals.ArsenalFireworkDust,
                    dir.RotatedByRandom(0.3f) * Main.rand.NextFloat(4f, 12f), 0, c, Main.rand.NextFloat(0.9f, 1.4f));
                d.noGravity = true;
            }
        }
    }
}
