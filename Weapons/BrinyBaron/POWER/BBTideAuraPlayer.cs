using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.POWER
{
    internal class BBTideAuraPlayer : ModPlayer
    {
        private float visualStrength;

        public override void ResetEffects()
        {
            int tideValue = Player.GetModPlayer<BBEXPlayer>().TideValue;
            Player.moveSpeed += tideValue * 0.04f;
        }

        public override void PostUpdate()
        {
            int tideValue = Player.GetModPlayer<BBEXPlayer>().TideValue;
            float targetStrength = MathHelper.Clamp(tideValue / 8f, 0f, 1f);
            visualStrength = MathHelper.Lerp(visualStrength, targetStrength, 0.12f);

            if (visualStrength <= 0.02f || Main.dedServ)
                return;

            int dustChance = Math.Max(1, 10 - tideValue);
            if (!Main.rand.NextBool(dustChance))
                return;

            Vector2 orbitBase = new Vector2(0f, -18f - tideValue * 1.5f);
            Vector2 orbitOffset = orbitBase.RotatedBy(Main.GlobalTimeWrappedHourly * (1.2f + visualStrength) + Player.whoAmI * 0.7f);
            orbitOffset.X *= 0.75f;

            Vector2 spawnPos = Player.Center + orbitOffset + Player.velocity * 0.15f;
            Vector2 driftVelocity = (-orbitOffset).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.3f, 1.1f) + Player.velocity * 0.08f;
            int dustType = Main.rand.NextBool(3) ? DustID.Frost : DustID.Water;

            Dust dust = Dust.NewDustPerfect(
                spawnPos,
                dustType,
                driftVelocity,
                100,
                Color.Lerp(new Color(70, 170, 255), new Color(210, 245, 255), visualStrength),
                MathHelper.Lerp(0.75f, 1.25f, visualStrength));
            dust.noGravity = true;

            if (tideValue >= 5 && Main.rand.NextBool(3))
            {
                Vector2 secondOffset = orbitBase.RotatedBy(-Main.GlobalTimeWrappedHourly * (1f + visualStrength) - Player.whoAmI * 0.45f);
                secondOffset.X *= 0.9f;

                Dust shimmer = Dust.NewDustPerfect(
                    Player.Center + secondOffset,
                    DustID.GemSapphire,
                    Player.velocity * 0.05f,
                    100,
                    new Color(120, 215, 255),
                    MathHelper.Lerp(0.55f, 0.95f, visualStrength));
                shimmer.noGravity = true;
            }
        }
    }
}
