using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 天理真和左键第三段召唤的巨剑：高空锁定、急坠、落地爆雷。
    internal sealed class AzureThunderGrandSword : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/AzureThunder";

        // ai[0] 锁定目标，ai[1]/ai[2] 保存兜底落点。
        private int TargetIndex => (int)Projectile.ai[0];
        private Vector2 StoredImpactPosition => new(Projectile.ai[1], Projectile.ai[2]);

        // 三段状态：预备悬停、下坠、爆炸。
        private Vector2 impactPosition;
        private int timer;
        private bool dashing;
        private bool exploding;

        // 巨剑下坠调校参数，决定前摇时长和落下速度曲线。
        private const int DropAnticipationFrames = 16;
        private const float InitialDropSpeed = 92f;
        private const float DropAcceleration = 12.5f;
        private const float MaxDropSpeed = 176f;

        public override void SetStaticDefaults()
        {
            // 残影缓存用于绘制巨剑下坠拖影。
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            // 巨剑预备期不伤害，下坠和爆炸阶段才打开判定。
            Projectile.width = 96;
            Projectile.height = 128;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 210;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.scale = 1.65f;
        }

        public override bool? CanDamage() => dashing || exploding;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // 爆炸阶段使用大圆形命中盒。
            if (exploding)
                return CalamityUtils.CircularHitboxCollision(Projectile.Center, 170f * Projectile.scale, targetHitbox);

            // 下坠阶段用剑身线段碰撞，贴合巨剑纵向形状。
            float collisionPoint = float.NaN;
            Vector2 bladeDirection = Vector2.UnitY;
            Vector2 start = Projectile.Center - bladeDirection * 42f * Projectile.scale;
            Vector2 end = Projectile.Center + bladeDirection * 145f * Projectile.scale;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 42f * Projectile.scale, ref collisionPoint);
        }

        public override void AI()
        {
            timer++;

            // impactPosition 首帧解析，之后根据目标存活情况持续校正。
            if (impactPosition == Vector2.Zero)
                impactPosition = ResolveImpactPosition();

            if (!dashing && !exploding)
            {
                // 预备阶段停在落点上方，逐渐放大并生成蓄势粒子。
                impactPosition = ResolveImpactPosition();
                Vector2 hoverPosition = impactPosition - Vector2.UnitY * 780f;
                Projectile.Center = Vector2.Lerp(Projectile.Center, hoverPosition, 0.28f);
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4;
                Projectile.scale = MathHelper.Lerp(Projectile.scale, 1.75f, 0.045f);
                SpawnChargeVisuals();

                if (timer >= DropAnticipationFrames)
                    BeginDrop();

                return;
            }

            if (dashing)
            {
                // 重坠调校：起步很快，再每帧增加 12.5 像素速度，读起来像处决式落剑。
                impactPosition = ResolveImpactPosition();
                float lateralCorrection = (impactPosition.X - Projectile.Center.X) * 0.08f;
                float trailSway = (float)Math.Sin(timer * 0.55f + Projectile.identity) * 2.2f;
                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, lateralCorrection + trailSway, 0.25f);
                Projectile.velocity.Y = Math.Min(MaxDropSpeed, Projectile.velocity.Y + DropAcceleration);
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
                SpawnFallingVisuals();

                if (Projectile.Distance(impactPosition) < 46f)
                    BeginExplosion(impactPosition);
                else if (timer >= 34)
                    // 最长下坠时间兜底，避免追不上高速目标而永远不爆。
                    BeginExplosion(Projectile.Center);

                return;
            }

            if (exploding)
            {
                // 爆炸阶段扩大贴图并淡出，同时持续喷出粒子。
                Projectile.velocity = Vector2.Zero;
                Projectile.scale = MathHelper.Lerp(Projectile.scale, 2.35f, 0.18f);
                Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 0f, 0.16f);
                SpawnExplosionVisuals();
                if (timer > 18)
                    Projectile.Kill();
            }
        }

        private Vector2 ResolveImpactPosition()
        {
            // 目标仍有效时持续追踪中心，否则使用创建时记录的落点。
            if (TargetIndex >= 0 && Main.npc.IndexInRange(TargetIndex))
            {
                NPC target = Main.npc[TargetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                    return target.Center;
            }

            return StoredImpactPosition == Vector2.Zero ? Projectile.Center + Vector2.UnitY * 560f : StoredImpactPosition;
        }

        private void BeginDrop()
        {
            // 切入下坠状态并把 X 对齐落点，保证巨剑是从正上方砸下。
            dashing = true;
            timer = 0;
            impactPosition = ResolveImpactPosition();
            Projectile.Center = new Vector2(impactPosition.X, Projectile.Center.Y);
            Projectile.velocity = Vector2.UnitY * InitialDropSpeed;
            Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4;
            Projectile.friendly = true;
            AzureThunderSounds.PlayHeavyDrop(Projectile.Center);
        }

        private void BeginExplosion(Vector2 explosionCenter)
        {
            // 下坠结束后切成爆炸状态，扩大局部无敌间隔以允许 AOE 命中。
            dashing = false;
            exploding = true;
            timer = 0;
            Projectile.Center = explosionCenter;
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = true;
            Projectile.localNPCHitCooldown = 5;

            if (Main.myPlayer == Projectile.owner)
            {
                // 落地爆炸由拥有者生成一圈平雷，避免多人重复生成伤害弹幕。
                int flags = AzureThunderFlatLightning.StaticDischargeFlag | AzureThunderFlatLightning.BigLightningFlag;
                for (int i = 0; i < 10; i++)
                {
                    Vector2 direction = (MathHelper.TwoPi * i / 10f).ToRotationVector2();
                    Vector2 spawnPosition = Projectile.Center - direction * Main.rand.NextFloat(90f, 180f);
                    AzureThunderPlayer.SpawnFlatLightning(
                        Projectile.GetSource_FromThis(),
                        spawnPosition,
                        Projectile.Center - spawnPosition,
                        Math.Max(1, (int)(Projectile.damage * 0.28f)),
                        Projectile.knockBack,
                        Projectile.owner,
                        i % 3 == 0 ? 1.35f : 0.95f,
                        flags);
                }
            }

            AzureThunderSounds.PlayHeavyImpact(Projectile.Center);
        }

        private void SpawnChargeVisuals()
        {
            // 预备悬停时的聚能粒子。
            if (!Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(52f, 52f),
                DustID.FireworksRGB,
                -Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1f, 4f) + Main.rand.NextVector2Circular(1.5f, 1.5f),
                0,
                Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                Main.rand.NextFloat(0.9f, 1.4f));
            dust.noGravity = true;
        }

        private void SpawnFallingVisuals()
        {
            // 下坠阶段沿反方向拉出尘埃、火花和线粒子。
            Vector2 upwardTrail = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * Main.rand.NextFloat(0.3f, 0.75f) + Main.rand.NextVector2Circular(42f, 42f),
                    DustID.FireworksRGB,
                    -Projectile.velocity * Main.rand.NextFloat(0.02f, 0.08f),
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.Yellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(1f, 1.6f));
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(38f, 38f),
                    upwardTrail * Main.rand.NextFloat(4f, 9f) + Main.rand.NextVector2Circular(1.2f, 1.2f),
                    false,
                    Main.rand.Next(14, 21),
                    Main.rand.NextFloat(0.045f, 0.075f),
                    Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    new Vector2(2.2f, 0.46f),
                    true,
                    true,
                    0.9f));
            }

            GeneralParticleHandler.SpawnParticle(new LineParticle(
                Projectile.Center - Projectile.velocity * Main.rand.NextFloat(0.12f, 0.35f),
                upwardTrail * Main.rand.NextFloat(2.5f, 5.5f),
                false,
                Main.rand.Next(12, 18),
                Main.rand.NextFloat(0.55f, 0.9f),
                Main.rand.NextBool(3) ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure));
        }

        private void SpawnExplosionVisuals()
        {
            // 爆炸阶段的外扩青金尘埃。
            if (!Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(120f, 120f),
                DustID.FireworksRGB,
                Main.rand.NextVector2Circular(7f, 7f),
                0,
                Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                Main.rand.NextFloat(1.1f, 1.8f));
            dust.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 巨剑命中统一附加电击、饰品效果和终极 DoT。
            target.AddBuff(BuffID.Electrified, 240);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
            AzureThunderPlayer.ApplyUltimateDot(target, 240);

            // 下坠过程中穿透数用完时提前在目标处爆开。
            if (dashing && !exploding && Projectile.numHits >= 5)
                BeginExplosion(target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 先画旧位置拖影，再画描边和主体。
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                // 下坠时拖影更亮，预备/爆炸时只保留淡影。
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                if (oldCenter == Projectile.Size * 0.5f)
                    continue;

                float opacity = (1f - i / (float)Projectile.oldPos.Length) * (dashing ? 0.34f : 0.12f);
                Color trailColor = Color.Lerp(AzureThunderColors.Azure, AzureThunderColors.PaleYellow, i / (float)Projectile.oldPos.Length) with { A = 0 };
                Main.EntitySpriteDraw(texture, oldCenter - Main.screenPosition, null, trailColor * opacity, Projectile.rotation, origin, Projectile.scale * (1f - i * 0.018f), SpriteEffects.None);
            }

            // 爆炸时描边半径扩大，表现落地冲击波。
            HoldoutOutlineHelper.DrawSolidOutline(
                texture,
                drawPosition,
                Projectile.rotation,
                origin,
                Vector2.One * Projectile.scale,
                SpriteEffects.None,
                AzureThunderColors.Yellow,
                exploding ? 12f : 7f,
                exploding ? 0.28f : 0.36f,
                Main.GlobalTimeWrappedHourly + Projectile.identity * 0.15f,
                exploding ? 26 : 18);

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
