namespace AudioPlayer
{
    //Member names are surfaced to the UI: StringContainsStringToVisibilityConverter matches
    //ToString() against "on"/"off"/"playlist"/"single" to pick the button image.
    public enum ShuffleState
    {
        On,
        Off
    }

    public enum RepeatMode
    {
        Off,
        Playlist,
        Single
    }
}
