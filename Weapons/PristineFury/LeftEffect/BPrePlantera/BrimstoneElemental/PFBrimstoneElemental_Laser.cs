using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    // 原名 Laser，现重做为亮星空蓝 Necroplasm 风格球
    internal sealed class PFBrimstoneElemental_StarBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color BlueA = new Color(80, 200, 255);
        private static readonly Color BlueB = new Color(40, 130, 255);

        private int timer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 200;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            timer++;

            if (timer > 20)
            {
                NPC target = FindTarget(1400f);
                if (target != null)
                {
                    float trackingPower = Utils.GetLerpValue(20f, 130f, timer, true);
                    float lateBoost = Projectile.timeLeft < 90 ? 1.8f : 1f;
                    float speed = MathHelper.Lerp(8f, 20f, trackingPower) * lateBoost;
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, MathHelper.Lerp(0.08f, 0.34f, trackingPower));
                }
                else
                    Projectile.velocity *= 0.972f;
            }
            else
                Projectile.velocity *= 0.98f;

            Lighting.AddLight(Projectile.Center, BlueA.ToVector3() * 0.55f);
        }

        private NPC FindTarget(float maxDistance)
        {
            NPC result = null;
            float bestDistance = maxDistance;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;
                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                result = npc;
            }
            return result;
        }

        public override bool? CanDamage() => timer < 20 ? false : null;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 360);

            if (Main.dedServ)
                return;

            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.BlueTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f),
                    0, Color.Lerp(BlueA, Color.White, 0.3f), Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 14; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f),
                    0, Color.Lerp(BlueA, Color.White, 0.25f), Main.rand.NextFloat(0.8f, 1.4f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SmallGreyscaleCircle").Value;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float colorInterp = (float)Math.Cos(
                    Projectile.timeLeft / 30f +
                    Main.GlobalTimeWrappedHourly / 18f +
                    i / (float)Projectile.oldPos.Length * MathHelper.Pi
                ) * 0.5f + 0.5f;

                Color color = Color.Lerp(BlueA, BlueB, colorInterp) * 0.85f;
                color.A = 220;

                Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition
                    + new Vector2(0f, Projectile.gfxOffY);

                float intensity = MathHelper.Lerp(0.12f, 1f, 1f - i / (float)Projectile.oldPos.Length);

                if (Projectile.timeLeft <= 60)
                    intensity *= Projectile.timeLeft / 60f;

                Color outer = color * intensity;
                Color inner = color * intensity * 0.5f;

                float outerScale = intensity * 0.62f;
                float innerScale = intensity * 0.62f * 0.7f;

                Main.EntitySpriteDraw(tex, drawPos, null, outer, 0f, tex.Size() * 0.5f, outerScale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(tex, drawPos, null, inner, 0f, tex.Size() * 0.5f, innerScale, SpriteEffects.None, 0);
            }

            return false;
        }
    }
}
