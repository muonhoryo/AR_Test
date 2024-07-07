

using System.Collections.Generic;
using UnityEngine;

public sealed class TempleMessagesManager : MonoBehaviour
{
    public static TempleMessagesManager Instance_ { get; private set; }

    [SerializeField] private GameObject TempleMessagePrefab;
    [SerializeField] private GameObject MessagesCanvas;

    [SerializeField] private float MessagesOffset;

    public GameObject TempleMessagePrefab_ => TempleMessagePrefab;

    private List<TempleMessage> CreatedMessages = new List<TempleMessage>();

    private void Awake()
    {
        if (Instance_ != null)
            Destroy(Instance_);
        Instance_ = this;
    }

    public TempleMessage CreateMessage(string message)
    {
        var messObj = InstantiateMessage();
        messObj.Initialize(message);
        return messObj;
    }
    public TempleMessage CreateMessage(string message,float delay)
    {
        var messObj = InstantiateMessage();
        messObj.Initialize(message, delay);
        return messObj;
    }
    private TempleMessage InstantiateMessage()
    {
        var messObj= Instantiate(TempleMessagePrefab,MessagesCanvas.transform).GetComponent<TempleMessage>();
        int pos=AddToListMessage(messObj);
        messObj.transform.position =new Vector3(messObj.transform.position.x, messObj.transform.position.y,pos * MessagesOffset);
        return messObj;
    }
    private int AddToListMessage(TempleMessage message)
    {
        for(int i = 0; i < CreatedMessages.Count; i++)
        {
            if (CreatedMessages[i] == null)
            {
                CreatedMessages[i] = message;
                return i;
            }
        }
        CreatedMessages.Add(message);
        return CreatedMessages.Count - 1;
    }
}