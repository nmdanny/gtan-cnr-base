# gtan-cnr-base
My experimentation in creating a GTA Network gamemode/script and useful infrastructure.

Currently it barely does anything useful and doesn't really interact with GTA itself much, as I'm
focusing on implementing the infrastructure (registration, persistence, architecture)..

You can use this as a starting path to creating GTAN scripts. (You can remove all files and folders inside Server/src except 'ScriptMain.cs')
, the resource is a compiled .DLL (no need to mess with meta.xml too much)

## How to get build/use

* I used Visual Studio 2017 to build this.
* You'll need PostgreSQL 9.5 or higher installed

1. Clone this repo within the `GTA Network Server/resources` folder
2. Restore the NuGet packages
3. Set up a PostgreSQL server, there are many tutorials for that on the web.
   I'm using the connection string `host=localhost;database=GTAIdentity;password=postgres;username=postgres`, so use those credentials
   when setting up your DB if you're lazy like me.
4. If you aren't using the same credentials, see below on how to change the connection string.
5. Build the VS solution(Ctrl+Shift+B).
6. Don't forget to include this resource inside the `settings.xml` file at your GTA Network Server's root. For example:
   ```xml
     <resource src="gtan-cnr-base" />
   ```

*There might be some issues with the solution as it references files local to my computer, namely settings.xml and acl.xml,
just point them to the corresponding files in GTA Network Server root folder.*

**Visual Studio Debugging**

You can debug the project(a class library) while running a GTA Network server, simply by attaching the debugger to a GTANetworkServer.exe process.
For convenience, you should set it to autostart GTANetworkServer.exe in debug mode:

1. Right click the project('Server') in the VS solution explorer and press 'Properties'
2. Enter Debug
3. Select Configuration: All Configurations
4. Configure it accordingly:
    
   Start Action:
     * Start external program > "C:\Path\To\GTANetwork Server\GTANetworkServer.exe"
     
   Start Options:
     * Working directory: "C:\Path\To\GTANetwork Server"
5. Now, whenever you press F5(Debug), the GTAN server will start in debug mode,
   and if you press Ctrl+F5, it'll start without debugging. Pretty convenient. 

## Server side overview
There's only one entry point to the script, the class `ScriptMain`,
in addition, there are several "Modules" that depend on `API` and possibly other modules,
but they're all instantaniated within the constructor `ScriptMain()` and passed to each other,
it's a very basic form of dependency injection.

#### Database/persistence
I'm using [Marten](http://jasperfx.github.io/marten/), which is a neat Document DB library using PostgreSQL, comes with
good LINQ support and is easy to use. 

See the `DevelopmentStoreConfigurator.cs` on how I initialize a `DocumentStore`, I've embedded my connection string for my convenience,
you may change it via that file or inside the `ScriptMain()` constructor, for example

```csharp
Store = new DevelopmentStoreConfigurator() {
  ConnectionString = "yourConnectionString"
}.GetStore();
```


#### Commands
You'll find that commands are actually in our script class 
```csharp
public partial class ScriptMain
```
Note that it's a partial class, because the way `Command`s are currently handled by GTA, that they must reside in a `Script`, 
and I didn't want to bundle them all to a single file. They do delegate the commands to modules though.

#### Modules
So far I've decided to group similar/related functionality into what's called "Modules", you can think of them as
controllers or services, I just don't have a good name for them. TODO: organize and refactor them better.

* IdentityModule: Handles account and character registration, also provides access to a list of all `LoggedPlayer`s and 
  exposes some events and methods to find `LoggedPlayer`s.
* AdminModule: Handles admin commands and enforces bans(via the `Ban` type). Currently has only a Ban command.
* CrimeModule: Really just a CnR module, will be refactored later. Currently has only has a non-functional arrest command.

#### Models
These are the core datatypes(classes,enums) used within our script. Mostly used for persistence as well as holding gamemode specific data

* Account: Used for user registration/login and admin roles.
* Ban: Used for handling bans, may be linked to an Account, a social club handle, IP addresses..
* Character: Used for persisting ingame information, must be linked to an Account
  * Cop: A cops and robbers cop.
  * Civilian: A cops and robbers robber, which might just be an innocent civilian. Don't judge.
  * Vector3S: A serializable replacement for `Vector3` which JSON.NET doesn't seem to like.
  * WeaponS: A serializable representation of a weapon.
* LoggedPlayer: This is a union of an Account, a Client and optionally a Character class.
  basically a complete representation of a logged in player
  Currently its API is a little ugly to use because Characteris nullable (a player might be connected to an Account but not to a Character) , will change that later.
  
### Util
Misc functions and classes.


## Open to ideas
I'm open to ideas on how to improve this code, better modularize it etc.. 
Feel free to fork it and use it however you like.
