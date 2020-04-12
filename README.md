# README

## Table of Contents

- [Commands](#commands)
  - [Pasteboard](#pasteboard)
  - [Clear](#clear)
  - [Basic Tools](#basic-tools)
- [Read More](#useful-links)

## Commands

### Pasteboard

Every user has his own `clipboard`. It is used to temporary store a rectangle of blocks and duplicate it
at another position.

Say `!mode paste` to enter a level-manipulating danger zone:

- Place a white basic block (bid: 1) to set the top-left checkpoint of the area you want to copy.
- Place a black basic block (bid: 3) to set the bottom-right checkpoint of the area you want to copy. (The content should naw be copied)
- Place a grey basic block (bid: 2) to paste the contents of the clipboard. 

### Clear

Components that clear entire regions.

|           |  Parameters |         Description        |
|:---------:|:-----------:|:--------------------------:|
| !clear    | x1 y1 x2 y2 | Clears a rectangular area. |
| !clearall |             | Clears the entire level.   |

### Basic Tools

|             |       Parameters      |                Description                |
|:-----------:|:---------------------:|:-----------------------------------------:|
| !fill       | layer x1 y1 x2 y2 bid | Fills a rectangular area with blocks.     |
| !rect       | layer x1 y1 x2 y2 bid | Creates the border of a rectangular area. |
| !circle     | layer x y radius bid  | Creates the border of a circular area.    |

## Modes

Modes can be set with the `!mode <name>` command, where name represents the name of the mode.

### Default
`!mode default`

Doesn't change anything.

### Rainbow
`!mode rainbow`

When placing the grey basic block (bid: 2), it changes to a color to have a full hue transition

# Useful Links

- [List of all Blocks](https://github.com/capasha/EEUProtocol/blob/master/Blocks.md) by [capasha](https://github.com/capasha)
- [EEU Protocol](https://github.com/capasha/EEUProtocol/blob/master/README.md) by [capasha](https://github.com/capasha)
- [Project Board](https://github.com/Anatoly03/CCBot/projects/2)