using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomNav : MonoBehaviour
{
    public GameObject defaultButton; // الزر المحدد تلقائياً عند البدء

    void Start()
    {
        if (defaultButton != null)
            EventSystem.current.SetSelectedGameObject(defaultButton);
    }

    void Update()
    {
        // الاختيار بزر Enter فقط
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            GameObject current = EventSystem.current.currentSelectedGameObject;
            if (current != null)
            {
                Button btn = current.GetComponent<Button>();
                if (btn != null && btn.interactable)
                    btn.onClick.Invoke();
            }
        }
    }
}