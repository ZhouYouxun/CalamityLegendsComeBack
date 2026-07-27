using System;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.RightClick
{
    /// <summary>
    /// 暗影魂针 —— 前两发投矛飞行时从其轨迹旁被"刺破的阴影"里钻出的细长黑色魂针。
    /// 出生后锁定 600px 内最近的可击敌人，8 帧转向进入轨道后以约 10px/帧追击；
    /// 无目标则短暂漂移后淡出。仅命中一次；不分裂、不放触手、不递归生成。
    /// 纯代码绘制的黑色针体 + 极小荧绿核，不做圆形追踪核心。
    /// </summary>
    public class UmbralNadirShadowSoul : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color MeldGreen = Color.LightGreen;
        private const float HomeSpeed = 10f;

        public ref float Time => ref Projectile.localAI[0];

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 180;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, MeldGreen.ToVector3() * 0.2f);

            // 稀疏黑砂拖尾 + 偶发深渊识别点
            if (Main.rand.NextBool(2))
                GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center,
                    -Projectile.velocity * 0.1f, Color.Black, Main.rand.NextFloat(0.12f, 0.22f), Main.rand.Next(7, 11), true, false));
            if (Main.rand.NextBool(4))
            {
                Dust vd = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>());
                vd.noGravity = true;
                vd.velocity = -Projectile.velocity * 0.15f;
                vd.scale = Main.rand.NextFloat(0.5f, 0.85f);
                vd.color = MeldGreen;
            }

            NPC target = Projectile.Center.ClosestNPCAt(600f);
            bool canHit = target != null && Collision.CanHit(Projectile.Center, 1, 1, target.Center, 1, 1);

            if (canHit)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * HomeSpeed;
                float turnIn = MathHelper.Clamp(Time / 8f, 0f, 1f);       // 8 帧转向进入轨道
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.05f + 0.16f * turnIn);
                if (Projectile.velocity.Length() > HomeSpeed + 1f)
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * (HomeSpeed + 1f);
            }
            else if (Time > 18f)
            {
                // 无目标：短暂漂移后淡出
                Projectile.velocity *= 0.95f;
                Projectile.alpha += 16;
                if (Projectile.alpha >= 255)
                    Projectile.Kill();
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 90);
            // 小型黑核闪烁 + 少量黑砂 + 一个小绿边缘脉冲
            GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center, Vector2.Zero, Color.Black, 0.32f, 10, true, false));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, MeldGreen with { A = 0 },
                "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.4f, 12, true),
                false, GeneralDrawLayer.AfterEverything);
            for (int i = 0; i < Main.rand.Next(4, 7); i++)
                GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center,
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4f), Color.Black,
                    Main.rand.NextFloat(0.14f, 0.26f), Main.rand.Next(8, 12), true, false));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacity = Projectile.Opacity;
            Asset<Texture2D> needle = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored");
            Asset<Texture2D> bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Vector2 pos = Projectile.Center - Main.screenPosition;

            // 细长黑色魂针（透明底 WaterFlavored，AlphaBlend 吸光）
            Main.EntitySpriteDraw(needle.Value, pos, null, Color.Black * (0.9f * opacity), Projectile.rotation,
                needle.Value.Size() * 0.5f, new Vector2(0.22f, 0.95f), SpriteEffects.None, 0);
            // 极小荧绿核（加色，with{A=0}）
            Main.EntitySpriteDraw(bloom.Value, pos, null, MeldGreen with { A = 0 } * opacity, 0f,
                bloom.Value.Size() * 0.5f, 0.09f, SpriteEffects.None, 0);
            return false;
        }
    }
}
