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
        // ===== 右键：二连散射折返扫射 =====
        // 一组 = 2 次散射（@0、@15），每次散射 5~7 束随机角度伪激光，直接飞行；组间隔 35 tick。

        private void RunRightSweep()
        {
            if (scattersFiredThisRound < AethersWhisperBalance.ScattersPerRound &&
                roundTick == scattersFiredThisRound * AethersWhisperBalance.ScatterGapTicks)
            {
                if (!Owner.CheckMana(AethersWhisperBalance.ScatterManaCost, true))
                {
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.25f, Pitch = -0.5f }, Owner.Center);
                    ResetRightRound();
                    return;
                }

                FireScatter(scattersFiredThisRound);
                scattersFiredThisRound++;
            }

            roundTick++;
            if (roundTick >= AethersWhisperBalance.RoundPeriodTicks)
            {
                roundTick = 0;
                scattersFiredThisRound = 0;
            }
        }

        private void ResetRightRound()
        {
            roundTick = 0;
            scattersFiredThisRound = 0;
        }

        // 一次散射：5~7 束随机角度/随机速度的伪激光，从枪口直接飞出。
        private void FireScatter(int scatterIndex)
        {
            Vector2 tip = GunTip;
            Vector2 aim = (Main.MouseWorld - tip).SafeNormalize(AimDirection);
            int beams = Main.rand.Next(AethersWhisperBalance.ScatterBeamsMin, AethersWhisperBalance.ScatterBeamsMax + 1);
            int weaponDamage = Owner.GetWeaponDamage(Owner.HeldItem);
            int damage = Math.Max(1, (int)(weaponDamage * AethersWhisperBalance.BeamDamageMult));

            if (Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < beams; i++)
                {
                    // 充满随机性：随机角度 + 随机速度倍率。
                    float ang = Main.rand.NextFloat(-AethersWhisperBalance.ScatterSpread, AethersWhisperBalance.ScatterSpread);
                    Vector2 dir = aim.RotatedBy(ang);
                    float speed = AethersWhisperBalance.BeamSpeed * Main.rand.NextFloat(0.9f, 1.18f);

                    int beam = Projectile.NewProjectile(Projectile.GetSource_FromThis(), tip, dir * speed,
                        ModContent.ProjectileType<AethersWhisperRefractionBeam>(), damage, AethersWhisperBalance.KnockBack,
                        Projectile.owner, 0f, 0f);
                    if (Main.projectile.IndexInRange(beam))
                    {
                        Main.projectile[beam].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                        Main.projectile[beam].netUpdate = true;
                    }
                }
            }

            // 只保留一点点后坐「动画」，无后坐力（不推玩家）。
            rightFlashTimer = 8;
            recoilOffset = Math.Max(recoilOffset, 9f);
            starPhaseKick = (starPhaseKick + MathHelper.Pi / 5f) % MathHelper.TwoPi;

            SoundEngine.PlaySound(SoundID.Item91 with { Volume = 0.5f, Pitch = 0.1f + scatterIndex * 0.08f, MaxInstances = 4 }, tip);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.32f, Pitch = 0.3f + scatterIndex * 0.05f, MaxInstances = 3 }, tip);

            SpawnScatterMuzzle(tip, aim, scatterIndex);
        }

        // 散射枪口爆发：军械库同频、密度更高——拉宽脉冲环×2 + 白核强闪 + 扇形 CustomSpark 光条 + 硬光方块 + 电弧/方块尘。
        private void SpawnScatterMuzzle(Vector2 muzzle, Vector2 aim, int scatterIndex)
        {
            if (Main.dedServ) return;
            Color c = AethersWhisperVisuals.Lerp(0.3f + scatterIndex * 0.25f);

            GeneralParticleHandler.SpawnParticle(new CustomSpark(muzzle, aim * 10f,
                AethersWhisperVisuals.PulseRingAltTex, false, 14, 0.055f, c, new Vector2(2.3f, 0.6f), true, false, shrinkSpeed: 0.22f));
            GeneralParticleHandler.SpawnParticle(new CustomSpark(muzzle, aim * 4f,
                AethersWhisperVisuals.PulseRingAltTex, false, 11, 0.035f, AethersWhisperVisuals.ToWhite(c, 0.35f), new Vector2(1.6f, 0.5f), true, false, shrinkSpeed: 0.3f));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(muzzle, aim * 0.4f,
                AethersWhisperVisuals.ToWhite(c, 0.5f), 0.3f, 10));

            // 扇形前向能量喷（呼应散射的角度）
            for (int i = 0; i < 7; i++)
            {
                Vector2 vel = aim.RotatedBy(MathHelper.Lerp(-AethersWhisperBalance.ScatterSpread, AethersWhisperBalance.ScatterSpread, i / 6f)) * Main.rand.NextFloat(6f, 12f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(muzzle, vel,
                    "CalamityMod/Particles/BloomCircle", false, Main.rand.Next(10, 16), Main.rand.NextFloat(0.08f, 0.12f),
                    AethersWhisperVisuals.ToWhite(c, Main.rand.NextFloat(0.4f)), new Vector2(0.5f, 1.7f), true, true,
                    glowCenterScale: 0.6f, shrinkSpeed: 0.2f));
            }
            for (int i = 0; i < 4; i++)
            {
                Vector2 vel = aim.RotatedByRandom(AethersWhisperBalance.ScatterSpread) * Main.rand.NextFloat(4f, 9f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(muzzle, vel,
                    AethersWhisperVisuals.GlowSquareTex, false, Main.rand.Next(12, 18), Main.rand.NextFloat(0.07f, 0.11f),
                    AethersWhisperVisuals.ToWhite(AethersWhisperVisuals.AetherPurple, 0.25f), new Vector2(1f, 1f), true, false, spin: Main.rand.NextFloat(-0.25f, 0.25f)));
            }
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(muzzle, Main.rand.NextBool() ? AethersWhisperVisuals.ElectricDust : AethersWhisperVisuals.HardLightDust,
                    aim.RotatedByRandom(AethersWhisperBalance.ScatterSpread + 0.1f) * Main.rand.NextFloat(4f, 13f), 0, c, Main.rand.NextFloat(0.9f, 1.4f));
                d.noGravity = true;
            }
        }
    }
}
