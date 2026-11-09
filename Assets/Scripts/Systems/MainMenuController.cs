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
        [SerializeField] private Font uiFont; // 비워두면 OS 한글 폰트 폴백. Game 씬의 폰트와 맞추려면 같은 폰트 지정.

        private void Start()
        {
            UIFont.SetOverride(uiFont); // MainMenu 씬에서도 지정 폰트 적용

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
