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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Providence
{
    internal static class ProvFx
    {
        public static void Burst(Vector2 position, float speed, int count, int dustType = DustID.GoldFlame)
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

    // HOLY COLLIDER — a slash trail marks a line; 1s later it chain-erupts into holy fire pillars.
    public class HolyColliderTrailProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 60;

        public override bool? CanDamage() => false;

        public override void SetDefaults()
        {
            Projectile.width = 8; Projectile.height = 8;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = TelegraphTime + 4;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == TelegraphTime && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item72, Projectile.Center);
                Vector2 dir = new(Projectile.ai[0], Projectile.ai[1]);
                for (int i = 0; i < 8; i++)
                {
                    Vector2 pos = Projectile.Center + dir * (i * 90f);
                    int idx = Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, Vector2.Zero, ModContent.ProjectileType<HolyFirePillarProj>(), Projectile.damage, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = i * 4f;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f);
            Vector2 dir = new(Projectile.ai[0], Projectile.ai[1]);
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = start + dir * 720f * t;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, start, new Rectangle(0, 0, 1, 1), new Color(255, 230, 120) * 0.6f, dir.ToRotation(), new Vector2(0f, 0.5f), new Vector2((end - start).Length(), 6f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class HolyFirePillarProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool? CanDamage() => Projectile.localAI[1] >= 1f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 50; Projectile.height = 500;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 60;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] >= Projectile.ai[0] && Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                SoundEngine.PlaySound(SoundID.Item72 with { Pitch = 0.2f }, Projectile.Center);
                ProvFx.Burst(Projectile.Center, 5f, 12);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.localAI[1] < 1f) return false;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float p = MathHelper.Clamp(Projectile.timeLeft / 55f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 220, 100, 0) * 0.5f * p, 0f, new Vector2(0.5f), new Vector2(60f, 500f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 245, 190) * p, 0f, new Vector2(0.5f), new Vector2(24f, 500f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // BURNING REVELATION — core drifts in, bursts into an outer expanding ring and an inner contracting ring.
    public class RevelationCoreProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/BurningRevelation";

        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 90;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.97f;
            Projectile.rotation += 0.1f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.06f, 100, default, 1.1f);
                d.fadeIn = 1.2f; d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.7f }, Projectile.Center);
            ProvFx.Burst(Projectile.Center, 6f, 20);
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            for (int i = 0; i < 12; i++)
            {
                float a = i * MathHelper.TwoPi / 12f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, a.ToRotationVector2() * 4f, ModContent.ProjectileType<RevelationRingFireProj>(), Projectile.damage / 2, 0f, Main.myPlayer, 0f);
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathHelper.TwoPi / 8f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, a.ToRotationVector2() * 3f, ModContent.ProjectileType<RevelationRingFireProj>(), Projectile.damage / 2, 0f, Main.myPlayer, 1f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 0.9f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class RevelationRingFireProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 20; Projectile.height = 20;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 130;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            bool inner = Projectile.ai[0] != 0f;
            if (inner && Projectile.localAI[0] >= 40f)
                Projectile.velocity *= 0.9f;
            if (inner && Projectile.localAI[0] >= 55f)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (target.active)
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 6f, 0.08f);
            }
            Projectile.rotation += 0.1f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, Vector2.Zero, 100, default, 0.9f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            bool inner = Projectile.ai[0] != 0f;
            Color c = inner ? new Color(255, 160, 60) : new Color(255, 220, 100);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), c, Projectile.rotation, new Vector2(0.5f), new Vector2(18f, 18f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // TELLURIC GLARE — 4 parallel lane markers lock, then fire thick beams across the screen.
    public class TelluricBeamProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int TelegraphTime = 34;
        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 2400; Projectile.height = 50;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = TelegraphTime + 40;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == TelegraphTime)
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.5f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f);
            bool armed = Projectile.localAI[0] >= TelegraphTime;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float h = armed ? 60f : MathHelper.Lerp(2f, 10f, t);
            Color core = armed ? new Color(255, 230, 130) : Color.Lerp(new Color(200, 160, 40), Color.White, t);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), core * 0.4f, 0f, new Vector2(0.5f), new Vector2(2400f, h * 2.2f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), core, 0f, new Vector2(0.5f), new Vector2(2400f, h), SpriteEffects.None, 0f);
            return false;
        }
    }

    // BLISSFUL BOMBARDIER — homing rocket splits into tracking mini-rockets near the player.
    public class BombardierRocketProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Ranged/BlissfulBombardier";

        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 200;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 9f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.025f);
                if ((Projectile.Center - target.Center).Length() < 200f)
                {
                    Split();
                    Projectile.Kill();
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.1f, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        private void Split()
        {
            SoundEngine.PlaySound(SoundID.Item62, Projectile.Center);
            ProvFx.Burst(Projectile.Center, 6f, 18);
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathHelper.TwoPi / 8f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, a.ToRotationVector2() * 6f, ModContent.ProjectileType<BombardierMiniRocketProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation + MathHelper.PiOver4, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class BombardierMiniRocketProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 14; Projectile.height = 14;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 140;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 12f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.05f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.06f, 100, default, 0.8f);
                d.fadeIn = 1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 220, 100, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(20f, 8f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 245, 190), rot, new Vector2(0.5f), new Vector2(14f, 4f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // PURGE GUZZLER — 3 crystal cores triangle-lock and beam inward to a death-lock cage.
    public class PurgeCoreProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Magic/PurgeGuzzler";
        private const int LockTime = 40;
        public override bool? CanDamage() => Projectile.localAI[0] >= LockTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 26;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = LockTime + 50;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.08f;
            if (Projectile.localAI[0] == LockTime)
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.5f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = MathHelper.Clamp(Projectile.localAI[0] / LockTime, 0f, 1f);
            bool armed = Projectile.localAI[0] >= LockTime;
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 center = new Vector2(Projectile.ai[0], Projectile.ai[1]) - Main.screenPosition;
            Vector2 delta = center - start;
            float len = delta.Length();
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float width = armed ? 40f : MathHelper.Lerp(2f, 8f, t);
            Color core = armed ? new Color(255, 245, 190) : Color.Lerp(new Color(200, 160, 40), Color.White, t);
            Main.spriteBatch.Draw(pixel, start, new Rectangle(0, 0, 1, 1), core * 0.5f, delta.ToRotation(), new Vector2(0f, 0.5f), new Vector2(len, width), SpriteEffects.None, 0f);

            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Main.spriteBatch.Draw(tex, start, null, Color.White, Projectile.rotation, tex.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // DAZZLING STABBER — a giant spear drops from the sky and slams into a debris wall barrier.
    public class DazzlingSpearProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Summon/DazzlingStabberStaff";
        public override bool? CanDamage() => Projectile.velocity.Y > 0.5f ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 90;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 120;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.06f, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
            if (Projectile.ai[0] > 0f && Projectile.Center.Y >= Projectile.ai[0] && Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.7f }, Projectile.Center);
                ProvFx.Burst(Projectile.Center, 5f, 20);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int idx = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<DebrisWallProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
                }
                Projectile.timeLeft = 10;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            ProvFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1f, new Color(255, 230, 120));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class DebrisWallProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 90;

        public override void SetDefaults()
        {
            Projectile.width = 80; Projectile.height = 400;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Main.rand.NextBool(3))
            {
                Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-40f, 40f), Main.rand.NextFloat(-190f, 190f));
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, Vector2.Zero, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 220, 100, 0) * 0.5f * alpha, 0f, new Vector2(0.5f), new Vector2(80f, 400f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 245, 190) * 0.7f * alpha, 0f, new Vector2(0.5f), new Vector2(40f, 400f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // MOLTEN AMPUTATOR — a spinning sickle arcs across the field, dripping gravity lava that pools.
    public class MoltenSickleProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/MoltenAmputator";

        public override void SetDefaults()
        {
            Projectile.width = 44; Projectile.height = 44;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 160;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.3f;
            Projectile.velocity.Y += 0.1f;
            if (Main.rand.NextBool(3) && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(0f, 3f), ModContent.ProjectileType<MoltenDripProj>(), Projectile.damage / 2, 0f, Main.myPlayer);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            ProvFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1f, new Color(255, 120, 40));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class MoltenDripProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 12; Projectile.height = 12;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 60;
            Projectile.tileCollide = true; Projectile.ignoreWater = true;
        }

        public override void AI() { Projectile.velocity.Y += 0.2f; }
        public override void OnKill(int timeLeft) => Pool();
        public override bool OnTileCollide(Vector2 oldVelocity) { Pool(); Projectile.Kill(); return false; }

        private void Pool()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MoltenPoolProj>(), Projectile.damage, 0f, Main.myPlayer);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 140, 40), 0f, new Vector2(0.5f), new Vector2(10f, 10f), SpriteEffects.None, 0f);
            return false;
        }
    }

    public class MoltenPoolProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 120; // 2s

        public override void SetDefaults()
        {
            Projectile.width = 80; Projectile.height = 20;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.OnFire3, 120);

        public override void AI()
        {
            if (Main.rand.NextBool(3))
            {
                Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-35f, 35f), 4f);
                Dust d = Dust.NewDustPerfect(pos, DustID.Torch, new Vector2(0f, Main.rand.NextFloat(-2f, -0.5f)), 100, default, 1.1f);
                d.fadeIn = 1.2f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 24f, 0f, 1f) * MathHelper.Clamp((Lifetime - Projectile.timeLeft) / 12f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 120, 40, 0) * 0.5f * alpha, 0f, new Vector2(0.5f), new Vector2(80f, 16f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 200, 120) * 0.6f * alpha, 0f, new Vector2(0.5f), new Vector2(70f, 8f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // PRISTINE FURY — a slow 120-degree sweeping fan of white fire.
    public class PristineFlameProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 28; Projectile.height = 28;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 80;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.1f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.WhiteTorch, -Projectile.velocity * 0.05f, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), Color.White, Projectile.rotation, new Vector2(0.5f), new Vector2(26f, 26f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // AETHERFLUX CANNON — arc-tracking blue-gold laser bolts.
    public class AetherfluxLaserProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 20; Projectile.height = 20;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 140;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 14f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.02f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.06f, 100, default, 0.9f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(120, 190, 255, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(26f, 10f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 240, 255), rot, new Vector2(0.5f), new Vector2(18f, 5f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // ANGELIC SHOTGUN — buckshot spray that bounces off the arena boundary.
    public class AngelicPelletProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 12; Projectile.height = 12;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 2; Projectile.timeLeft = 110;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            float half = Projectile.ai[0];
            Vector2 boundCenter = new(Projectile.ai[1], Projectile.ai[2]);
            if (half > 0f && Projectile.localAI[0] == 0f && (Projectile.Center - boundCenter).Length() > half)
            {
                Projectile.localAI[0] = 1f;
                Projectile.velocity = (boundCenter - Projectile.Center).SafeNormalize(Vector2.UnitY) * Projectile.velocity.Length();
            }
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.05f, 100, default, 0.8f);
                d.fadeIn = 1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 235, 150), Projectile.rotation, new Vector2(0.5f), new Vector2(10f, 6f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // DARK SPARK — a void spark bursts into a cross-shaped ray.
    public class DarkSparkProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Magic/DarkSpark";
        private const int TelegraphTime = 40;
        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime ? null : (bool?)false;

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 24;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = TelegraphTime + 30;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.1f;
            Projectile.scale = MathHelper.Lerp(0.6f, 1.2f, MathHelper.Clamp(Projectile.localAI[0] / TelegraphTime, 0f, 1f));
            if (Projectile.localAI[0] == TelegraphTime)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.3f }, Projectile.Center);
                ProvFx.Burst(Projectile.Center, 5f, 16, DustID.PurpleTorch);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            bool armed = Projectile.localAI[0] >= TelegraphTime;
            ProvFx.DrawBackglow(Main.spriteBatch, tex, pos, null, Projectile.rotation, origin, Projectile.scale, new Color(160, 60, 220));
            Main.spriteBatch.Draw(tex, pos, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            if (armed)
            {
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220) * 0.6f, 0f, new Vector2(0.5f), new Vector2(600f, 16f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(160, 60, 220) * 0.6f, MathHelper.PiOver2, new Vector2(0.5f), new Vector2(600f, 16f), SpriteEffects.None, 0f);
            }
            return false;
        }
    }

    // GALACTUS BLADE — meteor streaks fall and stand as light-blades.
    public class GalactusMeteorProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/GalactusBlade";

        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 60;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 90;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.06f, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            ProvFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1f, new Color(255, 230, 120));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // MOURNINGSTAR — twin double-helix firelines scattering sparks as they spin past.
    public class MourningstarLineProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/Mourningstar";

        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 160;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC owner = Main.npc[(int)Projectile.ai[0]];
            if (owner.active)
            {
                float angle = Projectile.ai[1] + Projectile.localAI[0] * 0.06f;
                float radius = 200f + MathF.Sin(Projectile.localAI[0] * 0.03f) * 100f;
                Vector2 desired = owner.Center + angle.ToRotationVector2() * radius;
                Projectile.Center = Vector2.Lerp(Projectile.Center, desired, 0.2f);
            }
            Projectile.rotation += 0.2f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, Vector2.Zero, 100, default, 0.9f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            ProvFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1f, new Color(255, 230, 120));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // SHATTERED DAWN — a golden disc bursts into a 24-way radial blast.
    public class ShatteredDiscProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/ShatteredDawn";

        public override void SetDefaults()
        {
            Projectile.width = 30; Projectile.height = 30;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 80;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.96f;
            Projectile.rotation += 0.15f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.06f, 100, default, 1f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62, Projectile.Center);
            ProvFx.Burst(Projectile.Center, 6f, 24);
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            for (int i = 0; i < 24; i++)
            {
                float a = i * MathHelper.TwoPi / 24f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, a.ToRotationVector2() * 9f, ModContent.ProjectileType<AngelicPelletProj>(), Projectile.damage / 2, 0f, Main.myPlayer, 0f, Projectile.Center.X, Projectile.Center.Y);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // SEEKING SCORCHER — a tracking ring orbits the player, leaving a burning trail.
    public class SeekingScorcherRingProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/SeekingScorcher";

        public override void SetDefaults()
        {
            Projectile.width = 34; Projectile.height = 34;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = 220;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active)
            {
                float angle = Projectile.ai[0] + Projectile.localAI[0] * 0.05f;
                Vector2 desired = target.Center + angle.ToRotationVector2() * 220f;
                Projectile.Center = Vector2.Lerp(Projectile.Center, desired, 0.1f);
            }
            Projectile.rotation += 0.2f;
            if (Main.rand.NextBool(2) && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MoltenPoolProj>(), Projectile.damage / 3, 0f, Main.myPlayer);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            ProvFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, Projectile.rotation, origin, 1f, new Color(255, 140, 40));
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // MIRROR OF KALANDRA — a fixed mirror plate; any friendly projectile that touches it is destroyed and
    // reflected back as a homing holy spear, punishing players who keep shooting through the center.
    public class KalandraMirrorProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Summon/MirrorofKalandra";
        private const int Lifetime = 130;

        public override bool? CanDamage() => false;

        public override void SetDefaults()
        {
            Projectile.width = 80; Projectile.height = 140;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.friendly && !p.hostile && (p.Center - Projectile.Center).Length() < 90f)
                {
                    p.Kill();
                    Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                    Vector2 vel = target.active ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 12f : -Vector2.UnitY * 10f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<KalandraSpearProj>(), Projectile.damage, 0f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f }, Projectile.Center);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f) * MathHelper.Clamp((Lifetime - Projectile.timeLeft) / 15f, 0f, 1f);
            ProvFx.DrawBackglow(Main.spriteBatch, tex, Projectile.Center - Main.screenPosition, null, 0f, origin, 1.3f, new Color(220, 240, 255) * alpha);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White * alpha, 0f, origin, 1.3f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class KalandraSpearProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16; Projectile.height = 40;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = 1; Projectile.timeLeft = 150;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 12f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.03f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.06f, 100, default, 0.9f);
                d.fadeIn = 1.1f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rot = Projectile.rotation;
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(220, 240, 255, 0) * 0.4f, rot, new Vector2(0.5f), new Vector2(12f, 34f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), Color.White, rot, new Vector2(0.5f), new Vector2(6f, 26f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // MAELSTROM — a central vortex pulls the player inward while cross lasers sweep.
    public class MaelstromVortexProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private const int Lifetime = 160;

        public override void SetDefaults()
        {
            Projectile.width = 60; Projectile.height = 60;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.penetrate = -1; Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false; Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.05f;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active && (target.Center - Projectile.Center).Length() < 600f)
                target.velocity += (Projectile.Center - target.Center).SafeNormalize(Vector2.Zero) * 0.4f;
            if (Main.rand.NextBool(2))
            {
                Vector2 around = Projectile.Center + Main.rand.NextVector2Circular(240f, 240f);
                Dust d = Dust.NewDustPerfect(around, DustID.GoldFlame, (Projectile.Center - around) * 0.04f, 100, default, 1.1f);
                d.fadeIn = 1.2f; d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float alpha = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 220, 100, 0) * 0.5f * alpha, 0f, new Vector2(0.5f), new Vector2(60f, 60f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 245, 190) * alpha, Projectile.rotation, new Vector2(0.5f), new Vector2(900f, 10f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), new Color(255, 245, 190) * alpha, Projectile.rotation + MathHelper.PiOver2, new Vector2(0.5f), new Vector2(900f, 10f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
