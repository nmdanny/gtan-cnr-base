using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using GTANetworkServer;
using GTANetworkShared;
using GTAIdentity.Shared;
using Marten;
using GTAIdentity.Models;
using GTAIdentity.Util;
using Common.Logging;
using Newtonsoft.Json;

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


        public ClientRestrictions ClientRestrictions = ClientRestrictions.Free;

        public IReadOnlyList<IngameAccount> Accounts { get => accounts.Values.ToList().AsReadOnly(); }
        public IReadOnlyList<Player> Players { get => players.Values.ToList().AsReadOnly(); }
        protected Dictionary<Client, IngameAccount> accounts;
        protected Dictionary<Client, Player> players;

        public virtual event Action<Account> AccountRegistered;
        public event Action<IngameAccount> AccountLoggedIn;
        public event Action<IngameAccount> AccountLoggedOut;
        public event Action<Character> CharacterRegistered;
        public event Action<Character> CharacterLoggedIn;
        public event Action<Character> CharacterLoggedOut;


        public IngameAccount GetAccount(Client client)
        {
            try
            {
                return accounts[client];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }
        public Player GetPlayer(Client client)
        {
            try
            {
                return players[client];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public IdentityModule(IDocumentStore store, API api)
        {
            Store = store;
            API = api;
            Log = LogManager.GetLogger<IdentityModule>();

            accounts = new Dictionary<Client, IngameAccount>(API.getMaxPlayers());
            players = new Dictionary<Client, Player>(API.getMaxPlayers());

            api.onPlayerConnected += API_onPlayerConnected;
            api.onPlayerDisconnected += API_onPlayerDisconnected;
            api.OnClientEventTrigger<string>(IdentityEvents.characterSelected).Subscribe((tuple) =>
            {
                var (client, character) = tuple;
                CharacterEnter(client, character);
            });


            Log.Info("Identity module started.");
            LogStatistics();
        }

        private void API_onPlayerConnected(Client player)
        {
            PromptPlayer(player);
            UpdateFreeze(player, promptPlayer: false);
            FirePlayerStateChanged(player);
        }


        private void API_onPlayerDisconnected(Client player, string reason)
        {
            ClientLogout(player);
        }

        public void ClientRegister(Client client, string username, string password)
        {
            if (GetAccount(client) != null)
            {
                Log.ClientLog(client, "You're already logged in.");
                return;
            }

            using (var session = Store.LightweightSession())
            {
                if (session.Query<Account>().Any(acc => acc.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    Log.ClientLog(client, $"An account with the username \"{username}\" already exists.");
                    return;
                }
                var account = new Account(username, password);
                session.Store(account);
                session.SaveChanges();
                AccountRegistered?.Invoke(account);
                Log.Info($"{client.name} has registered with the username {username}.");
                client.sendChatMessage($"Registered with the username {username} successfully. You may now login.");
            }
        }

        public void ClientLogin(Client client, string username, string password)
        {
            if (Accounts.Any(acc => acc.Account.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                Log.ClientLog(client, $"There's already a logged in player with the name {username}.");
                return;
            }
            else if (GetAccount(client) is IngameAccount acc)
            {
                Log.ClientLog(client, $"You're already logged in as {acc.Account.Username}");
                return;
            }
            using (var session = Store.LightweightSession())
            {
                var account = session.Query<Account>().FirstOrDefault(acc => acc.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (account == null)
                {
                    Log.ClientLog(client, $"No account with the username \"{username}\" was found.");
                    return;
                }
                if (!account.VerifyLogin(password))
                {
                    Log.ClientLog(client, $"You've entered an incorrect password while entering \"{username}\".");
                    return;
                }
                var logged = new IngameAccount(client, account);
                AccountLoggedIn?.Invoke(logged);
                accounts.Add(client, logged);
                Log.Info($"{client.name} has successfully logged into account {account.Username}");
                client.sendChatMessage($"You've successfully logged in as {account.Username}. Your last login was at {account.LastLoggedDate}.");
                UpdateFreeze(client);
                FirePlayerStateChanged(client);
                account.LastLoggedDate = DateTime.Now;
                session.SaveChanges();

            }
        }

        public void ClientLogout(Client client)
        {
            var account = GetAccount(client);
            if (account == null)
            {
                Log.ClientLog(client, "You're already logged out.");
                return;
            }
            accounts.Remove(client);
            Log.Info($"{client.name} logged out of account {account.Account.Username}.");
            client.sendChatMessage($"Logged out succesfully out of {account.Account.Username}.");
            AccountLoggedOut?.Invoke(account);
            UpdateFreeze(client);
            FirePlayerStateChanged(client);
        }

        public void CharacterRegister(Client client, string charName)
        {
            var player = GetPlayer(client);
            var account = GetAccount(client);
            if (account == null)
            {
                Log.ClientLog(client, $"You can't create a character while not logged into an account.");
                return;
            }
            if (player != null)
            {
                Log.ClientLog(client, $"You're already inside character \"{player.Character.Name}\", you can't create a new one.");
                return;
            }
            using (var session = Store.OpenSession())
            {
                var alreadyExisting = session.Query<Character>().Count(ch => ch.Name.Equals(charName, StringComparison.OrdinalIgnoreCase));
                if (alreadyExisting > 0)
                {
                    Log.ClientLog(client, $"A character with the name \"{charName}\" alraedy exists.");
                    return;
                }
                var character = new Character(charName, client, account.Account.Id);
                session.Store(character);
                session.SaveChanges();
                CharacterRegistered?.Invoke(character);
                Log.Info($"{client.name} of account {account.Account.Username} has created character {character.Name}.");
                client.sendChatMessage(nameof(IdentityModule), $"Character \"{character.Name}\" registered, you may now enter it.");
            }
        }

        public void CharacterEnter(Client client, string charName)
        {
            var account = GetAccount(client);
            var player = GetPlayer(client);
            if (account == null)
            {
                Log.ClientLog(client, "You must be logged into an account before entering a character.");
                return;
            }
            if (player != null)
            {
                Log.ClientLog(client, $"You can't enter a character while already inside \"{player.Character.Name}\".");
                return;
            }
            using (var session = Store.OpenSession())
            {
                var character = session.Query<Character>().FirstOrDefault(ch => ch.AccountId == account.Account.Id && ch.Name.Equals(charName, StringComparison.OrdinalIgnoreCase));
                if (character == null)
                {
                    Log.ClientLog(client, $"No character of the name {charName} was found under your account.");
                    return;
                }
                player = new Player(client, character);
                player.Character.ApplyTo(client);
                players.Add(client, player);
                client.sendChatMessage(nameof(IdentityModule), $"You've successfully logged into character {charName}");
                Log.Info($"{client.name} successfully logged as character \"{charName}\"");
                CharacterLoggedIn?.Invoke(character);
                UpdateFreeze(client);
                FirePlayerStateChanged(client);
            }
        }

        public void CharacterLeave(Client client)
        {
            var player = GetPlayer(client);
            var account = GetAccount(client);
            if (account == null || player == null)
            {
                Log.ClientLog(client, "You tried to leave a character despite not being logged into any account/character.");
                return;
            }
            using (var session = Store.OpenSession())
            {
                var character = player.Character;
                character.UpdateFrom(client);
                session.Store(character);
                session.SaveChanges();
                players.Remove(client);
                Log.Debug($"{client.name} succesfully unlogged from his character.");
                client.sendChatMessage($"You've successfully logged out of your character \"{character.Name}\"");

                CharacterLoggedOut?.Invoke(player.Character);
                UpdateFreeze(client);
                FirePlayerStateChanged(client);
            }
        }


        public void Whoami(Client client)
        {
            var info = $"Your name is {client.name}, nametag is {client.nametag}\n" +
                $"social club name is {client.socialClubName}\n" +
                $"address is {client.address}, handle is {client.handle}\n" +
                $"ACL group is {API.getPlayerAclGroup(client)}\n" +
                $"location is {API.toJson(client.position)} with rotation {API.toJson(client.rotation)}.\n";
            var account = GetAccount(client);
            var player = GetPlayer(client);
            if (account != null)
            {
                info += $"You are logged into {account.Account.Username} which has the following id: {account.Account.Id} and the following role: {account.Account.Role}\n";
                if (player != null)
                {
                    info += $"You're also logged into character {player.Character.Name}.\n";
                }
                using (var session = Store.OpenSession())
                {
                    var bans = session.Query<Ban>().Count(ban => ban.AccountId == account.Account.Id);
                    info += $"You have {bans} bans on record\n";
                    var characters = session.Query<Character>().Where(c => c.AccountId == account.Account.Id).ToList();
                    if (characters.Count == 0)
                        info += $"You have no characters.\n";
                    else
                        info += $"You have {characters.Count} characters: {string.Join(",", characters.Select(c => c.Name))}";
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
        private void UpdateFreeze(Client client, bool promptPlayer = true)
        {

            var player = GetPlayer(client);
            var account = GetAccount(client);
            if (ClientRestrictions >= ClientRestrictions.RequireAccountLogin && account == null)
            {
                client.freezePosition = true;
                if (promptPlayer)
                    client.sendChatMessage(nameof(IdentityModule), "You'll need to login to an account to proceed.");
                return;
            }
            if (ClientRestrictions >= ClientRestrictions.RequireCharacterLogin && player == null)
            {
                client.freezePosition = true;
                if (promptPlayer)
                    client.sendChatMessage(nameof(IdentityModule), "You'll need to enter a character to proceed.");
                return;
            }
            client.freezePosition = false;
        }

        protected void FirePlayerStateChanged(Client client)
        {
            
            var account = GetAccount(client);
            var player = GetPlayer(client);
            if (account == null)
                API.RequestResponseFlow(client, IdentityEvents.playerStateChanged, new PlayerStateChange()
                {
                    Account = null, Character = null, PlayerState = PlayerState.Connected
                });
            else if (player == null)
            {
                using (var session = Store.LightweightSession())
                {
                    var charNames = session.Query<Character>().Where(ch => ch.AccountId == account.Account.Id).Select(ch => ch.Name);
                    API.RequestResponseFlow(client, IdentityEvents.playerStateChanged, new PlayerStateChange()
                {
                    Account = account.Account, Character = null, PlayerState = PlayerState.AccountLogged, CharacterNames = charNames
                });

                }
            }
            else
                API.RequestResponseFlow(client, IdentityEvents.playerStateChanged, new PlayerStateChange()
                {
                    Account = account.Account, Character = player.Character, PlayerState = PlayerState.CharacterLogged
                });
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
