using System;
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

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

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

            // 延迟后微咬向蚀痕最深的敌人（与左键标记呼应）
            if (Time > 12f)
            {
                NPC t = FindCorrodedTarget(520f);
                if (t != null)
                {
                    Vector2 desired = (t.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * Projectile.velocity.Length();
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.06f);
                }
            }

            // 黑绿拖尾
            if (Projectile.FinalExtraUpdate())
            {
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center, back * 0.4f, Color.Black,
                    Main.rand.NextFloat(0.14f, 0.26f), Main.rand.Next(7, 11), true, false));
                if (Main.rand.NextBool(2))
                {
                    Dust vd = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>());
                    vd.noGravity = true;
                    vd.velocity = back * Main.rand.NextFloat(0.3f, 1.2f);
                    vd.scale = Main.rand.NextFloat(0.6f, 1f);
                    vd.color = Green;
                }
            }
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
                if (dist > range || !Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1))
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
