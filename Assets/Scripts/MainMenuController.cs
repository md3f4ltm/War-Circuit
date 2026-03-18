using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine.InputSystem;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "SampleScene";
    private Button playButton;
    private Button quitButton;
    private Coroutine bindRoutine;

    void OnEnable()
    {
        bindRoutine = StartCoroutine(BindUiWhenReady());
    }

    void OnDisable()
    {
        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }

        UnbindUi();
    }

    void Update()
    {
        if (playButton == null || quitButton == null)
        {
            return;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                StartGame();
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                QuitGame();
                return;
            }
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        IPanel panel = playButton.panel;
        if (panel == null)
        {
            return;
        }

        Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, Mouse.current.position.ReadValue());
        if (playButton.worldBound.Contains(panelPosition))
        {
            StartGame();
            return;
        }

        if (quitButton.worldBound.Contains(panelPosition))
        {
            QuitGame();
        }
    }

    IEnumerator BindUiWhenReady()
    {
        UIDocument document = GetComponent<UIDocument>();
        if (document == null)
        {
            yield break;
        }

        for (int i = 0; i < 10; i++)
        {
            VisualElement root = document.rootVisualElement;
            if (root != null)
            {
                playButton = root.Q<Button>("play-button");
                quitButton = root.Q<Button>("quit-button");

                if (playButton != null && quitButton != null)
                {
                    break;
                }
            }

            yield return null;
        }

        if (playButton == null || quitButton == null)
        {
            yield break;
        }

        playButton.clicked -= StartGame;
        playButton.clicked += StartGame;

        quitButton.clicked -= QuitGame;
        quitButton.clicked += QuitGame;
    }

    void UnbindUi()
    {
        if (playButton != null)
        {
            playButton.clicked -= StartGame;
        }

        if (quitButton != null)
        {
            quitButton.clicked -= QuitGame;
        }
    }

    void StartGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
