using System.Text;

namespace XDonation.Helpers
{
    public class TsplBuilder
    {
        private StringBuilder _tspl = new StringBuilder();

        public TsplBuilder Size(string width, string height)
        {
            _tspl.AppendLine($"SIZE {width}, {height}");
            return this;
        }

        public TsplBuilder Gap(string m, string n)
        {
            _tspl.AppendLine($"GAP {m}, {n}");
            return this;
        }

        public TsplBuilder Density(int density)
        {
            _tspl.AppendLine($"DENSITY {density}");
            return this;
        }

        public TsplBuilder Speed(int speed)
        {
            _tspl.AppendLine($"SPEED {speed}");
            return this;
        }

        public TsplBuilder Direction(int direction, int mirror = 0)
        {
            _tspl.AppendLine($"DIRECTION {direction},{mirror}");
            return this;
        }

        public TsplBuilder Reference(int x, int y)
        {
            _tspl.AppendLine($"REFERENCE {x},{y}");
            return this;
        }

        public TsplBuilder Cls()
        {
            _tspl.AppendLine("CLS");
            return this;
        }

        public TsplBuilder Text(int x, int y, string font, int rotation, int xMul, int yMul, string text)
        {
            _tspl.AppendLine($"TEXT {x},{y},\"{font}\",{rotation},{xMul},{yMul},\"{TsplEscape(text)}\"");
            return this;
        }

        public TsplBuilder Barcode(int x, int y, string codeType, int height, int humanReadable, int rotation, int narrow, int wide, string content)
        {
            _tspl.AppendLine($"BARCODE {x},{y},\"{codeType}\",{height},{humanReadable},{rotation},{narrow},{wide},\"{TsplEscape(content)}\"");
            return this;
        }

        public TsplBuilder Print(int sets, int copies = 1)
        {
            _tspl.AppendLine($"PRINT {sets},{copies}");
            return this;
        }

        public string Build()
        {
            return _tspl.ToString();
        }

        private string TsplEscape(string text)
        {
            return text?.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ") ?? "";
        }
    }
}
