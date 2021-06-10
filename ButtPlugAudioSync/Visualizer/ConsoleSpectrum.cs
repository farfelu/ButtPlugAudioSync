using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using CSCore.DSP;

namespace WinformsVisualization.Visualization
{
    public class ConsoleSpectrum : SpectrumBase
    {

        public ConsoleSpectrum(FftSize fftSize)
        {
            FftSize = fftSize;
            SpectrumResolution = 20;
        }

        private bool _hasMapped = false;

        public float[] GetSpectrumPoints(float height, float[] fftBuffer)
        {
            if (!_hasMapped)
            {
                base.UpdateFrequencyMapping();
                _hasMapped = true;
            }

            SpectrumPointData[] dats = CalculateSpectrumPoints(height, fftBuffer);
            float[] res = new float[dats.Length];
            for (int i = 0; i < dats.Length; i++)
            {
                res[i] = (float)dats[i].Value;
            }

            return res;
        }
    }
}