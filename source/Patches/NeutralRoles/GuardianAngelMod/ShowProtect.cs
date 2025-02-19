using HarmonyLib;
using TownOfUs.Roles;
using UnityEngine;
using TownOfUs.Extensions;

namespace TownOfUs.NeutralRoles.GuardianAngelMod
{
    public enum ProtectOptions
    {
        Self = 0,
        GA = 1,
        SelfAndGA = 2,
        Everyone = 3
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public class ShowProtect
    {
        public static Color ProtectedColor = new Color(1f, 0.85f, 0f, 1f);
        public static Color ShieldedColor = Color.cyan;

        public static void Postfix(HudManager __instance)
        {
            foreach (var role in Role.GetRoles(RoleEnum.GuardianAngel))
            {
                var ga = (GuardianAngel) role;

                var player = ga.target;
                if (player == null) continue;

                if ((player.Data.IsDead || ga.Player.Data.IsDead || ga.Player.Data.Disconnected) && !player.IsShielded())
                {
                    player.myRend().material.SetColor("_VisorColor", Palette.VisorColor);
                    player.myRend().material.SetFloat("_Outline", 0f);
                    continue;
                }

                if (ga.Protecting)
                {
                    var showProtected = CustomGameOptions.ShowProtect;
                    if (showProtected == ProtectOptions.Everyone)
                    {
                        player.myRend().material.SetColor("_VisorColor", ProtectedColor);
                        player.myRend().material.SetFloat("_Outline", 1f);
                        player.myRend().material.SetColor("_OutlineColor", ProtectedColor);
                    }
                    else if (PlayerControl.LocalPlayer.PlayerId == player.PlayerId && (showProtected == ProtectOptions.Self ||
                        showProtected == ProtectOptions.SelfAndGA))
                    {
                        player.myRend().material.SetColor("_VisorColor", ProtectedColor);
                        player.myRend().material.SetFloat("_Outline", 1f);
                        player.myRend().material.SetColor("_OutlineColor", ProtectedColor);
                    }
                    else if (PlayerControl.LocalPlayer.Is(RoleEnum.GuardianAngel) &&
                             (showProtected == ProtectOptions.GA || showProtected == ProtectOptions.SelfAndGA))
                    {
                        player.myRend().material.SetColor("_VisorColor", ProtectedColor);
                        player.myRend().material.SetFloat("_Outline", 1f);
                        player.myRend().material.SetColor("_OutlineColor", ProtectedColor);
                    }
                }
            }
        }
    }
}