public static class GameSessionFlowFlags
{
    private static bool skipMainMenuOnce;
    private static bool hasPendingMiniGame2ReturnSpawn;
    private static MiniGame2ReturnSpawnRequest pendingMiniGame2ReturnSpawn;

    public struct MiniGame2ReturnSpawnRequest
    {
        public string primaryAnchorName;
        public string fallbackAnchorName;
        public UnityEngine.Vector3 playerLocalOffset;
        public UnityEngine.Vector3 robotLocalOffset;
        public bool faceTowardAnchor;
    }

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

    public static void RequestMiniGame2ReturnSpawn(
        string primaryAnchorName,
        string fallbackAnchorName,
        UnityEngine.Vector3 playerLocalOffset,
        UnityEngine.Vector3 robotLocalOffset,
        bool faceTowardAnchor)
    {
        pendingMiniGame2ReturnSpawn = new MiniGame2ReturnSpawnRequest
        {
            primaryAnchorName = primaryAnchorName,
            fallbackAnchorName = fallbackAnchorName,
            playerLocalOffset = playerLocalOffset,
            robotLocalOffset = robotLocalOffset,
            faceTowardAnchor = faceTowardAnchor
        };

        hasPendingMiniGame2ReturnSpawn = true;
    }

    public static bool TryConsumeMiniGame2ReturnSpawn(out MiniGame2ReturnSpawnRequest request)
    {
        if (!hasPendingMiniGame2ReturnSpawn)
        {
            request = default;
            return false;
        }

        request = pendingMiniGame2ReturnSpawn;
        pendingMiniGame2ReturnSpawn = default;
        hasPendingMiniGame2ReturnSpawn = false;
        return true;
    }
}
