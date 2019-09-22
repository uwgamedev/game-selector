using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using TMPro;

public class GameList : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public readonly string textPath = "Game Info.txt";
    public readonly string imagePath = "Game Image";

    public Transform[] positions;
    public GameObject gameButtonPrefab;
    [SerializeField] private List<Game> games;
    private List<GameButton> buttons;

    public TextMeshPro descText;
    public UnityEngine.UI.RawImage gameImage;

    public float inputDelay = 0.2f;
    public float transitionSpeed = 2;

    private int currentGameIndex = 0;
    private Vector3[] originalScales;

    /// <summary>
    /// Initializes the game list, drawing from the directories specified in the README
    /// </summary>
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
            // Get all the files that we need.
            FileInfo[] gameFile = gameDir.GetFiles("*.exe*");
            if (gameFile.Length == 0)
                continue;
            FileInfo[] textFile = gameDir.GetFiles(textPath);
            FileInfo[] imageFile = gameDir.GetFiles("*.png");

            // Load Description
            string desc = "No description available.";
            if (textFile.Length != 0) {
                FileInfo text = textFile[0];
                desc = File.ReadAllText(text.FullName);
            }

            // Load image
            Texture2D image = null;
            if (imageFile.Length != 0) {
                image = new Texture2D(2, 2, TextureFormat.BGRA32, false);
                image.LoadImage(File.ReadAllBytes(imageFile[0].FullName));
            }

            // Load game
            FileInfo game = gameFile[0];
            string path = game.FullName;
            string name = game.Name.Replace(".exe", "");
            games.Add(new Game(name, path, desc, image));
        }
        for (int i = 0; i < positions.Length; i++) {
            GameButton btn = Instantiate(gameButtonPrefab, positions[i]).GetComponent<GameButton>();
            btn.UpdateGame(games[Mod(-2 + i, games.Count)], false);
            buttons.Add(btn);
        }
        UpdateInfo(buttons[2].game);
        StartCoroutine(HandleInput());
    }

    /// <summary>
    /// Handles traversing the list of the games. Does animations by hand
    /// </summary>
    /// <returns></returns>
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
                UpdateInfo(buttons[2].game);
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

    /// <summary>
    /// Updates information on screen about currently selected game
    /// </summary>
    /// <param name="game">Currently selected game</param>
    private void UpdateInfo(Game game) {
        descText.text = game.desc;
        if (game.image == null) {
            gameImage.CrossFadeAlpha(0, 0.1f, true);
        } else {
            gameImage.CrossFadeAlpha(1, 0.1f, true);
        }
        gameImage.texture = game.image;
    }

    // Performs the Modulus operation, i.e. a % b, as C# does remainder instead with %
    int Mod(int a, int b) {
        return (a % b + b) % b;
    }
}

/// <summary>
/// A simple struct to hold information about each game
/// </summary>
[Serializable]
public struct Game
{
    public string name;
    public string path;
    public string desc;
    public Texture2D image;

    public Game(string name, string path, string desc, Texture2D image) {
        this.name = name;
        this.path = path;
        this.desc = desc;
        this.image = image;
    }
}
