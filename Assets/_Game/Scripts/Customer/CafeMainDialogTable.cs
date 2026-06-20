using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GemCafe.Customer
{
    /// <summary>
    /// 카페 메인 대화 소스(Resources: Cafe/Main/cafe_MainDialog_Source)를 읽어
    /// 날짜(Day) + 분기(일반/대성공/성공/실패)별 대사 묶음을 제공한다.
    ///
    /// CSV 컬럼(헤더 기준, 순서 무관):
    ///   Day, sprite, 손님 구분, 분기, 화자, 순서, 대사
    ///   - Day      : 날짜 숫자
    ///   - sprite   : 감정(Happy/Normal/Angry) — 손님이 말하는 줄에서 손님 이미지 교체에 사용
    ///   - 손님 구분 : 손님 종류(예: 올빼미). 화자가 이 값과 같으면 "손님이 말하는 줄"로 본다.
    ///   - 분기      : 일반/대성공/성공/실패
    ///   - 화자      : 나레이션/손님/점원
    ///   - 순서      : 대사 진행 순서(오름차순 정렬)
    ///   - 대사      : 표시할 텍스트
    /// </summary>
    public class CafeMainDialogTable : MonoBehaviour
    {
        public const string BranchNormal = "일반";
        public const string BranchGreatSuccess = "대성공";
        public const string BranchSuccess = "성공";
        public const string BranchFail = "실패";

        [Tooltip("Resources 기준 CSV 경로 (확장자 제외). 예: Cafe/Main/cafe_MainDialog_Source")]
        [SerializeField] private string resourcePath = "Cafe/Main/cafe_MainDialog_Source";

        /// <summary>한 줄의 대사 정보.</summary>
        public struct Line
        {
            public string speaker;        // 화자 표시 이름
            public string text;           // 대사 텍스트
            public bool isCustomerLine;   // 화자 == 손님 구분 인 경우 true
            public Sprite customerSprite; // 손님이 말하는 줄일 때의 감정 스프라이트(아니면 null)
        }

        private struct RawRow
        {
            public int day;
            public string emotion;
            public string customerType;
            public string branch;
            public string speaker;
            public int order;
            public string text;
        }

        private bool _loaded;
        private readonly List<RawRow> _rows = new List<RawRow>();
        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        /// <summary>해당 날짜/분기의 대사 묶음을 순서대로 반환한다. 없으면 빈 목록.</summary>
        public List<Line> GetLines(int day, string branch)
        {
            EnsureLoaded();

            var matched = new List<RawRow>();
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                if (r.day == day && string.Equals(r.branch, branch, StringComparison.Ordinal))
                {
                    matched.Add(r);
                }
            }

            matched.Sort((a, b) => a.order.CompareTo(b.order));

            var lines = new List<Line>(matched.Count);
            for (int i = 0; i < matched.Count; i++)
            {
                var r = matched[i];
                bool isCustomer = !string.IsNullOrWhiteSpace(r.customerType)
                                  && !string.IsNullOrWhiteSpace(r.speaker)
                                  && string.Equals(r.speaker.Trim(), r.customerType.Trim(), StringComparison.Ordinal);

                Sprite sprite = null;
                if (isCustomer && !string.IsNullOrWhiteSpace(r.emotion))
                {
                    sprite = LoadEmotionSprite(r.day, r.emotion.Trim());
                }

                lines.Add(new Line
                {
                    speaker = r.speaker != null ? r.speaker.Trim() : string.Empty,
                    text = r.text,
                    isCustomerLine = isCustomer,
                    customerSprite = sprite
                });
            }

            return lines;
        }

        private void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;

            var csv = Resources.Load<TextAsset>(resourcePath);
            if (csv == null)
            {
                Debug.LogWarning($"[CafeMainDialogTable] CSV를 찾을 수 없습니다: Resources/{resourcePath}");
                return;
            }

            var rows = ParseCsv(csv.text);
            if (rows.Count == 0)
            {
                return;
            }

            var header = BuildHeaderMap(rows[0]);

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (IsEmptyRow(row))
                {
                    continue;
                }

                if (!int.TryParse(Field(header, row, "Day"), out var day))
                {
                    continue;
                }

                int.TryParse(Field(header, row, "순서"), out var order);

                _rows.Add(new RawRow
                {
                    day = day,
                    emotion = Field(header, row, "sprite"),
                    customerType = Field(header, row, "손님 구분"),
                    branch = (Field(header, row, "분기") ?? string.Empty).Trim(),
                    speaker = Field(header, row, "화자"),
                    order = order,
                    text = Field(header, row, "대사")
                });
            }
        }

        /// <summary>날짜+감정에 해당하는 손님 스프라이트를 로드(캐시). 예: Customers/cst_day1_Happy</summary>
        private Sprite LoadEmotionSprite(int day, string emotion)
        {
            string imagePath = $"Customers/cst_day{day}_{emotion}";
            if (_spriteCache.TryGetValue(imagePath, out var cached))
            {
                return cached;
            }

            var sprite = LoadPortrait(imagePath);
            _spriteCache[imagePath] = sprite;
            return sprite;
        }

        // ---- 초상화 디스크 우선 로딩 (CustomerCsvTable 과 동일한 방식) ----

        private static Sprite LoadPortrait(string imagePath)
        {
            foreach (var fullPath in GetDiskCandidatePaths(imagePath))
            {
                var sprite = TryLoadSpriteFromFile(fullPath);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            var res = Resources.Load<Sprite>(imagePath);
            if (res == null)
            {
                Debug.LogWarning($"[CafeMainDialogTable] 이미지를 찾을 수 없습니다: '{imagePath}' (디스크/Resources 모두 실패)");
            }
            return res;
        }

        private static IEnumerable<string> GetDiskCandidatePaths(string imagePath)
        {
            string[] exts = { ".png", ".jpg", ".jpeg" };

            foreach (var ext in exts)
            {
                yield return System.IO.Path.Combine(Application.streamingAssetsPath, imagePath + ext);
            }

#if UNITY_EDITOR
            foreach (var ext in exts)
            {
                yield return System.IO.Path.Combine(Application.dataPath, "Images", imagePath + ext);
            }
#endif

            foreach (var ext in exts)
            {
                yield return System.IO.Path.Combine(Application.dataPath, "..", "ExternalImages", imagePath + ext);
            }
        }

        private static Sprite TryLoadSpriteFromFile(string fullPath)
        {
            try
            {
                if (string.IsNullOrEmpty(fullPath) || !System.IO.File.Exists(fullPath))
                {
                    return null;
                }

                var loadImage = GetLoadImageMethod();
                if (loadImage == null)
                {
                    return null;
                }

                var bytes = System.IO.File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!(bool)loadImage.Invoke(null, new object[] { tex, bytes }))
                {
                    return null;
                }

                tex.wrapMode = TextureWrapMode.Clamp;
                return Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CafeMainDialogTable] 이미지 로드 실패: {fullPath} ({e.Message})");
                return null;
            }
        }

        private static System.Reflection.MethodInfo _loadImageMethod;
        private static bool _loadImageResolved;

        private static System.Reflection.MethodInfo GetLoadImageMethod()
        {
            if (_loadImageResolved)
            {
                return _loadImageMethod;
            }

            _loadImageResolved = true;
            var type = System.Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
            if (type != null)
            {
                _loadImageMethod = type.GetMethod(
                    "LoadImage",
                    new[] { typeof(Texture2D), typeof(byte[]) });
            }
            return _loadImageMethod;
        }

        // ---- CSV 파싱 유틸 ----

        private static string Field(Dictionary<string, int> header, List<string> row, string column)
        {
            if (header.TryGetValue(column, out var idx) && idx >= 0 && idx < row.Count)
            {
                return row[idx];
            }

            return string.Empty;
        }

        private static Dictionary<string, int> BuildHeaderMap(List<string> headerRow)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headerRow.Count; i++)
            {
                var key = headerRow[i] != null ? headerRow[i].Trim() : string.Empty;
                if (!string.IsNullOrEmpty(key) && !map.ContainsKey(key))
                {
                    map[key] = i;
                }
            }

            return map;
        }

        private static bool IsEmptyRow(List<string> row)
        {
            for (int i = 0; i < row.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // RFC4180 스타일 CSV 파서 (따옴표로 감싼 필드 안의 쉼표/줄바꿈/이스케이프("") 지원).
        private static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(text))
            {
                return rows;
            }

            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }

                    continue;
                }

                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (c == '\r')
                {
                    // \n 에서 행 종료를 처리하므로 무시
                }
                else if (c == '\n')
                {
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = new List<string>();
                }
                else
                {
                    field.Append(c);
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }
    }
}
