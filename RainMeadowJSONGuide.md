## Setup
To begin, define a `rainmeadow.json` file in your mod's directory, Meadow will automatically find it - rules for newest and targeted version folders also apply.

`rainmeadow.json` files across all mods with them defined will be merged to produce a final result for Meadow to use.

The following keys will be recognised:

## `sync_required_mods`
A list of mod IDs that must be synced between client and host, if the host has it enabled, client must too, and vice-versa for disabled. (i.e. Server-side mods)

It is important to note that mods that have a `modify/world` folder defined are automatically added to this list.

## `sync_required_mods_override`
A list of mod IDs that will be removed from the above list after merging if they are present, allowing them to bypass the requirement.

## `banned_online_mods`
A list of mod IDs that are banned from online play if the host doesn't have them enabled. Clients with mods on the list enabled will be blocked from joining the host.

## `banned_online_mods_override`
A list of mod IDs that will be removed from the above list after merging if they are present, allowing them to bypass the requirement.

## Example JSON
```json
{
    "sync_required_mods": [
        "pearlcat"
    ],
    "sync_required_mods_override": [
        "SBCameraScroll"
    ]
}
```

Pearlcat will be required to be synced between client and host.

SBCameraScroll won't be required to be synced, even if another mod defines it under `sync_required_mods`
