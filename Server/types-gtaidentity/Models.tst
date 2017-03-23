${
    // Enable extension methods by adding using Typewriter.Extensions.*
    using Typewriter.Extensions.Types;

    // Uncomment the constructor to change template settings.
    Template(Settings settings)
    {
        settings.IncludeProject("Server");
        settings.OutputExtension = ".ts";
    }

    // Custom extension methods can be used in the template by adding a $ prefix e.g. $LoudName
    string LoudName(Property property)
    {
        return property.Name.ToUpperInvariant();
    }
}

/// <reference path="../types-gtanetwork/index.d.ts" />

    // $Classes/Enums/Interfaces(filter)[template][separator]
    // filter (optional): Matches the name or full name of the current item. * = match any, wrap in [] to match attributes or prefix with : to match interfaces or base classes.
    // template: The template to repeat for each matched item
    // separator (optional): A separator template that is placed between all templates e.g. $Properties[public $name: $Type][, ]

    // More info: http://frhagn.github.io/Typewriter/
    
$Classes(GTAIdentity.Models.*)[
export interface $Name {
    $Properties[
    $name: $Type;]
}]

$Enums(GTAIdentity.Models.*)[
export enum $Name {
    $Values[
    $name = $Value][,]
}]
	
$Classes(GTAIdentity.Shared.*)[
export class $Name {
    $Constants[
    public static readonly $name: $Type = "$Value";]
}]

