using ManagedDoom;
using ManagedDoom.Video;

namespace WindbgDoom
{
    internal sealed class WindbgVideo : IVideo
    {
        private readonly Renderer _doomRenderer;
        private readonly byte[] _textureData;
        private readonly AsciiRenderer _ascii;

        public WindbgVideo(Config config, GameContent content, DbgEngOutput output, int charsW, int charsH)
        {
            _doomRenderer = new Renderer(config, content);
            _textureData = new byte[4 * _doomRenderer.Width * _doomRenderer.Height];
            _ascii = new AsciiRenderer(output, _doomRenderer.Width, _doomRenderer.Height, charsW, charsH);
        }

        public int Width => _doomRenderer.Width;
        public int Height => _doomRenderer.Height;

        public void Render(Doom doom, Fixed frameFrac)
        {
            _doomRenderer.Render(doom, _textureData, frameFrac);
            _ascii.PushFrame(_textureData);
        }

        public void InitializeWipe() => _doomRenderer.InitializeWipe();

        public bool HasFocus() => true;

        public int MaxWindowSize => _doomRenderer.MaxWindowSize;
        public int WindowSize { get => _doomRenderer.WindowSize; set => _doomRenderer.WindowSize = value; }

        public bool DisplayMessage { get => _doomRenderer.DisplayMessage; set => _doomRenderer.DisplayMessage = value; }

        public int MaxGammaCorrectionLevel => _doomRenderer.MaxGammaCorrectionLevel;
        public int GammaCorrectionLevel { get => _doomRenderer.GammaCorrectionLevel; set => _doomRenderer.GammaCorrectionLevel = value; }

        public int WipeBandCount => _doomRenderer.WipeBandCount;
        public int WipeHeight => _doomRenderer.WipeHeight;
    }
}
