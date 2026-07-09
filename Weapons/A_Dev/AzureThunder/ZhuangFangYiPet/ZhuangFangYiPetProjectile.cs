using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.ZhuangFangYiPet
{
    internal sealed class ZhuangFangYiPetProjectile : ModProjectile, ILocalizedModType
    {
        private enum PetAction
        {
            None,
            StrongAttack,
            WeakAttack,
            Transform
        }

        private const string TexturePath = "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/ZhuangFangYiPet/新版贴图/";
        private const int TransformFrameTime = 5;
        private const int TransformFrames = 13;
        private const int StrongAttackReleaseFrame = 18;
        private const int WeakAttackReleaseFrame = 14;
        private const int BlinkFrames = 5;
        private const int BlinkFrameTime = 3;
        private const int BlinkDuration = BlinkFrames * BlinkFrameTime;

        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => TexturePath + "庄方宜宠物_待机";

        private PetAction currentAction;
        private int actionTimer;
        private int actionTarget = -1;
        private int transformDuration;
        private int blinkCooldown;
        private int blinkTimer;
        private bool actionReleased;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8;
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 56;
            Projectile.netImportant = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            ZhuangFangYiPetPlayer petPlayer = owner.GetModPlayer<ZhuangFangYiPetPlayer>();
            if (!petPlayer.IsHoldingAzureThunder())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.friendly = false;
            Lighting.AddLight(Projectile.Center, new Vector3(0.16f, 0.95f, 0.74f) * 0.45f);
            UpdateBlink();
            ReadOwnerCommands(owner, petPlayer);

            switch (currentAction)
            {
                case PetAction.Transform:
                    TransformAI(owner, petPlayer);
                    break;

                case PetAction.StrongAttack:
                case PetAction.WeakAttack:
                    AttackAI(owner, petPlayer);
                    break;

                default:
                    FollowOwnerAI(owner);
                    break;
            }

            if (Projectile.velocity.X > 0.2f)
                Projectile.spriteDirection = 1;
            else if (Projectile.velocity.X < -0.2f)
                Projectile.spriteDirection = -1;

            Projectile.rotation = MathHelper.Lerp(Projectile.rotation, Projectile.velocity.X * 0.018f, 0.18f);
        }

        private void ReadOwnerCommands(Player owner, ZhuangFangYiPetPlayer petPlayer)
        {
            if (currentAction == PetAction.Transform)
                return;

            if (petPlayer.TryConsumeTransformRequest(out int requestedDuration))
            {
                StartTransform(requestedDuration);
                return;
            }

            if (currentAction != PetAction.None)
                return;

            if (petPlayer.TryConsumeStrongTarget(out int strongTarget))
            {
                StartAttack(strongTarget, PetAction.StrongAttack);
                return;
            }

            if (petPlayer.TryConsumeWeakTarget(out int weakTarget))
                StartAttack(weakTarget, PetAction.WeakAttack);
        }

        private void StartTransform(int duration)
        {
            currentAction = PetAction.Transform;
            actionTimer = 0;
            actionTarget = -1;
            actionReleased = false;
            transformDuration = Math.Max(1, duration);
            Projectile.velocity *= 0.35f;
            Projectile.netUpdate = true;
        }

        private void StartAttack(int targetIndex, PetAction action)
        {
            currentAction = action;
            actionTimer = 0;
            actionTarget = targetIndex;
            actionReleased = false;
            Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.35f, Pitch = 0.35f }, Projectile.Center);
        }

        private void TransformAI(Player owner, ZhuangFangYiPetPlayer petPlayer)
        {
            actionTimer++;
            Vector2 destination = owner.Center + new Vector2(-48f * owner.direction, -86f + owner.gfxOffY);
            SmoothMove(destination, 11f, 18f);
            Projectile.spriteDirection = owner.direction;
            Projectile.rotation = MathHelper.Lerp(Projectile.rotation, 0f, 0.18f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(32f, 42f),
                    DustID.FireworksRGB,
                    Main.rand.NextVector2Circular(1.2f, 1.2f),
                    0,
                    AzureThunderColors.PaleYellow,
                    Main.rand.NextFloat(0.75f, 1.2f));
                dust.noGravity = true;
            }

            if (actionTimer < TransformFrames * TransformFrameTime)
                return;

            currentAction = PetAction.None;
            actionTimer = 0;
            petPlayer.CompleteHarmonyTransform(transformDuration, Projectile.Center);
            transformDuration = 0;
            Projectile.netUpdate = true;
        }

        private void AttackAI(Player owner, ZhuangFangYiPetPlayer petPlayer)
        {
            actionTimer++;
            NPC target = ResolveTarget();
            Vector2 focus = target?.Center ?? owner.Center + Vector2.UnitX * owner.direction * 420f;
            Vector2 fromTarget = (Projectile.Center - focus).SafeNormalize(new Vector2(-owner.direction, -0.35f));
            Vector2 side = Math.Sign(fromTarget.X) == 0 ? new Vector2(-owner.direction, 0f) : new Vector2(Math.Sign(fromTarget.X), 0f);
            Vector2 destination = focus + side * 170f - Vector2.UnitY * 82f;

            if (Vector2.Distance(owner.Center, focus) > 1400f)
                destination = owner.Center + new Vector2(-68f * owner.direction, -76f + owner.gfxOffY);

            if (actionReleased)
            {
                Vector2 recoil = (Projectile.Center - focus).SafeNormalize(Vector2.UnitX * -owner.direction);
                float recoilPower = currentAction == PetAction.StrongAttack ? 22f : 14f;
                destination += recoil * recoilPower * (1f - Utils.GetLerpValue(0f, 18f, actionTimer - GetReleaseFrame(), true));
            }

            SmoothMove(destination, currentAction == PetAction.StrongAttack ? 15f : 12f, currentAction == PetAction.StrongAttack ? 14f : 18f);
            Projectile.spriteDirection = focus.X >= Projectile.Center.X ? 1 : -1;

            int releaseFrame = GetReleaseFrame();
            if (!actionReleased && actionTimer >= releaseFrame)
            {
                actionReleased = true;
                ReleaseAttack(owner, petPlayer, target, focus);
                Projectile.velocity -= (focus - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction) * (currentAction == PetAction.StrongAttack ? 5.5f : 3.5f);
            }

            int actionLength = currentAction == PetAction.StrongAttack
                ? (owner.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>()) ? 46 : 42)
                : 34;
            if (actionTimer >= actionLength)
            {
                currentAction = PetAction.None;
                actionTimer = 0;
                actionTarget = -1;
                actionReleased = false;
            }
        }

        private int GetReleaseFrame()
        {
            return currentAction == PetAction.StrongAttack ? StrongAttackReleaseFrame : WeakAttackReleaseFrame;
        }

        private void ReleaseAttack(Player owner, ZhuangFangYiPetPlayer petPlayer, NPC target, Vector2 focus)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            bool harmony = owner.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>());
            bool strong = currentAction == PetAction.StrongAttack;
            float damageMultiplier = strong ? (harmony ? 4.15f : 3.2f) : 1.45f;
            float lightningScale = strong ? (harmony ? 1.45f : 1.18f) : 0.82f;
            Vector2 muzzle = Projectile.Center + new Vector2(18f * Projectile.spriteDirection, -8f);
            Vector2 aimPoint = target?.Center ?? focus;
            Vector2 direction = (aimPoint - muzzle).SafeNormalize(Vector2.UnitX * Projectile.spriteDirection);
            aimPoint += direction * (strong ? 90f : 55f);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                muzzle,
                Vector2.Zero,
                ModContent.ProjectileType<ZhuangFangYiWeakLightning>(),
                petPlayer.GetAzureThunderDamage(damageMultiplier),
                petPlayer.GetAzureThunderKnockback() * (strong ? 0.65f : 0.35f),
                Projectile.owner,
                aimPoint.X,
                aimPoint.Y,
                lightningScale);

            AzureThunderAccessoryPlayer.TryReleaseWorldSplitter(owner, muzzle, target, focus, strong);
        }

        private void FollowOwnerAI(Player owner)
        {
            Vector2 idleOffset = new(-62f * owner.direction, -70f + owner.gfxOffY);
            float bob = (float)Math.Sin((Main.GameUpdateCount + Projectile.identity * 13) * 0.055f) * 7f;
            Vector2 destination = owner.Center + idleOffset + Vector2.UnitY * bob;
            float distance = Vector2.Distance(Projectile.Center, destination);

            if (distance > 2200f)
            {
                TeleportToOwner(owner, destination);
                return;
            }

            float speed = distance > 720f ? 23f : distance > 240f ? 13.5f : 7.5f;
            float inertia = distance > 720f ? 17f : distance > 240f ? 28f : 42f;
            SmoothMove(destination, speed, inertia);
            Projectile.spriteDirection = owner.direction;
        }

        private void SmoothMove(Vector2 destination, float speed, float inertia)
        {
            Vector2 toDestination = destination - Projectile.Center;
            float distance = toDestination.Length();
            if (distance < 12f)
            {
                Projectile.velocity *= 0.92f;
                return;
            }

            Vector2 desiredVelocity = toDestination.SafeNormalize(Vector2.Zero) * Math.Min(speed, distance / 8f + 3f);
            Projectile.velocity = (Projectile.velocity * inertia + desiredVelocity) / (inertia + 1f);
        }

        private void TeleportToOwner(Player owner, Vector2 destination)
        {
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(28f, 28f),
                    DustID.FireworksRGB,
                    Main.rand.NextVector2Circular(2.4f, 2.4f),
                    0,
                    AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.75f, 1.15f));
                dust.noGravity = true;
            }

            Projectile.Center = destination;
            Projectile.velocity = owner.velocity * 0.35f;
            Projectile.netUpdate = true;
        }

        private NPC ResolveTarget()
        {
            if (Main.npc.IndexInRange(actionTarget))
            {
                NPC target = Main.npc[actionTarget];
                if (target.active && target.CanBeChasedBy(Projectile))
                    return target;
            }

            return ZhuangFangYiPetPlayer.FindNearestPetTarget(Projectile.Center, 1600f, currentAction == PetAction.StrongAttack);
        }

        private void UpdateBlink()
        {
            if (blinkTimer > 0)
            {
                blinkTimer--;
                return;
            }

            if (blinkCooldown > 0)
            {
                blinkCooldown--;
                return;
            }

            blinkTimer = BlinkDuration;
            blinkCooldown = Main.rand.Next(150, 280);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color drawColor = Projectile.GetAlpha(lightColor);
            SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            if (currentAction == PetAction.Transform)
            {
                DrawTransform(drawPosition, drawColor, effects);
                return false;
            }

            if (owner.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>()))
            {
                DrawHarmony(drawPosition, drawColor, effects);
                return false;
            }

            if (currentAction == PetAction.StrongAttack || currentAction == PetAction.WeakAttack)
            {
                DrawAttack(drawPosition, drawColor, effects);
                return false;
            }

            if (ShouldUseFlyingAnimation(owner))
                DrawFlying(drawPosition, drawColor, effects);
            else if (Math.Abs(owner.velocity.X) > 1f)
                DrawMoving(drawPosition, drawColor, effects);
            else
                DrawIdle(drawPosition, drawColor, effects);

            return false;
        }

        private bool ShouldUseFlyingAnimation(Player owner)
        {
            return Math.Abs(owner.velocity.Y) > 0.25f ||
                Projectile.Distance(owner.Center) > 180f ||
                Math.Abs(Projectile.velocity.Y) > 1.8f;
        }

        private void DrawIdle(Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            DrawNormalTail(drawPosition, drawColor, effects, 6);
            DrawVerticalFrame(RequestTexture("庄方宜宠物_待机"), 8, LoopFrame(8, 5), drawPosition, drawColor, effects);
            DrawNormalBlink(drawPosition, drawColor, effects);
        }

        private void DrawMoving(Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            DrawNormalTail(drawPosition, drawColor, effects, 5);
            DrawVerticalFrame(RequestTexture("庄方宜宠物_移动"), 8, LoopFrame(8, 4), drawPosition, drawColor, effects);
            DrawNormalBlink(drawPosition, drawColor, effects);
        }

        private void DrawFlying(Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            DrawNormalTail(drawPosition, drawColor, effects, 5);
            DrawVerticalFrame(RequestTexture("庄方宜宠物_飞行"), 7, LoopFrame(7, 4), drawPosition, drawColor, effects);
            DrawNormalBlink(drawPosition, drawColor, effects);
        }

        private void DrawAttack(Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            DrawNormalTail(drawPosition, drawColor, effects, 4);
            int bodyFrame = Math.Min(6, actionTimer / 4);
            DrawVerticalFrame(RequestTexture("庄方宜宠物_攻击"), 7, bodyFrame, drawPosition, drawColor, effects);
        }

        private void DrawHarmony(Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            bool attacking = currentAction == PetAction.StrongAttack || currentAction == PetAction.WeakAttack;
            int bodyFrame = attacking ? Math.Min(4, actionTimer / 5) : 0;

            DrawVerticalFrame(RequestTexture("庄方宜宠物_天理真和_尾巴"), 8, LoopFrame(8, 6), drawPosition, drawColor, effects);
            DrawVerticalFrame(RequestTexture("庄方宜宠物_天理真和_飘带"), 6, LoopFrame(6, 5, 2), drawPosition, drawColor, effects);
            DrawVerticalFrame(RequestTexture("庄方宜宠物_天理真和_攻击（非攻击情况只用第一帧）"), 5, bodyFrame, drawPosition, drawColor, effects);
            DrawVerticalFrame(RequestTexture("庄方宜宠物_天理真和_头发"), 6, LoopFrame(6, 6, 4), drawPosition, drawColor, effects);
            DrawHarmonyExpression(drawPosition, drawColor, effects);
        }

        private void DrawTransform(Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            int frame = Math.Min(TransformFrames - 1, actionTimer / TransformFrameTime);
            DrawVerticalFrame(RequestTexture("庄方宜宠物_终结技变身动画（96x70）"), TransformFrames, frame, drawPosition, drawColor, effects);
        }

        private void DrawNormalTail(Vector2 drawPosition, Color drawColor, SpriteEffects effects, int interval)
        {
            DrawVerticalFrame(RequestTexture("庄方宜宠物_尾巴"), 8, LoopFrame(8, interval, 3), drawPosition, drawColor, effects);
        }

        private void DrawNormalBlink(Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            int frame = blinkTimer > 0 ? Math.Min(BlinkFrames - 1, (BlinkDuration - blinkTimer) / BlinkFrameTime) : 0;
            DrawVerticalFrame(RequestTexture("庄方宜宠物_眨眼"), BlinkFrames, frame, drawPosition, drawColor, effects);
        }

        private void DrawHarmonyExpression(Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            int frame = blinkTimer > 0 ? Math.Min(BlinkFrames - 1, (BlinkDuration - blinkTimer) / BlinkFrameTime) : 0;
            DrawVerticalFrame(RequestTexture("庄方宜宠物_天理真和_表情"), BlinkFrames, frame, drawPosition, drawColor, effects);
        }

        private static Texture2D RequestTexture(string name)
        {
            return ModContent.Request<Texture2D>(TexturePath + name).Value;
        }

        private void DrawVerticalFrame(Texture2D texture, int rows, int row, Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            rows = Math.Max(1, rows);
            row = Utils.Clamp(row, 0, rows - 1);
            int frameHeight = texture.Height / rows;
            Rectangle frame = new(0, row * frameHeight, texture.Width, frameHeight);
            Vector2 origin = frame.Size() * 0.5f;

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                frame,
                drawColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects);
        }

        private int LoopFrame(int frameCount, int frameInterval, int phase = 0)
        {
            frameCount = Math.Max(1, frameCount);
            ulong tick = Main.GameUpdateCount / (uint)Math.Max(1, frameInterval);
            return (int)((tick + (ulong)((Projectile.identity + phase) % frameCount)) % (ulong)frameCount);
        }
    }
}
