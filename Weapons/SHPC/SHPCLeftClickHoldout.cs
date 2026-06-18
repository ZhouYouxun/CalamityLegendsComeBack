using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    /// <summary>
    /// 左键手持弹幕。出现时播放后坐力动画和枪口星芒，动画结束后自动消失。
    /// ai[0] = effectID，ai[1] = burstCount（连射材料的脉冲次数）
    /// </summary>
    public class SHPCLeftClickHoldout : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int EffectID => (int)Projectile.ai[0];
        private int BurstCount => Math.Max(1, (int)Projectile.ai[1]);

        private int timer;
        private int lastBurstSegment = -1;
        private float recoilOffset;
        private float gunRotation;
        private int spriteDir;
        private int energyCoreGlowTime;
        private int energyCoreGlowBurst;

        // 完全对齐右键激光：polar angle -1.04f，distance 4f
        private Vector2 EnergyCorePosition
        {
            get
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX.RotatedBy(gunRotation));
                int facing = spriteDir == 0 ? 1 : spriteDir;
                return Projectile.Center + direction.RotatedBy(-1.04f * facing) * 4f;
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            if (!owner.active || owner.dead || owner.HeldItem.ModItem is not NewLegendSHPC)
            {
                Projectile.Kill();
                return;
            }

            if (owner.itemAnimation <= 0)
            {
                Projectile.Kill();
                return;
            }

            if (HasActiveRightClickHoldout(owner))
            {
                Projectile.Kill();
                return;
            }

            UpdateTransform(owner);
            UpdateRecoil();
            TriggerBurstEffects(owner);

            if (energyCoreGlowBurst > 0)
                energyCoreGlowBurst--;
            energyCoreGlowTime++;
            timer++;
            Projectile.timeLeft = 2;
        }

        private void UpdateTransform(Player owner)
        {
            Vector2 aimWorld = Projectile.owner == Main.myPlayer
                ? Main.MouseWorld
                : owner.Calamity().mouseWorld;

            Vector2 armPos = owner.RotatedRelativePoint(owner.MountedCenter, true);
            Vector2 aimDir = (aimWorld - armPos).SafeNormalize(Vector2.UnitX * owner.direction);

            gunRotation = aimDir.ToRotation();
            spriteDir = Math.Sign(aimDir.X);
            if (spriteDir == 0)
                spriteDir = owner.direction;

            owner.ChangeDir(spriteDir);
            owner.heldProj = Projectile.whoAmI;

            Projectile.Center = armPos + aimDir * 35f;
            Projectile.velocity = aimDir;
        }

        private void UpdateRecoil()
        {
            int burst = BurstCount;
            int totalDur = GetCurrentAnimationDuration();
            int segLen = Math.Max(1, totalDur / burst);
            int segTimer = timer % segLen;

            int kickFrames = Math.Min(segLen, 8);
            int returnFrames = Math.Min(segLen, 16);
            float maxRecoil = MathHelper.Clamp(14f / burst, 1f, 14f);

            if (segTimer < kickFrames)
                recoilOffset = MathF.Sin((float)segTimer / kickFrames * MathHelper.PiOver2) * maxRecoil;
            else if (segTimer < returnFrames)
                recoilOffset = MathF.Cos((float)(segTimer - kickFrames) / (returnFrames - kickFrames) * MathHelper.PiOver2) * maxRecoil;
            else
                recoilOffset = 0f;
        }

        private void TriggerBurstEffects(Player owner)
        {
            int burst = BurstCount;
            int totalDur = GetCurrentAnimationDuration();
            int segLen = Math.Max(1, totalDur / burst);
            int currentSegment = timer / segLen;

            if (currentSegment <= lastBurstSegment || currentSegment >= burst)
                return;

            lastBurstSegment = currentSegment;
            // 连射越多，每次爆闪强度越小（max 12 → 连射5次降到约2）
            energyCoreGlowBurst = Math.Max(2, 12 / BurstCount);

            if (Projectile.owner != Main.myPlayer || Main.dedServ)
                return;

            SpawnBurstMuzzleParticles(owner);
        }

        private int GetCurrentAnimationDuration()
        {
            Player owner = Main.player[Projectile.owner];
            return owner.HeldItem?.useAnimation ?? 60;
        }

        private Vector2 GetGunTipPos() =>
            Projectile.Center + gunRotation.ToRotationVector2() * 56f;

        private void SpawnBurstMuzzleParticles(Player owner)
        {
            if (Main.dedServ)
                return;

            RulesOfEffect effect = EffectRegistry.GetEffectByID(EffectID);
            Color themeColor = GetThemeColor(effect);

            Vector2 muzzlePos = GetGunTipPos();
            Vector2 fireDir = gunRotation.ToRotationVector2();

            for (int i = 0; i < 3; i++)
            {
                Vector2 vel = fireDir.RotatedByRandom(0.15f) * Main.rand.NextFloat(5f, 12f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    muzzlePos + fireDir * Main.rand.NextFloat(0f, 2f),
                    vel,
                    false,
                    10,
                    Main.rand.NextFloat(0.35f, 0.65f),
                    Color.Lerp(themeColor, Color.White, Main.rand.NextFloat(0.35f, 0.7f)),
                    true,
                    true));
            }

            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                muzzlePos,
                fireDir * Main.rand.NextFloat(1.5f, 3f),
                Main.rand.NextFloat(0.25f, 0.4f),
                Color.Lerp(themeColor, Color.White, 0.4f),
                Main.rand.Next(8, 14)));
        }

        private static Color GetThemeColor(RulesOfEffect effect)
        {
            if (effect == null)
                return new Color(205, 205, 205);
            Color c = effect.ThemeColor;
            return c == Color.Transparent || c == default ? new Color(205, 205, 205) : c;
        }

        private static bool HasActiveRightClickHoldout(Player owner)
        {
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (!proj.active || proj.owner != owner.whoAmI)
                    continue;

                if (proj.type == ModContent.ProjectileType<RightClick.SHPCRight_HoulOut>() ||
                    proj.type == ModContent.ProjectileType<RightClickMortar.RightClickMortar_HoldOut>() ||
                    proj.type == ModContent.ProjectileType<RightClickTurret.MilitaryCaller_HoldOut>() ||
                    proj.type == ModContent.ProjectileType<EXSkill.NL_SHPC_EXWeapon>())
                    return true;
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            if (owner.HeldItem.ModItem is not NewLegendSHPC)
                return false;

            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Magic/SHPC").Value;
            Vector2 origin = tex.Size() * 0.5f;

            RulesOfEffect effect = EffectRegistry.GetEffectByID(EffectID);
            Color themeColor = GetThemeColor(effect);

            Vector2 aimDir = gunRotation.ToRotationVector2();
            Vector2 drawCenter = Projectile.Center - aimDir * recoilOffset - Main.screenPosition;

            SpriteEffects flip = spriteDir == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            if (owner.gravDir == -1f)
                flip ^= SpriteEffects.FlipVertically;

            // 对齐迫击炮：idlePulse 0.45→1.0 缓慢震荡，burstPulse 开火时 0→1
            float idlePulse = 0.45f + 0.55f * (MathF.Sin(timer * 0.12f) * 0.5f + 0.5f);
            float burstPulse = MathHelper.Clamp(energyCoreGlowBurst / 12f, 0f, 1f);

            // 迫击炮同款：外圈颜色 Lerp(主题色→白, 0.62f)，内圈 0.8f
            Color outlineColor = (Color.Lerp(themeColor, Color.White, 0.62f) with { A = 0 })
                * (0.72f + idlePulse * 0.38f + burstPulse * 0.85f);
            Color innerOutlineColor = (Color.Lerp(themeColor, Color.White, 0.80f) with { A = 0 })
                * (0.5f + idlePulse * 0.26f + burstPulse * 0.52f);
            float outlineDistance = 2.2f + idlePulse * 2.8f + burstPulse * 4.2f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            // 开火瞬间：迫击炮同款彩虹描边 (DrawStarmadaRainbowOutline)
            if (burstPulse > 0f)
            {
                HoldoutOutlineHelper.DrawStarmadaRainbowOutline(
                    tex,
                    drawCenter,
                    gunRotation,
                    origin,
                    Vector2.One * (1f + burstPulse * 0.05f),
                    flip,
                    4.6f + burstPulse * 9.4f,
                    burstPulse * 1.35f,
                    Main.GlobalTimeWrappedHourly + Projectile.identity * 0.19f,
                    28,
                    manageBlendState: false);
            }

            // 迫击炮同款：14 方向外圈
            for (int i = 0; i < 14; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 14f).ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(tex, drawCenter + offset, null, outlineColor, gunRotation, origin, 1f, flip, 0);
            }

            // 迫击炮同款：8 方向内圈
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * (1.1f + burstPulse * 1.5f);
                Main.EntitySpriteDraw(tex, drawCenter + offset, null, innerOutlineColor, gunRotation, origin, 1.02f + burstPulse * 0.04f, flip, 0);
            }

            // 枪体贴图
            Main.EntitySpriteDraw(tex, drawCenter, null, Color.White, gunRotation, origin, 1f, flip, 0f);

            // 星芒在枪体之后绘制（否则被枪覆盖看不见），并传入后坐力偏移使位置同步
            DrawEnergyCoreGlow(themeColor, flip, -aimDir * recoilOffset);

            return false;
        }

        // 完全对齐右键激光 DrawApoctosisCoreGlow 的绘制逻辑
        private void DrawEnergyCoreGlow(Color themeColor, SpriteEffects flip, Vector2 recoilVec)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D sparkle = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;

            float manaPower = MathHelper.Clamp(energyCoreGlowBurst / 12f, 0f, 1f);

            // effectsColor = 材料主题色；coreWhite = 主题色偏白
            Color effectsColor = themeColor;
            Color coreWhite = Color.Lerp(themeColor, Color.White, 0.45f);

            Vector2 shake = Main.rand.NextVector2Circular(2f, 2f) * manaPower;
            // recoilVec 使星芒随后坐力同步移动，与枪体保持一致
            Vector2 corePos = EnergyCorePosition + recoilVec - Main.screenPosition;
            float time = energyCoreGlowTime;

            // reverseManaPower：开火时臂展更大，与右键公式完全一致
            float reverseManaPower = MathHelper.Lerp(0.7f, 0.1f, manaPower > 0f ? 1f - manaPower : 0.5f);

            for (int i = 0; i < 5; i++)
            {
                float iMult = 1f - 0.1f * i;

                // BloomCircle：仅在开火时绘制（与右键一致）
                if (manaPower > 0f)
                {
                    Main.EntitySpriteDraw(
                        bloom,
                        corePos + shake,
                        null,
                        Color.Lerp(effectsColor, coreWhite, i * 0.1f) with { A = 0 },
                        Main.rand.NextFloat(-5f, 5f),
                        bloom.Size() * 0.5f,
                        new Vector2(1f, 0.35f) * 0.75f * manaPower * Main.rand.NextFloat(0.7f, 1.3f) * iMult,
                        flip);
                }

                // HalfStar 双向臂（与右键完全一致的参数）
                for (int b = -1; b <= 1; b += 2)
                {
                    float sine = MathHelper.Lerp(
                        (float)Math.Sin(Main.GlobalTimeWrappedHourly * 20f / MathHelper.Pi),
                        reverseManaPower * b,
                        0.75f);
                    Vector2 scale = new Vector2(0.3f, 1f * sine * b) * (Main.rand.NextFloat(3f, 4.5f) * iMult + manaPower * 1.2f);
                    float rotation = gunRotation
                        + time * manaPower * Math.Max(i - 2, 0) * 0.2f
                        + MathHelper.PiOver4 * b;

                    Main.EntitySpriteDraw(
                        sparkle,
                        corePos,
                        null,
                        Color.Lerp(effectsColor, coreWhite, i * 0.1f) with { A = 0 },
                        rotation,
                        sparkle.Size() * 0.5f,
                        scale,
                        flip);
                }
            }
        }
    }
}
