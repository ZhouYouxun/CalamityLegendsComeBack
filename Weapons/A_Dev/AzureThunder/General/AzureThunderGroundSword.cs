using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.General;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 右键和被动生成的地剑：平时插地/环绕，收到命令后可俯冲攻击。
    internal sealed class AzureThunderGroundSword : ModProjectile, ILocalizedModType
    {
        // 全局地剑上限，右键和自动召剑都不能超过这个值。
        public const int MaxGroundSwords = 9;

        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/GroundAzureThunder";

        // initialized 控制首帧锻造演出；diveTarget 只在俯冲模式使用。
        private bool initialized;
        private Vector2 diveTarget;
        private int outlinePulse;

        // 锻造雷的起点高度，沿用旧竖直雷 1000f * 0.58f 的观感。
        private const float ForgingLightningHeight = 580f;

        // 饰品牵引地剑时使用椭圆轨道参数。
        private const float FollowEllipseRadiusX = 92f;
        private const float FollowEllipseRadiusY = 42f;
        private const float FollowOrbitSpeed = 0.045f;

        private const int ArrivalMode = 0;
        private const int ReadyMode = 1;
        private const int DiveMode = 2;
        private const int MaterializeTime = 18;
        private const float ArrivalHeight = 540f;

        // ai[0] 表示行为模式；ai[1]/ai[2] 保存解析后的最终插地点。
        private int Mode
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        // localAI[0] 是当前模式计时器，BeginDive 会重置。
        private int Timer
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private Vector2 LandingPoint => new(Projectile.ai[1], Projectile.ai[2]);
        private bool Arriving => Mode == ArrivalMode;
        private bool Ready => Mode == ReadyMode;
        private bool Diving => Mode == DiveMode;
        public bool IsReadyForCommand => initialized && Ready;

        public override void SetDefaults()
        {
            // 地剑默认不攻击，只有进入俯冲并过前摇后才打开 friendly。
            Projectile.width = 32;
            Projectile.height = 42;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = AzureThunderProgression.GroundSwordLifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.scale = 0.66f;
        }

        // 俯冲前 12 帧是蓄势，不参与伤害。
        public override bool? CanDamage() => Diving && Timer > 12;

        public override void AI()
        {
            if (!initialized)
            {
                // 生成参数表示目的地；先向下解析地面，再把剑移到高空进行显形与坠落。
                initialized = true;
                Vector2 landingPoint = FindGroundPosition(Projectile.Center);
                Projectile.ai[1] = landingPoint.X;
                Projectile.ai[2] = landingPoint.Y;
                Projectile.Center = landingPoint - Vector2.UnitY * ArrivalHeight;
                Mode = ArrivalMode;
                Timer = 0;
                Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4 + MathHelper.ToRadians(Main.rand.NextBool() ? 5f : -5f);
                AzureThunderSounds.PlaySwordMaterialize(Projectile.Center);
                SpawnIntroBurst();
                Projectile.netUpdate = true;
            }

            Timer++;
            if (outlinePulse > 0)
                outlinePulse--;

            if (Arriving)
            {
                DoArrivalAI();
                return;
            }

            if (Diving)
            {
                // 俯冲模式完全交给 DoDiveAI，跳过待机和跟随逻辑。
                DoDiveAI();
                return;
            }

            // 饰品效果可让部分地剑绕玩家运动，否则地剑保持插地不动。
            if (AzureThunderAccessoryPlayer.ShouldGroundSwordFollowPlayer(Projectile, out int followSlot))
                DoFollowOwnerAI(followSlot);
            else
                Projectile.velocity *= 0f;

            Lighting.AddLight(Projectile.Center, AzureThunderColors.Yellow.ToVector3() * 0.28f);

            if (Timer <= 45)
                SpawnChargeVisuals();

            // 即将消失时上浮粒子提示玩家地剑寿命快结束。
            if (Projectile.timeLeft <= 18)
                SpawnDeathChargeVisuals();
        }

        private void DoFollowOwnerAI(int followSlot)
        {
            Player owner = Main.player[Projectile.owner];
            float phase = (float)Main.GameUpdateCount * FollowOrbitSpeed + followSlot * MathHelper.TwoPi / 3f;

            // 千叮万叮会把前三把地剑变成类似钛金碎片的椭圆环绕炮台。
            Vector2 slotOffset = new(
                (float)Math.Cos(phase) * FollowEllipseRadiusX * (owner.direction == 0 ? 1 : owner.direction),
                -82f + (float)Math.Sin(phase) * FollowEllipseRadiusY);
            Vector2 desiredCenter = owner.MountedCenter + slotOffset;
            Projectile.Center = Vector2.Lerp(Projectile.Center, desiredCenter, 0.12f);
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = Projectile.rotation.AngleLerp(MathHelper.PiOver2 + MathHelper.PiOver4, 0.14f);
        }

        public void BeginDive(Vector2 target, int damage, float knockback)
        {
            if (!IsReadyForCommand)
                return;

            // 外部序列器调用此方法，把待机地剑切成一次性俯冲弹幕。
            Mode = DiveMode;
            Timer = 0;
            diveTarget = target;
            Projectile.damage = Math.Max(1, damage);
            Projectile.knockBack = knockback;
            Projectile.friendly = false;
            Projectile.timeLeft = Math.Min(Projectile.timeLeft, 90);
            AzureThunderSounds.PlaySwordLaunch(Projectile.Center);
            Projectile.netUpdate = true;
        }

        private void DoArrivalAI()
        {
            Projectile.friendly = false;
            Vector2 landingPoint = LandingPoint;

            if (Timer <= MaterializeTime)
            {
                // 高空先由透明到实体，位置只做很小的悬浮，不提前下坠。
                Projectile.velocity = Vector2.Zero;
                Projectile.Center = new Vector2(landingPoint.X, landingPoint.Y - ArrivalHeight + (float)Math.Sin((Timer + Projectile.identity) * 0.28f) * 7f);
                SpawnChargeVisuals();
                if (Timer % 6 == 0 && !Main.dedServ)
                    AzureThunderFissionBolt.Burst(Projectile.Center, 42f, 4, 0.38f);
                return;
            }

            // 显形结束后迅速加速下坠；视觉闪电沿途在剑身周围裂开。
            Projectile.velocity.Y = Math.Min(76f, Projectile.velocity.Y + 9.5f);
            Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, (landingPoint.X - Projectile.Center.X) * 0.18f, 0.32f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            if (Timer % 3 == 0 && !Main.dedServ)
                AzureThunderFissionBolt.Burst(Projectile.Center, 58f, 5, 0.46f);

            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(8f, 34f) + Main.rand.NextVector2Circular(10f, 8f),
                    DustID.FireworksRGB,
                    -Projectile.velocity * Main.rand.NextFloat(0.025f, 0.065f),
                    0,
                    Main.rand.NextBool(4) ? Color.White : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.7f, 1.15f));
                dust.noGravity = true;
            }

            if (Projectile.Center.Y < landingPoint.Y)
                return;

            Projectile.Center = landingPoint;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4;
            Mode = ReadyMode;
            Timer = 0;
            PulseLightningOutline();
            SpawnForgingLightning();
            SpawnIntroBurst();
            AzureThunderSounds.PlaySwordBurst(Projectile.Center, true);
            Main.player[Projectile.owner].Calamity().GeneralScreenShakePower = Math.Max(Main.player[Projectile.owner].Calamity().GeneralScreenShakePower, 2.2f);
            Projectile.netUpdate = true;
        }

        public void PulseLightningOutline()
        {
            // 右键序列击发时调用，刷新描边脉冲计时。
            outlinePulse = Math.Max(outlinePulse, 20);
        }

        private void DoDiveAI()
        {
            // 没有目标时向下俯冲，避免零向量导致停住。
            if (diveTarget == Vector2.Zero)
                diveTarget = Projectile.Center + Vector2.UnitY * 320f;

            if (Timer <= 12)
            {
                // 俯冲前摇：转向目标并生成蓄势粒子，但不造成伤害。
                Projectile.friendly = false;
                Projectile.velocity *= 0.8f;
                Projectile.rotation = Projectile.rotation.AngleLerp((diveTarget - Projectile.Center).ToRotation() + MathHelper.PiOver4, 0.18f);
                SpawnChargeVisuals();
                return;
            }

            // 正式俯冲后缓动到 26 像素/帧，接近目标即销毁。
            Projectile.friendly = true;
            Vector2 desiredVelocity = (diveTarget - Projectile.Center).SafeNormalize(Vector2.UnitY) * 26f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.22f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            if (Projectile.Distance(diveTarget) < 32f)
                Projectile.Kill();
        }

        private static Vector2 FindGroundPosition(Vector2 originalCenter)
        {
            // 生成点往下扫描实体方块，让地剑看起来插在地面上。
            Point startTile = originalCenter.ToTileCoordinates();

            for (int y = startTile.Y; y < startTile.Y + 45 && y < Main.maxTilesY - 2; y++)
            {
                Tile tile = Framing.GetTileSafely(startTile.X, y);
                if (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])
                    continue;

                return new Vector2(originalCenter.X, y * 16f - 16f);
            }

            return originalCenter;
        }

        private void SpawnChargeVisuals()
        {
            // 出生和俯冲前摇阶段的青金蓄势粒子。
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(28f, 34f),
                    DustID.FireworksRGB,
                    Main.rand.NextVector2Circular(1.4f, 1.4f),
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.75f, 1.2f));
                dust.noGravity = true;
            }
        }

        private void SpawnIntroBurst()
        {
            // 地剑成型瞬间环形喷出粒子。
            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 18f) * Main.rand.NextFloat(1.2f, 4.2f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(6f, 18f),
                    DustID.FireworksRGB,
                    velocity,
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.75f, 1.25f));
                dust.noGravity = true;
            }
        }

        private void SpawnForgingLightning()
        {
            // 锻造雷只负责表现“天雷锻剑”，不造成任何伤害。
            // 改用裂变电弧后它是纯本地粒子，不再走弹幕同步，所以每个客户端自己生成一次。
            if (Main.dedServ)
                return;

            AzureThunderSounds.PlayLightningSpawn(Projectile.Center);
            AzureThunderFissionBolt.Strike(
                Projectile.Center,
                ForgingLightningHeight,
                Main.rand.NextFloat(-0.2f, 0.2f),
                1.05f);
        }

        private void SpawnDeathChargeVisuals()
        {
            // 寿命末尾的淡金上升粒子。
            if (!Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center,
                DustID.FireworksRGB,
                Vector2.UnitY.RotatedByRandom(0.8f) * Main.rand.NextFloat(-5f, -2f),
                0,
                AzureThunderColors.PaleYellow,
                Main.rand.NextFloat(1.1f, 1.6f));
            dust.noGravity = true;
        }

        public override void OnKill(int timeLeft)
        {
            // 消失时爆出一圈粒子，并按是否俯冲决定音效重量。
            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.FireworksRGB,
                    Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(2f, 8f),
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.Yellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.8f, 1.45f));
                dust.noGravity = true;
            }

            // 消失演出保持克制，主要戏剧性雷击已经放在生成时。
            AzureThunderSounds.PlaySwordBurst(Projectile.Center, Diving);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 地剑命中统一附加电击和饰品联动；俯冲命中额外施加终极 DoT。
            bool harmonyActive = Main.player[Projectile.owner].HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>());
            if (harmonyActive)
            {
                target.AddBuff(BuffID.Electrified, 240);
                AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
            }
            if (Diving)
                AzureThunderSounds.PlaySwordHit(target.Center);
            if (Diving && harmonyActive)
                AzureThunderPlayer.ApplyUltimateDot(target, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 绘制阶段把淡入、蓄势、右键脉冲和俯冲状态合成到描边参数。
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            float fadeIn = Arriving ? 1f - (float)Math.Pow(1f - Utils.GetLerpValue(0f, MaterializeTime, Timer, true), 3f) : 1f;
            float chargeCompletion = MathHelper.Clamp(Timer / 45f, 0f, 1f);
            float pulseCompletion = outlinePulse / 20f;
            float pulseOpacity = (float)Math.Pow(pulseCompletion, 0.55f) * 0.52f;
            float outlineOpacity = (Diving ? 0.34f : MathHelper.Lerp(0.35f, 0.12f, chargeCompletion)) * fadeIn + pulseOpacity;
            float outlineRadius = (Diving ? 4.4f : 3f) + pulseCompletion * 7f;
            float drawScale = Projectile.scale * (0.72f + 0.28f * fadeIn + (float)Math.Sin(fadeIn * MathHelper.Pi) * 0.05f);
            Color drawColor = lightColor * fadeIn;

            // 描边颜色会在脉冲、俯冲和普通待机之间切换。
            HoldoutOutlineHelper.DrawSolidOutline(
                texture,
                drawPosition,
                Projectile.rotation,
                origin,
                Vector2.One * drawScale,
                SpriteEffects.None,
                outlinePulse > 0 ? AzureThunderColors.PaleYellow : Diving ? AzureThunderColors.Azure : AzureThunderColors.Yellow,
                outlineRadius,
                outlineOpacity,
                Main.GlobalTimeWrappedHourly + Projectile.identity * 0.1f,
                outlinePulse > 0 ? 22 : 16);

            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, Projectile.rotation, origin, drawScale, SpriteEffects.None);
            DrawBladeShine(texture, drawScale, fadeIn);

            return false;
        }

        private void DrawBladeShine(Texture2D texture, float drawScale, float opacity)
        {
            // 使用 SHPC 右键同款 HalfStar，在剑尖绘制简化的双轴星芒。
            Texture2D shineTex = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 shineScale = new Vector2(1.15f, 2.1f) * Projectile.scale;
            shineScale *= MathHelper.Lerp(
                0.9f,
                1.1f,
                (float)Math.Cos(Main.GlobalTimeWrappedHourly * 7.4f + Projectile.identity) * 0.5f + 0.5f);

            Vector2 bladeDirection = (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
            Vector2 lensFlareWorldPosition = Projectile.Center + bladeDirection * texture.Height * drawScale * 0.48f;
            Color lensFlareColor = (Color.Lerp(AzureThunderColors.Azure, AzureThunderColors.PaleYellow, 0.28f) * opacity) with { A = 0 };

            Main.EntitySpriteDraw(
                shineTex,
                lensFlareWorldPosition - Main.screenPosition,
                null,
                lensFlareColor,
                0f,
                shineTex.Size() * 0.5f,
                shineScale * 0.6f,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                shineTex,
                lensFlareWorldPosition - Main.screenPosition,
                null,
                lensFlareColor,
                MathHelper.PiOver2,
                shineTex.Size() * 0.5f,
                shineScale,
                SpriteEffects.None);
        }
    }
}
