using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.Shared;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.RightClick
{
    /// <summary>
    /// 冥蚀天底右键投矛（三连投掷中的一发）。
    /// 使用较小的物品贴图（手柄更短、更适合投掷），高更新次数、低速、直线飞行、不追踪。
    /// 全程拖出冥思系纯黑吸光 + 荧光绿加色的粒子云；命中/落地即引爆一整套冥思黑绿爆裂。
    /// 前两发（ai[0]=0/1）播撒追踪虚空核作连锁引信，第三发（ai[0]=2）引爆更大的"黑日坍缩"终结。
    /// </summary>
    public class UmbralNadirJavelin : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        // 右键用物品贴图（更短小）
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Upgrade/Nadir/UmbralNadir";

        private static readonly Color MeldGreen = Color.LightGreen;
        private static readonly Color ShaderColorOne = Color.Black;
        private static readonly Color ShaderColorTwo = new Color(40, 110, 55);
        private static readonly Color ShaderEndColor = Color.LightGreen;

        /// <summary>三连投掷中的序号（0/1=引信，2=终结）。</summary>
        public ref float ShotIndex => ref Projectile.ai[0];
        private bool IsFinisher => ShotIndex >= 1.5f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;           // 命中一次即引爆
            Projectile.timeLeft = 260;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 3;         // 高更新次数 → 直线飞行更顺滑、拖尾更密
            Projectile.scale = 0.85f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // 初始就把朝向摆正（物品贴图矛头在右上，故 +PiOver4），避免第一帧贴图朝向错误
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        }

        public override void AI()
        {
            // 直线飞行、不追踪、匀速；朝向恒定跟随速度
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, MeldGreen.ToVector3() * 0.35f);

            // 冥思拖尾（每真实帧一次，避免 extraUpdates 刷爆）——纯黑吸光 + 荧光绿加色
            if (Projectile.FinalExtraUpdate())
            {
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);

                GeneralParticleHandler.SpawnParticle(new GenericBloom(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), back * Main.rand.NextFloat(0.2f, 1.1f),
                    Color.Black, Main.rand.NextFloat(0.22f, 0.4f), Main.rand.Next(9, 13), true, false));

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center, back * Main.rand.NextFloat(0.3f, 0.9f), "CalamityMod/Particles/GlowSpark2",
                    false, 16, Main.rand.NextFloat(0.04f, 0.06f), Color.Black, new Vector2(0.6f, 1.3f), false));
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center, back * Main.rand.NextFloat(0.3f, 0.9f), "CalamityMod/Particles/GlowSpark",
                    false, 16, Main.rand.NextFloat(0.02f, 0.035f), MeldGreen, new Vector2(0.6f, 1.3f), true, false),
                    false, GeneralDrawLayer.AfterEverything);

                Dust vd = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>());
                vd.scale = Main.rand.NextFloat(0.7f, 1.2f);
                vd.velocity = back.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.3f, 1.6f);
                vd.noGravity = true;
                vd.color = MeldGreen;

                if (Main.rand.NextBool(3))
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                        Projectile.Center, back * Main.rand.NextFloat(0.4f, 1.2f), Color.Black, 20,
                        Main.rand.NextFloat(0.3f, 0.55f), 0.4f, Main.rand.NextFloat(-0.05f, 0.05f), false));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => target.AddBuff(ModContent.BuffType<Voidfrost>(), IsFinisher ? 240 : 150);

        // 落地/超时/命中(penetrate 归零) 统一走 OnKill 引爆，避免重复
        public override void OnKill(int timeLeft) => Explode();

        private void Explode()
        {
            Vector2 center = Projectile.Center;
            bool finisher = IsFinisher;
            float sizeBonus = finisher ? 1.8f : 1f;

            // ===== 音效（冥思 Meld 系）=====
            SoundEngine.PlaySound(new SoundStyle(finisher ? "CalamityMod/Sounds/Item/MeldExplosion" : "CalamityMod/Sounds/Item/MeldBurn")
                with { Volume = finisher ? 0.9f : 0.6f, Pitch = Main.rand.NextFloat(-0.1f, 0.1f) }, center);

            // ===== 荧光绿加色外爆（顶层）=====
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, MeldGreen with { A = 0 },
                "CalamityMod/Particles/LargeBloom", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.5f * sizeBonus, 12),
                false, GeneralDrawLayer.AfterEverything);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, Color.White with { A = 0 },
                "CalamityMod/Particles/LargeBloom", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.4f * sizeBonus, 12),
                false, GeneralDrawLayer.AfterEverything);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, MeldGreen,
                "CalamityMod/Particles/BloomRing", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 1.3f * sizeBonus, 18),
                false, GeneralDrawLayer.AfterEverything);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, MeldGreen,
                "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.2f * sizeBonus, 28),
                false, GeneralDrawLayer.AfterEverything);

            // ===== 纯黑坍缩核心（AlphaBlend，透明底 SmallBloom）=====
            int blackLayers = finisher ? 2 : 1;
            for (int i = 0; i < blackLayers; i++)
                GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, Color.Black,
                    "CalamityMod/Particles/SmallBloom", Vector2.One, Main.rand.NextFloat(-10f, 10f),
                    (0.9f + i * 0.5f) * sizeBonus, 0f, finisher ? 70 : 40, false));

            // 终结段追加冥思招牌"外绿内黑"双重坍缩核
            if (finisher)
            {
                GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, MeldGreen with { A = 0 },
                    Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.2f, 0.9f, 28), false, GeneralDrawLayer.AfterEverything);
                GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, Color.Black,
                    Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.12f, 0.55f, 26, false));
            }

            // ===== 双色火花爆裂（黑 GlowSpark2 + 绿 GlowSpark）=====
            int sparks = (int)(14 * sizeBonus);
            for (int i = 0; i < sparks; i++)
            {
                Vector2 v = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f * sizeBonus);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(center, v, "CalamityMod/Particles/GlowSpark2",
                    false, Main.rand.Next(14, 20), Main.rand.NextFloat(0.05f, 0.08f), Color.Black, new Vector2(0.6f, 1.3f), false));
                if (Main.rand.NextBool())
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(center, v * 0.8f, "CalamityMod/Particles/GlowSpark",
                        false, Main.rand.Next(12, 18), Main.rand.NextFloat(0.025f, 0.045f), MeldGreen, new Vector2(0.6f, 1.3f), true, false),
                        false, GeneralDrawLayer.AfterEverything);
            }

            // ===== 反向虚空尘 + 绿色烟花尘 =====
            for (int i = 0; i < (int)(16 * sizeBonus); i++)
            {
                Dust vd = Dust.NewDustPerfect(center, ModContent.DustType<VoidDustInverted>());
                vd.noGravity = true;
                vd.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 9f) * sizeBonus;
                vd.scale = Main.rand.NextFloat(1.3f, 2.1f) * sizeBonus;
                vd.color = MeldGreen;
            }
            for (int i = 0; i < (int)(12 * sizeBonus); i++)
            {
                Dust fd = Dust.NewDustPerfect(center, DustID.FireworksRGB);
                fd.noGravity = false;
                fd.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 10f) * sizeBonus;
                fd.scale = Main.rand.NextFloat(0.8f, 1.2f);
                fd.color = MeldGreen;
            }

            // ===== 黑烟团 =====
            for (int i = 0; i < (int)(4 * sizeBonus); i++)
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(center,
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 4f), Color.Black, Main.rand.Next(28, 44),
                    Main.rand.NextFloat(0.7f, 1.2f) * sizeBonus, 0.7f, Main.rand.NextFloat(-0.05f, 0.05f), true));

            ApplyScreenShake(center, finisher ? 6f : 2.6f);

            // ===== 弹幕载荷 =====
            if (Projectile.owner != Main.myPlayer)
                return;

            if (!finisher)
            {
                // 引信：播撒 2~3 枚追踪虚空核，连锁到附近其它敌人
                int cores = Main.rand.Next(2, 4);
                for (int i = 0; i < cores; i++)
                    SpawnVoidEssence(Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 8f));
            }
            else
            {
                // 终结：一波四散虚空核 + 深渊触须，把连锁彻底铺开
                int cores = Main.rand.Next(4, 7);
                for (int i = 0; i < cores; i++)
                    SpawnVoidEssence(Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 10f));
                int arms = Main.rand.Next(3, 5);
                for (int i = 0; i < arms; i++)
                    SpawnTentacle(Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 7f));
            }
        }

        private void SpawnVoidEssence(Vector2 velocity)
        {
            if (Main.player[Projectile.owner].ownedProjectileCounts[ModContent.ProjectileType<UmbralNadirVoidEssence>()] >= UmbralNadirBalance.MaxActiveVoidEssence)
                return;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
                ModContent.ProjectileType<UmbralNadirVoidEssence>(),
                Math.Max(1, (int)(Projectile.damage * 0.6f)), Projectile.knockBack * 0.5f, Projectile.owner, 0f, 0f);
        }

        private void SpawnTentacle(Vector2 velocity)
        {
            float curl0 = Main.rand.NextFloat(0.01f, 0.08f) * (Main.rand.NextBool() ? -1f : 1f);
            float curl1 = Main.rand.NextFloat(0.01f, 0.08f) * (Main.rand.NextBool() ? -1f : 1f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
                ModContent.ProjectileType<VoidTentacle>(),
                Math.Max(1, (int)(Projectile.damage * 0.6f)), Projectile.knockBack, Projectile.owner, curl0, curl1);
        }

        private static void ApplyScreenShake(Vector2 source, float power)
        {
            float distanceFactor = Utils.GetLerpValue(1400f, 0f, Vector2.Distance(source, Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower =
                Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        // ===== 冥思黑→绿着色器拖尾 =====

        private float PrimitiveWidthFunction(float completionRatio, Vector2 vertexPos)
        {
            float arrowheadCutoff = 0.36f;
            float width = 52f;
            if (completionRatio <= arrowheadCutoff)
                width = MathHelper.Lerp(0.03f, width, Utils.GetLerpValue(0f, arrowheadCutoff, completionRatio, true));
            return width;
        }

        private Color PrimitiveColorFunction(float completionRatio, Vector2 vertexPos)
        {
            float endFadeRatio = 0.41f;
            float endFadeTerm = Utils.GetLerpValue(0f, endFadeRatio * 0.5f, completionRatio, true) * 3.2f;
            float cosArgument = completionRatio * 2.7f - Main.GlobalTimeWrappedHourly * 5.3f + endFadeTerm;
            float startingInterpolant = (float)Math.Cos(cosArgument) * 0.5f + 0.5f;

            Color startingColor = Color.Lerp(ShaderColorOne, ShaderColorTwo, startingInterpolant * 0.6f);
            return Color.Lerp(startingColor, ShaderEndColor, MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(0f, endFadeRatio, completionRatio, true)));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            Vector2 overallOffset = Projectile.Size * 0.5f + Projectile.velocity * 1.4f;
            PrimitiveRenderer.RenderTrail(Projectile.oldPos,
                new PrimitiveSettings(PrimitiveWidthFunction, PrimitiveColorFunction,
                    (completionRatio, vertexPos) => overallOffset, shader: GameShaders.Misc["CalamityMod:TrailStreak"]), 90);

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            // 纯黑吸光剪影垫底，再画本体，营造冥思负光感
            Main.EntitySpriteDraw(texture, drawPos, null, Color.Black * 0.5f, Projectile.rotation, origin, Projectile.scale * 1.08f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
