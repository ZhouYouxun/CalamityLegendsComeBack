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

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.RightClick
{
    /// <summary>
    /// 虚空碎渊 —— 终爆 / 奇点新星炸开时甩出的黑色碎片，短暂扩散后追向最近的敌人。
    /// 命中叠 1 层蚀痕：把"引爆蚀痕"的终结重新变成"再叠层"的起点，让循环不断续上。仅命中一次。
    /// </summary>
    public class UmbralNadirVoidShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color Green = UmbralNadirPalette.MeldGreen;
        public ref float Time => ref Projectile.localAI[0];

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 90;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, Green.ToVector3() * 0.22f);

            // 先扩散，再追击
            if (Time < 8f)
                Projectile.velocity *= 0.9f;
            else
            {
                NPC target = Projectile.Center.ClosestNPCAt(560f);
                if (target != null && Collision.CanHit(Projectile.Center, 1, 1, target.Center, 1, 1))
                {
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 11f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.14f);
                }
            }

            if (Main.rand.NextBool(2))
                GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center, -Projectile.velocity * 0.1f,
                    Color.Black, Main.rand.NextFloat(0.1f, 0.2f), Main.rand.Next(6, 10), true, false));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 90);
            UmbralCorrosionGlobalNPC.AddStacks(target, 1);
            GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center, Vector2.Zero, Color.Black, 0.28f, 9, true, false));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Green with { A = 0 },
                "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.35f, 10, true),
                false, GeneralDrawLayer.AfterEverything);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacity = Projectile.Opacity;
            Asset<Texture2D> needle = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored");
            Asset<Texture2D> bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(needle.Value, pos, null, Color.Black * (0.9f * opacity), Projectile.rotation,
                needle.Value.Size() * 0.5f, new Vector2(0.2f, 0.85f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom.Value, pos, null, Green with { A = 0 } * opacity, 0f,
                bloom.Value.Size() * 0.5f, 0.08f, SpriteEffects.None, 0);
            return false;
        }
    }
}
