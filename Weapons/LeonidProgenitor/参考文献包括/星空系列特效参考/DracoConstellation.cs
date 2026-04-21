using System;
using System.Collections.Generic;
using System.Linq;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Utilities.Daybreak;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Magic
{
    public class DracoConstellation : ModProjectile, ILocalizedModType
    {

        public new string LocalizationCategory => "Projectiles.Magic";
        public override string Texture => "CalamityMod/Particles/Sparkle";
        public float TailLength => 600;
        public List<DracoSegment> Segments = new();
        /// <summary>
        /// Do not reference this field directly. Use GetGlowTex() instead.
        /// </summary>
        private static Asset<Texture2D> GlowTex = null;
        public static Texture2D GetGlowTex()
        {
            if (GlowTex == null)
            {
                GlowTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            }
            return GlowTex.Value;
        }
        public class DracoSegment
        {
            public Vector2 Center = Vector2.Zero;
            public float rotation = 0;
            public Vector2 velocity = Vector2.Zero;
            public Vector2 size = Vector2.One;
            public float followDistance = 50;
            public float PoseRotation = 0;
            public DracoSegment(Projectile Head, float followingDistance = 50)
            {
                Center = Head.Center;
                rotation = Head.rotation;
                velocity = Head.velocity;
                followDistance = followingDistance;
            }

            public DracoSegment(DracoConstellation Head, float followingDistance, float rotation)
            {
                var prior = Head.Segments.LastOrDefault(new DracoSegment(Head.Projectile));
                Center = prior.Center + rotation.ToRotationVector2() * (followingDistance * 600f);
                this.rotation = prior.rotation;
                velocity = prior.velocity;
                followDistance = followingDistance;
                PoseRotation = rotation;
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.localNPCHitCooldown = 15 * Projectile.MaxUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.timeLeft = 300;
        }

        public override void AI()
        {
            if (Main.player[Projectile.owner].Calamity().StratusStarburst < 1 && Projectile.timeLeft > 60)
            {
                Projectile.timeLeft = 60;
                return;
            }
            if (Projectile.FinalExtraUpdate() && Main.player[Projectile.owner].miscCounter % 15 == 0 &&
                Projectile.timeLeft > 60)
            {
                Main.player[Projectile.owner].Calamity().StratusStarburst--;
                Main.player[Projectile.owner].Calamity().StratusStarburstResetTimer = (int)MathHelper.Max(Main.player[Projectile.owner].Calamity().StratusStarburstResetTimer, 240);
            }
            if (Segments.Count <= 0)
            {
                //These segment lengths were gotten by getting the length between each star from Draco's Wikipedia diagam in pixels, then converting to a percentage of the total length.
                //This ignores the "head" of draco in those calculations. The values here are multiplied by TailLength to get the full length of Draco in game.
                Segments = new List<DracoSegment>();
                Segments.Add(new(this, 0.1985f, MathHelper.TwoPi * 0.74f));
                Segments.Add(new(this, 0.0835f, MathHelper.TwoPi * 0.01f));
                Segments.Add(new(this, 0.1209f, MathHelper.TwoPi * 0.2f));
                Segments.Add(new(this, 0.0938f, MathHelper.TwoPi * 0.15f));
                Segments.Add(new(this, 0.0571f, MathHelper.TwoPi * 0.15f));
                Segments.Add(new(this, 0.0711f, MathHelper.TwoPi * 0.99f));
                Segments.Add(new(this, 0.1560f, MathHelper.TwoPi * 0.9f));
                Segments.Add(new(this, 0.1458f, MathHelper.TwoPi * 0.85f));
                Segments.Add(new(this, 0.0733f, MathHelper.TwoPi * 0.9f));
            }
            ;
            if (Projectile.timeLeft <= 240 && Projectile.timeLeft > 60)
            {
                Projectile.timeLeft++;
                NPC target = null;
                if (Projectile.ai[2] > 0)
                {
                    target = Main.npc[(int)Projectile.ai[2]];
                }
                else if (Main.player[Projectile.owner].HasMinionAttackTargetNPC)
                {
                    target = Main.npc[Main.player[Projectile.owner].MinionAttackTargetNPC];
                }
                float targetDistance = 1600;
                if (target == null || !target.CanBeChasedBy(Projectile))
                    target = null;
                if (target == null)
                    foreach (var item in Main.ActiveNPCs)
                    {
                        if (!item.CanBeChasedBy(Projectile) || Projectile.localNPCImmunity[item.whoAmI] > 0 || item.Distance(Main.player[Projectile.owner].Center) > 1200)
                            continue;
                        var dis = Projectile.Distance(item.Center);
                        if (dis < targetDistance)
                        {
                            targetDistance = dis;
                            target = item;
                        }
                    }
                if (target != null)
                {
                    float velLength = Projectile.velocity.Length();
                    Projectile.velocity += Projectile.DirectionTo(target.Center) * 0.5f;
                    Projectile.ai[2] = target.whoAmI + 1;
                }
                else
                {
                    Projectile.ai[2] = 0;
                    if (Projectile.Distance(Main.player[Projectile.owner].Center) > 300)
                    {
                        float velLength = Projectile.velocity.Length();
                        Projectile.velocity += Projectile.DirectionTo(Main.player[Projectile.owner].Center) * 0.5f;
                    }
                }
                if (Projectile.velocity.Length() > 5)
                    Projectile.velocity *= 0.98f;

                if (Projectile.velocity != Vector2.Zero)
                    Projectile.rotation = Projectile.velocity.ToRotation();
            }
            else if (Projectile.timeLeft > 60)
            {

                Projectile.rotation = MathHelper.PiOver2;
                Projectile.scale = MathHelper.Lerp(1, 0, (Projectile.timeLeft - 240) / 60f);
            }
            else
            {
                Projectile.scale = MathHelper.Lerp(0, 1, (Projectile.timeLeft) / 60f);
                Projectile.velocity *= 0.95f;
            }
            Projectile.position += Projectile.velocity;
            if (Projectile.timeLeft <= 241 && Projectile.velocity != Vector2.Zero)
                for (int i = 0; i < Segments.Count; i++)
                {
                    float segmentDistance = Segments[i].followDistance * TailLength;
                    var thisSeg = Segments[i];
                    var aheadSeg = new DracoSegment(Projectile);
                    if (i != 0)
                    {
                        aheadSeg = Segments[i - 1];
                    }
                    float intensity = 0.05f;


                    Vector2 nexSegDir = aheadSeg.Center - thisSeg.Center;
                    if (aheadSeg.rotation != thisSeg.rotation)
                    {
                        nexSegDir = nexSegDir.RotatedBy(MathHelper.WrapAngle(aheadSeg.rotation - thisSeg.rotation) * intensity);
                        nexSegDir = nexSegDir.MoveTowards((aheadSeg.rotation - thisSeg.rotation).ToRotationVector2(), 1f);
                    }
                    thisSeg.rotation = nexSegDir.ToRotation();

                    thisSeg.Center = aheadSeg.Center - nexSegDir.SafeNormalize(Vector2.Zero) * segmentDistance;

                }
            Projectile.position -= Projectile.velocity;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (var i = 0; i < Segments.Count; i++)
            {
                var prevSeg = new DracoSegment(Projectile);
                if (i != 0)
                {
                    prevSeg = Segments[i - 1];
                }
                float cpoint = 0;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), prevSeg.Center, Segments[i].Center, 4, ref cpoint))
                {
                    return true;
                }
            }
            return base.Colliding(projHitbox, targetHitbox);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            using (Main.spriteBatch.Scope())
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,SamplerState.PointClamp,DepthStencilState.None,Main.Rasterizer,null,Main.Transform);
                var tex = TextureAssets.Projectile[Type].Value;
                var connectioncolor = Color.SkyBlue * ((MathF.Sin(Main.GlobalTimeWrappedHourly) + 1) * 0.125f + 0.75f);
                for (var i = 0; i < Segments.Count; i++)
                {
                    if (i == 0)
                    {
                        CalamityUtils.DrawLineBetter(Main.spriteBatch, Projectile.Center, Segments[i].Center, connectioncolor, 2 * Projectile.scale);
                    }
                    if (Segments.IndexInRange(i + 1))
                    {
                        var nextSegment = Segments[i + 1];
                        CalamityUtils.DrawLineBetter(Main.spriteBatch, Segments[i].Center, nextSegment.Center, connectioncolor, 2 * Projectile.scale);
                    }
                    Main.spriteBatch.Draw(GetGlowTex(), Segments[i].Center - Main.screenPosition, null, Color.SkyBlue, 0, GetGlowTex().Size() * 0.5f, 0.2f * Projectile.scale, SpriteEffects.None, 0);

                    Main.spriteBatch.Draw(tex, Segments[i].Center - Main.screenPosition, null, Color.White, 0 * Segments[i].followDistance * 5f * Main.GlobalTimeWrappedHourly, tex.Size() * 0.5f, 0.75f * Projectile.scale, SpriteEffects.None, 0);
                }
                void DrawHeadStar(Vector2 offsetPercentage)
                {
                    Main.spriteBatch.Draw(GetGlowTex(), Projectile.Center + offsetPercentage.RotatedBy(Projectile.rotation - MathHelper.PiOver2) * TailLength - Main.screenPosition, null, Color.SkyBlue, 0, GetGlowTex().Size() * 0.5f, 0.2f * Projectile.scale, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(tex, Projectile.Center + offsetPercentage.RotatedBy(Projectile.rotation - MathHelper.PiOver2) * TailLength - Main.screenPosition, null, Color.White, 0 * Math.Abs(offsetPercentage.X) * 5f * Main.GlobalTimeWrappedHourly, tex.Size() * 0.5f, 0.75f * Projectile.scale, SpriteEffects.None, 0);
                }
                void connectOffsets(Vector2 offset1, Vector2 offset2)
                {
                    CalamityUtils.DrawLineBetter(Main.spriteBatch, Projectile.Center + offset1.RotatedBy(Projectile.rotation - MathHelper.PiOver2) * TailLength, Projectile.Center + offset2.RotatedBy(Projectile.rotation - MathHelper.PiOver2) * TailLength, connectioncolor, 2 * Projectile.scale);

                }
                connectOffsets(new(), new(-0.0425f, 0.0637f));
                connectOffsets(new(), new(0.0307f, 0.0418f));
                connectOffsets(new(-0.0425f, 0.0637f), new(0.0168f, 0.0791f));
                connectOffsets(new(0.0307f, 0.0418f), new(0.0168f, 0.0791f));
                DrawHeadStar(new());
                DrawHeadStar(new(-0.0425f, 0.0637f));
                DrawHeadStar(new(0.0307f, 0.0418f));
                DrawHeadStar(new(0.0168f, 0.0791f));
                Main.spriteBatch.End();
            }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 60);
        }
    }
}
