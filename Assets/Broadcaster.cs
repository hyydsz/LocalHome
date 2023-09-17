using System;

namespace Mi.Assets
{

    public class Broadcaster
    {
        public event EventHandler<string> onLanguageChange;
        public void LanguageChange(string language)
        {
            onLanguageChange?.Invoke(this, language);
        }

        public static Broadcaster instance = new Broadcaster();
    }
}
