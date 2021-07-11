using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    private Text text;
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        text.text = (1.0f / Time.smoothDeltaTime).ToString();
    }
}
