using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.LeftGeneral;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.RightGeneral
{
    internal sealed class YC_RightDrone : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YC_Right_Drone";

        private const int ShutdownFrames = 120;

        private static readonly Vector2[] RearOffsets =
        {
            new(-48f, -20f),
            new(48f, -20f)
        };

        public int SlotIndex => (int)Projectile.ai[0];
        public int ParentHoldoutIndex => (int)Projectile.ai[1];
        public bool HeavyCommanded => Projectile.ai[2] == 1f;

        private ref float Timer => ref Projectile.localAI[0];
        private ref float ShutdownTimer => ref Projectile.localAI[1];
        private bool positionInitialized;
        private int shootTimer;

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!TryGetHoldout(out Projectile holdoutProj, out YC_RightCrystalHoldout holdout))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.damage = holdoutProj.damage;
            Timer++;
            if (HeavyCommanded)
                TriggerHeavyAttack();

            bool shutDown = ShutdownTimer > 0f;
            if (shutDown)
                ShutdownTimer--;

            // Movement logic
            Vector2 desiredCenter = Vector2.Zero;
            if (holdoutProj.localAI[0] < 120) // Stage 1: Spiral Orbit
            {
                float phase = holdoutProj.localAI[0] * 0.08f + SlotIndex * MathHelper.Pi;
                float radius = MathHelper.Lerp(180f, 70f, MathHelper.Clamp(holdoutProj.localAI[0] / 120f, 0f, 1f));
                desiredCenter = owner.MountedCenter + phase.ToRotationVector2() * radius;

                // Aim at convergence point
                Vector2 convergePoint = owner.MountedCenter + holdout.ForwardDirection * 240f;
                Vector2 aimDir = (convergePoint - Projectile.Center).SafeNormalize(holdout.ForwardDirection);
                Projectile.rotation = aimDir.ToRotation() + MathHelper.PiOver2;
            }
            else // Stage 2: Combat Rear Orbit
            {
                Vector2 forward = holdout.ForwardDirection;
                Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
                Vector2 localOffset = RearOffsets[Utils.Clamp(SlotIndex, 0, RearOffsets.Length - 1)];
                desiredCenter = owner.MountedCenter + right * localOffset.X + forward * (localOffset.Y - 60f);

                // Aim towards cursor
                Vector2 aimDir = (Main.MouseWorld - Projectile.Center).SafeNormalize(holdout.ForwardDirection);
                Projectile.rotation = aimDir.ToRotation() + MathHelper.PiOver2;

                // Fire small tracking splinters
                shootTimer++;
                if (!shutDown && shootTimer >= 26)
                {
                    shootTimer = 0;
                    if (Projectile.owner == Main.myPlayer)
                    {
                        Vector2 splinterVel = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.16f) * 14f;
                        int splinter = Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            Projectile.Center,
                            splinterVel,
                            ModContent.ProjectileType<YC_BurningShardSplinter>(),
                            (int)(Projectile.damage * 0.35f),
                            Projectile.knockBack * 0.2f,
                            Projectile.owner);
                        if (Main.projectile.IndexInRange(splinter))
                        {
                            YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[splinter], YCWeaponForm.Crystal);
                            Main.projectile[splinter].CritChance = Projectile.CritChance;
                        }
                    }
                    SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.1f, Pitch = 0.2f }, Projectile.Center);
                }

                // Deal damage with thin laser line
                if (!shutDown && Timer % 8 == 0 && Projectile.owner == Main.myPlayer)
                {
                    Vector2 laserDir = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2();
                    Vector2 endPoint = Projectile.Center + laserDir * 1200f;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (npc.active && !npc.friendly && npc.chaseable && !npc.dontTakeDamage)
                        {
                            float colPoint = 0f;
                            if (Collision.CheckAABBvLineCollision(npc.position, new Vector2(npc.width, npc.height), Projectile.Center, endPoint, 16f, ref colPoint))
                            {
                                npc.SimpleStrikeNPC((int)(Projectile.damage * 0.25f), Projectile.direction, false, Projectile.knockBack * 0.1f, DamageClass.Magic);
                            }
                        }
                    }
                }
            }

            if (!positionInitialized)
            {
                Projectile.Center = desiredCenter;
                positionInitialized = true;
            }
            else
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, desiredCenter, 0.2f);
            }

            Projectile.direction = Projectile.spriteDirection = (Projectile.Center.X >= owner.Center.X ? 1 : -1);
            Projectile.scale = 0.88f + 0.04f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.5f + SlotIndex * 0.7f);
        }

        private void TriggerHeavyAttack()
        {
            Projectile.ai[2] = 0f;
            ShutdownTimer = ShutdownFrames;
            shootTimer = -ShutdownFrames;

            if (Projectile.owner == Main.myPlayer)
            {
                // Shoot a heavy tracking missile towards the cursor.
                Vector2 targetDir = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX);
                if (YC_EssenceFlame.CanSpawnMoreFor(Main.player[Projectile.owner]))
                {
                    int bolt = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        targetDir * 20f,
                        ModContent.ProjectileType<YC_EssenceFlame>(),
                        (int)(Projectile.damage * 2.2f),
                        Projectile.knockBack * 1.5f,
                        Projectile.owner,
                        -1f, // target finder
                        0f);
                    if (Main.projectile.IndexInRange(bolt))
                    {
                        // Mark as heavy bolt
                        Main.projectile[bolt].scale = 1.6f;
                        Main.projectile[bolt].extraUpdates = 3;
                        YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[bolt], YCWeaponForm.Crystal);
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    float spread = MathHelper.ToRadians((i - 1.5f) * 5.5f);
                    int splinter = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center + targetDir.RotatedBy(MathHelper.PiOver2) * (i - 1.5f) * 5f,
                        targetDir.RotatedBy(spread) * (18f + i * 1.3f),
                        ModContent.ProjectileType<YC_BurningShardSplinter>(),
                        (int)(Projectile.damage * 0.55f),
                        Projectile.knockBack * 0.35f,
                        Projectile.owner);
                    if (Main.projectile.IndexInRange(splinter))
                    {
                        YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[splinter], YCWeaponForm.Crystal);
                        Main.projectile[splinter].CritChance = Projectile.CritChance;
                    }
                }
            }

            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.4f, Pitch = -0.1f }, Projectile.Center);
            
            // Spawn spark explosion at drone location
            if (!Main.dedServ)
            {
                for (int i = 0; i < 15; i++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, Main.rand.NextVector2Circular(6f, 6f), 0, default, 1.3f);
                    d.noGravity = true;
                }
            }
        }

        private bool TryGetHoldout(out Projectile holdoutProj, out YC_RightCrystalHoldout holdout)
        {
            holdoutProj = null;
            holdout = null;

            if (ParentHoldoutIndex < 0 || ParentHoldoutIndex >= Main.maxProjectiles)
                return false;

            Projectile candidate = Main.projectile[ParentHoldoutIndex];
            if (candidate.active && candidate.type == ModContent.ProjectileType<YC_RightCrystalHoldout>() && candidate.ModProjectile is YC_RightCrystalHoldout holdoutMod)
            {
                holdoutProj = candidate;
                holdout = holdoutMod;
                return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = SpriteEffects.None;

            Main.EntitySpriteDraw(texture, drawPos, null, lightColor, Projectile.rotation, origin, Projectile.scale, effects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, new Color(255, 238, 178, 0) * 0.4f, Projectile.rotation, origin, Projectile.scale * 1.08f, effects, 0);

            if (TryGetHoldout(out Projectile holdoutProj, out YC_RightCrystalHoldout holdout))
            {
                float opacity = Projectile.Opacity;
                Vector2 forwardDir = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2();

                if (holdoutProj.localAI[0] < 120)
                {
                    Vector2 convergePoint = Main.player[Projectile.owner].MountedCenter + holdout.ForwardDirection * 240f;
                    DrawLaserLine(Projectile.Center, convergePoint, Color.Gold * 0.8f * opacity, 2f);
                }
                else
                {
                    Vector2 endPoint = Projectile.Center + forwardDir * 1200f;
                    DrawLaserLine(Projectile.Center, endPoint, Color.Orange * 0.7f * opacity, 2.5f);
                }
            }

            return false;
        }

        private void DrawLaserLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineThick").Value;
            Vector2 diff = end - start;
            float length = diff.Length();
            float rot = diff.ToRotation();
            Vector2 drawPos = start + diff * 0.5f - Main.screenPosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                bloom,
                drawPos,
                null,
                color with { A = 0 },
                rot + MathHelper.PiOver2,
                bloom.Size() * 0.5f,
                new Vector2(width * 0.05f, length / (float)bloom.Height),
                SpriteEffects.None,
                0f);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }
    }
}
