using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.Passive
{
    internal sealed class PristineFuryPassiveTentacle : ModProjectile, ILocalizedModType
    {
        private static readonly Color DeepFlame = new(210, 24, 54);
        private static readonly Color HotFlame = new(255, 92, 34);
        private static readonly Color VioletFlame = new(174, 64, 255);

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TentacleIndex => Utils.Clamp((int)Projectile.ai[0], 0, 2);
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = false;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => true;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.HeldItem.type != ModContent.ItemType<NewLegendPristineFury>())
            {
                Projectile.Kill();
                return;
            }

            Timer++;
            Projectile.timeLeft = 2;
            Projectile.damage = 0;

            Vector2 buttAnchor = GetButtAnchor(owner);
            Vector2 idlePoint = GetIdlePoint(owner, buttAnchor);

            if (Timer <= 1f || Vector2.Distance(Projectile.Center, buttAnchor) > 280f)
            {
                Projectile.Center = idlePoint;
                Projectile.velocity = Vector2.Zero;
            }

            Vector2 playerMovement = owner.position - owner.oldPosition;
            Projectile.position += playerMovement;

            Vector2 toIdle = idlePoint - Projectile.Center;
            Projectile.velocity += toIdle * 0.035f;
            Projectile.velocity *= 0.9f;

            float distance = toIdle.Length();
            if (distance > 160f)
            {
                Projectile.velocity += toIdle.SafeNormalize(Vector2.UnitY) * 4f;
                Projectile.velocity *= 0.95f;
            }

            Lighting.AddLight(Vector2.Lerp(buttAnchor, Projectile.Center, 0.62f), new Vector3(0.36f, 0.08f, 0.1f));

            if (!Main.dedServ)
                EmitTentacleFlames(owner, buttAnchor);
        }

        private Vector2 GetButtAnchor(Player owner)
        {
            float behind = -owner.direction;
            return owner.Center + new Vector2(
                behind * 10f,
                owner.height * 0.25f * owner.gravDir);
        }

        private Vector2 GetIdlePoint(Player owner, Vector2 buttAnchor)
        {
            float behind = -owner.direction;
            int index = TentacleIndex;
            Vector2[] idleOffsets =
            {
                new(behind * 70f, -20f * owner.gravDir),
                new(behind * 90f, -45f * owner.gravDir),
                new(behind * 60f, -70f * owner.gravDir)
            };

            float phase = index * 2.1f;
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 idlePoint = buttAnchor + idleOffsets[index];
            idlePoint += new Vector2(
                behind * MathF.Sin(time * 2.2f + phase) * 10f,
                MathF.Cos(time * 1.7f + phase) * 8f * owner.gravDir);

            return idlePoint;
        }

        private Vector2 GetCurvePoint(Player owner, Vector2 buttAnchor, float t)
        {
            int index = TentacleIndex;
            float behind = -owner.direction;
            float backDistance = 78f + index * 13f;
            float upDistance = 108f + index * 18f;
            float phase = index * 1.7f;

            float x = behind * backDistance * (1f - MathF.Pow(1f - t, 2.5f));
            float y = -owner.gravDir * upDistance * MathF.Pow(t, 2.2f);
            Vector2 formulaPoint = buttAnchor + new Vector2(x, y);

            float endpointBlend = MathF.Pow(t, 3.35f);
            Vector2 point = Vector2.Lerp(formulaPoint, Projectile.Center, endpointBlend);

            float wave = MathF.Sin(Main.GlobalTimeWrappedHourly * 5.2f + t * 7.4f + phase) * (5.4f + index * 0.7f);
            Vector2 normal = new Vector2(0f, owner.gravDir).RotatedBy(owner.direction * 0.4f);
            point += normal * wave * MathF.Sin(t * MathHelper.Pi);
            return point;
        }

        private Vector2 GetCurveTangent(Player owner, Vector2 buttAnchor, float t)
        {
            float previousT = MathHelper.Clamp(t - 0.035f, 0f, 1f);
            float nextT = MathHelper.Clamp(t + 0.035f, 0f, 1f);
            Vector2 previous = GetCurvePoint(owner, buttAnchor, previousT);
            Vector2 next = GetCurvePoint(owner, buttAnchor, nextT);
            return (next - previous).SafeNormalize(-Vector2.UnitY * owner.gravDir);
        }

        private void EmitTentacleFlames(Player owner, Vector2 buttAnchor)
        {
            int index = TentacleIndex;
            int segmentCount = 24;
            float phase = index * 0.19f;

            for (int i = 0; i < segmentCount; i += 4)
            {
                float t = i / (float)(segmentCount - 1);
                Vector2 point = GetCurvePoint(owner, buttAnchor, t);
                float scale = MathHelper.Lerp(1.18f, 0.35f, t) * Main.rand.NextFloat(0.82f, 1.12f);
                Dust dust = Dust.NewDustPerfect(
                    point + Main.rand.NextVector2Circular(2.5f, 2.5f),
                    Main.rand.NextBool(4) ? DustID.Shadowflame : DustID.Torch,
                    Vector2.Zero,
                    120,
                    Color.Lerp(DeepFlame, VioletFlame, t * 0.28f),
                    scale);
                dust.noGravity = true;
                dust.fadeIn = 0.7f;
            }

            for (int i = 0; i < 4; i++)
            {
                float t = (Timer * 0.018f + i * 0.22f + phase) % 1f;
                SpawnFlameAlongCurve(owner, buttAnchor, t, true);
            }

            if (Main.rand.NextBool(2))
                SpawnFlameAlongCurve(owner, buttAnchor, Main.rand.NextFloat(0.08f, 0.98f), false);
        }

        private void SpawnFlameAlongCurve(Player owner, Vector2 buttAnchor, float t, bool flowing)
        {
            Vector2 point = GetCurvePoint(owner, buttAnchor, t);
            Vector2 tangent = GetCurveTangent(owner, buttAnchor, t);
            float deathFade = 1f;
            Vector2 dustVelocity =
                tangent
                .RotatedByRandom(0.34f)
                * Main.rand.NextFloat(flowing ? 3.4f : 1.4f, flowing ? 8.8f : 4.6f)
                - owner.velocity * 0.42f;

            Color color = Main.rand.NextBool(5)
                ? VioletFlame
                : Color.Lerp(DeepFlame, HotFlame, Main.rand.NextFloat(0.25f, 0.85f));
            if (Main.rand.NextBool(6))
                color = Color.Lerp(color, Color.White, 0.45f);

            float width = MathHelper.Lerp(0.95f, 0.32f, t);
            float scale = Main.rand.NextFloat(0.04f, 0.06f) * MathHelper.Lerp(8.6f, 3.4f, t);
            Particle flame = new CustomSpark(
                point + Main.rand.NextVector2Circular(3.5f, 3.5f),
                dustVelocity,
                "CalamityMod/Particles/BloomCircle",
                false,
                Main.rand.Next(11, 17),
                scale,
                color * deathFade,
                new Vector2(width, 1f),
                shrinkSpeed: 0.91f);
            GeneralParticleHandler.SpawnParticle(flame);

            if (Main.rand.NextBool(3))
            {
                Dust ember = Dust.NewDustPerfect(
                    point,
                    DustID.Shadowflame,
                    dustVelocity * 0.18f,
                    135,
                    Color.Lerp(color, Color.White, 0.18f),
                    Main.rand.NextFloat(0.55f, 0.95f) * MathHelper.Lerp(1.05f, 0.36f, t));
                ember.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
