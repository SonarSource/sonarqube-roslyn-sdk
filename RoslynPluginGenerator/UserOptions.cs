namespace SonarQube.Plugins.Roslyn
{
    public class UserOptions
    {
        private readonly bool _combineIdAndName;

        public bool CombineIdAndName { get { return _combineIdAndName; } }

        public UserOptions(bool combineIdAndName)
        {
            _combineIdAndName = combineIdAndName;
        }

    }
}