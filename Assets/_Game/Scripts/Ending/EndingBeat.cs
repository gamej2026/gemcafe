using GemCafe.Core;

namespace GemCafe.Ending
{
    /// <summary>
    /// 엔딩 연출의 한 단위(비트). CSV(Resources/Endings/ending_dialogue.csv)의 한 행을 표현한다.
    /// 각 필드는 리소스 경로(Resources 하위 상대 경로) 혹은 키워드이며, 비어 있을 수 있다.
    /// </summary>
    public struct EndingBeat
    {
        public EndingKind kind;     // 엔딩 구분 (A/B/C)
        public string order;        // 진행 순서 (A-1 등, 디버그/정렬용)
        public string speakerId;    // 화자 (UI). "-"/빈 값이면 대사 없음으로 취급
        public string text;         // 대사 및 내용
        public string bgPath;       // 배경 이미지 (Resources 경로)
        public string portraitPath; // 스탠딩 일러스트 (Resources 경로)
        public string cgPath;       // CG 일러스트 (Resources 경로)
        public string bgmPath;      // BGM (Resources 경로)
        public string sfxPath;      // SFX (Resources 경로)
        public string effect;       // 화면 효과 키워드 (none/fade/sepia/red_filter/blackout/flash/fadein/animate)
        public bool partnerOnRight; // 화자 위치 (right=true, left=false)

        public bool HasText => !string.IsNullOrEmpty(text);
    }
}
