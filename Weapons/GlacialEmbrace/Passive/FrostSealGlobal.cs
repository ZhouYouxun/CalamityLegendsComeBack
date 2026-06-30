using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.General;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.Passive
{
    public class FrostSealGlobal : GlobalNPC
    {
        public override bool InstancePerEntity => false;

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.gameMenu || Main.dedServ) return;
            Player local = Main.LocalPlayer;
            var mp = local.GetModPlayer<GlacialEmbracePlayer>();
            if (!mp.GlacialEmbraceMinion) return;
            if (!mp.SealStates.TryGetValue(npc.whoAmI, out var state)) return;
            if (!state.HasAnyMark) return;

            float time = Main.GlobalTimeWrappedHourly;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Vector2 center = npc.Center - screenPos;
            float npcRadius = Math.Max(npc.width, npc.height) * 0.5f + 18f;

            // ── mark type colors: cyan (rift), blue (pierce), orange (strike) ──
            Color[] markColors = {
                new Color(120, 235, 255, 0),
                new Color(70, 170, 255, 0),
                new Color(255, 175, 70, 0),
            };
            byte[] stacks = { state.RiftStacks, state.PierceStacks, state.StrikeStacks };
            int[] timers = { state.RiftFadeTimer, state.PierceFadeTimer, state.StrikeFadeTimer };
            int[] maxTimers = { GlacialEmbracePlayer.RiftMarkDuration, GlacialEmbracePlayer.PierceMarkDuration, GlacialEmbracePlayer.StrikeMarkDuration };
            float[] orbSpeeds = { 2.2f, -1.8f, 1.4f };

            Vector2[] typePositions = new Vector2[3];
            bool[] typePresent = new bool[3];

            for (int t = 0; t < 3; t++)
            {
                if (stacks[t] == 0) continue;
                typePresent[t] = true;

                float fadeAlpha = MathHelper.Clamp((float)timers[t] / 60f, 0f, 1f);
                Color baseCol = markColors[t] * fadeAlpha * 0.9f;
                float baseAngle = time * orbSpeeds[t];

                for (int s = 0; s < stacks[t]; s++)
                {
                    float angle = baseAngle + MathHelper.TwoPi * s / Math.Max(1, (int)stacks[t]);
                    float radius = npcRadius + s * 9f;
                    Vector2 pos = center + angle.ToRotationVector2() * radius;

                    if (s == 0) typePositions[t] = pos;

                    // Rotating cross mark
                    spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), baseCol,
                        time * 2.8f + t * 0.95f, new Vector2(0.5f, 0.5f),
                        new Vector2(7f, 1.5f), SpriteEffects.None, 0f);
                    spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), baseCol,
                        time * 2.8f + t * 0.95f + MathHelper.PiOver2, new Vector2(0.5f, 0.5f),
                        new Vector2(7f, 1.5f), SpriteEffects.None, 0f);

                    // Small bloom glow on each mark
                    spriteBatch.Draw(bloom, pos, null, baseCol * 0.4f, 0f,
                        bloom.Size() * 0.5f, 0.06f, SpriteEffects.None, 0f);
                }
            }

            // ── sealed state: draw resonance triangle + countdown pulse ──
            if (state.Sealed)
            {
                float sealProgress = 1f - (float)state.SealTimer / GlacialEmbracePlayer.SealCountdown;
                float pulse = 0.5f + 0.5f * MathF.Sin(time * 14f);
                Color sealCol = Color.Lerp(
                    new Color(0, 220, 255, 0),
                    new Color(255, 230, 80, 0),
                    sealProgress) * (0.55f + 0.35f * pulse);

                // Pulsing outer bloom around the NPC
                float ringScale = (npcRadius + 26f) / bloom.Width;
                spriteBatch.Draw(bloom, center, null, sealCol * 0.5f, 0f, bloom.Size() * 0.5f, ringScale, SpriteEffects.None, 0f);

                // Triangle connections between the three mark positions
                Color lineCol = new Color(80, 220, 255, 0) * (0.38f + 0.22f * pulse);
                for (int a = 0; a < 3; a++)
                {
                    int b = (a + 1) % 3;
                    if (!typePresent[a] || !typePresent[b]) continue;
                    DrawLine(spriteBatch, pixel, typePositions[a], typePositions[b], lineCol);
                }

                // Central glow at triangle center
                Vector2 triCenter = center;
                spriteBatch.Draw(bloom, triCenter, null, sealCol * 0.35f, time * 3f,
                    bloom.Size() * 0.5f, 0.09f + 0.03f * pulse, SpriteEffects.None, 0f);
            }
        }

        private static void DrawLine(SpriteBatch sb, Texture2D pixel, Vector2 a, Vector2 b, Color color)
        {
            Vector2 edge = b - a;
            if (edge.LengthSquared() < 0.01f) return;
            sb.Draw(pixel, a, new Rectangle(0, 0, 1, 1), color,
                edge.ToRotation(), new Vector2(0f, 0.5f),
                new Vector2(edge.Length(), 1.2f), SpriteEffects.None, 0f);
        }
    }
}
