using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AnimatedMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite hoverSprite;
    public Sprite pressedSprite;
    public Sprite extraSprite; // Para la puerta abierta

    [Header("Opcional: Imagen secundaria (puerta)")]
    public Image extraImage; // Para la puerta abierta/cerrada

    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
        if (image && normalSprite)
            image.sprite = normalSprite;
        if (extraImage && extraSprite)
            extraImage.sprite = normalSprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (image && hoverSprite)
            image.sprite = hoverSprite;
        if (extraImage && extraSprite)
            extraImage.sprite = extraSprite;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (image && normalSprite)
            image.sprite = normalSprite;
        if (extraImage && normalSprite)
            extraImage.sprite = normalSprite;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (image && pressedSprite)
            image.sprite = pressedSprite;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (image && hoverSprite)
            image.sprite = hoverSprite;
    }
}
