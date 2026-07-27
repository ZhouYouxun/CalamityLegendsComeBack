using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.Shared;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.LeftClick
{
    /// <summary>
    /// 天底奇点 —— 左键第三段冲刺贯穿命中时撕开的持续黑洞。
    /// 存活约 1.6 秒：持续把周围敌人吸向核心、周期性 DoT 并叠加蚀痕；消失时坍缩爆发。
    /// 是左键"把敌群斩开并卷成一团"的核心，为右键终爆/回旋新星的高层数消耗做铺垫。
    /// </summary>
    public class UmbralNadirSingularity : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color Green = UmbralNadirPalette.MeldGreen;

        /// <summary>坍缩爆发用的基准伤害（= 左键 holdout 伤害，生成时写入）。</summary>
        public ref float BaseDamage => ref Projectile.ai[0];

        private float spin;
        private int Age => UmbralNadirBalance.SingularityDuration - Projectile.timeLeft;
        private float VisualScale
        {
            get
            {
                float open = Utils.GetLerpValue(0f, 12f, Age, true);
                float close = Utils.GetLerpValue(UmbralNadirBalance.SingularityDuration - 16, UmbralNadirBalance.SingularityDuration, Age, true);
                return MathHelper.SmoothStep(0f, 1f, open) * (1f - MathHelper.SmoothStep(0f, 1f, close)) * (1f + 0.05f * (float)Math.Sin(Age * 0.4f));
            }
        }

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = UmbralNadirBalance.SingularityDuration;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override bool? CanDamage() => Age >= 12 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CalamityUtils.CircularHitboxCollision(Projectile.Center, 70f * MathHelper.Clamp(VisualScale, 0.3f, 1.2f), targetHitbox);

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldExplosion") with { Volume = 0.55f, Pitch = -0.4f }, Projectile.Center);
            UmbralNadirVisuals.EventHorizon(Projectile.Center, 0.7f, false);
            UmbralNadirVisuals.ScreenShake(Projectile.Center, 3f);
        }

        public override void AI()
        {
            spin += 0.16f + 0.12f * Utils.GetLerpValue(UmbralNadirBalance.SingularityDuration - 30, UmbralNadirBalance.SingularityDuration, Age, true);
            Projectile.rotation = spin;
            Lighting.AddLight(Projectile.Center, 0.25f, 0.75f, 0.4f);

            // 持续吸引周围敌人
            UmbralNadirVisuals.PullNPCs(Projectile.Center, UmbralNadirBalance.SingularityPullRange * MathHelper.Clamp(VisualScale, 0.4f, 1.2f), UmbralNadirBalance.SingularityPullStrength);

            // 向心的碎渊尘
            if (VisualScale > 0.2f)
                for (int i = 0; i < 2; i++)
                {
                    float r = Main.rand.NextFloat(90f, 220f) * VisualScale;
                    Vector2 edge = Projectile.Center + Main.rand.NextVector2Unit() * r;
                    Vector2 inward = (Projectile.Center - edge).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(3f, 7f);
                    Dust vd = Dust.NewDustPerfect(edge, ModContent.DustType<VoidDustInverted>(), inward, 0, Green, Main.rand.NextFloat(0.8f, 1.4f));
                    vd.noGravity = true;
                    vd.color = Green;
                }

            // 奇点持续吐出冥融虚空核，追向就近敌人（复用旧作弹幕，串进本武器的蚀痕循环）
            if (Projectile.owner == Main.myPlayer && Age % 20 == 10 && Age < UmbralNadirBalance.SingularityDuration - 18)
            {
                NPC t = Projectile.Center.ClosestNPCAt(UmbralNadirBalance.SingularityPullRange);
                Vector2 v = (t != null ? (t.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) : Main.rand.NextVector2Unit()) * 8f;
                int essDamage = Math.Max(1, (int)(BaseDamage * 0.3f));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, v,
                    ModContent.ProjectileType<UmbralNadirVoidEssence>(), essDamage, Projectile.knockBack * 0.4f, Projectile.owner, 0f, 0f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 120);
            UmbralCorrosionGlobalNPC.AddStacks(target, 1);
        }

        public override void OnKill(int timeLeft)
        {
            UmbralNadirVisuals.EventHorizon(Projectile.Center, 1.4f, true);
            UmbralNadirVisuals.ImplosionDust(Projectile.Center, 1.4f);
            UmbralNadirVisuals.MeldSparkBurst(Projectile.Center, 22, 9f);
            UmbralNadirVisuals.ScreenShake(Projectile.Center, 4.5f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldExplosion") with { Volume = 0.85f, Pitch = -0.15f }, Projectile.Center);

            // 坍缩爆发伤害（复用左键冲击弹幕，段位=2 半径最大）
            if (Projectile.owner == Main.myPlayer)
            {
                int detonate = Math.Max(1, (int)(BaseDamage * UmbralNadirBalance.SingularityDetonateMult));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<UmbralNadirImpactExplosion>(), detonate, Projectile.knockBack, Projectile.owner, 2f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float s = VisualScale;
            if (s <= 0.01f)
                return false;

            Asset<Texture2D> water = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored");
            Asset<Texture2D> bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Asset<Texture2D> soft = ModContent.Request<Texture2D>("CalamityMod/Particles/SmallBloom");
            Vector2 pos = Projectile.Center - Main.screenPosition;

            // 背后的荧绿事件视界环（加色，旋转）
            for (int i = 0; i < 2; i++)
                Main.EntitySpriteDraw(bloom.Value, pos, null, Green with { A = 0 } * (0.85f - i * 0.25f),
                    spin * (i == 0 ? 1f : -1f), bloom.Value.Size() * 0.5f, (0.7f + i * 0.4f) * s, SpriteEffects.None, 0);

            // 一圈黑色花瓣拼成吞光的旋涡口
            int petals = 9;
            for (int i = 0; i < petals; i++)
            {
                float ang = MathHelper.TwoPi * i / petals + spin;
                Vector2 offset = ang.ToRotationVector2() * 32f * s;
                Vector2 outward = offset.SafeNormalize(Vector2.UnitX);
                Main.EntitySpriteDraw(water.Value, pos + offset, null, Color.Black * 0.9f,
                    outward.ToRotation() + MathHelper.ToRadians(-90f), water.Value.Size() * 0.5f, new Vector2(0.36f, 1.1f) * s, SpriteEffects.None, 0);
            }

            // 无底纯黑核心（透明底 SmallBloom）
            Main.EntitySpriteDraw(soft.Value, pos, null, Color.Black, spin, soft.Value.Size() * 0.5f, 0.22f * s, SpriteEffects.None, 0);
            // 中央亮绿凝视点
            Main.EntitySpriteDraw(bloom.Value, pos, null, UmbralNadirPalette.MeldGreenBright with { A = 0 }, 0f, bloom.Value.Size() * 0.5f, 0.12f * s, SpriteEffects.None, 0);
            return false;
        }
    }
}
