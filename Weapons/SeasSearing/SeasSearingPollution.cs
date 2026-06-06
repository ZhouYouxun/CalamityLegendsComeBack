using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingRadiationBuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }

    internal sealed class SeasSearingPollutionNPC : GlobalNPC
    {
        public const int PollutionDuration = 16 * 60;
        public const int HighStackSpreadThreshold = 12;
        public const int PracticalStackLimit = 9999;

        private int pollutionTimeLeft;
        private int pollutionStacks;
        private int pollutionOwner = -1;
        private int spreadCooldown;
        private int pressureExposureTimer;
        private float visualPulse;

        public override bool InstancePerEntity => true;

        public int PollutionStacks => pollutionTimeLeft > 0 ? pollutionStacks : 0;
        public int PollutionOwner => pollutionOwner;

        public void ApplyPollution(NPC npc, int owner, int amount, int duration = PollutionDuration, bool fromSpread = false)
        {
            NPC carrier = GetPollutionCarrier(npc);
            SeasSearingPollutionNPC carrierState = carrier.GetGlobalNPC<SeasSearingPollutionNPC>();
            carrierState.ApplyDirect(carrier, owner, amount, duration, fromSpread);

            if (carrier.whoAmI != npc.whoAmI)
            {
                pollutionOwner = owner;
                pollutionTimeLeft = Math.Max(pollutionTimeLeft, Math.Min(duration, 60));
                visualPulse = Math.Max(visualPulse, 0.55f);
            }
        }

        public void ExposeToPressure(NPC npc, int owner, float proximity)
        {
            pressureExposureTimer++;
            if (pressureExposureTimer < 44)
                return;

            pressureExposureTimer = 0;
            int amount = proximity > 0.72f ? 2 : 1;
            ApplyPollution(npc, owner, amount, 7 * 60, fromSpread: true);
        }

        public static int CountPollutionForOwner(int owner)
        {
            int total = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active)
                    continue;

                SeasSearingPollutionNPC pollution = npc.GetGlobalNPC<SeasSearingPollutionNPC>();
                if (pollution.PollutionOwner == owner)
                    total += pollution.PollutionStacks;
            }

            return total;
        }

        public static int DetonateAll(Player owner, int baseDamage, Vector2 pulseOrigin)
        {
            int detonated = 0;
            bool[] consumed = new bool[Main.maxNPCs];

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active)
                    continue;

                NPC carrier = GetPollutionCarrier(npc);
                if (!Main.npc.IndexInRange(carrier.whoAmI) || consumed[carrier.whoAmI])
                    continue;

                consumed[carrier.whoAmI] = true;
                SeasSearingPollutionNPC pollution = carrier.GetGlobalNPC<SeasSearingPollutionNPC>();
                if (pollution.PollutionStacks <= 0)
                    continue;

                if (pollution.PollutionOwner != owner.whoAmI && pollution.PollutionOwner != -1)
                    continue;

                pollution.DetonateCarrier(carrier, owner, baseDamage, pulseOrigin);
                detonated++;
            }

            return detonated;
        }

        public override void PostAI(NPC npc)
        {
            if (pollutionTimeLeft <= 0)
            {
                pollutionStacks = 0;
                pollutionOwner = -1;
            }
            else
            {
                pollutionTimeLeft--;
                MaintainBuff(npc);
                ApplyMovementDegradation(npc);
                TrySpreadPollution(npc);
            }

            if (visualPulse > 0f)
                visualPulse = MathHelper.Clamp(visualPulse - 0.035f, 0f, 1f);

            EmitPollutionLeak(npc);
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (PollutionStacks <= 0)
                return;

            float intensity = GetIntensity();
            modifiers.DefenseEffectiveness *= 1f - intensity * 0.42f;
            modifiers.ScalingArmorPenetration += intensity * 0.08f;
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (PollutionStacks <= 0)
                return;

            int dot = Math.Min(240, 8 + PollutionStacks / 2);
            if (npc.lifeRegen > 0)
                npc.lifeRegen = 0;

            npc.lifeRegen -= dot;
            damage = Math.Max(damage, Math.Max(2, dot / 8));
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (PollutionStacks <= 0 && visualPulse <= 0f)
                return;

            float intensity = Math.Max(GetIntensity(), visualPulse);
            Color color = SeasSearingPalette.PollutionColor(intensity);
            drawColor = Color.Lerp(drawColor, color, 0.18f + intensity * 0.34f);
            Lighting.AddLight(npc.Center, color.ToVector3() * (0.12f + intensity * 0.42f));

            if (Main.dedServ || Main.rand.NextFloat() > 0.08f + intensity * 0.18f)
                return;

            Dust dust = Dust.NewDustPerfect(
                npc.Center + Main.rand.NextVector2Circular(npc.width * 0.44f, npc.height * 0.44f),
                Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald,
                -Vector2.UnitY.RotatedByRandom(0.7f) * Main.rand.NextFloat(0.35f, 1.4f),
                145,
                color,
                Main.rand.NextFloat(0.55f, 1.05f));
            dust.noGravity = true;
        }

        private void ApplyDirect(NPC npc, int owner, int amount, int duration, bool fromSpread)
        {
            if (amount <= 0)
                return;

            pollutionOwner = owner;
            pollutionTimeLeft = Math.Max(pollutionTimeLeft, duration);
            pollutionStacks = Math.Min(pollutionStacks + amount, PracticalStackLimit);
            visualPulse = Math.Max(visualPulse, fromSpread ? 0.4f : 0.85f);

            int buffType = ModContent.BuffType<SeasSearingRadiationBuff>();
            npc.AddBuff(buffType, pollutionTimeLeft);

            if (!fromSpread)
            {
                npc.AddBuff(BuffID.Venom, 180);
                npc.AddBuff(ModContent.BuffType<Irradiated>(), 180);
            }
        }

        private void MaintainBuff(NPC npc)
        {
            int buffType = ModContent.BuffType<SeasSearingRadiationBuff>();
            int buffIndex = npc.FindBuffIndex(buffType);
            if (buffIndex >= 0)
                npc.buffTime[buffIndex] = Math.Max(npc.buffTime[buffIndex], pollutionTimeLeft);
            else
                npc.AddBuff(buffType, pollutionTimeLeft);
        }

        private void ApplyMovementDegradation(NPC npc)
        {
            float intensity = GetIntensity();
            float drag = MathHelper.Lerp(0.018f, 0.105f, intensity);
            if (npc.boss || npc.knockBackResist <= 0f)
                drag *= 0.36f;

            npc.velocity *= 1f - drag;
            if (Math.Abs(npc.rotation) > 0.01f && !npc.boss)
                npc.rotation *= 1f - drag * 0.55f;
        }

        private void TrySpreadPollution(NPC npc)
        {
            if (PollutionStacks < HighStackSpreadThreshold)
                return;

            if (spreadCooldown > 0)
            {
                spreadCooldown--;
                return;
            }

            spreadCooldown = Math.Max(36, 104 - Math.Min(PollutionStacks, 80));
            int amount = Math.Clamp(PollutionStacks / 9, 2, 10);
            float radius = MathHelper.Clamp(210f + PollutionStacks * 2.2f, 240f, 520f);
            float radiusSquared = radius * radius;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other.whoAmI == npc.whoAmI || !other.CanBeChasedBy())
                    continue;

                if (Vector2.DistanceSquared(npc.Center, other.Center) > radiusSquared)
                    continue;

                other.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(other, pollutionOwner, amount, 9 * 60, fromSpread: true);
                SpawnSpreadLink(npc.Center, other.Center, amount);
            }
        }

        private void DetonateCarrier(NPC npc, Player owner, int baseDamage, Vector2 pulseOrigin)
        {
            int stacks = PollutionStacks;
            bool worm = HasWormSegments(npc);
            int mode = worm ? 2 : npc.boss ? 1 : 0;
            int damage = ComputeDetonationDamage(baseDamage, stacks, mode);
            float knockback = mode == 0 ? 6.5f : 2.5f;

            Projectile.NewProjectile(
                npc.GetSource_FromThis(),
                npc.Center,
                Vector2.Zero,
                ModContent.ProjectileType<SeasSearingDetonationBlast>(),
                damage,
                knockback,
                owner.whoAmI,
                stacks,
                mode);

            if (worm)
                SpawnWormResonance(npc, owner, stacks);
            else if (!npc.boss)
                SpreadOnDetonation(npc, owner, stacks);

            SeasSearingVisualUtility.SpawnPressureRing(npc.Center, 4.8f + Math.Min(stacks, 80) * 0.035f, 10f, 34, SeasSearingPalette.RadioactiveCyan);
            SeasSearingVisualUtility.ShakeAt(npc.Center, mode == 0 ? 3.6f : 7.2f);
            ClearPollution(npc);
        }

        private void SpreadOnDetonation(NPC npc, Player owner, int stacks)
        {
            int amount = Math.Clamp(stacks / 5, 3, 24);
            float radius = MathHelper.Clamp(240f + stacks * 4f, 300f, 650f);
            float radiusSquared = radius * radius;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other.whoAmI == npc.whoAmI || !other.CanBeChasedBy())
                    continue;

                if (Vector2.DistanceSquared(npc.Center, other.Center) > radiusSquared)
                    continue;

                other.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(other, owner.whoAmI, amount, 12 * 60, fromSpread: true);
                if (stacks >= 22 && Main.myPlayer == owner.whoAmI)
                {
                    Projectile.NewProjectile(
                        npc.GetSource_FromThis(),
                        other.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<SeasSearingFalloutCloud>(),
                        Math.Max(1, ComputeDetonationDamage(owner.HeldItem.damage, amount, 0) / 3),
                        0f,
                        owner.whoAmI,
                        amount);
                }

                SpawnSpreadLink(npc.Center, other.Center, amount);
            }
        }

        private void SpawnWormResonance(NPC head, Player owner, int stacks)
        {
            if (Main.dedServ)
                return;

            int emitted = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC segment = Main.npc[i];
                if (!segment.active)
                    continue;

                bool sameWorm = segment.whoAmI == head.whoAmI || segment.realLife == head.whoAmI;
                if (!sameWorm)
                    continue;

                emitted++;
                segment.GetGlobalNPC<SeasSearingPollutionNPC>().visualPulse = 1f;
                if (emitted % 2 == 0)
                    SpawnSpreadLink(segment.Center, head.Center, Math.Min(stacks, 60));

                for (int d = 0; d < 3; d++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        segment.Center + Main.rand.NextVector2Circular(segment.width * 0.35f, segment.height * 0.35f),
                        DustID.GemEmerald,
                        Main.rand.NextVector2Circular(2.6f, 2.6f),
                        105,
                        SeasSearingPalette.RadioactiveCyan,
                        Main.rand.NextFloat(0.78f, 1.35f));
                    dust.noGravity = true;
                }
            }
        }

        private void ClearPollution(NPC npc)
        {
            pollutionStacks = 0;
            pollutionTimeLeft = 0;
            spreadCooldown = 0;
            pressureExposureTimer = 0;
            visualPulse = 0f;
            pollutionOwner = -1;

            int buffType = ModContent.BuffType<SeasSearingRadiationBuff>();
            int buffIndex = npc.FindBuffIndex(buffType);
            if (buffIndex >= 0)
                npc.DelBuff(buffIndex);
        }

        private static int ComputeDetonationDamage(int baseDamage, int stacks, int mode)
        {
            float stackPower = Math.Min(stacks, 260);
            float multiplier = 0.65f + stackPower * 0.105f + MathF.Sqrt(stackPower) * 0.34f;

            if (mode == 1)
                multiplier *= 1.55f;
            else if (mode == 2)
                multiplier *= 1.78f;
            else
                multiplier *= 0.78f;

            return Math.Max(1, (int)(baseDamage * multiplier));
        }

        private float GetIntensity()
        {
            if (pollutionStacks <= 0 || pollutionTimeLeft <= 0)
                return 0f;

            return MathHelper.Clamp(pollutionStacks / 60f, 0.08f, 1f);
        }

        private void EmitPollutionLeak(NPC npc)
        {
            if (Main.dedServ || PollutionStacks <= 0)
                return;

            int interval = PollutionStacks >= 45 ? 6 : PollutionStacks >= 20 ? 10 : 16;
            if (Main.GameUpdateCount % interval != 0)
                return;

            float intensity = GetIntensity();
            Vector2 position = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.42f, npc.height * 0.42f);
            Dust bubble = Dust.NewDustPerfect(
                position,
                DustID.Water,
                -Vector2.UnitY.RotatedByRandom(0.9f) * Main.rand.NextFloat(0.35f, 1.35f),
                120,
                Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, intensity),
                Main.rand.NextFloat(0.55f, 1.05f));
            bubble.noGravity = true;
        }

        private static void SpawnSpreadLink(Vector2 start, Vector2 end, int strength)
        {
            if (Main.dedServ)
                return;

            Vector2 line = end - start;
            float length = line.Length();
            if (length <= 1f)
                return;

            Vector2 direction = line / length;
            int count = Math.Clamp((int)(length / 42f), 3, 18);
            for (int i = 0; i < count; i++)
            {
                float progress = (i + Main.rand.NextFloat()) / count;
                Vector2 position = Vector2.Lerp(start, end, progress) + direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-12f, 12f);
                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.GemEmerald,
                    direction.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.4f, 2.2f),
                    120,
                    Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.ToxicGreen, MathHelper.Clamp(strength / 24f, 0f, 1f)),
                    Main.rand.NextFloat(0.6f, 1.15f));
                dust.noGravity = true;
            }
        }

        private static bool HasWormSegments(NPC npc)
        {
            if (npc.realLife >= 0)
                return true;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC segment = Main.npc[i];
                if (segment.active && segment.realLife == npc.whoAmI)
                    return true;
            }

            return false;
        }

        internal static NPC GetPollutionCarrier(NPC npc)
        {
            if (npc.realLife >= 0 && Main.npc.IndexInRange(npc.realLife) && Main.npc[npc.realLife].active)
                return Main.npc[npc.realLife];

            return npc;
        }
    }
}
