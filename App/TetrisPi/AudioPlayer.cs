using ManagedBass;

namespace TetrisApp
{
    public class AudioPlayer : IDisposable
    {
        private bool isPlaying = false;
        private object lockObj = new object();

        public bool Init(int deviceIndex = 0)
        {
            return Bass.Init(deviceIndex);
        }

        public void Play(string fileName)
        {
            if (isPlaying)
                return;

            lock (lockObj)
            {
                isPlaying = true;
            }

            Task.Run(() => PlayAudioAsync(fileName));
        }

        private async Task PlayAudioAsync(string fileName)
        {
            string audioFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice", fileName);

            int stream = Bass.CreateStream(audioFilePath);
            if (stream == 0)
            {
                lock (lockObj)
                {
                    isPlaying = false;
                }
                return;
            }

            Bass.ChannelSetAttribute(stream, ChannelAttribute.Volume, 1.0f);

            Bass.ChannelPlay(stream);

            while (Bass.ChannelIsActive(stream) == PlaybackState.Playing)
            {
                await Task.Delay(100);
            }

            Bass.StreamFree(stream);

            lock (lockObj)
            {
                isPlaying = false;
            }
        }

        public void Dispose()
        {
            Bass.Free();
        }
    }
}
