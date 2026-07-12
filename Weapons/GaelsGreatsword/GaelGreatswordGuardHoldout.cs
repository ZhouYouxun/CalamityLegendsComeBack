using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.GaelsGreatsword
{
    internal sealed class GaelGreatswordGuardHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword";

        private const float SwordVisualScale = 1.5f;
        private const float BladeReach = 132f;

        private static readonly Color DarkPurple = new(58, 18, 112);
        private static readonly Color BloodRed = new(175, 14, 40);
        private static readonly Color PaleCore = new(225, 207, 245);

        private Player Owner => Main.player[Projectile.owner];
        private int timer;
        private float guardAngle;
        private float scale = 1f;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 72;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 4;
            Projectile.noEnchantmentVisuals = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendGaelsGreatsword>())
            {
                Projectile.Kill();
                return;
            }

            GaelGreatswordPlayer gaelPlayer = Owner.GetModPlayer<GaelGreatswordPlayer>();
            if (gaelPlayer.GuardCooldown > 0 || !IsRightHeld())
            {
                Projectile.Kill();
                return;
            }

            timer++;
            scale = Owner.GetMeleeScale() * SwordVisualScale;
            Vector2 aim = Owner.MountedCenter.DirectionTo(NewLegendGaelsGreatsword.GetMouseWorld(Owner))
                .SafeNormalize(Vector2.UnitX * Owner.direction);
            Owner.direction = aim.X >= 0f ? 1 : -1;
            guardAngle = aim.ToRotation() - MathHelper.PiOver2 * Owner.direction;

            Projectile.Center = Owner.MountedCenter + new Vector2(Owner.direction * 12f, -4f);
            Projectile.rotation = guardAngle;
            Projectile.timeLeft = 4;

            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Owner.itemLocation = Owner.Center;
            Owner.itemRotation = guardAngle;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, guardAngle - MathHelper.ToRadians(116f));
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, guardAngle - MathHelper.ToRadians(152f));

            Vector2 guardCenter = Projectile.Center + guardAngle.ToRotationVector2() * BladeReach * scale * 0.52f;
            gaelPlayer.SetGuardActive(guardCenter, timer);

            // 持续举剑会缓慢积攒黑暗余烬，奖励沉住气的防守。
            if (timer % 45 == 0)
                gaelPlayer.AddDarkEmbers(1);

            EmitGuardEffects(guardCenter);
        }

        private bool IsRightHeld()
        {
            if (!Owner.channel)
                return false;
            if (Main.myPlayer != Projectile.owner)
                return true;

            return (Owner.Calamity().mouseRight || Main.mouseRight) && !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;
        }

        private void EmitGuardEffects(Vector2 guardCenter)
        {
            if (Main.dedServ)
                return;

            Lighting.AddLight(guardCenter, 0.28f, 0.04f, 0.34f);
            if (timer % 4 != 0)
                return;

            Vector2 bladeDirection = guardAngle.ToRotationVector2();
            Vector2 side = bladeDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 position = Projectile.Center + bladeDirection * Main.rand.NextFloat(30f, BladeReach) * scale + side * Main.rand.NextFloat(-12f, 12f);
            Vector2 velocity = side * Main.rand.NextFloat(-1.2f, 1.2f) - bladeDirection * Main.rand.NextFloat(0.4f, 1.8f);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(position, velocity, false,
                Main.rand.Next(18, 26), Main.rand.NextFloat(0.14f, 0.22f), Main.rand.NextBool() ? BloodRed : DarkPurple));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D swordTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D slash = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SwordSlashTexture").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;
            Vector2 origin = new(0f, swordTexture.Height);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            float drawRotation = guardAngle + MathHelper.PiOver4;
            float opacity = Utils.GetLerpValue(0f, 8f, timer, true);
            Vector2 bladeCenter = Projectile.Center + guardAngle.ToRotationVector2() * BladeReach * scale * 0.58f - Main.screenPosition;

            GaelGreatswordPlayer gaelPlayer = Owner.GetModPlayer<GaelGreatswordPlayer>();
            float parryWindow = gaelPlayer.ParryWindowOpen
                ? Utils.GetLerpValue(GaelGreatswordPlayer.ParryWindowFrames, 0f, timer, true)
                : 0f;
            float parryFlash = Utils.GetLerpValue(0f, 30f, gaelPlayer.ParryFlashTimer, true);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Main.EntitySpriteDraw(slash, bladeCenter, null, DarkPurple with { A = 0 } * opacity * 0.32f,
                guardAngle, slash.Size() * 0.5f, new Vector2(0.48f, 1.1f) * scale, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, bladeCenter, null, BloodRed with { A = 0 } * opacity * 0.28f,
                0f, bloom.Size() * 0.5f, scale * 0.5f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, PaleCore with { A = 0 } * opacity * 0.18f,
                0f, bloom.Size() * 0.5f, scale * 0.34f, SpriteEffects.None);

            // 完美格挡窗口：剑身泛起苍白流光，一圈收缩的光环提示时机正在流逝。
            if (parryWindow > 0.01f)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 3.4f * parryWindow;
                    Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null, PaleCore with { A = 0 } * parryWindow * 0.24f,
                        drawRotation, origin, scale, SpriteEffects.None);
                }

                float ringScale = MathHelper.Lerp(0.42f, 1.15f, parryWindow) * scale;
                Main.EntitySpriteDraw(ring, bladeCenter, null, PaleCore with { A = 0 } * parryWindow * 0.55f,
                    timer * 0.12f, ring.Size() * 0.5f, ringScale * 0.4f, SpriteEffects.None);
            }

            // 弹反成功的瞬间：整把剑爆发白炽闪光。
            if (parryFlash > 0.01f)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 5.5f * parryFlash;
                    Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null, Color.White with { A = 0 } * parryFlash * 0.3f,
                        drawRotation, origin, scale, SpriteEffects.None);
                }

                Main.EntitySpriteDraw(bloom, bladeCenter, null, Color.White with { A = 0 } * parryFlash * 0.6f,
                    0f, bloom.Size() * 0.5f, scale * (0.6f + parryFlash * 0.5f), SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(swordTexture, drawPosition, null, lightColor, drawRotation, origin, scale, SpriteEffects.None);
            return false;
        }
    }
}
