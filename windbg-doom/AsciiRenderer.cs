using System.Text;

namespace WindbgDoom
{
    internal sealed class AsciiRenderer
    {
        private const string Ramp = " .'`,:;-+*#%@&$";

        private readonly DbgEngOutput _output;
        private readonly int _srcW;
        private readonly int _srcH;
        private readonly int _charsW;
        private readonly int _charsH;
        private readonly StringBuilder _sb;

        public AsciiRenderer(DbgEngOutput output, int srcWidth, int srcHeight, int charsW, int charsH)
        {
            _output = output;
            _srcW = srcWidth;
            _srcH = srcHeight;
            _charsW = charsW;
            _charsH = charsH;
            _sb = new StringBuilder(charsW * charsH + charsH * 2 + 32);
        }

        public void PushFrame(byte[] source)
        {
            _sb.Clear();
            int rampLast = Ramp.Length - 1;
            int rampLen = Ramp.Length;

            for (int row = 0; row < _charsH; row++)
            {
                int yStart = (int)((long)row * _srcH / _charsH);
                int yEnd = (int)((long)(row + 1) * _srcH / _charsH);
                if (yEnd <= yStart) yEnd = yStart + 1;

                for (int col = 0; col < _charsW; col++)
                {
                    int xStart = (int)((long)col * _srcW / _charsW);
                    int xEnd = (int)((long)(col + 1) * _srcW / _charsW);
                    if (xEnd <= xStart) xEnd = xStart + 1;

                    int sum = 0;
                    int count = 0;
                    for (int x = xStart; x < xEnd; x++)
                    {
                        int colBase = x * _srcH * 4;
                        for (int y = yStart; y < yEnd; y++)
                        {
                            int p = colBase + y * 4;
                            sum += source[p] + source[p + 1] + source[p + 2];
                            count++;
                        }
                    }

                    int idx = (int)((long)sum * rampLen / (count * 3 * 256));
                    if (idx > rampLast) idx = rampLast;
                    _sb.Append(Ramp[idx]);
                }
                _sb.Append('\n');
            }

            _output.Write(_sb.ToString());
        }
    }
}
