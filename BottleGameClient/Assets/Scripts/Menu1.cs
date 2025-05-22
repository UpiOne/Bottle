using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if UNITY_EDITOR
using UnityEditor.Timeline;
#endif

public class Menu1 : MonoBehaviour
{
    public BottleController BottleController;
    public GameObject MenuPan;
    public float money;
    public GameObject HistoryPan;
    public GameObject HelpPan;
    public GameObject DarkBG;

    public GameObject AddMoney;
    public InputField AddMoneyInpyt;

    public GameObject RemoveMoney;
    public InputField RemoveMoneyInpyt;

    public GameObject ReferalPanel;
    public TextMeshProUGUI ReferalLinkText;
    public Button CopyReferalButton;
    public GameObject ReferalCopiedMessage;

    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip panelOpenSound;
    public AudioClip panelCloseSound;
    public AudioSource audioSource;

    public float buttonAnimationDuration = 0.1f;
    public float panelAnimationDuration = 0.2f;

    private ApiManager apiManager;
    private string currentReferalLink = "";
    private Coroutine hideMessageCoroutine = null;

    private static Menu1 _instance;
    public static Menu1 Instance { get { return _instance; } }

    private Dictionary<GameObject, Vector3> panelOriginalScales = new Dictionary<GameObject, Vector3>();
    private Vector2 menuOriginalPosition;
    private Vector2 menuStartPosition = new Vector2(-591f, 1000F);

    private Vector2 originalPosition;
    private Vector3 originalScale;

    private void Awake()
    {
        apiManager = ApiManager.Instance;
    }
    private void CacheOriginalPanelState(RectTransform panel)
    {
        originalPosition = panel.anchoredPosition;
        originalScale = panel.localScale;
    }

    private void Start()
    {
        FindBottleController();
        if (MenuPan != null)
        {
            RectTransform menuRT = MenuPan.GetComponent<RectTransform>();
            panelOriginalScales[MenuPan] = menuRT.localScale;
            menuOriginalPosition = menuRT.anchoredPosition;

            CacheOriginalPanelState(menuRT); 
        }
        if (AddMoney != null)
        {
            panelOriginalScales[AddMoney] = AddMoney.GetComponent<RectTransform>().localScale;
            AddMoney.SetActive(false);
        }
        if (RemoveMoney != null)
        {
            panelOriginalScales[RemoveMoney] = RemoveMoney.GetComponent<RectTransform>().localScale;
            RemoveMoney.SetActive(false);
        }
        if (HistoryPan != null)
        {
            panelOriginalScales[HistoryPan] = HistoryPan.GetComponent<RectTransform>().localScale;
            HistoryPan.SetActive(false);
        }
        if (HelpPan != null)
        {
            panelOriginalScales[HelpPan] = HelpPan.GetComponent<RectTransform>().localScale;
            HelpPan.SetActive(false);
        }
        if (ReferalPanel != null)
        {
            panelOriginalScales[ReferalPanel] = ReferalPanel.GetComponent<RectTransform>().localScale;
            ReferalPanel.SetActive(false);

            if (ReferalCopiedMessage != null)
            {
                ReferalCopiedMessage.SetActive(false);
            }
            
            if (CopyReferalButton != null)
            {
                CopyReferalButton.onClick.RemoveAllListeners();
                CopyReferalButton.onClick.AddListener(CopyReferalToClipboard);
                Debug.Log("Обработчик нажатия добавлен к кнопке копирования реферальной ссылки");
            }
            else
            {
                Debug.LogError("CopyReferalButton не задан в инспекторе!");
            }
        }
        if (MenuPan != null)
        {
            RectTransform menuRT = MenuPan.GetComponent<RectTransform>();
            panelOriginalScales[MenuPan] = menuRT.localScale;
            menuOriginalPosition = menuRT.anchoredPosition;
        }

        UpdateMoney();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindBottleController();
        UpdateMoney();
    }

    private void FindBottleController()
    {
        BottleController = FindObjectOfType<BottleController>();
    }

    private IEnumerator DelayedUpdateMoney()
    {
        UpdateMoney();
        yield return null;
    }

    private IEnumerator AnimateButtonAndToggle(Button button)
    {
        RectTransform rt = button.GetComponent<RectTransform>();
        Vector3 originalScale = rt.localScale;
        Vector3 pressedScale = originalScale * 0.9f; 

        float timer = 0f;
        while (timer < buttonAnimationDuration)
        {
            rt.localScale = Vector3.Lerp(originalScale, pressedScale, timer / buttonAnimationDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        rt.localScale = pressedScale;

        timer = 0f;
        while (timer < buttonAnimationDuration)
        {
            rt.localScale = Vector3.Lerp(pressedScale, originalScale, timer / buttonAnimationDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        rt.localScale = originalScale;

        ToggleMenu();
    }

    private IEnumerator AnimatePanelOpen(GameObject panel)
    {
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt == null)
            yield break;

        Vector3 targetScale = panelOriginalScales.ContainsKey(panel) ? panelOriginalScales[panel] : Vector3.one;
        rt.localScale = Vector3.zero;
        float timer = 0f;
        while (timer < panelAnimationDuration)
        {
            rt.localScale = Vector3.Lerp(Vector3.zero, targetScale, timer / panelAnimationDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        rt.localScale = targetScale;
    }

    private IEnumerator AnimatePanelClose(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogError("AnimatePanelClose: панель NULL!");
            yield break;
        }

        if (!panel.activeSelf)
        {
            Debug.LogWarning("AnimatePanelClose: панель была выключена, принудительно включаем");
            panel.SetActive(true);
        }

        Debug.Log("AnimatePanelClose: начинаем анимацию закрытия");

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogWarning("AnimatePanelClose: нет CanvasGroup у панели, просто отключаем объект");
            panel.SetActive(false);
            yield break;
        }

        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = 0f;
        float closeSpeed = 2f;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.unscaledDeltaTime * closeSpeed;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        panel.SetActive(false);

        Debug.Log("AnimatePanelClose: панель закрыта и деактивирована");
    }


    private IEnumerator AnimateMenuPanelOpen(GameObject panel)
    {
        panel.SetActive(true);
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt == null) yield break;

        rt.anchoredPosition = menuStartPosition;
        rt.localScale = Vector3.zero;

        float timer = 0f;
        while (timer < panelAnimationDuration)
        {
            rt.localScale = Vector3.Lerp(Vector3.zero, originalScale, timer / panelAnimationDuration);
            rt.anchoredPosition = Vector2.Lerp(menuStartPosition, originalPosition, timer / panelAnimationDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        rt.localScale = originalScale;
        rt.anchoredPosition = originalPosition;
    }


    private IEnumerator AnimateMenuPanelClose(GameObject panel)
    {
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt == null)
            yield break;

        Vector3 startScale = rt.localScale;
        Vector2 startPosition = rt.anchoredPosition;
        Vector2 targetPosition = menuStartPosition; 

        float timer = 0f;
        while (timer < panelAnimationDuration)
        {
            rt.localScale = Vector3.Lerp(startScale, Vector3.zero, timer / panelAnimationDuration);
            rt.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, timer / panelAnimationDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        rt.localScale = Vector3.zero;
        rt.anchoredPosition = targetPosition;
        panel.SetActive(false);
    }

    public void OpenIncPan()
    {
        AddMoney.SetActive(true);
        if (audioSource != null && panelOpenSound != null)
            audioSource.PlayOneShot(panelOpenSound);
        StartCoroutine(AnimatePanelOpen(AddMoney));
    }

    public void OpenWithdraw()
    {
        RemoveMoney.SetActive(true);
        if (audioSource != null && panelOpenSound != null)
            audioSource.PlayOneShot(panelOpenSound);
        StartCoroutine(AnimatePanelOpen(RemoveMoney));
    }

    public void OpenHistoryPan()
    {
        HistoryPan.SetActive(true);
        if (audioSource != null && panelOpenSound != null)
            audioSource.PlayOneShot(panelOpenSound);
        StartCoroutine(AnimatePanelOpen(HistoryPan));
    }

    public void OpenHelpPan()
    {
        HelpPan.SetActive(true);
        if (audioSource != null && panelOpenSound != null)
            audioSource.PlayOneShot(panelOpenSound);
        StartCoroutine(AnimatePanelOpen(HelpPan));
    }

    public void OpenReferalPanel()
    {
        if (audioSource != null && panelOpenSound != null)
        {
            audioSource.PlayOneShot(panelOpenSound);
        }

        if (ReferalPanel != null)
        {
            ReferalPanel.SetActive(true);
            StartCoroutine(AnimatePanelOpen(ReferalPanel));
            
            if (apiManager != null)
            {
                if (ReferalLinkText != null)
                {
                    ReferalLinkText.text = "Загрузка ссылки...";
                }
                
                StartCoroutine(apiManager.GetReferalLink((link) => {
                    if (!string.IsNullOrEmpty(link))
                    {
                        currentReferalLink = link;
                        if (ReferalLinkText != null)
                        {
                            ReferalLinkText.text = link;
                        }
                    }
                    else
                    {
                        if (ReferalLinkText != null)
                        {
                            ReferalLinkText.text = "Не удалось получить реферальную ссылку";
                        }
                    }
                }));
            }
            else
            {
                if (ReferalLinkText != null)
                {
                    ReferalLinkText.text = "Ошибка: API не инициализирован";
                }
            }
        }
        
        if (DarkBG != null && !DarkBG.activeSelf)
        {
            DarkBG.SetActive(true);
        }
    }

    public void CloseIncPan()
    {
        if (audioSource != null && panelCloseSound != null)
            audioSource.PlayOneShot(panelCloseSound);
        StartCoroutine(AnimatePanelClose(AddMoney));

        BottleController.AnimateCoinsChange(money);
    }

    public void CloseWithdraw()
    {
        if (audioSource != null && panelCloseSound != null)
            audioSource.PlayOneShot(panelCloseSound);
        StartCoroutine(AnimatePanelClose(RemoveMoney));

        BottleController.AnimateCoinsChange(money);
    }

    public void CloseHistoryPan()
    {
        if (audioSource != null && panelCloseSound != null)
            audioSource.PlayOneShot(panelCloseSound);
        StartCoroutine(AnimatePanelClose(HistoryPan));
    }

    public void CloseHelpPan()
    {
        if (audioSource != null && panelCloseSound != null)
            audioSource.PlayOneShot(panelCloseSound);
        StartCoroutine(AnimatePanelClose(HelpPan));
    }

    public void CloseReferalPanel()
    {
        if (audioSource != null && panelCloseSound != null)
        {
            audioSource.PlayOneShot(panelCloseSound);
        }

        if (ReferalPanel != null)
        {
            StartCoroutine(AnimatePanelClose(ReferalPanel));
        }
        
        bool otherPanelsOpen = 
            (AddMoney != null && AddMoney.activeSelf) ||
            (RemoveMoney != null && RemoveMoney.activeSelf) ||
            (HistoryPan != null && HistoryPan.activeSelf) ||
            (HelpPan != null && HelpPan.activeSelf);
        
        if (!otherPanelsOpen && DarkBG != null)
        {
            DarkBG.SetActive(false);
        }
    }

    public void WithdrawMoney()
    {
        if (float.TryParse(RemoveMoneyInpyt.text, out float amount))
        {
            if (BottleController != null && BottleController.coins >= amount)
            {
                int amountToWithdraw = Mathf.RoundToInt(amount);
                StartCoroutine(apiManager.SendTransaction(amountToWithdraw, false, (success) => {
                    if (success)
                    {
                        UpdateMoney();
                        CloseWithdraw();
                        CloseMenu();
                    }
                }));
            }
        }
    }

    public void IncreaseMoney()
    {
        if (float.TryParse(AddMoneyInpyt.text, out float amount))
        {
            int amountToAdd = Mathf.RoundToInt(amount);
            StartCoroutine(apiManager.SendTransaction(amountToAdd, true, (success) => {
                if (success)
                {
                    UpdateMoney();
                    CloseIncPan();
                    CloseMenu();
                }
            }));
        }
    }

    public void UpdateMoney()
    {
        if (apiManager != null && apiManager.IsReady())
        {
            if (apiManager.IsLocalMode())
            {
                money = PlayerPrefs.GetFloat("local_coins", 1000);
                if (BottleController != null)
                {
                    BottleController.coins = money;
                    BottleController.AnimateCoinsChange(money);
                }
                return;
            }
            StartCoroutine(apiManager.GetUserBalance((balance) => {
        if (BottleController != null)
        {
                    money = balance;
                    BottleController.coins = balance;
                    BottleController.AnimateCoinsChange(money);
                }
            }));
        }
    }

    public void OpenGame()
    {
        MenuPan.SetActive(false);
    }

    public void ToggleMenu()
    {
        if (MenuPan == null)
        {
            return;
        }

        bool isActive = MenuPan.activeSelf;
        if (audioSource != null)
        {
            AudioClip clip = (!isActive) ? openSound : closeSound;
            audioSource.PlayOneShot(clip);
        }
        else
        {
        }

        if (!isActive)
        {
            OpenMenu();
        }
        else
        {
            CloseMenu();
        }
    }
    public void CloseMenu()
    {
        if (MenuPan == null)
        {
            return;
        }

        if (!MenuPan.activeSelf)
        {
            return;
        }

        if (audioSource != null && closeSound != null)
        {
            audioSource.PlayOneShot(closeSound);
        }
        else
        {
        }

        StartCoroutine(AnimateMenuPanelCloseAndHideDarkBG());
    
    }

    private IEnumerator AnimateMenuPanelCloseAndHideDarkBG()
    {
        yield return StartCoroutine(AnimateMenuPanelClose(MenuPan));

        if (DarkBG != null)
        {
            DarkBG.SetActive(false);
        }
    }
    public void OpenMenu()
    {
        if (MenuPan == null)
        {
            return;
        }
        AudioClip clip = openSound;
        audioSource.PlayOneShot(clip);

        if (DarkBG != null)
        {
            DarkBG.SetActive(true);
        }

        UpdateMoney();
        
        StartCoroutine(AnimateMenuPanelOpen(MenuPan));
    }

    public void CopyReferalToClipboard()
    {
        Debug.Log($"Вызван метод CopyReferalToClipboard, текущая ссылка: '{currentReferalLink}'");
        
        if (string.IsNullOrEmpty(currentReferalLink))
        {
            Debug.LogWarning("Menu1: Невозможно скопировать пустую реферальную ссылку");
            if (ReferalCopiedMessage != null)
            {
                ReferalCopiedMessage.SetActive(true);
                ReferalCopiedMessage.GetComponent<TextMeshProUGUI>().text = "Ошибка: ссылка не получена";
                if (hideMessageCoroutine != null)
                {
                    StopCoroutine(hideMessageCoroutine);
                }
                hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay(3f));
            }
            return;
        }
        
        if (apiManager != null)
        {
            // Определяем, находимся ли мы в Telegram
            bool isTelegramWebApp = !apiManager.IsLocalMode();
            
            // Используем обновленный метод для копирования
            bool success = apiManager.CopyTextToClipboard(currentReferalLink);
            Debug.Log($"Результат копирования: {success}");
            
            // Показываем сообщение о результате
            if (ReferalCopiedMessage != null)
            {
                ReferalCopiedMessage.SetActive(true);
                
                // В Telegram показываем другое сообщение
                if (isTelegramWebApp && currentReferalLink.Contains("t.me/"))
                {
                    ReferalCopiedMessage.GetComponent<TextMeshProUGUI>().text = "Реферальная ссылка скопирована";
                }
                else
                {
                    ReferalCopiedMessage.GetComponent<TextMeshProUGUI>().text = "Реферальная ссылка скопирована";
                }
                
                Debug.Log("Показано сообщение об успешном копировании/шаринге");
                
                if (hideMessageCoroutine != null)
                {
                    StopCoroutine(hideMessageCoroutine);
                }
                hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay(3f));
            }
        }
        else
        {
            Debug.LogError("ApiManager не инициализирован (null)");
            if (ReferalCopiedMessage != null)
            {
                ReferalCopiedMessage.SetActive(true);
                ReferalCopiedMessage.GetComponent<TextMeshProUGUI>().text = "Ошибка: не удалось скопировать";
                if (hideMessageCoroutine != null)
                {
                    StopCoroutine(hideMessageCoroutine);
                }
                hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay(3f));
            }
        }
    }
    
    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ReferalCopiedMessage != null)
        {
            ReferalCopiedMessage.SetActive(false);
        }
        hideMessageCoroutine = null;
    }
}
