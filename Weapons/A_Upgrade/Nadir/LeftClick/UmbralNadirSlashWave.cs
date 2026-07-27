using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
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

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.LeftClick
{
    /// <summary>
    /// 冥蚀刃波 —— 左键每段挥砍沿矛尖甩出的黑绿虚空月刃，给近战提供一点中距离压制。
    /// 直线飞行、穿透数次，命中叠 1 层蚀痕并炸一记小黑洞。让左键的"斩开敌群"延伸到稍远处，也为右键终爆铺蚀痕。
    /// </summary>
    public class UmbralNadirSlashWave : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color Green = UmbralNadirPalette.MeldGreen;
        private readonly List<Vector2> trail = new();

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 44;
            Projectile.friendly = true;
            Projectile.penetrate = 4;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 46;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.985f;
            Lighting.AddLight(Projectile.Center, Green.ToVector3() * 0.4f);

            if (Projectile.FinalExtraUpdate())
            {
                trail.Insert(0, Projectile.Center);
                if (trail.Count > 10)
                    trail.RemoveAt(trail.Count - 1);

                GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    -Projectile.velocity * 0.1f, Color.Black, Main.rand.NextFloat(0.2f, 0.36f), Main.rand.Next(8, 12), true, false));
                if (Main.rand.NextBool(2))
                {
                    Dust vd = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>());
                    vd.noGravity = true;
                    vd.velocity = -Projectile.velocity * 0.12f;
                    vd.scale = Main.rand.NextFloat(0.8f, 1.3f);
                    vd.color = Green;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 90);
            UmbralCorrosionGlobalNPC.AddStacks(target, 1);
            UmbralNadirVisuals.EventHorizon(Projectile.Center, 0.34f, false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 黑→绿刃波拖痕
            if (trail.Count >= 2)
                UmbralNadirVisuals.RenderTipTrail(trail, 30f, 1f, false);

            // 月刃前缘：黑核 + 绿边，垂直于飞行方向拉长成刃
            Asset<Texture2D> soft = ModContent.Request<Texture2D>("CalamityMod/Particles/SmallBloom");
            Asset<Texture2D> bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float rot = Projectile.velocity.ToRotation();
            Main.EntitySpriteDraw(bloom.Value, pos, null, Green with { A = 0 }, rot, bloom.Value.Size() * 0.5f, new Vector2(0.28f, 0.7f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(soft.Value, pos, null, Color.Black * 0.9f, rot, soft.Value.Size() * 0.5f, new Vector2(0.12f, 0.34f), SpriteEffects.None, 0);
            return false;
        }
    }
}
