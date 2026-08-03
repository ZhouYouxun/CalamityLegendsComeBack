using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 左键持械：M4A1 自动步枪。长按持续射击并暖机（提升战术同步率 -> 阶段），
    /// 发射三种弹幕：特殊子弹（主流）、荧光绿能量弹（穿插）、火箭弹（间歇）。
    /// 干净绘制（参考 OmniGun / ScorchedEarth），无后坐动画、无发光包边环。
    /// 松开左键后短暂放下并自动消失（非常态手持）。
    /// </summary>
    public class M4A1LeftHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/M4A1/M4A1";

        private const float DrawScale = 0.6f;      // 贴图整体 ×0.6（原来偏大）
        private const float ForwardOffset = 14f; // 往后挪 ~32px（枪更贴身）
        private const float VerticalOffset = 2f;
        private const float MuzzleReach = 30f;
        private const int ReleaseLowerGrace = 8;

        private float shotAccumulator;
        private int consecutiveShots;
        private int rocketTimer;
        private int orbTimer;
        private int notFiringTicks;
        private int muzzleFlash;
        private int muzzleVariant;
        private float gunRotation;
        private int spriteDir = 1;

        private Player Owner => Main.player[Projectile.owner];
        private InheritedCaseM4A1 Weapon => Owner.HeldItem.ModItem as InheritedCaseM4A1;

        private Vector2 AimDir => Projectile.velocity.SafeNormalize(Vector2.UnitX * Math.Max(Owner.direction, 1));
        private Vector2 GunTip => Projectile.Center + gunRotation.ToRotationVector2() * MuzzleReach;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 22;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Weapon == null || HasOtherHoldout(Owner))
            {
                Projectile.Kill();
                return;
            }

            UpdateTransform();
            if (muzzleFlash > 0) muzzleFlash--;

            bool leftHeld = Projectile.owner == Main.myPlayer &&
                Main.mouseLeft &&
                InheritedCaseM4A1.CanUseWorldInput(Owner);

            if (!leftHeld)
            {
                consecutiveShots = 0;
                shotAccumulator = 0f;
                notFiringTicks++;
                if (notFiringTicks > ReleaseLowerGrace)
                {
                    Projectile.Kill();
                    return;
                }
                Projectile.timeLeft = 2;
                return;
            }

            notFiringTicks = 0;
            KeepUseAnimation();

            int stage = M4A1Player.Get(Owner).SyncStage;

            // ===== 特殊子弹（RPM 累加器）=====
            shotAccumulator += Math.Max(0.01f, BalanceM4A1.GetFireRateRpm(stage) / 3600f);
            int guard = 0;
            while (shotAccumulator >= 1f && guard < 3)
            {
                shotAccumulator -= 1f;
                guard++;
                FireBullet(stage);
            }

            // ===== 荧光绿能量弹 =====
            if (orbTimer > 0)
                orbTimer--;
            else
            {
                FireEnergyOrb(stage);
                orbTimer = BalanceM4A1.GetEnergyOrbInterval(stage);
            }

            // ===== 火箭弹（频率保持不变）=====
            if (rocketTimer > 0)
                rocketTimer--;
            else
            {
                FireRocket(stage);
                rocketTimer = BalanceM4A1.GetRocketInterval(stage);
            }

            Projectile.timeLeft = 2;
        }

        private void UpdateTransform()
        {
            Vector2 aimWorld = Projectile.owner == Main.myPlayer
                ? InheritedCaseM4A1.GetMouseWorld(Owner)
                : Owner.Calamity().mouseWorld;

            Vector2 armPos = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Vector2 aimDir = (aimWorld - armPos).SafeNormalize(Vector2.UnitX * Owner.direction);

            gunRotation = aimDir.ToRotation();
            spriteDir = aimDir.X >= 0f ? 1 : -1;

            Owner.ChangeDir(spriteDir);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemRotation = (aimDir * spriteDir).ToRotation();
            Owner.HeldItem.noUseGraphic = true;

            Projectile.velocity = aimDir;
            Projectile.rotation = gunRotation;
            Projectile.spriteDirection = spriteDir;
            Projectile.Center = armPos + aimDir * ForwardOffset + new Vector2(0f, VerticalOffset * Owner.gravDir);

            float armRotation = (gunRotation - MathHelper.PiOver2) * Owner.gravDir + (Owner.gravDir == -1f ? MathHelper.Pi : 0f);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRotation + 0.1f * spriteDir);

            if (Projectile.owner == Main.myPlayer)
                Projectile.netUpdate = true;
        }

        private void KeepUseAnimation() => Owner.itemTime = Owner.itemAnimation = 2;

        // ===================================================================
        //  发射三弹种
        // ===================================================================
        private void FireBullet(int stage)
        {
            Vector2 dir = AimDir;
            float sustainedTighten = MathHelper.Lerp(1f, 0.35f, MathHelper.Clamp(consecutiveShots / 45f, 0f, 1f));
            float spread = BalanceM4A1.GetSpreadDegrees(stage) * sustainedTighten;
            consecutiveShots++;

            Vector2 velocity = dir.RotatedByRandom(MathHelper.ToRadians(spread)) *
                (Owner.HeldItem.shootSpeed * BalanceM4A1.GetBulletSpeedMultiplier(stage));

            int damage = InheritedCaseM4A1.ScaledDamage(Owner, Owner.HeldItem, BalanceM4A1.GetBulletBaseDamage());
            SpawnShot(ModContent.ProjectileType<M4A1Bullet>(), GunTip + Main.rand.NextVector2Circular(2f, 2f), velocity, damage, Projectile.knockBack);

            muzzleFlash = 4;
            SpawnMuzzleFlash(dir, false);
        }

        private void FireEnergyOrb(int stage)
        {
            Vector2 dir = AimDir;
            Vector2 velocity = dir.RotatedByRandom(MathHelper.ToRadians(2f)) * (Owner.HeldItem.shootSpeed * 0.72f);
            int damage = InheritedCaseM4A1.ScaledDamage(Owner, Owner.HeldItem, (int)(BalanceM4A1.GetBulletBaseDamage() * 1.8f));
            SpawnShot(ModContent.ProjectileType<M4A1EnergyOrb>(), GunTip, velocity, damage, Projectile.knockBack);

            muzzleFlash = 5;
            SpawnMuzzleFlash(dir, true);
            SoundEngine.PlaySound(SoundID.Item157 with { Volume = 0.35f, Pitch = 0.35f, PitchVariance = 0.1f, MaxInstances = 4 }, GunTip);
        }

        private void FireRocket(int stage)
        {
            Vector2 dir = AimDir;
            Vector2 velocity = dir * (Owner.HeldItem.shootSpeed * 0.7f);
            int damage = InheritedCaseM4A1.ScaledDamage(Owner, Owner.HeldItem, BalanceM4A1.GetRocketBaseDamage());
            SpawnShot(ModContent.ProjectileType<M4A1Rocket>(), GunTip, velocity, damage, Projectile.knockBack * 1.6f);

            muzzleFlash = 6;
            SpawnMuzzleFlash(dir, true);
            SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.5f, Pitch = -0.1f }, GunTip);
        }

        private void SpawnShot(int type, Vector2 pos, Vector2 velocity, int damage, float knockback)
        {
            int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, velocity, type, damage, knockback, Projectile.owner);
            if (Main.projectile.IndexInRange(index))
            {
                Projectile shot = Main.projectile[index];
                shot.DamageType = DamageClass.Ranged;
                shot.CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
            }
        }

        private void SpawnMuzzleFlash(Vector2 dir, bool heavy)
        {
            muzzleVariant = Main.rand.Next(3);
            SoundEngine.PlaySound(SoundID.Item41 with { Volume = heavy ? 0.6f : 0.4f, Pitch = heavy ? -0.15f : 0.25f, PitchVariance = 0.12f, MaxInstances = 5 }, GunTip);

            if (Main.dedServ || Projectile.owner != Main.myPlayer)
                return;

            // 冲击环（枪口爆发的猛劲）
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                GunTip, dir * (heavy ? 4f : 2.5f), M4A1Visuals.NeonGreen with { A = 0 },
                new Vector2(0.35f, 1f), gunRotation, 0.12f, heavy ? 0.55f : 0.4f, heavy ? 16 : 12));

            // 迸射的绿火花
            int sparks = heavy ? 10 : 6;
            for (int i = 0; i < sparks; i++)
            {
                Vector2 vel = dir.RotatedByRandom(0.28f) * Main.rand.NextFloat(4f, heavy ? 16f : 11f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    GunTip + dir * Main.rand.NextFloat(0f, 6f),
                    vel,
                    false,
                    heavy ? 15 : 10,
                    Main.rand.NextFloat(0.35f, heavy ? 0.85f : 0.6f),
                    Color.Lerp(M4A1Visuals.NeonGreen, M4A1Visuals.NeonGreenBright, Main.rand.NextFloat(0.3f, 0.85f)),
                    true,
                    true));
            }

            // 尖锐火星（细长）
            for (int i = 0; i < (heavy ? 5 : 3); i++)
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    GunTip, dir.RotatedByRandom(0.22f) * Main.rand.NextFloat(6f, heavy ? 20f : 13f),
                    false, Main.rand.Next(9, 16), Main.rand.NextFloat(0.4f, 0.75f), M4A1Visuals.NeonGreenBright));
            }

            for (int i = 0; i < (heavy ? 5 : 3); i++)
            {
                Dust smoke = Dust.NewDustPerfect(GunTip, DustID.Smoke, -dir.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.6f, 2f), 130, Color.Gray, Main.rand.NextFloat(0.7f, 1.2f));
                smoke.noGravity = true;
            }

            Lighting.AddLight(GunTip, 0.5f, 1.1f, 0.35f);
        }

        // ===================================================================
        //  绘制：干净的枪体（无包边环 / 无星芒 / 无后坐），加轻量绿枪口闪
        // ===================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Weapon == null)
                return false;

            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;

            SpriteEffects flip = spriteDir == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            if (Owner.gravDir == -1f)
                flip ^= SpriteEffects.FlipVertically;

            // 枪口爆闪：绿枪口闪光贴图 + 加法光晕（无旋转贴图堆）
            float flashPulse = MathHelper.Clamp(muzzleFlash / 6f, 0f, 1f);
            if (flashPulse > 0f)
            {
                Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                Texture2D flashTex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/M1GarandMuzzleFlash").Value;
                Rectangle flashFrame = flashTex.Frame(1, 3, 0, muzzleVariant);
                Vector2 aimDir = gunRotation.ToRotationVector2();
                Vector2 muzzle = GunTip - Main.screenPosition;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                // 光晕
                Main.EntitySpriteDraw(bloom, muzzle, null, (M4A1Visuals.NeonGreen with { A = 0 }) * (flashPulse * 0.95f), gunRotation, bloom.Size() * 0.5f, new Vector2(0.42f, 0.2f) * (0.6f + flashPulse), SpriteEffects.None, 0);
                Main.EntitySpriteDraw(bloom, muzzle, null, (M4A1Visuals.NeonGreenBright with { A = 0 }) * (flashPulse * 0.85f), 0f, bloom.Size() * 0.5f, 0.16f * (0.6f + flashPulse), SpriteEffects.None, 0);
                // 枪口闪光贴图（染绿）
                Color flashColor = (Color.Lerp(M4A1Visuals.NeonGreen, Color.White, 0.45f) with { A = 0 }) * flashPulse;
                Main.EntitySpriteDraw(flashTex, muzzle + aimDir * 4f, flashFrame, flashColor, gunRotation, flashFrame.Size() * 0.5f, (0.42f + flashPulse * 0.4f), flip, 0);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }

            // 枪体本体（干净）
            Main.EntitySpriteDraw(tex, drawCenter, null, Projectile.GetAlpha(lightColor), gunRotation, origin, DrawScale, flip, 0);
            return false;
        }

        private static bool HasOtherHoldout(Player owner)
        {
            int cannon = ModContent.ProjectileType<M4A1CannonHoldout>();
            int ult = ModContent.ProjectileType<M4A1UltimateHoldout>();
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.owner == owner.whoAmI && (p.type == cannon || p.type == ult))
                    return true;
            }
            return false;
        }
    }
}
