using UnityEngine;
using UnityEngine.UI;

public class InventoryStackRow : MonoBehaviour
{
    [SerializeField] private Image oreIcon;
    [SerializeField] private Text oreLabel;

    public void SetStack(OreStack stack)
    {
        gameObject.SetActive(stack != null && stack.count > 0);
        if (stack == null || stack.count <= 0) return;

        if (oreIcon != null)
        {
            oreIcon.sprite = stack.icon;
            oreIcon.enabled = stack.icon != null;
        }
        if (oreLabel != null)
        {
            oreLabel.text = $"{stack.displayName}  x{stack.count}   ${stack.unitValueCents / 100f:0.00} each";
        }
    }
}
