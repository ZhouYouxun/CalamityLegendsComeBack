using System;
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

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.RightClick
{
    /// <summary>
    /// 冥蚀天底右键投矛（三连之一）。物品贴图（更短小）、高更新次数、低速、直线飞行、不追踪。
    /// 前两发（ai[0]=0/1）飞行时从轨迹后方唤出少量暗影魂针追敌；第三发（ai[0]=2）命中即刻终爆。
    /// 撞墙 / 超时只有无伤害的小型熄灭特效，绝不触发终爆或召唤。
    /// </summary>
    public class UmbralNadirJavelin : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Upgrade/Nadir/UmbralNadir";

        private static readonly Color MeldGreen = Color.LightGreen;
        private static readonly Color ShaderColorOne = Color.Black;
        private static readonly Color ShaderColorTwo = new Color(40, 110, 55);

        /// <summary>三连序号（0/1 = 引信，2 = 终结）。</summary>
        public ref float ShotIndex => ref Projectile.ai[0];
        private bool IsFinisher => ShotIndex >= 1.5f;

        private ref float SoulCount => ref Projectile.localAI[0];
        private ref float FrameTimer => ref Projectile.localAI[1];
        private bool hitEnemy;

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
            Projectile.penetrate = 1;
            Projectile.timeLeft = 260;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = UmbralNadirBalance.JavelinExtraUpdates; // 7 次/帧 → 约旧版 2.4 倍飞速
            Projectile.scale = 0.85f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
            => Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

        public override void AI()
        {
            // 直线飞行、匀速、不追踪；朝向恒随速度（物品贴图矛头右上 → +PiOver4）
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, MeldGreen.ToVector3() * 0.32f);

            if (Projectile.FinalExtraUpdate())
            {
                FrameTimer++;
                SpawnFlightTrail();

                // 前两发：沿飞行轨迹后方唤出暗影魂针
                if (!IsFinisher && Projectile.owner == Main.myPlayer)
                    TrySpawnShadowSoul();
            }
        }

        private void SpawnFlightTrail()
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
            if (Main.rand.NextBool(3))
            {
                Dust vd = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>());
                vd.scale = Main.rand.NextFloat(0.7f, 1.2f);
                vd.velocity = back.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.3f, 1.6f);
                vd.noGravity = true;
                vd.color = MeldGreen;
            }
        }

        private void TrySpawnShadowSoul()
        {
            if (FrameTimer < UmbralNadirBalance.ShadowSoulSpawnStart)
                return;
            if ((int)(FrameTimer - UmbralNadirBalance.ShadowSoulSpawnStart) % UmbralNadirBalance.ShadowSoulSpawnInterval != 0)
                return;
            if (SoulCount >= UmbralNadirBalance.ShadowSoulsPerJavelin)
                return;
            if (Main.player[Projectile.owner].ownedProjectileCounts[ModContent.ProjectileType<UmbralNadirShadowSoul>()] >= UmbralNadirBalance.MaxShadowSoulsPerPlayer)
                return;

            Vector2 fwd = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 perp = fwd.RotatedBy(MathHelper.PiOver2);
            // 出生于投矛后方 140~280px，沿飞行路线附近，加垂直方向 ±80px 偏移
            Vector2 birth = Projectile.Center - fwd * Main.rand.NextFloat(140f, 280f) + perp * Main.rand.NextFloat(-80f, 80f);
            Vector2 vel = fwd.RotatedByRandom(0.5f) * Main.rand.NextFloat(3.5f, 5f);
            int soulDamage = Math.Max(1, (int)(Projectile.damage * UmbralNadirBalance.ShadowSoulDamageMult));

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), birth, vel,
                ModContent.ProjectileType<UmbralNadirShadowSoul>(), soulDamage, Projectile.knockBack * 0.3f, Projectile.owner);

            // 出生处一小簇黑雾，读作"被矛划开的阴影"
            for (int i = 0; i < 3; i++)
            {
                Dust vd = Dust.NewDustPerfect(birth, ModContent.DustType<VoidDustInverted>());
                vd.noGravity = true;
                vd.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.5f, 1.8f);
                vd.scale = Main.rand.NextFloat(0.7f, 1.1f);
                vd.color = MeldGreen;
            }
            SoulCount++;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitEnemy = true;
            target.AddBuff(ModContent.BuffType<Voidfrost>(), IsFinisher ? 240 : 150);

            // 第三发命中：立刻在目标中心生成终爆（前两发不产生爆炸，其价值在沿路魂针）
            if (IsFinisher && Projectile.owner == Main.myPlayer)
            {
                int finalDamage = Math.Max(1, (int)(Projectile.damage * UmbralNadirBalance.FinalExplosionDamageMult));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<UmbralNadirFinalExplosion>(), finalDamage, Projectile.knockBack, Projectile.owner);
            }
        }

        // 撞墙 / 超时：只熄灭，不伤害、不召唤、不终爆
        public override void OnKill(int timeLeft)
        {
            if (hitEnemy)
                return;
            Fizzle();
        }

        private void Fizzle()
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldBurn") with { Volume = 0.35f, Pitch = 0.3f }, Projectile.Center);
            // 约 45% 尺寸的小型无伤害熄灭
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Black,
                "CalamityMod/Particles/SmallBloom", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.1f, 0.28f, 14, false));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, MeldGreen with { A = 0 },
                "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.1f, 0.4f, 12, true),
                false, GeneralDrawLayer.AfterEverything);
            for (int i = 0; i < 6; i++)
                GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center,
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4f), Color.Black,
                    Main.rand.NextFloat(0.14f, 0.26f), Main.rand.Next(8, 12), true, false));
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
            return Color.Lerp(startingColor, MeldGreen, MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(0f, endFadeRatio, completionRatio, true)));
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
            Main.EntitySpriteDraw(texture, drawPos, null, Color.Black * 0.5f, Projectile.rotation, origin, Projectile.scale * 1.08f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
