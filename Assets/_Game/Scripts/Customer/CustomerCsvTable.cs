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
    ///   - ingredientN : IngredientSO.id (예: ing_water) — ingredientPool 에서 매핑
    ///   - imagePath   : Resources 기준 스프라이트 경로(확장자 제외, 예: Customers/cst_day1)
    /// </summary>
    public class CustomerCsvTable : MonoBehaviour
    {
        [Tooltip("Resources 기준 CSV 경로 (확장자 제외). 예: InportCsv/CustumersData")]
        [SerializeField] private string resourcePath = "InportCsv/CustumersData";

        [Tooltip("CSV의 재료 id(ingredientN)를 매핑할 재료 풀")]
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
                customer.portrait = Resources.Load<Sprite>(imagePath.Trim());
                if (customer.portrait == null)
                {
                    Debug.LogWarning($"[CustomerCsvTable] 이미지를 찾을 수 없습니다: Resources/{imagePath.Trim()}");
                }
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

        private void AddIngredient(List<IngredientSO> list, string id)
        {
            var ing = ResolveIngredient(id);
            if (ing != null)
            {
                list.Add(ing);
            }
            else if (!string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning($"[CustomerCsvTable] 재료 id를 찾을 수 없습니다: {id}");
            }
        }

        private IngredientSO ResolveIngredient(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || ingredientPool == null)
            {
                return null;
            }

            var key = id.Trim();
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
