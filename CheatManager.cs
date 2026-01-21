namespace ChaosGateTrainer;

public class Cheat
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Pattern { get; set; } = "";
    public int PatternOffset { get; set; } = 0;
    public byte[] EnableBytes { get; set; } = Array.Empty<byte>();
    public byte[] DisableBytes { get; set; } = Array.Empty<byte>();
    public bool IsEnabled { get; set; } = false;
    public IntPtr? Address { get; set; } = null;
    public Keys Hotkey { get; set; } = Keys.None;
}

public class CheatManager
{
    private readonly MemoryManager _memory;
    private readonly List<Cheat> _cheats = new();

    public IReadOnlyList<Cheat> Cheats => _cheats;

    public CheatManager(MemoryManager memory)
    {
        _memory = memory;
        InitializeCheats();
    }

    private void InitializeCheats()
    {
        // Unlimited Ammo
        // Pattern from CE: aobscanmodule(ammo,GameAssembly.dll,FF 4B ?? 48 8B ?? ?? ?? 00 00 ?? ?? ?? ?? ?? ?? ?? ?? 48 83 ?? 00 74)
        _cheats.Add(new Cheat
        {
            Name = "Unlimited Ammo",
            Description = "Weapons never run out of ammunition",
            Pattern = "FF 4B ?? 48 8B ?? ?? ?? 00 00 ?? ?? ?? ?? ?? ?? ?? ?? 48 83 ?? 00 74",
            PatternOffset = 0,
            EnableBytes = new byte[] { 0x90, 0x90, 0x90 }, // NOP the dec instruction
            DisableBytes = new byte[] { 0xFF, 0x4B }, // Restore dec [rbx+??] (partial, needs context)
            Hotkey = Keys.F1
        });

        // Unlimited Action Points
        // Pattern: aobscanmodule(actionpoints,GameAssembly.dll,29 ?? 89 ?? 18 48 8B ?? E8)
        _cheats.Add(new Cheat
        {
            Name = "Unlimited Action Points",
            Description = "Actions don't consume AP",
            Pattern = "29 ?? 89 ?? 18 48 8B ?? E8",
            PatternOffset = 0,
            EnableBytes = new byte[] { 0x90, 0x90 }, // NOP the sub instruction
            DisableBytes = new byte[] { 0x29, 0xC2 }, // sub edx, eax
            Hotkey = Keys.F2
        });

        // Unlimited Remains (WP/Willpower)
        // Pattern: aobscanmodule(remains,GameAssembly.dll,29 ?? 89 ?? 1C 48 8B ?? E8)
        _cheats.Add(new Cheat
        {
            Name = "Unlimited Remains (WP)",
            Description = "Psychic powers don't consume Willpower",
            Pattern = "29 ?? 89 ?? 1C 48 8B ?? E8",
            PatternOffset = 0,
            EnableBytes = new byte[] { 0x90, 0x90 },
            DisableBytes = new byte[] { 0x29, 0xC2 },
            Hotkey = Keys.F3
        });

        // Fast Recovery
        // Pattern: F3 0F 5F ?? F3 0F 11 ?? ?? 48 8B ?? ?? ?? 00 00 48 85 ?? 0F 84
        _cheats.Add(new Cheat
        {
            Name = "Fast Recovery",
            Description = "Knights recover instantly from injuries",
            Pattern = "F3 0F 5F ?? F3 0F 11 ?? ?? 48 8B ?? ?? ?? 00 00 48 85 ?? 0F 84",
            PatternOffset = 0,
            EnableBytes = new byte[] { 0xEB, 0x02 }, // JMP +2 (skip the maxss)
            DisableBytes = new byte[] { 0xF3, 0x0F },
            Hotkey = Keys.F4
        });

        // Fast Grandmaster
        // Pattern: FF ?? 18 48 8B ?? 10 48 85 ?? 0F 84 ?? ?? ?? ?? 83 78 18 00
        _cheats.Add(new Cheat
        {
            Name = "Fast Grandmaster",
            Description = "Grandmaster abilities recharge instantly",
            Pattern = "FF ?? 18 48 8B ?? 10 48 85 ?? 0F 84 ?? ?? ?? ?? 83 78 18 00",
            PatternOffset = 0,
            EnableBytes = new byte[] { 0x90, 0x90, 0x90 }, // NOP the dec
            DisableBytes = new byte[] { 0xFF, 0x49, 0x18 },
            Hotkey = Keys.F5
        });

        // Fast Construction
        // Pattern: 31 D2 E8 ?? ?? ?? ?? 29 C3 89
        _cheats.Add(new Cheat
        {
            Name = "Fast Construction",
            Description = "Ship construction completes faster",
            Pattern = "31 D2 E8 ?? ?? ?? ?? 29 C3 89",
            PatternOffset = 7,
            EnableBytes = new byte[] { 0x31, 0xDB }, // xor ebx, ebx (zero remaining days)
            DisableBytes = new byte[] { 0x29, 0xC3 }, // sub ebx, eax
            Hotkey = Keys.F6
        });

        // Fast Research
        // Pattern: 29 C2 31 C9 E8
        _cheats.Add(new Cheat
        {
            Name = "Fast Research",
            Description = "Research completes faster",
            Pattern = "29 C2 31 C9 E8",
            PatternOffset = 0,
            EnableBytes = new byte[] { 0x31, 0xD2 }, // xor edx, edx
            DisableBytes = new byte[] { 0x29, 0xC2 }, // sub edx, eax
            Hotkey = Keys.F7
        });

        // Equip Duplicate Gear
        // Pattern from CE - allows equipping same item on multiple knights
        _cheats.Add(new Cheat
        {
            Name = "Equip Duplicate Gear",
            Description = "Same equipment can be used on multiple knights",
            Pattern = "74 ?? 48 8B ?? ?? 48 8B ?? E8 ?? ?? ?? ?? 84 C0 75",
            PatternOffset = 0,
            EnableBytes = new byte[] { 0xEB }, // JMP (always jump)
            DisableBytes = new byte[] { 0x74 }, // JE (conditional)
            Hotkey = Keys.F8
        });
    }

    public void ScanForAddresses()
    {
        foreach (var cheat in _cheats)
        {
            cheat.Address = _memory.AOBScan(cheat.Pattern);
            if (cheat.Address.HasValue && cheat.PatternOffset != 0)
            {
                cheat.Address = cheat.Address.Value + cheat.PatternOffset;
            }
        }
    }

    public bool ToggleCheat(Cheat cheat)
    {
        if (!cheat.Address.HasValue) return false;

        byte[] bytesToWrite = cheat.IsEnabled ? cheat.DisableBytes : cheat.EnableBytes;

        if (_memory.WriteMemory(cheat.Address.Value, bytesToWrite))
        {
            cheat.IsEnabled = !cheat.IsEnabled;
            return true;
        }

        return false;
    }

    public bool EnableCheat(Cheat cheat)
    {
        if (!cheat.Address.HasValue || cheat.IsEnabled) return false;
        return ToggleCheat(cheat);
    }

    public bool DisableCheat(Cheat cheat)
    {
        if (!cheat.Address.HasValue || !cheat.IsEnabled) return false;
        return ToggleCheat(cheat);
    }

    public void DisableAllCheats()
    {
        foreach (var cheat in _cheats.Where(c => c.IsEnabled))
        {
            DisableCheat(cheat);
        }
    }
}
