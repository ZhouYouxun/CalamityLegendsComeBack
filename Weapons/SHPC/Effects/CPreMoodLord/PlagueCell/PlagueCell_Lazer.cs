using System.Collections.Generic;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.PlagueCell
{
    internal class PlagueCell_Lazer : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 30;
        private const float MaxBeamLength = 100f * 16f;
        private const float BeamWidth = 24f;
        private const float MinMuzzleDistance = 42f;
        private const float MaxMuzzleDistance = 92f;
        private const int MaxHits = 4;
        private const int DamageChannelCloseHits = 4;
        private static readonly Color OuterColor = new(255, 24, 16);
        private static readonly Color InnerColor = Color.White;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int elapsedTime;
        private int damagingHits;
        private bool damageChannelClosed;
        private Vector2 beamVector = Vector2.UnitX;
        private readonly HashSet<int> markedTargets = new();
        private ref float BeamLength => ref Projectile.localAI[0];

        private Vector2 ForwardDirection
        {
            get
            {
                Vector2 storedDirection = new(Projectile.ai[0], Projectile.ai[1]);
                if (storedDirection.LengthSquared() > 0.0001f)
                    return storedDirection.SafeNormalize(Vector2.UnitX);

                return Projectile.velocity.SafeNormalize(Vector2.UnitX);
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 50;
            Projectile.alpha = 0;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => damageChannelClosed ? false : null;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 forward = GetOwnerAimDirection(owner, ForwardDirection);

            Projectile.ai[2] = MathHelper.Clamp(Vector2.Distance(owner.Center, Projectile.Center), MinMuzzleDistance, MaxMuzzleDistance);
            SetForwardDirection(forward);
            Projectile.Center = GetAnchoredMuzzlePosition(owner, forward);
            Projectile.velocity = forward;
            Projectile.rotation = forward.ToRotation();
            beamVector = forward;
            BeamLength = MaxBeamLength;
            Projectile.netUpdate = true;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.numHits >= MaxHits)
                return false;

            return IsValidLaserTarget(target) ? null : false;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            beamVector = GetOwnerAimDirection(owner, ForwardDirection);
            SetForwardDirection(beamVector);
            Projectile.Center = GetAnchoredMuzzlePosition(owner, beamVector);
            Projectile.velocity = beamVector;
            Projectile.rotation = beamVector.ToRotation();
            BeamLength = MaxBeamLength;

            EmitBeamDust();
            Lighting.AddLight(Projectile.Center, OuterColor.ToVector3() * 0.28f);
            DelegateMethods.v3_1 = OuterColor.ToVector3() * CalculateOpacity() * 0.28f;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + beamVector * BeamLength, BeamWidth, DelegateMethods.CastLight);
            TryLockInvulnerableBosses();
            elapsedTime++;
        }

        private void TryLockInvulnerableBosses()
        {
            if (Projectile.owner != Main.myPlayer || damageChannelClosed || elapsedTime % 4 != 0)
                return;

            foreach (NPC target in Main.ActiveNPCs)
            {
                if (markedTargets.Count >= MaxHits || !IsValidLaserTarget(target) || !IsBossLikeTarget(target) || markedTargets.Contains(target.whoAmI))
                    continue;

                float collisionPoint = 0f;
                bool touching = Collision.CheckAABBvLineCollision(
                    target.Hitbox.TopLeft(),
                    target.Hitbox.Size(),
                    Projectile.Center,
                    Projectile.Center + beamVector * BeamLength,
                    BeamWidth,
                    ref collisionPoint);

                if (touching)
                    TrySpawnMark(target);
            }
        }

        private void SetForwardDirection(Vector2 direction)
        {
            Vector2 safeDirection = direction.SafeNormalize(Vector2.UnitX);
            Projectile.ai[0] = safeDirection.X;
            Projectile.ai[1] = safeDirection.Y;
        }

        private Vector2 GetAnchoredMuzzlePosition(Player owner, Vector2 forward)
        {
            float muzzleDistance = Projectile.ai[2];
            if (muzzleDistance <= 0f)
                muzzleDistance = 68f;

            return owner.Center + forward * MathHelper.Clamp(muzzleDistance, MinMuzzleDistance, MaxMuzzleDistance);
        }

        private static Vector2 GetOwnerAimDirection(Player owner, Vector2 fallback)
        {
            Vector2 mouseWorld = owner.whoAmI == Main.myPlayer && !Main.dedServ ? Main.MouseWorld : owner.Calamity().mouseWorld;
            return (mouseWorld - owner.Center).SafeNormalize(fallback.SafeNormalize(Vector2.UnitX * owner.direction));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.scale <= 0f)
                return false;

            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + beamVector * BeamLength,
                BeamWidth,
                ref collisionPoint);
        }

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + beamVector * BeamLength, BeamWidth + 8f, DelegateMethods.CutTiles);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            damagingHits++;

            target.AddBuff(ModContent.BuffType<Plague>(), 180);
            target.AddBuff(BuffID.Venom, 180);
            target.AddBuff(BuffID.Poisoned, 180);

            if (damagingHits >= DamageChannelCloseHits)
                damageChannelClosed = true;

            TrySpawnMark(target);
        }

        private void TrySpawnMark(NPC target)
        {
            if (Projectile.owner != Main.myPlayer || markedTargets.Count >= MaxHits || !IsValidLaserTarget(target) || !markedTargets.Add(target.whoAmI))
                return;

            bool isBig = markedTargets.Count == 1;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<PlagueCell_Marked>(),
                Projectile.damage,
                0f,
                Projectile.owner,
                target.whoAmI,
                isBig ? 1f : 0f);
        }

        private static bool IsValidLaserTarget(NPC target)
        {
            if (target == null || !target.active || target.friendly)
                return false;

            return target.CanBeChasedBy() || IsBossLikeTarget(target);
        }

        private static bool IsBossLikeTarget(NPC target)
        {
            return target.boss ||
                NPCID.Sets.ShouldBeCountedAsBoss[target.type] ||
                target.type == NPCID.CultistBoss ||
                target.type == NPCID.CultistBossClone;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return false;

            Texture2D outer = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D inner = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineThick").Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 unit = Projectile.rotation.ToRotationVector2();
            float fx = MathHelper.Max(0.2f, CalculateOpacity());
            float beamLengthScale = BeamLength / 1000f;
            Vector2 beamCenter = start + unit * BeamLength * 0.5f;
            Color outerColor = OuterColor with { A = 0 };
            Color innerColor = InnerColor with { A = 0 };
            float randomSize = 1f;
            float widthFactor = MathHelper.Clamp(BeamWidth / 10f, 0.9f, 2.6f);

            Main.EntitySpriteDraw(
                outer,
                beamCenter,
                null,
                outerColor * fx,
                Projectile.rotation + MathHelper.PiOver2,
                outer.Size() * 0.5f,
                new Vector2(2.5f * randomSize * fx * widthFactor, 55f * beamLengthScale) * Projectile.scale * 0.01f,
                SpriteEffects.FlipVertically,
                0f);

            Main.EntitySpriteDraw(
                inner,
                beamCenter,
                null,
                Color.Lerp(innerColor, Color.White with { A = 0 }, 0.3f) * MathHelper.Min(fx, 1f),
                Projectile.rotation + MathHelper.PiOver2,
                inner.Size() * 0.5f,
                new Vector2(0.4f * MathHelper.Min(fx, 1f) * MathHelper.Lerp(0.95f, 1.35f, (widthFactor - 0.9f) / 1.7f), 55f * beamLengthScale) * Projectile.scale * 0.01f,
                SpriteEffects.FlipVertically,
                0f);

            for (int i = 0; i < 2; i++)
            {
                Main.EntitySpriteDraw(
                    glow,
                    start + unit * 7f * Projectile.scale,
                    null,
                    Color.Lerp(outerColor, Color.White with { A = 0 }, i) * MathHelper.Max(fx - 0.15f, 0f),
                    Projectile.rotation + MathHelper.PiOver2,
                    glow.Size() * 0.5f,
                    new Vector2((2f + 0.05f * (fx + 25f)) * MathHelper.Lerp(0.9f, 1.45f, (widthFactor - 0.9f) / 1.7f), 1f) * Projectile.scale * MathHelper.Lerp(fx, 1f, 0.7f) * (0.03f - 0.01f * i),
                    SpriteEffects.FlipVertically,
                    0f);
            }

            return false;
        }

        private float CalculateOpacity()
        {
            float fadeIn = Utils.GetLerpValue(0f, 8f, elapsedTime, true);
            float fadeOutFrames = System.Math.Min(12f, Lifetime * 0.35f);
            float fadeOut = Utils.GetLerpValue(0f, fadeOutFrames, Projectile.timeLeft, true);
            return fadeIn * fadeOut;
        }

        private void EmitBeamDust()
        {
            if (Main.dedServ || Main.GameUpdateCount % 8 != 0)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + direction * Main.rand.NextFloat(12f, 34f),
                DustID.RainbowTorch,
                direction.RotatedByRandom(0.18f) * Main.rand.NextFloat(0.6f, 1.5f),
                0,
                Color.Lerp(OuterColor, InnerColor, 0.35f),
                Main.rand.NextFloat(0.75f, 1.05f));
            dust.noGravity = true;
        }
    }
}
