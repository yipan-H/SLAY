using UnityEngine;
using UnityEngine.UI;
using XGame;

public class ExpBar : MonoBehaviour
{
    Slider slider;

    private void Awake()
    {
        this.RegisterEvent<ExpUpdatedEvent>(ExpUpdated);

        slider = GetComponent<Slider>();
        slider.minValue = 0;
    }

    private void OnDestroy()
    {
        this.UnRegisterEvent<ExpUpdatedEvent>();
    }

    void ExpUpdated(ExpUpdatedEvent e)
    {
        EventCenterManager.Send<ShowHudEvent>();
        if (slider.maxValue != e.max)
        {
            slider.maxValue = e.max;
        }

        if (slider.value != e.value)
        {
            slider.value = e.value;
        }
    }
}
