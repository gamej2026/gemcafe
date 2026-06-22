using System.Collections.Generic;
using GemCafe.Core;
using UnityEngine;

namespace GemCafe.Ending
{
    /// <summary>
    /// Resources/Endings/ending_dialogue.csv 를 읽어 EndingBeat 목록으로 변환한다.
    /// RFC4180 인용 규칙(쌍따옴표 escape, 셀 내 줄바꿈)을 지원한다.
    /// </summary>
    public static class EndingCsvLoader
    {
        public const string ResourcePath = "Endings/ending_dialogue";

        private const int ColKind = 0;
        private const int ColOrder = 1;
        // 2: 화면/시스템 연출 (참고용, 사용 안 함)
        private const int ColSpeaker = 3;
        private const int ColText = 4;
        private const int ColBg = 5;
        private const int ColPortrait = 6;
        private const int ColCg = 7;
        private const int ColBgm = 8;
        private const int ColSfx = 9;
        private const int ColEffect = 10;
        private const int ColSide = 11;
        private const int MinColumns = 12;

        /// <summary>지정한 엔딩 종류에 해당하는 비트만 순서대로 반환한다.</summary>
        public static List<EndingBeat> Load(EndingKind kind)
        {
            var all = LoadAll();
            var result = new List<EndingBeat>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].kind == kind)
                {
                    result.Add(all[i]);
                }
            }

            return result;
        }

        /// <summary>CSV 전체를 비트 목록으로 파싱한다.</summary>
        public static List<EndingBeat> LoadAll()
        {
            var beats = new List<EndingBeat>();

            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"EndingCsvLoader: '{ResourcePath}' 리소스를 찾을 수 없습니다.");
                return beats;
            }

            var rows = ParseCsv(asset.text);
            // 첫 행은 헤더.
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count < MinColumns)
                {
                    continue;
                }

                if (!TryParseKind(row[ColKind], out var kind))
                {
                    continue;
                }

                var beat = new EndingBeat
                {
                    kind = kind,
                    order = row[ColOrder],
                    speakerId = NormalizeSpeaker(row[ColSpeaker]),
                    text = row[ColText],
                    bgPath = Trim(row[ColBg]),
                    portraitPath = Trim(row[ColPortrait]),
                    cgPath = Trim(row[ColCg]),
                    bgmPath = Trim(row[ColBgm]),
                    sfxPath = Trim(row[ColSfx]),
                    effect = NormalizeEffect(row[ColEffect]),
                    partnerOnRight = !string.Equals(Trim(row[ColSide]), "left", System.StringComparison.OrdinalIgnoreCase)
                };

                beats.Add(beat);
            }

            return beats;
        }

        private static bool TryParseKind(string raw, out EndingKind kind)
        {
            kind = EndingKind.B;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            if (raw.Contains("A"))
            {
                kind = EndingKind.A;
                return true;
            }

            if (raw.Contains("B"))
            {
                kind = EndingKind.B;
                return true;
            }

            if (raw.Contains("C"))
            {
                kind = EndingKind.C;
                return true;
            }

            return false;
        }

        // 화자가 "-" 또는 시스템/연출 표기이거나 비어 있으면 화자 없음(빈 문자열)으로 처리한다.
        private static string NormalizeSpeaker(string raw)
        {
            var s = Trim(raw);
            if (string.IsNullOrEmpty(s) || s == "-")
            {
                return string.Empty;
            }

            if (s.StartsWith("(") && s.EndsWith(")"))
            {
                return string.Empty;
            }

            return s;
        }

        private static string NormalizeEffect(string raw)
        {
            var s = Trim(raw);
            return string.IsNullOrEmpty(s) ? "none" : s.ToLowerInvariant();
        }

        private static string Trim(string s)
        {
            return s == null ? string.Empty : s.Trim();
        }

        /// <summary>RFC4180 인용 규칙을 지원하는 CSV 파서.</summary>
        private static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(text))
            {
                return rows;
            }

            var current = new List<string>();
            var field = new System.Text.StringBuilder();
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

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        current.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        // \r\n 및 단독 \r 모두 행 종료로 처리.
                        if (i + 1 < text.Length && text[i + 1] == '\n')
                        {
                            i++;
                        }

                        current.Add(field.ToString());
                        field.Clear();
                        rows.Add(current);
                        current = new List<string>();
                        break;
                    case '\n':
                        current.Add(field.ToString());
                        field.Clear();
                        rows.Add(current);
                        current = new List<string>();
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }

            // 마지막 필드/행 마무리.
            if (field.Length > 0 || current.Count > 0)
            {
                current.Add(field.ToString());
                rows.Add(current);
            }

            return rows;
        }
    }
}
