# README

## Table of Contents

- Todo
- Commands
  - [Pasteboard](#pasteboard)
  - [Clear](#clear)
  - [Basic Tools](#tools)
  
## Todo

- a !track command which will be drawing the background of the movement you had, so you can see your trail
- scan and build hard-coded elements, trees, temples, crew logo, designs, etc.
  - !mode paste -> just place grey basic block at the coordinates instead of having to say !paste x y
- !undo?
- 'mirror mode': Once 'git'ting with leo and blocks placer can be identified: For the sake of even faster world building, set a symmetry axis (x or y), or a point; also: paste?
- current limit: x2 must be > x1: Fix: Aslong x1 != x2 increase x1 till x2 is reached

## Commands

### <a id="pasteboard">Pasteboard</a>

Every user has his own `clipboard`, which stores 

|        |  Parameters |                          Description                          |
|:------:|:-----------:|:-------------------------------------------------------------:|
| !copy  | x1 y1 x2 y2 | Copies a rectangular area to the users' pasteboard.           |
| !cut   | x1 y1 x2 y2 | Cuts a rectangular area to the users' pasteboard.             |
| !paste | x y         | Pastes the content of the pasteboard to the given coordinate. |

### <a id="clear">Clear</a>

Components that clear entire regions.

|           |  Parameters |         Description        |
|:---------:|:-----------:|:--------------------------:|
| !clear    | x1 y1 x2 y2 | Clears a rectangular area. |
| !clearall |             | Clears the entire level.   |

### <a id="tools">Basic Tools</a>

|             |       Parameters      |                Description                |
|:-----------:|:---------------------:|:-----------------------------------------:|
| !fill       | layer x1 y1 x2 y2 bid | Fills a rectangular area with blocks.     |
| !rect       | layer x1 y1 x2 y2 bid | Creates the border of a rectangular area. |
| !fillcircle | layer x y radius bid  | Fills a circular area with blocks.        |
| !circle     | layer x y radius bid  | Creates the border of a circular area.    |

## Modes

Modes can be set with the `!mode <name>` command, where name represents the name of the mode.

### Default `!mode default`

Doesn't change anything.

### Rainbow `!mode rainbow`

When placing the grey basic block (bid: 2), it changes to a color to have a full hue transition

# Useful Links

- [List of all Blocks](https://github.com/capasha/EEUProtocol/blob/master/Blocks.md) by [capasha](https://github.com/capasha)
- [EEU Protocol](https://github.com/capasha/EEUProtocol/blob/master/README.md) by [capasha](https://github.com/capasha)
