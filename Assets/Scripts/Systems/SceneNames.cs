namespace DungeonCore
{
    /// <summary>
    /// 씬 이름을 문자열로 여기저기 하드코딩하지 않도록 한 곳에서 관리.
    /// 씬 파일명을 바꾸면 이 값만 고치면 된다.
    /// (Build Settings의 Scenes In Build에 두 씬이 등록되어 있어야 로드된다.)
    /// </summary>
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string Game = "Game";
    }
}
