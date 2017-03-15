using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTANetworkServer;
using GTANetworkShared;

using Marten;
using GTAIdentity.Models;
using GTAIdentity.Util;
using Common.Logging;

namespace GTAIdentity.Modules
{
    /// <summary>
    /// This identity module handles account logins/logouts and keeps track of the currently logged players. (as well as their <see cref="Account"/> and <see cref="Character"/> references)
    /// </summary>
    public class IdentityModule
    {
        protected IDocumentStore Store { get; }
        protected API API { get; }
        protected ILog Log { get; }


        public IReadOnlyList<LoggedPlayer> LoggedPlayers { get => _loggedPlayers?.AsReadOnly(); }
        public ClientRestrictions ClientRestrictions = ClientRestrictions.Free;

        protected List<LoggedPlayer> _loggedPlayers;

        public virtual event Action<Account> AccountRegistered;
        public event Action<LoggedPlayer> AccountLoggedIn;
        public event Action<LoggedPlayer> AccountLoggedOut;
        public event Action<Character> CharacterRegistered;
        public event Action<Character> CharacterLoggedIn;
        public event Action<Character> CharacterLoggedOut;

        public IdentityModule(IDocumentStore store,API api)
        {
            Store = store;
            API = api;
            Log = LogManager.GetLogger<IdentityModule>();

            _loggedPlayers = new List<LoggedPlayer>(API.getMaxPlayers());

            api.onPlayerConnected += API_onPlayerConnected;
            api.onPlayerDisconnected += API_onPlayerDisconnected;

            Log.Info("Identity module started.");
            LogStatistics();
        }

        private void API_onPlayerConnected(Client player)
        {
            PromptPlayer(player);
            UpdateFreeze(player, promptPlayer: false);
        }

        
        private void API_onPlayerDisconnected(Client player, string reason)
        {
            ClientLogout(player);
        }

        public void ClientRegister(Client client, string username, string password)
        {
            if(FindLoggedPlayer(client) != null)
            {
                client.sendChatMessage("Identity Module", "You're already logged in.");
                Log.Debug($"{client.name} tried to register while already logged.");
                return;
            }
            using (var session = Store.LightweightSession())
            {
                if (session.Query<Account>().Any(acc => acc.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    Log.Debug($"{client.name} tried to register with an already occupied username \"{username}\".");
                    client.sendChatMessage("Identity Module", "An account with that username already exists.");
                    return;
                }
                var account = new Account(username, password);
                session.Store(account);
                session.SaveChanges();
                AccountRegistered?.Invoke(account);
                Log.Info($"{client.name} has registered with the username {username}.");
                client.sendChatMessage("Identity Module", $"Registered with the username {username} successfully. You may now login.");
            }
        }

        public void ClientLogin(Client client, string username, string password)
        {
            if (LoggedPlayers.Any(lp => lp.Account.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                Log.Debug($"{client.name} tried to log into a logged in player of name {username}.");
                client.sendChatMessage("Identity Module", "There's already a logged in player with that username.");
                return;
            }
            if(FindLoggedPlayer(client) != null)
            {
                Log.Debug($"{client.name} tried to login while already logged to an account.");
                client.sendChatMessage("Identity Module", "You're already logged in.");
                return;
            }
            using (var session = Store.LightweightSession())
            {
                var account = session.Query<Account>().FirstOrDefault(acc => acc.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (account == null)
                {
                    Log.Debug($"{client.name} tried to log to a non-existant account of name {username}");
                    client.sendChatMessage("Identity Module", "No account with that username was found.");
                    return;
                }
                if (!account.VerifyLogin(password))
                {
                    Log.Debug($"{client.name} tried to log into {username} with a wrong password.");
                    client.sendChatMessage("Identity Module", $"Error: incorrect password for username {username}.");
                    return;
                }
                var logged = new LoggedPlayer(client, account);
                AccountLoggedIn?.Invoke(logged);
                _loggedPlayers.Add(logged);
                Log.Info($"{client.name} has successfully logged into account {account.Username}");
                client.sendChatMessage("Identity Module", $"You've successfully logged in as {account.Username}. Your last login was at {account.LastLoggedDate}.");
                UpdateFreeze(client);
                account.LastLoggedDate = DateTime.Now;
                session.SaveChanges();

            }
        }

        public void ClientLogout(Client client)
        {
            var loggedPlayer = FindLoggedPlayer(client);
            if (loggedPlayer == null)
            {
                Log.Debug($"{client.name} tried to logout while already logged out.");
                client.sendChatMessage("Identity Module", "You're already logged out.");
                return;
            }
            _loggedPlayers.Remove(loggedPlayer);
            AccountLoggedOut?.Invoke(loggedPlayer);
            if (loggedPlayer.Character != null)
                CharacterLeave(client);
            Log.Info($"{client.name} logged out of account {loggedPlayer.Account.Username}.");
            client.sendChatMessage("Identity Module", "Logged out succesfully.");
            UpdateFreeze(client);
        }

        public void CharacterRegister(Client client,string charName)
        {
            var loggedPlayer = FindLoggedPlayer(client);
            if (loggedPlayer == null)
            {
                Log.Debug($"{client.name} tried to create a character while he's not logged to an account.");
                client.sendChatMessage(nameof(IdentityModule), "You must be logged into an account before creating a character.");
                return;
            }
            if (loggedPlayer.Character != null)
            {
                Log.Debug($"{client.name} tried to create a character while already logged to one.");
                client.sendChatMessage(nameof(IdentityModule), "You're already logged into a character.");
                return;
            }
            using (var session = Store.OpenSession())
            {
                var alreadyExisting = session.Query<Character>().Count(character => character.Name.Equals(charName,StringComparison.OrdinalIgnoreCase));
                if (alreadyExisting > 0)
                {
                    Log.Debug($"{client.name} tried to create an already existing character of name {charName}.");
                    client.sendChatMessage(nameof(IdentityModule), "There already exists a character with that name.");
                    return;
                }
                var chara = new Character(charName, loggedPlayer.Account.Id);
                session.Store(chara);
                session.SaveChanges();
                CharacterRegistered?.Invoke(chara);
                Log.Info($"{client.name} of account {loggedPlayer.Account.Username} has created character {chara.Name}.");
                client.sendChatMessage(nameof(IdentityModule), $"Character \"{chara.Name}\" registered, you may now enter it.");
            }
        }

        public void CharacterEnter(Client client,string charName)
        {
            var loggedPlayer = FindLoggedPlayer(client);
            if (loggedPlayer == null)
            {
                Log.Debug($"{client.name} tried to enter a character while not logged to an account.");
                client.sendChatMessage(nameof(IdentityModule), "You must be logged into an account before entering a character.");
                return;
            }
            if (loggedPlayer.Character != null)
            {
                Log.Debug($"{client.name} tried to log to a character while alerady in one.");
                client.sendChatMessage(nameof(IdentityModule), "You're already logged into a character.");
                return;
            }
            using (var session = Store.OpenSession())
            {
                var chara = session.Query<Character>().FirstOrDefault(character => character.AccountId == loggedPlayer.Account.Id && character.Name.Equals(charName,StringComparison.OrdinalIgnoreCase));
                if (chara == null)
                {
                    Log.Debug($"{client.name} of account {loggedPlayer.Account.Username} tried to access non-existant character \"{charName}\".");
                    client.sendChatMessage(nameof(IdentityModule), $"No character with the name of {charName} has been found under your account.");
                    return;
                }
                client.sendChatMessage(nameof(IdentityModule), $"You've successfully logged into character {charName}");
                Log.Info($"{loggedPlayer} successfully logged as character {charName}");
                loggedPlayer.Character = chara;
                loggedPlayer.Character.ApplyTo(client);
                CharacterLoggedIn?.Invoke(chara);
                UpdateFreeze(client);
            }
        }

        public void CharacterLeave(Client client)
        {
            var loggedPlayer = FindLoggedPlayer(client);
            if (loggedPlayer == null || loggedPlayer.Character == null)
            {
                Log.Debug($"{client.name} tried to leave a character while not logged into any account/character.");
                client.sendChatMessage(nameof(IdentityModule), "You're not logged into any account/character.");
                return;
            }
            
            CharacterLoggedOut?.Invoke(loggedPlayer.Character);
            loggedPlayer.Character = null;
            Log.Debug($"{client.name} succesfully unlogged from his character.");
            client.sendChatMessage(nameof(IdentityModule), $"You've successfully logged out of your character.");
            UpdateFreeze(client);
        }
        

        public void Whoami(Client client)
        {
            var info = $"Your name is {client.name}, nametag is {client.nametag}\n" +
                $"social club name is {client.socialClubName}\n" +
                $"address is {client.address}, handle is {client.handle}\n" +
                $"ACL group is {API.getPlayerAclGroup(client)}\n" +
                $"location is {API.toJson(client.position)} with rotation {API.toJson(client.rotation)}.\n";
            var loggedPlayer = FindLoggedPlayer(client);
            if (loggedPlayer != null)
            {
                info += $"You are logged into {loggedPlayer.Account.Username} which has the following id: {loggedPlayer.Account.Id} and the following role: {loggedPlayer.Account.Role}\n";
                if (loggedPlayer.Character != null)
                {
                    info += $"You're also logged into character {loggedPlayer.Character.Name}.\n";
                }
                using (var session = Store.OpenSession())
                {
                    var bans = session.Query<Ban>().Count(ban => ban.AccountId == loggedPlayer.Account.Id);
                    info += $"You have {bans} bans on record\n";
                    var chars = session.Query<Character>().Where(c => c.AccountId == loggedPlayer.Account.Id).ToList();
                    if (chars.Count == 0)
                        info += $"You have no characters.\n";
                    else
                        info += $"You have {chars.Count} characters: {string.Join(",", chars.Select(c => c.Name))}";
                }
            }
            else
                info += "You aren't logged in.";
            info.Split('\n').ToList().ForEach(line => client.sendChatMessage(nameof(IdentityModule), line));
            Log.Debug($"{client.name} called Whoami: {info}");
        }

        public void LogStatistics()
        {
            using (var session = Store.OpenSession())
            {
                var accounts = session.Query<Account>().Count();
                var characters = session.Query<Character>().Count();
                Log.Info($"There are a total of {accounts} accounts and {characters} characters registered.");
            }
        }

        // A welcome message, notifies the player if he has to connect to an account and a character.
        private void PromptPlayer(Client client)
        {
            var info = "Welcome to the server.";
            if (ClientRestrictions >= ClientRestrictions.RequireAccountLogin)
                info += "\nYou'll need to register or login into an account to proceed, see /register and /login, respectively.";
            if (ClientRestrictions >= ClientRestrictions.RequireCharacterLogin)
                info += "\nYou'll also need to create or enter a character, see /createCharacter and /enterCharacter, respectively.";
            client.sendChatMessage(nameof(IdentityModule), info);
        }

        // Ensures that the player is frozen if logged out from an account,character, or not at all, depending on ClientRestrictions.
        // TODO: teleport him to some nice area, change camera, stuff like that..
        // Exploit: if I implement admin-freezing, players could evade it by invoking this function. Should add variable that checks if a player is admin-frozen.
        private void UpdateFreeze(Client client,bool promptPlayer = true)
        {

            var loggedPlayer = FindLoggedPlayer(client);
            if (ClientRestrictions >= ClientRestrictions.RequireAccountLogin && loggedPlayer == null)
            {
                client.freezePosition = true;
                if (promptPlayer)
                    client.sendChatMessage(nameof(IdentityModule), "You'll need to login to an account to proceed.");
                return;
            }
            if(ClientRestrictions >= ClientRestrictions.RequireCharacterLogin && loggedPlayer.Character == null)
            {
                client.freezePosition = true;
                if (promptPlayer)
                    client.sendChatMessage(nameof(IdentityModule), "You'll need to enter a character to proceed.");
                return;
            }
            client.freezePosition = false;
        }

        public LoggedPlayer FindLoggedPlayer(Client client)
        {
            return LoggedPlayers.FirstOrDefault(lp => lp.Client == client);
        }
        public LoggedPlayer FindLoggedPlayer(Account account)
        {
            return LoggedPlayers.FirstOrDefault(lp => lp.Account == account);
        }
        public LoggedPlayer FindLoggedPlayer(Character character)
        {
            return LoggedPlayers.FirstOrDefault(lp => lp.Character == character);
        }
        public LoggedPlayer FindLoggedPlayer(NetHandle handle)
        {
            return LoggedPlayers.FirstOrDefault(lp => lp.Client.handle == handle);
        }

    }

    /// <summary>
    /// Defines whether players are required to login an account, and maybe to a character, or not at all, in order to proceed.
    /// </summary>
    public enum ClientRestrictions
    {
        Free = 0, 
        RequireAccountLogin = 1,
        RequireCharacterLogin = 2
    }

}
