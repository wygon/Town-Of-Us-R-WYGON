﻿using AmongUs.GameOptions;
using InnerNet;
using Reactor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TownOfUs.DisableAbilities;
using TownOfUs.Extensions;
using TownOfUs.Patches.NeutralRoles;
using TownOfUs.Patches;
using TownOfUs.Roles.Modifiers;
using UnityEngine;
using System.Collections;
using Object = UnityEngine.Object;
using Reactor.Utilities.Extensions;


namespace TownOfUs.Roles
{
    public class Icenberg : Role//, IVisualAlteration
    {
        //public static Sprite MimicSprite = TownOfUs.MimicSprite;
        //public static Sprite HackSprite = TownOfUs.HackSprite;
        public static Sprite LockSprite = TownOfUs.LockSprite;
        public static Sprite FreezeSprite = TownOfUs.FreezeSprite;

        public Icenberg(PlayerControl owner) : base(owner)
        {
            Name = "Icenberg";
            Color = Patches.Colors.Icenberg;
            LastFreeze = DateTime.UtcNow;
            LastKill = DateTime.UtcNow;
            FreezeButton = null;
            KillTarget = null;
            RoleType = RoleEnum.Icenberg;
            AddToRoleHistory(RoleType);
            ImpostorText = () => "So cold... Ye?";
            TaskText = () => "Freeze to death and win\nFake Tasks:";
            Faction = Faction.NeutralKilling;
        }
        public PlayerControl ClosestPlayer;
        public PlayerControl Freezed;
        public DateTime LastFreeze { get; set; }
        public DateTime LastKill { get; set; }
        public KillButton FreezeButton { get; set; }
        public PlayerControl KillTarget { get; set; }
        public PlayerControl FreezeTarget { get; set; }
        public bool IsUsingFreeze { get; set; }
        public bool IcenbergWins { get; set; }

        internal override bool GameEnd(LogicGameFlowNormal __instance)
        {
            if (Player.Data.IsDead || Player.Data.Disconnected) return true;

            if (PlayerControl.AllPlayerControls.ToArray().Count(x => !x.Data.IsDead && !x.Data.Disconnected) <= 2 &&
                    PlayerControl.AllPlayerControls.ToArray().Count(x => !x.Data.IsDead && !x.Data.Disconnected &&
                    (x.Data.IsImpostor() || x.Is(Faction.NeutralKilling) || x.IsCrewKiller())) == 1)
            {
                Utils.Rpc(CustomRPC.IcenbergWin, Player.PlayerId);
                Wins();
                Utils.EndGame();
                return false;
            }

            return false;
        }

        public void Wins()
        {
            //System.Console.WriteLine("Reached Here - Glitch Edition");
            IcenbergWins = true;
        }

        protected override void IntroPrefix(IntroCutscene._ShowTeam_d__38 __instance)
        {
            var icenbergTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            icenbergTeam.Add(PlayerControl.LocalPlayer);
            __instance.teamToShow = icenbergTeam;
        }

        public void Update(HudManager __instance)
        {
            if (HudManager.Instance?.Chat != null)
            {
                foreach (var bubble in HudManager.Instance.Chat.chatBubblePool.activeChildren)
                {
                    if (bubble.Cast<ChatBubble>().NameText != null &&
                        Player.Data.PlayerName == bubble.Cast<ChatBubble>().NameText.text)
                    {
                        bubble.Cast<ChatBubble>().NameText.color = Color;
                    }
                }
            }

            FixedUpdate(__instance);
        }

        public void FixedUpdate(HudManager __instance)
        {
            KillButtonHandler.KillButtonUpdate(this, __instance);

            FreezeButtonHandler.FreezeButtonUpdate(this, __instance);

            if (__instance.KillButton != null && Player.Data.IsDead)
                __instance.KillButton.SetTarget(null);

            if (FreezeButton != null && Player.Data.IsDead)
                FreezeButton.SetTarget(null);
        }

        public bool UseAbility(KillButton __instance)
        {
            if (__instance == FreezeButton)
            {
                FreezeButtonHandler.FreezeButtonPress(this);
                Debug.Log("FreezeButton click");
            }
            else
                KillButtonHandler.KillButtonPress(this);

            return false;
        }

        public void RpcSetFreezed(PlayerControl freezed)
        {
            //Utils.Rpc(CustomRPC.Freeze, Player.PlayerId, freezed.PlayerId);
            Coroutines.Start(AbilityCoroutine.Freeze(this, freezed));
            //SetFreezed(freezed);
        }
        public static class AbilityCoroutine
        {
            public static Dictionary<byte, DateTime> tickDictionary = new();

            public static IEnumerator Freeze(Icenberg __instance, PlayerControl freezePlayer)
            {
                __instance.LastFreeze = DateTime.UtcNow;
                Utils.Rpc(CustomRPC.Freeze, PlayerControl.LocalPlayer.PlayerId, freezePlayer.PlayerId);

                if (!Utils.AbilityUsed(PlayerControl.LocalPlayer)) yield break;

                Debug.Log("Freeze activation time: " + __instance.LastFreeze);

                //Coroutines.Start(CheckFreezeDuration(__instance.LastFreeze, CustomGameOptions.FreezeDuration));

                var freezeText = new GameObject("_Player").AddComponent<ImportantTextTask>();
                freezeText.transform.SetParent(PlayerControl.LocalPlayer.transform, false);
                freezeText.Text = $"{__instance.ColorString}Freezing {freezePlayer.Data.PlayerName} ({CustomGameOptions.FreezeDuration}s)</color>";
                PlayerControl.LocalPlayer.myTasks.Insert(0, freezeText);

                Coroutines.Start(DisableAbility.StopAbility(CustomGameOptions.FreezeDuration));
                __instance.FreezeTarget = freezePlayer;

                while (true)
                {
                    var elapsedTime = (float)(DateTime.UtcNow - __instance.LastFreeze).TotalSeconds;
                    var remainingTime = CustomGameOptions.FreezeDuration - elapsedTime;
                    Debug.Log($"Total freeze time elapsed: {elapsedTime}s");

                    if (__instance.Player.Data.IsDead || remainingTime <= 0)
                    {
                        Debug.Log("Freeze duration expired or player is dead. Unfreezing...");
                        break;
                    }

                    freezeText.Text = $"{__instance.ColorString}Freezing {freezePlayer.Data.PlayerName} ({Math.Round(remainingTime)}s)</color>";
                    __instance.FreezeButton.SetCoolDown(remainingTime, CustomGameOptions.FreezeDuration);
                    yield return new WaitForSeconds(0.5f);
                }

                PlayerControl.LocalPlayer.myTasks.Remove(freezeText);
                __instance.LastFreeze = DateTime.UtcNow;
                //Utils.UnfreezeAll(PlayerControl.LocalPlayer, freezePlayer);
                //Utils.UnfreezeAll(freezePlayer, PlayerControl.LocalPlayer);
                __instance.FreezeTarget = null;
                Debug.Log("Exiting freeze loop");
            }
            public static IEnumerator CheckFreezeDuration(DateTime freezeActivation, float freezeDuration)
            {
                while ((DateTime.UtcNow - freezeActivation).TotalSeconds < freezeDuration)
                {
                    yield return new WaitForSeconds(0.5f);
                }

                // Odmrażanie wszystkich po zakończeniu czasu
                UnfreezeAllPlayers();
            }
            public static void UnfreezeAllPlayers()
            {
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player.MyPhysics != null)
                    {
                        player.MyPhysics.enabled = true;
                    }
                }
                Debug.Log("All players have been unfrozen.");
            }
        }

        //public void SetFreezed(PlayerControl freezed)
        //{
        //    LastFreeze = DateTime.UtcNow;
        //    Freezed = freezed;
        //}
        //public static class AbilityCoroutine
        //{
        //    public static Dictionary<byte, DateTime> tickDictionary = new();
        //    public static IEnumerator Freeze(Icenberg __instance, PlayerControl freezePlayer)
        //    {
        //        Utils.Rpc(CustomRPC.Freeze, PlayerControl.LocalPlayer.PlayerId, freezePlayer.PlayerId);

        //        var abilityUsed = Utils.AbilityUsed(PlayerControl.LocalPlayer);
        //        if (!abilityUsed) yield break;

        //        var player = PlayerControl.LocalPlayer;
        //        Debug.Log("Ability used: " + abilityUsed);
        //        //Utils.Freeze(player, freezePlayer);
        //        var freezeActivation = DateTime.UtcNow;
        //        Debug.Log("Freeze activation time: " + freezeActivation);
        //        //var mimicActivation = DateTime.UtcNow;
        //        GameObject[] lockImg = { null, null, null };
        //        var freezeText = new GameObject("_Player").AddComponent<ImportantTextTask>();
        //        freezeText.transform.SetParent(PlayerControl.LocalPlayer.transform, false);
        //        freezeText.Text =
        //            $"{__instance.ColorString}Freezing {freezePlayer.Data.PlayerName} ({CustomGameOptions.FreezeDuration}s)</color>";
        //        PlayerControl.LocalPlayer.myTasks.Insert(0, freezeText);

        //        Coroutines.Start(DisableAbility.StopAbility(CustomGameOptions.FreezeDuration));
        //        __instance.FreezeTarget = freezePlayer;
        //        var utcNow = DateTime.UtcNow;
        //        ////var timeSpan = utcNow - LastRampaged;
        //        //var num = CustomGameOptions.RampageCd * 1000f;
        //        //var flag2 = num - (float)timeSpan.TotalMilliseconds < 0f;
        //        //return (num - (float)timeSpan.TotalMilliseconds) / 1000f;
        //        var timeSpan = DateTime.UtcNow - freezeActivation;
        //        var duration = CustomGameOptions.FreezeDuration * 1000f;
        //        //var totalFreezeTime = (DateTime.UtcNow - freezeActivation).TotalMilliseconds / 1000;
        //        var totalFreezeTime = (duration - (float)timeSpan.TotalMilliseconds) / 1000f;
        //        //bool a = true;
        //        //    a = false;
        //        while (true)
        //        {
        //            Debug.Log("Inside freeze loop");
        //            //__instance.IsUsingFreeze = true;
        //            Debug.Log("Total freeze time: " + totalFreezeTime);
        //            if (__instance.Player.Data.IsDead)
        //            {
        //                totalFreezeTime = CustomGameOptions.FreezeDuration;
        //                Debug.Log("Player is dead. Forcing exit of loop.");
        //            }
        //            freezeText.Text =
        //                $"{__instance.ColorString}Freezing {freezePlayer.Data.PlayerName} ({CustomGameOptions.FreezeDuration - Math.Round(totalFreezeTime)}s)</color>";
        //            //if (totalFreezeTime > CustomGameOptions.FreezeDuration ||
        //            if (totalFreezeTime < CustomGameOptions.FreezeDuration ||
        //                PlayerControl.LocalPlayer.Data.IsDead ||
        //                AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Ended)
        //            {
        //                Debug.Log("Inside 3rd if");
        //                if (PlayerControl.LocalPlayer != freezePlayer)
        //                {
        //                    Debug.Log("Inside local player != freezePlayer");

        //                    //if (HudManager.Instance.KillButton != null)
        //                    //{
        //                    //    if (lockImg[0] == null)
        //                    //    {
        //                    //        lockImg[0] = new GameObject();
        //                    //        var lockImgR = lockImg[0].AddComponent<SpriteRenderer>();
        //                    //        lockImgR.sprite = LockSprite;
        //                    //    }

        //                    //    lockImg[0].layer = 5;
        //                    //    lockImg[0].transform.position =
        //                    //        new Vector3(HudManager.Instance.KillButton.transform.position.x,
        //                    //            HudManager.Instance.KillButton.transform.position.y, -50f);
        //                    //}

        //                    //var role = GetRole(PlayerControl.LocalPlayer);
        //                    //if (role?.ExtraButtons.Count > 0)
        //                    //{
        //                    //    if (lockImg[1] == null)
        //                    //    {
        //                    //        lockImg[1] = new GameObject();
        //                    //        var lockImgR = lockImg[1].AddComponent<SpriteRenderer>();
        //                    //        lockImgR.sprite = LockSprite;
        //                    //    }

        //                    //    lockImg[1].transform.position = new Vector3(
        //                    //        role.ExtraButtons[0].transform.position.x,
        //                    //        role.ExtraButtons[0].transform.position.y, -50f);
        //                    //    lockImg[1].layer = 5;
        //                    //}

        //                    //if (HudManager.Instance.ReportButton != null)
        //                    //{
        //                    //    if (lockImg[2] == null)
        //                    //    {
        //                    //        lockImg[2] = new GameObject();
        //                    //        var lockImgR = lockImg[2].AddComponent<SpriteRenderer>();
        //                    //        lockImgR.sprite = LockSprite;
        //                    //    }

        //                    //    lockImg[2].transform.position =
        //                    //        new Vector3(HudManager.Instance.ReportButton.transform.position.x,
        //                    //            HudManager.Instance.ReportButton.transform.position.y, -50f);
        //                    //    lockImg[2].layer = 5;
        //                    //    HudManager.Instance.ReportButton.enabled = false;
        //                    //    HudManager.Instance.ReportButton.SetActive(false);
        //                    //}
        //                    //var totalFreezetime = (DateTime.UtcNow - tickDictionary[freezePlayer.PlayerId]).TotalMilliseconds /
        //                    //                1000;
        //                    freezeText.Text =
        //                    $"{"<color=#" + Colors.Glitch.ToHtmlStringRGBA() + ">"}Hacked {freezePlayer.Data.PlayerName} ({CustomGameOptions.FreezeDuration - Math.Round(totalFreezeTime)}s)</color>";
        //                    Debug.Log("Checking break conditions. TotalFreezeTime: " + totalFreezeTime);
        //                    if (MeetingHud.Instance || totalFreezeTime > CustomGameOptions.FreezeDuration || freezePlayer?.Data.IsDead != false || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
        //                    {
        //                        //foreach (var obj in lockImg)
        //                        //{
        //                        //    obj?.SetActive(false);
        //                        //}

        //                        //if (PlayerControl.LocalPlayer == freezePlayer)
        //                        //{
        //                        //    HudManager.Instance.ReportButton.enabled = true;
        //                        //}

        //                        PlayerControl.LocalPlayer.myTasks.Remove(freezeText);
        //                        //System.Console.WriteLine("Unsetting mimic");
        //                        __instance.LastFreeze = DateTime.UtcNow;
        //                        //__instance.IsUsingFreeze = false;
        //                        //Utils.Unfreeze(__instance.FreezeTarget);
        //                        Utils.UnfreezeAll();
        //                        __instance.FreezeTarget = null;

        //                        Debug.Log("Exiting freeze loop");
        //                        yield break;
        //                    }

        //                    //Utils.Freeze(player, freezePlayer);
        //                    __instance.FreezeButton.SetCoolDown(CustomGameOptions.FreezeDuration - (float)totalFreezeTime,
        //                        CustomGameOptions.FreezeDuration);
        //                    Debug.Log("Freeze cooldown set to: " + (CustomGameOptions.FreezeDuration - (float)totalFreezeTime));
        //                    yield return null;
        //                }
        //            }
        //        }
        //    }
        //} 

        public static class KillButtonHandler
        {
            public static void KillButtonUpdate(Icenberg __gInstance, HudManager __instance)
            {
                if (!__gInstance.Player.Data.IsImpostor() && Rewired.ReInput.players.GetPlayer(0).GetButtonDown(8))
                    __instance.KillButton.DoClick();

                __instance.KillButton.gameObject.SetActive((__instance.UseButton.isActiveAndEnabled || __instance.PetButton.isActiveAndEnabled)
                    && !MeetingHud.Instance && !__gInstance.Player.Data.IsDead
                    && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started);
                __instance.KillButton.SetCoolDown(
                    CustomGameOptions.GlitchKillCooldown -
                    (float)(DateTime.UtcNow - __gInstance.LastKill).TotalSeconds,
                    CustomGameOptions.GlitchKillCooldown);

                __instance.KillButton.SetTarget(null);
                __gInstance.KillTarget = null;

                if (__instance.KillButton.isActiveAndEnabled && __gInstance.Player.moveable)
                {
                    if ((CamouflageUnCamouflage.IsCamoed && CustomGameOptions.CamoCommsKillAnyone) || PlayerControl.LocalPlayer.IsHypnotised()) Utils.SetTarget(ref __gInstance.ClosestPlayer, __instance.KillButton);
                    else if (__gInstance.Player.IsLover()) Utils.SetTarget(ref __gInstance.ClosestPlayer, __instance.KillButton, float.NaN, PlayerControl.AllPlayerControls.ToArray().Where(x => !x.IsLover()).ToList());
                    else Utils.SetTarget(ref __gInstance.ClosestPlayer, __instance.KillButton);
                    __gInstance.KillTarget = __gInstance.ClosestPlayer;
                }

                __gInstance.KillTarget?.myRend().material.SetColor("_OutlineColor", __gInstance.Color);
            }

            public static void KillButtonPress(Icenberg __gInstance)
            {
                if (__gInstance.KillTarget != null)
                {
                    var interact = Utils.Interact(__gInstance.Player, __gInstance.KillTarget, true);
                    if (interact[4])
                    {
                        return;
                    }
                    else if (interact[0])
                    {
                        __gInstance.LastKill = DateTime.UtcNow;
                        return;
                    }
                    else if (interact[1])
                    {
                        __gInstance.LastKill = DateTime.UtcNow;
                        __gInstance.LastKill = __gInstance.LastKill.AddSeconds(CustomGameOptions.ProtectKCReset - CustomGameOptions.GlitchKillCooldown);
                        return;
                    }
                    else if (interact[2])
                    {
                        __gInstance.LastKill = DateTime.UtcNow;
                        __gInstance.LastKill = __gInstance.LastKill.AddSeconds(CustomGameOptions.VestKCReset - CustomGameOptions.GlitchKillCooldown);
                        return;
                    }
                    else if (interact[3])
                    {
                        return;
                    }
                    return;
                }
            }
        }

        public static class FreezeButtonHandler
        {
            public static void FreezeButtonUpdate(Icenberg __gInstance, HudManager __instance)
            {
                if (__gInstance.FreezeButton == null)
                {
                    __gInstance.FreezeButton = Object.Instantiate(__instance.KillButton, __instance.KillButton.transform.parent);
                    __gInstance.FreezeButton.gameObject.SetActive(true);
                    __gInstance.FreezeButton.graphic.enabled = true;
                }

                __gInstance.FreezeButton.graphic.sprite = FreezeSprite;

                __gInstance.FreezeButton.gameObject.SetActive((__instance.UseButton.isActiveAndEnabled || __instance.PetButton.isActiveAndEnabled)
                    && !MeetingHud.Instance && !__gInstance.Player.Data.IsDead
                    && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started);
                if (__instance.UseButton != null)
                {
                    __gInstance.FreezeButton.transform.position = new Vector3(
                        Camera.main.ScreenToWorldPoint(new Vector3(0, 0)).x + 0.75f,
                        __instance.UseButton.transform.position.y, __instance.UseButton.transform.position.z);
                }
                else
                {
                    __gInstance.FreezeButton.transform.position = new Vector3(
                        Camera.main.ScreenToWorldPoint(new Vector3(0, 0)).x + 0.75f,
                        __instance.PetButton.transform.position.y, __instance.PetButton.transform.position.z);
                }
                __gInstance.IsUsingFreeze = true;
                if (__gInstance.IsUsingFreeze)
                {
                    __gInstance.FreezeButton.graphic.material.SetFloat("_Desat", 0f);
                    __gInstance.FreezeButton.graphic.color = Palette.EnabledColor;
                }
                else if (!__gInstance.FreezeButton.isCoolingDown && __gInstance.Player.moveable)
                {
                    __gInstance.FreezeButton.isCoolingDown = false;
                    __gInstance.FreezeButton.graphic.material.SetFloat("_Desat", 0f);
                    __gInstance.FreezeButton.graphic.color = Palette.EnabledColor;
                    if (Rewired.ReInput.players.GetPlayer(0).GetButtonDown("ToU bb/disperse/mimic/freeze")) __gInstance.FreezeButton.DoClick();
                }
                else
                {
                    __gInstance.FreezeButton.isCoolingDown = true;
                    __gInstance.FreezeButton.graphic.material.SetFloat("_Desat", 1f);
                    __gInstance.FreezeButton.graphic.color = Palette.DisabledClear;
                }
                if (__gInstance.IsUsingFreeze)
                {
                    __gInstance.FreezeButton.SetCoolDown(
                        CustomGameOptions.FreezeCooldown -
                        (float)(DateTime.UtcNow - __gInstance.LastFreeze).TotalSeconds,
                        CustomGameOptions.FreezeCooldown);
                }
            }

            public static void FreezeButtonPress(Icenberg __gInstance)
            {
                List<byte> freezeTargets = new List<byte>();
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player != __gInstance.Player && !player.Data.Disconnected)
                    {
                        if (!player.Data.IsDead) freezeTargets.Add(player.PlayerId);
                        else
                        {
                            foreach (var body in Object.FindObjectsOfType<DeadBody>())
                            {
                                if (body.ParentId == player.PlayerId) freezeTargets.Add(player.PlayerId);
                            }
                        }
                    }
                }
                byte[] freezetargetIDs = freezeTargets.ToArray();
                var pk = new PlayerMenu((x) =>
                {
                    Debug.Log($"SET FREEZED {x.PlayerId}   {x.name}");
                    __gInstance.RpcSetFreezed(x);
                    //Coroutines.Start(Utils.FlashCoroutine(Colors.Sheriff));
                }, (y) =>
                {
                    return freezetargetIDs.Contains(y.PlayerId);
                });
                Coroutines.Start(pk.Open(0f, true));
            }
        }
    }
}