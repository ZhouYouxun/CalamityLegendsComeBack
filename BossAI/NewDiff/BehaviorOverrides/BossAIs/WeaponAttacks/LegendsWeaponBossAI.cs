using System;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.WeaponAttacks
{
    internal sealed class LegendsWeaponBossAI : LegendsBossAI
    {
        private const int PhaseCount = 4;

        private readonly int npcType;
        private readonly LegendsWeaponBossProfile profile;

        public LegendsWeaponBossAI(int npcType, LegendsWeaponBossProfile profile)
        {
            this.npcType = npcType;
            this.profile = profile;
        }

        public override int NPCType => npcType;

        public override string BossName => profile.DisplayName;

        public override Color DebugColor => profile.ThemeColor;

        public override float[] PhaseLifeRatios => new[] { 0.72f, 0.48f, 0.24f };

        public override bool PreAI(NPC npc, LegendsGlobalNPC data)
        {
            if (profile.Attacks.Length <= 0)
                return true;

            if (npc.target < 0 || npc.target >= Main.maxPlayers || !Main.player[npc.target].active || Main.player[npc.target].dead)
                npc.TargetClosest(false);

            if (npc.target < 0 || npc.target >= Main.maxPlayers)
                return true;

            Player target = Main.player[npc.target];
            if (!target.active || target.dead)
                return true;

            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.dontTakeDamage = false;
            npc.netAlways = true;

            UpdatePhase(npc, data);

            if (data.TransitionTimer > 0)
            {
                data.TransitionTimer--;
                data.AttackState = LegendsAttackState.PhaseShift;
                MoveBoss(npc, target, data, LegendsWeaponAttackPattern.SpaceRift);
                return false;
            }

            LegendsWeaponBossAttack attack = CurrentAttack(data);
            LegendsWeaponAttackPattern pattern = ResolvePattern(attack);

            MoveBoss(npc, target, data, pattern);

            if (data.PatternTimer == 0)
                StartAttack(npc, profile, attack, pattern, data);

            data.PatternTimer++;
            data.AttackTimer++;
            ExecuteAttack(npc, target, profile, pattern, data.PatternTimer);

            int duration = GetAttackDuration(pattern);
            if (data.PatternTimer >= duration)
            {
                data.PatternTimer = 0;
                data.AttackIndex++;
                data.BroadcastedAttackIndex = -1;
                npc.netUpdate = true;
            }

            return false;
        }

        public override void PostAI(NPC npc, LegendsGlobalNPC data)
        {
        }

        public override string PhaseName(int phase)
        {
            return phase switch
            {
                1 => "Weapon Memory I",
                2 => "Weapon Memory II",
                3 => "Weapon Memory III",
                4 => "Weapon Memory IV",
                _ => "Weapon Memory"
            };
        }

        public override string StateName(LegendsGlobalNPC data)
        {
            if (data.AttackState == LegendsAttackState.PhaseShift)
                return "Weapon Phase Shift";

            LegendsWeaponBossAttack attack = CurrentAttack(data);
            LegendsWeaponAttackPattern pattern = ResolvePattern(attack);
            return $"{PatternLabel(pattern)} - {attack.DisplayName}";
        }

        private void UpdatePhase(NPC npc, LegendsGlobalNPC data)
        {
            int nextPhase = CalculatePhase(npc);
            if (data.CurrentPhase == nextPhase)
                return;

            data.CurrentPhase = nextPhase;
            data.AttackTimer = 0;
            data.PatternTimer = 0;
            data.AttackIndex = 0;
            data.TransitionTimer = 36;
            data.AttackState = LegendsAttackState.PhaseShift;
            data.BroadcastedAttackIndex = -1;
            npc.netUpdate = true;
        }

        private int CalculatePhase(NPC npc)
        {
            float lifeRatio = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            int phase = 1;

            foreach (float threshold in PhaseLifeRatios)
            {
                if (lifeRatio <= threshold)
                    phase++;
            }

            return Math.Clamp(phase, 1, PhaseCount);
        }

        private LegendsWeaponBossAttack CurrentAttack(LegendsGlobalNPC data)
        {
            GetPhaseWindow(data.CurrentPhase, out int start, out int count);
            int index = start + PositiveModulo(data.AttackIndex, count);
            return profile.Attacks[index];
        }

        private void GetPhaseWindow(int phase, out int start, out int count)
        {
            int total = profile.Attacks.Length;
            int clampedPhase = Math.Clamp(phase, 1, PhaseCount);

            start = (clampedPhase - 1) * total / PhaseCount;
            int end = clampedPhase * total / PhaseCount;
            count = Math.Max(1, end - start);

            if (start >= total)
            {
                start = 0;
                count = total;
            }
        }

        private static int PositiveModulo(int value, int divisor)
        {
            if (divisor <= 0)
                return 0;

            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private static LegendsWeaponAttackPattern ResolvePattern(LegendsWeaponBossAttack attack)
        {
            if (attack.Pattern != LegendsWeaponAttackPattern.Auto)
                return attack.Pattern;

            string text = (attack.ItemName + " " + attack.DisplayName).ToLowerInvariant();
            if (text.Contains("void") || text.Contains("singularity") || text.Contains("horizon") || text.Contains("rupture") || text.Contains("mirror"))
                return LegendsWeaponAttackPattern.SpaceRift;
            if (text.Contains("storm") || text.Contains("thunder") || text.Contains("tesla") || text.Contains("volterion") || text.Contains("shocker"))
                return LegendsWeaponAttackPattern.LightningChain;
            if (text.Contains("star") || text.Contains("stellar") || text.Contains("astral") || text.Contains("radiant") || text.Contains("sirius") || text.Contains("vega"))
                return LegendsWeaponAttackPattern.StarField;
            if (text.Contains("blood") || text.Contains("sanguine") || text.Contains("viscera") || text.Contains("arterial"))
                return LegendsWeaponAttackPattern.BloodPulse;
            if (text.Contains("acid") || text.Contains("toxic") || text.Contains("sulph") || text.Contains("caustic") || text.Contains("septic"))
                return LegendsWeaponAttackPattern.AcidRain;
            if (text.Contains("staff") || text.Contains("scepter") || text.Contains("totem") || text.Contains("lamp"))
                return LegendsWeaponAttackPattern.SummonCore;
            if (text.Contains("cannon") || text.Contains("blaster") || text.Contains("shotgun") || text.Contains("smg") || text.Contains("fury"))
                return LegendsWeaponAttackPattern.Gunline;
            if (text.Contains("bomb") || text.Contains("flare") || text.Contains("flame") || text.Contains("inferno") || text.Contains("vesuvius"))
                return LegendsWeaponAttackPattern.BombRain;
            if (text.Contains("teeth") || text.Contains("viper") || text.Contains("eels") || text.Contains("dragon") || text.Contains("slime"))
                return LegendsWeaponAttackPattern.CreatureRush;
            if (text.Contains("knife") || text.Contains("scythe") || text.Contains("reaper") || text.Contains("throw") || text.Contains("hook"))
                return LegendsWeaponAttackPattern.ReturningBlade;

            return LegendsWeaponAttackPattern.Slash;
        }

        private static int GetAttackDuration(LegendsWeaponAttackPattern pattern)
        {
            return pattern switch
            {
                LegendsWeaponAttackPattern.SummonCore => 172,
                LegendsWeaponAttackPattern.MagicCore => 154,
                LegendsWeaponAttackPattern.SpaceRift => 156,
                LegendsWeaponAttackPattern.AcidRain => 148,
                LegendsWeaponAttackPattern.LightningChain => 144,
                LegendsWeaponAttackPattern.StarField => 150,
                LegendsWeaponAttackPattern.CreatureRush => 136,
                _ => 132
            };
        }

        private void MoveBoss(NPC npc, Player target, LegendsGlobalNPC data, LegendsWeaponAttackPattern pattern)
        {
            float t = Main.GameUpdateCount + npc.whoAmI * 29 + npc.type;
            Vector2 desiredPosition;

            switch (profile.MovementStyle)
            {
                case LegendsWeaponBossMovementStyle.Worm:
                    {
                        float speed = 13f + (1f - npc.life / (float)Math.Max(1, npc.lifeMax)) * 4.5f;
                        Vector2 desiredVelocity = LegendsWeaponBossVisuals.SafeDirection(npc.Center, target.Center + target.velocity * 16f, Vector2.UnitY) * speed;
                        npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.035f);
                        npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                        return;
                    }

                case LegendsWeaponBossMovementStyle.VoidCore:
                    desiredPosition = target.Center + new Vector2(MathF.Cos(t * 0.018f) * 360f, -180f + MathF.Sin(t * 0.023f) * 120f);
                    SmoothMove(npc, desiredPosition, 0.032f, 13f);
                    npc.rotation += 0.025f;
                    break;

                case LegendsWeaponBossMovementStyle.HeavyHover:
                    desiredPosition = target.Center + new Vector2(MathF.Sin(t * 0.015f) * 420f, -245f + MathF.Cos(t * 0.021f) * 90f);
                    SmoothMove(npc, desiredPosition, 0.038f, 16f);
                    break;

                default:
                    desiredPosition = target.Center + new Vector2(MathF.Sin(t * 0.026f) * 480f, -260f + MathF.Cos(t * 0.017f) * 110f);
                    SmoothMove(npc, desiredPosition, 0.048f, 19f);
                    break;
            }

            if ((pattern == LegendsWeaponAttackPattern.Slash || pattern == LegendsWeaponAttackPattern.CreatureRush) && data.PatternTimer is > 52 and < 70)
            {
                Vector2 dashVelocity = LegendsWeaponBossVisuals.SafeDirection(npc.Center, target.Center, Vector2.UnitX) * 20f;
                npc.velocity = Vector2.Lerp(npc.velocity, dashVelocity, 0.14f);
            }

            if (Math.Abs(npc.velocity.X) > 0.2f)
                npc.direction = npc.spriteDirection = Math.Sign(npc.velocity.X);
        }

        private static void SmoothMove(NPC npc, Vector2 desiredPosition, float acceleration, float maxSpeed)
        {
            Vector2 desiredVelocity = (desiredPosition - npc.Center) * acceleration;
            if (desiredVelocity.Length() > maxSpeed)
                desiredVelocity = Vector2.Normalize(desiredVelocity) * maxSpeed;

            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.12f);
        }

        private static void StartAttack(NPC npc, LegendsWeaponBossProfile profile, LegendsWeaponBossAttack attack, LegendsWeaponAttackPattern pattern, LegendsGlobalNPC data)
        {
            data.AttackState = pattern switch
            {
                LegendsWeaponAttackPattern.Slash or LegendsWeaponAttackPattern.CreatureRush => LegendsAttackState.VectorDash,
                LegendsWeaponAttackPattern.SummonCore or LegendsWeaponAttackPattern.SpaceRift => LegendsAttackState.OrbitLock,
                LegendsWeaponAttackPattern.BombRain or LegendsWeaponAttackPattern.AcidRain => LegendsAttackState.PhasePressure,
                _ => LegendsAttackState.MatrixHover
            };

            data.BroadcastedAttackIndex = data.AttackIndex;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnWeaponTelegraph(npc, profile, attack, npc.Center + new Vector2(0f, -npc.height * 0.66f - 34f), 118f);
            }

            SoundEngine.PlaySound(GetSound(pattern), npc.Center);
        }

        private static void ExecuteAttack(NPC npc, Player target, LegendsWeaponBossProfile profile, LegendsWeaponAttackPattern pattern, int timer)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int color = LegendsWeaponBossVisuals.PackColor(profile.ThemeColor);
            int damage = GetProjectileDamage(npc);

            switch (pattern)
            {
                case LegendsWeaponAttackPattern.Slash:
                    if (timer == 32 || timer == 76)
                        SpawnLine(npc, target.Center + target.velocity * 14f, LegendsWeaponBossVisuals.SafeDirection(npc.Center, target.Center, Vector2.UnitX).RotatedBy(timer == 32 ? 0.36f : -0.36f), color, damage, 1650f);
                    break;

                case LegendsWeaponAttackPattern.Gunline:
                    if (timer is 24 or 52 or 80)
                        FireSpread(npc, target, color, damage, 5, 8.8f, 0.18f, 0f, 0.004f);
                    break;

                case LegendsWeaponAttackPattern.MagicCore:
                    if (timer == 20 || timer == 88)
                        SpawnCore(npc, target.Center + Main.rand.NextVector2Circular(160f, 90f), color, damage, Main.rand.NextFloat(MathHelper.TwoPi));
                    if (timer == 58 || timer == 118)
                        FireRadial(npc, target.Center, color, damage, 7, 6.2f, Main.rand.NextFloat(MathHelper.TwoPi), 0f);
                    break;

                case LegendsWeaponAttackPattern.SummonCore:
                    if (timer == 18)
                    {
                        SpawnCore(npc, target.Center + new Vector2(-230f, -120f), color, damage, 0f);
                        SpawnCore(npc, target.Center + new Vector2(230f, -120f), color, damage, MathHelper.Pi);
                    }
                    break;

                case LegendsWeaponAttackPattern.ReturningBlade:
                    if (timer is 24 or 62 or 100)
                        FireSideBlades(npc, target, color, damage, 4);
                    break;

                case LegendsWeaponAttackPattern.BombRain:
                    if (timer % 26 == 14 && timer < 124)
                        DropRain(npc, target.Center, color, damage, 4, 0f);
                    break;

                case LegendsWeaponAttackPattern.StarField:
                    if (timer is 26 or 64 or 102)
                        FireStarField(npc, target.Center, color, damage, timer);
                    break;

                case LegendsWeaponAttackPattern.LightningChain:
                    if (timer is 26 or 68 or 110)
                        FireLightningField(npc, target.Center, color, damage);
                    break;

                case LegendsWeaponAttackPattern.SpaceRift:
                    if (timer == 26 || timer == 82)
                    {
                        SpawnLine(npc, target.Center + new Vector2(0f, -40f), Vector2.UnitX.RotatedBy(Main.rand.NextFloat(-0.22f, 0.22f)), color, damage, 1800f);
                        SpawnLine(npc, target.Center + new Vector2(0f, 80f), Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.28f, 0.28f)), color, damage, 1200f);
                    }
                    if (timer == 54)
                        FireRadial(npc, target.Center, color, damage, 6, 4.8f, Main.rand.NextFloat(MathHelper.TwoPi), 0.018f);
                    break;

                case LegendsWeaponAttackPattern.AcidRain:
                    if (timer % 20 == 10 && timer < 128)
                        DropRain(npc, target.Center, color, damage, 5, 0.09f);
                    break;

                case LegendsWeaponAttackPattern.CreatureRush:
                    if (timer is 24 or 66 or 108)
                        FireCreatureRush(npc, target.Center, color, damage, timer);
                    break;

                case LegendsWeaponAttackPattern.BloodPulse:
                    if (timer is 22 or 58 or 94)
                    {
                        DropRain(npc, target.Center, color, damage, 3, 0.05f);
                        FireSpread(npc, target, color, damage, 4, 7f, 0.32f, 3f, 0.012f);
                    }
                    break;
            }
        }

        private static void SpawnWeaponTelegraph(NPC npc, LegendsWeaponBossProfile profile, LegendsWeaponBossAttack attack, Vector2 position, float scale)
        {
            int itemType = LegendsWeaponBossRegistry.GetItemType(attack.ItemName);
            Projectile.NewProjectile(
                npc.GetSource_FromAI(),
                position,
                npc.velocity * 0.15f,
                ModContent.ProjectileType<LegendsWeaponTelegraphProjectile>(),
                0,
                0f,
                Main.myPlayer,
                itemType,
                LegendsWeaponBossVisuals.PackColor(profile.ThemeColor),
                scale);
        }

        private static void SpawnLine(NPC npc, Vector2 center, Vector2 direction, int packedColor, int damage, float length)
        {
            Projectile.NewProjectile(
                npc.GetSource_FromAI(),
                center,
                direction.SafeNormalize(Vector2.UnitX),
                ModContent.ProjectileType<LegendsWeaponLineHazard>(),
                damage,
                0f,
                Main.myPlayer,
                0f,
                packedColor,
                length);
        }

        private static void SpawnCore(NPC npc, Vector2 position, int packedColor, int damage, float phase)
        {
            Projectile.NewProjectile(
                npc.GetSource_FromAI(),
                position,
                Vector2.Zero,
                ModContent.ProjectileType<LegendsWeaponSummonCore>(),
                damage,
                0f,
                Main.myPlayer,
                packedColor,
                0f,
                phase);
        }

        private static void FireSpread(NPC npc, Player target, int packedColor, int damage, int count, float speed, float spread, float style, float homing)
        {
            Vector2 baseDirection = LegendsWeaponBossVisuals.SafeDirection(npc.Center, target.Center + target.velocity * 18f, Vector2.UnitY);
            float start = -spread * (count - 1) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = baseDirection.RotatedBy(start + spread * i) * speed;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, velocity, ModContent.ProjectileType<LegendsWeaponHostileBolt>(), damage, 0f, Main.myPlayer, style, packedColor, homing);
            }
        }

        private static void FireRadial(NPC npc, Vector2 center, int packedColor, int damage, int count, float speed, float rotation, float homing)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = (rotation + MathHelper.TwoPi * i / count).ToRotationVector2() * speed;
                Projectile.NewProjectile(npc.GetSource_FromAI(), center, velocity, ModContent.ProjectileType<LegendsWeaponHostileBolt>(), damage, 0f, Main.myPlayer, homing > 0f ? 1f : 0f, packedColor, homing);
            }
        }

        private static void FireSideBlades(NPC npc, Player target, int packedColor, int damage, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Vector2 spawn = target.Center + new Vector2(side * (620f + i * 20f), -180f + i * 90f);
                Vector2 velocity = LegendsWeaponBossVisuals.SafeDirection(spawn, target.Center + target.velocity * 24f, -Vector2.UnitX * side) * (7.2f + i * 0.45f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, velocity, ModContent.ProjectileType<LegendsWeaponHostileBolt>(), damage, 0f, Main.myPlayer, 1f, packedColor, 0.024f);
            }
        }

        private static void DropRain(NPC npc, Vector2 targetCenter, int packedColor, int damage, int count, float horizontalDrift)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 spawn = targetCenter + new Vector2(Main.rand.NextFloat(-480f, 480f), -520f - Main.rand.NextFloat(180f));
                Vector2 velocity = new(Main.rand.NextFloat(-1.8f, 1.8f) + horizontalDrift * Math.Sign(targetCenter.X - spawn.X), Main.rand.NextFloat(5.2f, 7.8f));
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, velocity, ModContent.ProjectileType<LegendsWeaponHostileBolt>(), damage, 0f, Main.myPlayer, 2f, packedColor, 0f);
            }
        }

        private static void FireStarField(NPC npc, Vector2 targetCenter, int packedColor, int damage, int timer)
        {
            int count = 8;
            float radius = 420f;
            float rotation = timer * 0.07f;
            for (int i = 0; i < count; i++)
            {
                Vector2 spawn = targetCenter + (rotation + MathHelper.TwoPi * i / count).ToRotationVector2() * radius;
                Vector2 velocity = LegendsWeaponBossVisuals.SafeDirection(spawn, targetCenter, Vector2.UnitY) * 6.1f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, velocity, ModContent.ProjectileType<LegendsWeaponHostileBolt>(), damage, 0f, Main.myPlayer, 0f, packedColor, 0f);
            }
        }

        private static void FireLightningField(NPC npc, Vector2 targetCenter, int packedColor, int damage)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 center = targetCenter + Main.rand.NextVector2Circular(260f, 220f);
                Vector2 direction = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
                Projectile.NewProjectile(npc.GetSource_FromAI(), center, direction, ModContent.ProjectileType<LegendsWeaponLineHazard>(), damage, 0f, Main.myPlayer, 0f, packedColor, Main.rand.NextFloat(640f, 980f));
            }
        }

        private static void FireCreatureRush(NPC npc, Vector2 targetCenter, int packedColor, int damage, int timer)
        {
            int count = 4;
            float side = timer % 2 == 0 ? -1f : 1f;
            for (int i = 0; i < count; i++)
            {
                Vector2 spawn = targetCenter + new Vector2(side * 640f, -240f + i * 150f);
                Vector2 velocity = new(-side * Main.rand.NextFloat(7f, 9.5f), Main.rand.NextFloat(-1.4f, 1.4f));
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, velocity, ModContent.ProjectileType<LegendsWeaponHostileBolt>(), damage, 0f, Main.myPlayer, 3f, packedColor, 0.006f);
            }
        }

        private static int GetProjectileDamage(NPC npc)
        {
            int damage = npc.defDamage > 0 ? npc.defDamage : npc.damage;
            if (damage <= 0)
                damage = Math.Max(1, npc.lifeMax / 1000);
            return damage;
        }

        private static string PatternLabel(LegendsWeaponAttackPattern pattern)
        {
            return pattern switch
            {
                LegendsWeaponAttackPattern.Slash => "预警斩击",
                LegendsWeaponAttackPattern.Gunline => "轴线火力",
                LegendsWeaponAttackPattern.MagicCore => "法术核心",
                LegendsWeaponAttackPattern.SummonCore => "召唤副核心",
                LegendsWeaponAttackPattern.ReturningBlade => "回返飞刃",
                LegendsWeaponAttackPattern.BombRain => "爆裂雨幕",
                LegendsWeaponAttackPattern.StarField => "星阵弹幕",
                LegendsWeaponAttackPattern.LightningChain => "连锁闪电",
                LegendsWeaponAttackPattern.SpaceRift => "空间裂隙",
                LegendsWeaponAttackPattern.AcidRain => "酸雨封场",
                LegendsWeaponAttackPattern.CreatureRush => "生物幻影冲场",
                LegendsWeaponAttackPattern.BloodPulse => "血肉脉冲",
                _ => "武器招式"
            };
        }

        private static SoundStyle GetSound(LegendsWeaponAttackPattern pattern)
        {
            return pattern switch
            {
                LegendsWeaponAttackPattern.Slash => SoundID.Item71 with { Volume = 0.7f, Pitch = -0.08f },
                LegendsWeaponAttackPattern.Gunline => SoundID.Item12 with { Volume = 0.58f, Pitch = 0.05f },
                LegendsWeaponAttackPattern.MagicCore => SoundID.Item29 with { Volume = 0.62f, Pitch = 0.1f },
                LegendsWeaponAttackPattern.SummonCore => SoundID.Item8 with { Volume = 0.6f, Pitch = -0.1f },
                LegendsWeaponAttackPattern.ReturningBlade => SoundID.Item1 with { Volume = 0.66f, Pitch = 0.18f },
                LegendsWeaponAttackPattern.BombRain => SoundID.Item20 with { Volume = 0.58f, Pitch = -0.12f },
                LegendsWeaponAttackPattern.LightningChain => SoundID.Item122 with { Volume = 0.55f, Pitch = 0.06f },
                LegendsWeaponAttackPattern.SpaceRift => SoundID.Item74 with { Volume = 0.58f, Pitch = -0.2f },
                LegendsWeaponAttackPattern.AcidRain => SoundID.Item17 with { Volume = 0.48f, Pitch = -0.25f },
                LegendsWeaponAttackPattern.BloodPulse => SoundID.NPCDeath13 with { Volume = 0.42f, Pitch = 0.15f },
                _ => SoundID.Item42 with { Volume = 0.5f }
            };
        }
    }
}
