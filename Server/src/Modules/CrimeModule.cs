﻿using GTAIdentity.Models;
using GTANetworkServer;
using GTANetworkShared;
using Marten;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using GTAIdentity.Util;

namespace GTAIdentity.Modules
{

    public class CrimeModule
    {
        protected API API { get; }
        protected IdentityModule Identity { get; }
        protected IDocumentStore Store { get;  }
        protected ILog Log { get; }

        public const float ArrestRange = 10f;

        public CrimeModule(API api,IdentityModule identity,IDocumentStore store)
        {
            API = api;
            Identity = identity;
            Store = store;
            Log = LogManager.GetLogger<CrimeModule>();

            API.onPlayerDeath += API_onPlayerDeath;
            Log.Info("Crime module initialized.");
        }

        private void API_onPlayerDeath(Client player, NetHandle entityKiller, int weapon)
        {
            LoggedPlayer attacker = null;
            LoggedPlayer victim = Identity.FindLoggedPlayer(player);
            if (API.getEntityType(entityKiller) == EntityType.Vehicle)
            {
                Log.Debug($"{player.name} was killed by a vehicle. TODO: handle this");
                // TODO wait for getEntityFromHandle to be available to find the player in the car
            }
            else if(API.getEntityType(entityKiller) == EntityType.Player)
            {
                attacker = Identity.FindLoggedPlayer(entityKiller);
            }
            else
            {
                Log.Debug($"{player.name} was killed by unknown type of handle {entityKiller}");
            }
            if (victim != null && attacker != null && victim != attacker)
            {
                if (!victim.CharacterLogged)
                {
                    Log.Warn($"Victim {victim} isn't logged into a character.");
                    return;
                }
                if(!attacker.CharacterLogged)
                {
                    Log.Warn($"Attacker {attacker} isn't logged into a character.");
                    return;
                }
                HandleGameplayDeath(victim.Character, attacker.Character);
            }
        }


        // TODO implement game logic here, anti-griefing mechanism, etc..
        private void HandleGameplayDeath(Character victim, Character attacker)
        {

            Log.Debug($"Handling the killing of {victim} by {attacker}.");
            if (attacker is Cop && victim is Cop)
            {
                Log.Info($"{attacker} has teamkilled {victim}");
            }
            else if (attacker is Cop && victim is Civilian civ)
            {
                if (civ.WantedLevel >= Wanted.KillAuthorized)
                {
                    Log.Info($"{attacker} has rightfully killed {victim}");
                }
                else
                {
                    Log.Info($"{attacker} has wrongfully killed {victim}");
                }
            }
            else if (attacker is Civilian)
            {
                Log.Info($"{attacker} has murdered {victim}");

            }
        }

        public async Task Arrest(Cop cop,Civilian civ)
        {
            var loggedCiv = Identity.FindLoggedPlayer(civ);
            var loggedCop = Identity.FindLoggedPlayer(cop);
            var copClient = loggedCop.Client;
            var civClient = loggedCiv.Client;

            
            if (civ.WantedLevel < Wanted.Arrestable)
            {
                Log.Debug($"{cop} tried to arrest {civ} who is not arrestable.");
                Identity.FindLoggedPlayer(civ).Client.sendChatMessage($"You can't arrest {civ.Name}, his wanted level isn't high enough.");
                return;
            }
            var distanceVector = (copClient.position - civClient.position);
            var distance = distanceVector.Length();
            if (distance <= ArrestRange)
            {
                copClient.sendChatMessage($"You're too far from {civ.Name}, you need to be within {ArrestRange} from the target in order to arrest him.");
                return;
            }
            var response = await API.RequestResponseFlow(copClient, "check_los", "check_los_response", args: civClient.position);
            if (response.Length == 1 && response[0] is bool result)
            {
                if (result)
                {
                    Log.Info($"{cop} has arrested {civ}.");
                    copClient.sendChatMessage($"You've succesfully arrested {civ.Name}!");
                } 
                else
                {
                    Log.Debug($"{cop} has no line of sight to {civ}, so he can't arrest him.");
                    copClient.sendChatMessage($"You have no line of sight to {civ.Name}");
                }
            }
            else
            {
                Log.Error($"Invalid response sent by 'check_los_response' within Arrest method.");
                copClient.sendChatMessage("Something went wrong while checking for line of sight with the suspect.");
                return;
            }

        }

        public async Task ArrestNearby(Client caller)
        {
            var callerChar = Identity.FindLoggedPlayer(caller);
            if (callerChar?.Character is Cop cop)
            {
                var nearbyCrim = Identity.LoggedPlayers.FirstOrDefault(lp =>
                    lp.Character is Civilian civ &&
                    civ.WantedLevel >= Wanted.Arrestable
                    && (lp.Client.position - caller.position).Length() < ArrestRange);
                if (nearbyCrim == null)
                {
                    caller.sendChatMessage("There are no arrestable criminals within arresting range.");
                    Log.Debug($"{caller.name} tried to arrest somebody, but no matching criminals were found.");
                    return;
                }
                await Arrest(cop, (Civilian)nearbyCrim.Character);
            }
            else
            {
                caller.sendChatMessage("You're unlogged or not a cop.");
                Log.Debug($"{caller.name} who isn't logged (as a cop) tried to arrest somebody.");
            }
        }


    }
}
