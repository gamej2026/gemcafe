using System;
using System.Collections.Generic;
using System.Text;
using GemCafe.Data;
using UnityEngine;

namespace GemCafe.Customer
{
    /// <summary>
    /// 손님 데이터 테이블. Resources 폴더의 CSV(기본: InportCsv/CustumersData)를 읽어
    /// 손님별 [재료 3종, 등장 대사(메뉴 주문), 이미지 리소스 경로]를 런타임 CustomerSO 로 만든다.
    ///
    /// CSV 컬럼(헤더 기준, 순서 무관):
    ///   id, day, drinkName, ingredient1, ingredient2, ingredient3, speaker, orderText, imagePath
    ///   - ingredientN : IngredientSO.displayName (예: 곳감) — ingredientPool 에서 매핑(미일치 시 id 로도 매핑)
    ///   - imagePath   : 이미지 상대 경로(확장자 제외, 예: Customers/cst_day1).
    ///                   런타임에 디스크 파일을 우선 직접 읽고(재임포트/재빌드 불필요),
    ///                   실패 시 Resources 폴백.
    /// </summary>
    public class CustomerCsvTable : MonoBehaviour
    {
        [Tooltip("Resources 기준 CSV 경로 (확장자 제외). 예: InportCsv/CustumersData")]
        [SerializeField] private string resourcePath = "InportCsv/CustumersData";

        [Tooltip("CSV의 재료 이름(displayName)을 매핑할 재료 풀")]
        [SerializeField] private List<IngredientSO> ingredientPool = new List<IngredientSO>();

        public string ResourcePath => resourcePath;

        /// <summary>CSV를 읽어 손님 목록을 만든다. 실패 시 빈 목록.</summary>
        public List<CustomerSO> Load()
        {
            var customers = new List<CustomerSO>();

            var csv = Resources.Load<TextAsset>(resourcePath);
            if (csv == null)
            {
                Debug.LogWarning($"[CustomerCsvTable] CSV를 찾을 수 없습니다: Resources/{resourcePath}");
                return customers;
            }

            var rows = ParseCsv(csv.text);
            if (rows.Count == 0)
            {
                return customers;
            }

            var header = BuildHeaderMap(rows[0]);

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (IsEmptyRow(row))
                {
                    continue;
                }

                var customer = BuildCustomer(header, row);
                if (customer != null)
                {
                    customers.Add(customer);
                }
            }

            return customers;
        }

        private CustomerSO BuildCustomer(Dictionary<string, int> header, List<string> row)
        {
            string id = Field(header, row, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var customer = ScriptableObject.CreateInstance<CustomerSO>();
            customer.id = id.Trim();

            int.TryParse(Field(header, row, "day"), out var day);
            customer.day = Mathf.Clamp(day, 1, 3);

            string imagePath = Field(header, row, "imagePath");
            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                customer.portrait = LoadPortrait(imagePath.Trim());
            }

            customer.orderDialogue = new[]
            {
                new DialogueLine
                {
                    speakerId = Field(header, row, "speaker"),
                    text = Field(header, row, "orderText"),
                    portrait = null
                }
            };

            var recipe = ScriptableObject.CreateInstance<RecipeSO>();
            recipe.id = "rcp_" + customer.id;
            recipe.drinkName = Field(header, row, "drinkName");

            var ingredients = new List<IngredientSO>();
            AddIngredient(ingredients, Field(header, row, "ingredient1"));
            AddIngredient(ingredients, Field(header, row, "ingredient2"));
            AddIngredient(ingredients, Field(header, row, "ingredient3"));
            recipe.ingredients = ingredients.ToArray();

            customer.targetRecipe = recipe;
            return customer;
        }

        /// <summary>
        /// 손님 초상화를 런타임에 로드한다.
        /// 1) 디스크 파일을 직접 읽어(재빌드/재임포트 불필요) 우선 사용하고,
        /// 2) 실패 시 Resources 에 구워진 기본 이미지로 폴백한다.
        /// imagePath 는 확장자 제외 상대 경로(예: Customers/cst_day1).
        /// </summary>
        private Sprite LoadPortrait(string imagePath)
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
                Debug.LogWarning($"[CustomerCsvTable] 이미지를 찾을 수 없습니다: '{imagePath}' (디스크/Resources 모두 실패)");
            }
            return res;
        }

        /// <summary>imagePath 에 대해 디스크에서 찾아볼 후보 경로들(우선순위 순).</summary>
        private IEnumerable<string> GetDiskCandidatePaths(string imagePath)
        {
            string[] exts = { ".png", ".jpg", ".jpeg" };

            // 1) StreamingAssets/<imagePath>.png  — 빌드 후에도 파일 교체로 즉시 반영
            foreach (var ext in exts)
            {
                yield return System.IO.Path.Combine(Application.streamingAssetsPath, imagePath + ext);
            }

#if UNITY_EDITOR
            // 2) (에디터 전용) 원본 Images PNG 를 직접 읽음 — 재임포트 없이 즉시 반영
            foreach (var ext in exts)
            {
                yield return System.IO.Path.Combine(Application.dataPath, "Images", imagePath + ext);
            }
#endif

            // 3) 실행파일 옆 ExternalImages/<imagePath>.png — 빌드 외부 오버라이드 폴더
            foreach (var ext in exts)
            {
                yield return System.IO.Path.Combine(Application.dataPath, "..", "ExternalImages", imagePath + ext);
            }
        }

        /// <summary>디스크 PNG/JPG 파일을 읽어 Sprite 로 만든다. 없거나 실패하면 null.</summary>
        private Sprite TryLoadSpriteFromFile(string fullPath)
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
                    // ImageConversion 모듈을 사용할 수 없음(빌드에서 스트립 등) → Resources 폴백
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
                Debug.LogWarning($"[CustomerCsvTable] 이미지 로드 실패: {fullPath} ({e.Message})");
                return null;
            }
        }

        // UnityEngine.ImageConversion.LoadImage(Texture2D, byte[]) 를 리플렉션으로 해석한다.
        // 이 asmdef 는 ImageConversionModule 을 컴파일 타임에 참조하지 않으므로 직접 호출 대신 리플렉션을 쓴다.
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

        private void AddIngredient(List<IngredientSO> list, string id)
        {
            var ing = ResolveIngredient(id);
            if (ing != null)
            {
                list.Add(ing);
            }
            else if (!string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning($"[CustomerCsvTable] 재료를 찾을 수 없습니다: {id}");
            }
        }

        private IngredientSO ResolveIngredient(string nameOrId)
        {
            if (string.IsNullOrWhiteSpace(nameOrId) || ingredientPool == null)
            {
                return null;
            }

            var key = nameOrId.Trim();

            // 우선 displayName 으로 매칭한다.
            for (int i = 0; i < ingredientPool.Count; i++)
            {
                var ing = ingredientPool[i];
                if (ing != null && string.Equals(ing.displayName, key, StringComparison.OrdinalIgnoreCase))
                {
                    return ing;
                }
            }

            // 미일치 시 id 로도 매칭(하위 호환).
            for (int i = 0; i < ingredientPool.Count; i++)
            {
                var ing = ingredientPool[i];
                if (ing != null && string.Equals(ing.id, key, StringComparison.OrdinalIgnoreCase))
                {
                    return ing;
                }
            }

            return null;
        }

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
