using System.IO;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.Items.Weapons.Ranged.FlakKraken;
using static CalamityMod.Projectiles.Ranged.FlakKrakenHoldout;
using static Terraria.ModLoader.ModContent;

namespace CalamityMod.Projectiles.Ranged
{
    public class FlakKrakenProjectile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public override string Texture => "CalamityMod/Projectiles/Summon/CalamarisLamentProjectile";

        public ref float RocketID => ref Projectile.ai[0];
        public ref float DistanceOwnerMouse => ref Projectile.ai[1];
        public bool IsShrapnel
        {
            get => Projectile.ai[2] == 1f;
            set => Projectile.ai[2] = value == true ? 1f : 0f;
        }
        public bool HasHitEnemyWithInitialShot;

        public Player Owner { get; set; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
        }

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.extraUpdates = 1;
            Projectile.width = Projectile.height = 28;
            Projectile.timeLeft = 600;
            Projectile.localNPCHitCooldown = -1;

            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
        }

        // Syncs other variables that can't fit into the Projectile.ai[] array.
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(HasHitEnemyWithInitialShot);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            HasHitEnemyWithInitialShot = reader.ReadBoolean();
        }

        public override void AI()
        {
            Owner ??= Main.player[Projectile.owner];
            
            // Enforces the gravity.
            if (IsShrapnel)
            {
                if (Projectile.velocity.Y < 25f)
                    Projectile.velocity.Y += ProjectileGravityStrength;
            }
            // Scale for non Shrapnel
            if (!IsShrapnel)
            {
                Projectile.scale = 1.5f;
                Projectile.extraUpdates = 3;
            }

            // Rotates towards its velocity.
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
                Projectile.frameCounter = 0;
            }

            // If it's the initial shot and the shot has gone past the distance of the player and the mouse when shooting,
            // split into shrapnel.
            if (!IsShrapnel && !Projectile.WithinRange(Owner.MountedCenter, DistanceOwnerMouse))
                Projectile.Kill();

            // Below here go all the things that dedicated servers shouldn't spend resources on.
            // Like visuals and sounds.
            if (Main.dedServ)
                return;

            // The projectile will fade away as its time alive is ending.
            Projectile.alpha = (int)Utils.Remap(Projectile.timeLeft, 30f, 0f, 0f, 255f);

            // Shrapnel trail
            if (IsShrapnel)
            {
                if (Projectile.timeLeft % 2 == 0 && Projectile.timeLeft <= 590)
                {
                    AltSparkParticle spark = new AltSparkParticle(Projectile.Center - Projectile.velocity * 1.5f, Projectile.velocity * 0.01f, false, 8, 1.3f, EffectsColor * 0.135f);
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }
            else
            {
                Vector2 dustPos = Projectile.Center - Projectile.velocity * 3 + Main.rand.NextVector2Circular(12, 12);
                Dust dust = Dust.NewDustPerfect(dustPos, Main.rand.NextBool(4) ? 191 : DustEffectsID);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.4f, 1.65f);
                dust.velocity = -Projectile.velocity * Main.rand.NextFloat(0.1f, 0.7f);
            }

            Dust trailDust = Dust.NewDustDirect(Projectile.position,
                Projectile.width,
                Projectile.height,
                DustEffectsID,
                Scale: Main.rand.NextFloat(0.5f, 0.8f),
                Alpha: 127);
            trailDust.noGravity = true;
            trailDust.noLight = true;
            trailDust.alpha = (int)Utils.Remap(Projectile.timeLeft, 30f, 0f, 127f, 0f);
        }

        public override void OnSpawn(IEntitySource source) => Projectile.scale = 1.5f;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffType<CrushDepth>(), 180);
            if (!IsShrapnel)
                HasHitEnemyWithInitialShot = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffType<CrushDepth>(), 180);
            if (!IsShrapnel)
                HasHitEnemyWithInitialShot = true;
        }

        // The initial shot deals less damage.
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *= IsShrapnel ? 1f : InitialShotDamageMultiplier;
        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) => modifiers.SourceDamage *= IsShrapnel ? 1f : InitialShotDamageMultiplier;

        public override void OnKill(int timeLeft)
        {
            var info = new CalamityUtils.RocketBehaviorInfo((int)RocketID)
            {
                // Since we use our own spawning method for the cluster rockets, we don't need them to shoot anything,
                // we'll do it ourselves.
                clusterProjectileID = ProjectileID.None,
                destructiveClusterProjectileID = ProjectileID.None,
            };
            int blastRadius = Projectile.RocketBehavior(info);
            Projectile.ExpandHitboxBy((float)blastRadius);
            Projectile.Damage();

            if (!IsShrapnel)
            {
                bool usedClusterRockets = RocketID == ItemID.ClusterRocketI || RocketID == ItemID.ClusterRocketII;
                int flakAmount = usedClusterRockets ? ClusterShrapnelAmount : ShrapnelAmount;
                for (int i = 0; i < flakAmount && Main.myPlayer == Projectile.owner; i++)
                {
                    Projectile shrapnel = Projectile.NewProjectileDirect(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        Projectile.velocity.SafeNormalize(-Vector2.UnitY).RotatedByRandom(usedClusterRockets ? ClusterShrapnelAngleOffset : ShrapnelAngleOffset) * ProjectileShootSpeed * Main.rand.NextFloat(0.45f, 0.85f),
                        ProjectileType<FlakKrakenProjectile>(),
                        (int)(Projectile.damage * (HasHitEnemyWithInitialShot ? InitialShotHitShrapnelDamageMultiplier : .6f)),
                        Projectile.knockBack,
                        Projectile.owner,
                        RocketID,
                        ai2: 1f);

                    // Since the shrapnel and the initial shot are the same projectile,
                    // and we want to apply the damage penalty for hitting with the initial shot,
                    // all shrapnel will have this bool on and we'll apply the penalty here.
                    if (HasHitEnemyWithInitialShot)
                        shrapnel.ModProjectile<FlakKrakenProjectile>().HasHitEnemyWithInitialShot = true;
                }
            }

            // Since we do our own spread for the cluster rockets, it won't explode tiles.
            // So we do it manually here.
            if (RocketID == ItemID.ClusterRocketII)
                Projectile.ExplodeTiles(blastRadius, info.respectStandardBlastImmunity, info.tilesToCheck, info.wallsToCheck);

            // Inside here go all the things that dedicated servers shouldn't spend resources on.
            // Like visuals and sounds.
            if (Main.dedServ)
                return;

            // These effects will only run if the initial shot is splitting,
            // or the shrapnel doesn't have the damage pentalty, so we don't have effect overload.
            if (!IsShrapnel || (IsShrapnel && !HasHitEnemyWithInitialShot))
            {
                int dustAmount = Main.rand.Next(20, 30 + 1);
                for (int i = 0; i < dustAmount; i++)
                {
                    Dust boomDust = Dust.NewDustPerfect(Projectile.Center,
                        DustEffectsID,
                        Main.rand.NextVector2Circular(5f, 5f),
                        newColor: EffectsColor,
                        Scale: Main.rand.NextFloat(.8f, 1.2f));
                    boomDust.noLight = true;
                    boomDust.noLightEmittence = true;
                }

                Particle boomRing = new DirectionalPulseRing(Projectile.Center,
                    Vector2.Zero,
                    Color.White * 0.3f,
                    Vector2.One,
                    0f,
                    Projectile.width / 1560f,
                    Projectile.width / 156f,
                    20);
                GeneralParticleHandler.SpawnParticle(boomRing);

                int blobAmount = Main.rand.Next(4, 6 + 1);
                for (int i = 0; i < blobAmount; i++)
                {
                    Particle blob = new WaterGlobParticle(Projectile.Center,
                        Main.rand.NextVector2Circular(5f, 5f),
                        Main.rand.NextFloat(0.8f, 1.2f));
                    blob.Color = EffectsColor;
                    GeneralParticleHandler.SpawnParticle(blob);
                }

                int mistAmount = Main.rand.Next(3, 5 + 1);
                for (int mistIndex = 0; mistIndex < mistAmount; mistIndex++)
                {
                    float angle = MathHelper.TwoPi / mistAmount * mistIndex;
                    Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(2f, 7f);
                    Particle boomMist = new HeavySmokeParticle(Projectile.Center,
                        velocity,
                        EffectsColor * Main.rand.NextFloat(0.15f, 0.25f),
                        Main.rand.Next(45, 61),
                        Main.rand.NextFloat(.4f, 1.1f),
                        Main.rand.NextFloat(0.2f, 0.35f));
                    GeneralParticleHandler.SpawnParticle(boomMist);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Color drawColor = IsShrapnel ? Projectile.GetAlpha(lightColor) : Color.Black;
            Vector2 origin = frame.Size() * 0.5f;

            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor * 0.3f);

            Main.EntitySpriteDraw(texture, drawPosition, frame, drawColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
}
