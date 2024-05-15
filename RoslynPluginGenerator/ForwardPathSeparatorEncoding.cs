using System.Text;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Replaces the back slash to foward slash to attend the international convention of directories separation.
    /// This will allow the zip file to be unzipped the correct way both linux and windows OS's.
    /// </summary>
    public class ForwardPathSeparatorEncoding : UTF8Encoding
    {
        public ForwardPathSeparatorEncoding() : base(true, true) { }

        public override byte[] GetBytes(string s)
        {
            s = s.Replace("\\", "/");
            return base.GetBytes(s);
        }
    }
}