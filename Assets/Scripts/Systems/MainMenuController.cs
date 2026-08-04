using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCore
{
    /// <summary>
    /// MainMenu 씬에 배치되는 컨트롤러. 타이틀 화면을 띄우고,
    /// 시작 버튼을 누르면 Game 씬을 로드한다.
    /// 빈 GameObject에 이 스크립트만 붙이면 동작 (MainMenuUI는 자동으로 추가됨).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string gameTitle = "PIXEL DUNGEON";

        private void Start()
        {
            var menu = gameObject.AddComponent<MainMenuUI>();
            menu.gameTitle = gameTitle;
            menu.OnStartPressed += LoadGameScene;
        }

        private void LoadGameScene()
        {
            SceneManager.LoadScene(SceneNames.Game);
        }
    }
}
