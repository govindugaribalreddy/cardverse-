using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// This script is used for Card Events like - movements, click and drag
/// </summary>
public class CardBehaviour : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector3 startPosition;
    private Transform originalParent;
    private Transform discardedCardsContainer;
    private CanvasGroup canvasGroup;
    private HorizontalLayoutGroup layoutGroup;
    private DeckDisplayManager DeckDisplayManager;

    void Awake()
    {
        originalParent = transform.parent;
        layoutGroup = originalParent.GetComponent<HorizontalLayoutGroup>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        discardedCardsContainer = GameObject.Find("DiscardedCards").transform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Optionally, disable the layout temporarily
        if (layoutGroup != null)
            layoutGroup.enabled = false;

        startPosition = transform.position;
        canvasGroup.blocksRaycasts = false;

        // Lift the card visually by setting it to a higher parent
        transform.SetParent(discardedCardsContainer.parent);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;

        // Keep the logic for reordering within the original parent
        if (transform.parent == originalParent)
        {
            int newSiblingIndex = originalParent.childCount;

            for (int i = 0; i < originalParent.childCount; i++)
            {
                RectTransform child = originalParent.GetChild(i).GetComponent<RectTransform>();

                if (transform.position.x < child.position.x + (child.rect.width / 2))
                {
                    newSiblingIndex = i;
                    if (transform.GetSiblingIndex() < newSiblingIndex)
                        newSiblingIndex--;
                    break;
                }
            }

            if (transform.position.x > originalParent.GetChild(originalParent.childCount - 1).position.x)
                newSiblingIndex = originalParent.childCount - 1;

            transform.SetSiblingIndex(newSiblingIndex);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Determine if the drop location is within the discarded area
        if (RectTransformUtility.RectangleContainsScreenPoint(discardedCardsContainer.GetComponent<RectTransform>(), Input.mousePosition))
        {
            DeckDisplayManager = GameObject.FindObjectOfType<DeckDisplayManager>();
            if (DeckDisplayManager.DiscardValid())
            {
                transform.SetParent(discardedCardsContainer);
                DeckDisplayManager.DiscardCard(transform.gameObject.GetInstanceID());
            }
            else
            {
                Debug.Log("Its not your turn or you already discarded the card");
                transform.SetParent(originalParent);
            }
        }

        else
        {
            transform.SetParent(originalParent);
        }

        // Re-enable layout and reset block raycasts
        canvasGroup.blocksRaycasts = true;
        if (layoutGroup != null && transform.parent == originalParent)
            layoutGroup.enabled = true;

        // Reset position for clarity in UI
        transform.localPosition = Vector3.zero;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)originalParent);
    }
}
