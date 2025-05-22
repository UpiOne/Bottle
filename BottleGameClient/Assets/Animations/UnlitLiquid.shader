Shader "UnlitLiquid"
{
    Properties
    {
        // === Основные параметры жидкости ===
        _Tint ("Tint", Color) = (1,1,1,1)           // Цвет жидкости (RGBA)
        _MainTex ("Texture", 2D) = "white" {}       // Основная текстура жидкости
        _FillAmount ("Fill Amount", Range(-1,1)) = 0.0  // Уровень заполнения контейнера (-1=пустой, 1=полный)
        
        // === Динамические эффекты колебаний (скрыты в инспекторе) ===
        [HideInInspector] _WobbleX ("WobbleX", Range(-1,1)) = 0.0   // Колебание по оси X
        [HideInInspector] _WobbleZ ("WobbleZ", Range(-1,1)) = 0.0   // Колебание по оси Z

        // === Цветовые настройки ===
        _TopColor ("Top Color", Color) = (1,1,1,1)       // Цвет верхней поверхности жидкости
        _FoamColor ("Foam Line Color", Color) = (1,1,1,1) // Цвет пенной линии
        _Rim ("Foam Line Width", Range(0,0.1)) = 0.0     // Ширина пенной линии
        _RimColor ("Rim Color", Color) = (1,1,1,1)       // Цвет краевого подсвечивания
        _RimPower ("Rim Power", Range(0,100)) = 0.0      // Сила краевого эффекта

        // === Параметры волн ===
        _WaveSpeed ("Wave Speed", Float) = 1.0          // Скорость распространения волн
        _WaveFrequency ("Wave Frequency", Float) = 10.0 // Частота волн
        [HideInInspector] _DynamicWaveAmplitude ("Dynamic Wave Amplitude", Float) = 0.0 // Амплитуда динамических волн
    }

    SubShader
    {
        Tags {"Queue"="Transparent"  "DisableBatching" = "True" }

        Pass
        {
         Zwrite off
         Cull Off 
         AlphaToMask off 
         Blend SrcAlpha OneMinusSrcAlpha

         CGPROGRAM

         #pragma vertex vert
         #pragma fragment frag
         #pragma multi_compile_fog

         #include "UnityCG.cginc"

         struct appdata
         {
           float4 vertex : POSITION;
           float2 uv : TEXCOORD0;
           float3 normal : NORMAL;
         };

         struct v2f
         {
            float2 uv : TEXCOORD0;
            UNITY_FOG_COORDS(1)
            float4 vertex : SV_POSITION;
            float3 viewDir : COLOR;
            float3 normal : COLOR2;
            float fillEdge : TEXCOORD2;
            float objectLocalWaveCoord : TEXCOORD3;
         };

         sampler2D _MainTex;
         float4 _MainTex_ST;
         float _FillAmount, _WobbleX, _WobbleZ;
         float4 _TopColor, _RimColor, _FoamColor, _Tint;
         float _Rim, _RimPower;
         
         float _WaveSpeed;
         float _WaveFrequency;
         float _DynamicWaveAmplitude;

         // Поворот вершины вокруг оси Y на заданные градусы
         float4 RotateAroundYInDegrees (float4 vertex, float degrees)
         {
            float alpha = degrees * UNITY_PI / 180;
            float sina, cosa;
            sincos(alpha, sina, cosa);
            float2x2 m = float2x2(cosa, sina, -sina, cosa);
            return float4(vertex.yz , mul(m, vertex.xz)).xzyw ;
         }
         
         v2f vert (appdata v)
         {
            v2f o;

            // Преобразование координат в экранное пространство
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            UNITY_TRANSFER_FOG(o,o.vertex);
            
            // Получаем мировые координаты вершины
            float3 worldPos = mul (unity_ObjectToWorld, v.vertex.xyz);
             
            // Вычисляем векторы для колебаний по X и Z осям
            float3 worldPosForXWobbleCalc = RotateAroundYInDegrees(float4(worldPos,0),360); 
            float3 worldPosForZWobbleCalc = float3(worldPosForXWobbleCalc.y, worldPosForXWobbleCalc.z, worldPosForXWobbleCalc.x); 
             
            // Применяем динамическое колебание
            float3 worldPosAdjusted = worldPos + (worldPosForXWobbleCalc * _WobbleX) + (worldPosForZWobbleCalc * _WobbleZ);
             
            // Вычисляем границу заполнения с учётом колебаний
            o.fillEdge = worldPosAdjusted.y + _FillAmount; 
            
            // Направление взгляда и нормаль вершины
            o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
            o.normal = v.normal;
             
            // Сохраняем координату для расчёта волн
            o.objectLocalWaveCoord = v.vertex.x; 
             
            return o;
         }

         fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
         {
           // Базовый цвет с учетом текстуры и тинта
           fixed4 col = tex2D(_MainTex, i.uv) * _Tint;
           UNITY_APPLY_FOG(i.fogCoord, col);

           // Краевой эффект (rim lighting)
           float dotProduct = 1 - pow(dot(i.normal, i.viewDir), _RimPower);
           float4 RimResult = smoothstep(0.5, 1.0, dotProduct);
           RimResult *= _RimColor;
             
           // Расчёт волны по оси X
           float waveOffset = sin(i.objectLocalWaveCoord * _WaveFrequency + _Time.y * _WaveSpeed) * _DynamicWaveAmplitude;
           float currentFillEdge = i.fillEdge + waveOffset; 
             
           // Пенная линия (foam line)
           float4 foam = (step(currentFillEdge, 0.5) - step(currentFillEdge, (0.5 - _Rim)));
           float4 foamColored = foam * (_FoamColor * 0.9);

           // Основная масса жидкости
           float4 result = step(currentFillEdge, 0.5) - foam; 
           float4 resultColored = result * col;
           
           // Комбинируем все элементы
           float4 finalResult = resultColored + foamColored;
           finalResult.rgb += RimResult;

           // Цвет верхней поверхности
           float4 topColor = _TopColor * (foam + result);
           
           // Возвращаем цвет в зависимости от направления полигона
           return facing > 0 ? finalResult: topColor;
         }
         ENDCG
        }
    }
}