(function e(t,n,r){function s(o,u){if(!n[o]){if(!t[o]){var a=typeof require=="function"&&require;if(!u&&a)return a(o,!0);if(i)return i(o,!0);var f=new Error("Cannot find module '"+o+"'");throw f.code="MODULE_NOT_FOUND",f}var l=n[o]={exports:{}};t[o][0].call(l.exports,function(e){var n=t[o][1][e];return s(n?n:e)},l,l.exports,e,t,n,r)}return n[o].exports}var i=typeof require=="function"&&require;for(var o=0;o<r.length;o++)s(r[o]);return s})({1:[function(require,module,exports){
/// <reference path="../../Server/types-gtanetwork/index.d.ts" />
"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t;
    return { next: verb(0), "throw": verb(1), "return": verb(2) };
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (_) try {
            if (f = 1, y && (t = y[op[0] & 2 ? "return" : op[0] ? "throw" : "next"]) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [0, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
Object.defineProperty(exports, "__esModule", { value: true });
function requestResponseFlow(api, opts) {
    return __awaiter(this, void 0, void 0, function () {
        return __generator(this, function (_a) {
            return [2 /*return*/, new Promise(function (resolve) {
                    var handler = function (eventName, args) {
                        if (eventName === opts.clientEventName) {
                            var data = JSON.parse(args[0]);
                            resolve(data);
                        }
                    };
                    if (opts.clientEventName)
                        api.onServerEventTrigger.connect(handler);
                    api.triggerServerEvent(opts.serverEventName, JSON.stringify(opts.data || null));
                })];
        });
    });
}
exports.requestResponseFlow = requestResponseFlow;

},{}],2:[function(require,module,exports){
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
/// <reference path="../../Server/types-gtanetwork/index.d.ts" />
var GTAIdentity = require("../../Server/types-gtaidentity/");
var ConcurrencyUtils_1 = require("./ConcurrencyUtils");
var menuPool = null;
API.onServerEventTrigger.connect(function (eventName, args) {
    var st = "eventName: " + eventName + "\n";
    for (var i = 0; i < args.Length; i++) {
        st += "  arg[" + i + "]=" + args[i];
    }
    API.sendChatMessage(st);
});
API.onServerEventTrigger.connect(function (eventName, args) {
    if (eventName === GTAIdentity.IdentityEvents.playerStateChanged) {
        var playerStateChange = JSON.parse(args[0]);
        var playerState = playerStateChange.playerState;
        switch (playerState) {
            case GTAIdentity.PlayerState.connected:
                API.sendNotification("You're now connected, please log-in.");
                menuPool = null;
                break;
            case GTAIdentity.PlayerState.accountLogged:
                API.sendChatMessage("You're now account logged, enter a character.");
                renderCharacterMenu(playerStateChange.characterNames);
                break;
            case GTAIdentity.PlayerState.characterLogged:
                API.sendNotification("You've successfully logged into a character.");
                menuPool = null;
                break;
            default:
                API.sendChatMessage("Unknown playerState '" + playerState + "'. PlayerStateChange is '" + playerStateChange + "'");
                break;
        }
    }
});
function renderCharacterMenu(characters) {
    menuPool = API.getMenuPool();
    var menu = API.createMenu("Characters", "All of your available characters", 0, 0, 2 /* TopRight */);
    var _loop_1 = function (char) {
        var item = API.createMenuItem(char, "");
        item.Activated.connect(function () {
            API.sendChatMessage("connecting to " + char);
            ConcurrencyUtils_1.requestResponseFlow(API, { serverEventName: GTAIdentity.IdentityEvents.characterSelected, data: char });
        });
        menu.AddItem(item);
    };
    for (var _i = 0, characters_1 = characters; _i < characters_1.length; _i++) {
        var char = characters_1[_i];
        _loop_1(char);
    }
    menuPool.Add(menu);
    menu.Visible = true;
}
API.onUpdate.connect(function () {
    if (menuPool != null)
        menuPool.ProcessMenus();
});
API.onServerEventTrigger.connect(function (eventName, args) {
    if (eventName == "test") {
        var res = args[0] + args[1];
        API.triggerServerEvent("test_response", [res]);
    }
});

},{"../../Server/types-gtaidentity/":8,"./ConcurrencyUtils":1}],3:[function(require,module,exports){
/// <reference path="../types-gtanetwork/index.d.ts" />
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var Role;
(function (Role) {
    Role[Role["plainUser"] = 0] = "plainUser";
    Role[Role["moderator"] = 1] = "moderator";
    Role[Role["admin"] = 2] = "admin";
    Role[Role["superAdmin"] = 3] = "superAdmin";
    Role[Role["rootAccess"] = 4] = "rootAccess";
})(Role = exports.Role || (exports.Role = {}));

},{}],4:[function(require,module,exports){
/// <reference path="../types-gtanetwork/index.d.ts" />
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var Job;
(function (Job) {
    Job[Job["unemployed"] = 0] = "unemployed";
    Job[Job["thief"] = 1] = "thief";
})(Job = exports.Job || (exports.Job = {}));
var Wanted;
(function (Wanted) {
    Wanted[Wanted["none"] = 0] = "none";
    Wanted[Wanted["ticketable"] = 1] = "ticketable";
    Wanted[Wanted["arrestable"] = 2] = "arrestable";
    Wanted[Wanted["killAuthorized"] = 3] = "killAuthorized";
})(Wanted = exports.Wanted || (exports.Wanted = {}));

},{}],5:[function(require,module,exports){
/// <reference path="../types-gtanetwork/index.d.ts" />
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var Rank;
(function (Rank) {
    Rank[Rank["cadet"] = 0] = "cadet";
    Rank[Rank["officer"] = 1] = "officer";
    Rank[Rank["sergeant"] = 2] = "sergeant";
    Rank[Rank["lieutenant"] = 3] = "lieutenant";
})(Rank = exports.Rank || (exports.Rank = {}));

},{}],6:[function(require,module,exports){
/// <reference path="../types-gtanetwork/index.d.ts" />
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
// $Classes/Enums/Interfaces(filter)[template][separator]
// filter (optional): Matches the name or full name of the current item. * = match any, wrap in [] to match attributes or prefix with : to match interfaces or base classes.
// template: The template to repeat for each matched item
// separator (optional): A separator template that is placed between all templates e.g. $Properties[public $name: $Type][, ]
// More info: http://frhagn.github.io/Typewriter/
var IdentityEvents = (function () {
    function IdentityEvents() {
    }
    return IdentityEvents;
}());
IdentityEvents.playerStateChanged = "player_state_changed";
IdentityEvents.characterSelected = "character_selected";
exports.IdentityEvents = IdentityEvents;

},{}],7:[function(require,module,exports){
/// <reference path="../types-gtanetwork/index.d.ts" />
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var PlayerState;
(function (PlayerState) {
    PlayerState[PlayerState["connected"] = 0] = "connected";
    PlayerState[PlayerState["accountLogged"] = 1] = "accountLogged";
    PlayerState[PlayerState["characterLogged"] = 2] = "characterLogged";
})(PlayerState = exports.PlayerState || (exports.PlayerState = {}));

},{}],8:[function(require,module,exports){
/*
export { Client } from "./Client"
export { Account, Role } from "./Account"
export { Ban } from "./Ban"
export { Character } from "./Character"
export { Civilian, Job, Wanted } from "./Civilian"
export { Cop, Rank } from "./Cop"
export { IdentityEvents } from "./IdentityEvents"
export { IngameAccount } from "./IngameAccount"
export { Player } from "./Player"
export { PlayerStateChange, PlayerState } from "./PlayerState"
export { Vector3S } from "./Vector3S"
export { WeaponS } from "./WeaponS"
*/
"use strict";
function __export(m) {
    for (var p in m) if (!exports.hasOwnProperty(p)) exports[p] = m[p];
}
Object.defineProperty(exports, "__esModule", { value: true });
__export(require("./Account"));
__export(require("./Civilian"));
__export(require("./Cop"));
__export(require("./IdentityEvents"));
__export(require("./PlayerState"));

},{"./Account":3,"./Civilian":4,"./Cop":5,"./IdentityEvents":6,"./PlayerState":7}]},{},[2]);
