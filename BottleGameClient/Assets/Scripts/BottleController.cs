using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Haptics;
#if UNITY_WEBGL
using UnityEngine.Scripting;
#endif


public class BottleController : MonoBehaviour
{
    private ApiManager apiManager; // Добавлено
    [Header("Settings")]
    [SerializeField] private float throwForce;
    public Transform startPos;
    [SerializeField] private bool throwBegan;

    [SerializeField] public float coins = 100;
    [SerializeField] private Vector2 startTouchPosition;

    [SerializeField] private bool canRotate;
    [SerializeField] private float timer;
    [SerializeField] private float scoreCooldown;
    [SerializeField] private float rotateStreakTime;
    [SerializeField] private int score;
    [SerializeField] private static int highScore;
    public float bet;
    [SerializeField] private float StartCoins;
    public bool gameOver;
    [SerializeField] private Animator animator;
    [SerializeField] private float winTimer = 0;
    [SerializeField] private float currentCash;
    [SerializeField] private float maxBet = 100000f;

    public float currentMultiplier = 1;
    public float rotationSpeed;
    public Rigidbody2D rb;
    public bool win;
    public WinChanceManager winChanceManager;

    [SerializeField] public GameObject youWinPopUp;
    [SerializeField] private bool glassBottle;
    [SerializeField] private Sprite brokenBottle;
    [SerializeField] private float throwForceBonus;
    [SerializeField] private PhysicsMaterial2D bouncingMaterial;
    [SerializeField] private PhysicsMaterial2D defaultMaterial;

    [Header("Win Messages")]
    [SerializeField] private GameObject niceWinText; // Для множителя 3x
    [SerializeField] private GameObject bigWinText;  // Для множителя 5x
    [SerializeField] private GameObject megaWinText; // Для множителя 10x
    [SerializeField] private GameObject jackpotText; // Для множителя >10x

    [Header("Air Rotation Settings")]
    [SerializeField] private float airRotationMultiplierMin = 1.0f;
    [SerializeField] private float airRotationMultiplierMax = 5.0f;
    [SerializeField] private float landingRotationThreshold = 1.5f;
    [SerializeField] private float rotationCorrectionSpeed = 5f;
    [SerializeField] private bool hasTouchedGround;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] public TextMeshProUGUI coinsText;
    [SerializeField] private InputField betInputfield;
    [SerializeField] private TextMeshProUGUI currentCashText;
    [SerializeField] private GameObject betButton;
    public GameObject DarkBG;
    public UnityEngine.UI.Toggle ToggleAutoStart;

    private Vector3 originalCashScale;


    [Header("Coin Animation")]
    [SerializeField] private float coinAnimDuration = 0.5f;
    [SerializeField] private float coinScaleAmount = 1.2f;
    [SerializeField] private AnimationCurve coinScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private Coroutine coinAnimationCoroutine;
    public float currentDisplayedCoins;

    [SerializeField] private float cashAnimDuration = 0.5f;
    [SerializeField] private float cashScaleAmount = 1.2f;
    [SerializeField] private AnimationCurve cashScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private Coroutine cashAnimationCoroutine;
    private float currentDisplayedCash;

    [Header("GroundCheck")]
    [SerializeField] private Transform feetPos;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool onCap;

    [Header("Boost Settings")]
    [SerializeField] private float boostMultiplierMin = 0.1f;
    [SerializeField] private float boostMultiplierMax = 0.2f;
    [SerializeField] private float boostWinChancePenalty = 15f;

    [Header("Sideways Defeat Settings")]
    [SerializeField] private float sidewaysDefeatThreshold = 1f;
    public float sidewaysTimer;


    [Header("Jump Settings")]
    [SerializeField] private int maxAirJumps = 31;
    public int jumpsCount = 0;

    [Header("Swipe Settings")]
    [SerializeField] private float verticalSwipeMultiplier = 3f;
    [SerializeField] private float maxBonusPercentage = 35f;
    [SerializeField] private float initialRotationBoost = 1.25f;
    [SerializeField] private float boostDuration = 0.3f;

    [Header("Hold Settings")]
    [SerializeField] private float minHoldMultiplier = 1.5f;
    [SerializeField] private float maxHoldMultiplier = 3f;
    [SerializeField] private float maxHoldTime = 2f;

    [Header("Horizontal Throw Settings")]
    [SerializeField] private float horizontalForceMultiplier = 1.8f;
    [SerializeField] private float maxHorizontalOffset = 2f;
    [SerializeField]
    private AnimationCurve horizontalForceCurve = new AnimationCurve(
        new Keyframe(0, 0),
        new Keyframe(1, 1)
    );

    private Vector2 _initialPosition;
    private float _initialTorqueDirection = 1f;
    public bool _firstThrow = false;

    [Header("Force Settings")]
    [SerializeField] private float minThrowForce = 3f;
    [SerializeField] private float maxThrowForce = 5f;
    [SerializeField] private float forceMultiplier = 1.6f;

    [SerializeField] private float maxSwipeLength = 100f;

    [Header("Rotation Settings")]

    private float _currentRotationSpeed = 0f;

    [SerializeField] private float rotationSpeedMultiplier = 1113f;
    [SerializeField] private float maxRotationSpeed = 1500f;

    [Header("Advanced Physics")]
    [SerializeField] private float bottleMass = 0.5f;
    [SerializeField] private float airResistance = 0.1f;
    [SerializeField] private float angularDrag = 0.05f;
    private float holdStartTime;
    private float holdDuration;

    private float gravityTimer = 0f;
    private bool isGrounded;
    private bool rewardGiven = false;
    private bool multiplierLocked = false;
    private bool canFlip;
    private bool multiplierUsed;
    private bool allowAdditionalBoost = false;


    private bool canBoost = true;
    private bool betPlaced = false;

    private float animatedMultiplier = 1f;
    private float targetMultiplier = 3f;
    private Coroutine multiplierAnimationCoroutine;

    private float previousRotationZ;
    private float totalRotation;
    private int fullRotations;
    [SerializeField] private float rotationMultiplierIncrement = 1.0f;

    [Header("Trigger Zone Settings")]
    [SerializeField] private LayerMask triggerZoneLayer;
    [SerializeField] private float uprightThreshold = 10f;
    [SerializeField] private float stabilizationSpeed = 5f;
    private bool hasExitedZone;
    private bool inTriggerZone;
    private bool isStabilized;

    [Header("Air Throw Settings")]
    [SerializeField] private float airThrowWindow = 0.4f;
    [SerializeField] private float airThrowForceMultiplier = 0.66f;
    private float lastSwipeTime;
    private bool inAirThrowWindow;

    [Header("Multiplier Colors")]
    [SerializeField]
    private MultiplierColor[] multiplierColors = new MultiplierColor[]
{
    new MultiplierColor { threshold = 3f, color = Color.white },
    new MultiplierColor { threshold = 7f, color = new Color(0.678f, 0.847f, 0.902f) },
    new MultiplierColor { threshold = 10f, color = Color.blue },
    new MultiplierColor { threshold = 15f, color = new Color(0.5f, 0f, 0.5f) },
    new MultiplierColor { threshold = 20f, color = Color.yellow },
    new MultiplierColor { threshold = 25f, color = Color.red }
};
    private bool _directionLocked = false;
    private Vector3 originalCoinScale;

    [Header("Auto Restart Settings")]
    [SerializeField] private float autoRestartDelay = 2f;
    public float lastBet;
    public FastBet fastBet;

    private static bool autoRestartPending
    {
        get => PlayerPrefs.GetInt("AutoRestartPending", 0) == 1;
        set => PlayerPrefs.SetInt("AutoRestartPending", value ? 1 : 0);
    }

    [Header("Skybox Settings")]
    [SerializeField] private LayerMask skyboxLayer;
    [SerializeField] private PhysicsMaterial2D skyboxMaterial;


    [SerializeField] private float flipHoverTime = 0.1f;

    [Header("Trigger Settings")]
    [SerializeField] private float stabilizationTime = 0.3f;
    [SerializeField] private float victoryDelay = 2f;
    private bool _isStabilizing;
    private Coroutine _stabilizationCoroutine;

    [Header("Trigger Settings")]
    [SerializeField] private float postTriggerCheckDelay = 1f;
    [SerializeField] private float victoryCheckDuration = 2f;

    [Header("Force Settings")]
    [SerializeField] private float horizontalMultiplier = 1f;
  

    [Header("Rotation Settings")]
    [SerializeField] private float minTorque = 100f;
    [SerializeField] private float maxTorque = 500f;

    private bool isClaimed = false;
    private Coroutine autoClaimCoroutine;

    private bool sideDefeatTriggered = false;

    [System.Serializable]
    public struct MultiplierColor
    {
        public float threshold;
        public Color color;
    }
    [Header("Multiplier Colors")]

    [SerializeField] private float scaleIntensity = 1.2f;
    [SerializeField] private float colorTransitionSpeed = 2f;

    [Header("Vertical Force Settings")]
    [SerializeField] private float verticalJumpMultiplier = 1f;

    private Vector3 _inputFieldOriginalPos;
    private List<Vector3> _shakeObjectsOriginalPositions = new List<Vector3>();

    [Header("Bounce Settings")]
    [SerializeField] private int maxBounces = 3;
    public int bounceCount = 0;
    [SerializeField] private float bounceForceReduction = 0.5f;
    [SerializeField] private float maxBounceForce = 8f;
    [SerializeField] private float maxVerticalBounce = 12f;
    [SerializeField] private float sidewaysBounceMultiplier = 0.3f;
    [SerializeField] private float velocityDamping = 0.7f;

    [Header("Sounds")]
    public GameObject swipeSound;
    public GameObject betSound;
    public GameObject errorSound;
    public GameObject landingSound;
    public GameObject winLowSound;
    public GameObject winHighSound;
    public GameObject loseSound;
    public GameObject coinsReceivedSound;
    public GameObject inputClickSound;

    [Header("Popup Animation")]
    [SerializeField] private float popupJumpScale = 1.2f;
    [SerializeField] private float popupJumpDuration = 0.15f;
    [SerializeField] private AnimationCurve popupJumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private Vector3 originalPopupScale;

    [Header("New Victory Settings")]
    [SerializeField] private float victoryRotationThreshold = 40f;
    [SerializeField] private float maxRotationDelta = 0.3f;
    [SerializeField] private float throwCooldownTime = 1.5f;

    private bool victoryCheckActive;
    private bool throwCooldown;
    private bool victoryAchieved;

    [Header("Shake Settings")]
    [SerializeField] private GameObject[] shakeObjects;
    [SerializeField] private float shakeIntensity = 5f;
    [SerializeField] private float shakeDuration = 0.5f;


    [Header("Smooth Rotation Settings")]
    [SerializeField] private float initialRotationSpeed = 200f;
    [SerializeField] private float targetRotationSpeed = 700f;
    [SerializeField] private float rotationRampDuration = 1f;
    private float currentRotationSpeed;
    private bool isSpeedRamping;

    [Header("Bet Buttons")]
    [SerializeField] private UnityEngine.UI.Button increaseBetButton;
    [SerializeField] private UnityEngine.UI.Button decreaseBetButton;

    private const int MIN_BET = 10;
    private const int BET_STEP = 10;
    private const int MAX_BET = 1000000;
    [SerializeField] private Color disabledButtonColor = Color.gray;

    [Header("Loss Settings")]
    [SerializeField] private float respawnDelay = 2f;
    private bool isTriggerAfterLoss;
    private Collider2D bottleCollider;



    [Header("Vibration Settings")]
    [SerializeField] private bool vibrationsEnabled = true;
    [SerializeField] private float multiplierVibrationDuration = 0.02f;
    [SerializeField] private float winVibrationDuration = 0.2f;
    [SerializeField] public float buttonVibrationDuration = 0.05f;

    [SerializeField] private float iOSVibrationIntensity = 0.5f;

    [Header("Slot Integration Settings")]
    [SerializeField] private bool virtualLinesActive;

    [SerializeField] private float rotationForLine = 360f;

    private bool _mainLineActive;
    private bool _bonusLineActive;
    private bool _scatterHit;

    [SerializeField] private TimerUI roundTimer;


    [Header("Horizontal Movement Settings")]
    [SerializeField] private float maxSideForce = 3f;
    [SerializeField] private float sideForceMultiplier = 1.8f;
    [SerializeField] private float airControl = 0.4f;
    [SerializeField]
    private AnimationCurve sideForceCurve = new AnimationCurve(
        new Keyframe(0, 0),
        new Keyframe(1, 1)
    );
    public GameObject TrigerZoneObj;
    public GameObject EffectWin;
    public GameObject ButtonHideOpenMenu;
    public float particleYOffset = 50f;
    private float initialBottleAngle;
    private const float AngleThreshold = 30f;

    private float lastGroundTouchTime;
    private Coroutine _lossCheckCoroutine;
    private bool isCheckingLoss;
    private bool InTrigger = false;

    private bool CanGround;
    private bool CanGameOver;
    private bool NeedLoss;
    private bool hasBouncedRight = false;
    private bool canBet;
    public bool isGameEnding = false;
    private int rightBounceCount = 0;

    private bool firstthrow = false;
    private bool startStable = false;
    private bool SwipeNo;

    private Camera mainCamera;
    private int bounce;

    private Rigidbody2D _rb;
    private Collider2D _collider;
    private Transform _transform;

    private Vector2 _verticalVelocity = Vector2.up;
    private float _currentTorque;
    private float _invMaxSwipeLengthSqr;
    private float _minJumpForce;
    private float _maxJumpForce;
    public bool FirstBet;

    private Camera _mainCamera;
    private Animator _cameraAnimator;
    private Vector3 _cachedPosition;
    private float _cachedRotationZ;
    private bool _isTimerStart;
    private Animator _scoreAnimator;
    private bool _isWinChanceManagerNull;
    private float StartCoinsss;


    private static PhysicsMaterial2D _cachedBouncyMaterial;
    void Awake()
{
    apiManager = ApiManager.Instance; // Добавлено
    ToggleAutoStart.isOn = PlayerPrefs.GetInt("AutoStart", 0) == 1;
    ToggleAutoStart.onValueChanged.AddListener(OnAutoStartToggleChanged);
    rb = GetComponent<Rigidbody2D>();
    rb.constraints = RigidbodyConstraints2D.FreezePositionX;
    _invMaxSwipeLengthSqr = 1f / (maxSwipeLength * maxSwipeLength);
    _minJumpForce = minThrowForce * 0.7f;
    _maxJumpForce = maxThrowForce * 3.5f;
    _currentTorque = currentRotationSpeed * Mathf.Sign(_initialTorqueDirection);
    _scoreAnimator = scoreText.GetComponent<Animator>();
    _isWinChanceManagerNull = winChanceManager == null;

    _mainCamera = Camera.main;
    if (_mainCamera != null)
        _cameraAnimator = _mainCamera.GetComponent<Animator>();

    _verticalVelocity = new Vector2(0, 1);
    mainCamera = Camera.main;
    _rb = GetComponent<Rigidbody2D>();
    _collider = GetComponent<Collider2D>();
    _transform = transform;

    lastBet = PlayerPrefs.GetFloat("LastBet", 0);
}
    private bool IsInTelegramWebView()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return WebGLHandler.IsTelegramEnvironment();
#else
        return false;
#endif
    }

    private string GetHapticType(float duration)
    {
        return duration switch
        {
            > 0.1f => "heavy",
            > 0.05f => "medium",
            _ => "light"
        };
    }

    private void ExecuteHapticFeedback(string type)
    {
        try
        {
            Application.ExternalEval(@"
            if (window.Telegram && Telegram.WebApp) {
                Telegram.WebApp.HapticFeedback.impactOccurred('" + type + @"');
            }
        ");
        }

        catch
        { }
    }
    public static class WebGLHandler
    {
        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
            Application.ExternalEval(@"
            if (window.Telegram && Telegram.WebApp) {
                SendMessage('WebGLHandler', 'SetTelegramEnvironment', 'true');
            }
        ");
        }

        public static bool IsTelegramEnvironment()
        {
            return PlayerPrefs.GetInt("IsTelegram", 0) == 1;
        }

        public static void SetTelegramEnvironment(string value)
        {
            PlayerPrefs.SetInt("IsTelegram", value == "true" ? 1 : 0);
        }
    }
    private void UpdateBalanceFromServer()
{
    if (apiManager != null && apiManager.IsReady())
    {
        if (apiManager.IsLocalMode())
        {
            coins = PlayerPrefs.GetFloat("local_coins", 1000);
            AnimateCoinsChange(coins);
            if (winChanceManager != null)
            {
                winChanceManager.UpdatePlayerBalance(coins);
            }
            return;
        }
        StartCoroutine(apiManager.GetUserBalance((balance) => {
            coins = balance; // Обновляем локальную переменную
            AnimateCoinsChange(coins); // Обновляем UI
            if (winChanceManager != null)
            {
                winChanceManager.UpdatePlayerBalance(coins); // Обновляем WinChanceManager
            }
        }));
    }
}
     private void Start()
    {
        originalCoinScale = coinsText.transform.localScale;
        originalPopupScale = youWinPopUp.transform.localScale;
        youWinPopUp.transform.localScale = Vector3.zero; 

        // Скрываем все тексты выигрышей при старте
        HideAllWinTexts();
     
        lastBet = PlayerPrefs.GetFloat("LastBet", MIN_BET);
        if (lastBet < MIN_BET) lastBet = MIN_BET;
        betInputfield.text = lastBet.ToString(); 

        // Логика авто-рестарта (теперь использует проверку баланса сервера)
        if (autoRestartPending && ToggleAutoStart.isOn)
        {
            autoRestartPending = false; // Используем флаг
            ButtonHideOpenMenu.SetActive(false);
            // Используем корутину, которая теперь проверит баланс сервера перед ставкой
            StartCoroutine(AutoRestartAfterSceneLoad());
        }

        _inputFieldOriginalPos = betInputfield.transform.position;

        // Инициализация позиций для тряски UI
        if (shakeObjects != null && shakeObjects.Length > 0)
        {
            _shakeObjectsOriginalPositions.Clear();
            foreach (var obj in shakeObjects)
            {
                if (obj != null)
                    _shakeObjectsOriginalPositions.Add(obj.transform.position);
            }
        }

        originalCashScale = currentCashText.transform.localScale;
        // originalCoinScale уже установлен выше
        firstthrow = false;
        NeedLoss = false;
        SwipeNo = true; // Изначально свайпа не было
        TrigerZoneObj.SetActive(false);
        _initialPosition = startPos.position;
        initialBottleAngle = NormalizeAngle(transform.eulerAngles.z);
        rb = GetComponent<Rigidbody2D>(); // Уже сделано в Awake, возможно избыточно
        bottleCollider = GetComponent<Collider2D>(); // Уже сделано в Awake, возможно избыточно

        // Настройка поля ввода
        betInputfield.contentType = (InputField.ContentType)TMP_InputField.ContentType.DecimalNumber;
        betInputfield.keyboardType = TouchScreenKeyboardType.NumberPad;
        betInputfield.onValidateInput += ValidateNumericInput;
        // Добавляем слушатель для обновления интерактивности кнопок +/- при изменении значения
        betInputfield.onValueChanged.AddListener(OnBetValueChanged);

        // Настройка EventTrigger для поля ввода (звук клика при выборе)
        EventTrigger trigger = betInputfield.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = betInputfield.gameObject.AddComponent<EventTrigger>(); // Убедимся, что триггер есть
        // Очистим существующие триггеры на всякий случай
        trigger.triggers.Clear();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.Select;
        entry.callback.AddListener((data) => { OnInputFieldSelected(betInputfield.text); });
        trigger.triggers.Add(entry);

        // Инициализация отображаемых монет (будет обновлено с сервера)
        currentDisplayedCoins = 0; // Начинаем с 0 для анимации
        coinsText.text = currentDisplayedCoins.ToString("F2"); // Показываем начальное значение

        if (bottleCollider == null)
        {
            bottleCollider = GetComponent<Collider2D>(); // Избыточная проверка
        }
        // Настройка физики Rigidbody
        rb.mass = bottleMass;
        rb.drag = airResistance;
        rb.angularDrag = angularDrag; // Используем переменную angularDrag

        // Настройка физического материала по умолчанию
        if (defaultMaterial != null) // Проверяем, назначен ли материал
        {
            defaultMaterial.friction = 0.4f; // Устанавливаем трение
            if (bottleCollider != null) bottleCollider.sharedMaterial = defaultMaterial; // Применяем материал к коллайдеру
        }

        // Интеграция с балансом сервера 
        if (apiManager != null && apiManager.IsLocalMode())
        {
            coins = PlayerPrefs.GetFloat("local_coins", 1000);
            AnimateCoinsChange(coins);
            if (winChanceManager != null)
            {
                winChanceManager.UpdatePlayerBalance(coins);
            }
        }
        else
        {
            UpdateBalanceFromServer(); // Получаем начальный баланс с сервера
            InvokeRepeating("UpdateBalanceFromServer", 5f, 5f); // Запускаем периодическое обновление баланса
        }
        // Конец интеграции с балансом сервера 

        // Настройка материала Skybox (если это предполагалось)
        skyboxMaterial = new PhysicsMaterial2D
        {
            bounciness = 0f,
            friction = 1f
        };

        hasExitedZone = false; // Сбрасываем флаг выхода из триггер-зоны

        scoreText.text = ""; // Очищаем текст множителя при старте

        // Логика авто-старта при включенном Toggle (но не при авто-рестарте после сцены)
        if (ToggleAutoStart.isOn && lastBet > 0 && !autoRestartPending)
        {
            ButtonHideOpenMenu.SetActive(false);
            // Используем корутину для проверки баланса ПЕРЕД ставкой
            StartCoroutine(apiManager.GetUserBalance((balance) => {
                coins = balance; // Обновляем локальную переменную для справки
                AnimateCoinsChange(coins); // Обновляем UI
                if (winChanceManager != null)
                {
                    winChanceManager.UpdatePlayerBalance(coins);
                }
                // Продолжаем только если баланс достаточный
                if (balance >= lastBet)
                {
                    // Используем компонент FastBet для ставки и старта
                    if (fastBet != null) // Убедимся, что компонент FastBet назначен
                    {
                        fastBet.BetAndStart((int)lastBet);
                    }
                    else
                    {
                        Debug.LogError("Компонент FastBet не назначен!");
                        // Запасной вариант или обработка ошибки, если FastBet отсутствует
                        OnBetButtonClicked(); // Пытаемся использовать обычную логику кнопки как запасной вариант
                    }
                    if (roundTimer != null) // Убедимся, что таймер назначен
                    {
                       roundTimer.StartNewRound();
                    } else {
                        Debug.LogWarning("RoundTimer не назначен!");
                    }
                } else {
                     // Опционально: Уведомляем игрока, если авто-старт не удался из-за низкого баланса
                     Debug.Log("Авто-старт пропущен: Недостаточно средств.");
                     ButtonHideOpenMenu.SetActive(true); // Показываем меню, если авто-старт не удался
                     ToggleAutoStart.isOn = false; // Выключаем переключатель, если не можем авто-стартовать
                }
            }));
        }
        else if (!ToggleAutoStart.isOn) // Если авто-старт выключен
        {
             ButtonHideOpenMenu.SetActive(true); // Убедимся, что меню видно
        }

        // TrigerZoneObj.SetActive(false); // Уже установлено выше

        // Обновляем состояние кнопок +/- ставки при старте
        UpdateBetButtonsInteractable();
    }
    private IEnumerator AutoRestartAfterSceneLoad()
    {
        yield return new WaitForEndOfFrame();
        /*
        if (coins >= lastBet && lastBet > 0)
        {
            betInputfield.text = lastBet.ToString();
            OnBetButtonClicked();
        }
        else
        {
            autoRestartPending = false;
            betButton.SetActive(true);
        }
      */
    }
    public void OnAutoStartToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt("AutoStart", isOn ? 1 : 0);
        PlayerPrefs.Save();
        ButtonHideOpenMenu.SetActive(!isOn);
    }
    public void IncreaseBet()
    {
        if (FirstBet == false)
        {
            if (!int.TryParse(betInputfield.text, out int currentBet))
            {
                if (errorSound != null) Instantiate(errorSound);
                StartCoroutine(ShakeInputField());
                StartCoroutine(ShakeObjects());
                currentBet = MIN_BET;
            }

            int newBet = Mathf.Min(currentBet + BET_STEP, MAX_BET);
            betInputfield.text = newBet.ToString();

            UpdateBetButtonsInteractable();
            if (inputClickSound != null) Instantiate(inputClickSound);
        }
    }

    public void DecreaseBet()
    {
        if (FirstBet == false)
        {
            if (!int.TryParse(betInputfield.text, out int currentBet))
            {
                currentBet = MIN_BET;
            }

            int newBet = Mathf.Max(currentBet - BET_STEP, MIN_BET);
            betInputfield.text = newBet.ToString();

            UpdateBetButtonsInteractable();
            if (inputClickSound != null) Instantiate(inputClickSound);
        }
    }
    private void UpdateBetButtonsInteractable()
    {
        if (!int.TryParse(betInputfield.text, out int currentBet))
        {
            currentBet = MIN_BET;
        }

        increaseBetButton.interactable = currentBet < MAX_BET;
        decreaseBetButton.interactable = currentBet > MIN_BET;

    }
    private void OnBetValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        if (int.TryParse(value, out int betValue))
        {
            int clampedBet = Mathf.Clamp(betValue, MIN_BET, MAX_BET);
            if (betValue != clampedBet)
            {
                betInputfield.text = clampedBet.ToString();
            }
        }

        UpdateBetButtonsInteractable();
    }
    public void SetInputEnabled(bool enabled)
    {
        canRotate = enabled;
    }

    public void AnimateCoinsChange(float targetCoins)
    {
        if (coinAnimationCoroutine != null)
        {
            StopCoroutine(coinAnimationCoroutine);
            coinsText.transform.localScale = originalCoinScale;
        }

        if (Mathf.Approximately(currentDisplayedCoins, targetCoins))
        {
            return;
        }

        coinAnimationCoroutine = StartCoroutine(AnimateCoinsCoroutine(targetCoins));

        float coinsAdded = targetCoins - currentDisplayedCoins;
        for (int i = 0; i < coinsAdded; i++)
        {
            //  TriggerCoinVibration();
        }
    }
     private void TriggerCoinVibration()
      {
          TriggerVibration(0.05f); // Short, light vibration
      }
     public void TriggerVibration(float duration)
      {
          if (!vibrationsEnabled)
              return;

          // For iOS: Use haptic feedback if supported.
  #if UNITY_IOS
      if (Device.hapticSupportSupported)
      {
          HapticFeedback.Generate(
              duration > 0.2f ? HapticFeedback.HapticFeedbackType.Success :
              duration > 0.1f ? HapticFeedback.HapticFeedbackType.Warning :
              HapticFeedback.HapticFeedbackType.Failure
          );
          return;
      }
  #endif

          // For Android: Use the built-in vibration.
  #if UNITY_ANDROID
      Handheld.Vibrate();
      return;
  #endif

          // For web platforms
        
      }

      private void ExecuteWebVibrationForTrigger(float duration)
      {
          try
          {
              string vibrationCommand = $@"
          if (navigator.vibrate) {{
              navigator.vibrate({duration * 1000});
          }}";
              Application.ExternalEval(vibrationCommand);
          }

          catch
          { }
      }
   
    private IEnumerator AnimateCoinsCoroutine(float targetCoins)
    {
        float startCoins = currentDisplayedCoins;
        float elapsed = 0f;
        Vector3 originalScale = coinsText.transform.localScale;

        while (elapsed < coinAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = coinScaleCurve.Evaluate(elapsed / coinAnimDuration);

            float scale = Mathf.Lerp(1f, coinScaleAmount, Mathf.PingPong(t * 2, 1));
            coinsText.transform.localScale = originalCoinScale * scale;

            currentDisplayedCoins = Mathf.Lerp(startCoins, targetCoins, t);
            coinsText.text = currentDisplayedCoins.ToString("F2");

            yield return null;
        }

        currentDisplayedCoins = targetCoins;
        coinsText.text = currentDisplayedCoins.ToString("F2");
        coinsText.transform.localScale = originalCoinScale;
    }
    public void HandleTimeExpired()
    {
        if (!win) HandleLoss("Time Expired!");
    }

    private void ApplyRotationForce(float swipePower)
    {
        float torque = swipePower * rotationSpeed * Time.fixedDeltaTime * 1;
        rb.AddTorque(torque, ForceMode2D.Impulse);
    }

    private IEnumerator DoubleCheckBounceDefeat()
    {
        yield return new WaitForSeconds(4f);

        if (bounceCount >= maxBounces && !gameOver)
        {
            HandleLoss("Too many bounces!");
        }
    }
    private void ApplyBounceEffects(Collision2D collision)
    {
        if (Mathf.Abs(transform.eulerAngles.z) < 3f && firstthrow && win) 
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
       
            currentRotationSpeed = 0;
            rotationSpeed = 0;

        }
        if (!throwBegan || bounceCount >= maxBounces || gameOver) return;

        if (_lossCheckCoroutine != null)
        {
            StopCoroutine(_lossCheckCoroutine);
            _lossCheckCoroutine = null;
        }

        float reductionFactor = Mathf.Pow(bounceForceReduction, bounceCount);
        float impactForce = collision.relativeVelocity.magnitude * reductionFactor;
        float bounceForce = Mathf.Min(impactForce, maxBounceForce);

        Vector2 bounceDirection = collision.contacts[0].normal.normalized;
        rb.AddForce(bounceDirection * bounceForce, ForceMode2D.Impulse);

        rb.velocity *= velocityDamping;
        rb.velocity = Vector2.ClampMagnitude(rb.velocity, maxVerticalBounce);

        bounceCount++;

        if (bounceCount >= maxBounces)
        {
            rb.velocity *= 0.3f;
            rb.angularVelocity *= 0.5f;
            _lossCheckCoroutine = StartCoroutine(DoubleCheckBounceDefeat());
        }

        StartCoroutine(PlayBounceEffect());


        if (bounceCount == 2 && win)
        {

            StopCoroutine("ForceStabilization");

            StartCoroutine(CheckLandingResult());
        }
    }


    private IEnumerator PlayBounceEffect()
    {
        Vector3 originalScale = transform.localScale;

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float squashY = Mathf.Lerp(1f, 0.9f, Mathf.Sin(t * Mathf.PI));
            float stretchX = Mathf.Lerp(1f, 1.1f, Mathf.Sin(t * Mathf.PI));

            // transform.localScale = new Vector3(
            //    originalScale.x * stretchX,
            //    originalScale.y * squashY,
            //    originalScale.z
            // );

            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float squashY = Mathf.Lerp(0.9f, 1f, Mathf.Sin(t * Mathf.PI));
            float stretchX = Mathf.Lerp(1.1f, 1f, Mathf.Sin(t * Mathf.PI));

            // transform.localScale = new Vector3(
            //    originalScale.x * stretchX,
            //    originalScale.y * squashY,
            //    originalScale.z
            // );

            yield return null;
        }

        // transform.localScale = originalScale;
    }

    private char ValidateNumericInput(string text, int charIndex, char addedChar)
    {
        if (text.Length >= 7 && addedChar != '\b')
        {
            if (errorSound != null) Instantiate(errorSound);
            StartCoroutine(ShakeInputField());
            StartCoroutine(ShakeObjects());
            return '\0';
        }
        if (addedChar == '-')
            return '\0';

        if (char.IsDigit(addedChar) || addedChar == '\b')
            return addedChar;
      
            if (char.IsDigit(addedChar) || addedChar == '\b') 
            {
                return addedChar;
            }
        if (text.Length >= 7)
        {
            return '\0';
        }
        return '\0';
    }
    private IEnumerator ResetSquash()
    {
        yield return new WaitForSeconds(0.1f);
        // transform.localScale = Vector3.one; 
    }

    public bool PlaceBet(int value, Action onBetConfirmed = null)
    {
        if (FirstBet) return false;
        if (value < MIN_BET || value <= 0 || value > maxBet)
        {
            if (errorSound != null) Instantiate(errorSound);
            StartCoroutine(ShakeInputField());
            StartCoroutine(ShakeObjects());
            return false;
        }
        if (apiManager != null && apiManager.IsLocalMode())
        {
            // Локальный режим: мгновенно списываем баланс и подтверждаем ставку
            float localBalance = PlayerPrefs.GetFloat("local_coins", 1000);
            localBalance -= value;
            PlayerPrefs.SetFloat("local_coins", localBalance);
            PlayerPrefs.Save();
            coins = localBalance;
            AnimateCoinsChange(coins);
            winChanceManager?.UpdatePlayerBalance(coins);
            bet = value;
            lastBet = value;
            betPlaced = true;
            PlayerPrefs.SetFloat("LastBet", lastBet);
            ButtonHideOpenMenu.SetActive(false);
            FirstBet = true;
            betInputfield.interactable = false;
            betButton.SetActive(false);
            onBetConfirmed?.Invoke();
            return true;
        }
        StartCoroutine(apiManager.SendTransaction(value, false, (success) => {
            if (success)
            {
                StartCoroutine(apiManager.GetUserBalance((balance) => {
                    coins = balance;
                    AnimateCoinsChange(coins);
                    winChanceManager?.UpdatePlayerBalance(coins);
                }));

           
                onBetConfirmed?.Invoke();
            }
            else
            {
                // Ошибка транзакции: показать ошибки и разблокировать UI
                if (errorSound != null) Instantiate(errorSound);
                StartCoroutine(ShakeInputField());
                StartCoroutine(ShakeObjects());

                betPlaced = false;
                FirstBet = false;
                betButton.SetActive(true);
                betInputfield.interactable = true;
                ButtonHideOpenMenu.SetActive(true);
            }
        }));

        // Локальная блокировка UI до ответа от сервера
        if (betSound != null) Instantiate(betSound);

        bet = value;
        lastBet = value;
        betPlaced = true;
        PlayerPrefs.SetFloat("LastBet", lastBet);

        ButtonHideOpenMenu.SetActive(false);
        FirstBet = true;
        betInputfield.interactable = false;
        betButton.SetActive(false);

        return true;
    }


    public void RestartScene()
    {
 
        CancelInvoke();
        StopAllCoroutines();
        if (!ToggleAutoStart.isOn) autoRestartPending = false;
        if (bottleCollider != null)
        {
            bottleCollider.isTrigger = false;
        }
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReceiveCoins(float value)
{
    // Отправка транзакции выигрыша на сервер
    StartCoroutine(apiManager.SendTransaction(Mathf.RoundToInt(value), true, (success) => {
        if (success)
        {
            // При УСПЕШНОМ зачислении, запрашиваем АКТУАЛЬНЫЙ баланс
            StartCoroutine(apiManager.GetUserBalance((balance) => {
                coins = balance;
                AnimateCoinsChange(coins);
                if (winChanceManager != null)
                {
                    winChanceManager.UpdatePlayerBalance(coins);
                }
            }));
        }
        else
        {
            // Обработка ошибки зачисления (маловероятно, но возможно)
            Debug.LogError("Failed to receive coins transaction on server!");
            // Возможно, стоит попробовать повторить транзакцию или уведомить пользователя
        }
    }));

    // Локальное изменение баланса и сохранение в PlayerPrefs удалено
    // coins += value;
    // AnimateCoinsChange(coins); // Анимация будет вызвана после ответа сервера
    // PlayerPrefs.SetFloat("coins", coins);
    // PlayerPrefs.Save();
}
    private void OnEnable()
{
    // Возобновляем обновление баланса при активации объекта
    UpdateBalanceFromServer(); // Получить баланс сразу при включении
    // Перезапускаем InvokeRepeating, если он был отменен в OnDisable
    // Проверяем, не запущен ли он уже, чтобы избежать дублирования
    CancelInvoke("UpdateBalanceFromServer"); // Отменяем на всякий случай
    InvokeRepeating("UpdateBalanceFromServer", 5f, 5f); // Запускаем снова
}

private void OnDisable()
{
    // Останавливаем периодическое обновление баланса при деактивации
    CancelInvoke("UpdateBalanceFromServer");
}
    public void ClaimReward()
    {
        if (coinsReceivedSound != null) Instantiate(coinsReceivedSound);

        if (autoClaimCoroutine != null)
        {
            StopCoroutine(autoClaimCoroutine);
            autoClaimCoroutine = null;
        }

        currentCash = Mathf.Floor(bet * currentMultiplier * 100) / 100;
        AnimateCashChange(currentCash);
        ReceiveCoins(currentCash);

        currentCash = 0;
        currentMultiplier = 1;
        targetMultiplier = 1f;
        animatedMultiplier = 1f;

        if (multiplierAnimationCoroutine != null)
        {
            StopCoroutine(multiplierAnimationCoroutine);
        }
        multiplierAnimationCoroutine = StartCoroutine(AnimateMultiplier());

        scoreText.text = "";
        youWinPopUp.SetActive(false);

        if (winChanceManager != null)
        {
            winChanceManager.GenerateNewMultiplier();
        }
    }



    private void Update()
    {
        if (!roundTimer.TimerStart) return;
        _isTimerStart = true;

        _cachedPosition = transform.position;
        _cachedRotationZ = transform.localEulerAngles.z;

        if (InTrigger && firstthrow && win)
        {
            HandleRotationFreeze();
        }

        HandlePhysicsChecks();
        HandleInput();
        HandleRotationLogic();
        HandleGroundChecks();
        HandleSpecialConditions();
    }

    private void HandleRotationFreeze()
    {
        if (Mathf.Abs(_cachedRotationZ) < 3f)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.angularVelocity = 0f;
            currentRotationSpeed = 0;
            rotationSpeed = 0;
        }
    }

    private void HandlePhysicsChecks()
    {
        if (throwBegan && !gameOver)
        {
            float clampedX = Mathf.Clamp(
                _cachedPosition.x,
                _initialPosition.x - maxHorizontalOffset,
                _initialPosition.x + maxHorizontalOffset
            );
            transform.position = new Vector3(clampedX, _cachedPosition.y, _cachedPosition.z);
        }

        isGrounded = Physics2D.OverlapCircle(feetPos.position, groundCheckRadius, groundLayer);
        hasTouchedGround |= isGrounded;
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startTouchPosition = (Vector2)Input.mousePosition;
            holdStartTime = Time.time;
        }

        if (Input.GetMouseButtonUp(0))
        {
            HandleSwipeActions();
        }
    }

    private void HandleSwipeActions()
    {
        Vector2 currentMousePos = (Vector2)Input.mousePosition;

        if (!throwBegan && timer > 0.2f)
        {
            Vector2 swipeDelta = currentMousePos - startTouchPosition;
            float swipeLength = Mathf.Clamp(swipeDelta.magnitude, 0, maxSwipeLength);
            ThrowBottle(swipeLength / maxSwipeLength);
        }
        else if (throwBegan && !isGrounded)
        {
            ApplyAirJump(currentMousePos - startTouchPosition * 2);
        }
    }

    private void HandleRotationLogic()
    {
        if (Mathf.Abs(rb.angularVelocity) > targetRotationSpeed && !isSpeedRamping)
        {
            rb.angularVelocity = Mathf.Sign(rb.angularVelocity) * targetRotationSpeed;
        }

        rotationSpeed = Mathf.Min(rotationSpeed, maxRotationSpeed);
        rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -maxRotationSpeed, maxRotationSpeed);

        if ((isGrounded && throwBegan) || (onCap && throwBegan))
        {
            winTimer += Time.deltaTime;
        }
    }

    private void HandleGroundChecks()
    {
        if (isGrounded && !gameOver)
        {
            HandleSidewaysDefeat();
        }

        canRotate = !(isGrounded || onCap);
        if (!canRotate) rotationSpeed = 0;
    }

    private void HandleSidewaysDefeat()
    {
        float currentAngle = _cachedRotationZ % 360;
        bool isSideways = (currentAngle > 75f && currentAngle < 105f) ||
                         (currentAngle > 255f && currentAngle < 285f);

        if (isSideways && !sideDefeatTriggered)
        {
            sideDefeatTriggered = true;
        }
    }

    private void HandleSpecialConditions()
    {
        float deltaTime = Time.deltaTime;
        scoreCooldown += deltaTime;
        timer += deltaTime;

        if (win && !isGrounded && _cachedPosition.y < landingRotationThreshold)
        {
            RotateForLanding();
        }
    }
    public void AnimateCashChange(float targetCash)
    {
        if (Mathf.Approximately(currentDisplayedCash, targetCash))
        {
            return;
        }

        if (cashAnimationCoroutine != null)
        {
            StopCoroutine(cashAnimationCoroutine);
            currentCashText.transform.localScale = Vector3.one;
        }


        cashAnimationCoroutine = StartCoroutine(AnimateCashCoroutine(targetCash));
    }


    private IEnumerator AnimateCashCoroutine(float targetCash)
    {
        float startCash = currentDisplayedCash;
        float elapsed = 0f;
        Vector3 originalScale = originalCashScale;
        while (elapsed < cashAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = cashScaleCurve.Evaluate(elapsed / cashAnimDuration);

            float scale = Mathf.Lerp(1f, cashScaleAmount, Mathf.PingPong(t * 2, 1));
            currentCashText.transform.localScale = originalScale * scale;

            currentDisplayedCash = Mathf.Lerp(startCash, targetCash, t);
            currentCashText.text = currentDisplayedCash.ToString("F2");

            yield return null;
        }

        currentDisplayedCash = targetCash;
        currentCashText.text = currentDisplayedCash.ToString("F2");
        currentCashText.transform.localScale = originalScale;
        currentCashText.transform.localScale = originalScale;
    }
    private void ApplyAirJump(Vector2 swipeDelta)
    {
        if (swipeSound != null) Instantiate(swipeSound);

        NeedLoss = false;

        if (jumpsCount >= maxAirJumps || gameOver) return;

        startStable = false;

        if (!TrigerZoneObj.activeSelf)
            TrigerZoneObj.SetActive(true);

        firstthrow = true;

        float swipePower = Mathf.Clamp01(swipeDelta.sqrMagnitude * _invMaxSwipeLengthSqr * Time.deltaTime);

        float verticalForce = Mathf.Lerp(
            _minJumpForce,
            _maxJumpForce,
            swipePower
        ) * verticalJumpMultiplier;

        bool isRotating = Mathf.Abs(rb.angularVelocity) > 1f; 

        if (!isRotating)
        {
            float rotationDirection = UnityEngine.Random.Range(0, 2) * 2 - 1; 
            rb.angularVelocity = 400f * rotationDirection;

            rb.angularDrag = 0f; 
            rb.constraints &= ~RigidbodyConstraints2D.FreezeRotation; 
        }

        rb.velocity = _verticalVelocity * verticalForce;
        rb.AddTorque(_currentTorque, ForceMode2D.Impulse);

        currentCash = bet * currentMultiplier;
        currentCashText.text = currentDisplayedCash.ToString("F2");
        bounceCount = 0;
        jumpsCount++;

        AddMultiplier();

        NeedLoss = (jumpsCount >= maxAirJumps);
    }


    private IEnumerator AirThrowWindowCheck()
    {
        inAirThrowWindow = true;
        yield return new WaitForSeconds(airThrowWindow);
        inAirThrowWindow = false;
    }

    private IEnumerator CheckLossAfterDelay(float delay)
    {
        isCheckingLoss = true;
        float startTime = Time.time;

        while (Time.time - startTime < delay)
        {
            if (!isGrounded || gameOver)
            {
                isCheckingLoss = false;
                yield break;
            }
            yield return null;
        }

        if (isGrounded && !gameOver)
        {
            yield return new WaitForSeconds(1f);

        }
        isCheckingLoss = false;
    }
    private IEnumerator DelayedSideDefeat()
    {
        yield return new WaitForSeconds(0.2f);
        if (isGameEnding) yield break;
        isGameEnding = true;
        bool isSideways = (transform.localEulerAngles.z > 80 && transform.localEulerAngles.z < 100) ||
                          (transform.localEulerAngles.z > 260 && transform.localEulerAngles.z < 280);
        if (isGrounded && isSideways)
        {
            win = false;
            gameOver = true;
            canRotate = false;
            rb.velocity = Vector2.zero;

            youWinPopUp.SetActive(true);
            TextMeshProUGUI lossText = youWinPopUp.GetComponent<TextMeshProUGUI>();

            if (lossText != null)
            {
                lossText.enableVertexGradient = true;
                lossText.color = Color.white; 

                lossText.colorGradient = new VertexGradient(
                    new Color(1f, 0.713f, 0.216f),    //  (#FFB637)
                    new Color(1f, 0.713f, 0.216f),    //  (правый)
                    new Color(1f, 0.145f, 0.259f),    //  (#FF2542)
                    new Color(1f, 0.145f, 0.259f)     //  (правый)
                );

                lossText.text = "FAILED";
                lossText.ForceMeshUpdate(); 
            }

            yield return new WaitForSeconds(2f);
            RestartScene();
        }
    }



    private IEnumerator AutoClaimAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isClaimed)
        {
            ClaimReward();
            betButton.SetActive(true);
            betPlaced = false;
        }
    }

    private void HandleLanding()
    {
        canRotate = false;
        rb.angularVelocity = 0f;
        rotationSpeed = 0f;

        if (win)
        {
        }
        else
        {
            rb.angularVelocity = UnityEngine.Random.Range(200f, 400f);
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (winChanceManager == null) return;

        if (((1 << other.gameObject.layer) & triggerZoneLayer) != 0 && win)
        {
            inTriggerZone = true;
            hasExitedZone = false;

            InTrigger = true;

            if (isStabilized)
            {
                StopAllCoroutines();
                //StartCoroutine(ForceStabilization());
            }
        }

    }
    private void OnTriggerExit(Collider other)
    {
        InTrigger = true;
    }
    private IEnumerator StabilizationProcess()
    {
        if (NeedLoss == false)
        {
            _isStabilizing = true;
            float timer = 0f;

            while (timer < stabilizationTime && !gameOver)
            {
                if (!IsStablePosition())
                {
                    _isStabilizing = false;
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            LockBottlePosition();

            timer = 0f;
            while (timer < victoryDelay)
            {
                if (!IsPerfectlyUpright())
                {
                    ReleaseBottle();
                    yield break;
                }
                timer += Time.deltaTime;
                yield return null;
            }

            HandleVictory();
        }
    }
    private bool IsStablePosition()
    {
        float angle = Mathf.Abs(transform.eulerAngles.z % 360);
        return (angle <= 45f || angle >= 315f) &&
               rb.angularVelocity < 45f &&
               rb.velocity.magnitude < 1f;
    }
    private bool IsPerfectlyUpright()
    {
        float angle = Mathf.Abs(transform.eulerAngles.z % 360);
        return (angle <= 1f || angle >= 359f) &&
               rb.angularVelocity < 5f &&
               rb.velocity.magnitude < 0.1f;
    }
    private void LockBottlePosition()
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }
    private void ReleaseBottle()
    {
        rb.isKinematic = false;
        _isStabilizing = false;
        rb.AddForce(new Vector2(
            UnityEngine.Random.Range(-0.5f, 0.5f),
            UnityEngine.Random.Range(0.5f, 1.5f)
        ), ForceMode2D.Impulse);
    }
    private IEnumerator PostTriggerCheckRoutine()
    {
        yield return new WaitForSeconds(postTriggerCheckDelay);

        float checkTimer = 0f;
        while (checkTimer < victoryCheckDuration && !gameOver && !inTriggerZone)
        {
            if (IsUpright() && rb.velocity.magnitude < 0.1f)
            {
                HandleVictory();
                yield break;
            }
            checkTimer += Time.deltaTime;
            yield return null;
        }
    }


    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & triggerZoneLayer) != 0)
        {
            inTriggerZone = false;
            isStabilized = false;
            hasExitedZone = true;

            if (!win)
            {
                rb.isKinematic = false;
                rb.simulated = true;
            }
        }
    }
    private IEnumerator VictoryCheckRoutine()
    {
        float checkTimer = 0f;
        while (inTriggerZone && checkTimer < victoryCheckDuration && !gameOver)
        {
            if (IsUpright() && rb.velocity.magnitude < 0.3f)
            {
                HandleVictory();
                yield break;
            }
            checkTimer += Time.deltaTime;
            yield return null;
        }
    }
    private bool IsUpright()
    {
        float angle = transform.eulerAngles.z % 360;
        bool angleCheck = angle <= 45f || angle >= 315f;
        bool physicsCheck = rb.velocity.magnitude < 0.5f && Mathf.Abs(rb.angularVelocity) < 15f;
        return angleCheck && physicsCheck;
    }
    private void HandleVictory()
    {
        victoryAchieved = true;
        gameOver = true;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.Euler(0, 0, 0);
        rb.simulated = false;

        youWinPopUp.SetActive(true);
        currentCash = bet * currentMultiplier;

    }


    private void FixedUpdate()
    {
        /*   if (win)
           {
               StabilizeBottle();
           }
        */


        rb.velocity = new Vector2(0, rb.velocity.y);

        if (isTriggerAfterLoss)
        {
            rb.gravityScale = 3f;
            rb.AddForce(Vector2.down * 10f, ForceMode2D.Impulse);
            transform.localScale *= 0.98f;
            return;
        }

        if (!throwBegan || gameOver) return;

        rb.drag = isGrounded ?
            Mathf.Lerp(0.5f, 2f, rb.velocity.magnitude / maxVerticalBounce) :
            airResistance;

        Vector2 clampedVelocity = rb.velocity;
        clampedVelocity.x = Mathf.Clamp(clampedVelocity.x, -maxHorizontalOffset * 2, maxHorizontalOffset * 2);
        clampedVelocity.y = Mathf.Min(clampedVelocity.y, maxVerticalBounce);
        rb.velocity = clampedVelocity;
    
            if (Mathf.Abs(rb.velocity.x) > 0.1f)
            {
                rb.velocity = Vector2.Lerp(rb.velocity, new Vector2(0, rb.velocity.y),
                    Time.fixedDeltaTime * 5f);
            }
     
        if (throwBegan && !gameOver && !CanGround)
        {
            if (!startStable)
            {
                float direction = Mathf.Sign(rb.angularVelocity);
                if (direction == 0) direction = 1f; 

                if (Mathf.Abs(rb.angularVelocity) < 200)
                {
                    rb.angularVelocity = direction * 200;
                }

                if (Mathf.Abs(rb.angularVelocity) > maxRotationSpeed)
                {
                    rb.angularVelocity = Mathf.Sign(rb.angularVelocity) * maxRotationSpeed;
                }
            }
        }
    }
    public IEnumerator ReturnToStartPosition(float duration)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        rb.isKinematic = true;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 1; 

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPosition, _initialPosition, t);
            transform.rotation = Quaternion.Lerp(startRotation, Quaternion.identity, t);

            yield return null;
        }

        transform.position = _initialPosition;
        transform.rotation = Quaternion.identity;

        rb.isKinematic = true;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0;

        rotationSpeed = 0;
        canRotate = false;
        throwBegan = false;
        RestartScene();
    }

    private IEnumerator DelayedRestart(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(ReturnToStartPosition(1));
        RestartScene();
    }


    private IEnumerator ResetAfterLoss()
    {
        yield return new WaitForSeconds(1f);

        youWinPopUp.SetActive(false);
        betButton.SetActive(true);
        betPlaced = false;
        gameOver = false;

        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;


        ResetForNextThrow();
    }
    private IEnumerator ShakeInputField()
    {
        float duration = 0.4f;
        float elapsed = 0f;

        float maxRotationOffset = 5f;

        Quaternion originalRotation = betInputfield.transform.rotation;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float dampening = 1 - (progress * progress);

            float rotationZ = UnityEngine.Random.Range(-maxRotationOffset, maxRotationOffset) * dampening;
            Quaternion rotation = Quaternion.Euler(0, 0, rotationZ);

            betInputfield.transform.rotation = originalRotation * rotation;

            elapsed += Time.deltaTime;
            yield return null;
        }

        betInputfield.transform.rotation = Quaternion.identity;
    }



    private IEnumerator ShakeObjects()
    {
        if (shakeObjects == null || shakeObjects.Length == 0) yield break;

        float elapsed = 0f;
        float duration = 0.5f;
        float rotationIntensity = 10f;

        List<Quaternion> originalRotations = new List<Quaternion>();

        foreach (var obj in shakeObjects)
        {
            if (obj != null)
            {
                originalRotations.Add(obj.transform.rotation);
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            for (int i = 0; i < shakeObjects.Length; i++)
            {
                if (shakeObjects[i] == null) continue;

                Quaternion shakeRotation = Quaternion.Euler(
                    0, 0, UnityEngine.Random.Range(-rotationIntensity, rotationIntensity)
                );

                shakeObjects[i].transform.rotation = originalRotations[i] * shakeRotation;
            }
            yield return null;
        }
         
        for (int i = 0; i < shakeObjects.Length; i++)
        {
            if (shakeObjects[i] != null)
            {
                shakeObjects[i].transform.rotation = Quaternion.identity;
            }
        }
    }



    public void DisableAllObjectsInArray()
    {
        if (shakeObjects == null || shakeObjects.Length == 0) return;

        foreach (GameObject obj in shakeObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }
    public void OnBetButtonClicked()
    {
        if (string.IsNullOrEmpty(betInputfield.text))
        {
            betInputfield.text = MIN_BET.ToString();
        }

        if (!int.TryParse(betInputfield.text, out int betValue))
        {
            if (errorSound != null) Instantiate(errorSound);
            StartCoroutine(ShakeInputField());
            return;
        }

        // Проверяем, что apiManager не null
        if (apiManager == null)
        {
            Debug.LogError("ApiManager is null! Trying to get instance...");
            apiManager = ApiManager.Instance;
            
            // Если всё ещё null, используем запасной вариант без проверки баланса
            if (apiManager == null)
            {
                Debug.LogError("ApiManager instance is still null! Proceeding without balance check.");
                PlaceBetWithoutBalanceCheck(betValue);
                return;
            }
        }

        // Сначала проверяем баланс на сервере перед размещением ставки
        StartCoroutine(apiManager.GetUserBalance((serverBalance) => {
            Debug.Log($"Server balance received: {serverBalance}. Bet amount required: {betValue}");

            if (serverBalance >= betValue)
            {
                Debug.Log("Sufficient balance confirmed by server. Proceeding with bet.");
                
                bool started = PlaceBet(betValue, () => {
                    // Запуск игры только после подтверждения ставки сервером
                    winChanceManager.CheckForDeviation();
                    currentMultiplier = winChanceManager.CurrentMultiplier;
                    float currentChance = winChanceManager.CurrentWinChance;

                    currentChance = Mathf.Clamp(
                        currentChance,
                        winChanceManager.minWinChance,
                        winChanceManager.maxWinChance
                    );

                    win = winChanceManager.CalculateWinResult();

                    scoreText.text = "";
                    scoreText.gameObject.SetActive(true);
                    roundTimer.StartNewRound();

                    transform.position = startPos.position;
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    rotationSpeed = 0;
                    bounceCount = 0;
                    jumpsCount = 0;
                    gameOver = false;
                });

                if (!started)
                {
                    // Опционально: обработка случая, если PlaceBet() вернул false сразу
                    if (errorSound != null) Instantiate(errorSound);
                    StartCoroutine(ShakeInputField());
                    StartCoroutine(ShakeObjects());
                }
            }
            else
            {
                Debug.LogWarning($"Insufficient server balance. Server has: {serverBalance}, Bet requires: {betValue}. Bet rejected.");
                if (errorSound != null) Instantiate(errorSound);
                StartCoroutine(ShakeInputField());
                StartCoroutine(ShakeObjects());
            }
        }));
    }


    private IEnumerator ThrowCooldownRoutine()
    {
        throwCooldown = true;
        yield return new WaitForSeconds(throwCooldownTime);
        throwCooldown = false;
    }

    private void ThrowBottle(float normalizedSwipe)
    {
        canBet = true;

        if (SwipeNo)
        {
            if (swipeSound != null) Instantiate(swipeSound);

            startStable = false;
            winChanceManager.GeneratePredefinedResult();

            targetMultiplier = winChanceManager.CurrentMultiplier;
            if (multiplierAnimationCoroutine != null)
            {
                StopCoroutine(multiplierAnimationCoroutine);
            }
            multiplierAnimationCoroutine = StartCoroutine(AnimateMultiplier());

            const float fixedTorque = 15f;
            const float fixedVerticalForce = 90f; 
            const float fixedHorizontalForce = 8f;

            Vector3 bottleScreenPos = mainCamera.WorldToScreenPoint(transform.position);

            if (!_directionLocked)
            {
                _initialTorqueDirection = startTouchPosition.x < bottleScreenPos.x ? -1f : 1f;
                _directionLocked = true;
            }

            float torque = fixedTorque * _initialTorqueDirection;
            float verticalForce = fixedVerticalForce ;
            float horizontalForce = fixedHorizontalForce * horizontalMultiplier * -_initialTorqueDirection;

            rb.AddTorque(torque, ForceMode2D.Impulse);
            rb.AddForce(new Vector2(horizontalForce, verticalForce), ForceMode2D.Impulse);
            if (coins >= StartCoinsss)
            {
                HandleLoss("");
            }
            throwBegan = true;
            isStabilized = false;
            StartCoroutine(ThrowCooldownRoutine());
        }
    }




    private IEnumerator RampRotationSpeed()
    {
        float elapsed = 0f;
        float startSpeed = currentRotationSpeed;

        while (elapsed < rotationRampDuration)
        {
            elapsed += Time.deltaTime;
            currentRotationSpeed = Mathf.Lerp(startSpeed, targetRotationSpeed, elapsed / rotationRampDuration);
            yield return null;
        }

        currentRotationSpeed = targetRotationSpeed;
        isSpeedRamping = false;
    }
    private Vector2 CalculateThrowForce(float normalizedSwipe)
    {
        return new Vector2(
            Mathf.Lerp(minThrowForce * Time.deltaTime, maxThrowForce * Time.deltaTime, normalizedSwipe) * horizontalMultiplier,
            Mathf.Lerp(minThrowForce * Time.deltaTime, maxThrowForce * Time.deltaTime, normalizedSwipe) 
        );
    }
    private float CalculateTorque(float normalizedSwipe)
    {
        return Mathf.Lerp(minTorque, maxTorque, normalizedSwipe) *
               Mathf.Sign(UnityEngine.Random.Range(-1f, 1f));
    }
    private IEnumerator ResetRotationBoost()
    {
        yield return new WaitForSeconds(boostDuration);
        rotationSpeed /= initialRotationBoost;
    }

    private void ApplyAdditionalBoost()
    {
        if (gameOver || jumpsCount >= maxAirJumps || win || !inAirThrowWindow) return;

        Vector2 endTouchPosition = Input.mousePosition;
        float swipeDistance = (endTouchPosition - startTouchPosition).magnitude * 1.9f;

        if (swipeDistance < 700) return;

        float direction = _directionLocked ? _initialTorqueDirection : 1f;

        float holdMultiplier = Mathf.Lerp(minHoldMultiplier, maxHoldMultiplier,
            Mathf.Clamp01(holdDuration / maxHoldTime));

        float swipeMultiplier = Mathf.Clamp01(swipeDistance / 1000f);
        float totalForceMultiplier = holdMultiplier * (1 + swipeMultiplier);

        float newThrowForce = (swipeDistance / 15f + throwForceBonus) * totalForceMultiplier * 2f;
        float newRotationSpeed = Mathf.Max(500f, 700f - (swipeDistance / 1500f)) * initialRotationBoost;

        rotationSpeed = Mathf.Abs(newRotationSpeed * 1) * direction;
        rb.velocity = new Vector2(0f, newThrowForce * airThrowForceMultiplier);

        StartCoroutine(ResetRotationBoost());
        canRotate = true;
        winTimer = 0;
        timer = 0;

        float multiplierIncrement = UnityEngine.Random.Range(airRotationMultiplierMin, airRotationMultiplierMax);
        targetMultiplier += multiplierIncrement;
        targetMultiplier = Mathf.Round(targetMultiplier * 100) / 100;

        if (multiplierAnimationCoroutine != null)
            StopCoroutine(multiplierAnimationCoroutine);

        multiplierAnimationCoroutine = StartCoroutine(AnimateMultiplier());
        currentCash = Mathf.Floor(bet * targetMultiplier);
        currentCashText.text = currentDisplayedCash.ToString("F2");


        jumpsCount++;
        StartCoroutine(ResetBoostCooldown());

        holdDuration = 0f;
        holdStartTime = 0f;

        lastSwipeTime = Time.time;
        StartCoroutine(AirThrowWindowCheck());
    }


    private IEnumerator ResetBoostCooldown()
    {
        canBoost = false;
        yield return new WaitForSeconds(0.3f);
        canBoost = true;
    }


    private void UpdateRTPBalance()
    {
        float expectedRTP = (winChanceManager.CurrentWinChance / 100f) * currentMultiplier;

        if (expectedRTP < 0.9f)
        {
            winChanceManager.AdjustChance(true);
        }
        else if (expectedRTP > 1.1f)
        {
            winChanceManager.AdjustChance(false);
        }
    }
    private void OnInputFieldSelected(string text)
    {
        if (inputClickSound != null) Instantiate(inputClickSound);
    }

    private void StopVibration()
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
#endif
    }
    private void iOSVibrate(float duration)
    {
#if UNITY_IOS
    if (Device.hapticSupportSupported)
    {
        HapticFeedback.Generate(
            duration > 0.2f ? HapticFeedback.HapticFeedbackType.Success :
            duration > 0.1f ? HapticFeedback.HapticFeedbackType.Warning :
            HapticFeedback.HapticFeedbackType.Failure
        );
    }
    else
    {
        Handheld.Vibrate();
    }
#endif
    }

    private void ExecuteWebVibration(float duration)
    {
        try
        {
            string vibrationCommand = $@"
            if (navigator.vibrate) {{
                navigator.vibrate({duration * 1000});
            }}";
            Application.ExternalEval(vibrationCommand);
        }
        catch
        { }
    }
    public void AddMultiplier()
    {
        if (gameOver) return;

        float oldMultiplier = currentMultiplier;

        winChanceManager.GenerateNewMultiplier();

        float baseMultiplier = Mathf.Max(1.1f, winChanceManager.CurrentMultiplier);
        float rotationBonus = fullRotations * 1.2f;

        float newMultiplier = Mathf.Max(
            oldMultiplier * 1.05f,  
            baseMultiplier + rotationBonus
        );

        // Убираем ограничение на множитель, используя только minMultiplier
        currentMultiplier = Mathf.Max(newMultiplier, winChanceManager.minMultiplier);

        if (winChanceManager != null)
        {
            winChanceManager.DecreaseWinChancePerRotation();
        }

        if (multiplierAnimationCoroutine != null)
        {
            StopCoroutine(multiplierAnimationCoroutine);
        }

        targetMultiplier = currentMultiplier;
        multiplierAnimationCoroutine = StartCoroutine(AnimateMultiplier());

        currentCash = Mathf.Floor(bet * currentMultiplier * 100) / 100; 
        AnimateCashChange(currentCash);

        if (currentMultiplier > oldMultiplier)
        {
            scoreText.GetComponent<Animator>().SetTrigger("score");
        }
    }

    private IEnumerator AnimateMultiplier()
    {
        float startValue = animatedMultiplier;
        float endValue = targetMultiplier;
        int lastInteger = Mathf.FloorToInt(startValue);

        if (Mathf.Approximately(startValue, endValue))
        {
            scoreText.transform.localScale = originalCoinScale;
            yield break;
        }

        float duration = 0.5f;
        float elapsed = 0f;

        Vector3 originalScale = originalCoinScale; 
        Color startColor = scoreText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            animatedMultiplier = Mathf.Lerp(startValue, endValue, t);

            // Улучшенная анимация цвета
            Color targetColor = GetColorForMultiplier(animatedMultiplier);
            scoreText.color = Color.Lerp(startColor, targetColor, t);

            // Улучшенная анимация масштаба
            float scaleFactor = Mathf.Lerp(1f, coinScaleAmount, Mathf.SmoothStep(0, 1, Mathf.PingPong(t * 2, 1)));
            scoreText.transform.localScale = originalScale * scaleFactor;

            scoreText.text = "x" + (Mathf.Floor(animatedMultiplier * 100) / 100).ToString("F2");

            int currentInteger = Mathf.FloorToInt(animatedMultiplier);
            if (currentInteger > lastInteger)
            {
                lastInteger = currentInteger;
            }

            yield return null;
        }

        // Устанавливаем финальные значения
        scoreText.transform.localScale = originalScale;
        scoreText.text = "x" + endValue.ToString("F2");
        scoreText.color = GetColorForMultiplier(endValue);
        animatedMultiplier = endValue;
    }

    private Color GetColorForMultiplier(float multiplier)
    {
        if (multiplierColors.Length == 0) return Color.white;

        if (multiplier >= multiplierColors[multiplierColors.Length - 1].threshold)
            return multiplierColors[multiplierColors.Length - 1].color;

        for (int i = 0; i < multiplierColors.Length - 1; i++)
        {
            if (multiplier >= multiplierColors[i].threshold &&
                multiplier < multiplierColors[i + 1].threshold)
            {
                float t = (multiplier - multiplierColors[i].threshold) /
                         (multiplierColors[i + 1].threshold - multiplierColors[i].threshold);
                return Color.Lerp(
                    multiplierColors[i].color,
                    multiplierColors[i + 1].color,
                    Mathf.Clamp01(t)
                );
            }
        }

        return multiplierColors[0].color;
    }

    private void RotateForLanding()
    {
        if (win && !isGrounded && transform.position.y < landingRotationThreshold)
        {
            rb.angularVelocity = Mathf.Lerp(rb.angularVelocity, 0f, Time.deltaTime * 2f);
        }
    }

    public void ResetForNextThrow()
    {
        _currentRotationSpeed = 0f;
        _firstThrow = true;
        initialBottleAngle = NormalizeAngle(transform.eulerAngles.z);
        hasTouchedGround = false;
        transform.position = startPos.position;
        scoreText.color = GetColorForMultiplier(1f);
        throwBegan = false;
        canRotate = false;
        timer = 0;
        winTimer = 0;
        rotationSpeed = 0;
        rb.velocity = Vector2.zero;
        rb.sharedMaterial = defaultMaterial;

        _directionLocked = false;
        _initialTorqueDirection = 1f;

        transform.rotation = Quaternion.identity;
        gameOver = false;
        rewardGiven = false;
        multiplierLocked = false;

        scoreText.text = "x1.00";
        scoreText.gameObject.SetActive(false);

        // Скрываем все тексты выигрышей
        HideAllWinTexts();

        bounceCount = 0;
        rb.drag = airResistance;
        hasExitedZone = false;
        win = false;

        if (winChanceManager != null)
        {
            winChanceManager.GenerateNewMultiplier();
            winChanceManager.AdjustChanceAfterThrow(win, bet);
        }
    }

    public void OnClaimButtonClicked()
    {
        if (autoClaimCoroutine != null)
        {
            StopCoroutine(autoClaimCoroutine);
            autoClaimCoroutine = null;
        }

        ClaimReward();
        betButton.SetActive(true);
        betPlaced = false;
    }

    private IEnumerator SmoothFall(float targetAngle)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector2 startVelocity = rb.velocity;
        float startAngular = rb.angularVelocity;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rb.velocity = Vector2.Lerp(startVelocity, Vector2.zero, t);
            rb.angularVelocity = Mathf.Lerp(startAngular, 0f, t);
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);

            yield return null;
        }

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.rotation = targetRotation;
        gameOver = true;
    }


    private void HandleSkyboxCollision(Collision2D collision)
    {
        if (!gameOver)
        {
            HandleLoss("FAILED");
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        ApplyBounceEffects(collision);
        if (throwBegan == true)
        {
            SwipeNo = false;
            Debug.Log(SwipeNo);
        }

        CanGround = true;
        if (collision.gameObject.CompareTag("Ground") && IsBottleUpright())
        {
            if (landingSound != null) Instantiate(landingSound);
        }
        if ((skyboxLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            HandleSkyboxCollision(collision);
            return;
        }

        if (collision.gameObject.CompareTag("Ground") && !gameOver)
        {
            if (firstthrow == true)
            {
                InTrigger = true;
                StartCoroutine(CheckLandingResult());
            }

        }

    }


    public void HandleWin()
    {
        if (isGameEnding) return;
        isGameEnding = true;

        if (CanGround == false)
        {
            StartCoroutine(HandleLoss(""));
        }
        else
        {
            if (canBet == true)
            {
                lastBet = bet;
                lastBet = PlayerPrefs.GetFloat("LastBet", 0);
                if (gameOver) return;

                StartCoroutine(AlignBottleAndWin());
            }
        }
    }

    private IEnumerator AlignBottleAndWin()
    {
        rb.isKinematic = true;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Показываем соответствующий текст в зависимости от множителя
        HideAllWinTexts(); // Скрываем все тексты перед показом нужного
        
        if (currentMultiplier >= 10)
        {
            if (currentMultiplier > 10 && jackpotText != null)
            {
                jackpotText.SetActive(true);
                yield return StartCoroutine(AnimateWinText(jackpotText));
            }
            else if (megaWinText != null)
            {
                megaWinText.SetActive(true);
                yield return StartCoroutine(AnimateWinText(megaWinText));
            }
        }
        else if (currentMultiplier >= 5 && bigWinText != null)
        {
            bigWinText.SetActive(true);
            yield return StartCoroutine(AnimateWinText(bigWinText));
        }
        else if (currentMultiplier >= 3 && niceWinText != null)
        {
            niceWinText.SetActive(true);
            yield return StartCoroutine(AnimateWinText(niceWinText));
        }

        EffectWin.SetActive(true);
        DarkBG.SetActive(true);
        AnimateCoinsChange(coins);
        AnimateCashChange(currentCash);

        if (currentMultiplier <= 4f)
        {
            if (winLowSound != null) Instantiate(winLowSound);
        }
        else
        {
            if (winHighSound != null) Instantiate(winHighSound);
        }
        win = true;
        gameOver = true;
        canRotate = false;
        winChanceManager.AdjustChanceAfterThrow(true, bet);

        currentCash = bet * currentMultiplier;
        ClaimReward();
        youWinPopUp.SetActive(true);

        // Отправляем результат игры в API для записи в историю
        StartCoroutine(apiManager.SendGameRoundResult(Mathf.RoundToInt(bet), true, (success) => {
            if (!success) {
                Debug.LogError("Не удалось отправить результат выигрыша в API");
            }
        }));

        TextMeshProUGUI winText = youWinPopUp.GetComponent<TextMeshProUGUI>();
        if (winText != null)
        {
            winText.color = Color.white;
            winText.enableVertexGradient = true;
            winText.colorGradient = new VertexGradient(
                new Color(1f, 0.882f, 0.556f),
                new Color(1f, 0.882f, 0.556f),
                new Color(1f, 0.713f, 0.216f),
                new Color(1f, 0.713f, 0.216f)
            );
            winText.text = "YOU WIN!";
            winText.ForceMeshUpdate();
        }
        yield return StartCoroutine(AnimatePopupJump("YOU WIN!", new VertexGradient(
      new Color(1f, 0.882f, 0.556f),
      new Color(1f, 0.882f, 0.556f),
      new Color(1f, 0.713f, 0.216f),
      new Color(1f, 0.713f, 0.216f)
  )));

        StopAllCoroutines();
        StartCoroutine(ReturnToStartPosition(1));
    }

    // Скрыть все тексты выигрышей
    public void HideAllWinTexts()
    {
        if (niceWinText != null) niceWinText.SetActive(false);
        if (bigWinText != null) bigWinText.SetActive(false);
        if (megaWinText != null) megaWinText.SetActive(false);
        if (jackpotText != null) jackpotText.SetActive(false);
    }

    // Анимация текста выигрыша
    public IEnumerator AnimateWinText(GameObject winTextObject)
    {
        // Сохраняем оригинальный размер
        Vector3 originalScale = winTextObject.transform.localScale;
        
        // Начинаем с нулевого размера
        winTextObject.transform.localScale = Vector3.zero;
        
        // Увеличиваем размер
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            winTextObject.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale * 1.2f, t);
            yield return null;
        }
        
        // Немного уменьшаем до нормального размера
        elapsed = 0f;
        duration = 0.2f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            winTextObject.transform.localScale = Vector3.Lerp(originalScale * 1.2f, originalScale, t);
            yield return null;
        }
        
        // Отображаем текст некоторое время
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator ConfirmWinWithPriorityCheck()
    {
        yield return new WaitForSeconds(0.3f);

        float finalCheckAngle = NormalizeAngle(transform.eulerAngles.z);
        bool finalUpright = finalCheckAngle <= 15f || finalCheckAngle >= 345f;
        bool finalStable = rb.velocity.magnitude < 0.5f
                         && Mathf.Abs(rb.angularVelocity) < 20f;

        if (finalUpright && finalStable && !gameOver)
        {
            win = true;
            gameOver = true;

            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;

            youWinPopUp.SetActive(true);
            currentCash = Mathf.Round(bet * targetMultiplier * 100) / 100;
        }
        else
        {
            HandleLoss("Win verification failed");
            PlayerPrefs.SetFloat("LastBet", lastBet);
            PlayerPrefs.Save();
        }
    }
    public IEnumerator HandleLoss(string reason)
    {
        if (isGameEnding) yield break;
        isGameEnding = true;
        if (canBet == true)
        {
            if (gameOver) yield break;

            if (loseSound != null) Instantiate(loseSound);

            sidewaysTimer = 0f;

            lastBet = bet;
            lastBet = PlayerPrefs.GetFloat("LastBet", 0);
            float finalAngle = NormalizeAngle(transform.eulerAngles.z);
            bool lastChanceWin = finalAngle <= 15f || finalAngle >= 345f;

            if (lastChanceWin && rb.velocity.magnitude < 0.5f)
            {
                HandleWin();
                yield break;
            }

            yield return null;

            win = false;
            gameOver = true;

            // Отправляем результат игры в API для записи в историю
            StartCoroutine(apiManager.SendGameRoundResult(Mathf.RoundToInt(bet), false, (success) => {
                if (!success) {
                    Debug.LogError("Не удалось отправить результат проигрыша в API");
                }
            }));

            DarkBG.SetActive(true); 
            youWinPopUp.SetActive(true);
            TextMeshProUGUI lossText = youWinPopUp.GetComponent<TextMeshProUGUI>();

            if (lossText != null)
            {
                lossText.enableVertexGradient = true;
                lossText.color = Color.white; 

                lossText.colorGradient = new VertexGradient(
                    new Color(1f, 0.713f, 0.216f),    //  (#FFB637)
                    new Color(1f, 0.713f, 0.216f),    //  (правый)
                    new Color(1f, 0.145f, 0.259f),    //  (#FF2542)
                    new Color(1f, 0.145f, 0.259f)     //  (правый)
                );

                lossText.text = "FAILED";
                lossText.ForceMeshUpdate(); 
            }
            yield return StartCoroutine(AnimatePopupJump("FAILED", new VertexGradient(
    new Color(1f, 0.713f, 0.216f),
    new Color(1f, 0.713f, 0.216f),
    new Color(1f, 0.145f, 0.259f),
    new Color(1f, 0.145f, 0.259f)
)));


            StopAllCoroutines();
            StartCoroutine(ReturnToStartPosition(1));
            yield return new WaitForSeconds(1);

        }
    }
    public IEnumerator AnimatePopupJump(string text, VertexGradient gradient)
    {
        youWinPopUp.SetActive(true);
        TextMeshProUGUI popupText = youWinPopUp.GetComponent<TextMeshProUGUI>();

        if (popupText != null)
        {
            popupText.text = text;
            popupText.enableVertexGradient = true;
            popupText.colorGradient = gradient;
            popupText.ForceMeshUpdate();
        }

        // Анимация увеличения
        float elapsed = 0f;
        while (elapsed < popupJumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = popupJumpCurve.Evaluate(elapsed / popupJumpDuration);
            youWinPopUp.transform.localScale = originalPopupScale * Mathf.Lerp(0f, popupJumpScale, t);
            yield return null;
        }

        // Анимация уменьшения до нормального размера
        elapsed = 0f;
        while (elapsed < popupJumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = popupJumpCurve.Evaluate(elapsed / popupJumpDuration);
            youWinPopUp.transform.localScale = originalPopupScale * Mathf.Lerp(popupJumpScale, 1f, t);
            yield return null;
        }

        youWinPopUp.transform.localScale = originalPopupScale;
    }

    private IEnumerator ShowWinAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (isGameEnding) yield break;
        isGameEnding = true;
        win = true;
        gameOver = true;
        canRotate = false;
        winChanceManager.AdjustChanceAfterThrow(true, bet);

        currentCash = Mathf.Round(bet * targetMultiplier * 100) / 100;
        ClaimReward();

        youWinPopUp.SetActive(true);

        TextMeshProUGUI winText = youWinPopUp.GetComponent<TextMeshProUGUI>();

        if (winText != null)
        {
            winText.color = Color.white; 
            winText.enableVertexGradient = true;

            winText.colorGradient = new VertexGradient(
                new Color(1f, 0.882f, 0.556f), 
                new Color(1f, 0.882f, 0.556f), 
                new Color(1f, 0.713f, 0.216f), 
                new Color(1f, 0.713f, 0.216f)  
            );

            winText.text = "YOU WIN!";
            winText.ForceMeshUpdate(); 
        }

        DarkBG.SetActive(true);
        yield return null;
    }

    private IEnumerator ShowLossAfterDelay(string reason, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (isGameEnding) yield break;
        isGameEnding = true;
        win = false;
        gameOver = true;
        canRotate = false;
        rb.velocity = Vector2.zero;

        DarkBG.SetActive(true); 
        youWinPopUp.SetActive(true);
        TextMeshProUGUI lossText = youWinPopUp.GetComponent<TextMeshProUGUI>();
        lossText.colorGradient = new VertexGradient(
            new Color(1f, 0.713f, 0.216f),    //  #FFB637
            new Color(1f, 0.713f, 0.216f),    //  #FFB637 
            new Color(1f, 0.145f, 0.259f),    //  #FF2542
            new Color(1f, 0.145f, 0.259f)     //  #FF2542
        );
        youWinPopUp.GetComponent<TextMeshProUGUI>().text = "FAILED";


        StartCoroutine(DelayedRestart(2f));
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        CanGround = false;
    }
    private bool IsBottleUpright()
    {
        float currentAngle = NormalizeAngle(transform.localEulerAngles.z);
        return currentAngle <= 35f || currentAngle >= 335f;
    }
    public float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0) angle += 360f;
        return angle;
    }


    private bool CheckStability()
    {
        float currentAngle = NormalizeAngle(transform.localEulerAngles.z);
        bool isUpright = currentAngle <= 35f || currentAngle >= 325f;

        bool isVelocityLow = rb.velocity.magnitude < 0.8f;
        bool isRotationSlow = Mathf.Abs(rb.angularVelocity) < 60f;

        return isUpright && isVelocityLow && isRotationSlow;
    }
    private bool IsWithinAngleThreshold()
    {
        float currentAngle = NormalizeAngle(transform.eulerAngles.z);
        float minAngle = NormalizeAngle(initialBottleAngle - AngleThreshold);
        float maxAngle = NormalizeAngle(initialBottleAngle + AngleThreshold);

        if (minAngle > maxAngle)
        {
            return currentAngle >= minAngle || currentAngle <= maxAngle;
        }

        return currentAngle >= minAngle && currentAngle <= maxAngle;
    }
    private IEnumerator CheckLandingResult()
    {

        yield return new WaitForSeconds(2f);

        if (gameOver) yield break;
        autoRestartPending = ToggleAutoStart.isOn;

        lastBet = bet;
        PlayerPrefs.SetFloat("LastBet", lastBet);
        PlayerPrefs.Save();

        Vector2 currentVelocity = rb.velocity;
        float currentAngularVelocity = rb.angularVelocity;
        float currentAngle = NormalizeAngle(transform.eulerAngles.z);

        bool isUpright = currentAngle <= 10f || currentAngle >= 350f;
        bool isStable = currentVelocity.magnitude < 0.5f
                        && Mathf.Abs(currentAngularVelocity) < 30f;

        if (IsUpsideDown())
        {
            StartCoroutine(HandleLoss("Bottle landed upside down!"));
            yield break;

        }

        if (isUpright && isStable)
        {
            StartCoroutine(Stabilization());
            HandleWin();
            AnimateCoinsChange(coins);
            lastBet = bet;
            PlayerPrefs.SetFloat("LastBet", lastBet);
            PlayerPrefs.Save();
        }
        else
        {
            bool isSideways = (currentAngle > 75f && currentAngle < 105f)
                               || (currentAngle > 255f && currentAngle < 285f);
            string lossReason = isSideways ? "Bottle landed sideways!"
                                           : "Bottle didn't stabilize!";
            lastBet = bet;
            PlayerPrefs.SetFloat("LastBet", lastBet);
            PlayerPrefs.Save();
            StartCoroutine(HandleLoss(""));
        }
    }

    public IEnumerator Stabilization()
    {
        float duration = 0;
        float time = 0.2f;

        while ( duration > time) { 


            time += Time.deltaTime;
       
        }

        yield return null;  
    }  
    
    private void CheckWinImmediately()
    {
        float currentAngle = transform.localEulerAngles.z % 360;
        bool isWinPosition = (currentAngle >= 350f || currentAngle <= 10f);

        if (isWinPosition)
        {
            HandleWin();
        }
    }
    private bool IsUpsideDown()
    {
        float angle = NormalizeAngle(transform.eulerAngles.z);
        return angle > 176f && angle < 183f;
    }
    private float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }
    private void OnApplicationQuit()
    {
        // Сброс автостарта при выходе
        // ToggleAutoStart.isOn = false; // Не обязательно, т.к. объект уничтожится
        PlayerPrefs.SetInt("AutoStart", 0); // Сбрасываем настройку автостарта

        // PlayerPrefs.SetFloat("coins", coins); // Удалено - баланс на сервере
        PlayerPrefs.SetFloat("LastBet", lastBet); // Сохраняем последнюю ставку
        PlayerPrefs.Save(); // Сохраняем PlayerPrefs
    }

    /*   private void TriggerVibrationForWeb(float duration)
       {
           if (!vibrationsEnabled)
               return;

           string vibrationCommand = $@"
       vibrate({duration * 1000});
       ";
           Application.ExternalEval(vibraыtionCommand);
       }
    */
    private IEnumerator CheckFinalResult(bool isWinPosition)
    {
        yield return new WaitForSeconds(5f);

        float stabilizedAngle = transform.localEulerAngles.z % 360;
        bool stabilizedWin = (stabilizedAngle >= 350f || stabilizedAngle <= 5f);

        if (stabilizedWin && isWinPosition)
        {
            HandleWin();
        }
        else
        {
            StartCoroutine(CheckLandingResult());
        }
    }

    // Новый метод для запасного варианта
    private void PlaceBetWithoutBalanceCheck(int betValue)
    {
        Debug.LogWarning("Using fallback bet placement without balance check!");
        
        bool started = PlaceBet(betValue, () => {
            // Запуск игры только после подтверждения ставки сервером
            winChanceManager.CheckForDeviation();
            currentMultiplier = winChanceManager.CurrentMultiplier;
            float currentChance = winChanceManager.CurrentWinChance;

            currentChance = Mathf.Clamp(
                currentChance,
                winChanceManager.minWinChance,
                winChanceManager.maxWinChance
            );

            win = winChanceManager.CalculateWinResult();

            scoreText.text = "";
            scoreText.gameObject.SetActive(true);
            roundTimer.StartNewRound();

            transform.position = startPos.position;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rotationSpeed = 0;
            bounceCount = 0;
            jumpsCount = 0;
            gameOver = false;
        });

        if (!started)
        {
            // Опционально: обработка случая, если PlaceBet() вернул false сразу
            if (errorSound != null) Instantiate(errorSound);
            StartCoroutine(ShakeInputField());
            StartCoroutine(ShakeObjects());
        }
    }
}