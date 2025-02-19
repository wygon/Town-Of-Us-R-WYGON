using System;
using System.Linq;
using Reactor.Utilities.Extensions;
using TownOfUs.Roles;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TownOfUs.CrewmateRoles.PoliticianMod
{
    public class AddRevealButtonPolitician
    {
        public static Sprite RevealSprite => TownOfUs.RevealSprite;

        public static void GenButton(Politician role, int index)
        {
            var confirmButton = MeetingHud.Instance.playerStates[index].Buttons.transform.GetChild(0).gameObject;

            var newButton = Object.Instantiate(confirmButton, MeetingHud.Instance.playerStates[index].transform);
            var renderer = newButton.GetComponent<SpriteRenderer>();
            var passive = newButton.GetComponent<PassiveButton>();

            renderer.sprite = RevealSprite;
            newButton.transform.position = confirmButton.transform.position - new Vector3(0.75f, 0f, 0f);
            newButton.transform.localScale *= 0.8f;
            newButton.layer = 5;
            newButton.transform.parent = confirmButton.transform.parent.parent;

            passive.OnClick = new Button.ButtonClickedEvent();
            passive.OnClick.AddListener(Reveal(role));
            role.RevealButton = newButton;
        }


        private static Action Reveal(Politician role)
        {
            void Listener()
            {
                role.RevealButton.Destroy();
                if (role.CampaignedPlayers.ToArray().Where(x => Utils.PlayerById(x) != null && Utils.PlayerById(x).Data != null && !Utils.PlayerById(x).Data.IsDead && !Utils.PlayerById(x).Data.Disconnected && Utils.PlayerById(x).Is(Faction.Crewmates)).ToList().Count * 2 >=
                    PlayerControl.AllPlayerControls.ToArray().Where(x => !x.Data.IsDead && !x.Data.Disconnected && x.Is(Faction.Crewmates) && !x.Is(RoleEnum.Politician)).ToList().Count)
                {
                    Role.RoleDictionary.Remove(role.Player.PlayerId);
                    var mayorRole = new Mayor(role.Player);
                    mayorRole.Revealed = true;
                    mayorRole.RegenTask();
                    Utils.Rpc(CustomRPC.Elect, role.Player.PlayerId);
                }
                else
                {
                    role.CanCampaign = false;
                    DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "You need to campaign more Crewmates! However, you may not campaign next round");
                }
            }

            return Listener;
        }

        public static void AddPoliticianButtons(MeetingHud __instance)
        {
            foreach (var role in Role.GetRoles(RoleEnum.Politician))
            {
                var politician = (Politician)role;
                politician.RevealButton.Destroy();
            }

            if (PlayerControl.LocalPlayer.Data.IsDead) return;
            if (!PlayerControl.LocalPlayer.Is(RoleEnum.Politician)) return;
            if (PlayerControl.LocalPlayer.IsJailed()) return;
            var politicianrole = Role.GetRole<Politician>(PlayerControl.LocalPlayer);
            politicianrole.CanCampaign = true;
            for (var i = 0; i < __instance.playerStates.Length; i++)
                if (PlayerControl.LocalPlayer.PlayerId == __instance.playerStates[i].TargetPlayerId)
                {
                    GenButton(politicianrole, i);
                }
        }
    }
}