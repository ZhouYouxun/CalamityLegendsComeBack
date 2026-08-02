using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 左键持械：M4A1 自动步枪。长按持续射击并暖机（提升战术同步率 -> 阶段），
    /// 时不时甩出火箭弹；阶段越高射速/弹速/精度越好、枪体发光随之升温。
    /// 松开左键后短暂放下并自动消失（非常态手持）。
    /// </summary>
    public class M4A1LeftHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/M4A1/M4A1";

        private const float ForwardOffset = 52f;
        private const float VerticalOffset = 2f;
        private const float MuzzleReach = 50f;
        private const int ReleaseLowerGrace = 8;

        private float shotAccumulator;
        private int consecutiveShots;
        private int rocketTimer;
        private int notFiringTicks;
        private float recoilOffset;
        private float gunRotation;
        private int spriteDir = 1;
        private int muzzleFlash;

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
            Projectile.width = 60;
            Projectile.height = 30;
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
            if (!Owner.active || Owner.dead || Weapon == null)
            {
                Projectile.Kill();
                return;
            }

            // 右键重炮 / 大招进行中时让位
            if (HasOtherHoldout(Owner))
            {
                Projectile.Kill();
                return;
            }

            UpdateTransform();

            if (muzzleFlash > 0) muzzleFlash--;
            if (recoilOffset > 0f) recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.3f);

            bool leftHeld = Projectile.owner == Main.myPlayer &&
                Main.mouseLeft &&
                InheritedCaseM4A1.CanUseWorldInput(Owner);

            if (!leftHeld)
            {
                consecutiveShots = 0;
                shotAccumulator = 0f;
                notFiringTicks++;
                // 松开后短暂放下再消失
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

            M4A1Player mp = M4A1Player.Get(Owner);
            int stage = mp.SyncStage;

            // ===== 子弹节奏（RPM 累加器）=====
            shotAccumulator += Math.Max(0.01f, BalanceM4A1.GetFireRateRpm(stage) / 3600f);
            int guard = 0;
            while (shotAccumulator >= 1f && guard < 3)
            {
                shotAccumulator -= 1f;
                guard++;
                FireBullet(stage);
            }

            // ===== 火箭弹节奏 =====
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
            Projectile.Center = armPos + aimDir * (ForwardOffset - recoilOffset) + new Vector2(0f, VerticalOffset * Owner.gravDir);

            float armRotation = (gunRotation - MathHelper.PiOver2) * Owner.gravDir + (Owner.gravDir == -1f ? MathHelper.Pi : 0f);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRotation + 0.1f * spriteDir);

            if (Projectile.owner == Main.myPlayer)
                Projectile.netUpdate = true;
        }

        private void KeepUseAnimation()
        {
            Owner.itemTime = Owner.itemAnimation = 2;
        }

        // ===================================================================
        //  发射
        // ===================================================================
        private void FireBullet(int stage)
        {
            Vector2 dir = AimDir;

            // 持续射击收束：暖机让弹道逐渐稳定（叠加阶段基础散布）。
            float sustainedTighten = MathHelper.Lerp(1f, 0.35f, MathHelper.Clamp(consecutiveShots / 45f, 0f, 1f));
            float spread = BalanceM4A1.GetSpreadDegrees(stage) * sustainedTighten;
            consecutiveShots++;

            Vector2 velocity = dir.RotatedByRandom(MathHelper.ToRadians(spread)) *
                (Owner.HeldItem.shootSpeed * BalanceM4A1.GetBulletSpeedMultiplier(stage));

            int damage = InheritedCaseM4A1.ScaledDamage(Owner, Owner.HeldItem, BalanceM4A1.GetBulletBaseDamage());
            int index = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTip + Main.rand.NextVector2Circular(2f, 2f),
                velocity,
                ModContent.ProjectileType<M4A1Bullet>(),
                damage,
                Projectile.knockBack,
                Projectile.owner);

            if (Main.projectile.IndexInRange(index))
            {
                Projectile shot = Main.projectile[index];
                shot.DamageType = DamageClass.Ranged;
                shot.CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
            }

            ApplyRecoil(2.5f);
            SpawnMuzzleFlash(dir, stage, false);
        }

        private void FireRocket(int stage)
        {
            Vector2 dir = AimDir;
            Vector2 velocity = dir * (Owner.HeldItem.shootSpeed * 0.7f);

            int damage = InheritedCaseM4A1.ScaledDamage(Owner, Owner.HeldItem, BalanceM4A1.GetRocketBaseDamage());
            int index = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTip,
                velocity,
                ModContent.ProjectileType<M4A1Rocket>(),
                damage,
                Projectile.knockBack * 1.6f,
                Projectile.owner);

            if (Main.projectile.IndexInRange(index))
            {
                Projectile shot = Main.projectile[index];
                shot.DamageType = DamageClass.Ranged;
                shot.CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
            }

            ApplyRecoil(7f);
            SpawnMuzzleFlash(dir, stage, true);
            SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.5f, Pitch = -0.1f }, GunTip);
        }

        private void ApplyRecoil(float power)
        {
            recoilOffset = power;
            muzzleFlash = 4;
            Owner.velocity -= AimDir * (power * 0.02f);
        }

        private void SpawnMuzzleFlash(Vector2 dir, int stage, bool heavy)
        {
            SoundEngine.PlaySound(SoundID.Item41 with { Volume = heavy ? 0.6f : 0.32f, Pitch = heavy ? -0.2f : 0.25f, PitchVariance = 0.12f, MaxInstances = 5 }, GunTip);

            if (Main.dedServ || Projectile.owner != Main.myPlayer)
                return;

            Color theme = StageColor(stage);
            int sparks = heavy ? 7 : 3;
            for (int i = 0; i < sparks; i++)
            {
                Vector2 vel = dir.RotatedByRandom(0.18f) * Main.rand.NextFloat(3f, heavy ? 12f : 8f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    GunTip + dir * Main.rand.NextFloat(0f, 4f),
                    vel,
                    false,
                    heavy ? 14 : 9,
                    Main.rand.NextFloat(0.3f, heavy ? 0.75f : 0.5f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.3f, 0.7f)),
                    true,
                    true));
            }

            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                GunTip,
                dir * Main.rand.NextFloat(1.5f, 3f),
                Main.rand.NextFloat(0.22f, heavy ? 0.5f : 0.34f),
                Color.Lerp(theme, Color.White, 0.35f),
                Main.rand.Next(8, 14)));

            for (int i = 0; i < (heavy ? 5 : 2); i++)
            {
                Dust smoke = Dust.NewDustPerfect(GunTip, DustID.Smoke, -dir.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.6f, 2f), 130, Color.Gray, Main.rand.NextFloat(0.6f, 1f));
                smoke.noGravity = true;
            }
        }

        private static Color StageColor(int stage) => M4A1Visuals.StageColor(stage);

        // ===================================================================
        //  绘制：枪体 + 战术发光包边（随阶段升温）+ 枪口暴闪
        // ===================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Weapon == null)
                return false;

            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;

            int stage = M4A1Player.Get(Owner).SyncStage;
            Color theme = StageColor(stage);

            Vector2 aimDir = gunRotation.ToRotationVector2();
            Vector2 drawCenter = Projectile.Center - aimDir * recoilOffset - Main.screenPosition;

            SpriteEffects flip = spriteDir == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            if (Owner.gravDir == -1f)
                flip ^= SpriteEffects.FlipVertically;

            float idlePulse = 0.5f + 0.5f * (MathF.Sin(Main.GlobalTimeWrappedHourly * 3f + Projectile.identity) * 0.5f + 0.5f);
            float stageIntensity = 0.25f + stage * 0.22f;
            float flashPulse = MathHelper.Clamp(muzzleFlash / 4f, 0f, 1f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 发光包边（14 方向外圈 + 8 方向内圈），强度随阶段与开火升温
            Color outline = (Color.Lerp(theme, Color.White, 0.5f) with { A = 0 }) * (stageIntensity * (0.6f + idlePulse * 0.5f) + flashPulse * 0.7f);
            float outlineDist = 1.6f + stage * 0.8f + flashPulse * 3.2f;
            for (int i = 0; i < 14; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 14f).ToRotationVector2() * outlineDist;
                Main.EntitySpriteDraw(tex, drawCenter + offset, null, outline, gunRotation, origin, Projectile.scale, flip, 0);
            }

            // 枪体
            Main.EntitySpriteDraw(tex, drawCenter, null, lightColor, gunRotation, origin, Projectile.scale, flip, 0);

            // 枪口核心暴闪
            if (flashPulse > 0f || stage >= 2)
                DrawMuzzleCore(theme, aimDir, flashPulse, stage);

            return false;
        }

        private void DrawMuzzleCore(Color theme, Vector2 aimDir, float flashPulse, int stage)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D sparkle = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;

            Vector2 corePos = GunTip - aimDir * recoilOffset - Main.screenPosition;
            float power = Math.Max(flashPulse, stage >= 3 ? 0.28f : stage * 0.08f);

            Main.EntitySpriteDraw(bloom, corePos, null, (Color.Lerp(theme, Color.White, 0.4f) with { A = 0 }) * (0.35f + power * 0.7f),
                0f, bloom.Size() * 0.5f, new Vector2(0.9f, 0.5f) * (0.15f + power * 0.6f), SpriteEffects.None, 0);

            for (int b = -1; b <= 1; b += 2)
            {
                Vector2 scale = new Vector2(0.28f, 0.9f * b) * (2.6f + power * 4f);
                Main.EntitySpriteDraw(sparkle, corePos, null, (Color.Lerp(theme, Color.White, 0.5f) with { A = 0 }) * (0.4f + power * 0.6f),
                    gunRotation + MathHelper.PiOver4 * b, sparkle.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }
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
