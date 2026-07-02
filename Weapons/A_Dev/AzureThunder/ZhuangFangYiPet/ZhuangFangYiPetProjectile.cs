using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
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

        private const string TexturePath = "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/ZhuangFangYiPet/";
        private const int TransformFrameTime = 5;
        private const int TransformFrames = 13;
        private const int StrongAttackReleaseFrame = 18;
        private const int WeakAttackReleaseFrame = 14;

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
            Main.projFrames[Type] = 9;
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
            if (!petPlayer.HasAzureThunderInInventory())
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
            Vector2 focus = target?.Center ?? owner.Center + Vector2.UnitX * owner.direction * 360f;
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

            if (currentAction == PetAction.StrongAttack)
            {
                int damage = petPlayer.GetAzureThunderDamage(5.4f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<ZhuangFangYiSwordFlash>(),
                    0,
                    0f,
                    Projectile.owner,
                    (target?.Center ?? focus).X,
                    (target?.Center ?? focus).Y,
                    1.35f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target?.Center ?? focus,
                    Vector2.Zero,
                    ModContent.ProjectileType<ZhuangFangYiConvergenceController>(),
                    damage,
                    petPlayer.GetAzureThunderKnockback(),
                    Projectile.owner,
                    target?.whoAmI ?? -1,
                    owner.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>()) ? 1f : 0f);
                return;
            }

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<ZhuangFangYiSwordFlash>(),
                0,
                0f,
                Projectile.owner,
                focus.X,
                focus.Y,
                0.88f);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<ZhuangFangYiWeakLightning>(),
                petPlayer.GetAzureThunderDamage(2.4f),
                petPlayer.GetAzureThunderKnockback() * 0.35f,
                Projectile.owner,
                focus.X,
                focus.Y);
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

            blinkTimer = 12;
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
            Texture2D texture = ModContent.Request<Texture2D>(TexturePath + "庄方宜宠物_待机").Value;
            int bodyFrame = LoopFrame(9, 4);
            int tailFrame = LoopFrame(9, 6);
            DrawFrame(texture, 2, 11, 1, tailFrame, drawPosition, drawColor, effects);
            DrawFrame(texture, 2, 11, 0, bodyFrame, drawPosition, drawColor, effects);

            if (blinkTimer > 0)
            {
                int blinkFrame = 9 + Math.Min(2, (12 - blinkTimer) / 4);
                DrawFrame(texture, 2, 11, 0, blinkFrame, drawPosition, drawColor, effects);
            }
        }

        private void DrawMoving(Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            Texture2D texture = ModContent.Request<Texture2D>(TexturePath + "庄方宜宠物_移动").Value;
            DrawFrame(texture, 2, 8, 1, LoopFrame(8, 6), drawPosition, drawColor, effects);
            DrawFrame(texture, 2, 8, 0, LoopFrame(8, 4), drawPosition, drawColor, effects);
        }

        private void DrawFlying(Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            Texture2D texture = ModContent.Request<Texture2D>(TexturePath + "庄方宜宠物_飞行").Value;
            DrawFrame(texture, 2, 7, 1, LoopFrame(7, 6), drawPosition, drawColor, effects);
            DrawFrame(texture, 2, 7, 0, LoopFrame(7, 4), drawPosition, drawColor, effects);
        }

        private void DrawAttack(Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            Texture2D texture = ModContent.Request<Texture2D>(TexturePath + "庄方宜宠物_攻击").Value;
            int bodyFrames = currentAction == PetAction.WeakAttack ? 2 : 7;
            int bodyFrame = Math.Min(bodyFrames - 1, actionTimer / 4 % bodyFrames);
            int tailFrame = Math.Min(7, actionTimer / 4 % 8);
            int hairFrame = 8 + Math.Min(2, actionTimer / 3 % 3);

            DrawFrame(texture, 2, 11, 1, tailFrame, drawPosition, drawColor, effects);
            DrawFrame(texture, 2, 11, 0, bodyFrame, drawPosition, drawColor, effects);
            DrawFrame(texture, 2, 11, 1, hairFrame, drawPosition, drawColor, effects);

            if (currentAction == PetAction.StrongAttack)
            {
                int faceFrame = 7 + Math.Min(3, actionTimer / 3 % 4);
                DrawFrame(texture, 2, 11, 0, faceFrame, drawPosition, drawColor, effects);
            }
        }

        private void DrawHarmony(Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            Texture2D texture = ModContent.Request<Texture2D>(TexturePath + "小庄终结技").Value;
            bool attacking = currentAction == PetAction.StrongAttack;
            int bodyFrame = attacking ? Math.Min(4, actionTimer / 6 % 5) : 0;
            int tailFrame = actionTimer / 6 % 8;
            int hairFrame = 5 + actionTimer / 6 % 6;
            int ribbonFrame = 8 + actionTimer / 4 % 6;

            DrawFrame(texture, 2, 14, 1, tailFrame, drawPosition, drawColor, effects);
            DrawFrame(texture, 2, 14, 0, bodyFrame, drawPosition, drawColor, effects);
            DrawFrame(texture, 2, 14, 0, hairFrame, drawPosition, drawColor, effects);
            DrawFrame(texture, 2, 14, 1, ribbonFrame, drawPosition, drawColor, effects);
        }

        private void DrawTransform(Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            Texture2D texture = ModContent.Request<Texture2D>(TexturePath + "庄方宜宠物终结技变身动画").Value;
            int frame = Math.Min(TransformFrames - 1, actionTimer / TransformFrameTime);
            DrawFrame(texture, 1, TransformFrames, 0, frame, drawPosition, drawColor, effects);
        }

        private void DrawFrame(Texture2D texture, int columns, int rows, int column, int row, Vector2 drawPosition, Color drawColor, SpriteEffects effects)
        {
            column = Utils.Clamp(column, 0, columns - 1);
            row = Utils.Clamp(row, 0, rows - 1);
            int frameWidth = texture.Width / columns;
            int frameHeight = texture.Height / rows;
            Rectangle frame = new(column * frameWidth, row * frameHeight, frameWidth, frameHeight);
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

        private int LoopFrame(int frameCount, int frameInterval)
        {
            frameCount = Math.Max(1, frameCount);
            ulong tick = Main.GameUpdateCount / (uint)Math.Max(1, frameInterval);
            return (int)((tick + (ulong)(Projectile.identity % frameCount)) % (ulong)frameCount);
        }
    }
}
