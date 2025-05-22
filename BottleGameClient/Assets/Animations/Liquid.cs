using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс для реализации эффекта жидкости с динамическими колебаниями
/// Взаимодействует с шейдером UnlitLiquid для визуализации жидкости
/// </summary>
[ExecuteInEditMode]
public class Liquid : MonoBehaviour
{
    // Режимы обновления (с учетом времени или без)
    public enum UpdateMode { Normal, UnscaledTime }
    public UpdateMode updateMode;

    [SerializeField]
    // Максимальное возможное колебание (чувствительность к движению)
    float MaxWobble = 0.03f;

    [SerializeField]
    // Скорость основной синусоидальной волны колебания
    float WobbleSpeedMove = 1f;

    [SerializeField]
    // Уровень заполнения контейнера (от 0 до 1)
    float fillAmount = 0.5f;

    [SerializeField]
    // Скорость затухания колебаний (восстановления)
    float Recovery = 1f;

    [SerializeField]
    // Толщина жидкости (используется для расчета интенсивности колебаний)
    float Thickness = 1f;

    // Коэффициент компенсации формы контейнера (0-1)
    [Range(0, 1)]
    public float CompensateShapeAmount;

    [SerializeField]
    // Максимальная амплитуда динамических волн на поверхности
    float MaxDynamicWaveAmplitude = 0.01f;

    [SerializeField]
    // Ссылка на меш (сетку) контейнера
    Mesh mesh;

    [SerializeField]
    // Ссылка на рендерер контейнера
    Renderer rend;

    // Предыдущие позиции и вращения для расчета движения
    Vector3 pos;
    Vector3 lastPos;
    Vector3 velocity;
    Quaternion lastRot;
    Vector3 angularVelocity;

    // Параметры колебаний
    float wobbleAmountX; // Итоговое колебание по оси X
    float wobbleAmountZ; // Итоговое колебание по оси Z
    float wobbleAmountToAddX; // Накопленное потенциальное колебание по X
    float wobbleAmountToAddZ; // Накопленное потенциальное колебание по Z
    float pulse;
    float sinewave;
    float time = 0.5f;
    Vector3 comp;

    // ID шейдерных свойств для эффективного доступа
    private int _WobbleX_ID;
    private int _WobbleZ_ID;
    private int _FillAmount_ID;
    private int _DynamicWaveAmplitude_ID;

    void Start()
    {
        GetMeshAndRend();
        CacheShaderPropertyIDs();
    }

    private void OnValidate()
    {
        GetMeshAndRend();
    }

    /// <summary>
    /// Кэширует ID шейдерных свойств для быстрого доступа
    /// </summary>
    void CacheShaderPropertyIDs()
    {
        if (rend != null && rend.sharedMaterial != null)
        {
            _WobbleX_ID = Shader.PropertyToID("_WobbleX");
            _WobbleZ_ID = Shader.PropertyToID("_WobbleZ");
            _FillAmount_ID = Shader.PropertyToID("_FillAmount");
            _DynamicWaveAmplitude_ID = Shader.PropertyToID("_DynamicWaveAmplitude");
        }
    }

    /// <summary>
    /// Получает компоненты Mesh и Renderer, если они не назначены вручную
    /// </summary>
    void GetMeshAndRend()
    {
        if (mesh == null)
        {
            mesh = GetComponent<MeshFilter>()?.sharedMesh;
        }
        if (rend == null)
        {
            rend = GetComponent<Renderer>();
        }
        if (rend != null && rend.sharedMaterial != null && (_WobbleX_ID == 0))
        {
            CacheShaderPropertyIDs();
        }
    }

    void Update()
    {
        if (rend == null || rend.sharedMaterial == null || mesh == null)
        {
            GetMeshAndRend();
            if (rend == null || rend.sharedMaterial == null || mesh == null) return;
        }

        float deltaTime = 0;
        switch (updateMode)
        {
            case UpdateMode.Normal:
                deltaTime = Time.deltaTime;
                break;
            case UpdateMode.UnscaledTime:
                deltaTime = Time.unscaledDeltaTime;
                break;
        }

        time += deltaTime;

        if (deltaTime != 0)
        {
            // Уменьшение колебаний со временем (эффект затухания)
            wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, (deltaTime * Recovery));
            wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, (deltaTime * Recovery));

            // Расчет линейной и угловой скорости
            velocity = (lastPos - transform.position) / deltaTime;
            angularVelocity = GetAngularVelocity(lastRot, transform.rotation);

            // Добавление текущего движения к потенциальному колебанию
            wobbleAmountToAddX += Mathf.Clamp((velocity.x + (velocity.y * 0.2f) + angularVelocity.z + angularVelocity.y) * MaxWobble, -MaxWobble, MaxWobble);
            wobbleAmountToAddZ += Mathf.Clamp((velocity.z + (velocity.y * 0.2f) + angularVelocity.x + angularVelocity.y) * MaxWobble, -MaxWobble, MaxWobble);

            // Ограничение максимального значения накопленного колебания
            wobbleAmountToAddX = Mathf.Clamp(wobbleAmountToAddX, -MaxWobble, MaxWobble);
            wobbleAmountToAddZ = Mathf.Clamp(wobbleAmountToAddZ, -MaxWobble, MaxWobble);
            
            // Создание синусоидальной волны для основного эффекта плеска
            pulse = 2 * Mathf.PI * WobbleSpeedMove;
            sinewave = Mathf.Lerp(sinewave, Mathf.Sin(pulse * time), deltaTime * Mathf.Clamp(velocity.magnitude + angularVelocity.magnitude, Thickness, 10));

            // Расчет итогового колебания с учетом синусоиды
            wobbleAmountX = wobbleAmountToAddX * sinewave;
            wobbleAmountZ = wobbleAmountToAddZ * sinewave;

            // Расчет уровня "возбуждения" жидкости для динамических волн
            float agitationLevel = (Mathf.Abs(wobbleAmountToAddX) + Mathf.Abs(wobbleAmountToAddZ)) * 0.5f;
            float normalizedAgitation = 0f;
            
            if (MaxWobble > 0.00001f)
            {
                normalizedAgitation = Mathf.Clamp01(agitationLevel / MaxWobble);
            }
            
            float currentDynamicWaveAmplitude = normalizedAgitation * MaxDynamicWaveAmplitude;

            // Передача параметров в шейдер
            rend.sharedMaterial.SetFloat(_WobbleX_ID, wobbleAmountX);
            rend.sharedMaterial.SetFloat(_WobbleZ_ID, wobbleAmountZ);
            rend.sharedMaterial.SetFloat(_DynamicWaveAmplitude_ID, currentDynamicWaveAmplitude);
        }

        // Обновление позиции жидкости
        UpdatePos(deltaTime);

        // Сохранение предыдущих позиции и вращения
        lastPos = transform.position;
        lastRot = transform.rotation;
    }

    /// <summary>
    /// Обновляет позицию жидкости с учетом компенсации формы контейнера
    /// </summary>
    void UpdatePos(float deltaTime)
    {
        Vector3 worldPos = transform.TransformPoint(new Vector3(mesh.bounds.center.x, mesh.bounds.center.y, mesh.bounds.center.z));
        
        if (CompensateShapeAmount > 0)
        {
            if (deltaTime != 0)
            {
                comp = Vector3.Lerp(comp, (worldPos - new Vector3(0, GetLowestPoint(), 0)), deltaTime * 10);
            }
            else
            {
                comp = (worldPos - new Vector3(0, GetLowestPoint(), 0));
            }
            pos = worldPos - transform.position - new Vector3(0, fillAmount - (comp.y * CompensateShapeAmount), 0);
        }
        else
        {
            pos = worldPos - transform.position - new Vector3(0, fillAmount, 0);
        }
    }

    /// <summary>
    /// Расчет угловой скорости на основе предыдущего и текущего вращения
    /// </summary>
    Vector3 GetAngularVelocity(Quaternion foreLastFrameRotation, Quaternion lastFrameRotation)
    {
        var q = lastFrameRotation * Quaternion.Inverse(foreLastFrameRotation);
        
        if (Mathf.Abs(q.w) > 1023.5f / 1024.0f)
            return Vector3.zero;
        
        float gain;
        float angle;
        
        if (q.w < 0.0f)
        {
            angle = Mathf.Acos(-q.w);
            gain = -2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
        }
        else
        {
            angle = Mathf.Acos(q.w);
            gain = 2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
        }
        
        Vector3 angularVel = new Vector3(q.x * gain, q.y * gain, q.z * gain);

        if (float.IsNaN(angularVel.x) || float.IsNaN(angularVel.y) || float.IsNaN(angularVel.z))
        {
            return Vector3.zero;
        }
        
        return angularVel;
    }

    /// <summary>
    /// Нахождение самой нижней точки меша для корректного расчета уровня жидкости
    /// </summary>
    float GetLowestPoint()
    {
        if (mesh == null) return transform.position.y;

        float lowestY = float.MaxValue;
        Vector3 lowestVert = transform.position;
        Vector3[] vertices = mesh.vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldVertexPos = transform.TransformPoint(vertices[i]);
            
            if (worldVertexPos.y < lowestY)
            {
                lowestY = worldVertexPos.y;
                lowestVert = worldVertexPos;
            }
        }
        
        return lowestVert.y;
    }
}