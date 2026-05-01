using System.Collections.Generic;
using UnityEngine;

public class Team : MonoBehaviour
{
    public List<Player> _teamPlayers = new List<Player>();
    // [0] = currently controlled, [1] = previously controlled
    public List<Player> _currentPlayer = new List<Player>();

    private void Awake()
    {
        // Collect all players from children
        foreach (Transform child in transform)
        {
            Player p = child.GetComponent<Player>();
            if (p != null) _teamPlayers.Add(p);
        }
    }

    private void Start()
    {
        if (_teamPlayers.Count >= 2)
        {
            _currentPlayer.Add(_teamPlayers[0]);  // [0] = controlled
            _currentPlayer.Add(_teamPlayers[1]);  // [1] = previous

            _teamPlayers[0].UserBrain();   // only player 0 can move
            _teamPlayers[1].AiBrain();

            // All others also AI
            for (int i = 2; i < _teamPlayers.Count; i++)
                _teamPlayers[i].AiBrain();
        }
    }
}