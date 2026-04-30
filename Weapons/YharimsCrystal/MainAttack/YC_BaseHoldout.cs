using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack
{
    public abstract class YC_BaseHoldout : ModProjectile, ILocalizedModType
    {
        private const float AnimationRampMax = 150f;
        private const int SoundInterval = 20;

        protected Player Owner => Main.player[Projectile.owner];
        protected ref float HoldFrameCounter => ref Projectile.localAI[0];

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YharimsCrystalPrism";
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public Vector2 ForwardDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);

        protected virtual float HoldoutDistance => 0f;
        protected virtual float SoundPitch => 0.05f;
        protected virtual bool UsesStandardChannelKill => true;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 22;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed || Owner.HeldItem.type != ModContent.ItemType<NewLegendYharimsCrystal>())
            {
                Projectile.Kill();
                return;
            }

            if (UsesStandardChannelKill &&
                Projectile.owner == Main.myPlayer &&
                (!Main.mouseLeft || Main.mapFullscreen || Main.blockMouse))
            {
                Projectile.Kill();
                return;
            }

            HoldFrameCounter++;
            UpdateHoldout();
            UpdateAnimation();
            OnHoldoutAI();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawPrism(Color.White, Projectile.scale, Projectile.Center + Vector2.UnitY * Projectile.gfxOffY, Projectile.rotation);
            return false;
        }

        protected virtual void OnHoldoutAI()
        {
        }

        protected void UpdateHoldout()
        {
            Vector2 holdoutCenter = Owner.RotatedRelativePoint(Owner.MountedCenter, true);

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 aimVector = (Main.MouseWorld - holdoutCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
                if (aimVector != Projectile.velocity)
                    Projectile.netUpdate = true;

                Projectile.velocity = aimVector;
            }

            Projectile.Center = holdoutCenter + Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction) * HoldoutDistance;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction;

            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRotation += MathHelper.Pi;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Projectile.timeLeft = 2;

            if (Projectile.soundDelay <= 0)
            {
                Projectile.soundDelay = SoundInterval;
                if (HoldFrameCounter > 1f)
                    SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.16f, Pitch = SoundPitch }, Projectile.Center);
            }
        }

        protected void DrawPrism(Color color, float scale, Vector2 worldPosition, float rotation)
        {
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frameCount = Main.projFrames[Type] <= 0 ? 1 : Main.projFrames[Type];
            if (texture.Height % frameCount != 0)
                frameCount = 1;

            int frame = frameCount == 1 ? 0 : Projectile.frame % frameCount;
            int frameHeight = texture.Height / frameCount;
            int frameYOffset = frameHeight * frame;
            Vector2 drawPosition = (worldPosition - Main.screenPosition).Floor();

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                new Rectangle(0, frameYOffset, texture.Width, frameHeight),
                color,
                rotation,
                new Vector2(texture.Width * 0.5f, frameHeight * 0.5f),
                scale,
                effects,
                0f);
        }

        protected void EmitDust(Vector2 center, Vector2 velocity, Color color, float scale = 1f, int dustType = DustID.GoldFlame)
        {
            if (Main.dedServ)
                return;

            Dust dust = Dust.NewDustPerfect(center, dustType, velocity, 0, color, scale);
            dust.noGravity = true;
        }

        protected NPC FindTargetAhead(float range, float coneDegrees, bool requireLineOfSight)
        {
            float maxDistanceSquared = range * range;
            float maxAngle = MathHelper.ToRadians(coneDegrees) * 0.5f;
            NPC nearest = null;
            Vector2 forward = ForwardDirection;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                Vector2 toNpc = npc.Center - Projectile.Center;
                float distanceSquared = toNpc.LengthSquared();
                if (distanceSquared > maxDistanceSquared)
                    continue;

                float angleDifference = System.Math.Abs(MathHelper.WrapAngle(forward.ToRotation() - toNpc.ToRotation()));
                if (angleDifference > maxAngle)
                    continue;

                if (requireLineOfSight && !Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;

                maxDistanceSquared = distanceSquared;
                nearest = npc;
            }

            return nearest;
        }

        private void UpdateAnimation()
        {
            Projectile.frameCounter++;
            int framesPerUpdate = HoldFrameCounter >= AnimationRampMax ? 2 :
                HoldFrameCounter >= AnimationRampMax * 0.66f ? 3 : 4;

            if (Projectile.frameCounter >= framesPerUpdate)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }
    }
}
