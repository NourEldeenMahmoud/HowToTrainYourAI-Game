public static class GameSessionFlowFlags
{
    private static bool skipMainMenuOnce;

    public static void RequestSkipMainMenuOnce()
    {
        skipMainMenuOnce = true;
    }

    public static bool ConsumeSkipMainMenuOnce()
    {
        if (!skipMainMenuOnce)
            return false;

        skipMainMenuOnce = false;
        return true;
    }
}
