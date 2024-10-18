using Mkey;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Purchase : MonoBehaviour
{
    private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
    public void Purchase5000()
    {
        MPlayer.AddCoins(500000);
    }
    public void Purchase10000()
    {
        MPlayer.AddCoins(1000000);
    }
    public void Purchase50000()
    {
        MPlayer.AddCoins(5000000);
    }
    public void Purchase100000()
    {
        MPlayer.AddCoins(10000000);
    }
}
