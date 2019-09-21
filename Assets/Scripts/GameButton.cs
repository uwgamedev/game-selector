using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class GameButton : MonoBehaviour
{
    public Game game;
    public bool interactable = false;
    public TextMeshPro text;

    public void Press() {
        Debug.Log("Launching: " + game.name);
    }

    public void UpdateGame(Game game, bool interactable) {
        this.game = game;
        text.text = game.name;
        this.interactable = interactable;
    }
}
