using System;
using System.Drawing;
using HWKUltra.Vision.Abstractions;

namespace HWKUltra.Vision.Inference
{
    /// <summary>
    /// Simulated inference backend. Returns a 256-element int[] with the first 6 slots
    /// set to -1 (matching legacy "no detection" sentinel format), and zeros elsewhere.
    /// Used when main.dll is unavailable (developer workstations, unit tests).
    /// </summary>
    public class SimulatedInferenceEngine : IInferenceEngine
    {
        public bool IsAvailable => true;

        public void LoadModel()
        {
            // no-op
        }

        public int[] Predict(Bitmap bmp)
        {
            int[] resultlist = new int[256];
            for (int i = 0; i < 6; i++) resultlist[i] = -1;
            return resultlist;
        }
    }
}
