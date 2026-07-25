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
    internal sealed class GaelGreatswordPlungeHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword";

        private const int TelegraphFrames = 8;
        private const int DescentFrames = 18;
        private const int Duration = 48;
        private const float SwordVisualScale = 1.64f;
        private const float BladeReach = 158f;
        private const float ImpactRadius = 136f;

        // 硫火红-虚空黑，与至尊灾厄同源。
        private static readonly Color BrimBody = GaelGreatswordVisuals.BrimstoneRed;
        private static readonly Color BloodRed = GaelGreatswordVisuals.BrimstoneHot;
        private static readonly Color PaleCore = GaelGreatswordVisuals.WhiteHot;

        private Player Owner => Main.player[Projectile.owner];
        private int timer;
        private bool initialized;
        private bool impactEffectsPlayed;
        private float currentAngle;
        private float scale = 1f;
        private Vector2 startPoint;
        private Vector2 targetPoint;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 72;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
            Projectile.timeLeft = 4;
            Projectile.noEnchantmentVisuals = true;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendGaelsGreatsword>())
            {
                Projectile.Kill();
                return;
            }

            scale = Owner.GetMeleeScale() * SwordVisualScale;
            if (!initialized)
                InitializePlunge();

            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Projectile.timeLeft = 4;

            timer++;
            float descentProgress = MathHelper.Clamp((timer - TelegraphFrames) / (float)DescentFrames, 0f, 1f);
            float easedDescent = CalamityUtils.EaseInOutExp(descentProgress, 5f, 2f);
            Vector2 intendedCenter = Vector2.Lerp(startPoint, targetPoint, easedDescent);
            if (timer <= TelegraphFrames + DescentFrames + 2)
            {
                Owner.Center = intendedCenter;
                Owner.velocity = descentProgress < 1f ? new Vector2(0f, 18f) : Vector2.Zero;
                Owner.fallStart = (int)(Owner.position.Y / 16f);
            }
            else
            {
                Owner.velocity *= 0.72f;
            }

            Projectile.Center = Owner.MountedCenter;
            currentAngle = MathHelper.PiOver2 + MathHelper.Lerp(-0.42f * Owner.direction, 0.08f * Owner.direction, easedDescent);

            if (!impactEffectsPlayed && timer > TelegraphFrames)
                EmitDescentEffects(descentProgress);

            if (!impactEffectsPlayed && timer >= TelegraphFrames + DescentFrames)
            {
                impactEffectsPlayed = true;
                SpawnImpactEffects();
                Owner.GetModPlayer<GaelGreatswordPlayer>().FollowupSlashWindow = 45;
                Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 6.4f);
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.88f, Pitch = -0.5f }, targetPoint);
            }

            float armAngle = currentAngle - MathHelper.ToRadians(130f);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.itemLocation = Owner.Center;
            Owner.itemRotation = currentAngle;

            if (timer >= Duration)
            {
                Owner.GetModPlayer<GaelGreatswordPlayer>().FollowupSlashWindow = 45;
                Projectile.Kill();
            }
        }

        public override bool? CanDamage()
        {
            return timer >= TelegraphFrames && timer <= Duration - 8 ? null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 hilt = Owner.MountedCenter - Vector2.UnitY * 10f;
            Vector2 bladeTip = hilt + currentAngle.ToRotationVector2() * BladeReach * scale;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                hilt, bladeTip, 38f * scale, ref collisionPoint))
            {
                return null;
            }

            if (impactEffectsPlayed && targetHitbox.ClosestPointInRect(targetPoint).Distance(targetPoint) <= ImpactRadius)
                return null;

            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.55f, MathHelper.Clamp(Projectile.numHits / 5f, 0f, 1f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            GaelGreatswordPlayer gaelPlayer = Owner.GetModPlayer<GaelGreatswordPlayer>();
            gaelPlayer.FollowupSlashWindow = 45;
            gaelPlayer.RegisterGreatswordHit(target, 16, true);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 4.5f);

            if (Main.myPlayer == Projectile.owner)
            {
                int soulDamage = Math.Max(1, (int)(Projectile.damage * 0.22f));
                for (int i = 0; i < 2; i++)
                {
                    Vector2 spawnPosition = target.Center + Main.rand.NextVector2Circular(120f, 100f);
                    Vector2 velocity = spawnPosition.DirectionTo(target.Center).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(7f, 10f);
                    Projectile.NewProjectile(Projectile.GetSource_OnHit(target), spawnPosition, velocity,
                        ModContent.ProjectileType<GaelGreatswordDarkSoul>(), soulDamage, 1f, Projectile.owner, target.whoAmI);
                }
            }
        }

        private void InitializePlunge()
        {
            initialized = true;
            targetPoint = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            if (targetPoint == Vector2.Zero)
                targetPoint = NewLegendGaelsGreatsword.GetMouseWorld(Owner);

            startPoint = targetPoint + new Vector2(0f, -10f * 16f);
            Owner.direction = Math.Sign(targetPoint.X - Owner.Center.X);
            if (Owner.direction == 0)
                Owner.direction = 1;

            Owner.Center = startPoint;
            Owner.velocity = Vector2.Zero;
            Owner.fallStart = (int)(Owner.position.Y / 16f);
            currentAngle = MathHelper.PiOver2 - 0.42f * Owner.direction;

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.58f, Pitch = -0.65f }, startPoint);
            SpawnArrivalEffects();
        }

        private void SpawnArrivalEffects()
        {
            if (Main.dedServ)
                return;

            // 剑自天顶显形：一圈硫火尘向外炸开，中心凝起一枚熔火团块与硫红辉光。
            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f);
                GaelGreatswordVisuals.SpawnBrimstoneDust(startPoint + Main.rand.NextVector2Circular(22f, 28f),
                    velocity, Main.rand.NextFloat(1f, 1.55f));
            }
            GaelGreatswordVisuals.SpawnBrimstoneMetaball(startPoint, Vector2.Zero, 26f, 0.85f);
            GeneralParticleHandler.SpawnParticle(new BloomParticle(startPoint, Vector2.Zero, BrimBody * 0.5f, 0.05f, 0.5f, 16, false));
        }

        private void EmitDescentEffects(float descentProgress)
        {
            if (Main.dedServ)
                return;

            // 坠落如一颗硫火陨星：剑身两侧甩出硫火尘，尾迹烧进流体火焰场，剑尖曳出烬火。
            Vector2 bladeDirection = currentAngle.ToRotationVector2();
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = Owner.MountedCenter + bladeDirection * Main.rand.NextFloat(30f, BladeReach * 0.9f) * scale +
                    Main.rand.NextVector2Circular(14f, 6f);
                Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 5f) * (0.4f + descentProgress) + Main.rand.NextVector2Circular(1.5f, 0.5f);
                GaelGreatswordVisuals.SpawnBrimstoneDust(position, velocity, Main.rand.NextFloat(1f, 1.5f) * scale);
            }

            Vector2 trailPos = Owner.MountedCenter + bladeDirection * BladeReach * scale * Main.rand.NextFloat(0.5f, 0.95f);
            GaelGreatswordVisuals.RegisterBrimstoneFire(trailPos, -Vector2.UnitY * (2f + descentProgress * 4f), 0.35f + descentProgress * 0.35f, 0.3f);
            if (Main.rand.NextBool(2))
                GaelGreatswordVisuals.SpawnBrimstoneMetaball(trailPos, -Vector2.UnitY * Main.rand.NextFloat(1f, 3f),
                    Main.rand.NextFloat(12f, 20f) * scale, 0.75f);

            if (Main.rand.NextBool(2))
            {
                Vector2 tipPosition = Owner.MountedCenter + bladeDirection * BladeReach * scale * Main.rand.NextFloat(0.85f, 1f);
                GeneralParticleHandler.SpawnParticle(new CritSpark(tipPosition,
                    -Vector2.UnitY * Main.rand.NextFloat(2f, 5f) + Main.rand.NextVector2Circular(1.5f, 1.5f),
                    Color.White, Main.rand.NextBool() ? GaelGreatswordVisuals.EmberGold : BloodRed,
                    Main.rand.NextFloat(0.35f, 0.7f), Main.rand.Next(8, 14)));
            }
        }

        private void SpawnImpactEffects()
        {
            if (Main.dedServ)
                return;

            // 硫火尘埃冲击环。
            for (int i = 0; i < 38; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 10f);
                GaelGreatswordVisuals.SpawnBrimstoneDust(targetPoint + Main.rand.NextVector2Circular(28f, 24f),
                    velocity, Main.rand.NextFloat(1f, 1.9f));
            }

            // 熔火坑：沿地面铺开一排硫火元球，像剑砸出的熔岩坑口。
            for (int i = 0; i < 7; i++)
            {
                float side = MathHelper.Lerp(-1f, 1f, i / 6f);
                Vector2 blobPos = targetPoint + new Vector2(side * Main.rand.NextFloat(20f, 74f), Main.rand.NextFloat(-6f, 10f));
                GaelGreatswordVisuals.SpawnBrimstoneMetaball(blobPos,
                    new Vector2(side * Main.rand.NextFloat(1f, 4f), -Main.rand.NextFloat(0.5f, 2.5f)),
                    Main.rand.NextFloat(22f, 40f), 0.85f);
            }

            // 黑烟柱：落点腾起几股硫火黑烟，撑起坠击的体量。
            for (int i = 0; i < 5; i++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    targetPoint + Main.rand.NextVector2Circular(40f, 14f),
                    new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), -Main.rand.NextFloat(2f, 5f)),
                    Color.Lerp(GaelGreatswordVisuals.VoidSmoke, BloodRed, Main.rand.NextFloat(0.2f, 0.5f)),
                    Main.rand.Next(34, 52), Main.rand.NextFloat(0.7f, 1.15f), 0.6f, Main.rand.NextFloat(-0.04f, 0.04f), true));
            }

            // 双冲击环：竖向喷发的硫红柱环 + 贴地摊开的灼白宽环。
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(targetPoint, -Vector2.UnitY * 4f,
                BloodRed, new Vector2(1.2f, 2.6f), -MathHelper.PiOver2, 0.22f, 0.04f, 28));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(targetPoint, Vector2.Zero,
                GaelGreatswordVisuals.EmberGold, new Vector2(2.2f, 0.7f), 0f, 0.16f, 0.05f, 24));

            // 落点冲击补强：白炽核心强光 + 硫红辉光 + 沿地面两侧喷溅的火花与闪星，末尾喷一大口流体火。
            GeneralParticleHandler.SpawnParticle(new StrongBloom(targetPoint, Vector2.Zero, PaleCore * 0.75f, 1.1f, 16));
            GeneralParticleHandler.SpawnParticle(new BloomParticle(targetPoint, Vector2.Zero, BloodRed * 0.5f, 0.06f, 1.1f, 20, false));
            for (int i = 0; i < 14; i++)
            {
                float side = Main.rand.NextBool() ? 1f : -1f;
                Vector2 velocity = new Vector2(side * Main.rand.NextFloat(2f, 9f), -Main.rand.NextFloat(2f, 7.5f));
                GeneralParticleHandler.SpawnParticle(new CritSpark(targetPoint + new Vector2(side * Main.rand.NextFloat(0f, 46f), Main.rand.NextFloat(-10f, 6f)),
                    velocity, Color.White, Main.rand.NextBool() ? BloodRed : GaelGreatswordVisuals.EmberGold,
                    Main.rand.NextFloat(0.45f, 0.9f), Main.rand.Next(11, 19)));
            }

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(targetPoint + Main.rand.NextVector2Circular(52f, 22f),
                    -Vector2.UnitY * Main.rand.NextFloat(1f, 3.5f), Color.White, GaelGreatswordVisuals.EmberGold,
                    Main.rand.NextFloat(0.5f, 0.85f), Main.rand.Next(12, 18), Main.rand.NextFloat(-0.12f, 0.12f), 2.4f));
            }
            for (int i = 0; i < 6; i++)
                GaelGreatswordVisuals.RegisterBrimstoneFire(targetPoint + new Vector2(Main.rand.NextFloat(-60f, 60f), Main.rand.NextFloat(-8f, 8f)),
                    new Vector2(Main.rand.NextFloat(-2f, 2f), -Main.rand.NextFloat(1f, 4f)), 0.9f, 0.35f);

            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), targetPoint, Vector2.Zero,
                ModContent.ProjectileType<GaelGreatswordBloodEcho>(), Math.Max(1, (int)(Projectile.damage * 0.52f)),
                Projectile.knockBack, Projectile.owner, 0.85f);

            for (int i = 0; i < 6; i++)
            {
                float angle = -MathHelper.PiOver2 + MathHelper.Lerp(-1.25f, 1.25f, i / 5f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(8f, 13f);
                int soulType = i % 2 == 0 ? ModContent.ProjectileType<GaelGreatswordDarkSoul>() : ModContent.ProjectileType<GaelGreatswordVengefulSoul>();
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), targetPoint + Main.rand.NextVector2Circular(36f, 24f),
                    velocity, soulType, Math.Max(1, (int)(Projectile.damage * 0.28f)), 1f, Projectile.owner);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D swordTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;
            Vector2 origin = new(0f, swordTexture.Height);
            float drawRotation = currentAngle + MathHelper.PiOver4;
            Vector2 drawPosition = Owner.MountedCenter - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            float opacity = Utils.GetLerpValue(0f, 6f, timer, true) * Utils.GetLerpValue(Duration, Duration - 8f, timer, true);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            // VerticalSmearLarge 是弧顶朝上的拱形弧光（rotation=0 时弧顶指向 -PiOver2），
            // 加 PiOver2 让弧顶对准剑尖坠落方向，作为下坠的破空前缘。
            Main.EntitySpriteDraw(smear, drawPosition, null, BloodRed with { A = 0 } * opacity * 0.78f,
                currentAngle + MathHelper.PiOver2, smear.Size() * 0.5f, scale * 1.15f, SpriteEffects.None);
            Vector2 tip = Owner.MountedCenter + currentAngle.ToRotationVector2() * BladeReach * scale - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, tip, null, Color.White with { A = 0 } * opacity * 0.5f,
                0f, bloom.Size() * 0.5f, scale * 0.56f, SpriteEffects.None);
            if (impactEffectsPlayed)
            {
                float ringOpacity = Utils.GetLerpValue(Duration, Duration - 14f, timer, true);
                Main.EntitySpriteDraw(ring, targetPoint - Main.screenPosition, null, PaleCore with { A = 0 } * ringOpacity * 0.72f,
                    timer * 0.08f, ring.Size() * 0.5f, ImpactRadius / ring.Width * 2f, SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(swordTexture, drawPosition, null, lightColor, drawRotation, origin, scale, SpriteEffects.None);
            return false;
        }
    }
}
