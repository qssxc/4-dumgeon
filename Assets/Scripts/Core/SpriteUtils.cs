using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 정식 아트가 준비되기 전, 색만 다른 정사각형 스프라이트를 코드로 만들어서
    /// 바로 화면에서 확인할 수 있게 하는 임시용 유틸.
    /// 나중에 실제 픽셀아트 스프라이트로 교체하면 이 유틸은 안 써도 됨.
    /// </summary>
    public static class SpriteUtils
    {
        private static Sprite cachedWhiteSquare;

        /// <summary>
        /// pixelsPerUnit 기본값은 1 - GridUtils.CellSize(1유닛)와 맞춰야
        /// 1x1 텍스처가 그리드 한 칸을 정확히 채운다. 이 값을 CellSize와
        /// 다르게 주면 타일 사이에 틈이 생기거나 겹치니 주의.
        /// </summary>
        public static Sprite CreateSolidSprite(Color color, int pixelsPerUnit = 1)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.filterMode = FilterMode.Point; // 픽셀아트 느낌 유지

            return Sprite.Create(
                texture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );
        }

        /// <summary>여러 오브젝트가 같은 흰 사각형을 공유해서 쓸 때 (타일맵처럼 개수가 많을 때) 사용.</summary>
        public static Sprite GetSharedWhiteSquare()
        {
            if (cachedWhiteSquare == null)
                cachedWhiteSquare = CreateSolidSprite(Color.white);
            return cachedWhiteSquare;
        }
    }
}
