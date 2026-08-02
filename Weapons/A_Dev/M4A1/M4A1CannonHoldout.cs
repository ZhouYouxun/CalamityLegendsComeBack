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
    /// 右键：从背后武器箱展开的便携式重炮。生成时消耗约 35 点战术同步率，
    /// 按消耗前的阶段决定齐射形态（2 普通 / 2 普通+终结 / 三发更大范围）。
    /// 展开弹开动画结束后自动收起。
    /// </summary>
    public class M4A1CannonHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/M4A1/InheritedCase";

        private const int DeployFrames = 11;
        private const int FireFrame = 12;
        private const int TotalLife = 56;
        private const float ForwardOffset = 46f;

        private int tier;
        private int timer;
        private bool fired;
        private float recoil;
        private float gunRotation;
        private int spriteDir = 1;

        private Player Owner => Main.player[Projectile.owner];
        private InheritedCaseM4A1 Weapon => Owner.HeldItem.ModItem as InheritedCaseM4A1;
        private Vector2 AimDir => Projectile.velocity.SafeNormalize(Vector2.UnitX * Math.Max(Owner.direction, 1));
        private Vector2 MuzzlePos => Projectile.Center + gunRotation.ToRotationVector2() * 46f;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalLife + 4;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(IEntitySource source)
        {
            // 消耗前的阶段决定形态；实际扣除同步率在此发生（每次展开只扣一次）。
            tier = M4A1Player.Get(Owner).SpendForRightClick();

            SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.75f, Pitch = -0.35f }, Owner.Center);
            if (!Main.dedServ)
            {
                for (int i = 0; i < 12; i++)
                {
                    Dust smoke = Dust.NewDustPerfect(Owner.MountedCenter, DustID.Smoke, Main.rand.NextVector2Circular(3f, 3f), 130, Color.Gray, Main.rand.NextFloat(1.2f, 2f));
                    smoke.noGravity = true;
                }
            }
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Weapon == null || HasUltimate(Owner))
            {
                Projectile.Kill();
                return;
            }

            UpdateTransform();
            KeepUseAnimation();

            if (recoil > 0f)
                recoil = MathHelper.Lerp(recoil, 0f, 0.2f);

            if (!fired && timer >= FireFrame)
            {
                fired = true;
                FireBurst();
            }

            timer++;
            if (timer >= TotalLife)
            {
                Projectile.Kill();
                return;
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
            Projectile.Center = armPos + aimDir * (ForwardOffset - recoil) + new Vector2(0f, -6f * Owner.gravDir);

            float armRotation = (gunRotation - MathHelper.PiOver2) * Owner.gravDir + (Owner.gravDir == -1f ? MathHelper.Pi : 0f);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRotation + 0.1f * spriteDir);

            if (Projectile.owner == Main.myPlayer)
                Projectile.netUpdate = true;
        }

        private void KeepUseAnimation() => Owner.itemTime = Owner.itemAnimation = 2;

        private void FireBurst()
        {
            if (Projectile.owner != Main.myPlayer)
            {
                Recoil();
                return;
            }

            Vector2 dir = AimDir;
            // 0 档：两枚普通；1 档及以上：普通 + 终结 + 普通；2 档及以上追加更大范围（由 SyncTier 决定）。
            (float deg, bool finisher)[] pattern = tier == 0
                ? new[] { (-4f, false), (4f, false) }
                : new[] { (-8f, false), (0f, true), (8f, false) };

            int shellType = ModContent.ProjectileType<M4A1Shell>();
            foreach ((float deg, bool finisher) in pattern)
            {
                Vector2 velocity = dir.RotatedBy(MathHelper.ToRadians(deg)) * (Owner.HeldItem.shootSpeed * (finisher ? 0.8f : 0.95f));
                int rawBase = finisher ? BalanceM4A1.GetFinisherShellBaseDamage() : BalanceM4A1.GetShellBaseDamage();
                int damage = InheritedCaseM4A1.ScaledDamage(Owner, Owner.HeldItem, rawBase);

                int index = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    MuzzlePos,
                    velocity,
                    shellType,
                    damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    finisher ? 1f : 0f,
                    tier);

                if (Main.projectile.IndexInRange(index))
                {
                    Main.projectile[index].DamageType = DamageClass.Ranged;
                    Main.projectile[index].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                }
            }

            Recoil();
        }

        private void Recoil()
        {
            recoil = 12f;
            Owner.velocity -= AimDir * 1.4f;
            if (Main.myPlayer == Owner.whoAmI)
                Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 2.2f);

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.7f, Pitch = -0.2f }, MuzzlePos);
            if (Main.dedServ)
                return;

            Color theme = M4A1Visuals.StageColor(Math.Clamp(tier, 0, 3));
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = AimDir.RotatedByRandom(0.25f) * Main.rand.NextFloat(4f, 14f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(MuzzlePos, vel, false, 16, Main.rand.NextFloat(0.4f, 0.8f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.3f, 0.7f)), true, true));
            }
            GeneralParticleHandler.SpawnParticle(new GenericBloom(MuzzlePos, Vector2.Zero, Color.Lerp(theme, Color.White, 0.3f), 0.9f, 16, true));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Weapon == null)
                return false;

            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;

            float pop = MathHelper.Clamp(timer / (float)DeployFrames, 0f, 1f);
            float ease = 1f - (1f - pop) * (1f - pop); // ease-out
            float scale = MathHelper.Lerp(0.12f, 0.79f, ease); // 贴图整体 ×0.6

            Vector2 aimDir = gunRotation.ToRotationVector2();
            Vector2 drawCenter = Projectile.Center - aimDir * recoil - Main.screenPosition;
            SpriteEffects flip = spriteDir == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            if (Owner.gravDir == -1f)
                flip ^= SpriteEffects.FlipVertically;

            // 干净绘制（无包边环），保留展开弹开缩放 + 重炮后坐位移
            Main.EntitySpriteDraw(tex, drawCenter, null, Projectile.GetAlpha(lightColor), gunRotation, origin, scale, flip, 0);

            return false;
        }

        private static bool HasUltimate(Player owner)
        {
            int ult = ModContent.ProjectileType<M4A1UltimateHoldout>();
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.owner == owner.whoAmI && p.type == ult)
                    return true;
            }
            return false;
        }
    }
}
