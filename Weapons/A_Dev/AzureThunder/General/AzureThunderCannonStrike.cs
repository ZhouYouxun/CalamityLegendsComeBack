using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.General;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // GIF 参考的青霆重炮：规则立方体矩阵收束、地面方阵压缩，随后落下青黑巨雷。
    internal sealed class AzureThunderCannonStrike : ModProjectile, ILocalizedModType
    {
        public const int RedFinaleFlag = 1;
        public const int LeftComboFlag = 2;
        public const int SwordTriggeredFlag = 4;
        public const int HarmonyFlag = 8;
        public const int CrumblingFlag = 16;

        private static readonly Vector3[] CubeSlots =
        {
            new(-1f, -1f, 0f), new(0f, -1f, 0.35f), new(1f, -1f, 0f),
            new(-1f, 0f, -0.35f), new(0f, 0f, 0.55f), new(1f, 0f, -0.35f),
            new(-1f, 1f, 0f), new(0f, 1f, 0.35f), new(1f, 1f, 0f)
        };

        private static readonly Vector3[] CubePoints =
        {
            new(-1f, -1f, -1f), new(1f, -1f, -1f), new(1f, 1f, -1f), new(-1f, 1f, -1f),
            new(-1f, -1f, 1f), new(1f, -1f, 1f), new(1f, 1f, 1f), new(-1f, 1f, 1f)
        };

        private static readonly int[][] CubeFaces =
        {
            new[] { 0, 1, 2, 3 }, new[] { 4, 7, 6, 5 },
            new[] { 0, 4, 5, 1 }, new[] { 1, 5, 6, 2 },
            new[] { 2, 6, 7, 3 }, new[] { 3, 7, 4, 0 }
        };

        private static readonly int[,] CubeEdges =
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
            { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
        };

        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TargetIndex => (int)Projectile.ai[0];
        private float RequestedScale => MathHelper.Clamp(Projectile.ai[1], 0.6f, 2.2f);
        private int Flags => (int)Projectile.ai[2];
        private bool RedFinale => (Flags & RedFinaleFlag) != 0;
        private bool LeftCombo => (Flags & LeftComboFlag) != 0;
        private bool SwordTriggered => (Flags & SwordTriggeredFlag) != 0;
        private bool Harmony => (Flags & HarmonyFlag) != 0;
        private bool ApplyCrumbling => (Flags & CrumblingFlag) != 0;
        private int FireFrame => SwordTriggered ? 4 : RedFinale ? 8 : 16;

        private int timer;
        private bool fired;
        private Vector2 fallbackImpact;

        public static int Spawn(IEntitySource source, Vector2 impact, NPC target, int damage, float knockback, int owner, float scale, int flags)
        {
            return Projectile.NewProjectile(
                source,
                impact,
                Vector2.Zero,
                ModContent.ProjectileType<AzureThunderCannonStrike>(),
                Math.Max(1, damage),
                knockback,
                owner,
                target?.whoAmI ?? -1,
                scale,
                flags);
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 54;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            timer++;
            NPC target = ResolveTarget();
            if (timer == 1)
                fallbackImpact = Projectile.Center;

            if (target != null)
                fallbackImpact = target.Center;
            Projectile.Center = fallbackImpact;

            float charge = Utils.GetLerpValue(0f, FireFrame, timer, true);
            Lighting.AddLight(Projectile.Center, new Vector3(0.1f, 0.9f, 0.76f) * (0.45f + charge * 0.65f));

            if (!fired && timer >= FireFrame)
                Fire(target);

            if (!fired && Main.rand.NextBool(2))
                SpawnAssemblyDust(charge);

            if (fired && timer > FireFrame + 22)
                Projectile.Kill();
        }

        private void Fire(NPC target)
        {
            fired = true;
            Player owner = Main.player[Projectile.owner];
            int energyGain = LeftCombo ? 0 : AzureThunderAccessoryPlayer.GetRightClickLightningEnergyGain(owner);

            AzureThunderPlayer.SpawnVerticalLightning(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                target,
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                gainCharge: LeftCombo,
                applyStaticDischarge: !LeftCombo,
                big: true,
                ultimateEnergyGain: energyGain,
                applyCrumbling: ApplyCrumbling,
                spawnHeightMultiplier: 0.94f,
                fixedTiltRadians: MathHelper.ToRadians(owner.direction < 0 ? 6f : -6f),
                speedLines: true,
                normalVisualIntensity: !Harmony,
                oneThirdVisualIntensity: Harmony,
                lightningScale: RequestedScale);

            Color teal = new(50, 255, 210);
            Color hot = new(196, 255, 244);
            AzureThunderFissionBolt.Strike(Projectile.Center, 720f * RequestedScale, 0f, 1.4f * RequestedScale, teal, hot);
            AzureThunderFissionBolt.Burst(Projectile.Center, 135f * RequestedScale, 8, 0.78f * RequestedScale, teal, hot);

            if (RedFinale)
                SpawnRedExplosion();
            else
                SpawnTealImpact();

            owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, RedFinale ? 10f : 7f);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.9f, Pitch = RedFinale ? -0.22f : 0.04f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = RedFinale ? 0.7f : 0.42f, Pitch = -0.42f }, Projectile.Center);
        }

        private void SpawnAssemblyDust(float charge)
        {
            Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(95f, 70f) * MathHelper.Lerp(1.3f, 0.4f, charge);
            Vector2 velocity = (Projectile.Center - position).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.4f, 2.4f);
            Dust dust = Dust.NewDustPerfect(position, DustID.FireworksRGB, velocity, 0, Main.rand.NextBool(4) ? Color.White : new Color(55, 255, 214), Main.rand.NextFloat(0.55f, 0.95f));
            dust.noGravity = true;
        }

        private void SpawnTealImpact()
        {
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, new Color(53, 255, 214), "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.23f * RequestedScale, 12));
            for (int i = 0; i < 20; i++)
            {
                Vector2 velocity = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(2f, 10f) * RequestedScale;
                GeneralParticleHandler.SpawnParticle(new LineParticle(Projectile.Center, velocity, false, Main.rand.Next(10, 18), Main.rand.NextFloat(0.65f, 1.15f), Main.rand.NextBool(4) ? Color.White : new Color(51, 255, 210)));
            }
        }

        private void SpawnRedExplosion()
        {
            Color red = new(255, 42, 64);
            Color hotRed = new(255, 156, 128);
            AzureThunderFissionBolt.Burst(Projectile.Center, 185f * RequestedScale, 13, 1.18f * RequestedScale, red, hotRed);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, red, "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.34f * RequestedScale, 14));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.White, "CalamityMod/Particles/BloomCircle", Vector2.One, 0f, 1.35f * RequestedScale, 0.2f * RequestedScale, 12, true, 0.65f));

            for (int i = 0; i < 32; i++)
            {
                Vector2 velocity = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(3f, 15f) * RequestedScale;
                GeneralParticleHandler.SpawnParticle(new LineParticle(Projectile.Center, velocity, false, Main.rand.Next(11, 22), Main.rand.NextFloat(0.75f, 1.4f), Main.rand.NextBool(5) ? Color.White : red));
            }
        }

        private NPC ResolveTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC target = Main.npc[TargetIndex];
            return target.active && target.CanBeChasedBy(Projectile) ? target : null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float charge = Utils.GetLerpValue(0f, FireFrame, timer, true);
            float fade = Utils.GetLerpValue(FireFrame + 22f, FireFrame + 8f, timer, true);
            DrawGroundArray(charge, fade);
            DrawCubeAssembly(charge, fade);
            if (fired)
                DrawDarkLightning(fade);
            return false;
        }

        private void DrawGroundArray(float charge, float fade)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 center = Projectile.Center - Main.screenPosition + Vector2.UnitY * 22f;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f + Projectile.identity);
            float expansion = MathHelper.Lerp(0.48f, 1.65f, charge) * RequestedScale;
            if (fired)
                expansion += Utils.GetLerpValue(FireFrame, FireFrame + 16f, timer, true) * 0.9f;

            Color dark = new Color(4, 13, 17, 220) * (0.72f * fade);
            Color teal = Color.Lerp(new Color(34, 222, 186), Color.White, pulse * 0.28f) * (0.52f * fade);
            Color accent = RedFinale && fired ? new Color(255, 36, 58) * (0.55f * fade) : new Color(89, 255, 224) * (0.42f * fade);

            Main.EntitySpriteDraw(pixel, center, new Rectangle(0, 0, 1, 1), dark, MathHelper.PiOver4, new Vector2(0.5f), new Vector2(112f, 22f) * expansion, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(pixel, center, new Rectangle(0, 0, 1, 1), teal, MathHelper.PiOver4, new Vector2(0.5f), new Vector2(88f, 4f) * expansion, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(pixel, center, new Rectangle(0, 0, 1, 1), accent, -MathHelper.PiOver4, new Vector2(0.5f), new Vector2(76f, 3f) * expansion, SpriteEffects.None, 0f);
        }

        private void DrawCubeAssembly(float charge, float fade)
        {
            float easedCharge = charge * charge * (3f - 2f * charge);
            float collapse = fired ? Utils.GetLerpValue(FireFrame + 16f, FireFrame, timer, true) : 1f;
            float spacing = 21f * RequestedScale;
            float cubeSize = 7.5f * RequestedScale * (0.75f + 0.25f * easedCharge);
            Matrix rotation = Matrix.CreateFromYawPitchRoll(
                Main.GlobalTimeWrappedHourly * 0.86f + Projectile.identity * 0.11f,
                -0.58f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.3f) * 0.16f,
                Main.GlobalTimeWrappedHourly * 0.34f);

            for (int i = 0; i < CubeSlots.Length; i++)
            {
                Vector3 slot = CubeSlots[i] * spacing;
                Vector3 scattered = slot * 2.7f + new Vector3(0f, -26f - i * 2f, (i % 3 - 1) * 18f);
                Vector3 assembled = Vector3.Lerp(scattered, slot, easedCharge) * collapse;
                float slotFade = Utils.GetLerpValue(i * 0.035f, 0.34f + i * 0.035f, charge, true) * fade;
                DrawCube(assembled, cubeSize, rotation, slotFade, i);
            }
        }

        private void DrawCube(Vector3 offset, float halfSize, Matrix rotation, float opacity, int cubeIndex)
        {
            Vector2[] projected = new Vector2[CubePoints.Length];
            float[] depths = new float[CubePoints.Length];
            for (int i = 0; i < CubePoints.Length; i++)
            {
                Vector3 point = Vector3.Transform(CubePoints[i] * halfSize + offset, rotation);
                float perspective = 720f / (780f + point.Z);
                projected[i] = Projectile.Center + new Vector2(point.X, point.Y) * perspective;
                depths[i] = point.Z;
            }

            List<(float depth, int face)> orderedFaces = new(CubeFaces.Length);
            for (int face = 0; face < CubeFaces.Length; face++)
            {
                int[] indices = CubeFaces[face];
                float depth = (depths[indices[0]] + depths[indices[1]] + depths[indices[2]] + depths[indices[3]]) * 0.25f;
                orderedFaces.Add((depth, face));
            }
            orderedFaces.Sort((a, b) => a.depth.CompareTo(b.depth));

            Color[] faceColors =
            {
                new(7, 29, 33), new(73, 255, 219), new(22, 134, 128),
                new(169, 255, 238), new(14, 82, 88), new(44, 209, 183)
            };
            foreach ((float _, int face) in orderedFaces)
            {
                int[] indices = CubeFaces[face];
                Color color = faceColors[(face + cubeIndex) % faceColors.Length] * (0.58f * opacity);
                DrawFilledQuad(projected[indices[0]], projected[indices[1]], projected[indices[2]], projected[indices[3]], color);
            }

            Color edgeColor = Color.Lerp(new Color(41, 255, 215), Color.White, 0.34f) * (0.82f * opacity);
            Color shadowEdge = new Color(2, 10, 14) * (0.9f * opacity);
            for (int i = 0; i < CubeEdges.GetLength(0); i++)
            {
                Vector2 a = projected[CubeEdges[i, 0]];
                Vector2 b = projected[CubeEdges[i, 1]];
                DrawSegment(a, b, shadowEdge, 2.8f);
                DrawSegment(a, b, edgeColor, 1.05f);
            }
        }

        private void DrawDarkLightning(float fade)
        {
            float strikeProgress = Utils.GetLerpValue(FireFrame, FireFrame + 10f, timer, true);
            float opacity = (1f - strikeProgress) * fade;
            if (opacity <= 0.01f)
                return;

            for (int strand = 0; strand < 5; strand++)
            {
                Vector2 previous = Projectile.Center - Vector2.UnitY * (620f * RequestedScale) + new Vector2((strand - 2) * 28f, 0f);
                for (int i = 1; i <= 9; i++)
                {
                    float completion = i / 9f;
                    float wave = (float)Math.Sin(completion * 17f + strand * 2.13f + Projectile.identity) * (1f - completion) * 46f;
                    Vector2 next = Vector2.Lerp(Projectile.Center - Vector2.UnitY * (620f * RequestedScale), Projectile.Center, completion) + new Vector2(wave + (strand - 2) * 8f, 0f);
                    DrawSegment(previous, next, new Color(2, 8, 12) * (0.88f * opacity), 5.2f * RequestedScale);
                    DrawSegment(previous, next, new Color(36, 112, 108) * (0.34f * opacity), 1.2f * RequestedScale);
                    previous = next;
                }
            }
        }

        private static void DrawFilledQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            DrawFilledTriangle(a, b, c, color);
            DrawFilledTriangle(a, c, d, color);
        }

        private static void DrawFilledTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            float longest = Math.Max(Vector2.Distance(a, b), Vector2.Distance(a, c));
            int steps = Math.Clamp((int)(longest * 0.7f), 3, 14);
            for (int i = 0; i <= steps; i++)
            {
                float completion = i / (float)steps;
                DrawSegment(Vector2.Lerp(a, b, completion), Vector2.Lerp(a, c, completion), color, 2.2f);
            }
        }

        private static void DrawSegment(Vector2 a, Vector2 b, Color color, float width)
        {
            Vector2 edge = b - a;
            if (edge.LengthSquared() <= 0.001f)
                return;

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, a - Main.screenPosition, new Rectangle(0, 0, 1, 1), color, edge.ToRotation(), new Vector2(0f, 0.5f), new Vector2(edge.Length(), width), SpriteEffects.None, 0f);
        }
    }
}
