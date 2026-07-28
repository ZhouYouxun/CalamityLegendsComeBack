using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.LeftClick
{
    /// <summary>
    /// 散射虚空弹 —— 左键上挑 / 劈落时，随挥舞进程一颗颗甩出的黑绿虚空弹。
    /// 独有发射技巧：出膛先短暂"蓄势减速"，随后猛地爆冲加速；一小段延迟后微微咬向蚀痕最深的敌人。
    /// 命中叠 1 层蚀痕并炸一记微型黑洞。多颗沿挥舞弧线扇形铺开，形成真正的"扫射"。
    /// </summary>
    public class UmbralNadirVoidBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color Green = UmbralNadirPalette.MeldGreen;
        public ref float Time => ref Projectile.localAI[0];
        private Vector2 lastTrailPosition;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 26;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 90;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, Green.ToVector3() * 0.3f);

            // 发射技巧：先蓄势减速，后爆冲加速
            float speed = Projectile.velocity.Length();
            if (Time < 8f)
                Projectile.velocity *= 0.93f;
            else if (speed < 22f)
                Projectile.velocity *= 1.055f;

            // 追踪从轻微修正逐渐成长为强力咬合；后半程即使目标拐到身后也能拉回弹道。
            if (Time > 8f)
            {
                float homingRamp = MathHelper.Clamp((Time - 8f) / 52f, 0f, 1f);
                homingRamp = MathF.Pow(homingRamp, 1.45f);
                NPC t = FindCorrodedTarget(MathHelper.Lerp(560f, 820f, homingRamp));
                if (t != null)
                {
                    Vector2 toTarget = t.Center - Projectile.Center;
                    float currentRotation = Projectile.velocity.ToRotation();
                    float desiredRotation = toTarget.SafeNormalize(Vector2.UnitX).ToRotation();
                    float maxTurn = MathHelper.Lerp(0.012f, 0.19f, homingRamp);
                    if (toTarget.LengthSquared() < 180f * 180f)
                        maxTurn *= 1.35f;
                    float turn = MathHelper.Clamp(MathHelper.WrapAngle(desiredRotation - currentRotation), -maxTurn, maxTurn);
                    Projectile.velocity = (currentRotation + turn).ToRotationVector2() * Projectile.velocity.Length();
                }
            }

            // 黑绿拖尾沿真实帧间路径补点，避免高速时只剩互不相连的黑圆点。
            if (Projectile.FinalExtraUpdate())
            {
                SpawnConnectedTrail();
            }
        }

        private void SpawnConnectedTrail()
        {
            if (lastTrailPosition == Vector2.Zero)
                lastTrailPosition = Projectile.Center - Projectile.velocity * (Projectile.extraUpdates + 1);

            Vector2 segment = Projectile.Center - lastTrailPosition;
            int samples = Math.Clamp((int)MathF.Ceiling(segment.Length() / 11f), 2, 8);
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 1; i <= samples; i++)
            {
                Vector2 p = Vector2.Lerp(lastTrailPosition, Projectile.Center, i / (float)samples);
                float edgeFade = 0.78f + 0.22f * i / samples;
                GeneralParticleHandler.SpawnParticle(new GenericBloom(p, back * 0.35f, Color.Black,
                    Main.rand.NextFloat(0.12f, 0.2f) * edgeFade, Main.rand.Next(8, 12), true, false));
            }

            GeneralParticleHandler.SpawnParticle(new LineParticle(
                Vector2.Lerp(lastTrailPosition, Projectile.Center, 0.55f), back * 0.25f, false,
                Main.rand.Next(7, 11), Main.rand.NextFloat(0.5f, 0.8f), Color.Lerp(Color.Black, UmbralNadirPalette.MeldGreenDeep, 0.18f)));

            if (Main.rand.NextBool(2))
            {
                Dust vd = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>());
                vd.noGravity = true;
                vd.velocity = back * Main.rand.NextFloat(0.3f, 1.2f);
                vd.scale = Main.rand.NextFloat(0.6f, 1f);
                vd.color = Green;
            }
            lastTrailPosition = Projectile.Center;
        }

        private NPC FindCorrodedTarget(float range)
        {
            NPC best = null;
            float bestScore = float.MinValue;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;
                float dist = Projectile.Distance(npc.Center);
                if (dist > range)
                    continue;
                float score = UmbralCorrosionGlobalNPC.GetStacks(npc) * 40f - dist;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = npc;
                }
            }
            return best;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 90);
            UmbralCorrosionGlobalNPC.AddStacks(target, 1);
            UmbralNadirVisuals.EventHorizon(Projectile.Center, 0.3f, false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacity = Projectile.Opacity;
            Asset<Texture2D> body = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored");
            Asset<Texture2D> bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Vector2 pos = Projectile.Center - Main.screenPosition;

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            Vector2 trailOffset = Projectile.Size * 0.5f;
            PrimitiveRenderer.RenderTrail(Projectile.oldPos,
                new PrimitiveSettings(
                    (c, _) => MathHelper.Lerp(25f, 2f, c) * opacity,
                    (c, _) => Color.Lerp(Color.Black, UmbralNadirPalette.MeldGreenDeep, c * 0.22f) * (1f - c * 0.82f) * opacity,
                    (_, _) => trailOffset, shader: GameShaders.Misc["CalamityMod:TrailStreak"]), 56);
            PrimitiveRenderer.RenderTrail(Projectile.oldPos,
                new PrimitiveSettings(
                    (c, _) => MathHelper.Lerp(10f, 1f, c) * opacity,
                    (c, _) => Color.Lerp(UmbralNadirPalette.MeldGreenDeep, Green, c) with { A = 0 } * (0.5f - c * 0.42f) * opacity,
                    (_, _) => trailOffset, shader: GameShaders.Misc["CalamityMod:TrailStreak"]), 56);

            // 拉长的黑色弹体（透明底 WaterFlavored）
            Main.EntitySpriteDraw(body.Value, pos, null, Color.Black * (0.9f * opacity), Projectile.rotation,
                body.Value.Size() * 0.5f, new Vector2(0.24f, 0.72f), SpriteEffects.None, 0);
            // 荧绿核
            Main.EntitySpriteDraw(bloom.Value, pos, null, Green with { A = 0 } * opacity, 0f,
                bloom.Value.Size() * 0.5f, 0.1f, SpriteEffects.None, 0);
            return false;
        }
    }
}
