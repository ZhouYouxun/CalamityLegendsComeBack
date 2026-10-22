using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidRightClickHoldout : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        public Player Owner => Main.player[Projectile.owner];

        private Vector2 AimPosition => new Vector2(Projectile.ai[1], Projectile.ai[2]);
        private Vector2 AimDirection => (AimPosition - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);
        private int syncTimer;

        // Charge and cursor position are synchronized for remote players.
        public float ChargeProgress
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = MathHelper.Clamp(value, 0f, 100f);
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed || Owner.HeldItem.type != ModContent.ItemType<LeonidProgenitor>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 aimPoint = global::CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip.CtrlChipPlayer.GetFireAimWorld(Owner, Main.MouseWorld);
                Projectile.ai[1] = aimPoint.X;
                Projectile.ai[2] = aimPoint.Y;
                if (!Owner.controlUseTile)
                {
                    LaunchMeteor();
                    Projectile.Kill();
                    return;
                }
                if (++syncTimer >= 6)
                {
                    syncTimer = 0;
                    Projectile.netUpdate = true;
                }
            }

            // Align owner and set held projection
            Owner.ChangeDir(AimDirection.X >= 0f ? 1 : -1);
            Owner.itemRotation = AimDirection.ToRotation();
            if (Owner.direction == -1)
                Owner.itemRotation += MathHelper.Pi;

            Owner.itemTime = Owner.itemAnimation = 5;
            Owner.heldProj = Projectile.whoAmI;

            // Positioning in front of the player
            Vector2 aimDir = AimDirection;
            Projectile.Center = Owner.MountedCenter + aimDir * 28f;

            // Charging no longer reduces horizontal movement speed.
            if (Owner.velocity.Y * Owner.gravDir < 0f)
                Owner.velocity.Y *= 0.9f;

            // Increment charge progress naturally
            if (Projectile.owner == Main.myPlayer)
                ChargeProgress += 1.4f;

            // Search and recycle active reflective comets
            int reflectiveType = ModContent.ProjectileType<LeonidReflectiveMeteor>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == reflectiveType && p.ModProjectile is LeonidReflectiveMeteor reflective)
                {
                    if (Projectile.owner == Main.myPlayer && p.owner == Projectile.owner && Vector2.Distance(Projectile.Center, p.Center) < 1200f)
                    {
                        // Put it into returning state
                        if (!reflective.IsReturning)
                        {
                            reflective.IsReturning = true;
                            p.netUpdate = true;
                        }

                        // Merge if it gets very close
                        if (Vector2.Distance(Projectile.Center, p.Center) < 36f)
                        {
                            // Add 4% progress instantly
                            ChargeProgress += 4f;
                            Projectile.netUpdate = true;

                            // Spawn merge sparks
                            for (int j = 0; j < 6; j++)
                            {
                                Dust d = Dust.NewDustPerfect(
                                    p.Center,
                                    DustID.TintableDustLighted,
                                    Main.rand.NextVector2Circular(2f, 2f),
                                    100,
                                    Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.MoonWhite, 0.35f),
                                    Main.rand.NextFloat(0.7f, 1f));
                                d.noGravity = true;
                            }

                            // 每吞下一颗反射陨星，都从核心迸出一小把星光，
                            // 让"回收 → 增幅"这条链路在画面上有实感。
                            LeonidStarlight.Burst(
                                Projectile.Center,
                                4,
                                Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.MoonWhite, 0.4f),
                                LeonidStarlightShape.Mote,
                                speed: 4.5f,
                                scale: 0.65f,
                                hoverTime: 18,
                                lifetime: 110,
                                lanceSpeed: 15f);

                            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.35f, Pitch = 0.5f }, Projectile.Center);
                            p.Kill();
                        }
                    }
                }
            }

            // Charge fully sound and visual trigger
            if (ChargeProgress >= 100f && Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f; // ready flag
                SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.85f, Pitch = 0.1f }, Projectile.Center);
                for (int i = 0; i < 20; i++)
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.TintableDustLighted,
                        Main.rand.NextVector2Circular(4f, 4f),
                        100,
                        LeonidVisualUtils.GetReadyGold(Projectile.whoAmI * 0.11f),
                        Main.rand.NextFloat(1f, 1.4f));
                    d.noGravity = true;
                }

                // 蓄满：一圈金色星辉同步张开，仅一颗找目标，其余旋转外扩。
                LeonidStarlight.Ring(Projectile.Center, 12, 34f, LeonidVisualUtils.GetReadyGold(),
                    LeonidStarlightShape.Shard, speed: 6.5f, scale: 1f, hoverTime: 26, lifetime: 170, lanceSpeed: 19f);
                LeonidStarlight.Spawn(Projectile.Center, Vector2.Zero, LeonidVisualUtils.StarGold,
                    LeonidStarlightShape.Halo, 1.2f, 12, 80, 12f, linksToSiblings: false);
            }

            // 蓄力期间持续吸入星屑：星光在核心外围绕着悬停，越接近满蓄越密。
            if (ChargeProgress < 100f && Main.rand.NextBool(ChargeProgress > 60f ? 4 : 8))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 spawnPosition = Projectile.Center + angle.ToRotationVector2() * Main.rand.NextFloat(58f, 96f);
                LeonidStarlight.Spawn(
                    spawnPosition,
                    (Projectile.Center - spawnPosition).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.6f, 3.2f),
                    LeonidVisualUtils.GetCelestialColor(ChargeProgress / 100f, Main.rand.NextFloat(6f)),
                    LeonidStarlightShape.Mote,
                    0.55f,
                    hoverTime: 34,
                    lifetime: 120,
                    lanceSpeed: 14f);
            }
        }

        private void LaunchMeteor()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            LeonidConstellationPlayer constellation = Owner.GetModPlayer<LeonidConstellationPlayer>();
            float speed = 20f;
            Vector2 launchVel = (AimPosition - Projectile.Center).SafeNormalize(Vector2.UnitX * Owner.direction) * speed;
            float prog = ChargeProgress / 100f;
            
            // Charge scales damage linearly from 60% to 100%; Chertan removes the uncharged penalty.
            float chargeDamageFactor = constellation.IsUnlocked(LeonidStar.Chertan) ? 1f : 0.6f + 0.4f * prog;
            int damage = (int)(Projectile.damage * chargeDamageFactor);
            if (constellation.IsUnlocked(LeonidStar.Dingolay) && !Main.dayTime)
                damage = (int)(damage * 1.1f);
            
            int largeCometType = ModContent.ProjectileType<LeonidCometLarge>();
            int p = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                launchVel,
                largeCometType,
                damage,
                Projectile.knockBack * (0.8f + 0.4f * prog),
                Projectile.owner,
                0f,
                0f,
                prog); // pass progress as ai[2] for scale

            if (p.WithinBounds(Main.maxProjectiles))
            {
                Main.projectile[p].Calamity().stealthStrike = true;
                Main.projectile[p].netUpdate = true;
            }

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.9f, Pitch = -0.15f }, Projectile.Center);

            // 出膛：星光沿发射轴向前泼出去，蓄得越满泼得越多越快。
            LeonidStarlight.Spray(
                Projectile.Center,
                launchVel,
                5 + (int)(prog * 6f),
                LeonidVisualUtils.GetCelestialColor(prog),
                LeonidStarlightShape.Needle,
                speed: 8f + prog * 5f,
                spread: 0.55f,
                scale: 0.9f + prog * 0.4f,
                hoverTime: 18,
                lifetime: 140,
                lanceSpeed: 18f);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer != Projectile.owner)
                return;
            foreach (Projectile meteor in Main.ActiveProjectiles)
                if (meteor.owner == Projectile.owner && meteor.ModProjectile is LeonidReflectiveMeteor reflective
                    && reflective.IsReturning)
                    reflective.StopReturning();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float prog = ChargeProgress / 100f;
            float size = MathHelper.Lerp(0.35f, 1.25f, prog);

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D sparkle = ModContent.Request<Texture2D>("CalamityMod/Particles/Sparkle").Value;
            Color coreColor = Color.Lerp(LeonidVisualUtils.GetCelestialColor(prog, Projectile.whoAmI * 0.09f), LeonidVisualUtils.GetReadyGold(), prog * 0.24f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 aimDir = AimDirection;

            // The charge core is a pure light effect and must not render BloomCircle's black base.
            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            LeonidVisualUtils.DrawGlowBlade(Projectile.Center - aimDir * 4f, aimDir, coreColor, 0.42f + prog * 0.22f, size * 0.12f, size * 0.03f);
            Main.EntitySpriteDraw(bloom, drawPos, null, coreColor * 0.4f, 0f, bloom.Size() * 0.5f, size * 0.65f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, drawPos, null, Color.White * 0.3f, 0f, bloom.Size() * 0.5f, size * 0.35f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ring, drawPos, null, coreColor * (0.38f + prog * 0.24f), Main.GlobalTimeWrappedHourly * 2.8f, ring.Size() * 0.5f, size * 0.34f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(sparkle, drawPos, null, Color.White * (0.24f + prog * 0.24f), -Main.GlobalTimeWrappedHourly * 3.4f, sparkle.Size() * 0.5f, size * 0.42f, SpriteEffects.None, 0f);
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();

            // Orbiting stars spiraling in
            int orbitCount = 4;
            for (int i = 0; i < orbitCount; i++)
            {
                float angleOffset = i * MathHelper.TwoPi / orbitCount;
                float time = Main.GlobalTimeWrappedHourly * 6.5f + angleOffset;
                // Spiral radius oscillates and decreases as they converge
                float radius = MathHelper.Lerp(48f, 10f, (float)Math.Sin(time * 0.6f) * 0.5f + 0.5f);
                Vector2 orbitOffset = time.ToRotationVector2() * radius;
                
                Main.EntitySpriteDraw(sparkle, drawPos + orbitOffset, null, Color.Lerp(coreColor, Color.White, 0.32f) * 0.72f, -time, sparkle.Size() * 0.5f, size * 0.24f, SpriteEffects.None, 0f);
            }

            // 蓄力刻度环：外环随进度收紧，满蓄时并入核心并转为金色；
            // 核心星芒的大小和亮度同样跟着进度走，不看数字也读得出蓄了多少。
            LeonidStarlight.DrawReticle(
                Projectile.Center,
                Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.StarGold, prog),
                0.22f + 0.4f * prog,
                MathHelper.Lerp(0.4f, 0.15f, prog),
                Main.GlobalTimeWrappedHourly * (0.8f + prog * 1.6f));

            LeonidStarlight.DrawFlare(
                Projectile.Center,
                Color.Lerp(coreColor, LeonidVisualUtils.MoonWhite, 0.45f),
                0.45f + 0.45f * prog,
                size * 0.14f,
                -Main.GlobalTimeWrappedHourly * 1.2f);

            LeonidStarlight.DrawOrbitingStars(
                Projectile.Center,
                Color.Lerp(coreColor, LeonidVisualUtils.StarGold, prog * 0.5f),
                0.3f + 0.35f * prog,
                size * 0.035f,
                MathHelper.Lerp(52f, 22f, prog),
                5,
                1.9f);

            if (prog >= 1f)
            {
                // 满蓄的额外奖励：一枚缓慢自转的放射耀斑，宣告"可以放了"。
                LeonidStarlight.DrawSunburst(
                    Projectile.Center,
                    LeonidVisualUtils.GetReadyGold(),
                    0.3f,
                    0.05f,
                    Main.GlobalTimeWrappedHourly * 0.4f);
            }

            return false;
        }
    }
}
