using elFinder.NetCore.AzureBlobStorage.Driver.Models;

namespace elFinder.NetCore.AzureBlobStorage.Driver.Helpers
{

    public static class StringHelpers
    {
        public static PrefixedString Split(char separator, string str)
        {
            string prefix = null;
            string content = null;

            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] != separator) continue;

                content = str.Substring(i + 1);
                prefix = str.Substring(0, i + 1);

                break;
            }

            return new PrefixedString
            {
                Prefix = prefix,
                Content = content
            };
        }
    }

}