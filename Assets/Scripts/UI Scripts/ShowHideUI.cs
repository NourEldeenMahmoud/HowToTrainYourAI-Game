using UnityEngine;

public class ShowHideUI : MonoBehaviour
{
    public GameObject targetObject;

    public void Show()
    {
        targetObject.SetActive(true);
    }

    public void Hide()
    {
        targetObject.SetActive(false);
    }

    public void Toggle()
    {
        targetObject.SetActive(!targetObject.activeSelf);
    }
}