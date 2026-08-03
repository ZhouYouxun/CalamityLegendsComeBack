using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 左键间歇火箭弹：沿用 AcidRocket 贴图，换成我们的荧光绿。轻微追踪，命中/触地引爆小范围战术爆破。
    /// </summary>
    public class M4A1Rocket : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/AcidRocket";

        private const int BaseBlastRadius = 120;
        private const float CruiseSpeed = 22f; // 恒定巡航速度：不再逐帧衰减
        private bool exploding;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 1; // 更快、更猛
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (exploding)
            {
                Projectile.velocity *= 0.15f;
                return;
            }

            // 轻微追踪最近敌人 —— lerp 后必须重新归一化，否则模长逐帧缩短会「越来越慢」
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            NPC target = FindNearestTarget(720f);
            if (target != null)
            {
                Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(dir);
                dir = Vector2.Lerp(dir, toTarget, 0.055f).SafeNormalize(dir);
            }
            Projectile.velocity = dir * CruiseSpeed; // 恒定速度巡航

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 荧光绿尾迹
            if (!Main.dedServ)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust fire = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.4f, DustID.GreenTorch,
                        -Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.6f, 0.6f), 90, default, Main.rand.NextFloat(1.2f, 1.8f));
                    fire.noGravity = true;
                }
                if (Main.rand.NextBool())
                {
                    Particle gas = new MediumMistParticle(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), -Projectile.velocity * 0.25f,
                        M4A1Visuals.NeonGreen, new Color(120, 180, 90), Main.rand.NextFloat(0.4f, 0.8f), 150, 0.03f);
                    GeneralParticleHandler.SpawnParticle(gas);
                }
            }
            Lighting.AddLight(Projectile.Center, 0.3f, 0.7f, 0.12f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            StartExplosion();
            return false;
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
            StartExplosion();
        }

        private void StartExplosion()
        {
            if (exploding)
                return;
            exploding = true;

            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = BaseBlastRadius;
            Projectile.Center = center;
            Projectile.tileCollide = false;
            Projectile.knockBack = 6f;
            if (Projectile.timeLeft > 3)
                Projectile.timeLeft = 3;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = 0.1f }, center);
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 center = Projectile.Center;
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.55f }, center);

            if (Main.dedServ)
                return;

            for (int i = 0; i < 24; i++)
            {
                Dust fire = Dust.NewDustPerfect(center, DustID.GreenTorch, Main.rand.NextVector2Circular(6f, 6f), 90, default, Main.rand.NextFloat(1.4f, 2.4f));
                fire.noGravity = true;
            }
            for (int i = 0; i < 18; i++)
            {
                Color smokeColor = Main.rand.NextBool() ? M4A1Visuals.NeonGreen : new Color(90, 160, 70);
                Particle smoke = new MediumMistParticle(center, Main.rand.NextVector2Circular(6f, 6f), smokeColor, Color.Black, Main.rand.NextFloat(1.2f, 2.6f), 180 - Main.rand.Next(50), 0.05f);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, M4A1Visuals.NeonGreen, 1.2f, 22, true));
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, M4A1Visuals.NeonGreenBright, 0.7f, 18, true));
        }

        private NPC FindNearestTarget(float range)
        {
            NPC best = null;
            float bestDist = range * range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;
                float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = npc;
                }
            }
            return best;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;

            // 单层绿色背光（非旋转贴图堆）
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(bloom, pos, null, (M4A1Visuals.NeonGreen with { A = 0 }) * 0.65f, 0f, bloom.Size() * 0.5f, 0.32f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(tex, pos, frame, Projectile.GetAlpha(Color.Lerp(lightColor, M4A1Visuals.NeonGreenBright, 0.5f)), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
