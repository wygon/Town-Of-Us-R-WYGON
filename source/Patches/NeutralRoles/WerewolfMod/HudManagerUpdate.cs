﻿using HarmonyLib;
using System.Linq;
using TownOfUs.Roles;
using UnityEngine;

namespace TownOfUs.NeutralRoles.WerewolfMod
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class HudManagerUpdate
    {
        public static Sprite RampageSprite => TownOfUs.RampageSprite;
        
        public static void Postfix(HudManager __instance)
        {
            if (PlayerControl.AllPlayerControls.Count <= 1) return;
            if (PlayerControl.LocalPlayer == null) return;
            if (PlayerControl.LocalPlayer.Data == null) return;
            if (!PlayerControl.LocalPlayer.Is(RoleEnum.Werewolf)) return;
            var role = Role.GetRole<Werewolf>(PlayerControl.LocalPlayer);

            __instance.KillButton.gameObject.SetActive((__instance.UseButton.isActiveAndEnabled || __instance.PetButton.isActiveAndEnabled)
                    && !MeetingHud.Instance && !PlayerControl.LocalPlayer.Data.IsDead
                    && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started);
            __instance.KillButton.SetCoolDown(role.KillTimer(), CustomGameOptions.RampageKillCd);

            if (role.RampageButton == null)
            {
                role.RampageButton = Object.Instantiate(__instance.KillButton, __instance.KillButton.transform.parent);
                role.RampageButton.graphic.enabled = true;
                role.RampageButton.gameObject.SetActive(false);
            }

            role.RampageButton.graphic.sprite = RampageSprite;
            role.RampageButton.transform.localPosition = new Vector3(-2f, 0f, 0f);

            role.RampageButton.gameObject.SetActive((__instance.UseButton.isActiveAndEnabled || __instance.PetButton.isActiveAndEnabled)
                    && !MeetingHud.Instance && !PlayerControl.LocalPlayer.Data.IsDead
                    && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started);

            if (role.Rampaged)
            {
                role.RampageButton.SetCoolDown(role.TimeRemaining, CustomGameOptions.RampageDuration);
                role.RampageButton.graphic.color = Palette.EnabledColor;
                role.RampageButton.graphic.material.SetFloat("_Desat", 0f);
                if ((CamouflageUnCamouflage.IsCamoed && CustomGameOptions.CamoCommsKillAnyone) || PlayerControl.LocalPlayer.IsHypnotised()) Utils.SetTarget(ref role.ClosestPlayer, __instance.KillButton);
                else if (role.Player.IsLover()) Utils.SetTarget(ref role.ClosestPlayer, __instance.KillButton, float.NaN, PlayerControl.AllPlayerControls.ToArray().Where(x => !x.IsLover()).ToList());
                else Utils.SetTarget(ref role.ClosestPlayer, __instance.KillButton);

                return;
            }
            else
            {
                role.RampageButton.SetCoolDown(role.RampageTimer(), CustomGameOptions.RampageCd);

                if (role.RampageTimer() > 0f || !PlayerControl.LocalPlayer.moveable)
                {
                    role.RampageButton.graphic.color = Palette.DisabledClear;
                    role.RampageButton.graphic.material.SetFloat("_Desat", 1f);
                }
                else
                {
                    role.RampageButton.graphic.color = Palette.EnabledColor;
                    role.RampageButton.graphic.material.SetFloat("_Desat", 0f);
                }

                return;
            }
        }
    }
}
