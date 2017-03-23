

/// <reference path="../types-gtanetwork/index.d.ts" />

    // $Classes/Enums/Interfaces(filter)[template][separator]
    // filter (optional): Matches the name or full name of the current item. * = match any, wrap in [] to match attributes or prefix with : to match interfaces or base classes.
    // template: The template to repeat for each matched item
    // separator (optional): A separator template that is placed between all templates e.g. $Properties[public $name: $Type][, ]

    // More info: http://frhagn.github.io/Typewriter/
    

export interface Civilian {
    
    wantedLevel: Wanted;
    cash: number;
    job: Job;
}


export enum Job {
    
    unemployed = 0,
    thief = 1
}
export enum Wanted {
    
    none = 0,
    ticketable = 1,
    arrestable = 2,
    killAuthorized = 3
}
	


