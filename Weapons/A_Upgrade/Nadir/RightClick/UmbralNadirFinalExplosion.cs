using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.RightClick
{
    /// <summary>
    /// 第三发投矛命中的终爆——冥蚀天底"呼应循环"的结算点。
    /// 生成时消耗半径内所有敌人的蚀痕层数，消耗越多，本次爆发伤害越高；随后一次范围坍缩。
    /// 只由第三发命中 NPC 时生成；撞墙 / 超时绝不生成它。
    /// </summary>
    public class UmbralNadirFinalExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 18;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? CanDamage() => Projectile.timeLeft >= 16 ? null : false; // 仅前 2 帧

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CalamityUtils.CircularHitboxCollision(Projectile.Center, UmbralNadirBalance.FinalExplosionRadius, targetHitbox);

        public override void OnSpawn(IEntitySource source)
        {
            Vector2 c = Projectile.Center;
            float radius = UmbralNadirBalance.FinalExplosionRadius;

            // 消耗半径内所有蚀痕，按总层数放大本次伤害
            int totalStacks = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || Vector2.Distance(npc.Center, c) > radius)
                    continue;
                int s = UmbralCorrosionGlobalNPC.ConsumeStacks(npc);
                if (s > 0)
                {
                    totalStacks += s;
                    // 每个被引爆的蚀痕点上闪一记小黑洞
                    UmbralNadirVisuals.EventHorizon(npc.Center, 0.32f, false);
                    npc.AddBuff(ModContent.BuffType<Voidfrost>(), 180);
                }
            }
            if (totalStacks > 0)
                Projectile.damage += (int)(Projectile.damage * UmbralNadirBalance.FinalStackBonusFraction * totalStacks);

            bool big = totalStacks >= 6;
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldExplosion") with { Volume = big ? 1f : 0.85f, Pitch = big ? -0.25f : -0.1f }, c);
            UmbralNadirVisuals.EventHorizon(c, big ? 1.5f : 1.15f, true);
            UmbralNadirVisuals.ImplosionDust(c, big ? 1.6f : 1.2f);
            UmbralNadirVisuals.MeldSparkBurst(c, big ? 26 : 18, big ? 10f : 8f);
            UmbralNadirVisuals.ScreenShake(c, UmbralNadirBalance.FinalExplosionScreenShake + (big ? 1.5f : 0f));

            // 碎渊：把"引爆"重新变成"再叠层"的起点
            if (Projectile.owner == Main.myPlayer)
            {
                int shards = big ? 6 : 4;
                int shardDamage = Math.Max(1, (int)(Projectile.damage * 0.32f));
                for (int i = 0; i < shards; i++)
                {
                    Vector2 v = (MathHelper.TwoPi * i / shards + Main.rand.NextFloat(-0.3f, 0.3f)).ToRotationVector2() * Main.rand.NextFloat(5f, 9f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), c, v,
                        ModContent.ProjectileType<UmbralNadirVoidShard>(), shardDamage, Projectile.knockBack * 0.4f, Projectile.owner);
                }
            }
        }

        public override void AI() => Projectile.velocity = Vector2.Zero;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => target.AddBuff(ModContent.BuffType<Voidfrost>(), 180);

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / 18f;
            float opacity = MathF.Sin(progress * MathHelper.Pi);
            float spin = Projectile.identity * 0.37f + progress * MathHelper.TwoPi * 1.45f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Texture2D circularSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearSmokey").Value;
            Texture2D halfSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmearSwipe").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Color deepGreen = UmbralNadirPalette.MeldGreenDeep with { A = 0 };
            Color brightGreen = UmbralNadirPalette.MeldGreenBright with { A = 0 };
            Main.EntitySpriteDraw(circularSmear, drawPos, null, deepGreen * (0.62f * opacity), -spin * 0.48f,
                circularSmear.Size() * 0.5f, new Vector2(1.45f + progress * 1.85f, 1.04f + progress * 1.28f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring, drawPos, null, brightGreen * (0.48f * opacity), spin * 0.2f,
                ring.Size() * 0.5f, 0.88f + progress * 2.05f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring, drawPos, null, deepGreen * (0.38f * opacity), -spin * 0.32f,
                ring.Size() * 0.5f, 0.42f + progress * 2.7f, SpriteEffects.None, 0);

            // 八条旋臂以等角展开，并用两组不同半径的光点打破单一圆形爆炸的局限。
            const int arms = 8;
            for (int i = 0; i < arms; i++)
            {
                float armAngle = spin + MathHelper.TwoPi * i / arms;
                float radius = 34f + progress * 138f + 14f * MathF.Sin(progress * MathHelper.TwoPi * 1.5f + i);
                Vector2 armOffset = armAngle.ToRotationVector2() * radius;
                Color armColor = i % 2 == 0 ? brightGreen : deepGreen;
                Main.EntitySpriteDraw(halfSmear, drawPos, null, armColor * (0.5f * opacity), armAngle,
                    halfSmear.Size() * 0.5f, new Vector2(0.76f + progress * 1.15f, 0.34f + progress * 0.44f), SpriteEffects.None, 0);
                Main.EntitySpriteDraw(bloom, drawPos + armOffset, null, armColor * (0.62f * opacity), 0f,
                    bloom.Size() * 0.5f, 0.16f + progress * 0.3f, SpriteEffects.None, 0);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
