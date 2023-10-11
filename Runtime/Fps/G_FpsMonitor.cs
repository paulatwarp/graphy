/* ---------------------------------------
 * Author:          Martin Pane (martintayx@gmail.com) (@martinTayx)
 * Contributors:    https://github.com/Tayx94/graphy/graphs/contributors
 * Project:         Graphy - Ultimate Stats Monitor
 * Date:            15-Dec-17
 * Studio:          Tayx
 *
 * Git repo:        https://github.com/Tayx94/graphy
 *
 * This project is released under the MIT license.
 * Attribution is not required, but it is always welcomed!
 * -------------------------------------*/

using UnityEngine;

namespace Tayx.Graphy.Fps
{
    public class StatBuffer
    {
        float expected;
        float [] values;
        float offsetSum;
        float offsetVarianceSum;
        int front;
        int back;
        int size;

        // shift is the expected mean of the values
        public StatBuffer(int length, float expected)
        {
            this.expected = expected;
            values = new float[length];
            size = 0;
            front = 0;
            back = 0;
            offsetSum = 0;
            offsetVarianceSum = 0;
        }

        public void  Push(float value)
        {
            if (size == values.Length)
            {
                Pop();
            }
            else
            {
                values[back] = value;
                back = (back + 1) % values.Length;
                size++;
                offsetSum += value - expected;
                offsetVarianceSum += (value - expected) * (value - expected);
            }
        }

        void Pop()
        {
            float value = values[front];
            size--;
            front = (front + 1) % values.Length;
            offsetSum -= value - expected;
            offsetVarianceSum -= (value - expected) * (value - expected);
        }

        public float Mean()
        {
            if (size > 0)
            {
                return expected + offsetSum / size;
            }
            else
            {
                return 0;
            }
        }

        public float Variance()
        {
            if (size > 1)
            {
                return (offsetVarianceSum - offsetSum * offsetSum / size) / (size - 1);
            }
            else
            {
                return 0;
            }   
        }

        public float StandardDeviation()
        {
            if (size > 0)
            {
                float variance = Variance();
                if (variance >= 0)
                {
                    return Mathf.Sqrt(variance);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
    }

    public class G_FpsMonitor : MonoBehaviour
    {
        #region Variables -> Private

        private short m_fpsSamplesCapacity = 60;
        StatBuffer fpsSamples;
        StatBuffer cpuSamples;
        StatBuffer gpuSamples;

        FrameTiming [] frameTimings = new FrameTiming[1];

        #endregion

        #region Properties -> Public

        public short CurrentFPS { get; private set; } = 0;
        public short AverageFPS { get; private set; } = 0;
        public short OnePercentFPS { get; private set; } = 0;
        public short Zero1PercentFps { get; private set; } = 0;
        public float CurrentCPU { get; private set; } = 0;
        public float AverageCPU { get; private set; } = 0;
        public float OnePercentCPU { get; private set; } = 0;
        public float Zero1PercentCpu { get; private set; } = 0;
        public float CurrentGPU { get; private set; } = 0;
        public float AverageGPU { get; private set; } = 0;
        public float OnePercentGPU { get; private set; } = 0;
        public float Zero1PercentGpu { get; private set; } = 0;

        #endregion

        #region Methods -> Unity Callbacks

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            FrameTimingManager.CaptureFrameTimings();
            FrameTimingManager.GetLatestTimings(1, frameTimings);
            float cpuTime = (float)frameTimings[0].cpuFrameTime;
            float gpuTime = (float)frameTimings[0].gpuFrameTime;

            // Update fps
            CurrentFPS = (short) Mathf.RoundToInt( 1.0f / Time.deltaTime );
            CurrentCPU = cpuTime;
            CurrentGPU = gpuTime;

            fpsSamples.Push( CurrentFPS );
            cpuSamples.Push( cpuTime );
            gpuSamples.Push( gpuTime );

            // 2.58 is the z-score for 99% confidence interval
            // 3.29 is the z-score for 99.9% confidence interval

            AverageCPU = cpuSamples.Mean();
            float standardDeviation  = cpuSamples.StandardDeviation();
            OnePercentCPU = AverageCPU + standardDeviation * 2.58f;
            Zero1PercentCpu = AverageCPU + standardDeviation * 3.29f;

            AverageGPU = gpuSamples.Mean();
            standardDeviation = gpuSamples.StandardDeviation();
            OnePercentGPU = AverageGPU + standardDeviation * 2.58f;
            Zero1PercentGpu = AverageGPU + standardDeviation * 3.29f;

            AverageFPS = (short) fpsSamples.Mean();
            standardDeviation = fpsSamples.StandardDeviation();
            OnePercentFPS = (short) Mathf.Max(AverageFPS - standardDeviation * 2.58f, 0f);
            Zero1PercentFps = (short) Mathf.Max(AverageFPS - standardDeviation * 3.29f, 0f);
        }

        #endregion

        #region Methods -> Public

        public void UpdateParameters()
        {
        }

        #endregion

        #region Methods -> Private

        private void Init()
        {
            fpsSamples = new StatBuffer(m_fpsSamplesCapacity, 30.0f);
            cpuSamples = new StatBuffer(m_fpsSamplesCapacity, 33.3f);
            gpuSamples = new StatBuffer(m_fpsSamplesCapacity, 33.3f);
        }

        #endregion
    }
}
