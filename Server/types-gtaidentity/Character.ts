

/// <reference path="../types-gtanetwork/index.d.ts" />

    // $Classes/Enums/Interfaces(filter)[template][separator]
    // filter (optional): Matches the name or full name of the current item. * = match any, wrap in [] to match attributes or prefix with : to match interfaces or base classes.
    // template: The template to repeat for each matched item
    // separator (optional): A separator template that is placed between all templates e.g. $Properties[public $name: $Type][, ]

    // More info: http://frhagn.github.io/Typewriter/

import { WeaponS, Vector3S } from "./index"
export interface Character {
    
    id: string;
    accountId: string;
    name: string;
    location: Vector3S;
    rotation: Vector3S;
    health: number;
    armor: number;
    weapons: { [key: string]: WeaponS; };
    createdAt: Date;
    lastSyncedAt: Date;
}


	


