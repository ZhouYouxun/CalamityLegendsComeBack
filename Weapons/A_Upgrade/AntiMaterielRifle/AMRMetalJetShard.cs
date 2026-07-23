using System;
using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    internal sealed class AMRMetalJetShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/Ranged/AMRShot";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 2;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 60;
            Projectile.scale = 0.85f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GoldFlame,
                    Projectile.velocity * 0.2f,
                    0,
                    new Color(255, 205, 80),
                    Main.rand.NextFloat(0.6f, 1.1f));
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Stage 0 真实伤害机制 (10% 基础，Boss 0.5%，受 DR 加成)
            if (target.active && target.lifeMax > 5)
            {
                bool isBoss = target.boss || target.type == NPCID.TargetDummy || target.realLife >= 0;
                float trueDamageRatio = isBoss ? 0.005f : 0.10f;
                float trueDamage = target.lifeMax * trueDamageRatio;

                // DR (Damage Reduction) 提升真实伤害
                float dr = target.Calamity().DR;
                if (dr > 0f)
                    trueDamage *= (1f + dr);

                int finalTrueDamage = Math.Max(1, (int)trueDamage);

                // 造成独立真实伤害
                hit.HideCombatText = false;
                target.life -= finalTrueDamage;
                CombatText.NewText(target.getRect(), new Color(255, 140, 40), finalTrueDamage, true);

                if (target.life <= 0)
                    target.checkDead();
            }

            // Stage 1 克眼强化：防御力永久降低 60%
            if (AMRBalance.DeathMarkUnlocked)
            {
                target.AddBuff(ModContent.BuffType<MarkedforDeath>(), 5 * 60);
                int defenseLoss = Math.Max(25, (int)(target.defense * 0.6f));
                target.Calamity().miscDefenseLoss = Math.Max(target.Calamity().miscDefenseLoss, defenseLoss);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, drawPos, null, new Color(255, 180, 50, 0), 0f,
                bloom.Size() * 0.5f, 0.12f * Projectile.scale, SpriteEffects.None, 0f);

            Main.EntitySpriteDraw(texture, drawPos, null, new Color(255, 230, 160, 0),
                Projectile.rotation, origin, new Vector2(0.8f, 1.4f) * Projectile.scale, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
