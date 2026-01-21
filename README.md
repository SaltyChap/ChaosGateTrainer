# Chaos Gate: Daemonhunters Trainer

A standalone trainer for **Warhammer 40,000: Chaos Gate - Daemonhunters** (2022).

This is a single-player game trainer that modifies game memory to provide various quality-of-life cheats. Built in C# with WinForms.

## Features

| Cheat | Hotkey | Description |
|-------|--------|-------------|
| Unlimited Ammo | F1 | Weapons never run out of ammunition |
| Unlimited Action Points | F2 | Actions don't consume AP |
| Unlimited Remains (WP) | F3 | Psychic powers don't consume Willpower |
| Fast Recovery | F4 | Knights recover instantly from injuries |
| Fast Grandmaster | F5 | Grandmaster abilities recharge instantly |
| Fast Construction | F6 | Ship construction completes faster |
| Fast Research | F7 | Research completes faster |
| Equip Duplicate Gear | F8 | Same equipment can be used on multiple knights |

## Screenshot

The trainer features a dark-themed UI that displays cheat status and allows toggling via checkboxes or global hotkeys.

## Requirements

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (included in self-contained build)

## Building from Source

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/SaltyChap/ChaosGateTrainer.git
   cd ChaosGateTrainer
   ```

2. Build using the batch file:
   ```bash
   build.bat
   ```

   Or manually:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -o publish
   ```

3. Find the executable in the `publish/` folder

## Usage

1. Start **Chaos Gate: Daemonhunters** and load a save
2. **Right-click** `ChaosGateTrainer.exe` → **Run as administrator** (required!)
3. Click **"Attach to Game"**
4. Use hotkeys (F1-F8) or click checkboxes to toggle cheats
5. Cheats are automatically disabled when closing the trainer

> **Important:** The trainer must run as Administrator to access the game's memory. If you don't run as admin, the "Attach to Game" button will fail.

## How It Works

The trainer uses:
- **AOB (Array of Bytes) Pattern Scanning** - Searches for specific byte patterns in the game's memory to find code locations. This makes the trainer resilient to game updates.
- **Memory Patching** - Writes small code changes (usually NOP instructions or jumps) to modify game behavior
- **Global Hotkeys** - Registers system-wide hotkeys so you can toggle cheats while the game is focused

## Compatibility

- Uses pattern scanning, so it should work across game updates as long as the underlying code patterns don't change significantly
- If a cheat shows as disabled after attaching, the pattern may need updating for the current game version
- Tested with the Steam version

## Disclaimer

This trainer is for **single-player use only**. Use at your own risk. The author is not responsible for any issues that may arise from using this software.

## License

MIT License - Feel free to use, modify, and distribute.

## Credits

- Pattern research based on Cheat Engine community work
- Built with .NET 8.0 and Windows Forms
