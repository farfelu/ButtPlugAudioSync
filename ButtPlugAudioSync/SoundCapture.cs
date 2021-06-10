using CSCore;
using CSCore.SoundIn;
using CSCore.Codecs.WAV;
using CSCore.DSP;
using CSCore.Streams;
using System;
using WinformsVisualization.Visualization;
using System.Linq;

namespace ButtPlugAudioSync
{
    public class SoundCapture
    {
        public int minFreq = 5;
        public int maxFreq = 4500;
        public bool logScale = true;
        public bool isAverage = false;

        public float highScaleAverage = 10.0f;
        public float highScaleNotAverage = 12.0f;
        
        readonly ConsoleSpectrum spectrum;
        readonly WasapiCapture capture;
        readonly FftSize fftSize;
        readonly BasicSpectrumProvider spectrumProvider;
        readonly IWaveSource finalSource;
        
        private float[] fftBuffer;

        public SoundCapture()
        {

            // This uses the wasapi api to get any sound data played by the computer
            capture = new WasapiLoopbackCapture();

            capture.Initialize();

            // Get our capture as a source
            IWaveSource source = new SoundInSource(capture);


            // From https://github.com/filoe/cscore/blob/master/Samples/WinformsVisualization/Form1.cs

            // This is the typical size, you can change this for higher detail as needed
            fftSize = FftSize.Fft4096;

            // Actual fft data
            fftBuffer = new float[(int)fftSize];

            // These are the actual classes that give you spectrum data
            // The specific vars of lineSpectrum here aren't that important because they can be changed by the user
            spectrumProvider = new BasicSpectrumProvider(capture.WaveFormat.Channels,
                        capture.WaveFormat.SampleRate, fftSize);

            spectrum = new ConsoleSpectrum(fftSize)
            {
                SpectrumProvider = spectrumProvider,
                UseAverage = true,
                IsXLogScale = false,
                ScalingStrategy = ScalingStrategy.Linear
            };

            // Tells us when data is available to send to our spectrum
            var notificationSource = new SingleBlockNotificationStream(source.ToSampleSource());

            notificationSource.SingleBlockRead += NotificationSource_SingleBlockRead;

            // We use this to request data so it actualy flows through (figuring this out took forever...)
            finalSource = notificationSource.ToWaveSource();

            capture.DataAvailable += Capture_DataAvailable;
            capture.Start();
        }

        private void Capture_DataAvailable(object sender, DataAvailableEventArgs e)
        {
            finalSource.Read(e.Data, e.Offset, e.ByteCount);
        }

        private void NotificationSource_SingleBlockRead(object sender, SingleBlockReadEventArgs e)
        {
            spectrumProvider.Add(e.Left, e.Right);
        }

        ~SoundCapture()
        {
            capture.Stop();
            capture.Dispose();
        }

        public float[] barData = new float[20];

        public float[] GetFFtData(float multiplier)
        {
            if (spectrumProvider.IsNewDataAvailable)
            {
                spectrum.MinimumFrequency = minFreq;
                spectrum.MaximumFrequency = maxFreq;
                spectrum.IsXLogScale = logScale;
                spectrum.SpectrumProvider.GetFftData(fftBuffer, this);
                return spectrum.GetSpectrumPoints(100.0f, fftBuffer, multiplier);
            }
            else
            {
                return null;
            }
        }

        public float GetData(float multiplier, bool print)
        {
            if (multiplier < 0.0f)
            {
                multiplier = 0;
            }

            float[] resData = GetFFtData(multiplier);

            if (resData == null)
            {
                return 0;
            }

            //lock (barData)
            //{
            //    for (int i = 0; i < barData.Length && i < resData.Length; i++)
            //    {
            //        // Make the data between 0.0 and 1.0
            //        barData[i] = resData[i] / 100.0f;
            //    }

            //    for (int i = 0; i < barData.Length && i < resData.Length; i++)
            //    {
            //        if (spectrum.UseAverage)
            //        {
            //            // Scale the data because for some reason bass is always loud and treble is soft
            //            barData[i] = barData[i] + highScaleAverage * (float)Math.Sqrt(i / (barData.Length + 0.0f)) * barData[i];
            //        }
            //        else
            //        {
            //            barData[i] = barData[i] + highScaleNotAverage * (float)Math.Sqrt(i / (barData.Length + 0.0f)) * barData[i];
            //        }
            //    }
            //}

            var arrayBassPart = resData.Length / 2;
            float averageValue = resData.Select((value, index) => new { value, index }).Where(x => x.index < arrayBassPart).Average(x => x.value);

            var normalized = Math.Clamp(averageValue / 10.0f, 0, 1);
            normalized = normalized < 0.01f ? 0 : normalized;

            if (print)
            {
                PrintData(resData, averageValue, normalized, multiplier);
            }


            return normalized;
        }

        private static void PrintData(float[] dataCollection, float average, float normalized, float multiplier)
        {
            var yOffset = 2;
            for (var x = 0; x < dataCollection.Length; x++)
            {
                var value = dataCollection[x];
                var xOffset = (x * 4) + 1;
                for (var y = 0; y < 10; y++)
                {
                    var character = value >= (10 - y) ? "##" : "  ";

                    Console.SetCursorPosition(xOffset, y + yOffset + 1);
                    Console.Write(character);
                }

                Console.SetCursorPosition(xOffset, yOffset);
                Console.Write("--");
                Console.SetCursorPosition(xOffset, 10 + yOffset + 1);
                Console.Write("--");
            }

            {
                var xOffset = (dataCollection.Length * 4) + 5;
                for (var y = 0; y < 10; y++)
                {
                    var character = average >= (10 - y) ? "#####" : "     ";

                    Console.SetCursorPosition(xOffset, y + yOffset + 1);
                    Console.Write(character);
                }

                Console.SetCursorPosition(xOffset, yOffset);
                Console.Write("-----");
                Console.SetCursorPosition(xOffset, 10 + yOffset + 1);
                Console.Write("-----");

                Console.SetCursorPosition(xOffset + 6, 5 + yOffset + 1);
                Console.Write("Intensity: {0:0.000}", normalized);
                Console.SetCursorPosition(xOffset + 6, 5 + yOffset + 2);
                Console.Write("Multiplier: {0:0.0}", multiplier);
            }
        }
    }
}
