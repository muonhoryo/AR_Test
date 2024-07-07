

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class TempleMessage : MonoBehaviour
{
    [SerializeField] private float DefaultDestroyingDelay;
    [SerializeField] private Text MessageField;

    public void Initialize(string messageText,float destroyingDelay)
    {
        MessageField.text = messageText;
        StartCoroutine(DelayedDestroying(destroyingDelay));
    }
    public void Initialize(string messageText)=>
        Initialize(messageText, DefaultDestroyingDelay);

    private IEnumerator DelayedDestroying(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}