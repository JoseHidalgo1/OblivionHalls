using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

 [RequireComponent(typeof(Image))]
public class AnimatedMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite hoverSprite;
    public Sprite pressedSprite;
    public Sprite extraSprite; // Para la puerta abierta

    [Header("Opcional: Imagen secundaria (puerta)")]
    public Image extraImage; // Para la puerta abierta/cerrada

    [Header("Acción al hacer click")]
    public UnityEvent onClick;

    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }
        if (onClick == null)
        {
            onClick = new UnityEvent();
        }
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (onClick != null)
        {
            onClick.Invoke();
        }
    }
}
