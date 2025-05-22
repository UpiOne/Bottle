    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Text;
    using TMPro;
    using System.Runtime.InteropServices;
    using UnityEngine.Serialization;


    [System.Serializable]
    public class AuthResponse 
    {
        public string token;
        public int playerId; 
    }
    [System.Serializable]
    public class ChangeTransactionStatusRequest { public int transactionStatusId; }
    [System.Serializable]
    public class EditPlayerRequest { public string name; public string avatarUrl; public float? winChance; }
    [System.Serializable]
    public class FinishRoundRequest { public int bet; public bool isWin; }
    [System.Serializable]
    public class RegisterWithReferalRequest { public string code; }
    [System.Serializable]
    public class GameParams // Swagger Schema (Убедись, что все поля совпадают с бэкендом)
    {
        public int id;
        public float baseWinChance;
        public float minWinChance;
        public float maxWinChance;
        public float chanceDecPerThrowMin;
        public float chanceDecPerThrowMax;
        public float chanceDecPerRotation;
        public float minRotationWinChance;
        public float minMultiplier;
        public float maxMultiplier;
        public float initialBalanceLoss;
        public float deviationChance;
        public float maxDeviationPercent;
        public int minGamesForBoost;
        public float winBoostChance;
        public float winBoostMultiplier;
        public float hardResetChance;
        public float controlGoal;
        public float reelSpeed;
        public float uprightThresholdForLine;
        
        // Новые параметры для шансов множителей
        public float? probabilityFor10;      // Шанс (0-1) получения множителя больше 10x
        public float? probabilityFor5;       // Шанс (0-1) получения множителя 5-10x
        public float? probabilityFor3;       // Шанс (0-1) получения множителя 3-5x
        public float? probabilityFor2;       // Шанс (0-1) получения множителя 2-3x
        public float? probabilityFor1_5;     // Шанс (0-1) получения множителя 1.5-2x
        public float? baseMultiplierProbability; // Шанс (0-1) получения множителя 1.2-1.5x
    }
    [System.Serializable]
    public class GetPlayerBalanceResponse { public int playerId; public int balance; }
    [System.Serializable]
    public class GetPlayerPlayedRoundsResponse { public PlayerDTO player; public RoundDTO[] rounds; }
    [System.Serializable]
    public class GetPlayerTransactionsResponse { public PlayerDTO player; public TransactionDTO[] transactions; }
    [System.Serializable]
    public class GetReferalLinkResponse { public string referalLink; }
    [System.Serializable]
    public class IsBannedResponse { public bool isBanned; }
    [System.Serializable]
    public class MoneyTransactionRequest { public bool isDeposit; public int amount; }
    [System.Serializable]
    public class PlayerDTO { public int id; public long telegramId; public string name; public string avatarUrl; public string registeredInAppAt; public double winRate; }
    [System.Serializable]
    public class PlayerInfoByIdResponse { public PlayerDTO player; public RoundDTO[] rounds; public string bannedAt; public int balance; public PlayerOptionsDTO options; public PlayerDTO[] playersInvited; }
    [System.Serializable]
    public class PlayerInfoResponse { public PlayerDTO player; public RoundDTO[] rounds; public string bannedAt; public int balance; public int[] playersIdsInvited; }
    [System.Serializable]
    public class PlayerOptionsDTO { public float winChance; }
    [System.Serializable]
    public class PlayerStatisticsResponse { public PlayerDTO player; public int balance; public int roundsQty; public int winsQty; public int lossQty; public int winAmount; public int lossAmount; }
    [System.Serializable]
    public class ProblemDetails { public string type; public string title; public int? status; public string detail; public string instance; }
    [System.Serializable]
    public class RoundDTO { public int id; public int bet; public bool isWin; public string finishedAt; }
    [System.Serializable]
    public class TransactionDTO { public int id; public double amount; public int statusId; public string statusName; public int typeId; public string typeName; public string createdAt; public string processedAt; }
    [System.Serializable]
    public class TransactionStatusOrTypeDTO { public int id; public string name; }
    [System.Serializable]
    public class TransactionStatusOrTypeDTOArrayWrapper { public TransactionStatusOrTypeDTO[] items; }
    [System.Serializable]
    public class PlayerInfoResponseArrayWrapper { public PlayerInfoResponse[] items; }
    [System.Serializable]
    public class PlayerStatisticsResponseArrayWrapper { public PlayerStatisticsResponse[] items; }
    [System.Serializable]
    public class GetPlayerTransactionsResponseArrayWrapper { public GetPlayerTransactionsResponse[] items; }


    public class ApiManager : MonoBehaviour
    {
        private static ApiManager _instance;
        public static ApiManager Instance { get { return _instance; } }

        // События для оповещения других частей игры
        public event Action<string> OnAuthenticationComplete; // Оповещает об окончании ВСЕЙ авторизации (ник + параметры игры)
        public event Action<GameParams> OnGameParamsUpdated;  // Оповещает о получении параметров игры
        public event Action<string> OnAuthenticationFailed; // Оповещает об ошибке авторизации

        [Header("API Settings")]
        [SerializeField] private bool useLocalMode = false; // По умолчанию выключен для продакшена
        [SerializeField] private string apiUrl = "https://api.adminbottle.ru/"; 
        [SerializeField] private float requestTimeout = 15f; // Немного увеличил таймаут
        [SerializeField] private float telegramInitTimeout = 15f;
        [SerializeField] private float telegramAvailabilityCheckInterval = 0.1f;

        private string API_BASE_URL => apiUrl.EndsWith("/") ? apiUrl : apiUrl + "/"; // Гарантируем слеш в конце
        private string _authToken = "";
        private int _playerId = 0; // Этот ID будет получен из ответа авторизации
        private string _username = "";
        private bool _isAuthenticated = false; // Становится true ТОЛЬКО после получения токена И ID
        private bool _isReady = false;         // Становится true ПОСЛЕ получения ника и параметров игры
        private bool _isLocalMode = false;
        private GameParams _currentGameParams = null; // Хранилище для параметров игры
        private float _playerWinChance = -1f; // Хранилище для индивидуального процента выигрыша пользователя
        private bool _telegramWebAppChecked = false;

    #if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool IsTelegramEnvironment(); // Не используется, но оставим на всякий случай

        [DllImport("__Internal")]
        private static extern bool IsTelegramWebAppAvailable();

        [DllImport("__Internal")]
        private static extern IntPtr GetTelegramWebAppInitData();
        
        [DllImport("__Internal")]
        private static extern IntPtr GetTelegramWebAppInitDataWithReferal();
        [DllImport("__Internal")]
        private static extern bool CopyToClipboard(string text);
        
        [DllImport("__Internal")]
        private static extern void FindAndSendReferalCodeToUnity(string unityGameObjectName);

        [DllImport("__Internal")]
        private static extern bool HasSavedReferalCode();
        
        [DllImport("__Internal")]
        private static extern IntPtr GetSavedReferalCode();
    #endif

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("ApiManager создан как синглтон");
            }
            else
            {
                Debug.LogWarning("ApiManager: Обнаружен дубликат синглтона. Уничтожаем новый объект.");
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartCoroutine(AutoAuthenticate());
            
            // Добавляем отложенную проверку телеграм параметров
            StartCoroutine(DelayedTelegramParamCheck());
        }

        // Основная логика авторизации

        private IEnumerator AutoAuthenticate()
        {
    #if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("ApiManager: Попытка авторизации через Telegram WebApp...");
            yield return StartCoroutine(GetTelegramInitDataAndAuthenticate());

            if (!_isReady) // Проверяем флаг готовности (который ставится после получения ника И параметров)
            {
                if (useLocalMode)
                {
                    Debug.Log("ApiManager: Авторизация Telegram не удалась или не завершена. useLocalMode = true. Активация локального режима.");
                    EnableLocalMode("Локальный (Fallback)"); // Локальный режим включает и оповещение
                }
                else
                {
                     Debug.LogWarning("ApiManager: Авторизация Telegram не удалась или не завершена. useLocalMode = false. Авторизация не выполнена.");
                     OnAuthenticationFailed?.Invoke("Ошибка входа или получения данных"); // Оповещаем об общей ошибке
                     // OnAuthenticationComplete НЕ вызывается в случае ошибки
                }
            }

    #else
            // Логика для редактора или не-WebGL сборок
            Debug.Log("ApiManager: Не WebGL сборка или редактор.");
            if (useLocalMode)
            {
                Debug.Log("ApiManager: useLocalMode = true. Активация локального режима.");
                EnableLocalMode("Локальный (Editor/NonWebGL)");
            }
            else
            {
                Debug.LogWarning("ApiManager: useLocalMode = false. Авторизация невозможна вне WebGL/Telegram.");
                _isAuthenticated = false;
                _isReady = false;
                OnAuthenticationFailed?.Invoke("Запуск вне WebGL без Local Mode");
            }
            yield break;
    #endif
        }

        private IEnumerator GetTelegramInitDataAndAuthenticate()
        {
    #if UNITY_WEBGL && !UNITY_EDITOR
            // Проверка доступности API Telegram
            Debug.Log("ApiManager: Проверка доступности Telegram WebApp API...");
            float elapsedTime = 0f;
            bool isAvailable = false;
            while (elapsedTime < telegramInitTimeout)
            {
                try {
                    if (IsTelegramWebAppAvailable()) {
                        Debug.Log("ApiManager: Telegram WebApp API доступен.");
                        isAvailable = true;
                        break;
                    }
                } catch (Exception e) {
                    Debug.LogError($"ApiManager: Ошибка вызова JS IsTelegramWebAppAvailable: {e.Message}.");
                    yield break; // Критическая ошибка, выходим
                }
                yield return new WaitForSeconds(telegramAvailabilityCheckInterval);
                elapsedTime += telegramAvailabilityCheckInterval;
            }

            if (!isAvailable) {
                Debug.LogError($"ApiManager: Таймаут ({telegramInitTimeout}s) или API недоступен. Telegram авторизация невозможна.");
                yield break; 
            }

            // 2. Получение initData
            Debug.Log("ApiManager: API доступен. Получение initData...");
            string telegramInitData = "";
            IntPtr initDataPtr = IntPtr.Zero;
            try {
                // ВАЖНОЕ ИЗМЕНЕНИЕ: используем новую функцию, которая добавляет реферальный код в initData
                initDataPtr = GetTelegramWebAppInitDataWithReferal();
                if (initDataPtr != IntPtr.Zero) {
                    telegramInitData = Marshal.PtrToStringUTF8(initDataPtr);
                     LogFullInitData(telegramInitData);
                    Debug.Log($"ApiManager: Получены Telegram initData: {(string.IsNullOrEmpty(telegramInitData) ? "пустые или null" : $"длина: {telegramInitData.Length}")}");
                    
                    // Проверяем наличие start_param в полученных данных
                    if (!string.IsNullOrEmpty(telegramInitData)) {
                        if (telegramInitData.Contains("start_param=")) {
                            Debug.Log($"ApiManager: [REFERAL] Обнаружен start_param в initData. Это хороший знак!");
                            string[] parts = telegramInitData.Split('&');
                            foreach (string part in parts) {
                                if (part.StartsWith("start_param=")) {
                                    string startParam = part.Substring("start_param=".Length);
                                    if (!string.IsNullOrEmpty(startParam)) {
                                        try {
                                            startParam = Uri.UnescapeDataString(startParam);
                                            Debug.Log($"ApiManager: [REFERAL] Содержимое start_param: {startParam}");
                                        } catch (Exception e) {
                                            Debug.LogError($"ApiManager: [REFERAL] Ошибка декодирования start_param: {e.Message}");
                                        }
                                    }
                                    break;
                                }
                            }
                        } else {
                            Debug.LogWarning($"ApiManager: [REFERAL] start_param НЕ НАЙДЕН в initData. Это может быть проблемой для рефералов.");
                        }
                    }
                    
                    Marshal.FreeHGlobal(initDataPtr); // Освобождаем память СРАЗУ после копирования
                    initDataPtr = IntPtr.Zero;
                } else {
                    Debug.LogWarning("ApiManager: GetTelegramWebAppInitDataWithReferal() вернул нулевой указатель.");
                }
            } catch (Exception e) {
                Debug.LogError($"ApiManager: Ошибка вызова JS GetTelegramWebAppInitDataWithReferal или преобразования указателя: {e.Message}.");
                if (initDataPtr != IntPtr.Zero) { try { Marshal.FreeHGlobal(initDataPtr); } catch { /* ignore */ } }
                yield break; // Ошибка, выходим
            }

            if (string.IsNullOrEmpty(telegramInitData)) {
                Debug.LogWarning("ApiManager: Telegram initData пусты или недоступны после получения.");
                yield break; // Нет данных для отправки, выходим
            }

            // Теперь проверяем URL на наличие реферальной ссылки (после получения initData)
            yield return StartCoroutine(CheckForReferalCode());

            // Отправка запроса на сервер для получения токена и ID
            string encodedInitData = Uri.EscapeDataString(telegramInitData);
            
            // ВАЖНО: Проверяем наличие start_param в кодированной строке перед отправкой на сервер
            if (!string.IsNullOrEmpty(encodedInitData)) {
                bool containsStartParam = encodedInitData.Contains("start_param");
                Debug.Log($"[REFERAL DEBUG] Кодированная initData " + 
                          (containsStartParam ? "СОДЕРЖИТ" : "НЕ СОДЕРЖИТ") + 
                          " параметр start_param. Это критично для работы рефералов!");
                
                if (containsStartParam) {
                    Debug.Log("[REFERAL DEBUG] Хорошо! Параметр start_param найден в кодированной строке");
                } else {
                    Debug.LogWarning("[REFERAL DEBUG] ВНИМАНИЕ! Параметр start_param не найден в кодированной строке!");
                    Debug.LogWarning("[REFERAL DEBUG] Это может быть причиной, почему реферальный код не записывается в базе данных");
                    Debug.LogWarning("[REFERAL DEBUG] Проверьте как формируется initData в Telegram WebApp");
                    
                    // Проверяем, был ли у нас сохранен реферальный код
                    if (!string.IsNullOrEmpty(_pendingReferalCode)) {
                        Debug.Log($"[REFERAL DEBUG] У нас есть реферальный код: {_pendingReferalCode}, но он не найден в initData");
                    }
                }
            }
            
            // ПРОВЕРЬТЕ URL И ПОРТ! 
            string loginUrl = $"{API_BASE_URL}api/Auth/player-login/{encodedInitData}";

            // Используем конструктор UnityWebRequest для POST без тела.
            using (UnityWebRequest www = new UnityWebRequest(loginUrl, UnityWebRequest.kHttpVerbPOST))
            {
                www.downloadHandler = new DownloadHandlerBuffer(); // Ожидаем ответ
                // www.uploadHandler остается null (нет тела запроса)
                www.timeout = (int)requestTimeout;
                Debug.Log($"ApiManager: Отправка initData (POST без тела) на авторизацию: {loginUrl}");
                yield return www.SendWebRequest();

                // Обработка ответа
                if (www.result == UnityWebRequest.Result.Success)
                {
                    AuthResponse authResponse = null;
                    bool authDataValid = false;
                    int extractedPlayerId = 0; // Временная переменная для ID

                    try {
                        string responseText = www.downloadHandler.text;
                        Debug.Log($"ApiManager: Ответ на авторизацию: {responseText}"); // Логируем ПОЛНЫЙ ответ сервера
                        // Используем класс AuthResponse с полем 'playerId'
                        authResponse = JsonUtility.FromJson<AuthResponse>(responseText);

                        // Проверяем наличие и токена, и playerId
                        if (authResponse != null && !string.IsNullOrEmpty(authResponse.token))
                        {
                            // ПРОВЕРЯЕМ, получили ли мы playerId из ответа (с правильным регистром)
                            // ИСПРАВЛЕНО ЗДЕСЬ (используем playerId):
                            if (authResponse.playerId > 0)
                            {
                                // ИСПРАВЛЕНО ЗДЕСЬ (используем playerId):
                                extractedPlayerId = authResponse.playerId;
                                authDataValid = true; // Все нужные данные есть
                                Debug.Log($"ApiManager: Токен и PlayerID ({extractedPlayerId}) успешно получены из ответа.");
                            }
                            else
                            {
                                 // Токен есть, но ID некорректный
                                 Debug.LogWarning($"ApiManager: Токен получен, НО playerId в ответе <= 0. Ответ: {responseText}");
                                 authDataValid = false; // Считаем данные невалидными, т.к. ID нужен дальше
                            }
                        } else {
                            // Либо authResponse null, либо токен пустой, либо парсинг не удался
                            Debug.LogError($"ApiManager: Ошибка авторизации Telegram: Ответ {www.responseCode}, но токен отсутствует/пуст или не удалось распарсить JSON. Ответ: {responseText}");
                            authDataValid = false;
                        }
                    } catch (Exception e) {
                        Debug.LogError($"ApiManager: Ошибка парсинга JSON ответа авторизации: {e.Message}\nОтвет: {www.downloadHandler.text}");
                        authDataValid = false;
                    }

                    //Действия при УСПЕШНОМ получении И токена, И ID
                    if (authDataValid) {
                        _authToken = $"Bearer {authResponse.token}";
                        _playerId = extractedPlayerId; // <<<--- Сохраняем извлеченный ID
                        _isAuthenticated = true; // <<<--- Флаг получения токена И ID
                        _isLocalMode = false;
                        Debug.Log($"ApiManager: Авторизация успешна. Токен установлен, PlayerID: {_playerId}");

                        // Запускаем получение остальной информации (ник, параметры игры),
                        // теперь у нас ДОЛЖЕН быть валидный _playerId
                        yield return StartCoroutine(GetMyPlayerInfoUsingToken());
                        // Внутри GetMyPlayerInfoUsingToken будет вызван GetGameParameters, если инфо получено
                    }
                    else // Если authDataValid == false (нет токена ИЛИ нет ID)
                    {
                        Debug.LogError("ApiManager: Данные авторизации невалидны (отсутствует токен или необходимый playerId). Авторизация прервана.");
                        // Выходим, AutoAuthenticate обработает fallback или покажет ошибку
                        // OnAuthenticationFailed будет вызван в AutoAuthenticate, если _isReady не станет true
                    }
                }
                else // Ошибка сети или HTTP (не 2xx код ответа)
                {
                    HandleRequestError(www, "запросе токена и ID");
                     // _isAuthenticated остается false, выходим, AutoAuthenticate обработает fallback
                }
            } 
    #else
        // Этот код выполнится, если сборка не WebGL или запущено в редакторе
        Debug.LogError("ApiManager: GetTelegramInitDataAndAuthenticate вызван в не-WebGL среде или редакторе. Авторизация через Telegram невозможна.");
        yield break;
    #endif
        } 

        private IEnumerator GetMyPlayerInfoUsingToken()
        {
            // Добавляем проверку на наличие _playerId
            if (!_isAuthenticated || string.IsNullOrEmpty(_authToken) || _playerId <= 0) {
                Debug.LogError($"GetMyPlayerInfoUsingToken: Невозможно выполнить запрос - нет токена или PlayerId ({_playerId}) не установлен.");
                yield break;
            }

            string getMeUrl = $"{API_BASE_URL}api/Player/get-player/{_playerId}"; // <<<--- Используем _playerId

            using (UnityWebRequest www = UnityWebRequest.Get(getMeUrl)) {
                www.SetRequestHeader("Authorization", _authToken); // Используем наш токен
                www.timeout = (int)requestTimeout;
                Debug.Log($"ApiManager: Запрос информации о себе (по ID из токена): {getMeUrl}"); // <<<--- Обновлен лог
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success) {
                     Debug.Log($"ApiManager: Ответ сервера на инфо о себе: {www.downloadHandler.text}");
                    // Ожидаем PlayerInfoByIdResponse, так как получаем по ID
                    PlayerInfoByIdResponse response = null;
                    bool playerInfoValid = false;
                    try {
                        response = JsonUtility.FromJson<PlayerInfoByIdResponse>(www.downloadHandler.text);
                        // Проверяем, что ID в ответе совпадает с запрошенным ID и есть ник
                        if (response?.player != null && response.player.id == _playerId && !string.IsNullOrEmpty(response.player.name)) {
                            playerInfoValid = true;
                        } else {
                            Debug.LogError($"ApiManager: Ошибка получения инфо о себе: Ответ 200 OK, но данные игрока некорректны (player null, ID не совпал, или имя пустое). Ожидали ID: {_playerId}. Ответ: {www.downloadHandler.text}");
                        }
                    } catch (Exception e) {
                        Debug.LogError($"ApiManager: Ошибка парсинга ответа инфо о себе: {e.Message}\nОтвет: {www.downloadHandler.text}");
                    }

                    if (playerInfoValid) {
                        // Устанавливаем имя пользователя (ID уже есть)
                        _username = response.player.name;
                        
                        // Получаем индивидуальный процент выигрыша пользователя
                        if (response.options != null) {
                            _playerWinChance = response.options.winChance;
                            Debug.Log($"ApiManager: [ИНДИВИДУАЛЬНЫЙ ШАНС] Получен индивидуальный процент выигрыша пользователя: {_playerWinChance}");
                        } else {
                            Debug.LogWarning("ApiManager: [ИНДИВИДУАЛЬНЫЙ ШАНС] В ответе сервера отсутствуют индивидуальные настройки пользователя (options)");
                        }
                        
                        Debug.Log($"ApiManager: Информация об игроке получена (по ID). Ник: {_username}, ID: {_playerId}");

                        // Теперь запрашиваем параметры игры
                        yield return StartCoroutine(GetGameParameters());
                    }
                    else {
                         yield break; // Ошибка парсинга или невалидные данные
                    }
                } else {
                     HandleRequestError(www, "получении инфо о себе");
                     yield break; 
                }
            }
        }

        private IEnumerator GetGameParameters()
        {
            if (!_isAuthenticated || string.IsNullOrEmpty(_authToken)) {
                Debug.LogError("GetGameParameters: Невозможно выполнить запрос - нет токена.");
                yield break; // Критическая ошибка, не можем получить параметры
            }

            // Убедитесь, что этот URL правильный для вашего API
            string url = $"{API_BASE_URL}api/GameParams/get-game-params";

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("Authorization", _authToken); // Используем токен
                www.timeout = (int)requestTimeout;
                Debug.Log($"ApiManager: [ПАРАМЕТРЫ ИГРЫ] Запрос параметров игры: {url}");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"ApiManager: [ПАРАМЕТРЫ ИГРЫ] Ответ сервера: {www.downloadHandler.text}");
                    GameParams loadedParams = null;
                    bool paramsValid = false;
                    try
                    {
                        // ВАЖНО: Ожидаем ОДИН объект GameParams, НЕ массив.
                        loadedParams = JsonUtility.FromJson<GameParams>(www.downloadHandler.text);
                        if (loadedParams != null) // Простая проверка, что парсинг прошел
                        {
                            paramsValid = true;
                        } else {
                             Debug.LogError($"ApiManager: [ПАРАМЕТРЫ ИГРЫ] Не удалось распарсить параметры игры из JSON. Ответ: {www.downloadHandler.text}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"ApiManager: [ПАРАМЕТРЫ ИГРЫ] Исключение при парсинге параметров игры: {e.Message}\nОтвет: {www.downloadHandler.text}");
                    }

                    if(paramsValid) {
                        _currentGameParams = loadedParams;
                        Debug.Log($"ApiManager: [ПАРАМЕТРЫ ИГРЫ] Параметры игры успешно получены и сохранены:");
                        Debug.Log($"ApiManager: [ПАРАМЕТРЫ ИГРЫ] BaseWinChance: {_currentGameParams.baseWinChance}");
                        Debug.Log($"ApiManager: [ПАРАМЕТРЫ ИГРЫ] MinWinChance: {_currentGameParams.minWinChance}");
                        Debug.Log($"ApiManager: [ПАРАМЕТРЫ ИГРЫ] MaxWinChance: {_currentGameParams.maxWinChance}");
                        Debug.Log($"ApiManager: [ПАРАМЕТРЫ ИГРЫ] MinMultiplier: {_currentGameParams.minMultiplier}");
                        Debug.Log($"ApiManager: [ПАРАМЕТРЫ ИГРЫ] MaxMultiplier: {_currentGameParams.maxMultiplier}");
                        
                        _isReady = true; 
                        
                        OnGameParamsUpdated?.Invoke(_currentGameParams); // Оповещаем подписчиков о параметрах
                        OnAuthenticationComplete?.Invoke(_username);   // Оповещаем об УСПЕШНОМ завершении ВСЕЙ авторизации
                    } else {
                        // Не смогли получить параметры, обработка ошибки выше (в AutoAuthenticate)
                        yield break;
                    }

                }
                else
                {
                    HandleRequestError(www, "получении параметров игры");
                    // Не смогли получить параметры, обработка ошибки выше (в AutoAuthenticate)
                    yield break;
                }
            }
        }

        // Новый метод для проверки реферальной ссылки
        private IEnumerator CheckForReferalCode()
        {
    #if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("[REFERAL DEBUG] Начинаем проверку реферальной ссылки через JSLIB");
            
            try {
                // Вызываем JavaScript функцию в JSLIB.
                // Передаем имя объекта, который должен получить SendMessage.
                FindAndSendReferalCodeToUnity(this.gameObject.name);
                Debug.Log("[REFERAL DEBUG] Вызвана JS функция FindAndSendReferalCodeToUnity");

            } catch (Exception e) {
                Debug.LogError($"[REFERAL DEBUG] Ошибка при вызове JS функции FindAndSendReferalCodeToUnity: {e.Message}");
            }
            
            // Ждем небольшую паузу, чтобы JavaScript успел выполниться
            Debug.Log("[REFERAL DEBUG] Ожидание выполнения JavaScript...");
            yield return new WaitForSeconds(0.5f);
            
            // Проверяем, сохранен ли код в localStorage
            try {
                if (HasSavedReferalCode()) {
                    IntPtr codePtr = GetSavedReferalCode();
                    if (codePtr != IntPtr.Zero) {
                        string referalCode = Marshal.PtrToStringUTF8(codePtr);
                        Debug.Log($"[REFERAL DEBUG] Получен реферальный код из localStorage: {referalCode}");
                        ProcessReferalCode(referalCode);
                        Marshal.FreeHGlobal(codePtr); // Освобождаем память
                    }
                } else {
                    Debug.Log("[REFERAL DEBUG] Реферальный код не найден в localStorage");
                }
            } catch (Exception e) {
                Debug.LogError($"[REFERAL DEBUG] Ошибка при получении кода из localStorage: {e.Message}");
            }
            
            Debug.Log("[REFERAL DEBUG] Ожидание JavaScript завершено. Проверяем _pendingReferalCode: " + 
                    (string.IsNullOrEmpty(_pendingReferalCode) ? "null/empty" : _pendingReferalCode));
            
    #else
            Debug.Log("[REFERAL DEBUG] Проверка реферальной ссылки не выполняется (не WebGL или редактор)");
            yield break;
    #endif
        }

        // Метод для логирования полного URL
        public void LogFullUrl(string url)
        {
            Debug.Log($"[REFERAL DEBUG] Полный URL: {url}");
        }

        // Метод для логирования ошибки когда реферальный код не найден
        public void LogReferalNotFound(string message)
        {
            Debug.LogWarning($"[REFERAL DEBUG] {message}");
        }

        // Метод для логирования ошибок JavaScript
        public void LogJavascriptError(string error)
        {
            Debug.LogError($"[REFERAL DEBUG] JavaScript ошибка: {error}");
        }
        
        // Этот метод будет вызван из JavaScript
        public void ProcessReferalCode(string referalCode)
        {
            Debug.Log($"[REFERAL DEBUG] ProcessReferalCode вызван с кодом: {referalCode}");
            
            if (!string.IsNullOrEmpty(referalCode)) {
                Debug.Log($"[REFERAL DEBUG] Получен реферальный код из URL: {referalCode}");
                
                // Сохраняем код для последующего использования после авторизации
                _pendingReferalCode = referalCode;
                
                // Выводим дополнительную отладочную информацию
                Debug.Log($"[REFERAL DEBUG] Сохранен реферальный код для последующей обработки: {_pendingReferalCode}");
                
                // Проверка хранилища
                if (PlayerPrefs.HasKey("last_referal_code")) {
                    Debug.Log($"[REFERAL DEBUG] В PlayerPrefs уже есть код: {PlayerPrefs.GetString("last_referal_code")}");
                }
                
                // Сохраняем в PlayerPrefs для большей надежности
                PlayerPrefs.SetString("last_referal_code", referalCode);
                PlayerPrefs.Save();
                Debug.Log("[REFERAL DEBUG] Код сохранен в PlayerPrefs");
            } else {
                Debug.LogWarning("[REFERAL DEBUG] ProcessReferalCode вызван с пустым кодом");
            }
        }
        
        private string _pendingReferalCode = null;
        

        private void EnableLocalMode(string username)
        {
            _isLocalMode = true;
            _isAuthenticated = true; // Считаем, что в локальном режиме "авторизован"
            _authToken = "local_test_token";
            _playerId = -1; // В локальном режиме ID не настоящий
            _username = username;
            _playerWinChance = 50f; // Устанавливаем дефолтный процент выигрыша в локальном режиме
            // В локальном режиме можно задать дефолтные параметры игры
            _currentGameParams = new GameParams() {
                 // Заполните тестовыми значениями, соответствующими вашей структуре GameParams
                 id = 0,
                 baseWinChance = 0.5f,
                 minWinChance = 0.1f,
                 maxWinChance = 0.9f,
                 chanceDecPerThrowMin = 0.01f,
                 chanceDecPerThrowMax = 0.05f,
                 chanceDecPerRotation = 0.02f,
                 minRotationWinChance = 0.05f,
                 minMultiplier = 1.1f,
                 maxMultiplier = 5.0f,
                 initialBalanceLoss = 0.1f,
                 deviationChance = 0.2f,
                 maxDeviationPercent = 0.7f,
                 minGamesForBoost = 10,
                 winBoostChance = 0.1f,
                 winBoostMultiplier = 2.0f,
                 hardResetChance = 0.01f,
                 controlGoal = 1000f,
                 reelSpeed = 5f,
                 uprightThresholdForLine = 0.9f,
                 
                 // Инициализируем параметры множителей для локального режима
                 probabilityFor10 = 0.01f,         // 1% шанс
                 probabilityFor5 = 0.03f,          // 3% шанс
                 probabilityFor3 = 0.05f,          // 5% шанс
                 probabilityFor2 = 0.25f,          // 25% шанс
                 probabilityFor1_5 = 0.40f,        // 40% шанс
                 baseMultiplierProbability = 0.65f // 65% шанс
            };
            // ДОБАВЛЕНО: инициализация баланса
            if (!PlayerPrefs.HasKey("local_coins")) {
                PlayerPrefs.SetFloat("local_coins", 1000);
                PlayerPrefs.Save();
            }
            _isReady = true; // Готов к работе в локальном режиме
            Debug.Log($"ApiManager: Активирован локальный режим. Ник: {_username}, PlayerID: {_playerId}");
            OnGameParamsUpdated?.Invoke(_currentGameParams); // Оповещаем о локальных параметрах
            OnAuthenticationComplete?.Invoke(_username);   // Оповещаем о завершении "авторизации"
        }

        // Получение баланса пользователя
        public IEnumerator GetUserBalance(Action<int> callback)
        {
            if (!_isReady && !_isLocalMode) { // Проверяем полную готовность или локальный режим
                Debug.LogError("ApiManager: ApiManager не готов для получения баланса (не авторизован или параметры не получены).");
                callback?.Invoke(0);
                yield break;
            }
             if (_isLocalMode) {
                Debug.Log("ApiManager: Локальный режим - Получение баланса из PlayerPrefs.");
                float localBalance = PlayerPrefs.GetFloat("local_coins", 1000); // Используем другое имя ключа для локального баланса
                callback?.Invoke(Mathf.RoundToInt(localBalance));
                yield break;
            }
            
            if (_playerId <= 0) { // Доп. проверка на ID
                Debug.LogError("ApiManager: Невозможно получить баланс - PlayerId не установлен.");
                callback?.Invoke(0);
                yield break;
            }

            string getBalanceUrl = $"{API_BASE_URL}api/Balance/get-player-balance/{_playerId}";
            using (UnityWebRequest www = UnityWebRequest.Get(getBalanceUrl)) {
                www.SetRequestHeader("Authorization", _authToken);
                www.timeout = (int)requestTimeout;
                // Debug.Log($"ApiManager: Запрос баланса: {getBalanceUrl}"); // Можно раскомментировать для отладки
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success) {
                    // Debug.Log($"ApiManager: Ответ сервера на баланс: {www.downloadHandler.text}");
                    try {
                        GetPlayerBalanceResponse response = JsonUtility.FromJson<GetPlayerBalanceResponse>(www.downloadHandler.text);
                        if (response != null) {
                            callback?.Invoke(response.balance);
                        } else {
                             Debug.LogError("ApiManager: Ошибка получения баланса: Ответ 200 OK, но данные не удалось распарсить или они null.");
                             callback?.Invoke(0);
                        }
                    } catch(Exception e) {
                        Debug.LogError($"ApiManager: Ошибка парсинга ответа баланса: {e.Message}\nОтвет: {www.downloadHandler.text}");
                        callback?.Invoke(0);
                    }
                } else {
                    HandleRequestError(www, "получении баланса");
                    callback?.Invoke(0);
                }
            }
        }

        // Отправка транзакции
        public IEnumerator SendTransaction(int amount, bool isDeposit, Action<bool> callback)
        {
            if (!_isReady && !_isLocalMode) {
                Debug.LogError($"ApiManager: ApiManager не готов для {(isDeposit ? "депозита" : "вывода")}.");
                callback?.Invoke(false);
                yield break;
            }
             if (_isLocalMode) {
                Debug.Log($"ApiManager: Локальный режим - {(isDeposit ? "Имитация депозита" : "Имитация вывода")} {amount}.");
                // Имитация изменения локального баланса
                float currentLocalBalance = PlayerPrefs.GetFloat("local_coins", 1000);
                currentLocalBalance += (isDeposit ? amount : -amount);
                PlayerPrefs.SetFloat("local_coins", currentLocalBalance);
                PlayerPrefs.Save(); // Сохраняем изменение
                Debug.Log($"ApiManager: Локальный баланс обновлен: {currentLocalBalance}");
                callback?.Invoke(true);
                yield break;
            }
 
            if (_playerId <= 0) {
                Debug.LogError($"ApiManager: Невозможно создать транзакцию - PlayerId не установлен.");
                callback?.Invoke(false);
                yield break;
            }

            // Валидация amount - проверка на соответствие требованиям API (int32, minimum 1)
            if (amount < 1) {
                Debug.LogError($"ApiManager: Невозможно создать транзакцию - сумма должна быть не менее 1 (получено: {amount})");
                callback?.Invoke(false);
                yield break;
            }

            string transactionUrl = $"{API_BASE_URL}api/Balance/create-transaction/{_playerId}";
            MoneyTransactionRequest requestBody = new MoneyTransactionRequest { isDeposit = isDeposit, amount = amount };
            string jsonData = JsonUtility.ToJson(requestBody);

            Debug.Log($"ApiManager: Отправка транзакции: isDeposit={isDeposit}, amount={amount} (строго целое число int32)");

            // Используем using для автоматического Dispose запроса
            using (UnityWebRequest www = new UnityWebRequest(transactionUrl, UnityWebRequest.kHttpVerbPOST)) {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer(); // Не забываем получать ответ
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", _authToken);
                www.timeout = (int)requestTimeout;
                Debug.Log($"ApiManager: Запрос транзакции на URL: {transactionUrl}\nJSON: {jsonData}");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success || www.responseCode == 201 || www.responseCode == 204) { // Учитываем разные коды успеха
                    Debug.Log($"ApiManager: Ответ сервера на {(isDeposit ? "депозит" : "вывод")}: Успех (Код: {www.responseCode}).");
                    callback?.Invoke(true);
                } else {
                    HandleRequestError(www, $"{(isDeposit ? "депозите" : "выводе")}");
                    callback?.Invoke(false);
                }
            }
        }

        // Отправка результата раунда
        public IEnumerator SendGameRoundResult(int betAmount, bool isWin, Action<bool> callback = null)
        {
             if (!_isReady && !_isLocalMode) {
                Debug.LogError("ApiManager: ApiManager не готов для отправки раунда.");
                callback?.Invoke(false);
                yield break;
            }
             if (_isLocalMode) {
                Debug.Log($"ApiManager: Локальный режим - Имитация отправки раунда (Ставка: {betAmount}, Победа: {isWin}).");
                // Имитация изменения локального баланса (примерное, без учета реальных параметров)
                float currentLocalBalance = PlayerPrefs.GetFloat("local_coins", 1000);
                float winAmount = isWin ? betAmount * 1.5f : -betAmount; // Примерный расчет выигрыша/проигрыша
                currentLocalBalance += winAmount;
                PlayerPrefs.SetFloat("local_coins", currentLocalBalance);
                PlayerPrefs.Save();
                Debug.Log($"ApiManager: Локальный баланс после раунда обновлен: {currentLocalBalance}");
                callback?.Invoke(true);
                yield break;
            }

            if (_playerId <= 0) {
                Debug.LogError("ApiManager: Невозможно отправить результат раунда - PlayerId не установлен.");
                callback?.Invoke(false);
                yield break;
            }

            string gameRoundUrl = $"{API_BASE_URL}api/GameRound/finish-game-round/{_playerId}";
            FinishRoundRequest requestBody = new FinishRoundRequest { bet = betAmount, isWin = isWin };
            string jsonData = JsonUtility.ToJson(requestBody);

            using (UnityWebRequest www = new UnityWebRequest(gameRoundUrl, UnityWebRequest.kHttpVerbPOST)) {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", _authToken);
                www.timeout = (int)requestTimeout;
                Debug.Log($"ApiManager: Отправка раунда: {gameRoundUrl}\nData: {jsonData}");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success || www.responseCode == 201 || www.responseCode == 204) { // Учитываем разные коды успеха
                     Debug.Log($"ApiManager: Ответ сервера на раунд: Успех (Код: {www.responseCode}).");
                     callback?.Invoke(true);
                } else {
                     HandleRequestError(www, "отправке раунда");
                    callback?.Invoke(false);
                }
            }
        }

        // Получить историю игр пользователя
        public IEnumerator GetPlayerGameHistory(Action<RoundDTO[]> callback)
        {
            if (!_isReady && !_isLocalMode) {
                Debug.LogError("ApiManager: ApiManager не готов для получения истории игр.");
                callback?.Invoke(null);
                yield break;
            }
            
            if (_isLocalMode) {
                Debug.Log("ApiManager: Локальный режим - Возвращаем фиктивную историю игр.");
                RoundDTO[] mockHistory = new RoundDTO[3] {
                    new RoundDTO { id = 1, bet = 100, isWin = true, finishedAt = DateTime.Now.AddDays(-1).ToString() },
                    new RoundDTO { id = 2, bet = 200, isWin = false, finishedAt = DateTime.Now.AddDays(-2).ToString() },
                    new RoundDTO { id = 3, bet = 300, isWin = true, finishedAt = DateTime.Now.AddDays(-3).ToString() }
                };
                callback?.Invoke(mockHistory);
                yield break;
            }
            
            if (_playerId <= 0) {
                Debug.LogError("ApiManager: Невозможно получить историю игр - PlayerId не установлен.");
                callback?.Invoke(null);
                yield break;
            }

            string historyUrl = $"{API_BASE_URL}api/GameRound/get-player-rounds/{_playerId}";
            
            using (UnityWebRequest www = UnityWebRequest.Get(historyUrl))
            {
                www.SetRequestHeader("Authorization", _authToken);
                www.timeout = (int)requestTimeout;
                Debug.Log($"ApiManager: Запрос истории игр: {historyUrl}");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"ApiManager: Ответ сервера на запрос истории: {www.downloadHandler.text}");
                    try
                    {
                        GetPlayerPlayedRoundsResponse response = JsonUtility.FromJson<GetPlayerPlayedRoundsResponse>(www.downloadHandler.text);
                        if (response != null && response.rounds != null)
                        {
                            callback?.Invoke(response.rounds);
                        }
                        else
                        {
                            Debug.LogError("ApiManager: Ответ 200 OK, но история игр пустая или null.");
                            callback?.Invoke(new RoundDTO[0]);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"ApiManager: Ошибка парсинга ответа истории игр: {e.Message}\nОтвет: {www.downloadHandler.text}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    HandleRequestError(www, "получении истории игр");
                    callback?.Invoke(null);
                }
            }
        }

        // Получить реферальную ссылку игрока
        public IEnumerator GetReferalLink(Action<string> callback)
        {
            if (!_isReady && !_isLocalMode) {
                Debug.LogError("ApiManager: ApiManager не готов для получения реферальной ссылки.");
                callback?.Invoke("");
                yield break;
            }
            
            if (_isLocalMode) {
                Debug.Log("ApiManager: Локальный режим - Возвращаем фиктивную реферальную ссылку.");
                callback?.Invoke("https://t.me/bottlegame_bot?start=local_referal");
                yield break;
            }
            
            if (_playerId <= 0) {
                Debug.LogError("ApiManager: Невозможно получить реферальную ссылку - PlayerId не установлен.");
                callback?.Invoke("");
                yield break;
            }

            string url = $"{API_BASE_URL}api/Referal/get-referal-link/{_playerId}";
            
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("Authorization", _authToken);
                www.timeout = (int)requestTimeout;
                Debug.Log($"ApiManager: [РЕФЕРАЛЬНАЯ ССЫЛКА] Запрос реферальной ссылки: {url}");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"ApiManager: [РЕФЕРАЛЬНАЯ ССЫЛКА] Ответ сервера: {www.downloadHandler.text}");
                    try
                    {
                        GetReferalLinkResponse response = JsonUtility.FromJson<GetReferalLinkResponse>(www.downloadHandler.text);
                        if (response != null && !string.IsNullOrEmpty(response.referalLink))
                        {
                            string link = response.referalLink;
                            Debug.Log($"ApiManager: [РЕФЕРАЛЬНАЯ ССЫЛКА] Получена ссылка: {link}");
     
                            callback?.Invoke(link);
                        }
                        else
                        {
                            Debug.LogError("ApiManager: [РЕФЕРАЛЬНАЯ ССЫЛКА] Ответ 200 OK, но реферальная ссылка пустая или null.");
                            callback?.Invoke("");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"ApiManager: [РЕФЕРАЛЬНАЯ ССЫЛКА] Ошибка парсинга ответа: {e.Message}\nОтвет: {www.downloadHandler.text}");
                        callback?.Invoke("");
                    }
                }
                else
                {
                    HandleRequestError(www, "получении реферальной ссылки");
                    callback?.Invoke("");
                }
            }
        }
        
        // Копировать текст в буфер обмена
        public bool CopyTextToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogError("ApiManager: Невозможно скопировать пустой текст в буфер обмена.");
                return false;
            }

            Debug.Log($"ApiManager: Запрос на копирование в буфер обмена: {text}");

    #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
                // Вызываем функцию, импортированную из jslib, вместо ExternalEval
                Debug.Log("ApiManager: Используем импортированный метод CopyToClipboard из jslib.");
                bool result = CopyToClipboard(text); // Вызываем функцию из jslib
                Debug.Log($"ApiManager: Результат вызова CopyToClipboard из jslib: {result}");
                return result;
                // --- КОНЕЦ ИЗМЕНЕНИЯ ---
            }
            catch (Exception e)
            {
                // Эта ошибка может возникнуть, если jslib не найден или функция в нем отсутствует/неправильно названа
                Debug.LogError($"ApiManager: Ошибка при вызове внешней функции CopyToClipboard: {e.Message}");
                return false;
            }
    #else
            // Код для редактора и других платформ
            try
            {
                Debug.Log("ApiManager: Используем системный метод копирования (не WebGL)");
                GUIUtility.systemCopyBuffer = text;
                Debug.Log($"ApiManager: Текст '{text}' скопирован в буфер обмена через GUIUtility.systemCopyBuffer");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"ApiManager: Ошибка при копировании через системный буфер: {e.Message}");
                return false;
            }
    #endif
        }

        // --- Вспомогательные методы и геттеры ---

        private void HandleRequestError(UnityWebRequest www, string operationName)
        {
            Debug.LogError($"ApiManager: Ошибка сети или HTTP при {operationName}: {www.result}, Код: {www.responseCode}, Ошибка: {www.error}");
            if (!string.IsNullOrEmpty(www.downloadHandler?.text))
            {
                string responseText = www.downloadHandler.text;
                Debug.LogError($"Ответ сервера: {responseText}"); // Всегда логируем сам ответ
                try
                {
                    // Пытаемся распарсить как ProblemDetails для стандартных ошибок ASP.NET Core
                    ProblemDetails problemDetails = JsonUtility.FromJson<ProblemDetails>(responseText);
                    // Проверяем, что парсинг удался и поля не пустые (JsonUtility вернет объект с null полями, если JSON не соответствует)
                    if (problemDetails != null && (!string.IsNullOrEmpty(problemDetails.title) || !string.IsNullOrEmpty(problemDetails.detail) || problemDetails.status.HasValue))
                    {
                         Debug.LogError($"ProblemDetails: Title='{problemDetails.title}', Detail='{problemDetails.detail}', Status='{problemDetails.status}'");
                    }
                    // Можно добавить парсинг других возможных форматов ошибок, если они есть
                }
                catch (Exception parseError)
                {
                     // Ошибка парсинга JSON - это нормально, если ответ не был JSON или был в другом формате
                     // Debug.Log($"ApiManager: Не удалось распарсить ответ как ProblemDetails при {operationName}. Ошибка парсинга: {parseError.Message}");
                }
            }
        }

        // Готов ли ApiManager к выполнению запросов (авторизован + параметры получены)?
        public bool IsReady()
        {
            return _isReady || _isLocalMode;
        }

        // Получен ли токен?
        public bool IsAuthenticated()
        {
            return _isAuthenticated;
        }

        // Включен ли локальный режим?
        public bool IsLocalMode()
        {
            return _isLocalMode;
        }

        // Получить ID игрока (может быть 0 или -1)
        public int GetPlayerId()
        {
            // Комментарий бэкендера относился к тому, что этот ID нужно использовать в запросах,
            // а не к тому, как его получать. Этот метод просто возвращает сохраненное значение.
            return _playerId;
        }

         // Получить Ник игрока (может быть пустым или содержать ID/ошибку)
         public string GetUsername()
        {
            return _username;
        }

        // Позволяет получить текущие параметры игры (может вернуть null, если не загружены)
        public GameParams GetCurrentGameParams()
        {
            if (_currentGameParams == null && !_isLocalMode && _isAuthenticated) { // Логируем предупреждение только если авторизованы, но параметры не получены
                Debug.LogWarning("ApiManager: Запрос параметров игры, но они еще не загружены или произошла ошибка при их загрузке.");
            }
            return _currentGameParams;
        }

        // Получить индивидуальный процент выигрыша пользователя
        public float GetPlayerWinChance()
        {
            if (_playerWinChance > 0) {
                Debug.Log($"ApiManager: [ИНДИВИДУАЛЬНЫЙ ШАНС] Возвращаем индивидуальный процент выигрыша: {_playerWinChance}");
            } else {
                Debug.LogWarning($"ApiManager: [ИНДИВИДУАЛЬНЫЙ ШАНС] Запрошен индивидуальный процент выигрыша, но он не установлен или некорректен: {_playerWinChance}");
            }
            return _playerWinChance;
        }

        // Новый метод для отложенной проверки Telegram WebApp параметров
        private IEnumerator DelayedTelegramParamCheck()
        {
            Debug.Log("[REFERAL DEBUG] Запущена отложенная проверка параметров Telegram WebApp через JSLIB");
            
            yield return new WaitForSeconds(2.0f); // Ждем немного
            
            // Вызываем JS функцию еще раз на случай, если при первой попытке Telegram.WebApp не был готов
    #if UNITY_WEBGL && !UNITY_EDITOR
             try {
                FindAndSendReferalCodeToUnity(this.gameObject.name);
                Debug.Log("[REFERAL DEBUG] Вызвана JS функция FindAndSendReferalCodeToUnity (отложенная проверка)");
             } catch (Exception e) {
                 Debug.LogError($"[REFERAL DEBUG] Ошибка при вызове JS функции FindAndSendReferalCodeToUnity (отложенная проверка): {e.Message}");
             }
    #endif

            yield return new WaitForSeconds(3.0f); // Ждем еще немного
            
    #if UNITY_WEBGL && !UNITY_EDITOR
             try {
                // Можно вызвать еще раз для надежности, или проверить флаг, что уже найден
                if (string.IsNullOrEmpty(_pendingReferalCode)) {
                     FindAndSendReferalCodeToUnity(this.gameObject.name);
                     Debug.Log("[REFERAL DEBUG] Вызвана JS функция FindAndSendReferalCodeToUnity (вторая отложенная проверка)");
                }
             } catch (Exception e) {
                 Debug.LogError($"[REFERAL DEBUG] Ошибка при вызове JS функции FindAndSendReferalCodeToUnity (вторая отложенная проверка): {e.Message}");
             }
    #endif
        }

        // Новый метод для проверки параметров Telegram WebApp
        private void CheckTelegramWebAppParams()
        {
            if (_telegramWebAppChecked)
            {
                Debug.Log("[REFERAL DEBUG] Проверка Telegram WebApp параметров уже была выполнена");
                return;
            }
            
            _telegramWebAppChecked = true;
            Debug.Log("[REFERAL DEBUG] Выполняем проверку параметров Telegram WebApp");
            
    #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // Вместо ExternalEval используем наш новый метод из JSLIB
                FindAndSendReferalCodeToUnity(this.gameObject.name);
                Debug.Log("[REFERAL DEBUG] Вызвана JS функция для проверки параметров Telegram WebApp");
                
                // Проверяем localStorage через 0.5 секунды
                StartCoroutine(CheckLocalStorageAfterDelay(0.5f));
            }
            catch (Exception e)
            {
                Debug.LogError($"[REFERAL DEBUG] Ошибка при проверке Telegram WebApp параметров: {e.Message}");
            }
    #else
            Debug.Log("[REFERAL DEBUG] Проверка Telegram WebApp параметров недоступна (не WebGL или редактор)");
    #endif
        }
        
        // Новый метод для проверки localStorage после задержки
        private IEnumerator CheckLocalStorageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
    #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                if (HasSavedReferalCode())
                {
                    IntPtr codePtr = GetSavedReferalCode();
                    if (codePtr != IntPtr.Zero)
                    {
                        string referalCode = Marshal.PtrToStringUTF8(codePtr);
                        Debug.Log($"[REFERAL DEBUG] Получен реферальный код из localStorage: {referalCode}");
                        ProcessReferalCode(referalCode);
                        Marshal.FreeHGlobal(codePtr);
                    }
                }
                else
                {
                    Debug.Log("[REFERAL DEBUG] Реферальный код не найден в localStorage после проверки WebApp");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[REFERAL DEBUG] Ошибка при проверке localStorage: {e.Message}");
            }
    #endif
        }

        // Логирование отсутствия реферального кода в WebApp
        public void LogReferalWebAppNotFound(string message)
        {
            Debug.LogWarning($"[REFERAL DEBUG] WebApp: {message}");
        }

        // Добавляем метод для логирования полной initData
        public void LogFullInitData(string initData)
        {
            Debug.Log($"[REFERAL DEBUG] Получена полная initData: {initData}");
            
            // Пытаемся найти start_param в полной initData
            if (!string.IsNullOrEmpty(initData))
            {
                string[] parts = initData.Split('&');
                bool foundStartParam = false;
                
                Debug.Log($"[REFERAL DEBUG] Разделение initData на {parts.Length} частей:");
                for (int i = 0; i < parts.Length; i++)
                {
                    Debug.Log($"[REFERAL DEBUG] Часть {i}: {parts[i]}");
                    
                    if (parts[i].StartsWith("start_param="))
                    {
                        foundStartParam = true;
                        string startParam = parts[i].Substring("start_param=".Length);
                        if (!string.IsNullOrEmpty(startParam))
                        {
                            try
                            {
                                startParam = Uri.UnescapeDataString(startParam);
                                Debug.Log($"[REFERAL DEBUG] Извлечен start_param из полной initData: {startParam}");
                                ProcessReferalCode(startParam);
                                
                                // Дополнительное логирование для подтверждения получения реферального кода
                                Debug.Log($"[REFERAL DEBUG] ВНИМАНИЕ! ПОЛУЧЕН РЕФЕРАЛЬНЫЙ КОД: {startParam}");
                                Debug.Log($"[REFERAL DEBUG] Данный реферальный код будет отправлен на сервер в составе initData");
                                Debug.Log($"[REFERAL DEBUG] Если код не записывается в БД, проблема на стороне сервера или в его обработке API");
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"[REFERAL DEBUG] Ошибка декодирования start_param: {e.Message}");
                            }
                        }
                        break;
                    }
                }
                
                if (!foundStartParam)
                {
                    Debug.LogWarning("[REFERAL DEBUG] ВНИМАНИЕ! В initData НЕ НАЙДЕН параметр start_param!");
                    
                    // Если у нас есть сохраненный реферальный код, это ошибка - он должен был быть добавлен в initData
                    if (!string.IsNullOrEmpty(_pendingReferalCode))
                    {
                        Debug.LogError($"[REFERAL DEBUG] КРИТИЧЕСКАЯ ОШИБКА! У нас есть реферальный код ({_pendingReferalCode}), " +
                                     "но он НЕ был добавлен в initData! Проверьте работу GetTelegramWebAppInitDataWithReferal в JSLIB");
                    }
                }
            }
        }

        // Этот метод вызывается ИЗ JSLIB с помощью SendMessage
        public void ProcessReferalCodeFromWebApp(string referalCode)
        {
            Debug.Log($"[REFERAL DEBUG] ProcessReferalCodeFromWebApp вызван из JSLIB с кодом: {referalCode}");
            
            if (!string.IsNullOrEmpty(referalCode)) {
                Debug.Log($"[REFERAL DEBUG] Получен реферальный код из JSLIB: {referalCode}");
                
                // Сохраняем код для последующего использования после авторизации
                _pendingReferalCode = referalCode;
                
                // Выводим дополнительную отладочную информацию
                Debug.Log($"[REFERAL DEBUG] Сохранен реферальный код для последующей обработки: {_pendingReferalCode}");
                
                // Проверка хранилища
                if (PlayerPrefs.HasKey("last_referal_code")) {
                    Debug.Log($"[REFERAL DEBUG] В PlayerPrefs уже есть код: {PlayerPrefs.GetString("last_referal_code")}");
                }
                
                // Сохраняем в PlayerPrefs для большей надежности
                PlayerPrefs.SetString("last_referal_code", referalCode);
                PlayerPrefs.Save();
                Debug.Log("[REFERAL DEBUG] Код сохранен в PlayerPrefs");

                // ВАЖНО: Добавляем дополнительную проверку - есть ли этот код в initData
                Debug.Log("[REFERAL DEBUG] ВАЖНО: Убедитесь, что в отправляемой на сервер initData есть параметр start_param с этим кодом!");
                Debug.Log("[REFERAL DEBUG] Если бэкенд обрабатывает реферальные коды через initData, то код ДОЛЖЕН быть частью initData");
                Debug.Log("[REFERAL DEBUG] Запуск без параметра start_param в initData может быть причиной, почему реферальные коды не работают");
            } else {
                Debug.LogWarning("[REFERAL DEBUG] ProcessReferalCodeFromWebApp вызван с пустым кодом");
            }
        }
    }