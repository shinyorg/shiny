using System;


namespace Shiny
{
    public static partial class Extensions
    {
        public static void Assert(this AccessState state, string message = null, bool allowRestricted = false)
        {
            if (state == AccessState.Available)
                return;

            if (allowRestricted && state == AccessState.Restricted)
                return;

            throw new ArgumentException(message ?? $"Invalid State " + state);
        }
    }
}
