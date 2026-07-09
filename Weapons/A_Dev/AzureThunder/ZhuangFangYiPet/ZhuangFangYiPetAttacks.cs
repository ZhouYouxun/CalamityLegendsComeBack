using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.ZhuangFangYiPet
{
    internal static class ZhuangFangYiPetVisuals
    {
        public static readonly Color Teal = new(54, 255, 212);
        public static readonly Color Green = new(92, 255, 154);
        public static readonly Color Pale = new(177, 255, 232);

        public static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 1f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                (start + end) * 0.5f - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                edge.ToRotation(),
                new Vector2(0.5f, 0.5f),
                new Vector2(edge.Length(), width),
                SpriteEffects.None);
        }

        public static void SpawnElectricDust(Vector2 center, float radius, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(radius, radius),
                    DustID.FireworksRGB,
                    Main.rand.NextVector2Circular(speed, speed),
                    0,
                    Main.rand.NextBool() ? Teal : Green,
                    Main.rand.NextFloat(0.75f, 1.35f));
                dust.noGravity = true;
            }
        }
    }

    // 庄方宜宠物的攻击弹幕：一枚可复用的分叉闪电，弱攻击/强攻击/大招期间都靠 ai[2] 的视觉强度区分表现和威力。
    internal sealed class ZhuangFangYiWeakLightning : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 18;

        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Vector2 EndPoint => new(Projectile.ai[0], Projectile.ai[1]);
        private float VisualScale => MathHelper.Clamp(Projectile.ai[2] <= 0f ? 0.85f : Projectile.ai[2], 0.45f, 1.8f);
        private bool HarmonyBolt => VisualScale >= 1.35f;
        private int timer;

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 4;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            timer++;
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Vector2.Lerp(Projectile.Center, EndPoint, 0.55f), new Vector3(0.12f, 0.95f, 0.72f) * (0.45f + VisualScale * 0.2f));

            if (timer != 1)
                return;

            SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.56f + VisualScale * 0.08f, Pitch = 0.28f }, Projectile.Center);
            ZhuangFangYiPetVisuals.SpawnElectricDust(EndPoint, 24f + 16f * VisualScale, (int)(12 + 8 * VisualScale), 4.2f + VisualScale * 1.5f);

            Vector2 direction = (EndPoint - Projectile.Center).SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 7; i++)
            {
                float t = (i + Main.rand.NextFloat(0.15f, 0.85f)) / 7f;
                Vector2 position = GetLightningPoint(t) + normal * Main.rand.NextFloat(-10f, 10f) * VisualScale;
                Vector2 velocity = direction.RotatedByRandom(0.48f) * Main.rand.NextFloat(1.6f, 4.8f);
                Color color = Main.rand.NextBool(3) ? ZhuangFangYiPetVisuals.Pale : ZhuangFangYiPetVisuals.Teal;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    position,
                    velocity,
                    "CalamityMod/Particles/GlowSpark",
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.08f, 0.16f) * VisualScale,
                    color,
                    new Vector2(1.8f, 0.75f),
                    true,
                    true,
                    shrinkSpeed: 0.9f));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                EndPoint,
                18f + 10f * VisualScale,
                ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, HarmonyBolt ? 360 : 240);
            target.AddBuff(ModContent.BuffType<StaticDischarge>(), HarmonyBolt ? 240 : 120);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);

            if (HarmonyBolt)
                AzureThunderPlayer.ApplyUltimateDot(target, 180);

            if (Main.myPlayer == Projectile.owner && Projectile.numHits == 0)
                AzureThunderPlayer.SpawnHarmonyHitMark(Projectile.GetSource_FromThis(), target.Center, Projectile.owner, target.whoAmI, 0.75f + VisualScale * 0.28f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fadeIn = Utils.GetLerpValue(0f, 4f, timer, true);
            float fadeOut = Utils.GetLerpValue(0f, 9f, Projectile.timeLeft, true);
            float opacity = fadeIn * fadeOut;
            int segments = 7 + (int)MathF.Ceiling(VisualScale * 3f);
            Vector2 direction = (EndPoint - Projectile.Center).SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            DrawForkedBolt(segments, opacity, normal, direction, 1f, 0f);
            DrawForkedBolt(segments, opacity * 0.45f, normal, direction, 0.54f, 1.7f);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        private void DrawForkedBolt(int segments, float opacity, Vector2 normal, Vector2 direction, float widthScale, float phaseOffset)
        {
            Vector2 previous = GetLightningPoint(0f, phaseOffset);
            Color core = Color.White with { A = 0 };
            Color edge = ZhuangFangYiPetVisuals.Teal with { A = 0 };
            Color pale = ZhuangFangYiPetVisuals.Pale with { A = 0 };

            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector2 current = GetLightningPoint(t, phaseOffset);
                float envelope = (float)Math.Sin(t * MathHelper.Pi);
                float width = (2.4f + envelope * 4.6f * VisualScale) * widthScale;

                ZhuangFangYiPetVisuals.DrawLine(previous, current, edge * opacity * 0.74f, width + 3.2f * VisualScale);
                ZhuangFangYiPetVisuals.DrawLine(previous, current, core * opacity * 0.68f, Math.Max(1.5f, width * 0.42f));

                if (i == segments / 3 || i == segments * 2 / 3)
                {
                    float branchSide = i == segments / 3 ? -1f : 1f;
                    Vector2 branchEnd = current + (normal * branchSide + direction * 0.35f).SafeNormalize(Vector2.UnitY) * (38f + 22f * VisualScale) * envelope;
                    ZhuangFangYiPetVisuals.DrawLine(current, branchEnd, pale * opacity * 0.42f, 2.2f + 2f * VisualScale);
                }

                previous = current;
            }
        }

        private Vector2 GetLightningPoint(float completion, float phaseOffset = 0f)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = EndPoint;
            Vector2 direction = (end - start).SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float envelope = (float)Math.Sin(completion * MathHelper.Pi);
            float seed = Projectile.identity * 0.37f + Projectile.owner * 0.61f + phaseOffset;
            float zig = (float)Math.Sin(seed + completion * 31.4f) * 0.75f + (float)Math.Sin(seed * 1.73f + completion * 57.2f) * 0.35f;
            float bow = (float)Math.Sin(seed * 0.71f + completion * MathHelper.Pi) * 0.28f;
            return Vector2.Lerp(start, end, completion) + normal * (zig + bow) * envelope * (18f + 16f * VisualScale);
        }
    }
}
