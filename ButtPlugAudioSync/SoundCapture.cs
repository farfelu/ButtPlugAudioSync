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

        ConsoleSpectrum spectrum;

        WasapiCapture capture;
        FftSize fftSize;
        float[] fftBuffer;

        BasicSpectrumProvider spectrumProvider;

        IWaveSource finalSource;

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

        public float[] GetFFtData()
        {
            if (spectrumProvider.IsNewDataAvailable)
            {
                spectrum.MinimumFrequency = minFreq;
                spectrum.MaximumFrequency = maxFreq;
                spectrum.IsXLogScale = logScale;
                spectrum.SpectrumProvider.GetFftData(fftBuffer, this);
                return spectrum.GetSpectrumPoints(100.0f, fftBuffer);
            }
            else
            {
                return null;
            }
        }

        public void ComputeData()
        {
            float[] resData = GetFFtData();

            if (resData == null)
            {
                return;
            }

            lock (barData)
            {
                //for (int i = 0; i < barData.Length && i < resData.Length; i++)
                //{
                //    // Make the data between 0.0 and 1.0
                //    barData[i] = resData[i] / 100.0f;
                //}

                for (int i = 0; i < barData.Length && i < resData.Length; i++)
                {
                    if (spectrum.UseAverage)
                    {
                        // Scale the data because for some reason bass is always loud and treble is soft
                        barData[i] = barData[i] + highScaleAverage * (float)Math.Sqrt(i / (barData.Length + 0.0f)) * barData[i];
                    }
                    else
                    {
                        barData[i] = barData[i] + highScaleNotAverage * (float)Math.Sqrt(i / (barData.Length + 0.0f)) * barData[i];
                    }
                }
            }

            PrintData(resData);
        }

        private void PrintData(float[] dataCollection)
        {
            var yOffset = 2;
            for (var x = 0; x < dataCollection.Length; x++)
            {
                var value = (int)Math.Floor(dataCollection[x]);
                var xOffset = (x * 4) + 1;
                for (var y = 0; y < 10; y++)
                {
                    var character = value >= (10 - y) ? "#" : " ";

                    Console.SetCursorPosition(xOffset, y + yOffset);
                    Console.Write(character);
                    Console.SetCursorPosition(xOffset + 1, y + yOffset);
                    Console.Write(character);
                }
            }


            var arrayHalfSize = dataCollection.Length / 2;
            int averageValue = (int)Math.Floor(dataCollection.Select((value, index) => new { value, index }).Where(x => x.index < arrayHalfSize).Average(x => x.value));

            for (var x = 0; x < 4; x++)
            {
                var xOffset = (dataCollection.Length * 4) + 10;
                for (var y = 0; y < 10; y++)
                {
                    var character = averageValue >= (10 - y) ? "#" : " ";

                    Console.SetCursorPosition(x + xOffset, y + yOffset);
                    Console.Write(character);
                    Console.SetCursorPosition(x + xOffset, y + yOffset);
                    Console.Write(character);
                }
            }
        }
    }
}
