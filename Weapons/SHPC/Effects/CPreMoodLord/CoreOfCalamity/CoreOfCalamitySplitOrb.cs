using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.CoreOfCalamity
{

    /// <summary>
    /// 灾劫核心爆炸后的分裂弹，颜色从调色盘中独立随机抽取。
    /// 炸开后的滑行/开火/命中判定完整搬运自变压器光球（<see cref="CalamityMod.Items.Accessories.TheTransformer"/> 的
    /// TransformerBlob）：弱衰减滑行 -> 原地悬停蓄力 -> 锁定最近目标喷射成"激光" -> 命中判定/伤害衰减/尘埃与音效
    /// 全部照搬变压器的逻辑，只是把贴图换成了粒子拼出的辉光核心。命中或超时死亡后仍会释放一次小型爆炸。
    /// </summary>
    internal sealed class CoreOfCalamitySplitOrb : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        private const int ActivationDelay = 10;

        // 与变压器光球完全一致的弱衰减速率，炸开后能滑行相当长的距离才停下蓄力
        private const float PreFireDecel = 0.965f;
        private const int PreFireDuration = 70;
        private const float FireSpeed = 12f;
        private const int FireExtraUpdates = 8;
        private const float TrackRange = 900f;
        private const float PostFireEffectRange = 1400f;

        internal static readonly Color[] Palette =
        {
            new(24, 62, 188),
            new(106, 218, 255),
            new(232, 56, 62),
            new(255, 194, 62)
        };

        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private Color OrbColor => Palette[Utils.Clamp((int)Projectile.ai[0], 0, Palette.Length - 1)];
        private bool Fired => Timer > PreFireDuration;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 260;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60 * Projectile.MaxUpdates;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            Timer++;
            if (Projectile.localAI[1] == 0f)
                Projectile.localAI[1] = 120f + Projectile.ai[0] * 90f;

            Lighting.AddLight(Projectile.Center, OrbColor.ToVector3() * 0.58f);

            if (!Fired)
            {
                // 弱衰减滑行 + 缓慢自转，和变压器光球炸开后的表现完全一致
                Projectile.velocity *= PreFireDecel;
                Projectile.rotation += 0.05f * (Projectile.ai[0] % 2 == 0 ? 1f : -1f);

                Vector2 driftDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                SpawnFlightEffects(driftDirection);
                SpawnPreFireFlightEffects(driftDirection);

                if (Timer == PreFireDuration)
                    FireBeam(owner);
            }
            else
            {
                // 喷射成"激光"之后的表现，逐字段照抄变压器光球的 poweredTimer == 0 分支
                float sine = (float)Math.Sin(Projectile.timeLeft * 0.575f / MathHelper.Pi);
                Vector2 offset = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * sine * 20f;
                float targetDist = Vector2.Distance(owner.Center, Projectile.Center);
                if (targetDist < PostFireEffectRange)
                {
                    Particle spark = new GlowSparkParticle(Projectile.Center - Projectile.velocity, -Projectile.velocity * 0.3f, false, 21, 0.04f, OrbColor * 0.65f, new Vector2(0.6f, 0.5f), true, false, 0.7f);
                    GeneralParticleHandler.SpawnParticle(spark);

                    if (Timer % 2 == 0)
                    {
                        Vector2 dustVel = (-Projectile.velocity).RotatedByRandom(0.3f);
                        Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, ModContent.DustType<VoidDustInverted>(), dustVel * Main.rand.NextFloat(0.1f, 0.8f));
                        dust.noGravity = true;
                        dust.scale = Main.rand.NextFloat(0.4f, 0.6f);
                        dust.color = new Color(30, 30, 30);
                        dust.noLightEmittence = true;
                    }
                }

                Projectile.rotation = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
            }
        }

        private void FireBeam(Player owner)
        {
            NPC target = Projectile.Center.ClosestNPCAt(TrackRange);
            Vector2 direction = target != null
                ? (target.Center - Projectile.Center).SafeNormalize(Main.rand.NextVector2Unit())
                : Main.rand.NextVector2Unit();

            Projectile.netUpdate = true;
            if (Projectile.owner == Main.myPlayer)
                owner.SetScreenshake(3.5f);

            Projectile.numHits = 0;
            Projectile.velocity = direction * FireSpeed;
            Projectile.extraUpdates = FireExtraUpdates;
            Projectile.rotation = direction.ToRotation();
            for (int i = 0; i < Main.maxNPCs; i++)
                Projectile.localNPCImmunity[i] = 0;

            // 开火瞬间的尘埃迸发，样式与用色都照搬变压器
            for (int i = 0; i <= 9; i++)
            {
                float variance = Main.rand.NextFloat(-0.6f, 0.6f);
                const int dustStyle = 278;
                Dust dust2 = Dust.NewDustPerfect(Projectile.Center, dustStyle, Projectile.velocity);
                dust2.scale = Main.rand.NextFloat(0.9f, 1.2f) - Math.Abs(variance);
                dust2.velocity = (Projectile.velocity * 2).RotatedBy(variance) * Main.rand.NextFloat(0.3f, 1f) * (1 - Math.Abs(variance));
                dust2.noGravity = true;
                dust2.color = OrbColor;
            }

            SoundStyle fire = new("CalamityMod/Sounds/Item/OmicronBeam");
            SoundEngine.PlaySound(fire with { Volume = 0.2f, Pitch = Math.Clamp(Main.rand.NextFloat(0.1f, 0.2f) + Projectile.ai[0] * 0.02f, 0, 1), MaxInstances = -1 }, Projectile.Center);
        }

        public override bool? CanDamage() => Timer >= ActivationDelay ? null : false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 击杀目标时不消耗衰减次数，保留后续命中的伤害加成——照搬变压器光球
            if (target.life <= 0 && target.realLife == -1)
                Projectile.numHits--;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Fired)
            {
                float minMult = 0.05f;
                int hitsToMinMult = 4;
                float damageMult = Utils.Remap(Projectile.numHits, 0, hitsToMinMult, 1, minMult, true) * (Projectile.numHits == 0 ? 1.5f : 1);
                modifiers.SourceDamage *= damageMult;
            }
            else
                modifiers.SourceDamage *= 0.2f;

            Player owner = Main.player[Projectile.owner];
            target.MoveNPC(Utils.DirectionTo(owner.Center, target.Center), Fired ? 7 : 3, false, owner);

            if (Projectile.numHits == 0 && Fired)
            {
                Color secondary = Color.Lerp(OrbColor, Color.White, 0.6f);
                for (int i = 0; i <= 6; i++)
                {
                    float variance = Main.rand.NextFloat(-0.4f, 0.4f);
                    Vector2 fxVel = (Projectile.velocity * 3).RotatedBy(variance) * Main.rand.NextFloat(0.3f, 1f) * (1 - Math.Abs(variance));
                    Particle spark2 = new SparkParticle(Projectile.Center + fxVel, fxVel, false, 45, Main.rand.NextFloat(0.8f, 1f) - Math.Abs(variance), Main.rand.NextBool(4) ? secondary : OrbColor);
                    GeneralParticleHandler.SpawnParticle(spark2);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
                SpawnDamageExplosion();

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                OrbColor,
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.1f,
                0.54f,
                14));

            for (int i = 0; i < 8; i++)
            {
                Vector2 crossDir = (MathHelper.PiOver4 * i + Projectile.rotation).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center,
                    crossDir * Main.rand.NextFloat(2.8f, 7.5f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(10, 19),
                    Main.rand.NextFloat(0.14f, 0.30f),
                    OrbColor,
                    new Vector2(0.20f, 1.65f),
                    true,
                    true,
                    extraRotation: 0f,
                    shrinkSpeed: 0.52f,
                    glowOpacity: 0.90f));
            }

            // 照搬变压器光球死亡时的暗色尘埃迸发
            for (int i = 0; i < 14; i++)
            {
                Vector2 dustVel = (Vector2.One * 5).RotatedByRandom(100);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>(), dustVel * Main.rand.NextFloat(0.1f, 0.8f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.8f, 1.1f);
                dust.color = new Color(30, 30, 30);
                dust.noLightEmittence = true;
            }
        }

        private void SpawnDamageExplosion()
        {
            int explosionIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                (int)(Projectile.damage * 0.75),
                Projectile.knockBack,
                Projectile.owner);

            if (!Main.projectile.IndexInRange(explosionIndex))
                return;

            Projectile explosion = Main.projectile[explosionIndex];
            int explosionSize = (int)(new BalanceSHPC().GetDefaultOrbExplosionSize() * 0.66f);
            explosion.Resize(explosionSize, explosionSize);
            explosion.Center = Projectile.Center;
            explosion.DamageType = DamageClass.Magic;
            explosion.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 position = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 3; i++)
            {
                Main.EntitySpriteDraw(
                    bloom,
                    position,
                    null,
                    Color.Lerp(OrbColor, Color.White, i * 0.35f) with { A = 0 } * 0.76f,
                    Projectile.rotation,
                    bloom.Size() * 0.5f,
                    new Vector2(0.21f, 0.15f) * (1f - i * 0.2f),
                    SpriteEffects.None);
            }

            DrawTransformerHalo(bloom, position);

            if (Fired)
                DrawFireBurstRings(bloom, position);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        // 照搬变压器光球 PreDraw 里那圈 10 份偏移残影，只是把贴图换成辉光圆
        private void DrawTransformerHalo(Texture2D bloom, Vector2 position)
        {
            float sine = Math.Abs((float)Math.Sin(Timer * 0.15f / MathHelper.Pi) * 0.2f) + 0.8f;
            for (int i = 0; i < 10; i++)
            {
                Color auraColor = OrbColor with { A = 0 } * (Fired ? (float)Math.Pow(Utils.GetLerpValue(0, 15, Timer - PreFireDuration, true), 2) : sine) * 0.6f;
                Vector2 drawOffset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 4f;
                Main.EntitySpriteDraw(bloom, position + drawOffset, null, auraColor, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.21f, 0.15f), SpriteEffects.None);
            }
        }

        // 照搬变压器光球开火瞬间的扩张光环
        private void DrawFireBurstRings(Texture2D bloom, Vector2 position)
        {
            float ticksSinceFire = Timer - PreFireDuration;
            float ringOpen = MathHelper.Clamp(ticksSinceFire / 15f, 0f, 1f);
            Color auraColor = OrbColor with { A = 0 } * (1f - Utils.GetLerpValue(0, 40, ticksSinceFire, true));
            for (int i = 0; i < 2; i++)
            {
                Main.EntitySpriteDraw(
                    bloom,
                    position,
                    null,
                    auraColor,
                    Projectile.rotation,
                    bloom.Size() * 0.5f,
                    Vector2.Lerp(new Vector2(0.6f, 1.4f), Vector2.One, ringOpen) * 0.25f,
                    SpriteEffects.None);
            }
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] trailPoints = CoreOfCalamityEnergyOrb.BuildTrailPoints(Projectile);
            if (trailPoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    (completion, _) => MathF.Sin((1f - completion) * MathHelper.PiOver2) * 18f,
                    (completion, _) =>
                    {
                        Color color = Color.Lerp(OrbColor, Color.Transparent, completion);
                        color.A = 0;
                        return color;
                    },
                    (_, _) => Vector2.Zero,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                trailPoints.Length * 2);
        }

        private void SpawnFlightEffects(Vector2 direction)
        {
            if (!Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center,
                DustID.TintableDustLighted,
                -direction.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.6f, 2.4f),
                70,
                OrbColor,
                Main.rand.NextFloat(0.64f, 1.08f));
            dust.noGravity = true;
        }

        private void SpawnPreFireFlightEffects(Vector2 direction)
        {
            if (Main.dedServ || (Projectile.numUpdates != 0 && Main.rand.NextBool(3)))
                return;

            float phase = Projectile.localAI[1] + Timer * 0.09f;
            Color secondary = Color.Lerp(OrbColor, Color.White, 0.42f);

            // 侧翼波动火花（速度严格沿飞行方向，偏移由位置体现）
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 perp = direction.RotatedBy(MathHelper.PiOver2);
                Vector2 orbPos = Projectile.Center + perp * (side * (4f + MathF.Abs(MathF.Sin(phase)) * 5f)) - direction * 15f;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    orbPos,
                    -direction * Main.rand.NextFloat(0.35f, 1.2f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.12f, 0.23f),
                    Main.rand.NextBool() ? OrbColor : secondary,
                    new Vector2(0.34f, 1.15f),
                    true,
                    true,
                    extraRotation: 0f,
                    shrinkSpeed: 0.48f,
                    glowOpacity: 0.78f));
            }

            // 垂直方向随机漂移的柔性光球
            if (Main.rand.NextBool(4))
            {
                Vector2 perp = direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-14f, 14f);
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(10f, 28f) + perp,
                    -direction * Main.rand.NextFloat(0.3f, 1.0f),
                    Main.rand.NextFloat(0.14f, 0.27f),
                    Color.Lerp(OrbColor, Color.White, Main.rand.NextFloat(0.3f, 0.7f)),
                    Main.rand.Next(7, 13)));
            }
        }
    }
}
