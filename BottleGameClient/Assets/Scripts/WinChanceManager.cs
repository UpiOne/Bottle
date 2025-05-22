using System.Collections;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class WinChanceManager : MonoBehaviour
{
    [Header("Base Chance Settings")]
    [SerializeField][Range(0, 100)] public float baseWinChance;
    [Range(0, 100)] public float minWinChance;
    [Range(0, 100)] public float maxWinChance;

    [Header("Dynamic Adjustments")]
    [Range(0.1f, 100f)] public float chanceDecreasePerThrowMin = 10f;
    [Range(0.1f, 100f)] public float chanceDecreasePerThrowMax = 50f;
    [Range(0.01f, 20f)] public float chanceDecreasePerRotation = 5f;
    [Range(0, 50)] public float minRotationWinChance = 15f;

    [Header("Multiplier Settings")]
    [Min(1f)] public float minMultiplier = 1;
    [Min(1f)] public float maxMultiplier = 3;
    public float minAirJumpMultiplier = 0.1f;
    public float maxAirJumpMultiplier = 0.5f;
    [SerializeField] private float baseMultiplierProbability = 0.65f; // 65% шанс для >1.2x
    [SerializeField] private AnimationCurve multiplierProbabilityCurve;
    [SerializeField] private float probabilityFor1_5 = 0.40f; // 40% шанс для >1.5x
    [SerializeField] private float probabilityFor2 = 0.25f; // 25% шанс для >2x
    [SerializeField] private float probabilityFor3 = 0.05f; // 5% шанс для >3x
    [SerializeField] private float probabilityFor5 = 0.03f; // 3% шанс для >5x
    [SerializeField] private float probabilityFor10 = 0.01f; // 1% шанс для >10x
    
    [Header("Balance Control")]
    [Range(0.1f, 0.15f)] public float initialBalanceLoss = 0.12f;
    [Min(0.1f)] public float balanceSmoothness = 2f;
    [Range(0f, 1f)] public float deviationChance = 0.15f;
    [Range(0.5f, 0.9f)] public float maxDeviationPercent = 0.9f;
    [Min(1f)] public float recoveryBoostMultiplier = 2f;
    [SerializeField] public AnimationCurve winChanceCurve;

    [Header("Win Boost Mechanism")]
    [SerializeField][Min(0)] public int minGamesForBoost = 25;
    [Min(0)] public int maxGamesForBoost = 60;
    [Range(0f, 1f)] public float winBoostChance = 0.60f;
    [Min(1f)] public float winBoostMultiplier = 1.30f;

    [Header("Hard Reset Chance")]
    [SerializeField][Range(0f, 1f)] public float hardResetChance = 0.07f;
    [Min(0f)] public float controlGoal = 30;

    [Header("Slot Simulation Settings")]
    [SerializeField] public List<int> virtualReelStrip = new List<int> { 70, 65, 60, 55, 50, 45, 40 };
    [SerializeField] public int currentReelIndex;
    [SerializeField] public float reelAdvanceSpeed = 0.3f;
    [SerializeField] public bool forceWinNextThrow;

    [Header("Virtual Line Settings")]
    [SerializeField] public int minSpinsForWin = 3;
    [SerializeField] public float uprightThresholdForLine = 15f;
    [SerializeField] public float scatterZoneMultiplier = 2f;

    public bool _predeterminedWin;
    public float _calculatedMultiplier;
    public int _virtualLinesWon;

    public float currentGeneratedMultiplier;
    public float deviationMultiplier = 1f;
    public bool isDeviationActive;
    public int consecutiveLosses;
    public int totalGamesPlayed;
    public int nextBoostGame;
    public float groundTimeCounter;
    public bool isCheckingFinalResult;

    private ApiManager apiManager;
    private float currentPlayerBalance = 100f;
    private bool serverParametersLoaded = false;

    public float CurrentWinChance { get;  set; }
    public float CurrentMultiplier => currentGeneratedMultiplier;

    private void Start()
    {
        Debug.Log("[WinChanceManager] Инициализация WinChanceManager");
        
        // Инициализация значений по умолчанию для параметров множителей, если они не были заданы в инспекторе
        if (probabilityFor10 <= 0) probabilityFor10 = 0.01f;         // 1% шанс
        if (probabilityFor5 <= 0) probabilityFor5 = 0.03f;           // 3% шанс
        if (probabilityFor3 <= 0) probabilityFor3 = 0.05f;           // 5% шанс
        if (probabilityFor2 <= 0) probabilityFor2 = 0.25f;           // 25% шанс
        if (probabilityFor1_5 <= 0) probabilityFor1_5 = 0.40f;       // 40% шанс
        if (baseMultiplierProbability <= 0) baseMultiplierProbability = 0.65f; // 65% шанс
        
        ConfigureProbabilityCurve();
        
        // Устанавливаем начальное значение шанса выигрыша
        CurrentWinChance = baseWinChance;
        Debug.Log($"[WinChanceManager] Начальный шанс выигрыша (локальный): {CurrentWinChance}");
        
        GenerateNewMultiplier();
        
        // Загружаем параметры игры с сервера
        LoadServerParameters();
        
        // Получаем баланс пользователя
        FetchBalanceAndCalculateTarget();
        
        nextBoostGame = UnityEngine.Random.Range(minGamesForBoost, maxGamesForBoost);
    }

    private void Awake()
    {
        apiManager = ApiManager.Instance;
        Debug.Log("[WinChanceManager] Awake: Получаем ссылку на ApiManager");
        
        // Подписываемся на событие обновления параметров игры
        if (apiManager != null)
        {
            apiManager.OnGameParamsUpdated += UpdateGameParameters;
            Debug.Log("[WinChanceManager] Awake: Подписались на обновление параметров игры");
        }
        else
        {
            Debug.LogWarning("[WinChanceManager] Awake: ApiManager не найден!");
        }
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Загружаем локальные настройки, они будут заменены серверными когда те будут получены
        LoadAllSettings();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Отписываемся от события
        if (apiManager != null)
        {
            apiManager.OnGameParamsUpdated -= UpdateGameParameters;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetReelState();
        GenerateNewMultiplier();
    }

    // Обновляем параметры игры из серверных данных
    private void UpdateGameParameters(GameParams gameParams)
    {
        if (gameParams == null)
        {
            Debug.LogWarning("[WinChanceManager] UpdateGameParameters: Получены пустые параметры игры");
            return;
        }
        
        Debug.Log("[WinChanceManager] ======= ПОЛУЧЕНЫ СЕРВЕРНЫЕ ПАРАМЕТРЫ ИГРЫ =======");
        Debug.Log($"[WinChanceManager] ID параметров: {gameParams.id}");
        
        // Устанавливаем базовые параметры
        Debug.Log($"[WinChanceManager] Обновляем baseWinChance: {baseWinChance} -> {gameParams.baseWinChance}");
        baseWinChance = gameParams.baseWinChance;
        
        Debug.Log($"[WinChanceManager] Обновляем minWinChance: {minWinChance} -> {gameParams.minWinChance}");
        minWinChance = gameParams.minWinChance;
        
        Debug.Log($"[WinChanceManager] Обновляем maxWinChance: {maxWinChance} -> {gameParams.maxWinChance}");
        maxWinChance = gameParams.maxWinChance;
        
        // Устанавливаем параметры динамических настроек
        Debug.Log($"[WinChanceManager] Обновляем chanceDecreasePerThrowMin: {chanceDecreasePerThrowMin} -> {gameParams.chanceDecPerThrowMin}");
        chanceDecreasePerThrowMin = gameParams.chanceDecPerThrowMin;
        
        Debug.Log($"[WinChanceManager] Обновляем chanceDecreasePerThrowMax: {chanceDecreasePerThrowMax} -> {gameParams.chanceDecPerThrowMax}");
        chanceDecreasePerThrowMax = gameParams.chanceDecPerThrowMax;
        
        Debug.Log($"[WinChanceManager] Обновляем chanceDecreasePerRotation: {chanceDecreasePerRotation} -> {gameParams.chanceDecPerRotation}");
        chanceDecreasePerRotation = gameParams.chanceDecPerRotation;
        
        Debug.Log($"[WinChanceManager] Обновляем minRotationWinChance: {minRotationWinChance} -> {gameParams.minRotationWinChance}");
        minRotationWinChance = gameParams.minRotationWinChance;
        
        // Устанавливаем параметры мультипликаторов
        Debug.Log($"[WinChanceManager] Обновляем minMultiplier: {minMultiplier} -> {gameParams.minMultiplier}");
        minMultiplier = gameParams.minMultiplier;
        
        Debug.Log($"[WinChanceManager] Обновляем maxMultiplier: {maxMultiplier} -> {gameParams.maxMultiplier}");
        maxMultiplier = gameParams.maxMultiplier;
        
        // Устанавливаем параметры контроля баланса
        Debug.Log($"[WinChanceManager] Обновляем initialBalanceLoss: {initialBalanceLoss} -> {gameParams.initialBalanceLoss}");
        initialBalanceLoss = gameParams.initialBalanceLoss;
        
        Debug.Log($"[WinChanceManager] Обновляем deviationChance: {deviationChance} -> {gameParams.deviationChance}");
        deviationChance = gameParams.deviationChance;
        
        Debug.Log($"[WinChanceManager] Обновляем maxDeviationPercent: {maxDeviationPercent} -> {gameParams.maxDeviationPercent}");
        maxDeviationPercent = gameParams.maxDeviationPercent;
        
        // Устанавливаем параметры механизма усиления выигрыша
        Debug.Log($"[WinChanceManager] Обновляем minGamesForBoost: {minGamesForBoost} -> {Mathf.RoundToInt(gameParams.minGamesForBoost)}");
        minGamesForBoost = Mathf.RoundToInt(gameParams.minGamesForBoost);
        
        Debug.Log($"[WinChanceManager] Обновляем winBoostChance: {winBoostChance} -> {gameParams.winBoostChance}");
        winBoostChance = gameParams.winBoostChance;
        
        Debug.Log($"[WinChanceManager] Обновляем winBoostMultiplier: {winBoostMultiplier} -> {gameParams.winBoostMultiplier}");
        winBoostMultiplier = gameParams.winBoostMultiplier;
        
        // Устанавливаем параметры hard reset
        Debug.Log($"[WinChanceManager] Обновляем hardResetChance: {hardResetChance} -> {gameParams.hardResetChance}");
        hardResetChance = gameParams.hardResetChance;
        
        Debug.Log($"[WinChanceManager] Обновляем controlGoal: {controlGoal} -> {gameParams.controlGoal}");
        controlGoal = gameParams.controlGoal;
        
        // Устанавливаем параметры слот-симуляции
        Debug.Log($"[WinChanceManager] Обновляем reelAdvanceSpeed: {reelAdvanceSpeed} -> {gameParams.reelSpeed}");
        reelAdvanceSpeed = gameParams.reelSpeed;
        
        Debug.Log($"[WinChanceManager] Обновляем uprightThresholdForLine: {uprightThresholdForLine} -> {gameParams.uprightThresholdForLine}");
        uprightThresholdForLine = gameParams.uprightThresholdForLine;

        // Обновляем параметры шансов множителей
        UpdateMultiplierParams(gameParams);
        
        Debug.Log("[WinChanceManager] ======= СЕРВЕРНЫЕ ПАРАМЕТРЫ ПРИМЕНЕНЫ =======");
        
        // Отмечаем, что серверные параметры загружены
        serverParametersLoaded = true;
        
        // После загрузки серверных параметров, получаем индивидуальный шанс выигрыша пользователя
        LoadPlayerWinChance();
    }

    // Метод для обновления параметров кривой вероятности без параметров с сервера
    private void UpdateProbabilityCurve()
    {
        multiplierProbabilityCurve = new AnimationCurve(
            new Keyframe(1.2f, baseMultiplierProbability),   // Шанс получить множитель >1.2x
            new Keyframe(1.5f, probabilityFor1_5),     // Шанс получить множитель >1.5x
            new Keyframe(2f, probabilityFor2),      // Шанс получить множитель >2x
            new Keyframe(3f, probabilityFor3),     // Шанс получить множитель >3x
            new Keyframe(5f, probabilityFor5),     // Шанс получить множитель >5x
            new Keyframe(10f, probabilityFor10)     // Шанс получить множитель >10x
        );

        multiplierProbabilityCurve.SmoothTangents(0, 0.5f);
        multiplierProbabilityCurve.SmoothTangents(1, 0.5f);
        multiplierProbabilityCurve.SmoothTangents(2, 0.5f);
        multiplierProbabilityCurve.SmoothTangents(3, 0.5f);
        multiplierProbabilityCurve.SmoothTangents(4, 0.5f);
        multiplierProbabilityCurve.SmoothTangents(5, 0.5f);
        
        Debug.Log("[WinChanceManager] Обновили кривую вероятностей с новыми параметрами");
    }
    
    // Загружаем индивидуальный шанс выигрыша пользователя
    private void LoadPlayerWinChance()
    {
        Debug.Log("[WinChanceManager] LoadPlayerWinChance: Загрузка индивидуального шанса выигрыша пользователя");
        
        if (apiManager != null && apiManager.IsReady())
        {
            float playerWinChance = apiManager.GetPlayerWinChance();
            Debug.Log($"[WinChanceManager] LoadPlayerWinChance: Получен playerWinChance = {playerWinChance}");
            
            if (playerWinChance > 0)
            {
                // Используем индивидуальный шанс выигрыша пользователя вместо базового
                Debug.Log($"[WinChanceManager] LoadPlayerWinChance: ПРИМЕНЯЕМ индивидуальный шанс выигрыша пользователя: {playerWinChance} (было: {CurrentWinChance})");
                CurrentWinChance = playerWinChance;
            }
            else
            {
                // Если индивидуальный шанс не установлен, используем базовый
                Debug.Log($"[WinChanceManager] LoadPlayerWinChance: Индивидуальный шанс выигрыша пользователя не установлен, используем базовый: {baseWinChance}");
                CurrentWinChance = baseWinChance;
            }
            
            // Ограничиваем шанс выигрыша в пределах минимального и максимального
            float beforeClamp = CurrentWinChance;
            CurrentWinChance = Mathf.Clamp(CurrentWinChance, minWinChance, maxWinChance);
            
            if (beforeClamp != CurrentWinChance) {
                Debug.Log($"[WinChanceManager] LoadPlayerWinChance: Шанс выигрыша был скорректирован в пределах min-max: {beforeClamp} -> {CurrentWinChance}");
            }
            
            Debug.Log($"[WinChanceManager] LoadPlayerWinChance: Итоговый шанс выигрыша установлен: {CurrentWinChance}");
        }
        else
        {
            Debug.LogWarning("[WinChanceManager] LoadPlayerWinChance: ApiManager не готов для получения индивидуального шанса выигрыша");
            // Используем базовый шанс выигрыша
            CurrentWinChance = Mathf.Clamp(baseWinChance, minWinChance, maxWinChance);
            Debug.Log($"[WinChanceManager] LoadPlayerWinChance: Установлен локальный шанс выигрыша: {CurrentWinChance}");
        }
    }
    
    // Загружаем параметры игры с сервера
    private void LoadServerParameters()
    {
        Debug.Log("[WinChanceManager] LoadServerParameters: Пытаемся загрузить параметры игры с сервера");
        
        if (apiManager != null && apiManager.IsReady())
        {
            Debug.Log("[WinChanceManager] LoadServerParameters: ApiManager готов, получаем параметры игры");
            GameParams gameParams = apiManager.GetCurrentGameParams();
            
            if (gameParams != null)
            {
                Debug.Log("[WinChanceManager] LoadServerParameters: Параметры игры получены от ApiManager");
                // Обновляем параметры из серверных данных
                UpdateGameParameters(gameParams);
            }
            else
            {
                Debug.LogWarning("[WinChanceManager] LoadServerParameters: Не удалось получить параметры игры из ApiManager (null)");
            }
        }
        else
        {
            Debug.LogWarning("[WinChanceManager] LoadServerParameters: ApiManager не готов для получения параметров игры");
        }
    }

    public void LoadAllSettings()
    {
        // Устанавливаем базовый шанс выигрыша (будет заменен на серверный, когда тот будет получен)
        CurrentWinChance = Mathf.Clamp(baseWinChance, minWinChance, maxWinChance);
        Debug.Log($"[WinChanceManager] LoadAllSettings: Установлен начальный шанс выигрыша: {CurrentWinChance} (локальный)");
        consecutiveLosses = Mathf.Max(0, consecutiveLosses);
    }

    private void FetchBalanceAndCalculateTarget()
    {
        Debug.Log("[WinChanceManager] FetchBalanceAndCalculateTarget: Получаем баланс пользователя");
        
        if (apiManager != null && apiManager.IsReady())
        {
            Debug.Log("[WinChanceManager] FetchBalanceAndCalculateTarget: ApiManager готов, запрашиваем баланс");
            StartCoroutine(apiManager.GetUserBalance((balance) => {
                currentPlayerBalance = balance;
                Debug.Log($"[WinChanceManager] FetchBalanceAndCalculateTarget: Получен баланс пользователя: {balance}");
                CalculateTargetBalance();
            }));
        }
        else
        {
            Debug.LogWarning("[WinChanceManager] FetchBalanceAndCalculateTarget: ApiManager не готов, используем локальный баланс");
            CalculateTargetBalance();
        }
    }
  
    public void ResetReelState()
    {
        forceWinNextThrow = false;
        _virtualLinesWon = 0;
        deviationMultiplier = 1f;
    }

    public float GetPredeterminedMultiplier()
    {
        if (!_predeterminedWin) return 1f;

        float baseMultiplier = _calculatedMultiplier;

        float bonus = _virtualLinesWon * 0.5f;

        float result = Mathf.Floor((baseMultiplier + bonus) * 100) / 100;
        return Mathf.Clamp(result, minMultiplier, maxMultiplier);
    }

    public void GeneratePredefinedResult()
    {
        currentReelIndex = (currentReelIndex + 1) % virtualReelStrip.Count;
        CurrentWinChance = virtualReelStrip[currentReelIndex];

        if (consecutiveLosses >= controlGoal)
        {
            forceWinNextThrow = true;
        }

        _calculatedMultiplier = Mathf.Lerp(minMultiplier, maxMultiplier,
            winChanceCurve.Evaluate(currentPlayerBalance / 100f));

        _predeterminedWin = forceWinNextThrow ||
            (UnityEngine.Random.value <= CurrentWinChance / 100f * deviationMultiplier);
    }

    public void GenerateNewMultiplier()
    {
        float randomValue = UnityEngine.Random.value;
        float multiplier;

        if (randomValue < probabilityFor10) // 1% шанс
        {
            multiplier = UnityEngine.Random.Range(10f, maxMultiplier);
        }
        else if (randomValue < probabilityFor5) // 3% шанс (включая предыдущий 1%)
        {
            multiplier = UnityEngine.Random.Range(5f, 10f);
        }
        else if (randomValue < probabilityFor3) // 5% шанс (включая предыдущие 3%)
        {
            multiplier = UnityEngine.Random.Range(3f, 5f);
        }
        else if (randomValue < probabilityFor2) // 25% шанс (включая предыдущие 5%)
        {
            multiplier = UnityEngine.Random.Range(2f, 3f);
        }
        else if (randomValue < probabilityFor1_5) // 40% шанс (включая предыдущие 25%)
        {
            multiplier = UnityEngine.Random.Range(1.5f, 2f);
        }
        else if (randomValue < baseMultiplierProbability) // 65% шанс (включая предыдущие 40%)
        {
            multiplier = UnityEngine.Random.Range(1.2f, 1.5f);
        }
        else // Оставшиеся 35% (100% - 65%)
        {
            multiplier = UnityEngine.Random.Range(minMultiplier, 1.2f);
        }
        
        // Округление до двух знаков после запятой
        currentGeneratedMultiplier = Mathf.Floor(multiplier * 100) / 100;
        currentGeneratedMultiplier = Mathf.Clamp(currentGeneratedMultiplier, minMultiplier, maxMultiplier);
    }

    private void ConfigureProbabilityCurve()
    {
        // Проверка и корректировка значений перед созданием кривой
        // Убедимся, что все значения в диапазоне от 0 до 1
        baseMultiplierProbability = Mathf.Clamp01(baseMultiplierProbability);
        probabilityFor1_5 = Mathf.Clamp01(probabilityFor1_5);
        probabilityFor2 = Mathf.Clamp01(probabilityFor2);
        probabilityFor3 = Mathf.Clamp01(probabilityFor3);
        probabilityFor5 = Mathf.Clamp01(probabilityFor5);
        probabilityFor10 = Mathf.Clamp01(probabilityFor10);
        
        // Убедимся, что значения убывают (вероятность каждого следующего множителя меньше предыдущего)
        if (probabilityFor1_5 > baseMultiplierProbability)
            probabilityFor1_5 = baseMultiplierProbability * 0.8f;
            
        if (probabilityFor2 > probabilityFor1_5)
            probabilityFor2 = probabilityFor1_5 * 0.8f;
            
        if (probabilityFor3 > probabilityFor2)
            probabilityFor3 = probabilityFor2 * 0.8f;
            
        if (probabilityFor5 > probabilityFor3)
            probabilityFor5 = probabilityFor3 * 0.8f;
            
        if (probabilityFor10 > probabilityFor5)
            probabilityFor10 = probabilityFor5 * 0.8f;
        
        // Создаем кривую вероятности
        multiplierProbabilityCurve = new AnimationCurve(
            new Keyframe(1.2f, baseMultiplierProbability),   // Base probability (>1.2x)
            new Keyframe(1.5f, probabilityFor1_5),    // Probability for >1.5x
            new Keyframe(2f, probabilityFor2),     // Probability for >2x
            new Keyframe(3f, probabilityFor3),     // Probability for >3x
            new Keyframe(5f, probabilityFor5),     // Probability for >5x
            new Keyframe(10f, probabilityFor10)    // Probability for >10x
        );

        // Сглаживаем кривую для более плавных переходов
        multiplierProbabilityCurve.SmoothTangents(0, 0.5f);
        multiplierProbabilityCurve.SmoothTangents(1, 0.5f);
        multiplierProbabilityCurve.SmoothTangents(2, 0.5f);
        multiplierProbabilityCurve.SmoothTangents(3, 0.5f);
        multiplierProbabilityCurve.SmoothTangents(4, 0.5f);
        multiplierProbabilityCurve.SmoothTangents(5, 0.5f);
        
        Debug.Log($"[WinChanceManager] Настроена кривая вероятности множителей: " +
            $"base={baseMultiplierProbability}, 1.5={probabilityFor1_5}, 2={probabilityFor2}, " +
            $"3={probabilityFor3}, 5={probabilityFor5}, 10={probabilityFor10}");
    }

    public float GetAirJumpMultiplier() =>
    Mathf.Max(1.1f, UnityEngine.Random.Range(minAirJumpMultiplier, maxAirJumpMultiplier));

    public void AdjustChanceAfterThrow(bool won, float betAmount)
    {
        float adjustment = won ?
            -UnityEngine.Random.Range(chanceDecreasePerThrowMin, chanceDecreasePerThrowMax) :
            chanceDecreasePerThrowMax;

        if (won && currentGeneratedMultiplier > 3f)
        {
            adjustment *= 1.5f;
        }
        else if (won && currentGeneratedMultiplier > 5f)
        {
            adjustment *= 2f;
        }

        CurrentWinChance = Mathf.Clamp(
            CurrentWinChance + adjustment,
            minWinChance,
            maxWinChance
        );

        if (won)
        {
        float winAmount = Mathf.Floor(betAmount * currentGeneratedMultiplier * 100) / 100;
            currentPlayerBalance += winAmount;
        }
        else
        {
            currentPlayerBalance -= betAmount;
            if (currentPlayerBalance < 0) currentPlayerBalance = 0;
        }

        totalGamesPlayed++;
        CheckForWinBoost();

        GenerateNewMultiplier();
    }

    public void DecreaseWinChancePerRotation()
    {
        CurrentWinChance = Mathf.Clamp(
            CurrentWinChance - chanceDecreasePerRotation,
            minRotationWinChance,
            maxWinChance
        );
    }

    public bool CalculateWinResult()
{
    CurrentWinChance = Mathf.Clamp(CurrentWinChance, minWinChance, maxWinChance);

    float effectiveChanceProbability  = Mathf.Clamp(
        CurrentWinChance * deviationMultiplier,
        minWinChance, 
        maxWinChance
    );

    Debug.Log($"[WinChanceManager] CalculateWinResult: Расчет результата...");
    Debug.Log($"[WinChanceManager] CalculateWinResult: Базовый шанс: {CurrentWinChance:F2}%, Множ. отклонения: {deviationMultiplier:F2}, Эффективный шанс: {effectiveChanceProbability :F2}%, Порог вероятности (0-1): {effectiveChanceProbability:F4}");

    float randomValue = Random.value; 
    bool result = randomValue <= effectiveChanceProbability;

    Debug.Log($"[WinChanceManager] CalculateWinResult: Случайное число (0-1): {randomValue:F4}, Порог: {effectiveChanceProbability:F4} -> Результат: {(result ? "ПОБЕДА" : "ПРОИГРЫШ")}");

   
    if (!result)
    {
        consecutiveLosses++;
        Debug.Log($"[WinChanceManager] CalculateWinResult: Поражение, счетчик последовательных поражений: {consecutiveLosses}");
        CheckForHardReset(); 
    }
    else
    {
        if (consecutiveLosses > 0)
        {
             Debug.Log($"[WinChanceManager] CalculateWinResult: Победа, сброс счетчика поражений с {consecutiveLosses} на 0");
        }
        consecutiveLosses = 0; 
    }
    return result;
}

    public void CheckForWinBoost()
    {
        if (totalGamesPlayed >= nextBoostGame && UnityEngine.Random.value < winBoostChance)
        {
            CurrentWinChance *= winBoostMultiplier;
            nextBoostGame = totalGamesPlayed + UnityEngine.Random.Range(minGamesForBoost, maxGamesForBoost);
        }
    }

    public void CheckForHardReset()
    {
        if (consecutiveLosses >= controlGoal)
        {
            Debug.Log($"[WinChanceManager] CheckForHardReset: Достигнут порог контрольной цели ({controlGoal} поражений), проверяем необходимость сброса");
            
            if (UnityEngine.Random.value < hardResetChance)
            {
                Debug.Log($"[WinChanceManager] CheckForHardReset: Сработал сброс (шанс: {hardResetChance})");
                
                // Если серверные параметры загружены, используем шанс выигрыша пользователя
                if (serverParametersLoaded && apiManager != null && apiManager.IsReady())
                {
                    float playerWinChance = apiManager.GetPlayerWinChance();
                    if (playerWinChance > 0)
                    {
                        Debug.Log($"[WinChanceManager] CheckForHardReset: Сброс к индивидуальному шансу выигрыша пользователя: {playerWinChance}");
                        CurrentWinChance = playerWinChance;
                    }
                    else
                    {
                        Debug.Log($"[WinChanceManager] CheckForHardReset: Индивидуальный шанс не найден, сброс к базовому: {baseWinChance}");
                        CurrentWinChance = baseWinChance;
                    }
                }
                else
                {
                    Debug.Log($"[WinChanceManager] CheckForHardReset: Серверные параметры не загружены, сброс к локальному базовому: {baseWinChance}");
                    CurrentWinChance = baseWinChance;
                }
                
                Debug.Log($"[WinChanceManager] CheckForHardReset: Шанс выигрыша сброшен к {CurrentWinChance}, счетчик поражений сброшен с {consecutiveLosses} на 0");
                consecutiveLosses = 0;
            }
            else
            {
                Debug.Log($"[WinChanceManager] CheckForHardReset: Сброс не сработал (выпало число выше шанса {hardResetChance})");
            }
        }
    }

    public void CalculateTargetBalance()
    {
        float targetBalance = currentPlayerBalance * (1 - initialBalanceLoss);
        UpdateBalanceControl(targetBalance);
    }

    public void UpdateBalanceControl(float targetBalance)
    {
        float balanceDifference = currentPlayerBalance - targetBalance;
        float normalizedDifference = Mathf.Clamp(balanceDifference / targetBalance, -1f, 1f);
        float dynamicChance = winChanceCurve.Evaluate(normalizedDifference);

        if (isDeviationActive)
        {
            CurrentWinChance = Mathf.Lerp(
                CurrentWinChance,
                dynamicChance * deviationMultiplier,
                Time.deltaTime * balanceSmoothness
            );

            if (currentPlayerBalance <= targetBalance * (1 - maxDeviationPercent))
            {
                deviationMultiplier = recoveryBoostMultiplier;
            }

            if (Mathf.Abs(currentPlayerBalance - targetBalance) < targetBalance * 0.05f)
            {
                isDeviationActive = false;
                deviationMultiplier = 1f;
            }
        }
        else
        {
            CurrentWinChance = Mathf.Lerp(
                CurrentWinChance,
                dynamicChance,
                Time.deltaTime * balanceSmoothness
            );
        }
        
        CurrentWinChance = Mathf.Clamp(CurrentWinChance, minWinChance, maxWinChance);
    }

    public void AdjustChance(bool boost)
    {
        if (boost)
        {
            CurrentWinChance = Mathf.Clamp(
                CurrentWinChance + chanceDecreasePerRotation * 2,
                minWinChance,
                maxWinChance
            );
        }
        else
        {
            CurrentWinChance = Mathf.Clamp(
                CurrentWinChance - chanceDecreasePerRotation,
                minWinChance,
                maxWinChance
            );
        }
    }

    public void CheckForDeviation()
    {
        if (isDeviationActive) return;

        if (UnityEngine.Random.value < deviationChance)
        {
            deviationMultiplier = UnityEngine.Random.Range(0.1f, maxDeviationPercent);
            isDeviationActive = true;
        }
    }

    public void UpdatePlayerBalance(float balance)
    {
        currentPlayerBalance = balance;
    }

    public string GetGameParameters()
    {
        float roundedMultiplier = Mathf.Floor(currentGeneratedMultiplier * 100) / 100;
        float rtp = CurrentWinChance * roundedMultiplier * 100f;

        return $"RTP: {rtp:F2}% | Chance: {CurrentWinChance:F2} | Multiplier: {roundedMultiplier:F2}x";
    }

    // Метод для обновления параметров множителей из данных с сервера
    private void UpdateMultiplierParams(GameParams gameParams)
    {
        if (gameParams == null) return;
        
        bool updated = false;
        
        // Обновляем значения вероятностей, если они приходят с сервера
        if (gameParams.probabilityFor10.HasValue)
        {
            probabilityFor10 = gameParams.probabilityFor10.Value;
            updated = true;
        }
            
        if (gameParams.probabilityFor5.HasValue)
        {
            probabilityFor5 = gameParams.probabilityFor5.Value;
            updated = true;
        }
            
        if (gameParams.probabilityFor3.HasValue)
        {
            probabilityFor3 = gameParams.probabilityFor3.Value;
            updated = true;
        }
            
        if (gameParams.probabilityFor2.HasValue)
        {
            probabilityFor2 = gameParams.probabilityFor2.Value;
            updated = true;
        }
            
        if (gameParams.probabilityFor1_5.HasValue)
        {
            probabilityFor1_5 = gameParams.probabilityFor1_5.Value;
            updated = true;
        }
            
        if (gameParams.baseMultiplierProbability.HasValue)
        {
            baseMultiplierProbability = gameParams.baseMultiplierProbability.Value;
            updated = true;
        }
        
        if (updated)
        {
            Debug.Log($"[WinChanceManager] Обновлены параметры вероятности множителей: " +
                $"base={baseMultiplierProbability}, 1.5={probabilityFor1_5}, 2={probabilityFor2}, " +
                $"3={probabilityFor3}, 5={probabilityFor5}, 10={probabilityFor10}");
            
            // Применяем обновленные параметры к кривой вероятности
            UpdateProbabilityCurve();
        }
    }
}