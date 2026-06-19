using UnityEngine;
using UnityEngine.UI;

public class ElapsedTimeTextPresenter : MonoBehaviour
{
    [SerializeField] private Text targetText;

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<Text>();
        }
    }

    private void Update()
    {
        if (targetText == null)
        {
            return;
        }

        float elapsed = Time.timeSinceLevelLoad;
        targetText.text = string.Format("Elapsed: {0:0.0} s", elapsed);
    }
}
