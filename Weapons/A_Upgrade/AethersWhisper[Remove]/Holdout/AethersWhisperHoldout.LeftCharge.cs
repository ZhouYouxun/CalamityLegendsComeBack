using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.LeftClick;
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
        // ===== 左键：微光坍缩炮 =====

        private void AdvanceLeftCharge()
        {
            if (chargeTicks == 0)
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.4f, Pitch = -0.6f }, GunTip);

            chargeTicks = Math.Min(chargeTicks + 1, AethersWhisperBalance.FullChargeTicks);

            ApplyMovePenalty();
            SpawnChargeConvergence();
            PlayChargeSounds();
        }

        private void CancelLeftCharge()
        {
            chargeTicks = 0;
            lastPulseStep = -1;
            playedFullReady = false;
        }

        private void ApplyMovePenalty()
        {
            float m = chargeTicks >= AethersWhisperBalance.TierCriticalTicks ? AethersWhisperBalance.CriticalMoveSpeedMult
                    : chargeTicks >= AethersWhisperBalance.TierStableTicks ? AethersWhisperBalance.StableMoveSpeedMult
                    : 1f;
            if (m >= 1f) return;
            Owner.moveSpeed *= m; Owner.maxRunSpeed *= m; Owner.accRunSpeed *= m;
        }

        private void PlayChargeSounds()
        {
            if (!IsFullCharge) return;
            if (!playedFullReady)
            {
                playedFullReady = true;
                SoundEngine.PlaySound(SoundID.Item82 with { Volume = 0.45f, Pitch = 0.35f }, GunTip);
            }
            int step = (int)(Main.GameUpdateCount / 20);
            if (step != lastPulseStep)
            {
                lastPulseStep = step;
                SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.22f, Pitch = 0.5f }, GunTip);
            }
        }

        // 蓄力前摇：微光薄膜从准星方向被吸回枪口——一圈能量点向心汇聚 + 硬光碎屑绕枪口公转。
        private void SpawnChargeConvergence()
        {
            if (Main.dedServ) return;
            float charge = AethersWhisperBalance.ChargeProgress(chargeTicks);
            Vector2 aim = AimDirection;
            Vector2 tip = GunTip;

            // 向心汇聚的青紫能量点（越蓄越密、越靠近枪口）
            int every = IsFullCharge ? 2 : (charge > 0.5f ? 3 : 5);
            if (chargeTicks % every == 0 && AethersWhisperVisuals.CanSpawnGroup(2))
            {
                float dist = MathHelper.Lerp(120f, 26f, charge);
                Vector2 edge = tip + aim.RotatedByRandom(0.9f) * Main.rand.NextFloat(dist * 0.7f, dist);
                Vector2 inward = (tip - edge).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2.2f, 4.6f);
                Color c = AethersWhisperVisuals.Lerp(Main.rand.NextFloat());
                GeneralParticleHandler.SpawnParticle(new CustomSpark(edge, inward,
                    "CalamityMod/Particles/BloomCircle", false, Main.rand.Next(14, 20),
                    Main.rand.NextFloat(0.09f, 0.15f) * (0.8f + charge), c, new Vector2(0.7f, 1.4f),
                    true, true, glowCenterScale: 0.7f, shrinkSpeed: 0.08f));
            }

            // 硬光晶片碎屑绕枪口公转（军械库 SquareDust 硬光质感）
            if (chargeTicks % 3 == 0)
            {
                float ang = Main.GameUpdateCount * 0.25f + chargeTicks;
                Vector2 orbit = ang.ToRotationVector2() * MathHelper.Lerp(30f, 12f, charge);
                Dust d = Dust.NewDustPerfect(tip + orbit, AethersWhisperVisuals.HardLightDust,
                    orbit.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * 1.6f, 60,
                    AethersWhisperVisuals.ToWhite(AethersWhisperVisuals.AetherPurple, charge * 0.4f),
                    Main.rand.NextFloat(0.9f, 1.3f) * (0.7f + charge));
                d.noGravity = true;
            }

            // 满蓄：所有环收成一个珠白点前的最后一拍——偶发一记向心电弧尘
            if (IsFullCharge && Main.rand.NextBool(3))
            {
                Vector2 edge = tip + Main.rand.NextVector2CircularEdge(22f, 22f);
                Dust d = Dust.NewDustPerfect(edge, AethersWhisperVisuals.ElectricDust,
                    (tip - edge).SafeNormalize(Vector2.Zero) * 4f, 40, AethersWhisperVisuals.ShimmerCyan, 1.1f);
                d.noGravity = true;
            }
        }

        private void ReleaseLeftCharge()
        {
            // 未达最小蓄力：取消，不耗魔、不发射。
            if (chargeTicks < AethersWhisperBalance.MinChargeTicks)
            {
                CancelLeftCharge();
                return;
            }

            if (!Owner.CheckMana(AethersWhisperBalance.LeftManaCost, true))
            {
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.3f, Pitch = -0.5f }, Owner.Center);
                CancelLeftCharge();
                return;
            }

            float charge = AethersWhisperBalance.ChargeProgress(chargeTicks);
            bool full = IsFullCharge;
            Vector2 aim = AimDirection;
            float speed = AethersWhisperBalance.ChargedShotSpeed(charge) * AethersWhisperBalance.ChargedShotSpeedMult;
            int weaponDamage = Owner.GetWeaponDamage(Owner.HeldItem);
            int damage = Math.Max(1, (int)(weaponDamage * AethersWhisperBalance.ChargeDamageMultiplier(charge)));

            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 spawn = GetSafeMuzzle(aim);
                int shot = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, aim * speed,
                    ModContent.ProjectileType<AethersWhisperChargedShot>(), damage, AethersWhisperBalance.KnockBack,
                    Projectile.owner, charge, full ? 1f : 0f);
                if (Main.projectile.IndexInRange(shot))
                {
                    Main.projectile[shot].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                    Main.projectile[shot].netUpdate = true;
                }
            }

            // 只保留枪体后坐「动画」，不再推动玩家（无后坐力）；满蓄仍保留屏震表现重量。
            recoilOffset = full ? 18f : 8f + charge * 5f;
            recoilTimer = AethersWhisperBalance.FullChargeRecoilTicks;
            muzzleFlashTimer = full ? 14 : 9;
            starPhaseKick = (starPhaseKick + MathHelper.PiOver4) % MathHelper.TwoPi;

            if (full)
            {
                Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, AethersWhisperBalance.FullChargeScreenShake);
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.5f, Pitch = -0.9f }, GunTip);
                SoundEngine.PlaySound(SoundID.DD2_BetsysWrathImpact with { Volume = 0.7f, Pitch = -0.35f }, GunTip);
            }
            else
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.55f, Pitch = -0.2f - charge * 0.2f }, GunTip);

            SpawnLaunchBurst(aim, charge, full);
            CancelLeftCharge();
        }

        // 发射瞬间的枪口爆发（军械库同频：拉宽脉冲环 + 强闪 + CustomSpark 能量喷 + 硬光方块 + 签名尘）。
        private void SpawnLaunchBurst(Vector2 aim, float charge, bool full)
        {
            if (Main.dedServ) return;
            Vector2 muzzle = GunTip + aim * 4f;
            Vector2 right = aim.RotatedBy(MathHelper.PiOver2);
            float power = 0.75f + charge * 0.9f;
            Color cyan = AethersWhisperVisuals.ShimmerCyan;
            Color purple = AethersWhisperVisuals.AetherPurple;

            // 拉宽脉冲环（军械库 HighResHollowCircleHardEdgeAlt 横向 stretch）+ 反向气浪环
            GeneralParticleHandler.SpawnParticle(new CustomSpark(muzzle, aim * 6f,
                AethersWhisperVisuals.PulseRingAltTex,
                false, 16, 0.05f + charge * 0.05f, cyan, new Vector2(2.2f, 0.7f), true, false, shrinkSpeed: 0.2f));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(muzzle, -aim * 0.4f, cyan,
                new Vector2(0.35f, 1.5f), aim.ToRotation(), 0.05f, (0.8f + charge * 0.8f) * power, full ? 22 : 16));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(muzzle, -aim * 0.15f, purple,
                new Vector2(0.28f, 1.15f), aim.ToRotation(), 0.03f, (0.45f + charge * 0.5f) * power, full ? 15 : 11));

            // 白核强闪
            GeneralParticleHandler.SpawnParticle(new StrongBloom(muzzle, aim * 0.3f,
                AethersWhisperVisuals.ToWhite(cyan, 0.6f), (0.35f + charge * 0.55f), full ? 14 : 10));

            // 前向能量喷（CustomSpark 双绘制光条 + 硬光方块碎片）
            int sparks = full ? 10 : 6;
            for (int i = 0; i < sparks; i++)
            {
                Vector2 vel = aim.RotatedByRandom(0.5f) * Main.rand.NextFloat(5f, full ? 13f : 9f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(muzzle, vel,
                    "CalamityMod/Particles/BloomCircle", false, Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.1f, 0.18f) * power, AethersWhisperVisuals.Lerp(Main.rand.NextFloat()),
                    new Vector2(0.55f, 1.6f), true, true, glowCenterScale: 0.6f, shrinkSpeed: 0.12f));
            }
            for (int i = 0; i < (full ? 6 : 3); i++)
            {
                Vector2 vel = aim.RotatedByRandom(0.7f) * Main.rand.NextFloat(3f, 8f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(muzzle, vel,
                    AethersWhisperVisuals.GlowSquareTex, false, Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.08f, 0.14f) * power, AethersWhisperVisuals.ToWhite(purple, 0.3f),
                    new Vector2(1f, 1f), true, false, spin: Main.rand.NextFloat(-0.2f, 0.2f)));
            }

            // 军械库签名尘：脉冲空心尘 + 电弧尘 + 可染色烟花尘
            for (int i = 0; i < (full ? 16 : 9); i++)
            {
                int type = Main.rand.NextBool(3) ? AethersWhisperVisuals.ElectricDust : (Main.rand.NextBool() ? AethersWhisperVisuals.PulseDust : AethersWhisperVisuals.ArsenalFireworkDust);
                Dust d = Dust.NewDustPerfect(muzzle, type, aim.RotatedByRandom(0.35f) * Main.rand.NextFloat(3f, full ? 11f : 7f),
                    0, AethersWhisperVisuals.Lerp(Main.rand.NextFloat()), Main.rand.NextFloat(1f, 1.7f) * power);
                d.noGravity = true;
                d.fadeIn = 0.5f;
            }
        }
    }
}
