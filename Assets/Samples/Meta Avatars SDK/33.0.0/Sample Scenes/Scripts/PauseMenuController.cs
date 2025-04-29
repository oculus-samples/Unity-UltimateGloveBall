#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class PauseMenuController : MonoBehaviour
{
    public Canvas? canvas;
    public float delayBeforeHidingInstructions = 5.0f;
    public Color selectedColor = Color.yellow;
    public GameObject? pauseInstructionsOverlay;
    public Text? pauseInstructionsText;
    public GameObject? pauseMenuOverlay;
    public ScrollRect? scrollRect;
    public Font? customFont;

    // Event to notify when a scene will load by the pause menu controller
    public event Action? OnSceneWillLoad;
    // Event to notify when a scene will be paused by the pause menu controller
    public event Action? OnSceneWillPause;
    // Event to notify after a scene will be resumed by the pause menu controller
    public event Action? OnSceneWillResume;

    // ReSharper disable once InconsistentNaming
    private const float SceneYPositionOffset = 30.0f;
    // ReSharper disable once InconsistentNaming
    private static bool _isCreated;

    private readonly List<Text> _scenes = new();
    private int _selectedSceneIndex;
    private bool _isPaused;
    private float _fixedDeltaTime;

    private void Awake()
    {
        // We want to persist this prefab in all scenes so the user has a way to always switch scenes
        // even when the prefab was not added to a certain scene
        if (!_isCreated)
        {
            if (transform.parent != null)
            {
                Debug.Log("PauseMenuController must be a root gameObject, moving to scene root!", this);
                transform.SetParent(null, true);
            }

            StartCoroutine(BuildSceneList());
            DontDestroyOnLoad(gameObject);
            _isCreated = true;
        }
        else
        {
            // This is a duplicate instance, so destroy it
            Destroy(gameObject);
        }

        this._fixedDeltaTime = Time.fixedDeltaTime;
        StartCoroutine(UpdateSceneSelection());
        if (pauseInstructionsOverlay is not null)
        {
            pauseInstructionsOverlay.SetActive(true);
        }
        else
        {
            Debug.LogError("No pauseInstructionsOverlay found", this);
        }
        StartCoroutine(HideInstructionsOverlay());
    }

    void Update()
    {
        HandleInputEvents();
    }

    private IEnumerator BuildSceneList()
    {
        yield return null;

#if UNITY_EDITOR
        if (canvas is not null && pauseInstructionsText is not null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pauseInstructionsText.text = "Press `esc` to toggle pause menu";
        }
        else
        {
            Debug.LogError("Canvas or pauseInstructionsText not found");
        }
#endif

        float positionY = -30.0f;

        int numberOfScenes = SceneManager.sceneCountInBuildSettings;

        // Calculate the height of scroll view content
        // Start by adding the top padding
        float contentHeight = SceneYPositionOffset;
        if (scrollRect is not null)
        {
            for (int i = 0; i < numberOfScenes; i++)
            {
                string sceneName = GetSceneName(i);
                GameObject scrollViewItem = new GameObject(sceneName);
                scrollViewItem.transform.SetParent(scrollRect.content, false);
                Text textComponent = scrollViewItem.AddComponent<Text>();
                textComponent.text = sceneName;
                textComponent.alignment = TextAnchor.MiddleCenter;
                textComponent.color = Color.white;
                if (customFont is not null)
                {
                    textComponent.font = customFont;
                }
                textComponent.fontSize = 14;
                textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;

                RectTransform rectTransform = textComponent.rectTransform;

                if (rectTransform != null)
                {
                    // Set the anchor to be at the top of the parent
                    rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.x, 1f);
                    rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, 1f);
                    rectTransform.pivot = new Vector2(rectTransform.pivot.x, 1f);
                    rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, positionY);

                    // Adjust the RectTransform's width and height
                    float textWidth = textComponent.preferredWidth;
                    float textHeight = textComponent.preferredHeight;
                    rectTransform.sizeDelta = new Vector2(textWidth, textHeight);

                    float itemYOffset = textHeight + SceneYPositionOffset;

                    // Add the item y offset to the scroll view content height
                    contentHeight += itemYOffset;
                }

                positionY = positionY - SceneYPositionOffset;
                _scenes.Add(textComponent);
            }
            scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, contentHeight);
        }
        else
        {
            Debug.LogError("No scrollRect found");
        }
    }

    private string GetSceneName(int sceneIndex)
    {
        // Get the scene name at the specified build index
        string sceneName = SceneUtility.GetScenePathByBuildIndex(sceneIndex);

        // Extract just the scene name without the path and extension
        sceneName = System.IO.Path.GetFileNameWithoutExtension(sceneName);
        return sceneName;
    }

    private void HandleInputEvents()
    {
        if (OVRInput.GetUp(OVRInput.Button.Start))
        {
            HandlePauseButtonPressed();
        }

        if (_isPaused)
        {
            if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstickUp))
            {
                MoveSelectionUp();

            }
            else if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstickDown))
            {
                MoveSelectionDown();

            }
            else if (OVRInput.GetUp(OVRInput.Button.One))
            {

                // Load the selected scene
                LoadScene();
            }

        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandlePauseButtonPressed();
        }

        if (_isPaused)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                MoveSelectionUp();
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                MoveSelectionDown();
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                // Load the selected scene
                LoadScene();
            }
        }
#endif
    }

    private void MoveSelectionUp()
    {
        _selectedSceneIndex = Mathf.Max(0, _selectedSceneIndex - 1);
        StartCoroutine(UpdateSceneSelection());
    }

    private void MoveSelectionDown()
    {
        _selectedSceneIndex = Mathf.Min(_scenes.Count - 1, _selectedSceneIndex + 1);
        StartCoroutine(UpdateSceneSelection());
    }

    private void HandlePauseButtonPressed()
    {
        if (_isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    private void PauseGame()
    {
        OnSceneWillPause?.Invoke();
        _isPaused = true;
        Time.timeScale = 0.0f;
        Time.fixedDeltaTime = 0.0f;
        if (pauseMenuOverlay is not null)
        {
            pauseMenuOverlay.SetActive(true);
        }
        else
        {
            Debug.LogError("No pauseMenuOverlay found");
        }
    }

    private void ResumeGame()
    {
        OnSceneWillResume?.Invoke();
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = this._fixedDeltaTime * Time.timeScale;
        if (pauseMenuOverlay is not null)
        {
            pauseMenuOverlay.SetActive(false);
        }
        else
        {
            Debug.LogError("No pauseMenuOverlay found");
        }
        _isPaused = false;
    }

    private IEnumerator HideInstructionsOverlay()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delayBeforeHidingInstructions);

        // Deactivate the GameObject
        if (pauseInstructionsOverlay is not null)
        {
            pauseInstructionsOverlay.SetActive(false);
        }
        else
        {
            Debug.LogError("No pauseInstructionsOverlay found");
        }
    }

    private IEnumerator UpdateSceneSelection()
    {
        yield return null;

        for (int i = 0; i < _scenes.Count; i++)
        {
            Text text = _scenes[i];

            if (text != null)
            {
                text.color = (i == _selectedSceneIndex) ? selectedColor : Color.white;
            }

            if (_selectedSceneIndex == i)
            {
                if (text != null)
                {
                    RectTransform rectTransform = text.rectTransform;
                    if (rectTransform != null)
                    {
                        SnapTo(rectTransform, SceneYPositionOffset);
                    }
                }
                else
                {
                    Debug.LogError("text is null");
                }
            }
        }
    }

    private void SnapTo(RectTransform target, float padding = 0)
    {
        if (scrollRect is not null)
        {
            Debug.Assert(target.parent == scrollRect.content,
                "EnsureVisibility assumes that 'child' is directly nested in the content of 'scrollRect'");

            float viewportHeight = scrollRect.viewport.rect.height;
            Vector2 scrollPosition = scrollRect.content.anchoredPosition;

            float childRectHeight = target.rect.height;
            float elementTop = target.anchoredPosition.y;
            float elementBottom = elementTop - childRectHeight;

            float visibleContentTop = -scrollPosition.y - padding;
            float visibleContentBottom = -scrollPosition.y - viewportHeight + padding;

            // This keeps the target always visible
            float scrollDelta =
                elementTop > visibleContentTop ? visibleContentTop - elementTop :
                elementBottom < visibleContentBottom ? visibleContentBottom - elementBottom :
                0f;

            scrollPosition.y += scrollDelta;
            scrollRect.content.anchoredPosition = scrollPosition;
        }
        else
        {
            Debug.LogError("No scrollRect found");
        }
    }

    private void LoadScene()
    {
        string sceneName = _scenes[_selectedSceneIndex].text;

        if (sceneName != null)
        {
            OnSceneWillLoad?.Invoke();
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = this._fixedDeltaTime * Time.timeScale;
            if (pauseMenuOverlay is not null)
            {
                pauseMenuOverlay.SetActive(false);
            }
            else
            {
                Debug.LogError("No pauseMenuOverlay found");
            }
            _isPaused = false;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }
}
