using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.LeftClick
{
    /// <summary>
    /// 冥蚀贯穿矛 —— 左键第三段冲刺贯穿在突入瞬间射出的宏伟黑暗贯穿光矛。
    /// 11 次/帧的极高更新凸显庞大动能冲击，笔直匀速前进、无限穿透，拖出黑→荧绿双股虚空螺旋与巨大黑核光束。
    /// 每贯穿一个敌人都炸开一记中等的黑洞爆裂（借冥蚀冲击的分层事件视界）。
    /// </summary>
    public class UmbralNadirVoidLance : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color Green = UmbralNadirPalette.MeldGreen;
        private Vector2 lastFrameCenter;
        private int frameTimer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 34;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;                                       // 无限穿透
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = UmbralNadirBalance.VoidLanceTimeLeft;       // 600（子帧）
            Projectile.extraUpdates = UmbralNadirBalance.VoidLanceExtraUpdates; // 11 次/帧
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            lastFrameCenter = Projectile.Center - Projectile.velocity * (Projectile.extraUpdates + 1);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DeadSunRicochet") with { Volume = 0.85f, Pitch = -0.1f }, Projectile.Center);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldExplosion") with { Volume = 0.55f, Pitch = 0.15f }, Projectile.Center);
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();          // 笔直匀速，不加速不追踪
            Lighting.AddLight(Projectile.Center, 0.4f, 1.1f, 0.55f);

            if (Projectile.FinalExtraUpdate())
            {
                frameTimer++;
                SpawnHelixTrail();
            }
        }

        /// <summary>黑→绿双股虚空螺旋（来自深渊），沿真实帧间路径补点以在极高速下保持连续。</summary>
        private void SpawnHelixTrail()
        {
            Vector2 fwd = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -fwd;
            Vector2 perp = fwd.RotatedBy(MathHelper.PiOver2);
            Vector2 segment = Projectile.Center - lastFrameCenter;
            int samples = Math.Clamp((int)MathF.Ceiling(segment.Length() / 10f), 4, 16);

            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 p = Vector2.Lerp(lastFrameCenter, Projectile.Center, t);
                float phase = (frameTimer + t) * 0.7f + Projectile.identity;
                float amp = 26f;

                // 黑股：VoidDust
                Dust b = Dust.NewDustPerfect(p + perp * MathF.Sin(phase) * amp, ModContent.DustType<VoidDust>(),
                    back * 0.4f, 0, Color.Black, Main.rand.NextFloat(1.1f, 1.7f));
                b.noGravity = true;
                // 绿股：VoidDustInverted，反相对摆
                Dust g = Dust.NewDustPerfect(p + perp * MathF.Sin(phase + MathHelper.Pi) * amp, ModContent.DustType<VoidDustInverted>(),
                    back * 0.4f, 0, Green, Main.rand.NextFloat(0.9f, 1.4f));
                g.noGravity = true;
                g.color = Green;

                // 黑色弹芯球
                if ((i & 1) == 0)
                    GeneralParticleHandler.SpawnParticle(new GenericBloom(p, back * 0.35f, Color.Black,
                        Main.rand.NextFloat(0.26f, 0.44f), Main.rand.Next(9, 14), true, false));
            }

            // 巨大黑核 + 向后抽离的黑色能量痕
            GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center, Vector2.Zero, Color.Black,
                Main.rand.NextFloat(0.5f, 0.7f), Main.rand.Next(8, 12), true, false));
            if (Main.rand.NextBool(2))
                GeneralParticleHandler.SpawnParticle(new AltLineParticle(Projectile.Center, back * Main.rand.NextFloat(1.5f, 4f),
                    false, Main.rand.Next(10, 16), Main.rand.NextFloat(0.7f, 1.1f), Color.Black));

            lastFrameCenter = Projectile.Center;
        }

        // 拉长判定，读作贯穿光矛
        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = Projectile.Center - dir * 96f;
            Vector2 tl = Vector2.Min(Projectile.Center, back) - new Vector2(20f);
            Vector2 br = Vector2.Max(Projectile.Center, back) + new Vector2(20f);
            hitbox = new Rectangle((int)tl.X, (int)tl.Y, (int)(br.X - tl.X), (int)(br.Y - tl.Y));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 180);
            UmbralCorrosionGlobalNPC.AddStacks(target, 2);

            // 每次贯穿都炸一记中等黑洞爆裂（含范围伤害、拉扯与叠层）
            if (Projectile.owner == Main.myPlayer)
            {
                int explosionDamage = Math.Max(1, (int)(Projectile.damage * UmbralNadirBalance.VoidLanceHitExplosionMult));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<UmbralNadirImpactExplosion>(), explosionDamage, Projectile.knockBack, Projectile.owner, 1f);
            }
        }

        // ===== 黑→荧绿宏伟光束 =====

        private float BeamWidth(float completionRatio, Vector2 _)
            => MathHelper.Lerp(64f, 6f, completionRatio) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);

        private Color BeamColor(float completionRatio, Vector2 _)
            => Color.Lerp(UmbralNadirPalette.MeldGreenBright, Color.Lerp(Green, Color.Black, completionRatio * 1.2f), completionRatio) * (1f - completionRatio * 0.55f);

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            Vector2 offset = Projectile.Size * 0.5f;
            PrimitiveRenderer.RenderTrail(Projectile.oldPos,
                new PrimitiveSettings(BeamWidth, BeamColor, (_, _) => offset, shader: GameShaders.Misc["CalamityMod:TrailStreak"]), 90);

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D soft = ModContent.Request<Texture2D>("CalamityMod/Particles/SmallBloom").Value;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            // 巨大纯黑核（透明底 SmallBloom）+ 亮绿事件视界弹头
            Main.EntitySpriteDraw(soft, pos, null, Color.Black, Projectile.rotation, soft.Size() * 0.5f, 0.34f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, pos, null, Green with { A = 0 }, 0f, bloom.Size() * 0.5f, 0.55f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, pos, null, UmbralNadirPalette.MeldGreenBright with { A = 0 }, 0f, bloom.Size() * 0.5f, 0.28f, SpriteEffects.None, 0);
            return false;
        }
    }
}
