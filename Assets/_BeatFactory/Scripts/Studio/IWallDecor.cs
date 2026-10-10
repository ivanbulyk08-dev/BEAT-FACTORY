namespace BeatFactory
{
    /// <summary>Studio wall decorations (album posters, artist posters) that take part in the move-in show.</summary>
    public interface IWallDecor
    {
        /// <summary>Hide every item (before the new room is revealed).</summary>
        void HideAll();

        /// <summary>Pop every visible item in, one after another.</summary>
        void PopAll();
    }
}
