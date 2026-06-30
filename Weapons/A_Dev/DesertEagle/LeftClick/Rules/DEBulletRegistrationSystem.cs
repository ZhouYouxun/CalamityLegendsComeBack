using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.A_ClassicPistol;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.B_HardMode;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.C_PostPlantera;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.D_Endgame;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules
{
    /// <summary>
    /// 在内容加载完成后将所有沙漠之鹰弹幕规则注册到 DEBulletRegistry。
    /// </summary>
    public class DEBulletRegistrationSystem : ModSystem
    {
        public override void PostSetupContent()
        {
            DEBulletRegistry.Clear();

            // ── A 组：经典手枪 ──────────────────────────────────
            DEBulletRegistry.Register(new DERule_CrackshotColt());
            DEBulletRegistry.Register(new DERule_Fungicide());
            DEBulletRegistry.Register(new DERule_SlagMagnum());
            DEBulletRegistry.Register(new DERule_PhoenixBlaster());

            // ── B 组：硬模 ──────────────────────────────────────
            DEBulletRegistry.Register(new DERule_MidasPrime());
            DEBulletRegistry.Register(new DERule_Needler());
            DEBulletRegistry.Register(new DERule_ThermoclineBlaster());
            DEBulletRegistry.Register(new DERule_Arietes41());
            DEBulletRegistry.Register(new DERule_Helstorm());

            // ── C 组：植物后 ────────────────────────────────────
            DEBulletRegistry.Register(new DERule_Hydra());
            DEBulletRegistry.Register(new DERule_PestilentDefiler());
            DEBulletRegistry.Register(new DERule_AstralBlaster());
            DEBulletRegistry.Register(new DERule_Hellborn());

            // ── D 组：终局 ──────────────────────────────────────
            DEBulletRegistry.Register(new DERule_GoldenEagle());
            DEBulletRegistry.Register(new DERule_StellarCannon());
            DEBulletRegistry.Register(new DERule_PridefulHuntersPlanarRipper());
            DEBulletRegistry.Register(new DERule_AcesHigh());
        }

        public override void Unload()
        {
            DEBulletRegistry.Clear();
        }
    }
}
