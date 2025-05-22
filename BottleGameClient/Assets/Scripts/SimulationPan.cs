using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimulationPan : MonoBehaviour
{
    public GameObject pan;
    public InputField NumberSimulations;
    public InputField NumberMaxAirJumps;
    public InputField SimulationBalance;
    public InputField TheAverageInterestRateOnTheBalance;
    public TextMeshProUGUI textResultAllInfo;
    public Button startSimulationButton;

    private class SimulationStats
    {
        public float totalFinalBalance;
        public float minBalance = float.MaxValue;
        public float maxBalance = float.MinValue;
        public int totalWins;
        public int totalLosses;
    }

    private void Start()
    {
        startSimulationButton.onClick.AddListener(() => StartCoroutine(RunSimulationCoroutine()));
    }

    private IEnumerator RunSimulationCoroutine()
    {
        if (!TryParseInputs(out int numSimulations, out int maxAirJumps, out float initialBalance, out float averageBetPercent))
        {
            textResultAllInfo.text = "������ �����! ��������� ��������";
            yield break;
        }

        GameObject tempGO = new GameObject("TempWinChanceManager");
        WinChanceManager tempWCM = tempGO.AddComponent<WinChanceManager>();
        tempWCM.LoadAllSettings();

        SimulationStats stats = new SimulationStats();
        textResultAllInfo.text = "����� ���������...";

        for (int sim = 0; sim < numSimulations; sim++)
        {
            yield return SimulateSingleSession(tempWCM, initialBalance, maxAirJumps, averageBetPercent, stats);

            textResultAllInfo.text = $"��������: {sim + 1}/{numSimulations}\n" +
                                    $"������� ������� ������: {stats.totalFinalBalance / (sim + 1):F2}";

            yield return new WaitForSeconds(0.3f); 
        }

        DisplayResults(stats, numSimulations);
        Destroy(tempGO);
    }

    private bool TryParseInputs(out int numSimulations, out int maxAirJumps, out float initialBalance, out float averageBetPercent)
    {
        numSimulations = maxAirJumps = 0;
        initialBalance = averageBetPercent = 0f;

        try
        {
            numSimulations = Mathf.Clamp(int.Parse(NumberSimulations.text), 1, 1000);
            maxAirJumps = Mathf.Clamp(int.Parse(NumberMaxAirJumps.text), 1, 1000);
            initialBalance = Mathf.Clamp(float.Parse(SimulationBalance.text), 1f, 1000000f);
            averageBetPercent = Mathf.Clamp(float.Parse(TheAverageInterestRateOnTheBalance.text) / 100f, 0.01f, 0.99f);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private IEnumerator SimulateSingleSession(WinChanceManager wcm, float initialBalance, int maxAirJumps, float betPercent, SimulationStats stats)
    {
        float currentBalance = initialBalance;
        float sessionMin = currentBalance;
        float sessionMax = currentBalance;

        wcm.CurrentWinChance = wcm.baseWinChance;
        wcm.consecutiveLosses = 0;
        wcm.totalGamesPlayed = 0;
        wcm.nextBoostGame = UnityEngine.Random.Range(wcm.minGamesForBoost, wcm.maxGamesForBoost);

        for (int jump = 0; jump < maxAirJumps; jump++)
        {
            float betAmount = currentBalance * betPercent;
            if (betAmount <= 0 || betAmount > currentBalance) break;

            wcm.CalculateWinResult();
            bool win = wcm._predeterminedWin;
            float multiplier = win ? wcm.GetPredeterminedMultiplier() : 1f;

            currentBalance += win ? betAmount * (multiplier - 1f) : -betAmount;

            if (win) stats.totalWins++;
            else stats.totalLosses++;

            float adjustment = win ?
                -UnityEngine.Random.Range(wcm.chanceDecreasePerThrowMin, wcm.chanceDecreasePerThrowMax) :
                wcm.chanceDecreasePerThrowMax;

            wcm.CurrentWinChance = Mathf.Clamp(
                wcm.CurrentWinChance + adjustment,
                wcm.minWinChance,
                wcm.maxWinChance
            );

            UpdateDynamicParameters(wcm, win);

            if (currentBalance <= 0f) break;

            sessionMin = Mathf.Min(sessionMin, currentBalance);
            sessionMax = Mathf.Max(sessionMax, currentBalance);
        }

        stats.totalFinalBalance += currentBalance;
        stats.minBalance = Mathf.Min(stats.minBalance, sessionMin);
        stats.maxBalance = Mathf.Max(stats.maxBalance, sessionMax);

        yield return null;
    }

    private void UpdateDynamicParameters(WinChanceManager wcm, bool win)
    {
        wcm.totalGamesPlayed++;

        if (!win)
        {
            wcm.consecutiveLosses++;
            if (wcm.consecutiveLosses >= wcm.controlGoal && UnityEngine.Random.value < wcm.hardResetChance)
            {
                wcm.CurrentWinChance = wcm.baseWinChance;
                wcm.consecutiveLosses = 0;
            }
        }
        else
        {
            wcm.consecutiveLosses = 0;
        }

        if (wcm.totalGamesPlayed >= wcm.nextBoostGame && UnityEngine.Random.value < wcm.winBoostChance)
        {
            wcm.CurrentWinChance *= wcm.winBoostMultiplier;
            wcm.nextBoostGame = wcm.totalGamesPlayed +
                UnityEngine.Random.Range(wcm.minGamesForBoost, wcm.maxGamesForBoost);
        }
    }

    private void CopySettings(WinChanceManager source, WinChanceManager dest)
    {
        var fields = typeof(WinChanceManager).GetFields();
        foreach (var field in fields)
        {
            if (field.IsPublic)
            {
                field.SetValue(dest, field.GetValue(source));
            }
        }

        dest.virtualReelStrip = new List<int>(source.virtualReelStrip);
        dest.winChanceCurve = new AnimationCurve(source.winChanceCurve.keys);
    }
    private void DisplayResults(SimulationStats stats, int totalSimulations)
    {
        float avgFinal = stats.totalFinalBalance / totalSimulations;
        float winRate = (float)stats.totalWins / (stats.totalWins + stats.totalLosses) * 100f;

        textResultAllInfo.text =
            $"����� {totalSimulations} ���������:\n" +
            $"������� �������� ������: {avgFinal:F2}\n" +
            $"����������� ������: {stats.minBalance:F2}\n" +
            $"������������ ������: {stats.maxBalance:F2}\n" +
            $"����� �����: {stats.totalWins} ({winRate:F1}%)\n" +
            $"����� ���������: {stats.totalLosses}";
    }

    public void OpenPanel()
    {
        pan.SetActive(true);
        textResultAllInfo.text = "����� � ���������";
    }

    public void ClosePanel()
    {
        pan.SetActive(false);
    }
}