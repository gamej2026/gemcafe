using System.Collections.Generic;
using UnityEngine;

namespace GemCafe.Tutorial
{
    /// <summary>
    /// 튜토리얼 대화 UI 표시 방식.
    /// </summary>
    public enum TutorialUiType
    {
        /// <summary>화면 하단에 고정된 넓은 대화창.</summary>
        TalkDialog,
        /// <summary>화면상의 임의 위치에 띄우는 작은 팝업.</summary>
        PositionedPopup,
    }

    /// <summary>
    /// 튜토리얼 한 줄(CSV 한 행)에 대응하는 데이터.
    /// </summary>
    public struct TutorialLine
    {
        public string section;         // 구분 (예: 튜토리얼 1)
        public string order;           // 진행 순서
        public string staging;         // 화면/시스템 연출 (참고용)
        public string speaker;         // 화자 (UI). 비어 있으면 화자 표기 생략
        public Sprite illust;          // 화자 일러스트(Resources 경로)
        public string text;            // 대사 및 내용
        public string highlight;       // 강조할 실제 도구 키워드 (tray/bowl/pestle/teaware/recall/book ...)
        public string action;          // 머신 동작 (end 등)
        public TutorialUiType uiType;  // 대화 UI 표시 방식 (col 7)
        public Vector2 popupAnchor;    // PositionedPopup 전용: 정규화된 화면 위치 (0~1, col 8)
        public string spawnPrefab;     // 대화 동안 유지할 프리팹의 Resources 경로 (col 9). 비어 있으면 변경 없음.
    }

    /// <summary>
    /// Resources/Cafe/cafe_tutorial.csv 를 읽어 <see cref="TutorialLine"/> 목록으로 변환한다.
    /// 따옴표로 감싼 필드와 그 안의 줄바꿈/콤마/이중따옴표("")를 모두 처리한다.
    /// </summary>
    public static class CafeTutorialCsvLoader
    {
        public static List<TutorialLine> Load(string resourcePath)
        {
            var result = new List<TutorialLine>();

            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"CafeTutorialCsvLoader: '{resourcePath}' 리소스를 찾을 수 없습니다.");
                return result;
            }

            var rows = ParseCsv(asset.text);

            // 첫 행은 헤더이므로 건너뛴다.
            for (int i = 1; i < rows.Count; i++)
            {
                var fields = rows[i];
                if (fields.Count == 0)
                {
                    continue;
                }

                // 완전히 빈 행은 무시.
                bool allEmpty = true;
                for (int f = 0; f < fields.Count; f++)
                {
                    if (!string.IsNullOrEmpty(fields[f]))
                    {
                        allEmpty = false;
                        break;
                    }
                }

                if (allEmpty)
                {
                    continue;
                }

                bool hasIllustColumn = fields.Count >= 10;
                int illustIndex = hasIllustColumn ? 4 : -1;
                int textIndex = hasIllustColumn ? 5 : 4;
                int highlightIndex = hasIllustColumn ? 6 : 5;
                int actionIndex = hasIllustColumn ? 7 : 6;
                int uiTypeIndex = hasIllustColumn ? 8 : 7;
                int popupPosIndex = hasIllustColumn ? 9 : 8;
                int spawnPrefabIndex = hasIllustColumn ? 10 : 9;

                var line = new TutorialLine
                {
                    section = Get(fields, 0),
                    order = Get(fields, 1),
                    staging = Get(fields, 2),
                    speaker = Get(fields, 3),
                    illust = LoadSprite(Get(fields, illustIndex)),
                    text = Get(fields, textIndex),
                    highlight = Get(fields, highlightIndex).Trim().ToLowerInvariant(),
                    action = Get(fields, actionIndex).Trim().ToLowerInvariant(),
                    uiType = ParseUiType(Get(fields, uiTypeIndex)),
                    popupAnchor = ParseVector2(Get(fields, popupPosIndex), new Vector2(0.5f, 0.5f)),
                    spawnPrefab = NormalizeResourcePath(Get(fields, spawnPrefabIndex)),
                };

                // 대사가 비어 있는 행은 표시할 내용이 없으므로 건너뛴다.
                if (string.IsNullOrWhiteSpace(line.text))
                {
                    continue;
                }

                result.Add(line);
            }

            return result;
        }

        private static string Get(List<string> fields, int index)
        {
            return index >= 0 && index < fields.Count ? fields[index] : string.Empty;
        }

        private static TutorialUiType ParseUiType(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return TutorialUiType.TalkDialog;
            }

            switch (raw.Trim().ToLowerInvariant())
            {
                case "positionedpopup":
                case "positionpopup":
                    return TutorialUiType.PositionedPopup;
                default:
                    return TutorialUiType.TalkDialog;
            }
        }

        /// <summary>
        /// "0.5,0.8" 형식의 문자열을 Vector2 로 변환한다. 파싱 실패 시 <paramref name="fallback"/> 반환.
        /// </summary>
        private static Vector2 ParseVector2(string raw, Vector2 fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            var parts = raw.Trim().Split(',');
            if (parts.Length >= 2
                && float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float x)
                && float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float y))
            {
                return new Vector2(x, y);
            }

            return fallback;
        }

        private static Sprite LoadSprite(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            var path = NormalizeResourcePath(resourcePath);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return Resources.Load<Sprite>(path);
        }

        private static string NormalizeResourcePath(string path)
        {
            var value = path.Trim().Replace('\\', '/');

            int extIndex = value.LastIndexOf('.');
            if (extIndex > value.LastIndexOf('/'))
            {
                value = value.Substring(0, extIndex);
            }

            return value;
        }

        // RFC4180 스타일의 CSV 파서: 따옴표 필드, 내장 줄바꿈/콤마/"" 이스케이프 지원.
        private static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(text))
            {
                return rows;
            }

            var current = new List<string>();
            var sb = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // 이중따옴표("")는 따옴표 한 개로.
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }

                    continue;
                }

                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    current.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '\r')
                {
                    // \r\n 또는 단독 \r 모두 줄 끝으로 처리.
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }

                    current.Add(sb.ToString());
                    sb.Clear();
                    rows.Add(current);
                    current = new List<string>();
                }
                else if (c == '\n')
                {
                    current.Add(sb.ToString());
                    sb.Clear();
                    rows.Add(current);
                    current = new List<string>();
                }
                else
                {
                    sb.Append(c);
                }
            }

            // 마지막 필드/행 마무리.
            if (sb.Length > 0 || current.Count > 0)
            {
                current.Add(sb.ToString());
                rows.Add(current);
            }

            return rows;
        }
    }
}
