using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Dusts;
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
    internal sealed class GaelGreatswordCapeFinisher : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword";

        private const int Duration = 210;
        private const float CapeRadius = 150f;

        private static readonly Color SoulPurple = GaelGreatswordVisuals.CrimsonViolet;
        private static readonly Color BloodRed = GaelGreatswordVisuals.BrimstoneRed;

        private Player Owner => Main.player[Projectile.owner];
        private int timer;
        private float spinRotation;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = (int)(CapeRadius * 2f);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 11;
            Projectile.timeLeft = 4;
            Projectile.noEnchantmentVisuals = true;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            timer++;
            Projectile.Center = Owner.Center;
            Projectile.timeLeft = 4;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 1.4f);

            float spinSpeed = MathHelper.Lerp(0.21f, 0.42f, Utils.GetLerpValue(0f, 60f, timer, true));
            spinRotation += spinSpeed;
            Projectile.rotation = spinRotation;

            EmitCapeParticles();
            FireDarkSouls();

            if (timer == Duration - 12)
                ReleaseFinaleBurst();

            if (timer >= Duration)
                Projectile.Kill();
        }

        private void ReleaseFinaleBurst()
        {
            // 斗篷旋至终点并不悄然散去，而是把积攒的血与火一次性掀出去。
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 9f);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.35f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.65f, Pitch = -0.4f }, Projectile.Center);

            if (!Main.dedServ)
            {
                // 灼白核心 + 双冲击环（硫红外扩 + 烬金内爆）。
                GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, GaelGreatswordVisuals.WhiteHot * 0.85f, 1.6f, 18));
                GeneralParticleHandler.SpawnParticle(new BloomParticle(Projectile.Center, Vector2.Zero, BloodRed * 0.5f, 0.1f, 1.7f, 22, false));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero,
                    BloodRed, new Vector2(1f, 1f), 0f, 0.3f, 1.4f, 26));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero,
                    GaelGreatswordVisuals.EmberGold, new Vector2(1f, 1f), 0f, 0.2f, 0.95f, 20));

                // 熔火崩解 + 黑烟环：一圈硫火元球与重烟同时炸开，再喷一大口流体火。
                for (int i = 0; i < 12; i++)
                {
                    Vector2 dir = (MathHelper.TwoPi * i / 12f).ToRotationVector2();
                    GaelGreatswordVisuals.SpawnBrimstoneMetaball(Projectile.Center + dir * 30f, dir * Main.rand.NextFloat(4f, 9f),
                        Main.rand.NextFloat(26f, 42f), 0.85f);
                    GaelGreatswordVisuals.RegisterBrimstoneFire(Projectile.Center + dir * 40f, dir * 3f, 1f, 0.35f);
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(Projectile.Center + dir * 24f, dir * Main.rand.NextFloat(2f, 5f),
                        Color.Lerp(GaelGreatswordVisuals.VoidSmoke, BloodRed, 0.3f), Main.rand.Next(32, 48),
                        Main.rand.NextFloat(0.6f, 1f), 0.6f, Main.rand.NextFloat(-0.05f, 0.05f), true));
                }

                for (int i = 0; i < 24; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / 24f).ToRotationVector2().RotatedByRandom(0.16f) * Main.rand.NextFloat(4f, 12f);
                    GeneralParticleHandler.SpawnParticle(new CritSpark(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f),
                        velocity, Color.White, Main.rand.NextBool() ? BloodRed : GaelGreatswordVisuals.EmberGold,
                        Main.rand.NextFloat(0.45f, 0.95f), Main.rand.Next(12, 20)));
                }
            }

            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<GaelGreatswordBloodEcho>(), Math.Max(1, (int)(Projectile.damage * 1.45f)),
                Projectile.knockBack + 3f, Projectile.owner, 1f);

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = (spinRotation + MathHelper.TwoPi * i / 8f).ToRotationVector2() * Main.rand.NextFloat(9f, 12f);
                int soulType = i % 2 == 0
                    ? ModContent.ProjectileType<GaelGreatswordDarkSoul>()
                    : ModContent.ProjectileType<GaelGreatswordVengefulSoul>();
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + velocity.SafeNormalize(Vector2.UnitY) * 40f,
                    velocity, soulType, Math.Max(1, (int)(Projectile.damage * 0.5f)), 1.5f, Projectile.owner);
            }
        }

        public override bool? CanDamage() => timer >= 12 && timer <= Duration - 10 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = CapeRadius + MathF.Sin(timer * 0.11f) * 22f;
            Vector2 closest = targetHitbox.ClosestPointInRect(Projectile.Center);
            return closest.Distance(Projectile.Center) <= radius ? null : false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.42f, MathHelper.Clamp(Projectile.numHits / 12f, 0f, 1f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != Projectile.owner || Projectile.numHits % 4 != 0)
                return;

            int echoDamage = Math.Max(1, (int)(Projectile.damage * 0.32f));
            Projectile.NewProjectile(Projectile.GetSource_OnHit(target), target.Center, Vector2.Zero,
                ModContent.ProjectileType<GaelGreatswordBloodEcho>(), echoDamage, 1.5f, Projectile.owner, 1f);
        }

        private void FireDarkSouls()
        {
            int interval = Math.Max(4, 8 - GaelGreatswordProgression.GetStage());
            if (Main.myPlayer != Projectile.owner || timer % interval != 0)
                return;

            NPC target = FindTarget();
            Vector2 forward = target != null
                ? Projectile.Center.DirectionTo(target.Center).SafeNormalize(Vector2.UnitY)
                : (spinRotation + Main.rand.NextFloat(-0.7f, 0.7f)).ToRotationVector2();

            Vector2 spawnPosition = Projectile.Center - forward * Main.rand.NextFloat(80f, 140f) + Main.rand.NextVector2Circular(50f, 50f);
            Vector2 velocity = forward.RotatedBy(Main.rand.NextFloat(-0.35f, 0.35f)) * Main.rand.NextFloat(8f, 13f);
            int damage = Math.Max(1, (int)(Projectile.damage * 0.55f));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition, velocity,
                ModContent.ProjectileType<GaelGreatswordDarkSoul>(), damage, 1.2f, Projectile.owner, target?.whoAmI ?? -1);
        }

        private NPC FindTarget()
        {
            NPC closest = null;
            float closestDistance = 1200f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = npc.Distance(Projectile.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        private void EmitCapeParticles()
        {
            if (Main.dedServ)
                return;

            if (timer % 2 == 0)
            {
                Vector2 edge = Projectile.Center + spinRotation.ToRotationVector2().RotatedBy(Main.rand.NextFloat(-1.3f, 1.3f)) * Main.rand.NextFloat(80f, CapeRadius);
                Vector2 velocity = edge.DirectionFrom(Projectile.Center).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(2f, 5f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(edge, velocity, false,
                    Main.rand.Next(14, 24), Main.rand.NextFloat(0.45f, 0.78f), Main.rand.NextBool(3) ? BloodRed : SoulPurple));
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(CapeRadius, CapeRadius),
                    (int)CalamityDusts.Brimstone, Main.rand.NextVector2Circular(2f, 2f), 120, SoulPurple, Main.rand.NextFloat(1f, 1.55f));
                dust.noGravity = true;
            }

            // 硫火火旋：斗篷边缘每隔几帧甩出一枚绕转的熔火元球并烧进流体场，
            // 整套大招转成一圈流动的硫火结界，与至尊灾厄战场同源。
            if (timer % 3 == 0)
            {
                float ringAngle = spinRotation * 1.4f + timer * 0.2f;
                Vector2 ringPos = Projectile.Center + ringAngle.ToRotationVector2() * Main.rand.NextFloat(CapeRadius * 0.6f, CapeRadius);
                Vector2 tangent = ringAngle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 3f;
                GaelGreatswordVisuals.SpawnBrimstoneMetaball(ringPos, tangent, Main.rand.NextFloat(18f, 30f), 0.82f);
                GaelGreatswordVisuals.RegisterBrimstoneFire(ringPos, tangent, 0.5f, 0.3f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            (Asset<Texture2D> capeAsset, int capeVerticalFrames) = GetCapeTexture();
            Texture2D capeTexture = capeAsset.Value;
            // 装备动画表（如披风 40x1120 = 20 帧、翅膀 86x248 = 4 帧）只能取单帧绘制，
            // 直接整张绘制会把整条动画长带画出来。取第 0 帧（站立姿态）。
            Rectangle capeFrame = capeTexture.Frame(1, capeVerticalFrames, 0, 0);
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            Vector2 origin = capeFrame.Size() * 0.5f;
            // 依据实际帧尺寸把斗篷影像归一到约 110 像素长边，再叠加环形缩放，
            // 无论候选贴图哪张命中，视觉体量都一致。
            float normalizeScale = 110f / Math.Max(capeFrame.Width, capeFrame.Height);
            float fadeIn = Utils.GetLerpValue(0f, 18f, timer, true);
            float fadeOut = Utils.GetLerpValue(Duration, Duration - 24f, timer, true);
            float opacity = fadeIn * fadeOut;

            // 普通混合层：披风剪影本体。披风贴图整体偏暗，纯加算混合下黑色像素
            // 几乎不可见，先用常规混合铺出旋转的布面形体。
            for (int i = 0; i < 6; i++)
            {
                float angle = spinRotation + MathHelper.TwoPi * i / 6f;
                Vector2 offset = angle.ToRotationVector2() * 34f;
                Color silhouette = Color.Lerp(i % 2 == 0 ? BloodRed : SoulPurple, Color.White, 0.22f);
                Main.EntitySpriteDraw(capeTexture, center + offset, capeFrame, silhouette * opacity * 0.6f,
                    angle + MathHelper.PiOver2, origin, normalizeScale * (1.25f + i * 0.035f), SpriteEffects.None);
            }

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = 0; i < 6; i++)
            {
                float angle = spinRotation + MathHelper.TwoPi * i / 6f;
                Vector2 offset = angle.ToRotationVector2() * 34f;
                Color color = i % 2 == 0 ? BloodRed : SoulPurple;
                Main.EntitySpriteDraw(capeTexture, center + offset, capeFrame, color with { A = 0 } * opacity * 0.28f,
                    angle + MathHelper.PiOver2, origin, normalizeScale * (1.25f + i * 0.035f), SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, center, null, SoulPurple with { A = 0 } * opacity * 0.46f,
                0f, bloom.Size() * 0.5f, 3.1f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        private static (Asset<Texture2D> Asset, int VerticalFrames) GetCapeTexture()
        {
            // 候选贴图与其纵向帧数：装备表按 tML 玩家动画布局（40x1120 = 20 帧 56px），
            // 翼类装备表为 4 帧，物品贴图与本模组剑图为单帧。
            (string Path, int VerticalFrames)[] candidates =
            {
                ("CalamityMod/Items/Armor/Empyrean/EmpyreanCloak_Back", 20),
                ("CalamityMod/Items/Accessories/SandCloak", 1),
                ("CalamityMod/Items/Accessories/Wings/SilvaWings_Wings", 4),
                ("CalamityMod/Items/Accessories/Wings/TarragonWings_Wings", 4),
                ("CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword", 1),
            };

            foreach ((string path, int verticalFrames) in candidates)
            {
                if (ModContent.RequestIfExists(path, out Asset<Texture2D> asset))
                    return (asset, verticalFrames);
            }

            return (ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword"), 1);
        }
    }
}
