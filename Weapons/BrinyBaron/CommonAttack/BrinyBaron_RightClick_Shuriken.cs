using System.IO;
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

        private bool Empowered => Projectile.ai[0] == 1f;

        private bool stuckInTarget;
        private int stuckTargetIndex = -1;
        private int stickTimer;
        private int sliceEffectTimer;
        private int soundTimer;
        private Vector2 stickOffsetFromTarget = Vector2.Zero;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 52;
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

        public override void AI()
        {
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
            if (Empowered)
            {
                NPC target = FindNearestTarget(900f);
                if (target != null)
                {
                    Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 15f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.08f);
                }
            }

            if (Projectile.velocity.X != 0f)
                Projectile.direction = Projectile.velocity.X > 0f ? 1 : -1;

            float spin = Projectile.direction <= 0 ? -0.55f : 0.55f;
            Projectile.rotation += spin;

            if (Main.rand.NextBool(2))
            {
                Vector2 backPos = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(8f, 18f);

                Dust water = Dust.NewDustPerfect(backPos, DustID.Water, -Projectile.velocity * 0.15f);
                water.noGravity = true;
                water.scale = Main.rand.NextFloat(1f, 1.4f);

                Dust frost = Dust.NewDustPerfect(backPos, DustID.Frost, -Projectile.velocity * 0.08f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.9f, 1.2f);

                if (Main.rand.NextBool(3))
                {
                    Dust gem = Dust.NewDustPerfect(backPos, DustID.GemSapphire, -Projectile.velocity * 0.05f);
                    gem.noGravity = true;
                    gem.scale = Main.rand.NextFloat(0.8f, 1.1f);
                }
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

            for (int i = 0; i < 2; i++)
            {
                Vector2 dustVel = Main.rand.NextVector2Circular(3.5f, 3.5f) + target.velocity * 0.2f;

                Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, dustVel);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(1f, 1.4f);

                if (Main.rand.NextBool())
                {
                    Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, dustVel * 0.8f);
                    water.noGravity = true;
                    water.scale = Main.rand.NextFloat(1.1f, 1.5f);
                }
            }

            if (sliceEffectTimer >= 8)
            {
                sliceEffectTimer = 0;

                for (int i = 0; i < 6; i++)
                {
                    Vector2 burstVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 6.5f);

                    Dust gem = Dust.NewDustPerfect(Projectile.Center, DustID.GemSapphire, burstVel);
                    gem.noGravity = true;
                    gem.scale = Main.rand.NextFloat(1.2f, 1.6f);

                    Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, burstVel * 0.7f);
                    frost.noGravity = true;
                    frost.scale = Main.rand.NextFloat(1f, 1.4f);
                }

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

        public override bool? CanHitNPC(NPC target)
        {
            if (stuckInTarget)
                return target.whoAmI == stuckTargetIndex;

            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 180);

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
                    water.scale = Main.rand.NextFloat(1.2f, 1.7f);

                    Dust frost = Dust.NewDustPerfect(target.Center, DustID.Frost, burstVel * 0.8f);
                    frost.noGravity = true;
                    frost.scale = Main.rand.NextFloat(1f, 1.4f);
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
                for (int i = 0; i < 4; i++)
                {
                    Vector2 sliceVel = Main.rand.NextVector2Circular(4f, 4f);

                    Dust gem = Dust.NewDustPerfect(target.Center, DustID.GemSapphire, sliceVel);
                    gem.noGravity = true;
                    gem.scale = Main.rand.NextFloat(1f, 1.3f);
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
            if (Empowered && Main.myPlayer == Projectile.owner)
            {
                int typhoonType = ModContent.Find<ModProjectile>("CalamityMod/BrinyTyphoonBubble").Type;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    typhoonType,
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner);
            }

            for (int i = 0; i < 14; i++)
            {
                Vector2 burstVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f);

                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, burstVel);
                water.noGravity = true;
                water.scale = Main.rand.NextFloat(1.1f, 1.6f);

                Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, burstVel * 0.85f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(1f, 1.4f);

                if (Main.rand.NextBool())
                {
                    Dust gem = Dust.NewDustPerfect(Projectile.Center, DustID.GemSapphire, burstVel * 0.6f);
                    gem.noGravity = true;
                    gem.scale = Main.rand.NextFloat(0.9f, 1.2f);
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

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float factor = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(Color.DeepSkyBlue, Color.Cyan, factor) * factor * 0.45f;

                Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, drawPos, null, trailColor, Projectile.rotation, origin, Projectile.scale * (0.92f + factor * 0.12f), SpriteEffects.None, 0);
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
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * (stuckInTarget ? 4.5f : 2.5f);
                Color auraColor = Color.Lerp(Color.Cyan, Color.DeepSkyBlue, i / 8f) * 0.25f;
                Main.EntitySpriteDraw(tex, drawPos + offset, null, auraColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(
                smearHalf.Value,
                drawPos,
                null,
                (Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan) with { A = 0 } * 0.55f * strength,
                Projectile.rotation * Main.rand.NextFloat(1.55f, 1.72f),
                smearHalf.Size() * 0.5f,
                Main.rand.NextFloat(1.25f, 1.55f) * strength,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                smearRound.Value,
                drawPos,
                null,
                (Main.rand.NextBool() ? Color.LightSeaGreen : Color.CornflowerBlue) with { A = 0 } * 0.65f * strength,
                Projectile.rotation * Main.rand.NextFloat(1.15f, 1.32f),
                smearRound.Size() * 0.5f,
                Main.rand.NextFloat(1.05f, 1.35f) * strength,
                SpriteEffects.None,
                0);
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
    }
}
