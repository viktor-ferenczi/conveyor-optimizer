# DEPRECATED

Based on my initial tests after the recent major game update (Warfare 2, game version 1.200.025), this plugin is not required anymore. 

Please switch to the [Performance Improvements](https://github.com/viktor-ferenczi/performance-improvements) plugin: https://torchapi.com/plugins/view/?guid=c2cf3ed2-c6ac-4dbd-ab9a-613a1ef67784

---
Original content for historical relevance:
---

# Conveyor Optimizer

This is a [Torch plugin](https://torchapi.net/plugins/item/2d10eb73-03c1-4605-a9d1-392cf11e4c4c) for Space Engineers game servers.

## Description

The [Conveyor Optimizer](https://torchapi.net/plugins/item/2d10eb73-03c1-4605-a9d1-392cf11e4c4c) plugin attempts to reduce the overhead imposed by repeated failing pull requests "spamming" the conveyor network.

Such repeated pull requests happen if a functional block is looking for an item which is not available on the conveyor system.

### Typical cases

- Assemblers with queued parts
- Refineries looking for ore to refine
- Gas tanks and generators looking for bottles to refill
- Turrets and guns attempt to refill ammo
- Reactors looking for uranium

There are many more such cases, but the above ones are the worst offenders.

### Theory of operation

The assumption is that if a pull request fails repeatedly, then it is likely to fail for an extended period. For example, if an assembler is out of some material for 5-10 seconds, then it will need to be produced by a refinery somewhere else on the conveyor network.

It would require the refinery to finish producing something else in its queue (in front of the missing material), or would require some ore being loaded from a player inventory or a newly connected ship (if ore is missing). Usually none of these happen quickly.

The plugin saves processing time by ignoring (muting) most conveyor requests, which would likely to fail anyway based on the above heuristics.

This plugin does not affect moving items between inventories by players, because they always succeed. Players can move only what's already there.

There would be room for more optimization, but that would require more complex logic.

### Pull requests

Pull requests are handled separately based on their direction:

- Starting conveyor port, where the pull is attempted on the system
- Item definition or category of items attempted to pull

Pull requests in different directions are handled separately.

A pull request is said to **fail** if no items are pulled into the target inventory.

A pull request direction is **muted** if pull requests are ignored in that direction.

## Admin commands

`!help conveyor`

Prints the list of available sub-commands.

`!help conveyor COMMAND`

Prints help on a specific sub-commands.

`!conveyor on`

`!conveyor off`

Enables or disables the plugin without having to restart the server.

`!conveyor info`

Prints the current configuration and whether the plugin is enabled.

`!conveyor stat`

Prints performance statistics. 

`!conveyor minmute FRAMES`

`!conveyor maxmute FRAMES`

Defines the minimum and maximum time conveyor pull requests are muted (ignored) in a specific direction before letting the next request through. 

A higher mute time results in more savings, but the muted block will potentially resume processing only later due to the delay in getting the first items from the conveyor network. It does not affect subsequent pulls, as long as they are successful.

Defaults are 60 and 300 frames (1 and 5 seconds) respectively.

- After the first failed pull request the timer is set to `minmute` frames.
- On each subsequent failing request the mute duration is doubled, but only up to `maxmute` frames.
- On any successful pull request the mute timer is reset (disabled), so subsequent pull requests will get through.

__Remarks__

- **COMMAND**: Name of a command to get help on (Torch functionality)
- **FRAMES**: Integer number of simulation frames between 1 and 30000

Changes are remembered in the configuration file.

## Technical

Methods patched:

- `MyConveyorSystem.PullItem`
- `MyConveyorSystem.PullItems`

Configuration file:

- `ConveyorOptimizer.cfg` in the `Instance` folder

Statistics:

 - Performance statistics is gathered over subsequent periods of 10 minutes each (not a moving window). 
 - Statistics is printed into the Torch log file at the end of each period.

Logging:

 - Search for `ConveyorOptimizer` to find all the log lines in the Torch log. 
 - This plugin does not write anything into the Keen log.
