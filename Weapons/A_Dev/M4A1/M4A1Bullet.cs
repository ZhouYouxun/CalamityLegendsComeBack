using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 左键 M4A1 子弹。命中提升战术同步率并累积复仇印记（印记效果在 Phase 2 接线）。
    /// </summary>
    public class M4A1Bullet : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation();

            // 轻量曳光
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Projectile.velocity * 0.05f, 120, default, 0.6f);
                d.noGravity = true;
                d.color = new Color(255, 170, 90);
            }
            Lighting.AddLight(Projectile.Center, 0.4f, 0.22f, 0.08f);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            int marks = M4A1MarkGlobalNPC.Of(target).MarkLevel;
            if (marks >= 1)
                modifiers.SourceDamage *= 1f + BalanceM4A1.Mark1DamageBonus; // 一层：小幅提高伤害
            if (marks >= 2)
                modifiers.ArmorPenetration += BalanceM4A1.Mark2ArmorPen;      // 二层：少量破甲
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Player owner = Main.player[Projectile.owner];
                bool isBoss = target.boss || NPCID.Sets.ShouldBeCountedAsBoss[target.type];
                M4A1Player.Get(owner).GainSync(isBoss, hit.Crit);
                M4A1MarkGlobalNPC.RegisterHit(target, owner, damageDone);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLine").Value;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Color hot = new Color(255, 150, 70, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 曳光拖尾
            Main.EntitySpriteDraw(line, pos, null, hot * 0.85f, Projectile.rotation + MathHelper.PiOver2, line.Size() * new Vector2(0.5f, 1f),
                new Vector2(0.06f, 0.32f), SpriteEffects.None, 0);
            // 弹头光点
            Main.EntitySpriteDraw(bloom, pos, null, hot * 0.9f, 0f, bloom.Size() * 0.5f, 0.10f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, pos, null, new Color(255, 245, 220, 0) * 0.9f, 0f, bloom.Size() * 0.5f, 0.05f, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
