using FlaUI.Core.WindowsAPI;

namespace FlaUI.Cli.Services;

public static class KeyParser
{
    private static readonly Dictionary<string, VirtualKeyShort> KeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ctrl"] = VirtualKeyShort.CONTROL,
        ["control"] = VirtualKeyShort.CONTROL,
        ["alt"] = VirtualKeyShort.ALT,
        ["shift"] = VirtualKeyShort.SHIFT,
        ["tab"] = VirtualKeyShort.TAB,
        ["escape"] = VirtualKeyShort.ESCAPE,
        ["esc"] = VirtualKeyShort.ESCAPE,
        ["enter"] = VirtualKeyShort.ENTER,
        ["return"] = VirtualKeyShort.ENTER,
        ["space"] = VirtualKeyShort.SPACE,
        ["delete"] = VirtualKeyShort.DELETE,
        ["del"] = VirtualKeyShort.DELETE,
        ["backspace"] = VirtualKeyShort.BACK,
        ["up"] = VirtualKeyShort.UP,
        ["down"] = VirtualKeyShort.DOWN,
        ["left"] = VirtualKeyShort.LEFT,
        ["right"] = VirtualKeyShort.RIGHT,
        ["home"] = VirtualKeyShort.HOME,
        ["end"] = VirtualKeyShort.END,
        ["pageup"] = VirtualKeyShort.PRIOR,
        ["pagedown"] = VirtualKeyShort.NEXT,
        ["insert"] = VirtualKeyShort.INSERT,
        ["f1"] = VirtualKeyShort.F1,
        ["f2"] = VirtualKeyShort.F2,
        ["f3"] = VirtualKeyShort.F3,
        ["f4"] = VirtualKeyShort.F4,
        ["f5"] = VirtualKeyShort.F5,
        ["f6"] = VirtualKeyShort.F6,
        ["f7"] = VirtualKeyShort.F7,
        ["f8"] = VirtualKeyShort.F8,
        ["f9"] = VirtualKeyShort.F9,
        ["f10"] = VirtualKeyShort.F10,
        ["f11"] = VirtualKeyShort.F11,
        ["f12"] = VirtualKeyShort.F12,
        ["f13"] = VirtualKeyShort.F13,
        ["f14"] = VirtualKeyShort.F14,
        ["f15"] = VirtualKeyShort.F15,
        ["f16"] = VirtualKeyShort.F16,
        ["f17"] = VirtualKeyShort.F17,
        ["f18"] = VirtualKeyShort.F18,
        ["f19"] = VirtualKeyShort.F19,
        ["f20"] = VirtualKeyShort.F20,
        ["f21"] = VirtualKeyShort.F21,
        ["f22"] = VirtualKeyShort.F22,
        ["f23"] = VirtualKeyShort.F23,
        ["f24"] = VirtualKeyShort.F24,
        ["a"] = VirtualKeyShort.KEY_A,
        ["b"] = VirtualKeyShort.KEY_B,
        ["c"] = VirtualKeyShort.KEY_C,
        ["d"] = VirtualKeyShort.KEY_D,
        ["e"] = VirtualKeyShort.KEY_E,
        ["f"] = VirtualKeyShort.KEY_F,
        ["g"] = VirtualKeyShort.KEY_G,
        ["h"] = VirtualKeyShort.KEY_H,
        ["i"] = VirtualKeyShort.KEY_I,
        ["j"] = VirtualKeyShort.KEY_J,
        ["k"] = VirtualKeyShort.KEY_K,
        ["l"] = VirtualKeyShort.KEY_L,
        ["m"] = VirtualKeyShort.KEY_M,
        ["n"] = VirtualKeyShort.KEY_N,
        ["o"] = VirtualKeyShort.KEY_O,
        ["p"] = VirtualKeyShort.KEY_P,
        ["q"] = VirtualKeyShort.KEY_Q,
        ["r"] = VirtualKeyShort.KEY_R,
        ["s"] = VirtualKeyShort.KEY_S,
        ["t"] = VirtualKeyShort.KEY_T,
        ["u"] = VirtualKeyShort.KEY_U,
        ["v"] = VirtualKeyShort.KEY_V,
        ["w"] = VirtualKeyShort.KEY_W,
        ["x"] = VirtualKeyShort.KEY_X,
        ["y"] = VirtualKeyShort.KEY_Y,
        ["z"] = VirtualKeyShort.KEY_Z,
        ["0"] = VirtualKeyShort.KEY_0,
        ["1"] = VirtualKeyShort.KEY_1,
        ["2"] = VirtualKeyShort.KEY_2,
        ["3"] = VirtualKeyShort.KEY_3,
        ["4"] = VirtualKeyShort.KEY_4,
        ["5"] = VirtualKeyShort.KEY_5,
        ["6"] = VirtualKeyShort.KEY_6,
        ["7"] = VirtualKeyShort.KEY_7,
        ["8"] = VirtualKeyShort.KEY_8,
        ["9"] = VirtualKeyShort.KEY_9,
    };

    public static VirtualKeyShort[] Parse(string input)
    {
        var tokens = input.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var keys = new List<VirtualKeyShort>();

        foreach (var token in tokens)
        {
            if (KeyMap.TryGetValue(token, out var key))
            {
                keys.Add(key);
            }
            else
            {
                throw new ArgumentException($"Unknown key token: '{token}'. Supported keys: {string.Join(", ", KeyMap.Keys.Order())}");
            }
        }

        return keys.ToArray();
    }
}
