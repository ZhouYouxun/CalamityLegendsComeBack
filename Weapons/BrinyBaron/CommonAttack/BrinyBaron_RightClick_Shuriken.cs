using System.IO;
using CalamityLegendsComeBack.Weapons.BrinyBaron.POWER;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.LeftClick
{
    public class BrinyBaron_RightClick_Shuriken : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/TornadoProj";

        private const int BaseSize = 52;

        private int PresetTier => Utils.Clamp((int)Projectile.ai[0], 1, 3);
        private float PresetScale => PresetTier switch
        {
            1 => 0.76f,
            2 => 1f,
            _ => 1.34f,
        };
        private bool TideEmpowered => Main.player.IndexInRange(Projectile.owner) &&
                                      Main.player[Projectile.owner].active &&
                                      Main.player[Projectile.owner].GetModPlayer<BBEXPlayer>().TideFull;

        private bool stuckInTarget;
        private int stuckTargetIndex = -1;
        private int stickTimer;
        private int sliceEffectTimer;
        private int soundTimer;
        private bool presetInitialized;
        private Vector2 stickOffsetFromTarget = Vector2.Zero;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

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

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Projectile.ai[0] < 1f)
                Projectile.ai[0] = 1f;

            ApplyPresetScale();
        }

        public override void AI()
        {
            if (!presetInitialized)
                ApplyPresetScale();

            Projectile.alpha = Utils.Clamp(Projectile.alpha - 20, 0, 255);
            Lighting.AddLight(Projectile.Center, 0.05f, 0.22f, 0.32f);

            if (soundTimer > 0)
                soundTimer--;

            if (!stuckInTarget)
                DoFlyingAI();
            else
                DoStickySlashAI();
        }

        private void DoFlyingAI()
        {
            if (TideEmpowered)
            {
                NPC target = FindNearestTarget(840f + 140f * (PresetTier - 1));
                if (target != null)
                {
                    float homingSpeed = 14.5f + 0.8f * (PresetTier - 1);
                    float homingStrength = 0.075f + 0.018f * (PresetTier - 1);
                    Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * homingSpeed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, homingStrength);
                }
            }

            if (Projectile.velocity.X != 0f)
                Projectile.direction = Projectile.velocity.X > 0f ? 1 : -1;

            float spin = Projectile.direction <= 0 ? -0.55f : 0.55f;
            Projectile.rotation += spin;

            if (PresetTier >= 1)
                SpawnPresetOneFlightEffects();

            if (PresetTier >= 2)
                SpawnPresetTwoFlightEffects();

            if (PresetTier >= 3)
                SpawnPresetThreeFlightEffects();
        }

        private void SpawnPresetOneFlightEffects()
        {
            if (!Main.rand.NextBool(3))
                return;

            Vector2 velocityDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 backPos = Projectile.Center - velocityDirection * Main.rand.NextFloat(6f, 14f);
            Vector2 mistVelocity = -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.5f, 0.5f);

            Dust water = Dust.NewDustPerfect(backPos, DustID.Water, mistVelocity);
            water.noGravity = true;
            water.scale = Main.rand.NextFloat(0.85f, 1.15f) * Projectile.scale;

            if (Main.rand.NextBool(2))
            {
                Dust frost = Dust.NewDustPerfect(backPos, DustID.Frost, mistVelocity * 0.65f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.75f, 1f) * Projectile.scale;
            }

            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        backPos,
                        mistVelocity * 0.25f,
                        false,
                        6,
                        0.42f * Projectile.scale,
                        Color.DeepSkyBlue,
                        true,
                        false,
                        true));
            }
        }

        private void SpawnPresetTwoFlightEffects()
        {
            if (!Main.rand.NextBool(2))
                return;

            Vector2 backPos = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(8f, 18f);

            Dust water = Dust.NewDustPerfect(backPos, DustID.Water, -Projectile.velocity * 0.15f);
            water.noGravity = true;
            water.scale = Main.rand.NextFloat(1f, 1.4f) * Projectile.scale;

            Dust frost = Dust.NewDustPerfect(backPos, DustID.Frost, -Projectile.velocity * 0.08f);
            frost.noGravity = true;
            frost.scale = Main.rand.NextFloat(0.9f, 1.2f) * Projectile.scale;

            if (Main.rand.NextBool(3))
            {
                Dust gem = Dust.NewDustPerfect(backPos, DustID.GemSapphire, -Projectile.velocity * 0.05f);
                gem.noGravity = true;
                gem.scale = Main.rand.NextFloat(0.8f, 1.1f) * Projectile.scale;
            }

            if (Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(
                    new SparkParticle(
                        backPos,
                        -Projectile.velocity * 0.03f,
                        false,
                        5,
                        1.2f * Projectile.scale,
                        Color.SeaGreen,
                        true));
            }
        }

        private void SpawnPresetThreeFlightEffects()
        {
            if (Main.rand.NextBool(3))
            {
                Vector2 velocityDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 right = velocityDirection.RotatedBy(MathHelper.PiOver2);
                float helix = (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 18f + Projectile.identity * 0.6f);
                Vector2 spawnPos = Projectile.Center - velocityDirection * Main.rand.NextFloat(10f, 22f) + right * helix * 7f;
                Vector2 sparkVelocity = -Projectile.velocity * 0.06f + right * helix * 0.55f;

                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        spawnPos,
                        sparkVelocity,
                        false,
                        8,
                        0.62f * Projectile.scale,
                        Main.rand.NextBool() ? Color.Cyan : Color.DeepSkyBlue,
                        true,
                        false,
                        true));

                GeneralParticleHandler.SpawnParticle(
                    new HeavySmokeParticle(
                        spawnPos,
                        sparkVelocity * 0.35f,
                        Color.WhiteSmoke,
                        12,
                        0.62f * Projectile.scale,
                        0.28f,
                        Main.rand.NextFloat(-0.04f, 0.04f),
                        false));
            }
        }

        private void DoStickySlashAI()
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

            float spin = Projectile.direction <= 0 ? -1.35f : 1.35f;
            Projectile.rotation += spin;

            stickTimer++;
            sliceEffectTimer++;

            SpawnStickyAmbientEffects(target);

            if (sliceEffectTimer >= 8)
            {
                sliceEffectTimer = 0;

                SpawnStickySliceBurst();

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

            if (stickTimer >= 72)
                Projectile.Kill();
        }

        private void SpawnStickyAmbientEffects(NPC target)
        {
            int ambientCount = PresetTier >= 2 ? 2 : 1;
            for (int i = 0; i < ambientCount; i++)
            {
                Vector2 dustVel = Main.rand.NextVector2Circular(3.5f, 3.5f) + target.velocity * 0.2f;

                Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, dustVel);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.9f, 1.35f) * Projectile.scale;

                if (PresetTier >= 2 || Main.rand.NextBool())
                {
                    Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, dustVel * 0.8f);
                    water.noGravity = true;
                    water.scale = Main.rand.NextFloat(1f, 1.45f) * Projectile.scale;
                }
            }

            if (PresetTier >= 3 && Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                        target.velocity * 0.08f,
                        false,
                        8,
                        0.55f * Projectile.scale,
                        Color.LightSkyBlue,
                        true,
                        false,
                        true));
            }
        }

        private void SpawnStickySliceBurst()
        {
            int burstCount = PresetTier >= 2 ? 6 : 4;
            for (int i = 0; i < burstCount; i++)
            {
                Vector2 burstVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 6.5f);

                Dust gem = Dust.NewDustPerfect(Projectile.Center, DustID.GemSapphire, burstVel);
                gem.noGravity = true;
                gem.scale = Main.rand.NextFloat(1f, 1.6f) * Projectile.scale;

                Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, burstVel * 0.7f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.9f, 1.35f) * Projectile.scale;
            }

            if (PresetTier >= 3)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 sparkVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 5.5f);
                    GeneralParticleHandler.SpawnParticle(
                        new SparkParticle(
                            Projectile.Center,
                            sparkVelocity,
                            false,
                            6,
                            1.55f * Projectile.scale,
                            i == 0 ? Color.DeepSkyBlue : Color.Cyan,
                            true));
                }
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

            TrySpawnTideTyphoon(target);

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

                for (int i = 0; i < 12; i++)
                {
                    Vector2 burstVel = Main.rand.NextVector2Circular(5f, 5f);

                    Dust water = Dust.NewDustPerfect(target.Center, DustID.Water, burstVel);
                    water.noGravity = true;
                    water.scale = Main.rand.NextFloat(1.2f, 1.7f) * Projectile.scale;

                    Dust frost = Dust.NewDustPerfect(target.Center, DustID.Frost, burstVel * 0.8f);
                    frost.noGravity = true;
                    frost.scale = Main.rand.NextFloat(1f, 1.4f) * Projectile.scale;
                }

                if (PresetTier >= 3)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 orbVelocity = Main.rand.NextVector2Circular(1.4f, 1.4f);
                        GeneralParticleHandler.SpawnParticle(
                            new GlowOrbParticle(
                                target.Center + Main.rand.NextVector2Circular(10f, 10f),
                                orbVelocity,
                                false,
                                10,
                                0.65f * Projectile.scale,
                                i % 2 == 0 ? Color.DeepSkyBlue : Color.Cyan,
                                true,
                                false,
                                true));
                    }
                }

                SoundEngine.PlaySound(SoundID.Item39 with
                {
                    Volume = 0.7f,
                    Pitch = Main.rand.NextFloat(-0.1f, 0.15f)
                }, target.Center);

                Projectile.netUpdate = true;
            }
            else
            {
                int sliceCount = PresetTier >= 3 ? 6 : 4;
                for (int i = 0; i < sliceCount; i++)
                {
                    Vector2 sliceVel = Main.rand.NextVector2Circular(4f, 4f);

                    Dust gem = Dust.NewDustPerfect(target.Center, DustID.GemSapphire, sliceVel);
                    gem.noGravity = true;
                    gem.scale = Main.rand.NextFloat(1f, 1.3f) * Projectile.scale;
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 burstVel = Main.rand.NextVector2Circular(4f, 4f);

                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, burstVel);
                water.noGravity = true;
                water.scale = Main.rand.NextFloat(1f, 1.5f);

                Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, burstVel * 0.8f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.9f, 1.3f);
            }

            SoundEngine.PlaySound(SoundID.Item27 with
            {
                Volume = 0.5f,
                Pitch = 0.25f
            }, Projectile.Center);

            return true;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 14; i++)
            {
                Vector2 burstVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f);

                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, burstVel);
                water.noGravity = true;
                water.scale = Main.rand.NextFloat(1.1f, 1.6f) * Projectile.scale;

                Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, burstVel * 0.85f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(1f, 1.4f) * Projectile.scale;

                if (Main.rand.NextBool())
                {
                    Dust gem = Dust.NewDustPerfect(Projectile.Center, DustID.GemSapphire, burstVel * 0.6f);
                    gem.noGravity = true;
                    gem.scale = Main.rand.NextFloat(0.9f, 1.2f) * Projectile.scale;
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
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            float trailScaleBoost = PresetTier >= 2 ? 1f : 0.84f;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float factor = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(Color.DeepSkyBlue, Color.Cyan, factor) * factor * 0.45f;

                Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, drawPos, null, trailColor, Projectile.rotation, origin, Projectile.scale * trailScaleBoost * (0.92f + factor * 0.12f), SpriteEffects.None, 0);
            }

            if (PresetTier >= 3)
            {
                for (int i = 0; i < Projectile.oldPos.Length; i += 2)
                {
                    float factor = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                    Color trailColor = Color.Lerp(Color.White, Color.LightSkyBlue, 0.55f) * factor * 0.22f;
                    Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                    Main.EntitySpriteDraw(tex, drawPos, null, trailColor, -Projectile.rotation * 0.65f, origin, Projectile.scale * (1.02f + factor * 0.18f), SpriteEffects.None, 0);
                }
            }

            return true;
        }

        public override void PostDraw(Color lightColor)
        {
            Asset<Texture2D> smearRound = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearSmokey");
            Asset<Texture2D> smearHalf = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmearSwipe");

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float strength = stuckInTarget ? 1.15f : 0.8f;
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;

            if (PresetTier >= 2)
            {
                Main.EntitySpriteDraw(
                    smearHalf.Value,
                    drawPos,
                    null,
                    (Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan) with { A = 0 } * 0.55f * strength,
                    Projectile.rotation * Main.rand.NextFloat(1.55f, 1.72f),
                    smearHalf.Size() * 0.5f,
                    Main.rand.NextFloat(1.25f, 1.55f) * strength * Projectile.scale,
                    SpriteEffects.None,
                    0);

                Main.EntitySpriteDraw(
                    smearRound.Value,
                    drawPos,
                    null,
                    (Main.rand.NextBool() ? Color.LightSeaGreen : Color.CornflowerBlue) with { A = 0 } * 0.65f * strength,
                    Projectile.rotation * Main.rand.NextFloat(1.15f, 1.32f),
                    smearRound.Size() * 0.5f,
                    Main.rand.NextFloat(1.05f, 1.35f) * strength * Projectile.scale,
                    SpriteEffects.None,
                    0);
            }

            if (PresetTier >= 3)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * (stuckInTarget ? 4.8f : 2.8f) * Projectile.scale;
                    Color auraColor = Color.Lerp(Color.Cyan, Color.DeepSkyBlue, i / 8f) * (0.22f + 0.05f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 14f + i));
                    Main.EntitySpriteDraw(tex, drawPos + offset, null, auraColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
                }
            }
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

        private void ApplyPresetScale()
        {
            Vector2 center = Projectile.Center;
            int scaledSize = (int)(BaseSize * PresetScale);
            Projectile.width = scaledSize;
            Projectile.height = scaledSize;
            Projectile.scale = PresetScale;
            Projectile.Center = center;
            presetInitialized = true;
        }

        private void TrySpawnTideTyphoon(NPC target)
        {
            if (!TideEmpowered || Main.myPlayer != Projectile.owner || !Main.rand.NextBool(3))
                return;

            Player player = Main.player[Projectile.owner];
            if (player.ownedProjectileCounts[ModContent.ProjectileType<BrinySpout>()] != 0)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<BrinyTyphoonBubble>(),
                Projectile.damage,
                Projectile.knockBack,
                player.whoAmI);
        }
    }
}
