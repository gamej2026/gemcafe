using System.Collections.Generic;
using GemCafe.Data;
using UnityEngine;

namespace GemCafe.Dialogue
{
    /// <summary>
    /// Resources/Stage/interactable_dialogue.csv 를 읽어 Interactable 대사를 제공한다.
    /// CSV 컬럼: id,order,speaker,text,portraitPath
    /// portraitPath는 다음을 지원한다.
    /// 1) Resources 상대 경로 (예: Stage/Portraits/mc)
    /// 2) 에셋 경로 (예: Assets/Images/mc rough_illust.png)
    /// </summary>
    public static class InteractableDialogueCsvLoader
    {
        public const string ResourcePath = "Stage/interactable_dialogue";

        private const int ColId = 0;
        private const int ColOrder = 1;
        private const int ColSpeaker = 2;
        private const int ColText = 3;
        private const int ColPortrait = 4;
        private const int MinColumns = 4;

        private static Dictionary<string, DialogueLine[]> _cached;

        public static DialogueLine[] LoadById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return System.Array.Empty<DialogueLine>();
            }

            EnsureLoaded();
            if (_cached != null && _cached.TryGetValue(id.Trim(), out var lines))
            {
                return lines;
            }

            return System.Array.Empty<DialogueLine>();
        }

        public static void ClearCache()
        {
            _cached = null;
        }

        private static void EnsureLoaded()
        {
            if (_cached != null)
            {
                return;
            }

            _cached = new Dictionary<string, DialogueLine[]>();

            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"InteractableDialogueCsvLoader: '{ResourcePath}' 리소스를 찾을 수 없습니다.");
                return;
            }

            var rows = ParseCsv(asset.text);
            var grouped = new Dictionary<string, List<(int order, DialogueLine line)>>();

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count < MinColumns)
                {
                    continue;
                }

                var id = Trim(row[ColId]);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                int order = r;
                if (row.Count > ColOrder)
                {
                    int.TryParse(Trim(row[ColOrder]), out order);
                }

                var line = new DialogueLine
                {
                    speakerId = row.Count > ColSpeaker ? Trim(row[ColSpeaker]) : string.Empty,
                    text = row.Count > ColText ? row[ColText] : string.Empty,
                    portrait = LoadPortrait(row.Count > ColPortrait ? Trim(row[ColPortrait]) : string.Empty)
                };

                if (!grouped.TryGetValue(id, out var list))
                {
                    list = new List<(int order, DialogueLine line)>();
                    grouped[id] = list;
                }

                list.Add((order, line));
            }

            foreach (var kv in grouped)
            {
                kv.Value.Sort((a, b) => a.order.CompareTo(b.order));
                var lines = new DialogueLine[kv.Value.Count];
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    lines[i] = kv.Value[i].line;
                }

                _cached[kv.Key] = lines;
            }
        }

        private static Sprite LoadPortrait(string portraitPath)
        {
            if (string.IsNullOrWhiteSpace(portraitPath))
            {
                return null;
            }

            var raw = portraitPath.Trim();

            // 1) Resources 상대 경로 그대로 시도
            var sprite = Resources.Load<Sprite>(NormalizeResourcesPath(raw));
            if (sprite != null)
            {
                return sprite;
            }

            // 2) Assets 경로 입력을 지원한다. (에디터에서는 직접 로드)
            if (raw.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
            {
#if UNITY_EDITOR
                var editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(raw);
                if (editorSprite != null)
                {
                    return editorSprite;
                }
#endif

                // 빌드 환경에서는 /Resources/ 하위인 경우에 한해 변환 로드
                int marker = raw.IndexOf("/Resources/", System.StringComparison.OrdinalIgnoreCase);
                if (marker >= 0)
                {
                    string rel = raw.Substring(marker + "/Resources/".Length);
                    sprite = Resources.Load<Sprite>(NormalizeResourcesPath(rel));
                    if (sprite != null)
                    {
                        return sprite;
                    }
                }
            }

            Debug.LogWarning($"InteractableDialogueCsvLoader: portrait 로드 실패 '{raw}'");
            return null;
        }

        private static string NormalizeResourcesPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var value = path.Trim().Replace('\\', '/');
            int ext = value.LastIndexOf('.');
            if (ext > value.LastIndexOf('/'))
            {
                value = value.Substring(0, ext);
            }

            return value;
        }

        private static string Trim(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }

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

            if (field.Length > 0 || current.Count > 0)
            {
                current.Add(field.ToString());
                rows.Add(current);
            }

            return rows;
        }
    }
}
