using System;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.Shared
{
    /// <summary>
    /// 深渊之口 —— 冥蚀天底左键命中时撕开的一道"天底裂隙"。
    /// 无贴图纯代码绘制（冥思系高级手法）：一圈黑色花瓣拼成吞光的巨口，
    /// 内嵌无底纯黑核心与荧光绿事件视界，一只绿色"深渊之眼"在中央凝视。
    /// 生命周期分五幕：张开 → 凝视(伸出触须/吐出碎渊) → 坍缩 → 内爆 → 收束。
    /// 全程在默认预乘批次内绘制：黑花瓣用透明底 WaterFlavored(AlphaBlend 吸光)，
    /// 绿核用 with{A=0} 的加色技巧，绝不画出黑底方块。
    /// </summary>
    public class UmbralNadirAbyssRift : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color MeldGreen = Color.LightGreen;

        private const int Lifetime = 96;
        private const int OpenEnd = 20;      // 张开结束
        private const int GazeEnd = 66;      // 凝视结束、开始坍缩

        /// <summary>体型/自旋方向的随机种子（生成时写入 ai[0]）。</summary>
        public ref float Seed => ref Projectile.ai[0];

        private int Age => Lifetime - Projectile.timeLeft;
        private float spin;
        private float visualScale;

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 104;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        // 只在真正成形的阶段造成伤害
        public override bool? CanDamage() => Age >= 14 && Age <= GazeEnd + 22;

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            // 判定随视觉大小缩放
            float s = MathHelper.Clamp(visualScale, 0.2f, 1.4f);
            Vector2 center = hitbox.Center.ToVector2();
            hitbox.Width = (int)(Projectile.width * s);
            hitbox.Height = (int)(Projectile.height * s);
            hitbox.Location = (center - hitbox.Size() * 0.5f).ToPoint();
        }

        public override void AI()
        {
            if (Seed == 0f)
                Seed = Main.rand.NextBool() ? 1f : -1f;

            int age = Age;

            // 相位缩放：张开(缓出) → 满(带脉动) → 坍缩(收进奇点)
            float openT = Utils.GetLerpValue(0f, OpenEnd, age, true);
            float collapseT = Utils.GetLerpValue(GazeEnd, Lifetime, age, true);
            float pulse = 1f + 0.06f * (float)Math.Sin(age * 0.35f);
            visualScale = MathHelper.SmoothStep(0f, 1.12f, openT) * (1f - MathHelper.SmoothStep(0f, 1f, collapseT)) * pulse;

            // 自旋：坍缩期加速，营造被吸入的眩晕感
            spin += Seed * MathHelper.Lerp(0.05f, 0.22f, collapseT);
            Projectile.rotation = spin;

            Lighting.AddLight(Projectile.Center, 0.28f, 0.8f, 0.4f);

            // 张开瞬间：震屏 + 撕裂音 + 一圈外扩虚空尘
            if (age == 3)
            {
                SoundEngine.PlaySound(SoundID.Item104 with { Volume = 0.55f, Pitch = -0.35f }, Projectile.Center);
                ApplyScreenShake(Projectile.Center, 3.2f);
                for (int i = 0; i < 26; i++)
                {
                    Vector2 v = (MathHelper.TwoPi * i / 26f).ToRotationVector2() * Main.rand.NextFloat(3f, 7f);
                    Dust du = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>(), v, 0, MeldGreen, Main.rand.NextFloat(1.1f, 1.8f));
                    du.noGravity = true;
                    du.color = MeldGreen;
                }
            }

            // 凝视幕：吐出触须（伸自深渊的触手）与内旋碎渊尘，参数全随机化，绝不重复
            if (age > OpenEnd && age < GazeEnd)
            {
                if (age % 11 == 0 && Projectile.owner == Main.myPlayer)
                {
                    int arms = Main.rand.Next(1, 3);
                    for (int i = 0; i < arms; i++)
                        SpawnTentacle(Main.rand.NextVector2Unit() * Main.rand.NextFloat(3.5f, 6.5f));
                }

                // 向心的碎渊尘（被无底之口吞入）
                if (Main.rand.NextBool(2))
                {
                    float r = Main.rand.NextFloat(70f, 130f) * Math.Max(0.4f, visualScale);
                    Vector2 edge = Projectile.Center + Main.rand.NextVector2Unit() * r;
                    Vector2 inward = (Projectile.Center - edge).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 5f);
                    Dust du = Dust.NewDustPerfect(edge, ModContent.DustType<VoidDustInverted>(), inward, 0, MeldGreen, Main.rand.NextFloat(0.9f, 1.4f));
                    du.noGravity = true;
                    du.color = MeldGreen;
                }
            }
        }

        private void SpawnTentacle(Vector2 velocity)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            float curl0 = Main.rand.NextFloat(0.01f, 0.08f) * (Main.rand.NextBool() ? -1f : 1f);
            float curl1 = Main.rand.NextFloat(0.01f, 0.08f) * (Main.rand.NextBool() ? -1f : 1f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
                ModContent.ProjectileType<VoidTentacle>(),
                Math.Max(1, (int)(Projectile.damage * 0.85f)), Projectile.knockBack, Projectile.owner, curl0, curl1);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => target.AddBuff(ModContent.BuffType<Voidfrost>(), 120);

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item104 with { Volume = 0.5f, Pitch = 0.25f }, Projectile.Center);
            ApplyScreenShake(Projectile.Center, 3.6f);

            // 内爆尘暴
            for (int i = 0; i < 24; i++)
            {
                Vector2 v = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 7f);
                Dust du = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>(), v, 0, MeldGreen, Main.rand.NextFloat(1.0f, 1.7f));
                du.noGravity = true;
                du.color = MeldGreen;
            }

            // 收束时最后一波深渊触须
            if (Projectile.owner == Main.myPlayer)
            {
                int arms = Main.rand.Next(3, 5);
                for (int i = 0; i < arms; i++)
                    SpawnTentacle((MathHelper.TwoPi * i / arms + Main.rand.NextFloat(-0.3f, 0.3f)).ToRotationVector2() * Main.rand.NextFloat(4f, 7f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (visualScale <= 0.01f)
                return false;

            Asset<Texture2D> water = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored");
            Asset<Texture2D> bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Asset<Texture2D> softBloom = ModContent.Request<Texture2D>("CalamityMod/Particles/SmallBloom");

            Vector2 pos = Projectile.Center - Main.screenPosition;
            float s = visualScale;
            float opacity = MathHelper.Clamp(s / 1.1f, 0f, 1f);

            // 1) 背后的荧光绿事件视界光环（with{A=0} = 加色，黑底不出框）
            for (int i = 0; i < 2; i++)
            {
                float ring = (0.5f + i * 0.32f) * s;
                Main.EntitySpriteDraw(bloom.Value, pos, null, MeldGreen with { A = 0 } * (0.9f - i * 0.3f),
                    spin * 0.5f, bloom.Value.Size() * 0.5f, ring, SpriteEffects.None, 0);
            }

            // 2) 一圈黑色花瓣拼成吞光巨口（透明底 WaterFlavored，AlphaBlend 吸光）
            int petals = 8;
            float radius = MathHelper.Lerp(10f, 34f, opacity) * s;
            for (int i = 0; i < petals; i++)
            {
                float ang = MathHelper.TwoPi * i / petals + spin;
                Vector2 offset = ang.ToRotationVector2() * radius;
                Vector2 outward = offset.SafeNormalize(Vector2.UnitX);
                Main.EntitySpriteDraw(water.Value, pos + offset, null, Color.Black * (0.85f * opacity),
                    outward.ToRotation() + MathHelper.ToRadians(-90f), water.Value.Size() * 0.5f,
                    new Vector2(0.34f, 1.05f) * s, SpriteEffects.None, 0);
            }

            // 3) 无底纯黑核心（透明底 SmallBloom，AlphaBlend，绝不出框）
            Main.EntitySpriteDraw(softBloom.Value, pos, null, Color.Black * opacity,
                spin, softBloom.Value.Size() * 0.5f, 0.16f * s, SpriteEffects.None, 0);

            // 4) 中央凝视的深渊之眼（加色亮绿点）
            float gaze = (0.10f + 0.02f * (float)Math.Sin(Age * 0.5f)) * s;
            Main.EntitySpriteDraw(bloom.Value, pos, null, new Color(200, 255, 210) with { A = 0 } * opacity,
                0f, bloom.Value.Size() * 0.5f, gaze, SpriteEffects.None, 0);

            return false;
        }

        private static void ApplyScreenShake(Vector2 source, float power)
        {
            float distanceFactor = Utils.GetLerpValue(1400f, 0f, Vector2.Distance(source, Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower =
                Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }
    }
}
