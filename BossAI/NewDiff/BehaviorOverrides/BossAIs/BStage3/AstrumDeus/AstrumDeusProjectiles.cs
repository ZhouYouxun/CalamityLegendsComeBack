using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.WeaponAttacks;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.AstrumDeus
{
    // Shared astral burst, matching the established Fx standard (fadeIn bloom + upward-biased scatter).
    internal static class DeusFx
    {
        public static void Burst(Vector2 position, float speed, int count, int dustType = DustID.PurpleTorch)
        {
            for (int i = 0; i < count; i++)
            {
                Dust d = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(40f, 40f), dustType);
                d.velocity = Main.rand.NextVector2Circular(speed, speed) - Vector2.UnitY * 1.2f;
                d.scale = Main.rand.NextFloat(1.2f, 1.6f);
                d.fadeIn = 1.5f;
                d.noGravity = true;
            }
        }

        public static void DrawBackglow(SpriteBatch sb, Texture2D tex, Vector2 pos, Rectangle? frame, float rotation, Vector2 origin, float scale, Color glow)
        {
            glow.A = 0;
            for (int i = 0; i < 12; i++)
            {
                Vector2 off = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 3.5f;
                sb.Draw(tex, pos + off, frame, glow, rotation, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }

    // =====================================================================================================================
    // MICROWAVE — A: a 30-degree sweeping beam that shoves the player toward the star-rain edge (documented).
    //             B: a narrow straight piercing beam that actually deals real damage, no knockback theatrics.
    // =====================================================================================================================
    public class MicrowaveBeamProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 110;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.knockBack = 0f;
        }

        public override void AI()
        {
            NPC owner = Main.npc[(int)Projectile.ai[0]];
            if (!owner.active) { Projectile.Kill(); return; }
            bool sweeping = Projectile.ai[2] != 0f;
            float baseAngle = Projectile.ai[1];
            float angle = sweeping ? baseAngle + MathF.Sin(Projectile.localAI[0] * 0.05f) * 0.26f : baseAngle;
            Projectile.localAI[0]++;
            Projectile.Center = owner.Center;
            Projectile.rotation = angle;
            Projectile.knockBack = sweeping ? 6f : 0f;

            if (Main.rand.NextBool(2))
            {
                Vector2 tip = Projectile.Center + angle.ToRotationVector2() * Main.rand.NextFloat(60f, 780f);
                Dust d = Dust.NewDustPerfect(tip, DustID.OrangeTorch, Vector2.Zero, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 tip = Projectile.Center + Projectile.rotation.ToRotationVector2() * 900f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, tip, 22f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 150, 40, 0) * 0.4f, Projectile.rotation, new Vector2(0f, 0.5f), new Vector2(900f, 44f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 200, 120), Projectile.rotation, new Vector2(0f, 0.5f), new Vector2(900f, 14f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // STAR SPUTTER — golden star-sand drifts outward, then either straight-line reels back (A) or spirals
    // inward toward the player (B).
    // =====================================================================================================================
    public class StarSputterProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/SputterComet";

        public override bool? CanDamage() => Projectile.localAI[0] >= 40f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 200;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            bool spiral = Projectile.ai[2] != 0f;

            if (Projectile.localAI[0] < 40f)
            {
                Projectile.velocity *= 0.95f;
                Projectile.rotation += 0.1f;
            }
            else
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (target.active)
                {
                    if (!spiral)
                    {
                        Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 15f;
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.05f);
                    }
                    else
                    {
                        float ang = (Projectile.Center - target.Center).ToRotation() - 0.1f;
                        float dist = MathHelper.Lerp((Projectile.Center - target.Center).Length(), 0f, 0.03f);
                        Vector2 desired = target.Center + ang.ToRotationVector2() * dist;
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, (desired - Projectile.Center) * 0.2f, 0.1f);
                    }
                }
                Projectile.rotation = Projectile.velocity.ToRotation();
            }

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.06f, 100, default, 0.85f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            bool armed = Projectile.localAI[0] >= 40f;
            Color tint = armed ? Color.White : Color.Lerp(Color.White, new Color(120, 200, 255), 0.5f);
            DeusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(230, 200, 60));
            Main.spriteBatch.Draw(tex, pos, null, tint, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // STAR SHOWER — 6 warning lanes divide the arena; big star bombs fall odd-then-even (A) or reversed/faster (B).
    // =====================================================================================================================
    public class ColumnStarProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Typeless/AstralStar";
        private const int TelegraphTime = 34;

        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] >= TelegraphTime)
                Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.2f, Projectile.ai[0]);
            Projectile.rotation += 0.06f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, new Vector2(0f, -1f), 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f);
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;

            if (t < 1f)
            {
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Main.spriteBatch.Draw(pixel, pos - new Vector2(0f, 300f), new Rectangle(0, 0, 1, 1), new Color(160, 60, 220) * (0.25f + 0.25f * t), 0f, new Vector2(0.5f), new Vector2(4f, 600f), SpriteEffects.None, 0f);
            }

            DeusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // STARSPAWN HELIX — twin phantom strands weave a double-helix; only the crossing points are dangerous.
    // =====================================================================================================================
    public class StarspawnHelixProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Boss/AstralLaser";

        public override bool? CanDamage() => Projectile.localAI[0] >= 20f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 220;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC anchor = Main.npc[(int)Projectile.ai[0]];
            if (!anchor.active) { Projectile.Kill(); return; }

            float phase = Projectile.ai[1] + Projectile.localAI[0] * 0.045f;
            float radius = Projectile.ai[2] <= 0f ? 130f : Projectile.ai[2];
            Vector2 desired = anchor.Center + phase.ToRotationVector2() * radius + new Vector2(0f, -Projectile.localAI[0] * 1.3f);
            Projectile.Center = Vector2.Lerp(Projectile.Center, desired, 0.2f);
            Projectile.rotation += 0.15f;

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Vector2.Zero, 100, default, 0.9f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            DeusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // REGULUS RIOT — star-cores launch, pause, then burst outward in a 4-way cross.
    // =====================================================================================================================
    public class RegulusCoreProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/RegulusRiot";

        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 160;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.15f;
            if (Projectile.localAI[0] < Projectile.ai[0])
            {
                Projectile.velocity *= 0.92f;
            }
            else if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                Projectile.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.5f }, Projectile.Center);
                DeusFx.Burst(Projectile.Center, 5f, 12);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 vel = (i * MathHelper.PiOver2).ToRotationVector2() * 11f;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<RegulusEnergyProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            bool armed = Projectile.localAI[1] >= 1f;
            float readyT = MathHelper.Clamp((Projectile.localAI[0] - (Projectile.ai[0] - 10f)) / 10f, 0f, 1f);
            Color tint = armed ? new Color(120, 200, 255) : Color.Lerp(Color.White, new Color(255, 220, 120), readyT);
            DeusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(230, 200, 60));
            Main.spriteBatch.Draw(tex, pos, null, tint, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class RegulusEnergyProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.06f, 100, default, 0.8f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 220, 120, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(26f, 8f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 240, 190), rot, new Vector2(0.5f), new Vector2(18f, 4f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // ASTRAL PIKE — a dragon dashes with the pike extended; on boundary impact it bursts into 8 piercing lasers.
    // =====================================================================================================================
    public class AstralPikeProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/Spears/AstralPikeProj";

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 90;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.08f, 100, default, 1f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
            if (Projectile.timeLeft <= 30 && Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Detonate();
            }
        }

        public override void OnKill(int timeLeft) { if (Projectile.localAI[0] == 0f) Detonate(); }

        private void Detonate()
        {
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f }, Projectile.Center);
            DeusFx.Burst(Projectile.Center, 6f, 20);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 13f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<RegulusEnergyProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            DeusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(230, 200, 60));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // ASTRAL BLASTER — horizontal blaster rounds bounce off the arena boundary, splitting into star fragments.
    // =====================================================================================================================
    public class AstralRoundProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/AstralRound";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 140;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.2f;
            float half = Projectile.ai[0];
            if (half > 0f && Projectile.localAI[0] == 0f && Math.Abs(Projectile.Center.X - Projectile.ai[1]) > half)
            {
                Projectile.localAI[0] = 1f;
                Projectile.velocity.X *= -1f;
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.3f }, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 vel = Projectile.velocity.RotatedBy((i - 1) * 0.4f) * 0.7f;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<RegulusEnergyProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
                    }
                }
            }
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.06f, 100, default, 0.85f);
                d.fadeIn = 1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // ASTRAL STAFF — a rune circle descends, then tracking crystal meteors fall through it.
    // =====================================================================================================================
    public class AstralCrystalProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Boss/AstralFlame";
        private const int TelegraphTime = 40;

        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 220;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] < TelegraphTime)
            {
                Projectile.velocity *= 0.9f;
            }
            else
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (target.active)
                {
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 10f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.03f);
                }
            }
            Projectile.rotation += 0.08f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.05f, 100, default, 1f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            DeusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // RADIANT STAR — A: 6 knives orbit-constrict then release tangentially. B: pulsing ring, expand/contract twice.
    // =====================================================================================================================
    public class RadiantStarKnifeProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/RadiantStar";

        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 220;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC anchor = Main.npc[(int)Projectile.ai[0]];
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Vector2 center = target.active ? target.Center : (anchor.active ? anchor.Center : Projectile.Center);

            if (Projectile.localAI[1] == 0f)
            {
                bool pulsing = Projectile.ai[2] != 0f;
                float t = Projectile.localAI[0] / 150f;
                float radius = pulsing
                    ? 260f + MathF.Sin(t * MathHelper.TwoPi * 2f) * 140f
                    : MathHelper.Lerp(300f, 60f, MathHelper.Clamp(t, 0f, 1f));
                float angle = Projectile.ai[1] + Projectile.localAI[0] * 0.05f;
                Vector2 desired = center + angle.ToRotationVector2() * radius;
                Projectile.Center = Vector2.Lerp(Projectile.Center, desired, 0.2f);
                Projectile.rotation += 0.2f;

                if (!pulsing && Projectile.localAI[0] >= 150f)
                {
                    Projectile.localAI[1] = 1f;
                    Vector2 tangent = new(-(Projectile.Center - center).Y, (Projectile.Center - center).X);
                    Projectile.velocity = tangent.SafeNormalize(Vector2.UnitX) * 14f;
                }
                else if (pulsing && Projectile.localAI[0] >= 150f)
                {
                    Projectile.localAI[1] = 1f;
                    Projectile.velocity = (Projectile.Center - center).SafeNormalize(Vector2.UnitY) * 14f;
                }
            }
            else
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, Vector2.Zero, 100, default, 0.9f);
                d.fadeIn = 1.1f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            DeusFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, 1f, new Color(230, 200, 60));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // TRUE BIOME BLADE — a diagonal cross-flight leaves a rift line that erupts into an aurora curtain.
    // =====================================================================================================================
    public class TrueBiomeRiftProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 72; // 1.2s

        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 2600;
            Projectile.height = 2600;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TelegraphTime + 30;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == TelegraphTime)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.7f }, Projectile.Center);
                Player p = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (p.active) p.Calamity().GeneralScreenShakePower = 8f;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float rot = Projectile.ai[0];
            Vector2 dir = rot.ToRotationVector2();
            Vector2 start = Projectile.Center - dir * 1300f;
            Vector2 end = Projectile.Center + dir * 1300f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 40f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f);
            bool armed = Projectile.localAI[0] >= TelegraphTime;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.ai[0];
            float width = armed ? 90f : MathHelper.Lerp(2f, 10f, t);
            Color core = armed ? new Color(200, 120, 255) : Color.Lerp(new Color(120, 40, 180), Color.White, t);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), core * 0.4f, rot, new Vector2(0.5f), new Vector2(2600f, width * 2.2f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), core, rot, new Vector2(0.5f), new Vector2(2600f, width), SpriteEffects.None, 0f);
            return false;
        }
    }
}
