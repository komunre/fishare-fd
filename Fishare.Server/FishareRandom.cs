using System;

namespace Fishare.Server {
    public static class FishareRandom {
        private const string alphabet = "abcdefghiklmnopqrstuvwxyyz";
        public static string RandomString() {
            string result = "";
            Random random = new Random();

            for (int i = 0; i < 25; i++) {
                int letter = random.Next(alphabet.Length);
                char ch = alphabet[letter];
                result += ch;
            }

            return result;
        }
    }
}
