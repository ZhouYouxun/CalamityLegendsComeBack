using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.RightClick
{
    /// <summary>
    /// 高速投矛划开的短命冥幕：以投掷方向展开一条大范围黑绿斩幕。
    /// 本体只在展开前半段造成一次路径伤害，后半段纯粹作为逐渐熄灭的视觉残留。
    /// </summary>
    public class UmbralNadirJavelinCurtain : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float DirectionAngle => ref Projectile.ai[0];
        private bool IsFinisher => Projectile.ai[1] > 0.5f;
        private ref float Time => ref Projectile.localAI[0];
        private Vector2 Direction => DirectionAngle.ToRotationVector2();
        private Vector2 Normal => Direction.RotatedBy(MathHelper.PiOver2);
        private float CurtainLength => UmbralNadirBalance.JavelinCurtainLength * (IsFinisher ? 1.24f : 1f);
        private float CurtainWidth => UmbralNadirBalance.JavelinCurtainWidth * (IsFinisher ? 1.28f : 1f);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 24;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = DirectionAngle;
            Lighting.AddLight(Projectile.Center, UmbralNadirPalette.MeldGreen.ToVector3() * (IsFinisher ? 0.55f : 0.36f));

            if (Time == 1f)
            {
                SpawnOpeningDust();
                UmbralNadirVisuals.ScreenShake(Projectile.Center, IsFinisher ? 2.8f : 1.35f);
            }
        }

        public override bool? CanDamage() => Time >= 2f && Time <= (IsFinisher ? 11f : 9f);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            GetEndpoints(out Vector2 start, out Vector2 end);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                start, end, CurtainWidth, ref collisionPoint);
        }

        private void GetEndpoints(out Vector2 start, out Vector2 end)
        {
            start = Projectile.Center - Direction * CurtainLength * 0.22f;
            end = Projectile.Center + Direction * CurtainLength * 0.78f;
        }

        private void SpawnOpeningDust()
        {
            GetEndpoints(out Vector2 start, out Vector2 end);
            int count = IsFinisher ? 28 : 20;
            for (int i = 0; i < count; i++)
            {
                float t = (i + Main.rand.NextFloat()) / count;
                Vector2 p = Vector2.Lerp(start, end, t) + Normal * Main.rand.NextFloat(-CurtainWidth * 0.42f, CurtainWidth * 0.42f);
                Dust dust = Dust.NewDustPerfect(p, ModContent.DustType<VoidDustInverted>(),
                    Normal * Main.rand.NextFloat(-1.8f, 1.8f) - Direction * Main.rand.NextFloat(0.2f, 1.3f),
                    0, UmbralNadirPalette.MeldGreen, Main.rand.NextFloat(0.7f, 1.35f));
                dust.noGravity = true;
                dust.color = UmbralNadirPalette.MeldGreen;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), IsFinisher ? 150 : 90);
            UmbralCorrosionGlobalNPC.AddStacks(target, 1);
            UmbralNadirVisuals.EventHorizon(target.Center, IsFinisher ? 0.34f : 0.24f, false);
        }

        private List<Vector2> BuildPath(float lateralOffset, float phase)
        {
            GetEndpoints(out Vector2 start, out Vector2 end);
            List<Vector2> points = new(13);
            for (int i = 0; i < 13; i++)
            {
                float t = i / 12f;
                float bow = MathF.Sin(t * MathHelper.Pi) * MathF.Sin(phase + t * 5.4f) * CurtainWidth * 0.16f;
                points.Add(Vector2.Lerp(start, end, t) + Normal * (lateralOffset + bow));
            }
            return points;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float lifeProgress = MathHelper.Clamp(Time / 24f, 0f, 1f);
            float opacity = MathF.Sin(lifeProgress * MathHelper.Pi);
            float phase = Main.GlobalTimeWrappedHourly * 6f + Projectile.identity * 0.7f;

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            var shader = GameShaders.Misc["CalamityMod:TrailStreak"];

            List<Vector2> center = BuildPath(0f, phase);
            List<Vector2> upper = BuildPath(CurtainWidth * 0.42f, phase + 1.1f);
            List<Vector2> lower = BuildPath(-CurtainWidth * 0.42f, phase - 1.1f);

            PrimitiveRenderer.RenderTrail(center,
                new PrimitiveSettings(
                    (c, _) => MathF.Sin(c * MathHelper.Pi) * CurtainWidth * 1.7f * opacity + 3f,
                    (c, _) => Color.Black * (0.68f * MathF.Sin(c * MathHelper.Pi) * opacity),
                    (_, _) => Vector2.Zero, shader: shader), 110);
            PrimitiveRenderer.RenderTrail(upper,
                new PrimitiveSettings(
                    (c, _) => MathF.Sin(c * MathHelper.Pi) * CurtainWidth * 0.42f * opacity + 2f,
                    (c, _) => UmbralNadirPalette.MeldGreenDeep with { A = 0 } * (0.42f * MathF.Sin(c * MathHelper.Pi) * opacity),
                    (_, _) => Vector2.Zero, shader: shader), 90);
            PrimitiveRenderer.RenderTrail(lower,
                new PrimitiveSettings(
                    (c, _) => MathF.Sin(c * MathHelper.Pi) * CurtainWidth * 0.36f * opacity + 2f,
                    (c, _) => UmbralNadirPalette.MeldGreen with { A = 0 } * (0.34f * MathF.Sin(c * MathHelper.Pi) * opacity),
                    (_, _) => Vector2.Zero, shader: shader), 90);
            PrimitiveRenderer.RenderTrail(center,
                new PrimitiveSettings(
                    (c, _) => MathF.Sin(c * MathHelper.Pi) * CurtainWidth * 0.13f * opacity + 1f,
                    (c, _) => Color.Lerp(Color.White, UmbralNadirPalette.MeldGreen, c) with { A = 0 } *
                              (0.56f * MathF.Sin(c * MathHelper.Pi) * opacity),
                    (_, _) => Vector2.Zero, shader: shader), 110);

            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearSmokey").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float smearScale = CurtainWidth * 2.4f / Math.Max(smear.Width, smear.Height);
            Main.EntitySpriteDraw(smear, drawPos, null, Color.Black * (0.44f * opacity), -phase * 0.25f,
                smear.Size() * 0.5f, new Vector2(smearScale * 1.5f, smearScale * 0.8f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(smear, drawPos, null, UmbralNadirPalette.MeldGreen with { A = 0 } * (0.24f * opacity),
                phase * 0.18f, smear.Size() * 0.5f, smearScale, SpriteEffects.None, 0);
            return false;
        }
    }
}
