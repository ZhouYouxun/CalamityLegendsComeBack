using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 左键特殊子弹：清爽的荧光绿曳光弹（流线拖尾 + 亮核，不用贴图、不糊大光斑）。
    /// 命中提升战术同步率并累积伸冤者印记；一层伤害 / 二层破甲。
    /// </summary>
    public class M4A1Bullet : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
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
            Lighting.AddLight(Projectile.Center, 0.18f, 0.5f, 0.12f);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            int marks = M4A1MarkGlobalNPC.Of(target).MarkLevel;
            if (marks >= 1)
                modifiers.SourceDamage *= 1f + BalanceM4A1.Mark1DamageBonus;
            if (marks >= 2)
                modifiers.ArmorPenetration += BalanceM4A1.Mark2ArmorPen;
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
            Vector2 origin = bloom.Size() * 0.5f;
            float rot = Projectile.rotation;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 拖尾残影（细、淡）
            int trailLen = ProjectileID.Sets.TrailCacheLength[Type];
            for (int i = 1; i < trailLen; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float t = 1f - i / (float)trailLen;
                Vector2 tp = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, tp, null, (M4A1Visuals.NeonGreen with { A = 0 }) * (0.28f * t), rot, origin, new Vector2(0.2f, 0.06f) * t, SpriteEffects.None, 0);
            }

            Vector2 pos = Projectile.Center - Main.screenPosition;
            // 流线拖尾（沿速度方向拉长的细光条）
            Main.EntitySpriteDraw(bloom, pos, null, (M4A1Visuals.NeonGreen with { A = 0 }) * 0.85f, rot, origin, new Vector2(0.34f, 0.075f), SpriteEffects.None, 0);
            // 亮核
            Main.EntitySpriteDraw(bloom, pos, null, (M4A1Visuals.NeonGreenBright with { A = 0 }) * 0.95f, 0f, origin, 0.085f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, pos, null, new Color(240, 255, 235, 0) * 0.9f, 0f, origin, 0.045f, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
