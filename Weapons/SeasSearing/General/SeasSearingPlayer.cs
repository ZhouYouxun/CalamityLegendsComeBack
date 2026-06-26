using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    public sealed class SeasSearingPlayer : ModPlayer
    {
        public const int UltimateCooldownFrames    = 75 * 60;
        private const float BasePressureRadius     = 430f;
        private const float MaxPressureRadiusBonus = 170f;

        public bool  HoldingSeasSearing  { get; private set; }
        public int   UltimateCooldown    { get; private set; }
        public float PressureVisualPower { get; private set; }

        public override void ResetEffects() => HoldingSeasSearing = false;

        public override void PostUpdate()
        {
            if (UltimateCooldown > 0) UltimateCooldown--;

            if (!HoldingSeasSearing)
            {
                PressureVisualPower = MathHelper.Clamp(PressureVisualPower - 0.04f, 0f, 1f);
                return;
            }

            int   totalPollution  = SeasSearingPollutionNPC.CountPollutionForOwner(Player.whoAmI);
            float pollutionFactor = MathHelper.Clamp(totalPollution / 200f, 0f, 1f);
            PressureVisualPower   = MathHelper.Lerp(PressureVisualPower, 0.32f + pollutionFactor * 0.68f, 0.08f);

            ApplyPressureField(pollutionFactor);
            EmitPressureAtmosphere(pollutionFactor);
        }

        public void SetHoldingSeasSearing() => HoldingSeasSearing = true;

        public bool CanUseUltimate => UltimateCooldown <= 0;

        public void StartUltimateCooldown() => UltimateCooldown = UltimateCooldownFrames;

        private void ApplyPressureField(float pollutionFactor)
        {
            float radius        = BasePressureRadius + MaxPressureRadiusBonus * pollutionFactor;
            float radiusSquared = radius * radius;
            int   owner         = Player.whoAmI;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy()) continue;

                float distanceSquared = Vector2.DistanceSquared(Player.Center, npc.Center);
                if (distanceSquared > radiusSquared) continue;

                float proximity  = 1f - MathHelper.Clamp((float)Math.Sqrt(distanceSquared) / radius, 0f, 1f);
                float slowPower  = MathHelper.Lerp(0.035f, 0.13f, proximity) * MathHelper.Lerp(0.6f, 1.25f, pollutionFactor);
                if (npc.knockBackResist <= 0f || npc.boss) slowPower *= 0.38f;

                npc.position -= npc.velocity * slowPower;
                npc.velocity *= 1f - slowPower * 0.35f;

                SeasSearingPollutionNPC pollution = npc.GetGlobalNPC<SeasSearingPollutionNPC>();
                pollution.ExposeToPressure(npc, owner, proximity);
            }
        }

        private void EmitPressureAtmosphere(float pollutionFactor)
        {
            if (Main.dedServ) return;
            Lighting.AddLight(Player.Center, new Vector3(0.03f, 0.16f, 0.22f) * (0.8f + pollutionFactor));

            int interval = pollutionFactor > 0.6f ? 4 : 7;
            if (Main.GameUpdateCount % interval != 0) return;

            float   radius   = BasePressureRadius * Main.rand.NextFloat(0.58f, 1.04f);
            Vector2 offset   = Main.rand.NextVector2CircularEdge(radius, radius * Main.rand.NextFloat(0.5f, 0.95f));
            Vector2 position = Player.Center + offset;
            Vector2 velocity = -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.35f, 1.1f);
            Color   color    = Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, Main.rand.NextFloat(0.12f, 0.75f));

            Dust dust = Dust.NewDustPerfect(position,
                Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald,
                velocity, 135, color, Main.rand.NextFloat(0.55f, 1.05f));
            dust.noGravity = true;
        }
    }
}
