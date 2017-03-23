/// <reference path="../../Server/types-gtanetwork/index.d.ts" />
import * as GTAIdentity from "../../Server/types-gtaidentity/";

import { requestResponseFlow } from "./ConcurrencyUtils";

let menuPool : NativeUI.MenuPool | null = null;

API.onServerEventTrigger.connect((eventName, args) => {
    let st = `eventName: ${eventName}\n`
    for (let i=0;i< args.Length; i++) {
        st += `  arg[${i}]=${args[i]}`
    }
    API.sendChatMessage(st);
})

API.onServerEventTrigger.connect((eventName,args) => {
    if (eventName === GTAIdentity.IdentityEvents.playerStateChanged) {
        const playerStateChange = JSON.parse(args[0]) as GTAIdentity.PlayerStateChange;
        const playerState = playerStateChange.playerState;
        switch (playerState) {
            case GTAIdentity.PlayerState.connected: 
                API.sendNotification("You're now connected, please log-in.")
                if (menuPool)
                  menuPool.CloseAllMenus();
                menuPool = null;
                break;
            case GTAIdentity.PlayerState.accountLogged:
                API.sendChatMessage("You're now account logged, enter a character.");
                renderCharacterMenu(playerStateChange.characterNames);
                break;
            case GTAIdentity.PlayerState.characterLogged:
                API.sendNotification("You've successfully logged into a character.");
                if (menuPool)
                  menuPool.CloseAllMenus();
                menuPool = null;
                break;
            default:
                API.sendChatMessage(`Unknown playerState '${playerState}'. PlayerStateChange is '${playerStateChange}'`);
                break;
        }
    }
});

function renderCharacterMenu(characters: string[]) {
    menuPool = API.getMenuPool();
    const menu = API.createMenu("Characters","All of your available characters",0,0,Enums.MenuAnchor.TopRight);
    for (let char of characters) {
        const item = API.createMenuItem(char,"");
        item.Activated.connect(() => {
            API.sendChatMessage(`connecting to ${char}`);
            requestResponseFlow(API, { serverEventName: GTAIdentity.IdentityEvents.characterSelected, data: char});
        });
        menu.AddItem(item);
    }
    menuPool.Add(menu);
    menu.Visible = true;
    
}

API.onUpdate.connect(() => {
    if (menuPool != null)
        menuPool.ProcessMenus();
});

API.onServerEventTrigger.connect((eventName, args) => {
    if (eventName == "test") {
        var res = args[0] + args[1];
        API.triggerServerEvent("test_response", [res]);
    }
});

