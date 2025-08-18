
using DG.Tweening;
using UnityEngine;

public class BreathingMovement : MonoBehaviour
{
    [SerializeField] private float scaleAmout = 1.1f;
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private Ease easeType = Ease.InOutSine;
    
    private void Start()
    {
        Vector3 originalScale = transform.localScale;
        
        transform.DOScale(originalScale * scaleAmout, duration).SetEase(easeType).SetLoops(-1, LoopType.Yoyo);
    }
}