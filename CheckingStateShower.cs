

using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class CheckingStateShower:MonoBehaviour
{
    [SerializeField] private AnchorScanning Scanning;
    [SerializeField] private Text ShowedText;

    private string AddedText = "";
    private object Locker = new object();

    private void Awake()
    {
        Scanning.ChangeAnchorScanningStateEvent += StateChangedAction;
    }
    private void Update()
    {
        lock (Locker)
        {
            if (!string.IsNullOrEmpty(AddedText))
            {
                ShowedText.text = AddedText;
                AddedText = "";
            }
        }
    }
    private void StateChangedAction(string state)
    {
        lock (Locker)
        {
            AddedText += state;
        }
    }
}