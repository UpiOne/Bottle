using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FastBet : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BottleController bottleController;
    [SerializeField] private InputField betInputField;
    [SerializeField] private Button betButtonComponent; 
    [SerializeField] private GameObject betButtonGameObject; 

    [SerializeField] private TimerUI roundTimer;

    private ApiManager apiManager;
    private bool isCheckingBalance = false; 

    private void Awake()
    {
        apiManager = ApiManager.Instance;
        if (betButtonComponent == null && betButtonGameObject != null)
        {
            betButtonComponent = betButtonGameObject.GetComponent<Button>();
        }
        if (betButtonGameObject == null && betButtonComponent != null)
        {
            betButtonGameObject = betButtonComponent.gameObject;
        }
    }

    
    public void BetAndStart(int betAmount)
    {
        
        if (betAmount <= 0)
        {
            Debug.LogError("Bet amount must be positive.");
            PlayErrorSound();
            return;
        }
        StartBetProcess(betAmount);
    }

    
    public void AttemptManualBetAndStart()
    {
        if (string.IsNullOrWhiteSpace(betInputField.text))
        {
            Debug.LogWarning("Bet input field is empty.");
            PlayErrorSound();
            return;
        }

        if (int.TryParse(betInputField.text, out int amount))
        {
            if (amount <= 0)
            {
                Debug.LogError("Manual bet amount must be positive.");
                PlayErrorSound();
                return;
            }
            StartBetProcess(amount);
        }
        else
        {
            Debug.LogWarning("Invalid bet amount entered in input field.");
            PlayErrorSound();
        }
    }

    private void StartBetProcess(int betAmount)
    {
        if (isCheckingBalance)
        {
            Debug.LogWarning("Already checking balance, please wait.");
            return;
        }
        if (bottleController == null)
        {
            Debug.LogError("BottleController reference is missing!");
            return;
        }
        if (bottleController.FirstBet == true)
        {
            Debug.LogWarning("Cannot place bet now. A round is already in progress.");
            return;
        }
        if (bottleController.gameOver)
        {
            Debug.LogWarning("Cannot place bet now. Game is over. Please start a new game/round.");
            PlayErrorSound();
            return;
        }
        isCheckingBalance = true;
        SetBetControlsInteractable(false);
        if (apiManager != null && apiManager.IsLocalMode())
        {
            float localBalance = PlayerPrefs.GetFloat("local_coins", 1000);
            if (localBalance >= betAmount)
            {
                betInputField.text = betAmount.ToString();
                bottleController.DisableAllObjectsInArray();
                bool initiated = bottleController.PlaceBet(betAmount, SetupGameAfterBet);
                if (!initiated)
                {
                    Debug.LogError("Local balance OK, but local bet placement failed (PlaceBet returned false).");
                    PlayErrorSound();
                    SetBetControlsInteractable(true);
                }
            }
            else
            {
                Debug.LogWarning($"Insufficient local balance. Local has: {localBalance}, Bet requires: {betAmount}. Bet rejected.");
                PlayErrorSound();
                SetBetControlsInteractable(true);
            }
            isCheckingBalance = false;
            return;
        }
        Debug.Log($"Attempting to bet: {betAmount}. Requesting server balance check...");
        StartCoroutine(apiManager.GetUserBalance((serverBalance) => {
            Debug.Log($"Server balance received: {serverBalance}. Bet amount required: {betAmount}");
            if (serverBalance >= betAmount)
            {
                Debug.Log("Sufficient balance confirmed by server. Proceeding with bet.");
                betInputField.text = betAmount.ToString();
                bottleController.DisableAllObjectsInArray();
                bool initiated = bottleController.PlaceBet(betAmount, SetupGameAfterBet);
                if (!initiated)
                {
                    Debug.LogError("Server balance OK, but local bet placement failed (PlaceBet returned false).");
                    PlayErrorSound();
                    SetBetControlsInteractable(true);
                }
            }
            else
            {
                Debug.LogWarning($"Insufficient server balance. Server has: {serverBalance}, Bet requires: {betAmount}. Bet rejected.");
                PlayErrorSound();
                SetBetControlsInteractable(true);
            }
            isCheckingBalance = false;
        }));
    }



    private void SetupGameAfterBet()
    {
        
        bottleController.winChanceManager.CheckForDeviation();
        bottleController.currentMultiplier = bottleController.winChanceManager.CurrentMultiplier;
        bottleController.win = bottleController.winChanceManager.CalculateWinResult();
        roundTimer.StartNewRound();

        bottleController.scoreText.text = "";
        bottleController.scoreText.gameObject.SetActive(true);

        bottleController.transform.position = bottleController.startPos.position;
        bottleController.rb.velocity = Vector2.zero;
        bottleController.rb.angularVelocity = 0f;
        bottleController.rotationSpeed = 0;

        bottleController.bounceCount = 0;
        bottleController.jumpsCount = 0;
        bottleController.gameOver = false;

       
        if(betButtonGameObject != null) betButtonGameObject.SetActive(false); 
       
    }

    public void EnableBettingControls()
    {
         SetBetControlsInteractable(true);
         if (betButtonGameObject != null) betButtonGameObject.SetActive(true); 
         isCheckingBalance = false; 
    }

    
    private void SetBetControlsInteractable(bool interactable)
    {
        if (betButtonComponent != null)
        {
            betButtonComponent.interactable = interactable;
        }
       

        if (betInputField != null)
        {
            betInputField.interactable = interactable;
        }
    }

    private void PlayErrorSound()
    {
        if (bottleController != null && bottleController.errorSound != null)
        {
            Instantiate(bottleController.errorSound);
        }
        else
        {
            Debug.LogWarning("Error sound or BottleController reference missing.");
        }
    }

    public void ResetForNewRound() 
    {
        if (bottleController != null)
        {
            
            bottleController.ResetForNextThrow();
            EnableBettingControls();
        }
    }
}