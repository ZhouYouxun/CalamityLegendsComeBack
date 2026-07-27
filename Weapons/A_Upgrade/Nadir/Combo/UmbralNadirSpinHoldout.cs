using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.Combo
{
    /// <summary>
    /// 回旋斩迹——"按住左键期间再按右键"进入的贴身模式。
    /// 一根长矛绕玩家匀速回旋，用细长 line 碰撞清理贴身包围，同时**把周围敌人吸拢**、叠蚀痕、蓄奇点。
    /// 奇点蓄满即以玩家为中心释放黑洞新星（消耗蚀痕暴发）。只在左右键同时按住时维持。
    /// 视觉：矛尖黑色 shader 环迹 + 黑砂 + 每圈至多一次很弱的绿边缘闪烁。回旋本体不暴击。
    /// ai[0] = 左键基础伤害（供新星），ai[1] = 阶段尺寸。
    /// </summary>
    public class UmbralNadirSpinHoldout : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Upgrade/Nadir/UmbralNadirSpear";

        private float spinAngle;
        private int spinDir;
        private bool initialized;
        private float greenFlashCooldown;
        private readonly List<Vector2> tipHistory = new();

        private Player Owner => Main.player[Projectile.owner];
        public ref float NovaBaseDamage => ref Projectile.ai[0];
        public ref float Scale => ref Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = UmbralNadirBalance.SpinHitCooldown;
        }

        private bool BothKeysHeld()
        {
            if (Projectile.owner == Main.myPlayer)
                return Main.mouseLeft && Main.mouseRight && !Main.mapFullscreen && !Main.blockMouse;
            return Owner.channel;
        }

        public override void AI()
        {
            if (!initialized)
            {
                initialized = true;
                Vector2 aim = (Main.MouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
                spinDir = aim.X >= 0f ? 1 : -1;
                spinAngle = aim.ToRotation();
                if (Scale <= 0f)
                    Scale = 1f;
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.5f, Pitch = -0.2f }, Owner.Center);
            }

            bool active = Owner.active && !Owner.dead && !Owner.CCed && !Owner.noItems &&
                          Owner.HeldItem.type == ModContent.ItemType<UmbralNadir>();
            if (!active || (Projectile.owner == Main.myPlayer && !BothKeysHeld()))
            {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 2;
            Owner.Calamity().rightClickListener = true;
            Owner.Calamity().mouseWorldListener = true;

            // 匀速回旋 + 半径呼吸
            spinAngle += UmbralNadirBalance.SpinRotationSpeed * spinDir;
            float radius = MathHelper.Lerp(UmbralNadirBalance.SpinRadiusMin, UmbralNadirBalance.SpinRadiusMax,
                0.5f + 0.5f * (float)Math.Sin(spinAngle * 2f)) * Scale;
            Vector2 dir = spinAngle.ToRotationVector2();
            Projectile.Center = Owner.MountedCenter + dir * radius;

            // 把周围敌人吸拢到玩家身边（回旋的战术职责：卷走贴身包围）
            UmbralNadirVisuals.PullNPCs(Owner.MountedCenter, radius + 120f, 0.5f);

            // 玩家持握姿态
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.ChangeDir(dir.X >= 0f ? 1 : -1);
            Owner.itemRotation = dir.ToRotation();
            if (Owner.direction != 1)
                Owner.itemRotation -= MathHelper.Pi;
            Owner.itemRotation = MathHelper.WrapAngle(Owner.itemRotation);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, dir.ToRotation() - MathHelper.PiOver2);

            // 矛尖世界坐标 → 黑色 shader 环迹
            Vector2 tip = Owner.MountedCenter + dir * (radius + 34f * Scale);
            tipHistory.Insert(0, tip);
            if (tipHistory.Count > 16)
                tipHistory.RemoveAt(tipHistory.Count - 1);

            // 少量黑砂
            if (Main.rand.NextBool(2))
                GeneralParticleHandler.SpawnParticle(new GenericBloom(tip, dir.RotatedBy(MathHelper.PiOver2 * spinDir) * Main.rand.NextFloat(0.5f, 2f),
                    Color.Black, Main.rand.NextFloat(0.14f, 0.26f), Main.rand.Next(7, 11), true, false));

            // 每圈至多一次很弱的绿边缘闪烁
            if (greenFlashCooldown > 0f)
                greenFlashCooldown--;
            else if (Main.rand.NextBool(30))
            {
                greenFlashCooldown = MathHelper.TwoPi / UmbralNadirBalance.SpinRotationSpeed;
                GeneralParticleHandler.SpawnParticle(new GenericBloom(tip, Vector2.Zero, UmbralNadirPalette.MeldGreen with { A = 0 },
                    0.28f * Scale, 10, false, true), false, GeneralDrawLayer.AfterEverything);
            }

            // 奇点蓄满 → 释放黑洞新星
            if (Projectile.owner == Main.myPlayer)
            {
                var mp = Owner.GetModPlayer<UmbralNadirPlayer>();
                if (mp.NovaReady)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.MountedCenter, Vector2.Zero,
                        ModContent.ProjectileType<UmbralNadirSpinNova>(), 1, Projectile.knockBack, Projectile.owner, NovaBaseDamage);
                    mp.SpendChargeForNova();
                }
            }
        }

        public override bool? CanDamage() => initialized;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 dir = spinAngle.ToRotationVector2();
            Vector2 start = Owner.MountedCenter;
            Vector2 end = Owner.MountedCenter + dir * UmbralNadirBalance.SpinLineCollision * Scale;
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 20f * Scale, ref _);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 90);
            UmbralCorrosionGlobalNPC.AddStacks(target, UmbralNadirBalance.SpinStackPerHit);
            if (Projectile.owner == Main.myPlayer)
                Owner.GetModPlayer<UmbralNadirPlayer>().AddCharge(UmbralNadirBalance.ChargePerSpinHit);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 矛尖黑色 shader 环迹
            if (tipHistory.Count >= 2)
                UmbralNadirVisuals.RenderTipTrail(tipHistory, 24f, Scale, false);

            // 矛体：黑剪影垫底 + 主体，矛尖朝外（radialDir + 135°）
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float rot = (Projectile.Center - Owner.MountedCenter).SafeNormalize(Vector2.UnitX).ToRotation() + MathHelper.PiOver2 + MathHelper.PiOver4;
            Main.EntitySpriteDraw(tex, drawPos, null, Color.Black * 0.5f, rot, origin, Scale * 1.06f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex, drawPos, null, lightColor, rot, origin, Scale, SpriteEffects.None, 0);
            Owner.heldProj = Projectile.whoAmI;
            return false;
        }
    }
}
