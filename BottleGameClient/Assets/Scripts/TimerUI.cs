using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class TimerUI : MonoBehaviour
{
    [Header("Countdown Settings")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownDuration = 1.2f;
    [SerializeField] private Vector3 punchScale = new Vector3(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color goColor = new Color(0.2f, 1f, 0.2f);

    [Header("Round Timer Settings")]
    [SerializeField] private Slider fuseSlider;
    [SerializeField] private ParticleSystem flameParticle;
    [SerializeField] private float roundDuration = 10f;
    [SerializeField] private Gradient fuseGradient;

    [Header("Particle Settings")]
    [SerializeField] private float flameIntensity = 1.5f;
    [SerializeField] private float colorSmoothness = 0.3f;
    [SerializeField] private Vector2 particleSizeRange = new Vector2(0.3f, 0.7f);

    [Header("Independent Timer Settings")]
    [SerializeField] private float independentTimerDuration = 12f; // Slightly longer than roundDuration

    private BottleController bottleController;
    private UnityEngine.UI.Image sliderFill;
    private float currentRoundTime;
    private bool isRoundActive;
    public bool TimerStart;
    private bool hasTimerStarted = false;

    private void Awake()
    {
        InitializeComponents();
        ResetVisuals();
        TimerStart = false;
    }

    private void InitializeComponents()
    {
        bottleController = FindObjectOfType<BottleController>();
        sliderFill = fuseSlider.fillRect.GetComponent<Image>();
    }

    private void ResetVisuals()
    {
        fuseSlider.gameObject.SetActive(false);
        flameParticle.Stop();
        countdownText.gameObject.SetActive(false);
        countdownText.alpha = 1f;
    }

    public void StartNewRound()
    {
        if (bottleController.bet > 0 && !hasTimerStarted)
        {
            hasTimerStarted = true;
            StartCoroutine(CountdownSequence());
            StartCoroutine(IndependentTimerCoroutine()); // Start the independent timer
        }
    }

    private IEnumerator CountdownSequence()
    {
        bottleController.SetInputEnabled(false);
        countdownText.gameObject.SetActive(true);

        yield return AnimateCountdownNumber("3", 0.3f);
        yield return AnimateCountdownNumber("2", 0.25f);
        yield return AnimateCountdownNumber("1", 0.2f);
        yield return AnimateGoText();

        StartCoroutine(RoundTimerCoroutine());
    }

    private IEnumerator AnimateCountdownNumber(string number, float duration)
    {
        countdownText.text = number;
        yield return PunchTextAnimation(duration);
        yield return new WaitForSeconds(duration * 0.5f);
    }

    private IEnumerator AnimateGoText()
    {
        TimerStart = true;
        countdownText.text = "GO!";
        countdownText.color = goColor;
        yield return PunchTextAnimation(0.4f);
        yield return FadeText(0.2f);
        ResetTextVisuals();
    }

    private IEnumerator PunchTextAnimation(float duration)
    {
        Vector3 originalScale = countdownText.transform.localScale;
        float timer = 0f;

        while (timer < duration)
        {
            float progress = timer / duration;
            countdownText.transform.localScale = originalScale +
                punchScale * Mathf.Sin(progress * Mathf.PI);

            timer += Time.deltaTime;
            yield return null;
        }
        countdownText.transform.localScale = originalScale;
    }

    private IEnumerator FadeText(float duration)
    {
        float startAlpha = countdownText.alpha;
        float timer = 0f;

        while (timer < duration)
        {
            countdownText.alpha = Mathf.Lerp(startAlpha, 0f, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void ResetTextVisuals()
    {
        countdownText.gameObject.SetActive(false);
        countdownText.color = Color.white;
        countdownText.alpha = 1f;
    }

    private IEnumerator RoundTimerCoroutine()
    {
        InitializeRoundTimer();
        flameParticle.Play();

        float startTime = Time.time;
        while (Time.time - startTime < currentRoundTime)
        {
            UpdateTimerVisuals(startTime);
            yield return null;
        }

        StartCoroutine(FinishRound());
    }


    private IEnumerator IndependentTimerCoroutine()
    {
        yield return new WaitForSeconds(independentTimerDuration);
        if (isRoundActive)
        {
            Debug.LogWarning("Independent timer triggered failure.");
            StartCoroutine(FinishRound());
        }
    }


    private void InitializeRoundTimer()
    {
        currentRoundTime = roundDuration;
        fuseSlider.maxValue = currentRoundTime;
        fuseSlider.value = currentRoundTime;
        fuseSlider.gameObject.SetActive(true);
        bottleController.SetInputEnabled(true);
        isRoundActive = true;
    }

    private void UpdateTimerVisuals(float startTime)
    {
        float remainingTime = currentRoundTime - (Time.time - startTime);
        float normalizedTime = remainingTime / currentRoundTime;

        UpdateSlider(normalizedTime);
        UpdateParticles(normalizedTime);
    }

    private void UpdateSlider(float normalizedTime)
    {
        fuseSlider.value = currentRoundTime * normalizedTime;
        sliderFill.color = fuseGradient.Evaluate(normalizedTime);
    }

    private void UpdateParticles(float normalizedTime)
    {
        Color particleColor = EvaluateAdjustedColor(normalizedTime);
        var mainModule = flameParticle.main;

        mainModule.startColor = new ParticleSystem.MinMaxGradient(
            particleColor,
            Color.Lerp(particleColor, Color.white, colorSmoothness)
        );

        UpdateParticlePositionAndSize(normalizedTime);
    }

    private Color EvaluateAdjustedColor(float time)
    {
        Color baseColor = fuseGradient.Evaluate(time);
        return new Color(
            baseColor.r * flameIntensity,
            baseColor.g * flameIntensity,
            baseColor.b * flameIntensity,
            baseColor.a
        );
    }

    private void UpdateParticlePositionAndSize(float normalizedTime)
    {
        Vector3 particlePos = fuseSlider.handleRect.position;
        flameParticle.transform.position = particlePos;

        var sizeModule = flameParticle.sizeOverLifetime;
        float sizeMultiplier = Mathf.Lerp(
            particleSizeRange.x,
            particleSizeRange.y,
            normalizedTime
        );

        sizeModule.size = new ParticleSystem.MinMaxCurve(
            sizeMultiplier * 0.5f,
            sizeMultiplier
        );
    }

    private IEnumerator FinishRound()
    {
        if (bottleController == null)
        {
            Debug.LogError("bottleController is not assigned.");
            yield break;
        }
        if (bottleController.isGameEnding) yield break;
        bottleController.isGameEnding = true;

        bottleController.isGameEnding = true;

        if (bottleController.loseSound != null)
        {
            Instantiate(bottleController.loseSound);
        }

        bottleController.sidewaysTimer = 0f;

        bottleController.lastBet = bottleController.bet;
        float finalAngle = bottleController.NormalizeAngle(transform.eulerAngles.z);
        bool lastChanceWin = finalAngle <= 15f || finalAngle >= 345f;

        bottleController.win = false;
        bottleController.gameOver = true;

        bottleController.DarkBG.SetActive(true);
        bottleController.youWinPopUp.SetActive(true);
        TextMeshProUGUI lossText = bottleController.youWinPopUp.GetComponent<TextMeshProUGUI>();

        if (lossText != null)
        {
            lossText.enableVertexGradient = true;
            lossText.color = Color.white;

            lossText.colorGradient = new VertexGradient(
                new Color(1f, 0.713f, 0.216f),    //  (#FFB637)
                new Color(1f, 0.713f, 0.216f),    //  (������)
                new Color(1f, 0.145f, 0.259f),    //  (#FF2542)
                new Color(1f, 0.145f, 0.259f)     //  (������)
            );

            lossText.text = "FAILED";
            lossText.ForceMeshUpdate();
        }

        yield return StartCoroutine(bottleController.AnimatePopupJump("FAILED", new VertexGradient(
            new Color(1f, 0.713f, 0.216f),
            new Color(1f, 0.713f, 0.216f),
            new Color(1f, 0.145f, 0.259f),
            new Color(1f, 0.145f, 0.259f)
        )));

        isRoundActive = false;
        fuseSlider.gameObject.SetActive(false);
        flameParticle.Stop();
        TimerStart = false;

        StartCoroutine(bottleController.HandleLoss(""));
        StartCoroutine(ReloadScene());
    }

    private IEnumerator ReloadScene()
    {
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(0);

    }

    public void SetRoundDuration(float duration)
    {
        //   roundDuration = Mathf.Clamp(duration, durationLimits.x, durationLimits.y);
    }
}
