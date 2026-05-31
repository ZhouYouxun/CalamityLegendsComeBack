using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Cynosure
{
    /// <summary>
    /// 外层充能单元。它们围成圆环，短暂停留后向目标释放细电弧并自爆。
    /// </summary>
    public class CynosureChargedCell : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/EAfterDog/Cynosure/AuricBall";
        public new string LocalizationCategory => "Projectiles.SHPC";

        private Vector2 OrbitCenter
        {
            get => new(Projectile.localAI[1], Projectile.localAI[2]);
            set
            {
                Projectile.localAI[1] = value.X;
                Projectile.localAI[2] = value.Y;
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 74;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                OrbitCenter = Projectile.Center;
                Projectile.velocity = Projectile.ai[1].ToRotationVector2() * Main.rand.NextFloat(18f, 25f);
            }

            float age = 74f - Projectile.timeLeft;
            Projectile.rotation += 0.22f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.35f, 0.62f, 1f) * 0.45f);

            if (age < 38f)
            {
                Projectile.velocity *= 0.9f;

                if (Main.rand.NextBool(3))
                    CynosureVisuals.SpawnElectricBurst(Projectile.Center, 1, 0.8f, 2.2f);
                return;
            }

            Projectile.velocity *= 0.82f;
            if (age >= 58f)
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            NPC target = CynosureTargeting.FindTarget((int)Projectile.ai[0], Projectile.Center);
            if (target != null && Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CynosureLightningArc>(), Projectile.damage, Projectile.knockBack,
                    Projectile.owner, target.whoAmI, target.Center.X, target.Center.Y);
            }

            CynosureVisuals.SpawnElectricBurst(Projectile.Center, 14, 2.4f, 10f);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 214, 88),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.035f,
                0.18f,
                12));
        }

        public override bool? CanDamage() => false;
    }

    /// <summary>
    /// 命中点的闪电爆炸。视觉上参考金源地雷，但半径被压缩到适合武器命中的尺寸。
    /// </summary>
    public class CynosureLightningExplosion : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.SHPC";

        private readonly List<List<Vector2>> lightningTrails = new();

        public override void SetDefaults()
        {
            Projectile.width = 208;
            Projectile.height = 208;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 12;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.timeLeft == 12)
            {
                BuildLightning();
                CynosureVisuals.SpawnElectricBurst(Projectile.Center, 34, 4f, 20f);
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center, Vector2.Zero, Color.Cyan, Vector2.One, 0f, 0.08f, 0.62f, 18));
            }
        }

        private void BuildLightning()
        {
            // 视觉上只保留若干短电弧，相当于从五帧素材中随机抽取两帧播放的轻量替代实现。
            for (int i = 0; i < 10; i++)
            {
                List<Vector2> points = new() { Projectile.Center };
                Vector2 direction = Main.rand.NextVector2Unit();
                for (int j = 1; j < 6; j++)
                    points.Add(Projectile.Center + direction * j * 19f + Main.rand.NextVector2Circular(13f, 13f));
                lightningTrails.Add(points);
            }
        }

        internal float Width(float completion, Vector2 _) => MathHelper.Lerp(4.5f, 1f, completion);
        internal Color ColorFunction(float completion, Vector2 _) => Color.Lerp(Color.White, Color.Cyan, completion) * (1f - completion * 0.45f);

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            GameShaders.Misc["CalamityMod:TeslaTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ZapTrail"));
            foreach (List<Vector2> points in lightningTrails)
                PrimitiveRenderer.RenderTrail(points, new PrimitiveSettings(Width, ColorFunction, smoothen: false, shader: GameShaders.Misc["CalamityMod:TeslaTrail"]), 30);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

    /// <summary>
    /// 充能单元释放的细电弧。线段本身有伤害，但只存在极短时间。
    /// </summary>
    public class CynosureLightningArc : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.SHPC";

        private readonly List<Vector2> points = new();

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (points.Count > 0)
                return;

            Vector2 start = Projectile.Center;
            Vector2 end = new(Projectile.ai[1], Projectile.ai[2]);
            NPC target = CynosureTargeting.FindTarget((int)Projectile.ai[0], start);
            if (target != null)
                end = target.Center;

            for (int i = 0; i <= 10; i++)
            {
                float progress = i / 10f;
                Vector2 point = Vector2.Lerp(start, end, progress);
                if (i != 0 && i != 10)
                    point += Main.rand.NextVector2Circular(12f, 12f);
                points.Add(point);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (points.Count < 2)
                return false;

            float collisionPoint = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), points[i - 1], points[i], 5f, ref collisionPoint))
                    return true;
            }
            return false;
        }

        internal float Width(float completion, Vector2 _) => MathHelper.Lerp(4.2f, 1.15f, completion);
        internal Color ColorFunction(float completion, Vector2 _)
        {
            Color hotCore = Color.Lerp(Color.White, new Color(255, 224, 92), 0.4f);
            Color coldEdge = Color.Lerp(new Color(255, 196, 54), Color.Cyan, completion * 0.55f);
            return Color.Lerp(hotCore, coldEdge, completion) * (1f - completion * 0.18f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (points.Count < 2)
                return false;
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            GameShaders.Misc["CalamityMod:TeslaTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ZapTrail"));
            PrimitiveRenderer.RenderTrail(points, new PrimitiveSettings(Width, ColorFunction, smoothen: false, shader: GameShaders.Misc["CalamityMod:TeslaTrail"]), 24);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

    internal static class CynosureTargeting
    {
        internal static NPC FindTarget(int preferredTarget, Vector2 center)
        {
            if (Main.npc.IndexInRange(preferredTarget))
            {
                NPC preferred = Main.npc[preferredTarget];
                if (preferred.CanBeChasedBy())
                    return preferred;
            }

            NPC closest = null;
            float closestDistance = 1200f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;
                float distance = Vector2.Distance(center, npc.Center);
                if (distance < closestDistance)
                {
                    closest = npc;
                    closestDistance = distance;
                }
            }
            return closest;
        }
    }

    internal static class CynosureVisuals
    {
        internal static void SpawnElectricBurst(Vector2 center, int count, float minSpeed, float maxSpeed)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(minSpeed, maxSpeed);
                Color color = Main.rand.NextBool(3) ? new Color(255, 214, 86) : (Main.rand.NextBool(4) ? Color.White : Color.Cyan);
                Dust dust = Dust.NewDustPerfect(center, DustID.Electric, velocity, 0, color, Main.rand.NextFloat(0.8f, 1.5f));
                dust.noGravity = true;
            }
        }
    }
}
