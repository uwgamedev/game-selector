using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class GameList : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public Transform[] positions;
    public GameObject gameButtonPrefab;
    [SerializeField] private List<Game> games;
    private List<GameButton> buttons;

    public float inputDelay = 0.2f;
    public float transitionSpeed = 2;

    private int currentGameIndex = 0;
    private Vector3[] originalScales;

    void Start()
    {
        originalScales = new Vector3[positions.Length];
        for (int i = 0; i < originalScales.Length; i++) {
            originalScales[i] = positions[i].localScale;
        }
        buttons = new List<GameButton>();
        games = new List<Game>();
        DirectoryInfo library = Directory.CreateDirectory(Application.persistentDataPath + "/Games");
        DirectoryInfo[] possibleGames = library.GetDirectories();
        foreach (DirectoryInfo gameDir in possibleGames) {
            //Debug.Log(gameDir.Name);
            FileInfo[] files = gameDir.GetFiles("*.exe*");
            if (files.Length == 0)
                continue;
            FileInfo game = files[0];
            //Debug.Log(game.Name);
            string path = game.FullName;
            string name = game.Name.Replace(".exe", "");
            games.Add(new Game(name, path));
        }
        for (int i = 0; i < positions.Length; i++) {
            GameButton btn = Instantiate(gameButtonPrefab, positions[i]).GetComponent<GameButton>();
            btn.UpdateGame(games[Mod(-2 + i, games.Count)], false);
            buttons.Add(btn);
        }
        StartCoroutine(HandleInput());
    }

    private IEnumerator HandleInput() {
        while (true) {
            float horizontal = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(horizontal) > 0.1f) {
                int dir = Mathf.RoundToInt(Mathf.Sign(horizontal));
                currentGameIndex = Mod(currentGameIndex + dir, games.Count);
                float startTime = Time.time;
                while (Time.time - startTime <= inputDelay) {
                    float percent = (Time.time - startTime) / inputDelay;
                    for (int i = 0; i < buttons.Count; i++) {
                        buttons[i].transform.position = Vector3.Slerp(buttons[i].transform.position,
                            positions[Mod(i - dir, buttons.Count)].position, percent);
                        positions[i].localScale = Vector3.Lerp(positions[i].localScale, 
                            originalScales[Mod(i - dir, buttons.Count)], percent);
                    }
                    yield return null;
                }
                for (int i = 0; i < buttons.Count; i++) {
                    buttons[i].UpdateGame(games[Mod(currentGameIndex - 2 + i, games.Count)], true);
                    buttons[i].transform.localPosition = Vector3.zero;
                    positions[i].localScale = originalScales[i];
                }

            }
            if (Input.GetButtonDown("Submit")) {
                buttons[2].transform.localScale *= 0.8f;
                while (buttons[2].transform.localScale != Vector3.one) {
                    buttons[2].transform.localScale = Vector3.Lerp(buttons[2].transform.localScale,
                        Vector3.one, 0.1f);
                    yield return null;
                }
                Process gameInstance = Process.Start(buttons[2].game.path);
                SetForegroundWindow(gameInstance.MainWindowHandle);
                yield return new WaitUntil(delegate () {
                    return gameInstance.HasExited;
                });
            }
            yield return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    int Mod(int a, int b) {
        return (a % b + b) % b;
    }
}

[System.Serializable]
public struct Game
{
    public string name;
    public string path;

    public Game(string name, string path) {
        this.name = name;
        this.path = path;
    }
}
