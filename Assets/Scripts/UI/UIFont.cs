using UnityEngine;
using TMPro;

namespace DungeonCore
{
    /// <summary>
    /// 모든 UI(uGUI Text, OnGUI)가 공유하는 폰트 공급자.
    /// 한글이 보이도록, 다음 우선순위로 폰트를 고른다:
    ///  1) Resources 폴더의 번들 폰트 (BundledFontName) - 빌드 배포 시 OS 폰트에 의존하지 않으려면 이걸 사용
    ///  2) OS에 설치된 한글 폰트 (맑은 고딕 등) - 에디터/개발 중엔 이것만으로 충분, 임포트 불필요
    ///  3) 유니티 기본 폰트 (한글 미지원, 최후의 안전장치)
    /// </summary>
    public static class UIFont
    {
        // 번들 폰트를 쓰려면 한글 .ttf를 Assets/.../Resources/ 폴더에 넣고
        // 파일명(확장자 제외)을 여기에 맞추거나, 이 값을 그 파일명으로 바꾼다.
        private const string BundledFontName = "DungeonFont";

        private static Font cached;

        /// <summary>
        /// 인스펙터 등에서 직접 지정한 폰트. 설정되면 최우선으로 사용된다.
        /// DungeonBootstrap이 Start에서 SetOverride(...)로 넣어줌.
        /// </summary>
        private static Font overrideFont;

        public static void SetOverride(Font font) => overrideFont = font;

        /// <summary>
        /// 특정 UI가 자기 전용 폰트를 쓰고 싶을 때. preferred가 있으면 그걸,
        /// 없으면 전역 폰트를 반환. (UI별 폰트 확장용)
        /// </summary>
        public static Font Get(Font preferred) => preferred != null ? preferred : Get();

        public static Font Get()
        {
            // 0) 직접 지정한 폰트가 있으면 최우선
            if (overrideFont != null) return overrideFont;

            if (cached != null) return cached;

            // 1) 번들 폰트 (있으면 우선)
            cached = Resources.Load<Font>(BundledFontName);

            // 2) OS 한글 폰트 (윈도우 맑은 고딕, macOS Apple SD Gothic 등)
            if (cached == null)
            {
                string[] candidates =
                {
                    "Malgun Gothic", "맑은 고딕",
                    "Apple SD Gothic Neo", "AppleSDGothicNeo", "AppleGothic",
                    "NanumGothic", "Nanum Gothic",
                    "Noto Sans CJK KR", "Noto Sans KR",
                    "Gulim", "Dotum", "Batang"
                };
                cached = Font.CreateDynamicFontFromOSFont(candidates, 16);
            }

            // 3) 최후 fallback (한글 깨짐)
            if (cached == null)
                cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return cached;
        }

        // ===== TextMeshPro 용 =====
        // TMP는 Font가 아니라 TMP_FontAsset을 쓴다. Canvas UI(TMP)는 아래를 사용.
        // 한글이 보이려면 '한글 글리프가 포함된 TMP Font Asset'을 지정해야 한다
        // (Window > TextMeshPro > Font Asset Creator로 생성).

        private static TMP_FontAsset tmpOverride;

        /// <summary>인스펙터에서 지정한 TMP 폰트 에셋 등록 (DungeonBootstrap이 Start에서 호출).</summary>
        public static void SetTMPOverride(TMP_FontAsset font) => tmpOverride = font;

        /// <summary>지정된 TMP 폰트. 없으면 null(=TMP 기본 폰트 사용).</summary>
        public static TMP_FontAsset GetTMP() => tmpOverride;

        /// <summary>TMP 텍스트에 지정 폰트를 적용 (지정 없으면 TMP 기본 유지).</summary>
        public static void ApplyTMP(TMP_Text text)
        {
            if (text != null && tmpOverride != null)
                text.font = tmpOverride;
        }
    }
}
