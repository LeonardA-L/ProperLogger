using System;

namespace ProperLogger
{
    [Serializable]
    [Flags]
    internal enum ECategoryDisplay
    {
        InMessage = 1,
        NameColumn = 2,
        Icon = 4,
        ColorStrip = 8,
        InInspector = 16,
    }
}