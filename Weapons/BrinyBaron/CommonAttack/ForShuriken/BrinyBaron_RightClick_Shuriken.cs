using System;
using System.IO;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.POWER;
using CalamityMod;
using CalamityMod.Projectiles.Melee;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken
{
    public class BrinyBaron_RightClick_Shuriken : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/TornadoProj";

        private const int BaseSize = 50;
        private const int StickyLifetime = 72;

        private int HighestUnlockedStage =>
            DownedBossSystem.downedBoomerDuke ? 3 :
            NPC.downedFishron ? 2 :
            Main.hardMode ? 1 : 0;

        private float SizeScale => Projectile.width / (float)BaseSize;
        private float Radius => Projectile.width * 0.5f;
        private bool TideEmpowered => Main.player.IndexInRange(Projectile.owner) &&
                                      Main.player[Projectile.owner].active &&
                                      Main.player[Projectile.owner].GetModPlayer<BBEXPlayer>().TideFull;

        private bool stuckInTarget;
        private int stuckTargetIndex = -1;
        private int stickTimer;
        private int sliceEffectTimer;
        private int soundTimer;
        private Vector2 stickOffsetFromTarget;

        public override void SetDefaults()
        {
            Projectile.width = BaseSize;
            Projectile.height = BaseSize;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.alpha = 255;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void OnSpawn(IEntitySource source)
        {
            EnsureSquareHitbox();
            stuckInTarget = false;
            stuckTargetIndex = -1;
            stickTimer = 0;
            sliceEffectTimer = 0;
            soundTimer = 0;
            stickOffsetFromTarget = Vector2.Zero;
        }

        public override void AI()
        {
            EnsureSquareHitbox();
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 22, 0, 255);
            Lighting.AddLight(Projectile.Center, 0.05f, 0.22f, 0.32f);

            if (soundTimer > 0)
                soundTimer--;

            if (!stuckInTarget)
            {
                HandleFlightMovement();
                SpawnUnlockedFlightEffects();
            }
            else
            {
                HandleStickyState();
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (stuckInTarget)
                return target.whoAmI == stuckTargetIndex;

            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 180);

            Vector2 hitForward = Projectile.velocity.SafeNormalize((target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX));

            BBShuriken_Initial_Effects.SpawnHitBurst(Projectile, target, hitForward, SizeScale, HighestUnlockedStage);

            if (HighestUnlockedStage >= 2)
                BBShuriken_Fishron_Effects.SpawnHitBurst(Projectile, target, hitForward, SizeScale);

            if (HighestUnlockedStage >= 3)
                BBShuriken_BoomerDuke_Effects.SpawnHitBurst(Projectile, target, hitForward, SizeScale);

            if (!stuckInTarget)
            {
                stuckInTarget = true;
                stuckTargetIndex = target.whoAmI;
                stickOffsetFromTarget = Projectile.Center - target.Center;
                Projectile.tileCollide = false;
                Projectile.velocity = Vector2.Zero;
                Projectile.timeLeft = 90;
                stickTimer = 0;
                sliceEffectTimer = 0;
                Projectile.direction = Projectile.direction == 0 ? (Main.rand.NextBool() ? 1 : -1) : Projectile.direction;
                Projectile.netUpdate = true;

                SoundEngine.PlaySound(SoundID.Item39 with
                {
                    Volume = 0.7f,
                    Pitch = Main.rand.NextFloat(-0.1f, 0.15f)
                }, target.Center);
            }
            else
            {
                BBShuriken_Initial_Effects.SpawnStickySliceBurst(Projectile, SizeScale, HighestUnlockedStage);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override void OnKill(int timeLeft)
        {
            BBShuriken_Initial_Effects.SpawnDeathBurst(Projectile, SizeScale);

            if (HighestUnlockedStage >= 3 && Main.myPlayer == Projectile.owner)
            {
                Vector2 baseVelocity = Projectile.oldVelocity.LengthSquared() > 1f ? Projectile.oldVelocity : Projectile.velocity;
                float launchSpeed = Math.Max(baseVelocity.Length(), 8f);

                Vector2[] directions =
                {
                    Vector2.UnitX,
                    -Vector2.UnitX,
                    Vector2.UnitY,
                    -Vector2.UnitY
                };

                foreach (Vector2 direction in directions)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        direction * launchSpeed,
                        ModContent.ProjectileType<BBShuriken_Light>(),
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner);
                }
            }

            if (TideEmpowered && Main.myPlayer == Projectile.owner && Main.rand.NextBool(3))
            {
                Player player = Main.player[Projectile.owner];
                if (player.ownedProjectileCounts[ModContent.ProjectileType<BrinySpout>()] == 0)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<BrinyTyphoonBubble>(),
                        Projectile.damage,
                        Projectile.knockBack,
                        player.whoAmI);
                }
            }

            SoundEngine.PlaySound(SoundID.Item107 with
            {
                Volume = 0.55f,
                Pitch = 0.15f
            }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D projectileTexture = TextureAssets.Projectile[Type].Value;

            if (HighestUnlockedStage >= 1)
                BBShuriken_Hardmode_Effects.DrawRotatingCopies(Projectile, projectileTexture);

            BBShuriken_Initial_Effects.DrawOutlineAndBody(Projectile, projectileTexture, lightColor);
            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            if (HighestUnlockedStage >= 3)
                BBShuriken_BoomerDuke_Effects.DrawBladeDisc(Projectile);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(stuckInTarget);
            writer.Write(stuckTargetIndex);
            writer.Write(stickTimer);
            writer.Write(sliceEffectTimer);
            writer.WriteVector2(stickOffsetFromTarget);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            stuckInTarget = reader.ReadBoolean();
            stuckTargetIndex = reader.ReadInt32();
            stickTimer = reader.ReadInt32();
            sliceEffectTimer = reader.ReadInt32();
            stickOffsetFromTarget = reader.ReadVector2();
        }

        private void HandleFlightMovement()
        {
            NPC target = TideEmpowered ? FindNearestTarget(900f + HighestUnlockedStage * 140f) : null;

            if (TideEmpowered && target != null && target.active)
            {
                Vector2 desiredDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);

                Projectile.velocity = (
                    Projectile.velocity * 17f +
                    desiredDir * (20f * 1.25f)
                ) / 18f;

                float speed = Projectile.velocity.Length();
                speed = MathHelper.Lerp(speed, 14f, 0.08f);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
            }
            else
            {
                if (Projectile.velocity.LengthSquared() <= 0.01f)
                    Projectile.velocity = Vector2.UnitX * (Projectile.direction == 0 ? 8f : 8f * Projectile.direction);

                Projectile.velocity *= 1.01f;
            }

            if (Projectile.velocity.X != 0f)
                Projectile.direction = Projectile.velocity.X > 0f ? 1 : -1;

            Projectile.rotation += (Projectile.direction <= 0 ? -1f : 1f) * (0.55f + HighestUnlockedStage * 0.04f);
        }

        private void SpawnUnlockedFlightEffects()
        {
            BBShuriken_Initial_Effects.SpawnFlight(Projectile, SizeScale);

            if (HighestUnlockedStage >= 2)
                BBShuriken_Fishron_Effects.SpawnFlight(Projectile, SizeScale);

            if (HighestUnlockedStage >= 3)
                BBShuriken_BoomerDuke_Effects.SpawnFlight(Projectile, SizeScale);
        }

        private void HandleStickyState()
        {
            if (!Main.npc.IndexInRange(stuckTargetIndex))
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[stuckTargetIndex];
            if (!target.active || target.life <= 0 || target.dontTakeDamage)
            {
                Projectile.Kill();
                return;
            }

            Projectile.tileCollide = false;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = target.Center + stickOffsetFromTarget;
            Projectile.rotation += Projectile.direction <= 0 ? -1.35f : 1.35f;

            stickTimer++;
            sliceEffectTimer++;

            BBShuriken_Initial_Effects.SpawnStickyAmbient(Projectile, target, SizeScale, HighestUnlockedStage);

            if (sliceEffectTimer >= 8)
            {
                sliceEffectTimer = 0;
                BBShuriken_Initial_Effects.SpawnStickySliceBurst(Projectile, SizeScale, HighestUnlockedStage);

                if (soundTimer <= 0)
                {
                    SoundEngine.PlaySound(SoundID.Item71 with
                    {
                        Volume = 0.45f,
                        Pitch = Main.rand.NextFloat(0.15f, 0.35f)
                    }, Projectile.Center);

                    soundTimer = 8;
                }
            }

            if (stickTimer >= StickyLifetime)
                Projectile.Kill();
        }

        private NPC FindNearestTarget(float maxDistance)
        {
            NPC closestTarget = null;
            float closestDistance = maxDistance;

            foreach (NPC npc in Main.npc)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestTarget = npc;
            }

            return closestTarget;
        }

        private void EnsureSquareHitbox()
        {
            int sideLength = Math.Max(1, Math.Max(Projectile.width, Projectile.height));
            if (Projectile.width == sideLength && Projectile.height == sideLength)
                return;

            Vector2 center = Projectile.Center;
            Projectile.width = sideLength;
            Projectile.height = sideLength;
            Projectile.Center = center;
        }
    }
}
