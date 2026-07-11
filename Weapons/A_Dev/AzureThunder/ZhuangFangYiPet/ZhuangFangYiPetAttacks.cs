using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.ZhuangFangYiPet
{
    internal static class ZhuangFangYiPetAttacks
    {
        public static void SpawnAzureLightning(
            IEntitySource source,
            Player owner,
            Vector2 muzzle,
            Vector2 aimPoint,
            int damage,
            float knockback,
            bool strong,
            bool harmony,
            float scale)
        {
            Vector2 velocity = (aimPoint - muzzle).SafeNormalize(Vector2.UnitX * owner.direction);
            int flags = AzureThunderFlatLightning.PetLightningFlag |
                AzureThunderFlatLightning.NoBaseElectricDebuffFlag |
                AzureThunderFlatLightning.NormalVisualIntensityFlag;

            if (!strong)
                flags |= AzureThunderFlatLightning.WeakLightningFlag;

            if (strong)
                flags |= AzureThunderFlatLightning.BigLightningFlag;

            if (strong && harmony)
                flags |= AzureThunderFlatLightning.PetStrongLightningFlag;

            if (harmony)
                flags |= AzureThunderFlatLightning.SpeedLineFlag;

            AzureThunderPlayer.SpawnDirectionalLightning(
                source,
                muzzle,
                velocity,
                damage,
                knockback,
                owner.whoAmI,
                flags,
                big: strong,
                size: scale);
        }
    }
}
