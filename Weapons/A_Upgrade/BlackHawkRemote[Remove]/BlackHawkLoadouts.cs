using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.BlackHawkRemote
{
    internal enum BlackHawkLoadout : sbyte
    {
        Auto = -1,
        MachineGun = 0,
        GuidedMissiles = 1,
        ClusterBomb = 2,
        Napalm = 3,
        Cryogenic = 4,
        EMP = 5,
        HolyPayload = 6,
        DirtyBomb = 7,
        HeavyBomb = 8
    }

    internal static class BlackHawkLoadoutInfo
    {
        internal const int FirstWeapon = (int)BlackHawkLoadout.MachineGun;
        internal const int LastWeapon = (int)BlackHawkLoadout.HeavyBomb;

        internal static bool IsWeapon(BlackHawkLoadout loadout) =>
            loadout >= BlackHawkLoadout.MachineGun && loadout <= BlackHawkLoadout.HeavyBomb;

        internal static BlackHawkLoadout Sanitize(int raw) =>
            raw >= FirstWeapon && raw <= LastWeapon ? (BlackHawkLoadout)raw : BlackHawkLoadout.Auto;

        internal static int Ammo(BlackHawkLoadout loadout) => loadout switch
        {
            BlackHawkLoadout.MachineGun => 5,
            BlackHawkLoadout.GuidedMissiles => 4,
            BlackHawkLoadout.ClusterBomb => 3,
            BlackHawkLoadout.Napalm => 3,
            BlackHawkLoadout.Cryogenic => 2,
            BlackHawkLoadout.EMP => 2,
            BlackHawkLoadout.HolyPayload => 2,
            BlackHawkLoadout.DirtyBomb => 1,
            BlackHawkLoadout.HeavyBomb => 1,
            _ => 5
        };

        internal static int SortieCooldown(BlackHawkLoadout loadout) => loadout switch
        {
            BlackHawkLoadout.MachineGun => 120,
            BlackHawkLoadout.GuidedMissiles => 180,
            BlackHawkLoadout.ClusterBomb => 240,
            BlackHawkLoadout.Napalm => 270,
            BlackHawkLoadout.Cryogenic => 300,
            BlackHawkLoadout.EMP => 330,
            BlackHawkLoadout.HolyPayload => 300,
            _ => 0
        };

        internal static int ResupplyTime(BlackHawkLoadout loadout) => loadout switch
        {
            BlackHawkLoadout.MachineGun => 180,
            BlackHawkLoadout.GuidedMissiles => 240,
            BlackHawkLoadout.ClusterBomb => 300,
            BlackHawkLoadout.Napalm => 300,
            BlackHawkLoadout.Cryogenic => 300,
            BlackHawkLoadout.EMP => 330,
            BlackHawkLoadout.HolyPayload => 330,
            BlackHawkLoadout.DirtyBomb => 360,
            BlackHawkLoadout.HeavyBomb => 420,
            _ => 180
        };

        internal static int ConcurrentLimit(BlackHawkLoadout loadout) => loadout switch
        {
            BlackHawkLoadout.MachineGun => 3,
            BlackHawkLoadout.GuidedMissiles => 2,
            BlackHawkLoadout.ClusterBomb => 2,
            _ => 1
        };

        internal static bool UsesPersistentArea(BlackHawkLoadout loadout) =>
            loadout is BlackHawkLoadout.Napalm or BlackHawkLoadout.Cryogenic or
                BlackHawkLoadout.EMP or BlackHawkLoadout.DirtyBomb;

        internal static float MainDamageMultiplier(BlackHawkLoadout loadout) => loadout switch
        {
            BlackHawkLoadout.MachineGun => 0.35f,
            BlackHawkLoadout.GuidedMissiles => 1.40f,
            BlackHawkLoadout.ClusterBomb => 0.55f,
            BlackHawkLoadout.Napalm => 0.70f,
            BlackHawkLoadout.Cryogenic => 1.10f,
            BlackHawkLoadout.EMP => 0.60f,
            BlackHawkLoadout.HolyPayload => 1.60f,
            BlackHawkLoadout.DirtyBomb => 2.00f,
            BlackHawkLoadout.HeavyBomb => 6.80f,
            _ => 0.35f
        };

        internal static Color Color(BlackHawkLoadout loadout) => loadout switch
        {
            BlackHawkLoadout.MachineGun => new Color(255, 218, 130),
            BlackHawkLoadout.GuidedMissiles => new Color(255, 92, 70),
            BlackHawkLoadout.ClusterBomb => new Color(255, 174, 72),
            BlackHawkLoadout.Napalm => new Color(255, 91, 38),
            BlackHawkLoadout.Cryogenic => new Color(112, 231, 255),
            BlackHawkLoadout.EMP => new Color(104, 142, 255),
            BlackHawkLoadout.HolyPayload => new Color(255, 240, 145),
            BlackHawkLoadout.DirtyBomb => new Color(155, 214, 74),
            BlackHawkLoadout.HeavyBomb => new Color(255, 128, 66),
            _ => new Color(180, 220, 235)
        };

        internal static string ShortCode(BlackHawkLoadout loadout) => loadout switch
        {
            BlackHawkLoadout.MachineGun => "MG",
            BlackHawkLoadout.GuidedMissiles => "AAM",
            BlackHawkLoadout.ClusterBomb => "CBU",
            BlackHawkLoadout.Napalm => "NAP",
            BlackHawkLoadout.Cryogenic => "LN2",
            BlackHawkLoadout.EMP => "EMP",
            BlackHawkLoadout.HolyPayload => "HOLY",
            BlackHawkLoadout.DirtyBomb => "RAD",
            BlackHawkLoadout.HeavyBomb => "HE",
            _ => "AUTO"
        };

        internal static string Name(BlackHawkLoadout loadout)
        {
            string suffix = loadout switch
            {
                BlackHawkLoadout.MachineGun => "MachineGun",
                BlackHawkLoadout.GuidedMissiles => "GuidedMissiles",
                BlackHawkLoadout.ClusterBomb => "ClusterBomb",
                BlackHawkLoadout.Napalm => "Napalm",
                BlackHawkLoadout.Cryogenic => "Cryogenic",
                BlackHawkLoadout.EMP => "EMP",
                BlackHawkLoadout.HolyPayload => "HolyPayload",
                BlackHawkLoadout.DirtyBomb => "DirtyBomb",
                BlackHawkLoadout.HeavyBomb => "HeavyBomb",
                _ => "Auto"
            };
            return Language.GetTextValue($"Mods.CalamityLegendsComeBack.TheSpecialText.BlackHawkLoadout{suffix}");
        }
    }

    internal static class BlackHawkVFX
    {
        private const string BloomTexture = "CalamityMod/Particles/BloomCircle";
        private const string RingTexture = "CalamityMod/Particles/HollowCircleHardEdge";

        internal static void SpawnEnginePoint(Vector2 position, Vector2 velocity, Color color)
        {
            if (Main.dedServ || GeneralParticleHandler.FreeSpacesAvailable() < 1)
                return;

            GeneralParticleHandler.SpawnParticle(new BloomParticle(
                position,
                velocity * 0.02f,
                color,
                0.11f,
                0.11f,
                2,
                false));
        }

        internal static void SpawnSmokePoint(Vector2 position, Vector2 velocity, Color hotColor, Color fadeColor, float scale = 0.55f)
        {
            if (Main.dedServ || GeneralParticleHandler.FreeSpacesAvailable() < 1)
                return;

            GeneralParticleHandler.SpawnParticle(new SmallSmokeParticle(
                position,
                velocity,
                hotColor,
                fadeColor,
                scale,
                0.55f,
                Main.rand.NextFloat(-0.035f, 0.035f)));
        }

        internal static void SpawnPulse(Vector2 position, Color color, float fromScale, float toScale, int lifetime, Vector2? squish = null, float rotation = 0f)
        {
            if (Main.dedServ || GeneralParticleHandler.FreeSpacesAvailable() < 1)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                position,
                Vector2.Zero,
                color,
                squish ?? Vector2.One,
                rotation,
                fromScale,
                toScale,
                lifetime));
        }

        internal static void SpawnCompactImpact(Vector2 position, Vector2 attackDirection, Color color, bool heavy = false)
        {
            int wanted = heavy ? 5 : 3;
            if (Main.dedServ || GeneralParticleHandler.FreeSpacesAvailable() < wanted)
                return;

            SpawnPulse(position, color, heavy ? 0.22f : 0.11f, heavy ? 1.35f : 0.72f, heavy ? 24 : 15,
                new Vector2(1f, heavy ? 0.82f : 0.68f), attackDirection.ToRotation());

            int smokeCount = heavy ? 4 : 2;
            for (int i = 0; i < smokeCount; i++)
            {
                Vector2 offsetDirection = attackDirection.RotatedBy(MathHelper.PiOver2 * Main.rand.NextFloatDirection());
                Vector2 velocity = attackDirection * Main.rand.NextFloat(0.4f, 1.8f) + offsetDirection * Main.rand.NextFloat(0.3f, 1.5f);
                SpawnSmokePoint(position + offsetDirection * Main.rand.NextFloat(2f, 12f), velocity,
                    color, new Color(46, 47, 52), heavy ? 0.92f : 0.58f);
            }
        }

        internal static void DrawBloom(Vector2 worldPosition, Color color, float radius, float opacity = 1f)
        {
            Texture2D texture = ModContent.Request<Texture2D>(BloomTexture).Value;
            Color additive = Additive(color) * opacity;
            Main.EntitySpriteDraw(texture, worldPosition - Main.screenPosition, null, additive, 0f,
                texture.Size() * 0.5f, radius * 2f / Math.Max(1f, texture.Width), SpriteEffects.None, 0f);
        }

        internal static void DrawRing(Vector2 worldPosition, Color color, float radius, float opacity = 1f, float rotation = 0f, Vector2? squash = null)
        {
            Texture2D texture = ModContent.Request<Texture2D>(RingTexture).Value;
            Vector2 scale = new(radius * 2f / Math.Max(1f, texture.Width));
            scale *= squash ?? Vector2.One;
            Main.EntitySpriteDraw(texture, worldPosition - Main.screenPosition, null, Additive(color) * opacity,
                rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        internal static void DrawWorldLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 0.01f)
                return;

            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start - Main.screenPosition, new Rectangle(0, 0, 1, 1),
                color, edge.ToRotation(), new Vector2(0f, 0.5f), new Vector2(edge.Length(), thickness), SpriteEffects.None, 0f);
        }

        internal static Color Additive(Color color)
        {
            color.A = 0;
            return color;
        }
    }
}
