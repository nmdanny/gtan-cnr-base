using GTANetworkServer;
using Marten;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAIdentity.Models;
using GTAIdentity.Util;
using System.Linq.Expressions;
using Common.Logging;

namespace GTAIdentity.Modules
{
    /// <summary>
    /// The admin module provides admin commands, and enforces bans.
    /// </summary>
    public class AdminModule
    {
        protected IDocumentStore Store { get; }
        protected IdentityModule Identity { get; }
        protected API API { get; }
        protected ILog Log { get;  }

        public AdminModule(IDocumentStore store, IdentityModule identity, API api)
        {
            Store = store;
            Identity = identity;
            API = api;
            Log = LogManager.GetLogger<AdminModule>();

            API.onPlayerBeginConnect += EnsureConnectionBans;
            Identity.AccountLoggedIn += EnsureAccountBans;
            Log.Info("Admin module started.");
            LogBanStatistics();
        }

        /// <summary>
        /// Checks if a player has a certain role.
        /// </summary>
        /// <param name="caller">The player whose role we're checking</param>
        /// <param name="minimumRole">The minimum role we're looking for</param>
        /// <param name="account">An out reference to the account that's associated with the player, will be null if he's disconnected.</param>
        /// <param name="notifyPlayer">Whether to notify the player if he doesn't have the required role.</param>
        /// <returns>Whether he has that role or not.</returns>
        public bool EnsureRole(Client caller, Role minimumRole, out Account account, bool notifyPlayer = true)
        {
            account = Identity.FindLoggedPlayer(caller)?.Account;
            if (account == null)
            {
                if (notifyPlayer)
                    caller.sendChatMessage(nameof(AdminModule), "Error, you're not logged in.");
                return false;
            }
            if (account.Role >= minimumRole)
            {
                return true;
            }
            Log.Debug($"{account.Username} tried to perform an action he's not authorized to do, his role is {account.Role}, minimum is {minimumRole}.");
            caller.sendChatMessage(nameof(AdminModule), $"Error, you're not authorized to do this action, your role is {account.Role} but you need to be at least {minimumRole}. ");
            return false;
        }

        /// <summary>
        /// Bans a player.
        /// </summary>
        /// <param name="caller">The player ordering the ban.</param>
        /// <param name="player">The name of the player that is being banned, or part of it.</param>
        /// <param name="duration">The duration</param>
        /// <param name="durationUnits">The duration unit, must contain either 'second', 'minute', 'hour' or 'day'.</param>
        /// <param name="reason">The reason for the player's ban.</param>
        public void BanPlayer(Client caller, string player, int duration, string durationUnits = "hour", string reason = "No reason given")
        {
            if (!EnsureRole(caller, Role.Admin, out var adminAcc))
                return;
            try
            {
                var banDuration = FormatTimespan(duration, durationUnits);
                var targets = API.getAllPlayers().Where(client => client.name.ToLower().Contains(player.ToLower()) || client.socialClubName.ToLower().Contains(player.ToLower())).ToList();
                if (targets.Count > 1)
                {
                    var playerNames = targets.Select(pl => $"[Name :{pl.name} SocialClub Name:{pl.socialClubName}]");
                    throw new InvalidOperationException($"Error, ambigious ban command: multiple players with the string '{player}' in their name/social-club handle have been found:\n"
                        + string.Join(",", playerNames) + "\n" +
                        "Please use a more specific name.");
                }
                if (targets.Count == 0)
                    throw new InvalidOperationException($"Error, no players with the name {player} were found. ");
                var playerClient = targets[0];
                var playerAccId = Identity.FindLoggedPlayer(playerClient)?.Account.Id;
                var ban = new Ban()
                {
                    AccountId = playerAccId,
                    Reason = reason,
                    Start = DateTime.Now,
                    End = DateTime.Now + banDuration,
                    KnownIPs = new HashSet<string> { playerClient.address },
                    BannerAccountId = adminAcc.Id,
                    SocialClubHandle = playerClient.socialClubName
                };
                using (var session = Store.LightweightSession())
                {
                    session.Store(ban);
                    session.SaveChanges();
                }
                caller.sendChatMessage(nameof(AdminModule), $"You've successfully banned player of name {playerClient.name}, social-club handle {playerClient.socialClubName}.");
                Log.Info($"{adminAcc.Username} has banned {playerClient.name}(SC handle \"{ban.SocialClubHandle}\") until {ban.End}, reason: {ban.Reason}.");
                playerClient.kick($"You've been banned, reason:\n {reason}");
            }
            catch (ArgumentException ex)
            {
                caller.sendChatMessage(nameof(AdminModule), "Error parsing your command: " + ex.Message);
                return;
            }
            catch (InvalidOperationException ex)
            {
                caller.sendChatMessage(nameof(AdminModule), ex.Message);
                return;
            }


        }

        private TimeSpan FormatTimespan(int timespan, string unit)
        {
            var unitString = unit.ToLower().Trim();
            if (unitString.StartsWith("hour"))
                return TimeSpan.FromHours(timespan);
            if (unitString.StartsWith("minute"))
                return TimeSpan.FromMinutes(timespan);
            if (unitString.StartsWith("second"))
                return TimeSpan.FromSeconds(timespan);
            if (unitString.StartsWith("day"))
                return TimeSpan.FromDays(timespan);
            throw new ArgumentException($"Unknown unit - {unit}");
        }

        // Ensure that whenever a player connects, he's automatically kicked if he's actively banned.
        private void EnsureConnectionBans(Client player, CancelEventArgs cancelConnection)
        {
            using (var session = Store.LightweightSession())
            {
                var activeBan = session.Query<Ban>().Where(Ban.IsBanActiveExpr).FirstOrDefault(ban => (ban.KnownIPs.Contains(player.address) || ban.SocialClubHandle == player.socialClubName));
                if (activeBan != null)
                {
                    player.kick($"You're currently banned:\n " + activeBan.GetExplanation());
                }
            }
        }

        // If a banned wasn't kicked upon connection (he may have changed his IP/social club handle), kick him once he logs into his account and add his current IP to the ban.
        private void EnsureAccountBans(LoggedPlayer player)
        {
            using (var session = Store.LightweightSession())
            {
                var activeBan = session.Query<Ban>().Where(Ban.IsBanActiveExpr).FirstOrDefault(ban => ban.AccountId == player.Account.Id);
                if (activeBan != null)
                {
                    activeBan.KnownIPs.Add(player.Client.address);
                    activeBan.SocialClubHandle = player.Client.socialClubName;
                    player.Client.kick($"You're currently banned:\n " + activeBan.GetExplanation());
                    session.Store(activeBan);
                    session.SaveChanges();
                    Log.Info($"A banned player sneaked past the IP and social club filtering, but re-banned on login. His social-club handle is {activeBan.SocialClubHandle}");
                }
            }
        }

        private void LogBanStatistics()
        {
            using (var session = Store.LightweightSession())
            {
                var banCount = session.Query<Ban>().Count();
                var activeBans = session.Query<Ban>().Where(Ban.IsBanActiveExpr).ToList();
                Log.Info($"Found {activeBans.Count} active bans out of {banCount} total bans.");
                var latestBans = session.Query<Ban>().Where(Ban.IsBanActiveExpr).OrderByDescending(ban => ban.Start).Take(10).ToList();
                if (latestBans.Count > 0)
                    Log.Info($"The latest active bans are:\n{string.Join("\n", latestBans)}");
            }
        }
    }
}
